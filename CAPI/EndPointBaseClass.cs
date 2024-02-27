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

using BaseUtils;
using QuickJSON;
using System;
using System.Collections.Generic;

namespace CAPI
{
    // Orders is common to Market and Fleetcarrier

    public class CAPIEndPointBaseClass
    {
        public CAPIEndPointBaseClass(string profile)
        {
            json = JToken.Parse(profile, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL);
        }

        public JToken Json { get { return json; } }

        public class Commodity
        {
            public string Name{get;set;}
            public string LocName{get;set;}
            public long ID{get;set;}
            public string Legality{get;set;}
            public long Buy{get;set;}
            public long Sell{get;set;}
            public long Mean{get;set;}
            public long DemandBracket{get;set;}
            public long StockBracket{get;set;}
            public long Stock{get;set;}
            public long Demand{get;set;}
            public string Category;
        };

        public class OrdersCommoditySales
        {
            public string Name {get;set;}
            public long Stock {get;set;}
            public long Price {get;set;}
            public bool Blackmarket {get;set;}
        };

        public class OrdersCommodityPurchases
        {
            public string Name {get;set;}
            public long Total {get;set;}
            public long Outstanding {get;set;}
            public long Price {get;set;}
            public bool Blackmarket {get;set;}
        };
        public class OrdersMRPurchases
        {
            public string Name {get;set;}
            public string LocName {get;set;}
            public long Total {get;set;}
            public long Outstanding {get;set;}
            public long Price {get;set;}
        };

        public class OrdersMRSales
        {
            public long ID {get;set;}
            public string Name {get;set;}
            public string LocName {get;set;}
            public long Price {get;set;}
            public long Stock {get;set;}
        };

        public class Module
        {
            public long ID { get; set; }
            public string Category { get; set; }
            public string Name { get; set; }
            public long Cost { get; set; }
            public long Stock { get; set; }
        }

        public class Ship
        {
            public long ID { get; set; }
            public string Name { get; set; }
            public long BaseValue { get; set; }
            public string SKU { get; set; }
        }



        protected List<OrdersCommoditySales> GetOrdersCommoditiesSales(JArray clist)     // may return null. Returns name, locName, price, stock
        {
            if (clist != null && clist.Count > 0)
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

        protected List<OrdersCommodityPurchases> GetOrdersCommoditiesPurchases(JArray clist)     // may return null. Returns name, locName, price, stock
        {
            if (clist != null && clist.Count > 0)
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
        protected List<OrdersMRSales> GetOrdersMicroresourcesSales(JObject clist)     // may return null. Returns name, locName, price, stock
        {
            if (clist != null && clist.Count > 0)
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
        protected List<OrdersMRPurchases> GetOrdersMicroresourcesPurchases(JArray clist)     // may return null. Returns name, locName, price, stock
        {
            if (clist != null && clist.Count > 0)
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

        private OrdersCommoditySales OrdersCommoditySalesFromJSON(JObject data)
        {
            if (data != null)
            {
                var m = new OrdersCommoditySales()
                {
                    Name = data["name"].Str(),
                    Stock = data["stock"].Str("0").InvariantParseLong(0),
                    Price = data["price"].Str("0").InvariantParseLong(0),
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
                    ID = data["id"].Long(),
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

        protected List<Commodity> GetCommodityList(JArray clist)
        {
            if (clist != null && clist.Count>0)
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

        protected Dictionary<string, double> GetEconomies(JObject data)
        {
            var list = new Dictionary<string, double>();
            if (data != null && data.Count>0)
            {
                foreach (var e in data)
                    list.Add(e.Value["name"].Str("Unknown"), e.Value["proportion"].Double() * 100.0);
            }

            return list;
        }
        protected List<Module> GetModules(JObject moduleslist)        // may be null if no shipyard
        {
            if (moduleslist != null && moduleslist.Count>0)
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

        protected List<Ship> GetShips(JObject shiplist)        // may be null if no shipyard
        {
            if (shiplist != null && shiplist.Count > 0)
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

        protected QuickJSON.JToken json;
    }
}
