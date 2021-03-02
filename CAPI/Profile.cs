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
using System.Collections.Generic;

namespace CAPI
{
    // this decodes the JSON profile from Frontiers CAPI for you

    public class Profile
    {
        public Profile(string profile)
        {
            json = JToken.Parse(profile, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL);
        }

        public bool IsValid { get { return json != null && ID != long.MinValue && Commander != null; } }

        // Commander

        public long ID { get { return json["commander"].I("id").Long(long.MinValue); } }
        public string Commander { get { return json["commander"].I("name").StrNull(); } }
        public long Credits { get { return json["commander"].I("credits").Long(0); } }
        public long Debt { get { return json["commander"].I("debt").Long(0); } }
        public bool Docked { get { return json["commander"].I("docked").Bool(); } }
        public int ShipID { get { return json["commander"].I("currentShipId").Int(-1); } }
        public int RankCombat { get { return json["commander"].I("rank").I("combat").Int(-1); } }
        public int RankTrade { get { return json["commander"].I("rank").I("trade").Int(-1); } }
        public int RankExplore { get { return json["commander"].I("rank").I("explore").Int(-1); } }
        public int RankCrime { get { return json["commander"].I("rank").I("crime").Int(-1); } }
        public int RankService { get { return json["commander"].I("rank").I("service").Int(-1); } }
        public int RankEmpire { get { return json["commander"].I("rank").I("empire").Int(-1); } }
        public int RankFederation { get { return json["commander"].I("rank").I("federation").Int(-1); } }
        public int RankPower { get { return json["commander"].I("rank").I("power").Int(-1); } }
        public int RankCQC { get { return json["commander"].I("rank").I("cqc").Int(-1); } }

        public bool CobraMkIV { get { return json["commander"].I("capabilities").I("AllowCobraMkIV").Bool(); } }
        public bool Horizons { get { return json["commander"].I("capabilities").I("Horizons").Bool(); } }

        // System

        public string System { get { return json.I("lastSystem").I("name").StrNull(); } }
        public long SystemAddress { get { return json.I("lastSystem").I("id").Long(); } }
        public string SystemMajorFaction { get { return json.I("lastSystem").I("faction").Str("Unknown"); } }

        // Starport ( check docked)

        public string StarPort { get { return json.I("lastStarport").I("name").StrNull(); } }
        public long StarPortID { get { return json.I("lastStarport").I("id").Long(); } }
        public Dictionary<string, string> StarPortServices { get { return json["lastStarport"].I("services").Object()?.ToObject<Dictionary<string, string>>(); } }
        public string StarPortMajorFaction { get { return json.I("lastStarport").I("faction").StrNull(); } }
        public string StarPortMinorFaction { get { return json.I("lastStarport").I("minorfaction").StrNull(); } }

        // Ship

        public string Ship { get { return json.I("ship").I("name").StrNull(); } }
        public string ShipName { get { return json.I("ship").I("shipName").StrNull(); } }
        public string ShipIdent { get { return json.I("ship").I("shipID").StrNull(); } }
        public long ShipHullValue { get { return json.I("ship").I("value").I("hull").Long(); } }
        public long ShipModuleValue { get { return json.I("ship").I("value").I("modules").Long(); } }
        public long ShipCargo { get { return json.I("ship").I("value").I("cargo").Long(); } }
        public long ShipTotalValue { get { return json.I("ship").I("value").I("total").Long(); } }
        public long ShipInsurance { get { return json.I("ship").I("value").I("unloaned").Long(); } }
        public double ShipHealth { get { return json.I("ship").I("health").I("hull").Double() / 10000.0; } }      // in %, 0-100
        public double ShipShield { get { return json.I("ship").I("health").I("shield").Double() / 10000.0; } }    // in %, 0-100
        public bool ShipShieldUp { get { return json.I("ship").I("health").I("shieldup").Bool(); } }
        public long ShipIntegrity { get { return json.I("ship").I("value").I("integrity").Long(); } }
        public long ShipPaintwork { get { return json.I("ship").I("value").I("paintwork").Long(); } }
        public bool ShipCockpitBreached { get { return json.I("ship").I("cockpitBreached").Bool(); } }
        public double OxygenRemaining { get { return json.I("ship").I("oxygenRemaining").Long(); } }        // don't know quantities

        public class Module
        {
            public string Location;
            public string Name;
            public string LocName;
            public string LocDescription;
            public long Value;
            public bool Free;
            public double Health;
            public bool On;
            public int Priority;
            public long ID;
        }

        public List<Module> GetModules()  // may return null
        {
            JObject moduleslist = json.I("ship").I("modules").Object();
            if ( moduleslist != null )
            {
                List<Module> list = new List<Module>();
                foreach( var kvp in moduleslist)
                {
                    JObject data = kvp.Value["module"].Object();
                    if ( data != null )
                    {
                        Module m = new Module()
                        {
                            Location = kvp.Key,
                            ID = data["id"].Long(),
                            Name = data["name"].Str(),
                            LocName = data["locName"].Str(),
                            LocDescription = data["locDescription"].Str(),
                            Value = data["value"].Long(),
                            Free = data["free"].Bool(),
                            Health = data["health"].Double()/10000.0,
                            On = data["on"].Bool(),
                            Priority = data["priority"].Int(),
                        };

                        list.Add(m);
                    }

                }

                return list;
            }
            return null;
        }

        public class ShipInfo
        {
            public long ID;
            public string Name;
            public long ValueHull;
            public long ValueModules;
            public int Cargo;
            public long ValueTotal;
            public long Insurance;
            public bool Free;
            public long StationID;
            public string Station;
            public string System;
            public long SystemAddress;
        }

        public List<ShipInfo> GetShips()
        {
            JObject shiplist = json.I("ships").Object();
            if (shiplist != null)
            {
                var list = new List<ShipInfo>();
                foreach (var kvp in shiplist)
                {
                    JObject data = kvp.Value.Object();
                    if (data != null)
                    {
                        ShipInfo m = new ShipInfo()
                        {
                            ID = data["id"].Long(),
                            Name = data["name"].Str(),
                            ValueHull = data["value"].I("hull").Long(),
                            ValueModules = data["value"].I("modules").Long(),
                            ValueTotal = data["value"].I("total").Long(),
                            Cargo = data["value"].I("cargo").Int(),
                            Insurance = data["value"].I("unloaned").Long(),
                            Free = data["free"].Bool(),
                            StationID = data["station"].I("id").Long(),
                            Station = data["station"].I("name").Str(),
                            SystemAddress = data["starsystem"].I("id").Long(),
                            System = data["starsystem"].I("name").Str(),
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
