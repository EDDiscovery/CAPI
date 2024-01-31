/*
 * Copyright © 2021-2024 Robby & EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 */

 //#define CONSOLETESTHARNESS

using QuickJSON;
using System;
using System.IO;

namespace CAPI
{
    //To reset for test:
    //Delete From JournalEntries Where CommanderID=9;
    //Delete From TravelLogUnit Where path="C:\Users\RK\AppData\Local\EDDiscovery\CAPI";
    //Update Commanders Set Options = "{""CONSOLE"":true}" Where id = 9;
    //clean out CAPI folder of .logs
    //set up journal files for each day in c:\code\journal.21-03-13.log
    //enable

    public partial class CompanionAPI
    {
        public JObject ManageJournalDownload(JObject lasthistory, string storepath, string cmdrname, TimeSpan checktime, int daysinpast, 
                            Action<string> reportback = null, int pauseformessage = 1 )
        {
            //System.Diagnostics.Debug.WriteLine("---------------------- Journal console check @ " + DateTime.UtcNow.ToStringZulu());

            JObject newhistory = new JObject();
            const string datekeyformat = "yyyy-MM-dd";
            string todo = null;

            // Go back and check state of the last days search.. find one to process.  update newhistory with lasthistory 
            for (int day = -daysinpast; day <= 0; day++)
            {
                DateTime t = DateTime.UtcNow.AddDays(day).StartOfDay();
                string tname = t.ToString(datekeyformat, System.Globalization.CultureInfo.InvariantCulture);

                JToken value = null;
                lasthistory?.TryGetValue(tname, out value);           // value = null if not got

                if (value != null)
                    newhistory.Add(tname, value);                   // applicable, copy across

                string state = value.I("S").Str("NotTried");        // state of play

                //System.Diagnostics.Debug.WriteLine(tname + " Journal check: " + value?.ToString());

                if (todo == null)
                {
                    // either nottried (no record) or in a Check state and not too soon
                    if (state == "NotTried" || ((state.StartsWith("Check") ) && DateTime.UtcNow - value["T"].DateTimeUTC() >= checktime))   
                    {
                        todo = tname;
                    }
                }
            }

            if (todo != null)          // found one to try
            {
                reportback?.Invoke("CAPI Journal check " + todo);
                System.Diagnostics.Trace.WriteLine("Journal Check day " + todo + " @ " + DateTime.UtcNow.ToStringZulu());

                string journaljson = null;

#if CONSOLETESTHARNESS
                string subfile = @"c:\code\journal." + todo + ".log";                   // for the test, we pick up this file for this day

                System.Net.HttpStatusCode status = System.Net.HttpStatusCode.NoContent;
                if ( File.Exists(subfile))
                {
                    journaljson = File.ReadAllText(subfile);
                    status = System.Net.HttpStatusCode.OK;
                }
#else
                journaljson = Journal(todo, out System.Net.HttpStatusCode status);      // real code polls CAPI
#endif

                string dayzeroname = DateTime.UtcNow.StartOfDay().ToString(datekeyformat, System.Globalization.CultureInfo.InvariantCulture);     // name of current day in this system

                if (status == System.Net.HttpStatusCode.NoContent)
                {
                    // server says no content for the day. If its a previous day, its over. Else we are in check1 continuously because the game might start
                    reportback?.Invoke("CAPI Journal no content on server for " + todo);

                    newhistory[todo] = new JObject() { ["S"] = todo==dayzeroname ? "Check1" : "NoContent", ["T"] = DateTime.UtcNow.ToStringZulu() };
                }
                else if (journaljson != null)
                {
                    //File.WriteAllText(@"c:\code\readjournal.log",journaljson);

                    string filename = Path.Combine(storepath, (CAPIServer == CAPIServerType.Beta ? "JournalBeta." : "Journal.") + cmdrname.SafeFileString() + "." + todo + ".log");

                    string prevcontent = null;

                    if (File.Exists(filename))                          // if file there, try and read the lines, and if so, store
                    {
                        prevcontent = BaseUtils.FileHelpers.TryReadAllTextFromFile(filename);
                    }

                    string samesecondsegment = null;                      
                    string samesecondtimestamp = "";
                    string newoutput = "";

                    StringReader sr = new StringReader(journaljson);
                    string curline;
                    while ((curline = sr.ReadLine()) != null)
                    {
                        if (curline.HasChars())
                        {
                            JObject ev = JObject.Parse(curline);        // lets sanity check it..
                            if (ev != null && ev.Contains("event") && ev.Contains("timestamp"))     // reject lines which are not valid json records
                            {
                                string evtype = ev["event"].Str();
                                if (evtype == "Commander")              // we adjust commander/loadgame commander name to our commander name - so
                                {                                       // when we scan it it goes into the right history, irrespective of the naming of the commander 
                                                                        // vs the game
                                    ev["Name"] = cmdrname;              // adjust commander name, rewrite again
                                    curline = ev.ToString(" ");
                                }
                                else if (evtype == "LoadGame")
                                {
                                    ev["Commander"] = cmdrname;         // adjust commander name, rewrite again
                                    curline = ev.ToString(" ");
                                }

                                if (prevcontent == null)              // no previous file, just add
                                {
                                    newoutput += curline + Environment.NewLine;
                                }
                                else
                                {
                                    // we accumulate all entries with the same second (they cannot be distinguished if events are identical, so can't use a hashset same line check)
                                    // so that we have a group with the same second, then we see if its in the previous content

                                    string ts = ev["timestamp"].Str();

                                    if (samesecondtimestamp == ts)      // if same timestamp as previous, accumulate
                                    {
                                        //System.Diagnostics.Debug.WriteLine("  {0} Same segment {1}", ts, curline.Left(80));
                                        samesecondsegment += curline + Environment.NewLine;
                                    }
                                    else
                                    {
                                        if (samesecondsegment == null || prevcontent.Contains(samesecondsegment))      // duplicate segment, ignore
                                        {
                                            //  System.Diagnostics.Debug.WriteLine(samesecondsegment != null ? ".. Duplicate data " + ts + " " + samesecondsegment.Left(80): "");
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine("  " + ts + " New data");
                                            System.Diagnostics.Debug.WriteLine(samesecondsegment.LineNumbering(1, "#"));
                                            newoutput += samesecondsegment;
                                        }

                                        samesecondsegment = curline + Environment.NewLine;      // start a new timestamp
                                        samesecondtimestamp = ts;
                                    }
                                }
                            }
                        }
                    }

                    if (samesecondsegment.HasChars() && (prevcontent == null || !prevcontent.Contains(samesecondsegment)))      // clean up last segment
                    {
                        System.Diagnostics.Debug.WriteLine("  " + samesecondtimestamp + " New data");
                        System.Diagnostics.Debug.WriteLine(samesecondsegment.LineNumbering(1, "#"));
                        newoutput += samesecondsegment;
                    }

                    string stateout = "Check1";     // default is to go to check 1 state

                    if (newoutput.HasChars())       // we have new data, so we go into check1 and it will be downloaded again later
                    {
                        reportback?.Invoke("CAPI Journal records found for " + todo);
                        System.Diagnostics.Trace.WriteLine(string.Format("..{0} New content for {1}", todo, filename));
                        System.IO.File.WriteAllText(filename, (prevcontent ?? "") + newoutput);
                    }
                    else
                    {
                        reportback?.Invoke("CAPI Journal no new records found for " + todo);
                        System.Diagnostics.Trace.WriteLine(string.Format("..{0} No change for {1}", todo, filename));
                        string instate = lasthistory[todo].I("S").Str("NotTried");

                        if (instate == "Check1" && todo != dayzeroname)        // 1->2 only if not day0
                            stateout = "Check2";
                        else if (instate == "Check2")   // 2->Done
                            stateout = "Done";          // otherwise 1
                    }

                    newhistory[todo] = new JObject() { ["S"] = stateout, ["T"] = DateTime.UtcNow.ToStringZulu() };        // no new data, mark done.

                    System.Diagnostics.Trace.WriteLine(".. to state " + newhistory[todo].ToString());
                }
                else
                {
                    reportback?.Invoke("CAPI Journal no response from server");
                    System.Diagnostics.Trace.WriteLine("  No response to " + todo + " (" + status.ToString() + ") will try again");
                }
            }

            System.Threading.Thread.Sleep(pauseformessage);     // small pause to let message show
            reportback?.Invoke("");

            //System.Diagnostics.Debug.WriteLine("--------------- finished " + newhistory.ToString());

            return newhistory;
        }
    }
}

