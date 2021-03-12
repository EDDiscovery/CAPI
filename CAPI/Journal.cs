/*
 * Copyright © 2021 Robby & EDDiscovery development team
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
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using BaseUtils.JSON;
using System;
using System.Collections.Generic;
using System.IO;

namespace CAPI
{

    public partial class CompanionAPI
    {
        public JToken ManageJournalDownload(JToken settings, string storepath, string cmdrname, ref int refresh)
        {
            var history = settings != null ? settings.ToObject<Dictionary<DateTime, string>>() : new Dictionary<DateTime, string>();

            var newhistory = new Dictionary<DateTime, string>();

            for (int day = -20; day <= 0; day++)
            {
                DateTime t = DateTime.UtcNow.AddDays(day).StartOfDay();
                if (history.TryGetValue(t, out string value))     // we have a record for this day
                {
                    newhistory.Add(t, value);                      // lets store it in new history
                }

                value = null;

                if (value == null || value == "partial" || day == 0)  // no history, or partial, or current day
                {
                    if (day == 0 && value != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Day 0 was done, so next check will be a long time");
                        refresh = 10000;
                    }

                    System.Diagnostics.Debug.WriteLine("Check day time for " + t);

                    //string journaljson = Journal(t, out System.Net.HttpStatusCode status);

                    System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK;
                    string journaljson = File.ReadAllText(@"c:\code\journal.log");

                    if (status == System.Net.HttpStatusCode.NoContent)
                    {
                        newhistory[t] = "No Content";
                    }
                    else if (journaljson != null)
                    {
                        //File.WriteAllText(@"c:\code\journal.log",journaljson);

                        string filename = Path.Combine(storepath, "Journal.CAPI." + t.ToString("yy-MM-dd") + ".log");

                        var outfile = new System.Text.StringBuilder();      // where we build the new file

                        var curfilehash = new HashSet<string>();            // hash set, per line, in current file

                        if (File.Exists(filename))                          // if file there, try and read the lines, and if so, 
                        {
                            var filelines = BaseUtils.FileHelpers.TryReadAllLinesFromFile(filename);
                            if (filelines != null)
                            {
                                foreach (var l in filelines)                // add all lines to the hash set so we can detect repeats
                                {
                                    curfilehash.Add(l);
                                    outfile.Append(l);                      // and append to the outfile
                                    outfile.Append(Environment.NewLine);        // Verified this alog works on 12/3/21. Only updates if new lines
                                }
                            }
                        }

                        StringReader sr = new StringReader(journaljson);
                        string curline;
                        while( (curline = sr.ReadLine()) != null)
                        {
                            JObject ev = JObject.Parse(curline);        // lets sanity check it..
                            if (ev != null && ev.Contains("event") && ev.Contains("timestamp"))     // reject lines which are not valid json records
                            {
                                string evtype = ev["event"].Str();
                                if (evtype == "Commander")              // we adjust commander/loadgame commander name to our commander name - so
                                {                                       // when we scan it it goes into the right history, irrespective of the naming of the commander vs the game
                                    ev["Name"] = cmdrname;              // adjust commander name, rewrite again
                                    curline = ev.ToString();
                                }
                                else if (evtype == "LoadGame")          
                                {
                                    ev["Commander"] = cmdrname;         // adjust commander name, rewrite again
                                    curline = ev.ToString();
                                }

                                if ( !curfilehash.Contains(curline))     // if line,adjusted, is not in current file
                                {
                                    outfile.Append(curline);                // append to output
                                    outfile.Append(Environment.NewLine);
                                }
                                else
                                {       // for debugging
                                }
                            }
                            else
                            {   // for debugging
                            }
                        }


                        System.IO.File.WriteAllText(filename, outfile.ToNullSafeString());
                        newhistory[t] = status == System.Net.HttpStatusCode.PartialContent ? "Partial" : "Done";
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No response to " + t + " will try again");
                    }

                    break;
                }
            }

            return JToken.FromObject(newhistory);
        }
    }
}
