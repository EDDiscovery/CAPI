﻿/*
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

using QuickJSON;
using System;
using System.Collections.Generic;

namespace CAPI
{
    // this decodes the JSON profile from Frontiers CAPI for you

    public class Profile
    {
        public Profile(string profile,DateTime servertime)
        {
            json = JToken.Parse(profile, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL);
            ServerTimeUTC = servertime;
        }

        public DateTime ServerTimeUTC { get; set; }
        public bool IsValid { get { return json != null && ID != long.MinValue && Commander != null; } }

        // Commander

        public long ID { get { return json["commander"].I("id").Long(long.MinValue); } }
        public string Commander { get { return json["commander"].I("name").StrNull(); } }
        public long Credits { get { return json["commander"].I("credits").Long(0); } }
        public long Debt { get { return json["commander"].I("debt").Long(0); } }
        public bool Docked { get { return json["commander"].I("docked").Bool(); } }
        public int ShipID { get { return json["commander"].I("currentShipId").Int(-1); } }
        public bool OnFoot { get { return json["commander"].I("onfoot").Bool(); } } // odyssey
        public int RankCombat { get { return json["commander"].I("rank").I("combat").Int(-1); } }
        public int RankTrade { get { return json["commander"].I("rank").I("trade").Int(-1); } }
        public int RankExplore { get { return json["commander"].I("rank").I("explore").Int(-1); } }
        public int RankCrime { get { return json["commander"].I("rank").I("crime").Int(-1); } }
        public int RankService { get { return json["commander"].I("rank").I("service").Int(-1); } }
        public int RankEmpire { get { return json["commander"].I("rank").I("empire").Int(-1); } }
        public int RankFederation { get { return json["commander"].I("rank").I("federation").Int(-1); } }
        public int RankPower { get { return json["commander"].I("rank").I("power").Int(-1); } }
        public int RankCQC { get { return json["commander"].I("rank").I("cqc").Int(-1); } }
        public int RankSoldier { get { return json["commander"].I("rank").I("soldier").Int(-1); } } // odyssey
        public int RankExoBiologist { get { return json["commander"].I("rank").I("exobiologist").Int(-1); } } // odyssey
      
        public bool CobraMkIV { get { return json["commander"].I("capabilities").I("AllowCobraMkIV").Bool(); } }
        public bool Horizons { get { return json["commander"].I("capabilities").I("Horizons").Bool(); } }
        public bool Odyssey { get { return json["commander"].I("capabilities").I("Odyssey").Bool(); } } // odyssey

        // Last System
        public string System { get { return json.I("lastSystem").I("name").StrNull(); } }
        public long SystemAddress { get { return json.I("lastSystem").I("id").Long(); } }
        public string SystemMajorFaction { get { return json.I("lastSystem").I("faction").Str("Unknown"); } }

        // Last Starport (check docked)

        public string StarPort { get { return json.I("lastStarport").I("name").StrNull()?.TrimReplaceEnd('+'); } }
        public long StarPortID { get { return json.I("lastStarport").I("id").Long(); } }
        public Dictionary<string, string> StarPortServices { get { return json["lastStarport"].I("services").Object()?.ToObject<Dictionary<string, string>>(); } }
        public string StarPortMajorFaction { get { return json.I("lastStarport").I("faction").StrNull(); } }
        public string StarPortMinorFaction { get { return json.I("lastStarport").I("minorfaction").StrNull(); } }

        // Ship

        public string Ship { get { return json.I("ship").I("name").StrNull(); } }
        public long ShipHullValue { get { return json.I("ship").I("value").I("hull").Long(); } }
        public long ShipModuleValue { get { return json.I("ship").I("value").I("modules").Long(); } }
        public long ShipCargo { get { return json.I("ship").I("value").I("cargo").Long(); } }
        public long ShipTotalValue { get { return json.I("ship").I("value").I("total").Long(); } }
        public long ShipInsurance { get { return json.I("ship").I("value").I("unloaned").Long(); } }
        public string ShipName { get { return json.I("ship").I("shipName").StrNull(); } }
        public string ShipIdent { get { return json.I("ship").I("shipID").StrNull(); } }
        public double ShipHealth { get { return json.I("ship").I("health").I("hull").Double() / 10000.0; } }      // in %, 0-100
        public double ShipShield { get { return json.I("ship").I("health").I("shield").Double() / 10000.0; } }    // in %, 0-100
        public bool ShipShieldUp { get { return json.I("ship").I("health").I("shieldup").Bool(); } }
        public long ShipIntegrity { get { return json.I("ship").I("value").I("integrity").Long(); } }
        public long ShipPaintwork { get { return json.I("ship").I("value").I("paintwork").Long(); } }
        public bool ShipCockpitBreached { get { return json.I("ship").I("cockpitBreached").Bool(); } }
        public double ShipOxygenRemaining { get { return json.I("ship").I("oxygenRemaining").Long(); } }        // don't know quantities

        public class Module
        {
            public string Location{get;set;}
            public string Name{get;set;}
            public string LocName{get;set;}
            public string LocDescription{get;set;}
            public long Value{get;set;}
            public bool Free{get;set;}
            public double Health{get;set;}
            public bool On{get;set;}
            public int Priority{get;set;}
            public long ID{get;set;}
        }

        public List<Module> GetShipModules()  // may return null
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

        // launch bays

        public class LaunchBay
        {
            public string Location{get;set;}
            public string SubSlot{get;set;}
            public string Name{get;set;}
            public string LocName{get;set;}
            public int Rebuilds{get;set;}
            public string Loadout{get;set;}
            public string LoadoutName{get;set;}
        }

        public List<LaunchBay> GetShipLaunchBays()  // may return null
        {
            JObject baylist = json.I("ship").I("launchBays").Object();
            if (baylist != null)
            {
                List<LaunchBay> list = new List<LaunchBay>();
                foreach (var kvpslots in baylist)
                {
                    JObject subslots = kvpslots.Value.Object();
                    foreach (var kvpsubslots in subslots)
                    {
                        LaunchBay m = new LaunchBay()
                        {
                            Location = kvpslots.Key,
                            SubSlot = kvpsubslots.Key,
                            Name = kvpsubslots.Value["name"].Str(),
                            LocName = kvpsubslots.Value["locName"].Str(),
                            Rebuilds = kvpsubslots.Value["rebuilds"].Int(),
                            Loadout = kvpsubslots.Value["loadout"].Str(),
                            LoadoutName = kvpsubslots.Value["loadoutName"].Str(),
                        };
                        list.Add(m);
                    }
                }

                return list;
            }
            return null;
        }


        // Ships

        public class ShipInfo
        {
            public long ID{get;set;}
            public string Name{get;set;}
            public long ValueHull{get;set;}
            public long ValueModules{get;set;}
            public int Cargo{get;set;}
            public long ValueTotal{get;set;}
            public long Insurance{get;set;}
            public bool Free{get;set;}
            public long StationID{get;set;}
            public string Station{get;set;}
            public string System{get;set;}
            public long SystemAddress{get;set;}
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

        // suit

        public string SuitName { get { return json.I("suit").I("name").StrNull(); } }
        public string SuitLocName { get { return json.I("suit").I("locName").StrNull(); } }
        public long SuitId { get { return json.I("suit").I("id").Long(); } }
        public long SuitSuitId { get { return json.I("suit").I("suitId").Long(); } }
        public double SuitHealth { get { return json.I("suit").I("state").I("health").I("hull").Double()/10000.0; } }
        // tbd slot unknown

        // Loadouts

        public class SuitSlot
        {
            public string SlotName{get;set;}
            public string Name{get;set;}
            public string LocName{get;set;}
            public string LocDescription{get;set;}
            public long ID{get;set;}
            public long WeaponRackID{get;set;}

            // additional info only in Current Loadout

            public double Health{get;set;}       // %
            public long Value{get;set;}
            public bool Free{get;set;}
            public int AmmoClip{get;set;}
            public int HopperSize{get;set;}
        }

        public class SuitLoadout
        {
            public long LoadoutID{get;set;}
            public long SuitID{get;set;}
            public string SuitName{get;set;}
            public string SuitLocName{get;set;}
            public string UserLoadoutName{get;set;}
            public List<SuitSlot> slots{get;set;}
        }

        public List<SuitLoadout> GetSuitLoadouts()
        {
            JObject loadouts = json.I("loadouts").Object();
            if (loadouts != null)
            {
                var list = new List<SuitLoadout>();
                foreach (var kvploadouts in loadouts)
                {
                    JObject data = kvploadouts.Value.Object();
                    if (data != null)
                    {
                        SuitLoadout m = new SuitLoadout()
                        {
                            SuitID = data["suit"].I("suitId").Long(),
                            SuitName = data["suit"].I("name").Str(),
                            SuitLocName = data["suit"].I("locName").Str(),
                            LoadoutID = data["id"].Long(),
                            UserLoadoutName = data["name"].Str()
                        };

                        JObject slots = data["slots"].Object();

                        if ( slots != null )
                        {
                            m.slots = new List<SuitSlot>();
                            foreach( var kvp in slots)
                            {
                                var sl = new SuitSlot()
                                {
                                    SlotName = kvp.Key,
                                    Name = kvp.Value["name"].Str(),
                                    LocName = kvp.Value["locName"].Str(),
                                    LocDescription = kvp.Value["locDescription"].Str(),
                                    ID = kvp.Value["id"].Long(),
                                    WeaponRackID = kvp.Value["weaponrackId"].Long()
                                };

                                m.slots.Add(sl);
                            }
                        }

                        list.Add(m);
                    }
                }

                return list;
            }
            return null;
        }

        // loadout 
        // (loadout:suit replicates suit)

        public int LoadoutIndex { get { return json["loadout"].I("loadoutSlotId").Int(-1); } }     // current entry in Loadouts in use (2 = third entry)
        public string LoadoutUserName { get { return json["loadout"].I("name").StrNull(); } }
        public double LoadoutOxygenRemaining { get { return json["loadout"].I("state").I("oxygenRemaining").Double() / 1000; } }
        public double LoadoutEnergy { get { return json["loadout"].I("state").I("energy").Double(); } }

        public List<SuitSlot> GetSuitCurrentLoadout()
        {
            JObject slots = json.I("loadout").I("slots").Object();
            if (slots != null)
            {
                var list = new List<SuitSlot>();
                foreach (var kvp in slots)
                {
                    var sl = new SuitSlot()
                    {
                        SlotName = kvp.Key,
                        Name = kvp.Value["name"].Str(),
                        LocName = kvp.Value["locName"].Str(),
                        LocDescription = kvp.Value["locDescription"].Str(),
                        ID = kvp.Value["id"].Long(),
                        WeaponRackID = kvp.Value["weaponrackId"].Long(),

                        Health = kvp.Value["health"].Double() / 10000.0,
                        Value = kvp.Value["value"].Long(),
                        Free = kvp.Value["free"].Bool(),
                        AmmoClip = kvp.Value["ammo"].I("clip").Int(),
                        HopperSize = kvp.Value["ammo"].I("hopper").Int(),

                        // TBD slots, modifications, PaintJob, modifications not understood
                    };

                    list.Add(sl);
                }

                return list;
            }
            return null;
        }

        // suits - replicating what you get in Loadouts

        private QuickJSON.JToken json;
    }
}
