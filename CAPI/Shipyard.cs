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
    // this decodes the JSON profile from Frontiers CAPI for you
    // Note, if not docked, you may get an empty {} json back
    // then all entries will return null. Use IsValid to know you got data, but you still need to check each for null
    // as its frontiers data and they may have left the node out

    public class Shipyard
    {
        public Shipyard(string profile)
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
        public Dictionary<string, double> Economies
        {
            get
            {
                JObject data = json["economies"].Object();
                if (data != null)
                {
                    var list = new Dictionary<string, double>();
                    foreach (var e in data)
                        list.Add(e.Value["name"].Str("Unknown"), e.Value["proportion"].Double() * 100.0);
                    return list;
                }
                else
                    return null;
            }
        }

        public class Module
        {
            public long ID;
            public string Category;
            public string Name;
            public long Cost;
            public long Stock;
        }

        public List<Module> GetModules()        // may be null if no shipyard
        {
            JObject moduleslist = json.I("modules").Object();
            if (moduleslist != null)
            {
                List<Module> list = new List<Module>();
                foreach (var kvp in moduleslist)
                {
                    JObject data = kvp.Value.Object();
                    if (data != null)
                    {
                        Module m = new Module()
                        {
                            ID = data["id"].Long(),
                            Category = data["category"].Str(),
                            Name = data["name"].Str(),
                            Cost = data["cost"].Long(),
                            Stock = data["stock"].Long(),
                        };

                        list.Add(m);
                    }

                }

                return list;
            }
            return null;
        }

        public class Ship
        {
            public long ID;
            public string Name;
            public long BaseValue;
            public string SKU;
        }

        public List<Ship> GetShips()        // may be null if no shipyard
        {
            JObject shiplist = json.I("ships").I("shipyard_list").Object();
            if (shiplist != null)
            {
                List<Ship> list = new List<Ship>();
                foreach (var kvp in shiplist)
                {
                    JObject data = kvp.Value.Object();
                    if (data != null)
                    {
                        Ship m = new Ship()
                        {
                            ID = data["id"].Long(),
                            Name = data["name"].Str(),
                            BaseValue = data["basevalue"].Long(),
                            SKU = data["sku"].Str(),
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
