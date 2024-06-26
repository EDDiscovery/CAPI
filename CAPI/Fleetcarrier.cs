﻿/*
 * Copyright © 2022-2024 Robby & EDDiscovery development team
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

using BaseUtils;
using QuickJSON;
using System;
using System.Collections.Generic;

namespace CAPI
{
    // this decodes the Fleet carrier endpoint

    public class FleetCarrier : CAPIEndPointBaseClass
    {
        public FleetCarrier(string profile) : base(profile)
        {
        }

        public bool IsValid { get { return json != null && Name != null && CallSign != null; } }


        // Name
        public string Name { get { return json["name"].I("vanityName").StrNull()?.FromHexString(); } }
        public string FilteredName { get { return json["name"].I("filteredVanityName").StrNull()?.FromHexString(); } }
        public string CallSign { get { return json["name"].I("callsign").StrNull(); } }

        public string StarSystem { get { return json["currentStarSystem"].StrNull(); } }
        public long Balance { get { return json["balance"].Str("0").InvariantParseLong(0); } }  // strange frontier uses strings
        public int Fuel { get { return json["fuel"].Str("0").InvariantParseInt(0); } } // strange frontier uses strings
        public string State { get { return json["state"].StrNull(); } }
        public string Theme { get { return json["theme"].StrNull(); } }
        public string DockingAccess { get { return json["dockingAccess"].StrNull(); } }
        public bool NotoriousAccess { get { return json["notoriousAccess"].Bool(false); } }

        // Capacity

        public int CapacityShipPacks { get { return json["capacity"].I("shipPacks").Int(0); } }
        public int CapacityModulePacks { get { return json["capacity"].I("modulePacks").Int(0); } }
        public int CapacityCargoForSale { get { return json["capacity"].I("cargoForSale").Int(0); } }
        public int CapacityCargoNotForSale { get { return json["capacity"].I("cargoNotForSale").Int(0); } }
        public int CapacityCargoSpaceReserved { get { return json["capacity"].I("cargoSpaceReserved").Int(0); } }
        public int CapacityCrew { get { return json["capacity"].I("crew").Int(0); } }
        public int CapacityFreeSpace { get { return json["capacity"].I("freeSpace").Int(0); } }
        public int CapacityMRTotal { get { return json["capacity"].I("microresourceCapacityTotal").Int(0); } }
        public int CapacityMRFree { get { return json["capacity"].I("microresourceCapacityFree").Int(0); } }
        public int CapacityMRUsed { get { return json["capacity"].I("microresourceCapacityUsed").Int(0); } }
        public int CapacityMRReserved { get { return json["capacity"].I("microresourceCapacityReserved").Int(0); } }

        // Itinerary
        public long TotalDistanceJumpedLY { get { return json["itinerary"].I("totalDistanceJumpedLY").Long(0); } }
        // TBD CurrentJump

        public class Itinerary
        {
            public DateTime DepartureTime { get; set; }     // MINValue if not there
            public DateTime ArrivalTime { get; set; }
            public string State { get; set; }
            public int VisitDurationSeconds { get; set; }
            public string StarSystem { get; set; }
        };

        public List<Itinerary> GetCompletedItinerary()
        {
            JArray list = json["itinerary"].I("completed").Array();

            if (list != null)
            {
                List<Itinerary> it = new List<Itinerary>();
                foreach (var e in list)
                {
                    Itinerary i = new Itinerary();
                    i.DepartureTime = e["departureTime"].DateTimeUTC();
                    i.ArrivalTime = e["arrivalTime"].DateTimeUTC();
                    i.State = e["state"].Str("Unknown");
                    i.VisitDurationSeconds = e["visitDurationSeconds"].Int(0);
                    i.StarSystem = e["starsystem"].Str("Unknown");
                    it.Add(i);
                }
                return it;
            }
            else
                return null;
        }

        // marketFinance

        public long CargoTotalValue { get { return json["marketFinances"].I("cargoTotalValue").Long(0); } }
        public long AllTimeProfit { get { return json["marketFinances"].I("allTimeProfit").Long(0); } }
        public int CommoditiesForSale { get { return json["marketFinances"].I("numCommodsForSale").Int(0); } }
        public int CommoditiesPurchaseOrders { get { return json["marketFinances"].I("numCommodsPurchaseOrders").Int(0); } }
        public long BalanceAllocatedForPurchaseOrders { get { return json["marketFinances"].I("balanceAllocForPurchaseOrders").Long(0); } }

        // blackmarketFinances

        public long BlackMarketCargoValue { get { return json["blackmarketFinances"].I("cargoTotalValue").Long(0); } }
        public long BlackMarketAllTimeProfit { get { return json["blackmarketFinances"].I("allTimeProfit").Long(0); } }
        public int BlackMarketCommoditiesForSale { get { return json["blackmarketFinances"].I("numCommodsForSale").Int(0); } }
        public int BlackMarketCommoditiesPurchaseOrders { get { return json["blackmarketFinances"].I("numCommodsPurchaseOrders").Int(0); } }
        public long BlackMarketBalanceAllocatedForPurchaseOrders { get { return json["blackmarketFinances"].I("balanceAllocForPurchaseOrders").Long(0); } }

        // Finance

        public long BankBalance { get { return json["finance"].I("bankBalance").Long(0); } }
        public long BankReservedBalance { get { return json["finance"].I("bankReservedBalance").Long(0); } }

        // not in use public double Taxation { get { return json["finance"].I("taxation").Double(0); } }
        // not in use. public double ServiceTaxationBartender { get { return json["finance"].I("service_taxation").I("bartender").Double(0); } }
        public double ServiceTaxationPioneer { get { return json["finance"].I("service_taxation").I("pioneersupplies").Double(0); } }
        public double ServiceTaxationRearm { get { return json["finance"].I("service_taxation").I("rearm").Double(0); } }
        public double ServiceTaxationRefuel { get { return json["finance"].I("service_taxation").I("refuel").Double(0); } }
        public double ServiceTaxationRepair { get { return json["finance"].I("service_taxation").I("repair").Double(0); } }
        public double ServiceTaxationShipYard { get { return json["finance"].I("service_taxation").I("shipyard").Double(0); } }
        public double ServiceTaxationutFitting { get { return json["finance"].I("service_taxation").I("outfitting").Double(0); } }
        public int ServicesCount { get { return json["finance"].I("numServices").Int(0); } }
        public int ServicesOptionalCount { get { return json["finance"].I("numOptionalServices").Int(0); } }
        public long DebtThreshold { get { return json["finance"].I("debtThreshold").Long(0); } }
        public long MaintenanceCost { get { return json["finance"].I("maintenance").Long(0); } }
        public long MaintenanceCostToDate { get { return json["finance"].I("maintenanceToDate").Long(0); } }
        public long CoreCost { get { return json["finance"].I("coreCost").Long(0); } }
        public long ServicesCost { get { return json["finance"].I("servicesCost").Long(0); } }
        public long ServicesCostToDate { get { return json["finance"].I("servicesCostToDate").Long(0); } }
        public long FSDJumpsCost { get { return json["finance"].I("jumpsCost").Long(0); } }
        public long FSDJumpsMade { get { return json["finance"].I("numJumps").Long(0); } }
        public long BartenderMRTotalValue { get { return json["finance"].I("bartender").I("microresourcesTotalValue").Long(0); } }
        public long BartenderMRAllTimeProfit { get { return json["finance"].I("bartender").I("allTimeProfit").Long(0); } }
        public int BartenderMRForSale { get { return json["finance"].I("bartender").I("microresourcesForSale").Int(0); } }
        public int BartenderMRPurchaseOrders { get { return json["finance"].I("bartender").I("microresourcesPurchaseOrders").Int(0); } }
        public long BartenderMRBalanceAllocated { get { return json["finance"].I("bartender").I("balanceAllocForPurchaseOrders").Long(0); } }

        // TBD bartender Profit History - what does it mean

        // Services Crew

        public class Invoice
        {
            public long Wages { get; set; }
            public DateTime From { get; set; }
            public DateTime Until { get; set; }
            public string Type { get; set; }
        }

        public class CrewService
        {
            public string CrewMemberName { get; set; }
            public bool CrewMemberMale { get; set; }
            public bool CrewMemberEnabled { get; set; }
            public string CrewMemberFaction { get; set; }
            public long CrewMemberSalary { get; set; }
            public long CrewMemberHiringPrice { get; set; } // -1 for 'null'
            public DateTime CrewMemberLastEdit { get; set; }
            public List<Invoice> Invoices { get; set; }
        };

        // Crew service state. Will be null when no services are activated (default state)
        public Dictionary<string, CrewService> GetCrewServices()
        {
            JObject services = json["servicesCrew"].Object();
            if (services != null && services.Count > 0)
            {
                Dictionary<string, CrewService> ret = new Dictionary<string, CrewService>();
                foreach (var kvp in services)
                {
                    JObject crew = kvp.Value["crewMember"].Object();

                    if (crew != null)
                    {
                        CrewService s = new CrewService();
                        s.CrewMemberName = crew["name"].Str();
                        s.CrewMemberMale = crew["gender"].Str("M") == "M";
                        s.CrewMemberEnabled = crew["enabled"].Str("No") == "YES";
                        s.CrewMemberFaction = crew["faction"].Str("Unknown");
                        s.CrewMemberSalary = crew["salary"].Long(0);
                        s.CrewMemberHiringPrice = crew["hiringPrice"].Long(-1);
                        s.CrewMemberLastEdit = crew["lastEdit"].DateTimeUTC();

                        JArray inv = kvp.Value["invoicesWeekToDate"].Array();
                        if (inv != null)
                        {
                            s.Invoices = new List<Invoice>();

                            foreach (var entry in inv)
                            {
                                Invoice i = new Invoice();
                                i.Wages = entry["wages"].Long(0);
                                i.From = entry["from"].DateTimeUTC();
                                i.Until = entry["until"].DateTimeUTC();
                                i.Type = entry["type"].Str();
                                s.Invoices.Add(i);
                            }
                        }

                        ret[kvp.Key] = s;
                    }
                }

                return ret;
            }
            else
                return null;
        }

        // Cargo

        [System.Diagnostics.DebuggerDisplay("{Commodity} v{Value} q{Quantity} m{Mission} s{Stolen}")]
        public class Cargo : IComparable
        {
            public string Commodity { get; set; }
            public bool Mission { get; set; }
            public int Quantity { get; set; }
            public long Value { get; set; }
            public bool Stolen { get; set; }
            public string LocName { get; set; }

            public int CompareTo(object obj)
            {
                Cargo o = obj as Cargo;
                int v = Commodity.CompareTo(o.Commodity);
                if (v != 0)
                    return v;
                else if (o.Value != Value)
                    return o.Value.CompareTo(Value);
                else if (o.Mission != Mission)
                    return Mission.CompareTo(o.Mission);
                else
                    return o.Stolen.CompareTo(Stolen);
            }
        }

        // This is what is on board the carrier, including items for sale.  Items may be repeated
        public List<Cargo> GetCargo()
        {
            JArray clist = json["cargo"].Array();
            if (clist != null && clist.Count > 0)
            {
                List<Cargo> cargo = new List<Cargo>();
                foreach (var entry in clist)
                {
                    Cargo c = new Cargo();
                    c.Commodity = entry["commodity"].Str("Unknown");
                    c.Mission = entry["mission"].Bool();
                    c.Stolen = entry["stolen"].Bool();
                    c.Quantity = entry["qty"].Int();
                    c.Value = entry["value"].Long();
                    c.LocName = entry["locName"].Str("Unknown");
                    cargo.Add(c);
                }
                return cargo;
            }
            return null;
        }

        // merge entries with same name/value/flags together
        static public List<Cargo> MergeCargo(List<Cargo> list)
        {
            var todel = new List<Cargo>();
            foreach( Cargo c in list)
            {
                while(!todel.Contains(c))   // don't process deleted ones
                {
                    // find the next in the list the same as c, but not the same item, and not on the del list
                    var other = list.Find(x => x.CompareTo(c) == 0 && !Object.ReferenceEquals(x, c) && !todel.Contains(x));
                    if (other != null)
                    {
                        c.Quantity += other.Quantity;
                        todel.Add(other);
                    }
                    else
                        break;
                }
            }

            foreach (var x in todel)
                list.Remove(x);

            return list;
        }


        // orders - what is on sale or to purchase.
        // For commodities : Includes items from both the Carrier Commodity Trading (with blackmarket = false) and Secure Trading (blackmarket=true)
        // For MR: Items from Carrier Bartender

        public List<OrdersCommoditySales> GetOrdersCommoditiesSales()     // may return null. 
        {
            return GetOrdersCommoditiesSales(OrdersCommoditiesSales);
        }
        public List<OrdersCommodityPurchases> GetOrdersCommoditiesPurchaces()     // may return null. 
        {
            return GetOrdersCommoditiesPurchases(OrdersCommoditiesPurchases);
        }
        public List<OrdersMRSales> GetOrdersMicroresourcesSales()     // may return null. 
        {
            return GetOrdersMicroresourcesSales(OrdersMicroResourcesSales);
        }
        public List<OrdersMRPurchases> GetOrdersMicroresourcesPurchases()     // may return null. 
        {
            return GetOrdersMicroresourcesPurchases(OrdersMicroResourcesPurchases);
        }


        // Carrier locker

        public class LockerItem
        {
            public long ID { get; set; }
            public string Name { get; set; }
            public int Quantity { get; set; }
            public string LocName { get; set; }
            public LockerType Category { get; set; } 
        }

        public enum LockerType { Assets, Goods, Data}
        public List<LockerItem> GetCarrierLocker(LockerType t)
        {
            JArray clist = json["carrierLocker"].I(t.ToString().ToLowerInvariant()).Array();
            if (clist != null && clist.Count > 0)
            {
                List<LockerItem> cargo = new List<LockerItem>();
                foreach (var entry in clist)
                {
                    LockerItem c = new LockerItem();
                    c.ID = entry["id"].Long();
                    c.Quantity = entry["quantity"].Int();
                    c.Name = entry["name"].Str();
                    c.LocName = entry["locName"].Str("Unknown");
                    c.Category = t;
                    cargo.Add(c);
                }
                return cargo;
            }
            return null;
        }

        public List<LockerItem> GetCarrierLockerAll()
        {
            var a = GetCarrierLocker(LockerType.Assets);
            var g = GetCarrierLocker(LockerType.Goods);
            var d = GetCarrierLocker(LockerType.Data);

            List<LockerItem> merged = new List<LockerItem>();
            if (a != null)
                merged.AddRange(a);
            if (g != null)
                merged.AddRange(g);
            if (d != null)
                merged.AddRange(d);

            return merged;
        }

        // reputation

        public Dictionary<string, int> GetReputation()
        {
            JArray rlist = json["reputation"].Array();
            if (rlist != null)
            {
                Dictionary<string, int> rep = new Dictionary<string, int>();
                foreach (var entry in rlist)
                {
                    JObject o = entry.Object();
                    rep.Add(o["majorFaction"].Str(), o["score"].Int());
                }

                return rep;
            }
            else
                return null;
        }

        // Market

        public long ID { get { return json["market"].I("id").Long(0); } }

        // id name pairs
        public Dictionary<string, string> Imports { get { return json["market"].I("imported").Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, string> Exports { get { return json["market"].I("exported").Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, string> Services { get { return json["market"].I("services").Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, string> Prohibited { get { return json["market"].I("prohibited").Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, double> Economies { get { return GetEconomies(json["market"].I("economies").Object()); } }

        // Not sure what these are - they are not syncing up with the orders
        public List<Commodity> GetMarketCommodities()     // may return null, returns commodities info
        {
            return GetCommodityList(json["market"].I("commodities").Array());
        }

        // ships - what ships we are selling

        public List<Ship> GetShips()        // may be null if no shipyard
        {
            return GetShips(json.I("ships").I("shipyard_list").Object());
        }

        // modules - what modules we are selling

        public List<Module> GetModules()        // may be null if no shipyard
        {
            return GetModules(json.I("modules").Object());
        }

        private JArray OrdersCommoditiesSales { get { return json["orders"].I("commodities").I("sales").Array(); } }
        private JArray OrdersCommoditiesPurchases { get { return json["orders"].I("commodities").I("purchases").Array(); } }
        private JObject OrdersMicroResourcesSales { get { return json["orders"].I("onfootmicroresources").I("sales").Object(); } }
        private JArray OrdersMicroResourcesPurchases { get { return json["orders"].I("onfootmicroresources").I("purchases").Array(); } }




    }
}
