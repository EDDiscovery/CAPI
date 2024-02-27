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

using QuickJSON;
using System;
using System.Collections.Generic;

namespace CAPI
{
    // this decodes the Market endpoint
    // Note, if not docked, you may get an empty {} json back
    // then all entries will return null. Use IsValid to know you got data, but you still need to check each for null
    // as its frontiers data and they may have left the node out


    public class Market : CAPIEndPointBaseClass
    {
        public Market(string profile) : base(profile)
        {
        }


        public bool IsValid { get { return json != null && ID != long.MinValue && Name != null; } }

        // Commander

        public long ID { get { return json["id"].Long(long.MinValue); } }
        public string Name { get { return json["name"].StrNull()?.TrimReplaceEnd('+'); } }
        public string Type { get { return json["outpostType"].StrNull(); } }

        // id name pairs
        public Dictionary<string, string> Imports { get { return json["imported"].Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, string> Exports { get { return json["exported"].Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, string> Services { get { return json["services"].Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, string> Prohibited { get { return json["prohibited"].Object()?.ToObject<Dictionary<string, string>>(); } }
        public Dictionary<string, double> Economies { get { return GetEconomies(json["economies"].Object()); } }

        public JArray Commodities { get { return json["commodities"].Array(); } }       // return the JSON array, or Null
            
        public List<Commodity> GetCommodities()     // may return null, returns commodities info
        {
            return GetCommodityList(Commodities);
        }
        public List<OrdersCommoditySales> GetOrdersCommoditiesSales()     // may return null. Returns name, locName, price, stock
        {
            return GetOrdersCommoditiesSales(OrdersCommoditiesSales);
        }
        public List<OrdersCommodityPurchases> GetOrdersCommoditiesPurchaces()     // may return null. Returns name, locName, price, stock
        {
            return GetOrdersCommoditiesPurchases(OrdersCommoditiesPurchases);
        }

        public List<OrdersMRSales> GetOrdersMicroresourcesSales()     // may return null. Returns name, locName, price, stock
        {
            return GetOrdersMicroresourcesSales(OrdersMicroResourcesSales);
        }

        public List<OrdersMRPurchases> GetOrdersMicroresourcesPurchases()     // may return null. Returns name, locName, price, stock
        {
            return GetOrdersMicroresourcesPurchases(OrdersMicroResourcesPurchases);
        }

        private JArray OrdersCommoditiesSales { get { return json["orders"].I("commodities").I("sales").Array(); } }
        private JArray OrdersCommoditiesPurchases { get { return json["orders"].I("commodities").I("purchases").Array(); } }
        private JObject OrdersMicroResourcesSales { get { return json["orders"].I("onfootmicroresources").I("sales").Object(); } }
        private JArray OrdersMicroResourcesPurchases { get { return json["orders"].I("onfootmicroresources").I("purchases").Array(); } }

    }

}
