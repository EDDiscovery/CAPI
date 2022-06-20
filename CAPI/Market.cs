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

using QuickJSON;
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
        public string Name { get { return json["name"].StrNull()?.TrimReplaceEnd('+'); } }
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
        public JArray OrdersCommoditiesSales { get { return json["orders"].I("commodities").I("sales").Array(); } }
        public JArray OrdersCommoditiesPurchases { get { return json["orders"].I("commodities").I("purchases").Array(); } }
        public JObject OrdersMicroResourcesSales { get { return json["orders"].I("onfootmicroresources").I("sales").Object(); } }
        public JArray OrdersMicroResourcesPurchases { get { return json["orders"].I("onfootmicroresources").I("purchases").Array(); } }

        public List<Commodity> GetCommodities()     // may return null, returns commodities info
        {
            return GetCommodityList(Commodities);
        }
        public List<OrdersCommoditySales> GetOrdersCommoditiesSales()     // may return null. Returns name, locName, price, stock
        {
            var clist = OrdersCommoditiesSales;

            if (clist != null)
            {
                var list = new List<OrdersCommoditySales>();
                foreach (var kvp in clist)
                {
                    var m = OrdersCommoditySalesFromJSON(kvp.Object());
                    if (m != null)
                        list.Add(m);
                }

                return list;
            }
            else
                return null;
        }

        public List<OrdersCommodityPurchases> GetOrdersCommoditiesPurchaces()     // may return null. Returns name, locName, price, stock
        {
            var clist = OrdersCommoditiesPurchases;

            if (clist != null)
            {
                var list = new List<OrdersCommodityPurchases>();
                foreach (var kvp in clist)
                {
                    var m = OrdersCommodityPurchasesFromJSON(kvp.Object());
                    if (m != null)
                        list.Add(m);
                }

                return list;
            }
            else
                return null;
        }
        public List<OrdersMRSales> GetOrdersMicroresourcesSales()     // may return null. Returns name, locName, price, stock
        {
            var clist = OrdersMicroResourcesSales;

            if (clist != null)
            {
                var list = new List<OrdersMRSales>();
                foreach (var kvp in clist)
                {
                    var m = OrdersMRSalesFromJSON(kvp.Value.Object());
                    if (m != null)
                        list.Add(m);
                }

                return list;
            }
            else
                return null;
        
        
       }
        public List<OrdersMRPurchases> GetOrdersMicroresourcesPurchases()     // may return null. Returns name, locName, price, stock
        {
            var clist = OrdersMicroResourcesPurchases;

            if (clist != null)
            {
                var list = new List<OrdersMRPurchases>();
                foreach (var kvp in clist)
                {
                    var m = OrdersMRPurchasesFromJSON(kvp.Object());
                    if (m != null)
                        list.Add(m);
                }

                return list;
            }
            else
                return null;
        }
        public class Commodity
        {
            public string Name;         
            public string LocName;      
            public long ID;             
            public string Legality;
            public long Buy;
            public long Sell;
            public long Mean;
            public long DemandBracket;
            public long StockBracket;
            public long Stock;          
            public long Demand;
            public string Category;
        };

        public class OrdersCommoditySales
        {
            public string Name;
            public long Stock;
            public long Price;
            public bool Blackmarket;
        };

        public class OrdersCommodityPurchases
        {
            public string Name;
            public long Total;
            public long Outstanding;
            public long Price;
            public bool Blackmarket;
        };
        public class OrdersMRPurchases
        {
            public string Name;
            public string LocName;
            public long Total;
            public long Outstanding;
            public long Price;
        };

        public class OrdersMRSales
        {
            public long ID;
            public string Name;
            public string LocName;
            public long Price;
            public long Stock;
        };



        private List<Commodity> GetCommodityList(JArray clist)
        {
            if (clist != null)
            {
                List<Commodity> list = new List<Commodity>();
                foreach (var entry in clist)
                {
                    Commodity m = CommodityFromJSON(entry.Object());
                    if (m != null)
                        list.Add(m);
                }

                return list;
            }
            else
                return null;
        }

        private Commodity CommodityFromJSON(JObject data)
        {
            if (data != null)
            {
                var m = new Commodity()
                {
                    ID = data["id"].Long(),
                    Name = data["name"].Str(),
                    Legality = data["legality"].Str(),
                    Buy = data["buyPrice"].Long(),
                    Sell = data["sellPrice"].Long(),
                    Mean = data["meanPrice"].Long(),
                    DemandBracket = data["demandBracket"].Long(),
                    StockBracket = data["stockBracket"].Long(),
                    Stock = data["stock"].Long(),
                    Demand = data["demand"].Long(),
                    Category = data["categoryname"].Str(),
                    LocName = data["locName"].Str(),
                };

                return m;
            }
            else
                return null;
        }
        private OrdersCommoditySales OrdersCommoditySalesFromJSON(JObject data)
        {
            if (data != null)
            {
                var m = new OrdersCommoditySales()
                {
                    Name = data["name"].Str(),
                    Stock = data["stock"].Long(),
                    Price = data["price"].Long(),
                    Blackmarket = data["blackmarket"].Bool()
                };

                return m;
            }
            else
                return null;
        }
        private OrdersCommodityPurchases OrdersCommodityPurchasesFromJSON(JObject data)
        {
            if (data != null)
            {
                var m = new OrdersCommodityPurchases()
                {
                    Name = data["name"].Str(),
                    Total = data["total"].Long(),
                    Outstanding = data["outstanding"].Long(),
                    Price = data["price"].Long(),
                    Blackmarket = data["blackmarket"].Bool()
                };

                return m;
            }
            else
                return null;
        }
        private OrdersMRPurchases OrdersMRPurchasesFromJSON(JObject data)
        {
            if (data != null)
            {
                var m = new OrdersMRPurchases()
                {
                    Name = data["name"].Str(),
                    LocName = data["locName"].Str(),
                    Total = data["total"].Long(),
                    Outstanding = data["outstanding"].Long(),
                    Price = data["price"].Long(),
                };

                return m;
            }
            else
                return null;
        }

        private OrdersMRSales OrdersMRSalesFromJSON(JObject data)
        {
            if (data != null)
            {
                var m = new OrdersMRSales()
                {
                    ID = data["ID"].Long(),
                    Name = data["name"].Str(),
                    LocName = data["locName"].Str(),
                    Price = data["price"].Long(),
                    Stock = data["stock"].Long(),
                };

                return m;
            }
            else
                return null;
        }

        private QuickJSON.JToken json;
    }
}
