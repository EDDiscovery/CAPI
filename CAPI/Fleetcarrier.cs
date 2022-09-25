﻿/*
 * Copyright © 2022 Robby & EDDiscovery development team
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

using BaseUtils;
using QuickJSON;
using System;
using System.Collections.Generic;

namespace CAPI
{
    // this decodes the Fleet carrier endpoint

    public class FleetCarrier
    {
        public FleetCarrier(string profile)
        {
            json = JToken.Parse(profile, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL);
        }

        public bool IsValid { get { return json != null && Name != null && CallSign != null; } }

        // Carrier

        public string Name { get { return json["name"].I("vanityName").StrNull(); } }
        public string FilteredName { get { return json["name"].I("filteredVanityName").StrNull(); } }
        public string CallSign { get { return json["name"].I("callsign").StrNull(); } }
        public string StarSystem { get { return json["currentStarSystem"].StrNull(); } }
        public long Balance { get { return json["balance"].Str("0").InvariantParseLong(0); } }  // strange frontier uses strings
        public int Fuel { get { return json["fuel"].Str("0").InvariantParseInt(0); } } // strange frontier uses strings
        public string State { get { return json["state"].StrNull(); } }
        public string Theme { get { return json["theme"].StrNull(); } }
        public string DockingAccess { get { return json["dockingAccess"].StrNull(); } }
        public bool NotoriusAccess { get { return json["notoriousAccess"].Bool(false); } }
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
        public long CargoTotalValue { get { return json["marketFinances"].I("cargoTotalValue").Long(0); } }
        public long AllTimeProfit { get { return json["marketFinances"].I("allTimeProfit").Long(0); } }
        public int CommoditiesForSale { get { return json["marketFinances"].I("numCommodsForSale").Int(0); } }
        public int CommoditiesPurchaseOrders { get { return json["marketFinances"].I("numCommodsPurchaseOrders").Int(0); } }
        public long BalanceAllocatedForPurchaseOrders { get { return json["marketFinances"].I("balanceAllocForPurchaseOrders").Long(0); } }
        public long BlackMarketCargoValue { get { return json["blackmarketFinances"].I("cargoTotalValue").Long(0); } }
        public long BlackMarketAllTimeProfit { get { return json["blackmarketFinances"].I("allTimeProfit").Long(0); } }
        public int BlackMarketCommoditiesForSale { get { return json["blackmarketFinances"].I("numCommodsForSale").Int(0); } }
        public int BlackMarketCommoditiesPurchaseOrders { get { return json["blackmarketFinances"].I("numCommodsPurchaseOrders").Int(0); } }
        public long BlackMarketBalanceAllocatedForPurchaseOrders { get { return json["blackmarketFinances"].I("balanceAllocForPurchaseOrders").Long(0); } }

        public long BankBalance { get { return json["finance"].I("bankBalance").Long(0); } }
        public long BankReservedBalance { get { return json["finance"].I("bankReservedBalance").Long(0); } }
        public double Taxation { get { return json["finance"].I("taxation").Double(0); } }
        public double ServiceTaxationBartender { get { return json["finance"].I("service_taxation").I("bartender").Double(0); } }
        public double ServiceTaxationPioneer { get { return json["finance"].I("service_taxation").I("pioneersupplies").Double(0); } }
        public double ServiceTaxationRearm { get { return json["finance"].I("service_taxation").I("rearm").Double(0); } }
        public double ServiceTaxationRefuel { get { return json["finance"].I("service_taxation").I("refuel").Double(0); } }
        public double ServiceTaxationRepair { get { return json["finance"].I("service_taxation").I("repair").Double(0); } }
        public double ServiceTaxationShipYard { get { return json["finance"].I("service_taxation").I("shipyard").Double(0); } }
        public double ServiceTaxationOutFitting { get { return json["finance"].I("service_taxation").I("outfitting").Double(0); } }
        public int ServicesCount { get { return json["finance"].I("numServices").Int(0); } }
        public int ServicesOptionalCount { get { return json["finance"].I("numOptionalServices").Int(0); } }
        public long DebtThreshold { get { return json["finance"].I("debtThreshold").Long(0); } }
        public long MaintenanceCost { get { return json["finance"].I("maintenance").Long(0); } }
        public long MaintenanceCostToToDate { get { return json["finance"].I("maintenanceToDate").Long(0); } }
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

        public class Invoice
        {
            public long Wages { get; set; }
            public DateTime From { get; set; }
            public DateTime Until { get; set; }
            public string Type { get; set; }
        }

        public class Services
        {
            public string CrewMemberName { get; set; }
            public bool CrewMemberMale { get; set; }
            public bool CrewMemberEnabled { get; set; }
            public string CrewMemberFaction { get; set; }
            public long CrewMemberSalary { get; set; }
            public long CrewMemberHiringPrice { get; set; } // -1 for 'null'
            public DateTime CrewMemberLastEdit { get; set; }
            public List <Invoice> Invoices { get; set; }
        };

        public Dictionary<string, Services> GetServices()
        {
            JObject services = json["servicesCrew"].Object();
            if (services != null)
            {
                Dictionary<string, Services> ret = new Dictionary<string, Services>();
                foreach (var kvp in services)
                {
                    JObject crew = kvp.Value["crewMember"].Object();

                    if (crew != null)
                    {
                        Services s = new Services();
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

                            foreach( var entry in inv)
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

        //cargo..

        //   public long Bartender { get { return json["finance"].I("bartender").I("microresources").Long(0); } }
        //  public long Bartender { get { return json["finance"].I("bartender").I("microresources").Long(0); } }
        //public long Cargo { get { return json["marketFinances"].I("").Long(0); } }
        //public long Cargo { get { return json["marketFinances"].I("").Long(0); } }
        //public long Cargo { get { return json["marketFinances"].I("").Long(0); } }
        //public long Cargo { get { return json["marketFinances"].I("").Long(0); } }

        private QuickJSON.JToken json;
    }
}