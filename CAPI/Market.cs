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

namespace CAPI
{
    // this decodes the Market endpoint
    // Note, if not docked, you may get an empty {} json back
    // then all entries will return null. Use IsValid to know you got data, but you still need to check each for null
    // as its frontiers data and they may have left the node out

    public class Market
    {
        public Market(string profile)
        {
            json = JToken.Parse(profile, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL);
        }

        public bool IsValid { get { return json != null && ID != long.MinValue && Name != null; } }

        // Commander

        public long ID { get { return json["id"].Long(long.MinValue); } }
        public string Name { get { return json["name"].StrNull(); } }
        public string Type { get { return json["outpostType"].StrNull(); } }

        // id name pairs
        public Dictionary<string, string> Imports { get { return json["imported"].Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, string> Exports { get { return json["exported"].Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, string> Services { get { return json["services"].Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, string> Prohibited { get { return json["prohibited"].Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, double> Economies
        {
            get
            {
                JObject data = json["economies"].Object();
                var list = new Dictionary<string, double>();
                if (data != null)
                {
                    foreach (var e in data)
                        list.Add(e.Value["name"].Str("Unknown"), e.Value["proportion"].Double()*100.0);
                }

                return list;
            }
        }

        public JArray Commodities { get { return json["commodities"].Array(); } }

        public class Commodity
        {
            public long ID;
            public string Name;
            public string LocName;
            public string Legality;
            public long Buy;
            public long Sell;
            public long Mean;
            public long DemandBracket;
            public long StockBracket;
            public long Stock;
            public long Demand;
            public string Category;
        }

        public List<Commodity> GetCommodities()
        {
            JArray clist = json.I("commodities").Array();
            if ( clist != null )
            {
                List<Commodity> list = new List<Commodity>();
                foreach( var entry in clist)
                {
                    JObject data = entry.Object();
                    if ( data != null )
                    {
                        Commodity m = new Commodity()
                        {
                            ID = data["id"].Long(),
                            Name = data["name"].Str(),
                            Legality = data["legality"].Str(),
                            Buy = data["buyPrice"].Long(),
                            Sell= data["sellPrice"].Long(),
                            Mean = data["meanPrice"].Long(),
                            DemandBracket = data["demandBracket"].Long(),
                            StockBracket = data["stockBracket"].Long(),
                            Stock = data["stock"].Long(),
                            Demand = data["demand"].Long(),
                            Category = data["categoryname"].Str(),
                            LocName = data["locName"].Str(),
                        };

                        list.Add(m);
                    }

                }

                return list;
            }
            return null;
        }

        private BaseUtils.JSON.JToken json;
    }
}
