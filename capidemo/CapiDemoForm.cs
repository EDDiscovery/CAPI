using BaseUtils.DDE;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CAPI;
using QuickJSON;

// NOTE you need an enviromental variable called CAPIID set up with your Frontier CAPI ID BEFORE RUNNING visual studio.
// use the control panel system | Enviromental variables to initialise this
// also expects a c:\code to store the ceritificates.

namespace CAPIDemo
{
    public partial class CapiDemoForm : Form
    {
        CompanionAPI capi;
        DDEServer ddeserver;
        const string rootpath = @"c:\code";            // where to dump files

        public CapiDemoForm()
        {
            InitializeComponent();
            for( int i = 0; i < Environment.GetCommandLineArgs().Length; i++)
                richTextBox.AppendText( Environment.GetCommandLineArgs()[i] + Environment.NewLine);

            string appPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;

            string uri = "eddiscovery";
            string ddeservername = "edd-dde-server";
            DDEServer.RegisterURICallback(uri, appPath, ddeservername);

            ddeserver = new DDEServer();
            if ( ddeserver.Start(ddeservername))
            {
                if ( ddeserver.AddTopic("System", handleCallbackUrl))
                {
                    if ( ddeserver.Register() )
                    {
                        richTextBox.AppendText( "DDE Server setup" + Environment.NewLine);
                    }
                }
            }

            Directory.CreateDirectory(rootpath);

            System.Diagnostics.Debug.Assert(CapiClientIdentity.id.Contains("-"));

            capi = new CompanionAPI(rootpath, CapiClientIdentity.id, $"EDCD-Program-1.2.3.4", uri);

            dateTimePicker.Value = DateTime.UtcNow;
        }

        private void handleCallbackUrl(IntPtr hurl)
        {
            string url = DDEServer.FromDdeStringHandle(hurl);
            richTextBox.AppendText( "URL Callback " + url + Environment.NewLine);
            if (capi != null)
                capi.URLCallBack(url);
            richTextBox.ScrollToCaret();
        }


        private void buttonLoginOne_Click(object sender, EventArgs e)
        {
            capi.LogIn("one");
            richTextBox.AppendText( "-------------------------" + Environment.NewLine);
            richTextBox.AppendText($"Login One: {capi.CAPIServer} {capi.CAPIURI}" + Environment.NewLine);
            richTextBox.ScrollToCaret();
        }

        private void buttonLoginTwo_Click(object sender, EventArgs e)
        {
            capi.LogIn("two:user");
            richTextBox.AppendText( "-------------------------" + Environment.NewLine);
            richTextBox.AppendText($"Login Two: {capi.CAPIServer} {capi.CAPIURI}" + Environment.NewLine);
            richTextBox.ScrollToCaret();
        }

        private void buttonLogout_Click(object sender, EventArgs e)
        {
            capi.LogOut();
            richTextBox.AppendText("Logout" + Environment.NewLine);
        }

        private void buttonProfile_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Profile(out DateTime servertime);

                if (p != null)
                {
                    string json = JToken.Parse(p, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL).ToString(true);
                    File.WriteAllText(rootpath + "\\profile.json", json);
                    System.Diagnostics.Debug.WriteLine("Profile JSON" + json);
                }

                richTextBox.AppendText("------------------------- PROFILE" + Environment.NewLine);

                Profile pf = p != null ? new Profile(p,servertime) : null;
                if (pf != null && pf.IsValid)
                {
                    ReflectProperties(pf, " ", eol:Environment.NewLine);
                    richTextBox.AppendText(Environment.NewLine);

                    var ml = pf.GetShipModules();
                    if (ml != null)
                    {
                        richTextBox.AppendText("Modules " + ml.Count + Environment.NewLine);
                        foreach (var m in ml)
                        {
                            richTextBox.AppendText(" " + m.Name + " " + m.Value + ", ");
                        }
                        richTextBox.AppendText(Environment.NewLine);
                    }
                    else
                        richTextBox.AppendText("No Modules" + Environment.NewLine);

                    var lb = pf.GetShipLaunchBays();
                    if (lb != null)
                    {
                        richTextBox.AppendText("Launch Bays " + lb.Count + Environment.NewLine);
                        foreach (var m in lb)
                        {
                            richTextBox.AppendText(" " + m.Location + ":" + m.SubSlot + " = " + m.LocName + Environment.NewLine);
                        }
                    }
                    else
                        richTextBox.AppendText("No launch bays" + Environment.NewLine);

                    var sh = pf.GetShips();
                    if (sh != null)
                    {
                        richTextBox.AppendText("Ships " + sh.Count + Environment.NewLine);
                        foreach (var s in sh)
                        {
                            richTextBox.AppendText(" " + s.Name + " C:" + s.Cargo + " S:" + s.Station + ", ");
                        }
                        richTextBox.AppendText(Environment.NewLine);
                    }
                    else
                        richTextBox.AppendText("No ships" + Environment.NewLine);

                    var ld = pf.GetSuitLoadouts();
                    if (ld != null)
                    {
                        richTextBox.AppendText("Suit Loadouts " + ld.Count + Environment.NewLine);
                        foreach (var s in ld)
                        {
                            richTextBox.AppendText(" " + s.LoadoutID + " N:" + s.UserLoadoutName + " SN:" + s.SuitName + "," + s.SuitLocName + " C:" + s.SuitID + Environment.NewLine);

                            if (s.slots != null)
                            {
                                foreach (var sl in s.slots)
                                    richTextBox.AppendText("     " + sl.SlotName + " " + sl.Name + " " + sl.LocName + Environment.NewLine);

                            }
                        }
                    }
                    else
                        richTextBox.AppendText("No suit loadouts" + Environment.NewLine);

                    var cld = pf.GetSuitCurrentLoadout();
                    if (cld != null)
                    {
                        richTextBox.AppendText("Current Suit Loadouts " + cld.Count + Environment.NewLine);
                        foreach (var s in cld)
                        {
                            richTextBox.AppendText("     " + s.SlotName + " " + s.Name + " " + s.LocName + " " + s.Health + " " + s.Value +" " + s.AmmoClip + " " + s.HopperSize + Environment.NewLine);
                        }
                    }
                    else
                        richTextBox.AppendText("No current suit loadouts" + Environment.NewLine);

                }
                else
                    richTextBox.AppendText( "No profile" + Environment.NewLine);

                richTextBox.AppendText("------------------------- /PROFILE" + Environment.NewLine);
                richTextBox.ScrollToCaret();
            }
        }

        private void buttonMarket_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Market(out DateTime servertime);

                if (p != null)
                {
                    string json = JToken.Parse(p, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL).ToString(true);
                    File.WriteAllText(rootpath + "\\market.json", json);
                  //  System.Diagnostics.Debug.WriteLine("Market JSON" + json);
                }

                richTextBox.AppendText("------------------------- MARKET" + Environment.NewLine);

                Market ep = p != null ? new Market(p) : null;
                //Market mk = new Market(File.ReadAllText(@"c:\code\logs\capi\market-default(1).json")); // debug

                if (ep != null && ep.IsValid)
                {
                    ReflectProperties(ep, " ");

                    {
                        var imports = ep.Imports;
                        if (imports != null)
                        {
                            richTextBox.AppendText("Imports: ");
                            foreach (var kvp in imports)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                            richTextBox.AppendText(Environment.NewLine);

                        }
                        else
                            richTextBox.AppendText("No Imports" + Environment.NewLine);
                    }

                    {
                        var exports = ep.Exports;
                        if (exports != null)
                        {
                            richTextBox.AppendText("exports: " );
                            foreach (var kvp in exports)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                            richTextBox.AppendText(Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Exports" + Environment.NewLine);
                    }

                    {
                        var services = ep.Services;
                        if (services != null)
                        {
                            richTextBox.AppendText("services: " );
                            foreach (var kvp in services)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                            richTextBox.AppendText(Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Services" + Environment.NewLine);
                    }

                    {
                        var economies = ep.Economies;
                        if (economies != null)
                        {
                            richTextBox.AppendText("economies: " );
                            foreach (var kvp in economies)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                            richTextBox.AppendText(Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Economies" + Environment.NewLine);
                    }

                    {
                        var prohibit = ep.Prohibited;
                        if (prohibit != null)
                        {
                            richTextBox.AppendText("prohibit: " );
                            foreach (var kvp in prohibit)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                            richTextBox.AppendText(Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Prohibited" + Environment.NewLine);
                    }

                    {
                        var commodities = ep.GetCommodities();
                        if (commodities != null)
                        {
                            richTextBox.AppendText("Commds " + commodities.Count + ": ");
                            foreach (var s in commodities)
                            {
                                ReflectProperties(s, "  ", eol: ", ");
                                richTextBox.AppendText(Environment.NewLine);
                            }

                        }
                        else
                            richTextBox.AppendText("No Commodities" + Environment.NewLine);
                    }

                    {
                        var orderssales = ep.GetOrdersCommoditiesSales();
                        if (orderssales != null)
                        {
                            richTextBox.AppendText("FC Orders for commodity.sales " + orderssales.Count + ": ");
                            foreach (var s in orderssales)
                            {
                                ReflectProperties(s, "  ", eol: ", ");
                                richTextBox.AppendText(Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No Orders Commodities Sales" + Environment.NewLine);
                    }
                    {
                        var orderspurchases = ep.GetOrdersCommoditiesPurchaces();
                        if (orderspurchases != null)
                        {
                            richTextBox.AppendText("FC Orders for commodity.purchases " + orderspurchases.Count + ": ");
                            foreach (var s in orderspurchases)
                            {
                                ReflectProperties(s, "  ", eol: ", ");
                                richTextBox.AppendText(Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No Orders Commodities Purchases" + Environment.NewLine);
                    }
                    {
                        var ordersmrpurchases = ep.GetOrdersMicroresourcesPurchases();
                        if (ordersmrpurchases != null)
                        {
                            richTextBox.AppendText("FC Orders for MR purchases " + ordersmrpurchases.Count + ": ");
                            foreach (var s in ordersmrpurchases)
                            {
                                ReflectProperties(s, "  ", eol: ", ");
                                richTextBox.AppendText(Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No MR Purchases" + Environment.NewLine);
                    }
                    {
                        var ordersmrsales = ep.GetOrdersMicroresourcesSales();
                        if (ordersmrsales != null)
                        {
                            richTextBox.AppendText("FC Orders for MR sales " + ordersmrsales.Count + ": ");
                            foreach (var s in ordersmrsales)
                            {
                                ReflectProperties(s, "  ", eol: ", ");
                                richTextBox.AppendText(Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No MR sales" + Environment.NewLine);
                    }

                }
                else
                    richTextBox.AppendText("No market data" + Environment.NewLine);

                richTextBox.AppendText("------------------------- /MARKET" + Environment.NewLine);
                richTextBox.ScrollToCaret();
            }
        }

        private void buttonFleetCarrier_Click(object sender, EventArgs e)
        {
            bool debugfile = checkBoxCarrierRead.Checked;

            if (debugfile || capi.Active)
            {
                FleetCarrier ep = null;

                if (!debugfile)
                {
                    string p = capi.FleetCarrier(out DateTime servertime);
                    if (p != null)
                    {
                        string json = JToken.Parse(p, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL).ToString(true);
                        File.WriteAllText(rootpath + "\\fleetcarrierdata.json", json);
                        System.Diagnostics.Debug.WriteLine("Fleet JSON" + json);
                        ep = new FleetCarrier(p);
                    }
                }
                else
                {
                    ep = new FleetCarrier(File.ReadAllText(@"c:\code\fleetcarrier.json")); // debug
                }

                richTextBox.AppendText("------------------------- FLEET CARRIER" + Environment.NewLine);

                if (ep != null && ep.IsValid)
                {
                    FleetCarrierDump(ep);
                }
                else
                    richTextBox.AppendText("No Data" + Environment.NewLine);

                richTextBox.AppendText("------------------------- /FLEET CARRIER" + Environment.NewLine);
                richTextBox.ScrollToCaret();
            }

        }

        private void FleetCarrierDump(FleetCarrier ep)
        {
            ReflectProperties(ep);
            richTextBox.AppendText(Environment.NewLine);

            {
                var it = ep.GetCompletedItinerary();
                if (it != null)
                {
                    richTextBox.AppendText("Itinerary" + Environment.NewLine);
                    foreach (var v in it)
                        ReflectProperties(v, eol: ", ");
                    richTextBox.AppendText(Environment.NewLine);
                }
                else
                    richTextBox.AppendText("No Itinerary" + Environment.NewLine);
            }
            {
                var imports = ep.Imports;
                if (imports != null)
                {
                    richTextBox.AppendText("Market.Imports: ");
                    foreach (var kvp in imports)
                        richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                    richTextBox.AppendText(Environment.NewLine);

                }
                else
                    richTextBox.AppendText("No Market.Imports" + Environment.NewLine);
            }

            {
                var exports = ep.Exports;
                if (exports != null)
                {
                    richTextBox.AppendText("Market.Exports: ");
                    foreach (var kvp in exports)
                        richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                    richTextBox.AppendText(Environment.NewLine);
                }
                else
                    richTextBox.AppendText("No Market.Exports" + Environment.NewLine);
            }

            {
                var services = ep.Services;
                if (services != null)
                {
                    richTextBox.AppendText("Market.Services: ");
                    foreach (var kvp in services)
                        richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                    richTextBox.AppendText(Environment.NewLine);
                }
                else
                    richTextBox.AppendText("No Market.Services" + Environment.NewLine);
            }

            {
                var economies = ep.Economies;
                if (economies != null)
                {
                    richTextBox.AppendText("Market.Economies: ");
                    foreach (var kvp in economies)
                        richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                    richTextBox.AppendText(Environment.NewLine);
                }
                else
                    richTextBox.AppendText("No Market.Economies" + Environment.NewLine);
            }

            {
                var prohibit = ep.Prohibited;
                if (prohibit != null)
                {
                    richTextBox.AppendText("Market.Prohibited: ");
                    foreach (var kvp in prohibit)
                        richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                    richTextBox.AppendText(Environment.NewLine);
                }
                else
                    richTextBox.AppendText("No Market.Prohibited" + Environment.NewLine);
            }

            {
                var commodities = ep.GetMarketCommodities();
                if (commodities != null)
                {
                    richTextBox.AppendText("Market.Commodities " + commodities.Count + ": ");
                    foreach (var s in commodities)
                    {
                        ReflectProperties(s, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("Commds null" + Environment.NewLine);
            }

            {
                var services = ep.GetCrewServices();
                if (services != null)
                {
                    foreach (var kvp in services)
                    {
                        richTextBox.AppendText($"serviceCrew {kvp.Key} :");
                        ReflectProperties(kvp.Value, "   ", eol: ", ");
                        if (kvp.Value.Invoices != null)
                        {
                            richTextBox.AppendText(Environment.NewLine);
                            foreach (var x in kvp.Value.Invoices)
                                ReflectProperties(x, " ", eol: ", ");
                        }
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("No Crew Services" + Environment.NewLine);
            }

            {
                var cargo = ep.GetCargo();
                if (cargo != null)
                {
                    richTextBox.AppendText($"Cargo: ");
                    foreach (var c in cargo)
                    {
                        ReflectProperties(c, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("No Cargo" + Environment.NewLine);
            }


            {
                var orderssales = ep.GetOrdersCommoditiesSales();
                if (orderssales != null)
                {
                    richTextBox.AppendText("FC Orders for commodity.sales " + orderssales.Count + ": ");
                    foreach (var s in orderssales)
                    {
                        ReflectProperties(s, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("No Orders Commodities Sales" + Environment.NewLine);
            }
            {
                var orderspurchases = ep.GetOrdersCommoditiesPurchaces();
                if (orderspurchases != null)
                {
                    richTextBox.AppendText("FC Orders for commodity.purchases " + orderspurchases.Count + ": ");
                    foreach (var s in orderspurchases)
                    {
                        ReflectProperties(s, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("No Orders Commodities Purchases" + Environment.NewLine);
            }
            {
                var ordersmrpurchases = ep.GetOrdersMicroresourcesPurchases();
                if (ordersmrpurchases != null)
                {
                    richTextBox.AppendText("FC Orders for MR purchases " + ordersmrpurchases.Count + ": ");
                    foreach (var s in ordersmrpurchases)
                    {
                        ReflectProperties(s, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("No MR Purchases" + Environment.NewLine);
            }
            {
                var ordersmrsales = ep.GetOrdersMicroresourcesSales();
                if (ordersmrsales != null)
                {
                    richTextBox.AppendText("FC Orders for MR sales " + ordersmrsales.Count + ": ");
                    foreach (var s in ordersmrsales)
                    {
                        ReflectProperties(s, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("No MR sales" + Environment.NewLine);
            }

            {
                var v = ep.GetCarrierLocker(FleetCarrier.LockerType.Assets);
                if (v != null)
                {
                    richTextBox.AppendText("CarrierLocker.Assets: ");
                    foreach (var s in v)
                    {
                        ReflectProperties(s, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("No CarrierLocker.Assets" + Environment.NewLine);
            }
            {
                var v = ep.GetCarrierLocker(FleetCarrier.LockerType.Goods);
                if (v != null)
                {
                    richTextBox.AppendText("CarrierLocker.Goods: ");
                    foreach (var s in v)
                    {
                        ReflectProperties(s, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("No CarrierLocker.Goods" + Environment.NewLine);
            }
            {
                var v = ep.GetCarrierLocker(FleetCarrier.LockerType.Data);
                if (v != null)
                {
                    richTextBox.AppendText("CarrierLocker.Data: ");
                    foreach (var s in v)
                    {
                        ReflectProperties(s, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("No CarrierLocker.Data" + Environment.NewLine);
            }

            {
                var rep = ep.GetReputation();
                if (rep != null)
                {
                    foreach (var v in rep)
                        richTextBox.AppendText($"Reputation {v.Key} = {v.Value}" + ", ");
                    richTextBox.AppendText(Environment.NewLine);
                }
                else
                    richTextBox.AppendText("No Rep" + Environment.NewLine);
            }

            {
                var modules = ep.GetModules();
                if (modules != null)
                {
                    richTextBox.AppendText("Modules: ");
                    foreach (var s in modules)
                    {
                        ReflectProperties(s, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("modules null" + Environment.NewLine);

                var ships = ep.GetShips();
                if (ships != null)
                {
                    richTextBox.AppendText("Ships: ");
                    foreach (var s in ships)
                    {
                        ReflectProperties(s, "  ", eol: ", ");
                        richTextBox.AppendText(Environment.NewLine);
                    }
                }
                else
                    richTextBox.AppendText("Shipyard null" + Environment.NewLine);
            }
        }

        private void buttonSquadronCarrier_Click(object sender, EventArgs e)
        {
            bool debugfile = checkBoxSquadronRead.Checked;

            if (debugfile || capi.Active)
            {
                Squadrons ep = null;

                if (!debugfile)
                {
                    string p = capi.Squadrons(out DateTime servertime);
                    if (p != null)
                    {
                        string json = JToken.Parse(p, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL).ToString(true);
                        File.WriteAllText(rootpath + "\\squadrons.json", json);
                        System.Diagnostics.Debug.WriteLine("Squadrons JSON" + json);
                        ep = new Squadrons(p);
                    }
                }
                else
                {
                    ep = new Squadrons(File.ReadAllText(@"c:\code\squadrons.json")); // debug
                }

                richTextBox.AppendText("------------------------- SQUADRONS" + Environment.NewLine);

                richTextBox.AppendText($"Name {ep.ID} `{ep.Name}` created {ep.Created}" + Environment.NewLine);
                richTextBox.AppendText($"NewMemmbers {ep.AcceptingNewMembers}`" + Environment.NewLine);
                richTextBox.AppendText($"Power `{ep.PowerName}` Faction `{ep.FactionName}` Faction Home `{ep.FactionHomeSystem}`:{ep.FactionHomeSystemAddress}" + Environment.NewLine);
                richTextBox.AppendText($"Members {ep.MemberCount}" + Environment.NewLine);
                richTextBox.AppendText($"Mission {ep.MissionStatement}" + Environment.NewLine);
                richTextBox.AppendText($"Motto {ep.Motto}" + Environment.NewLine);
                richTextBox.AppendText($"Active {ep.Active7Days} {ep.Active30Days}" + Environment.NewLine);
                richTextBox.AppendText($"Perks `{ep.PrimaryPerk}` `{ep.SecondaryPerk}`" + Environment.NewLine);
                richTextBox.AppendText($"Credits {ep.Credits} carrier {ep.CarrierCredits}" + Environment.NewLine);

                foreach (var m in ep.Members)
                {
                    richTextBox.AppendText($"Member {m.Name} {m.Joined} flying {m.ShipName}" + Environment.NewLine);
                }

                foreach ( var kvp in ep.Commodities)
                {
                    richTextBox.AppendText($"Commodities {kvp.Key}" + Environment.NewLine);
                    foreach ( var c in kvp.Value)
                        richTextBox.AppendText($"  {c.Name} {c.Stock}" + Environment.NewLine);
                }

                FleetCarrierDump(ep.Carrier);

                richTextBox.AppendText("------------------------- /SQUADRONS" + Environment.NewLine);
                richTextBox.ScrollToCaret();
            }
        }

        private void buttonShipyard_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Shipyard(out DateTime servertime);

                if (p != null)
                {
                    string json = JToken.Parse(p, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL).ToString(true);
                    File.WriteAllText(rootpath + "\\shipyard.json", json);
                    System.Diagnostics.Debug.WriteLine("Ship JSON" + json);
                }

                richTextBox.AppendText("------------------------- SHIPYARD" + Environment.NewLine);

                Shipyard ep = p!=null ? new Shipyard(p) : null;
                if (ep!=null && ep.IsValid)
                {
                    ReflectProperties(ep, " ");
                    {
                        var imports = ep.Imports;
                        if (imports != null)
                        {
                            richTextBox.AppendText("Imports" + Environment.NewLine);
                            foreach (var kvp in imports)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                            richTextBox.AppendText(Environment.NewLine);

                        }
                        else
                            richTextBox.AppendText("No Imports" + Environment.NewLine);
                    }

                    {
                        var exports = ep.Exports;
                        if (exports != null)
                        {
                            richTextBox.AppendText("exports" + Environment.NewLine);
                            foreach (var kvp in exports)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                            richTextBox.AppendText(Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Exports" + Environment.NewLine);
                    }

                    {
                        var services = ep.Services;
                        if (services != null)
                        {
                            richTextBox.AppendText("services" + Environment.NewLine);
                            foreach (var kvp in services)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                            richTextBox.AppendText(Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Services" + Environment.NewLine);
                    }

                    {
                        var economies = ep.Economies;
                        if (economies != null)
                        {
                            richTextBox.AppendText("economies" + Environment.NewLine);
                            foreach (var kvp in economies)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + ", ");
                            richTextBox.AppendText(Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Economies" + Environment.NewLine);
                    }

                    {
                        var modules = ep.GetModules();
                        if (modules != null)
                        {
                            richTextBox.AppendText("modules" + Environment.NewLine);
                            foreach (var v in modules)
                                richTextBox.AppendText(string.Format("  {0} {1}", v.Name, v.Category) + ", ");
                            richTextBox.AppendText(Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Modules" + Environment.NewLine);
                    }
                    {
                        var ships = ep.GetPurchasableShips();
                        if (ships != null)
                        {
                            richTextBox.AppendText("purchasable ships" + Environment.NewLine);
                            foreach (var v in ships)
                                richTextBox.AppendText(string.Format("  {0} {1}", v.Name, v.BaseValue) + ", ");
                            richTextBox.AppendText(Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No purchasable ships" + Environment.NewLine);
                    }
                    {
                        var ships = ep.GetUnobtainableShips();
                        if (ships != null)
                        {
                            richTextBox.AppendText("unobtainable ships" + Environment.NewLine);
                            foreach (var v in ships)
                                richTextBox.AppendText(string.Format("  {0} {1}", v.Name, v.BaseValue) + ", ");
                            richTextBox.AppendText(Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No unobtainable ships" + Environment.NewLine);
                    }

                }
                else
                    richTextBox.AppendText( "No Ship data" + Environment.NewLine);

                richTextBox.AppendText("------------------------- /SHIPYARD" + Environment.NewLine);
                richTextBox.ScrollToCaret();
            }

        }

        private void buttonjournal_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Journal(dateTimePicker.Value, out System.Net.HttpStatusCode status);

                richTextBox.AppendText("------------------------- JOURNAL" + Environment.NewLine);
                richTextBox.AppendText( "Journal Response " + status + Environment.NewLine);

                if (p != null)
                {
                    File.WriteAllText(rootpath+"\\journal.json", p);
                }

                richTextBox.AppendText("------------------------- /JOURNAL" + Environment.NewLine);
                richTextBox.ScrollToCaret();
            }

        }


        private void buttonCG_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.CommunityGoals(out DateTime servertime);

                richTextBox.AppendText("------------------------- COMMUNITY GOALS" + Environment.NewLine);

                if (p != null)
                {
                    File.WriteAllText(rootpath+"\\communitygoals.json", p);
                    richTextBox.AppendText( p + Environment.NewLine);
                    richTextBox.ScrollToCaret();
                }

                richTextBox.AppendText("------------------------- /COMMUNITY GOALS" + Environment.NewLine);
            }

        }

        private void checkBoxBeta_CheckedChanged(object sender, EventArgs e)
        {
            capi.CAPIServer = CompanionAPI.CAPIServerType.Beta;
        }
        private void checkBoxLegacy_CheckedChanged(object sender, EventArgs e)
        {
            capi.CAPIServer = CompanionAPI.CAPIServerType.Legacy;
        }


        void ReflectProperties(Object fc, string prefix = " ", string secondprefix = null, string eol = "\r\n")
        {
            if (secondprefix == null)
                secondprefix = prefix;

            foreach (System.Reflection.PropertyInfo pi in fc.GetType().GetProperties())
            {
                if (!pi.PropertyType.FullName.Contains("Generic."))
                {
                    System.Reflection.MethodInfo getter = pi.GetGetMethod();
                    Object value = getter.Invoke(fc, null);
                    if (value != null)
                    {
                        richTextBox.AppendText(prefix + pi.Name + " = " + value.ToString() + eol);
                        prefix = secondprefix;
                    }
                }

            }

        }

        private void richTextBox_DoubleClick(object sender, EventArgs e)
        {
            richTextBox.Clear();
        }


    }
}
