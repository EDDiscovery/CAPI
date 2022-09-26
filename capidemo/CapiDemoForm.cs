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
            richTextBox.AppendText( "Login One" + Environment.NewLine);
            richTextBox.ScrollToCaret();
        }

        private void buttonLoginTwo_Click(object sender, EventArgs e)
        {
            capi.LogIn("two:user");
            richTextBox.AppendText( "-------------------------" + Environment.NewLine);
            richTextBox.AppendText( "Login Two" + Environment.NewLine);
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
                string p = capi.Profile();

                if (p != null)
                {
                    string json = JToken.Parse(p, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL).ToString(true);
                    File.WriteAllText(rootpath + "\\profile.json", json);
                    System.Diagnostics.Debug.WriteLine("Profile JSON" + json);
                }

                richTextBox.AppendText( "-------------------------" + Environment.NewLine);

                Profile pf = p != null ? new Profile(p) : null;
                if (pf != null && pf.IsValid)
                {
                    ReflectProperties(pf, " ");

                    var ml = pf.GetShipModules();
                    if (ml != null)
                    {
                        richTextBox.AppendText("Modules " + ml.Count + Environment.NewLine);
                        foreach (var m in ml)
                        {
                            richTextBox.AppendText(" " + m.Name + " " + m.Value + Environment.NewLine);
                        }
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
                            richTextBox.AppendText(" " + s.Name + " C:" + s.Cargo + " S:" + s.Station + Environment.NewLine);
                        }
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

                    richTextBox.AppendText("---------------" + Environment.NewLine);
                }
                else
                    richTextBox.AppendText( "No profile" + Environment.NewLine);

                richTextBox.AppendText("---------------" + Environment.NewLine);
                richTextBox.ScrollToCaret();
            }
        }

        private void buttonMarket_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Market();

                if (p != null)
                {
                    string json = JToken.Parse(p, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL).ToString(true);
                    File.WriteAllText(rootpath + "\\market.json", json);
                    System.Diagnostics.Debug.WriteLine("Market JSON" + json);
                }

                richTextBox.AppendText( "-------------------------" + Environment.NewLine);

                Market mk = p != null ? new Market(p) : null;
                //Market mk = new Market(File.ReadAllText(@"c:\code\logs\capi\market-default(1).json")); // debug

                if (mk != null && mk.IsValid)
                {
                    ReflectProperties(mk, " ");

                    {
                        var imports = mk.Imports;
                        if (imports != null)
                        {
                            richTextBox.AppendText("Imports" + Environment.NewLine);
                            foreach (var kvp in imports)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Imports" + Environment.NewLine);
                    }

                    {
                        var exports = mk.Exports;
                        if (exports != null)
                        {
                            richTextBox.AppendText("exports" + Environment.NewLine);
                            foreach (var kvp in exports)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Exports" + Environment.NewLine);
                    }

                    {
                        var services = mk.Services;
                        if (services != null)
                        {
                            richTextBox.AppendText("services" + Environment.NewLine);
                            foreach (var kvp in services)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Services" + Environment.NewLine);
                    }

                    {
                        var economies = mk.Economies;
                        if (economies != null)
                        {
                            richTextBox.AppendText("economies" + Environment.NewLine);
                            foreach (var kvp in economies)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Economies" + Environment.NewLine);
                    }

                    {
                        var prohibit = mk.Prohibited;
                        if (prohibit != null)
                        {
                            richTextBox.AppendText("prohibit" + Environment.NewLine);
                            foreach (var kvp in prohibit)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Prohibited" + Environment.NewLine);
                    }

                    {
                        var commodities = mk.GetCommodities();
                        if (commodities != null)
                        {
                            richTextBox.AppendText("Commds " + commodities.Count + Environment.NewLine);
                            foreach (var s in commodities)
                            {
                                richTextBox.AppendText("        " + s.Name + " LN:" + s.LocName + " S:" + s.Sell + " B:" + s.Buy + " Stock:" + s.Stock + Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No Commodities" + Environment.NewLine);
                    }

                    {
                        var orderssales = mk.GetOrdersCommoditiesSales();
                        if (orderssales != null)
                        {
                            richTextBox.AppendText("FC Orders for sales " + orderssales.Count + Environment.NewLine);
                            foreach (var s in orderssales)
                            {
                                richTextBox.AppendText("        " + s.Name +  " P:" + s.Price + " Stock:" + s.Stock + Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No Orders Commodities Sales" + Environment.NewLine);
                    }
                    {
                        var orderspurchases = mk.GetOrdersCommoditiesPurchaces();
                        if (orderspurchases != null)
                        {
                            richTextBox.AppendText("FC Orders for purchases " + orderspurchases.Count + Environment.NewLine);
                            foreach (var s in orderspurchases)
                            {
                                richTextBox.AppendText("        " + s.Name + " T:" + s.Total + " O:" + s.Outstanding + " P:" + s.Price + Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No Orders Commodities Purchases" + Environment.NewLine);
                    }
                    {
                        var ordersmrpurchases = mk.GetOrdersMicroresourcesPurchases();
                        if (ordersmrpurchases != null)
                        {
                            richTextBox.AppendText("FC Orders for MR purchases " + ordersmrpurchases.Count + Environment.NewLine);
                            foreach (var s in ordersmrpurchases)
                            {
                                richTextBox.AppendText("        " + s.Name + " LN:" + s.LocName + " T:" + s.Total + " O:" + s.Outstanding + " P:" + s.Price + Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No MR Purchases" + Environment.NewLine);
                    }
                    {
                        var ordersmrsales = mk.GetOrdersMicroresourcesSales();
                        if (ordersmrsales != null)
                        {
                            richTextBox.AppendText("FC Orders for MR sales " + ordersmrsales.Count + Environment.NewLine);
                            foreach (var s in ordersmrsales)
                            {
                                richTextBox.AppendText("        " + s.Name + " LN:" + s.LocName + " S:" + s.Stock + " P:" + s.Price + Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No MR sales" + Environment.NewLine);
                    }

                    richTextBox.AppendText("---------------" + Environment.NewLine);
                }
                else
                    richTextBox.AppendText("No market data" + Environment.NewLine);

                richTextBox.ScrollToCaret();
            }
        }

        private void buttonFleetCarrier_Click(object sender, EventArgs e)
        {
            bool debugfile = false;

            if (debugfile || capi.Active)
            {
                FleetCarrier fc = null;

                if (!debugfile)
                {
                    string p = capi.FleetCarrier();
                    if (p != null)
                    {
                        string json = JToken.Parse(p, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL).ToString(true);
                        File.WriteAllText(rootpath + "\\fleetcarrierdata.json", json);
                        System.Diagnostics.Debug.WriteLine("Fleet JSON" + json);
                        fc = new FleetCarrier(p);
                    }
                }
                else
                {
                    fc = new FleetCarrier(File.ReadAllText(@"c:\code\fleetcarrier.json")); // debug
                }


                richTextBox.AppendText("-------------------------" + Environment.NewLine);
                richTextBox.AppendText("Fleet Carrier" + Environment.NewLine);

                if (fc != null && fc.IsValid)
                {
                    ReflectProperties(fc);

                    {
                        var services = fc.GetCrewServices();
                        foreach (var kvp in services)
                        {
                            richTextBox.AppendText($" Service {kvp.Key}" + Environment.NewLine);
                            ReflectProperties(kvp.Value, "   ");
                            if (kvp.Value.Invoices != null)
                            {
                                foreach (var x in kvp.Value.Invoices)
                                    ReflectProperties(x, "     ", "     .. ");
                            }
                        }
                    }

                    {
                        var cargo = fc.GetCargo();
                        if (cargo != null)
                        {
                            richTextBox.AppendText($" Cargo" + Environment.NewLine);
                            foreach (var c in cargo)
                                ReflectProperties(c, "     ", "     .. ");
                        }
                        else
                            richTextBox.AppendText("No Cargo" + Environment.NewLine);
                    }

                    {
                        var orderssales = fc.GetOrdersCommoditiesSales();
                        if (orderssales != null)
                        {
                            richTextBox.AppendText("FC Orders for sales " + orderssales.Count + Environment.NewLine);
                            foreach (var s in orderssales)
                            {
                                richTextBox.AppendText("        " + s.Name + " P:" + s.Price + " Stock:" + s.Stock + Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No Orders Commodities Sales" + Environment.NewLine);
                    }
                    {
                        var orderspurchases = fc.GetOrdersCommoditiesPurchaces();
                        if (orderspurchases != null)
                        {
                            richTextBox.AppendText("FC Orders for purchases " + orderspurchases.Count + Environment.NewLine);
                            foreach (var s in orderspurchases)
                            {
                                richTextBox.AppendText("        " + s.Name + " T:" + s.Total + " O:" + s.Outstanding + " P:" + s.Price + Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No Orders Commodities Purchases" + Environment.NewLine);
                    }
                    {
                        var ordersmrpurchases = fc.GetOrdersMicroresourcesPurchases();
                        if (ordersmrpurchases != null)
                        {
                            richTextBox.AppendText("FC Orders for MR purchases " + ordersmrpurchases.Count + Environment.NewLine);
                            foreach (var s in ordersmrpurchases)
                            {
                                richTextBox.AppendText("        " + s.Name + " LN:" + s.LocName + " T:" + s.Total + " O:" + s.Outstanding + " P:" + s.Price + Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No MR Purchases" + Environment.NewLine);
                    }
                    {
                        var ordersmrsales = fc.GetOrdersMicroresourcesSales();
                        if (ordersmrsales != null)
                        {
                            richTextBox.AppendText("FC Orders for MR sales " + ordersmrsales.Count + Environment.NewLine);
                            foreach (var s in ordersmrsales)
                            {
                                richTextBox.AppendText("        " + s.Name + " LN:" + s.LocName + " S:" + s.Stock + " P:" + s.Price + Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("No MR sales" + Environment.NewLine);
                    }

                    {
                        var rep = fc.GetReputation();
                        if (rep != null)
                        {
                            foreach (var v in rep)
                                richTextBox.AppendText($"Reputation {v.Key} = {v.Value}" + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Rep" + Environment.NewLine);
                    }

                    {
                        var imports = fc.Imports;
                        if (imports != null)
                        {
                            richTextBox.AppendText("Imports" + Environment.NewLine);
                            foreach (var kvp in imports)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Imports" + Environment.NewLine);

                        var exports = fc.Exports;
                        if (exports != null)
                        {
                            richTextBox.AppendText("exports" + Environment.NewLine);
                            foreach (var kvp in exports)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Exports" + Environment.NewLine);

                        var services = fc.Services;
                        if (services != null)
                        {
                            richTextBox.AppendText("services" + Environment.NewLine);
                            foreach (var kvp in services)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("Services null" + Environment.NewLine);

                        var economies = fc.Economies;
                        if (economies != null)
                        {
                            richTextBox.AppendText("economies" + Environment.NewLine);
                            foreach (var kvp in economies)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("Economies null" + Environment.NewLine);

                        var prohibit = fc.Prohibited;
                        if (prohibit != null)
                        {
                            richTextBox.AppendText("prohibit" + Environment.NewLine);
                            foreach (var kvp in prohibit)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("prohibit null" + Environment.NewLine);

                        var commodities = fc.GetCommodities();
                        if (commodities != null)
                        {
                            richTextBox.AppendText("Commds " + commodities.Count + Environment.NewLine);
                            foreach (var s in commodities)
                            {
                                richTextBox.AppendText("        " + s.Name + " LN:" + s.LocName + " S:" + s.Sell + " B:" + s.Buy + " Stock:" + s.Stock + Environment.NewLine);
                            }
                        }
                        else
                            richTextBox.AppendText("Commds null" + Environment.NewLine);
                    }
                    {
                        var modules = fc.GetModules();
                        if (modules != null)
                        {
                            richTextBox.AppendText("modules" + Environment.NewLine);
                            foreach (var v in modules)
                                richTextBox.AppendText(string.Format("  {0} {1}", v.Name, v.Category) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("modules null" + Environment.NewLine);
                        var ships = fc.GetShips();
                        if (ships != null)
                        {
                            richTextBox.AppendText("ships" + Environment.NewLine);
                            foreach (var v in ships)
                                richTextBox.AppendText(string.Format("  {0} {1}", v.Name, v.BaseValue) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("Shipyard null" + Environment.NewLine);
                    }


                    richTextBox.ScrollToCaret();
                }
                else
                    richTextBox.AppendText("No Data" + Environment.NewLine);
            }

        }

        private void buttonShipyard_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Shipyard();

                if (p != null)
                {
                    string json = JToken.Parse(p, JToken.ParseOptions.AllowTrailingCommas | JToken.ParseOptions.CheckEOL).ToString(true);
                    File.WriteAllText(rootpath + "\\shipyard.json", json);
                    System.Diagnostics.Debug.WriteLine("Ship JSON" + json);
                }

                richTextBox.AppendText( "-------------------------" + Environment.NewLine);
                
                Shipyard sy = p!=null ? new Shipyard(p) : null;
                if (sy!=null && sy.IsValid)
                {
                    ReflectProperties(sy, " ");
                    {
                        var imports = sy.Imports;
                        if (imports != null)
                        {
                            richTextBox.AppendText("Imports" + Environment.NewLine);
                            foreach (var kvp in imports)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Imports" + Environment.NewLine);
                    }
                    {
                        var exports = sy.Exports;
                        if (exports != null)
                        {
                            richTextBox.AppendText("exports" + Environment.NewLine);
                            foreach (var kvp in exports)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Exports" + Environment.NewLine);
                    }

                    {
                        var services = sy.Services;
                        if (services != null)
                        {
                            richTextBox.AppendText("services" + Environment.NewLine);
                            foreach (var kvp in services)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Services" + Environment.NewLine);
                    }
                    {
                        var economies = sy.Economies;
                        if (economies != null)
                        {
                            richTextBox.AppendText("economies" + Environment.NewLine);
                            foreach (var kvp in economies)
                                richTextBox.AppendText(string.Format("  {0} = {1}", kvp.Key, kvp.Value) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Economies" + Environment.NewLine);
                    }

                    {
                        var modules = sy.GetModules();
                        if (modules != null)
                        {
                            richTextBox.AppendText("modules" + Environment.NewLine);
                            foreach (var v in modules)
                                richTextBox.AppendText(string.Format("  {0} {1}", v.Name, v.Category) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Modules" + Environment.NewLine);
                    }
                    { 
                        var ships = sy.GetShips();
                        if (ships != null)
                        {
                            richTextBox.AppendText("ships" + Environment.NewLine);
                            foreach (var v in ships)
                                richTextBox.AppendText(string.Format("  {0} {1}", v.Name, v.BaseValue) + Environment.NewLine);
                        }
                        else
                            richTextBox.AppendText("No Ships" + Environment.NewLine);
                    }
                    richTextBox.AppendText("---------------" + Environment.NewLine);
                }
                else
                    richTextBox.AppendText( "No Ship data" + Environment.NewLine);

                richTextBox.ScrollToCaret();

            }

        }

        private void buttonjournal_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Journal(dateTimePicker.Value, out System.Net.HttpStatusCode status);

                richTextBox.AppendText( "-------------------------" + Environment.NewLine);
                richTextBox.AppendText( "Journal Response " + status + Environment.NewLine);

                if (p != null)
                {
                    File.WriteAllText(rootpath+"\\journal.json", p);
                }

                richTextBox.ScrollToCaret();
            }

        }


        private void buttonCG_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.CommunityGoals();

                richTextBox.AppendText( "-------------------------" + Environment.NewLine);

                if (p != null)
                {
                    File.WriteAllText(rootpath+"\\communitygoals.json", p);
                    richTextBox.AppendText( "Community Goals" + p + Environment.NewLine);
                    richTextBox.ScrollToCaret();
                }

            }

        }

        private void checkBoxBeta_CheckedChanged(object sender, EventArgs e)
        {
            capi.GameIsBeta = checkBoxBeta.Checked;
        }


        void ReflectProperties(Object fc, string prefix = " ", string secondprefix = null)
        {
            if (secondprefix == null)
                secondprefix = prefix;

            foreach (System.Reflection.PropertyInfo pi in fc.GetType().GetProperties())
            {
                System.Reflection.MethodInfo getter = pi.GetGetMethod();
                Object value = getter.Invoke(fc, null);
                if (value != null)
                {
                    richTextBox.AppendText(prefix + pi.Name + " = " + value.ToString() + Environment.NewLine);
                    prefix = secondprefix;
                }

            }

        }

        private void richTextBox_DoubleClick(object sender, EventArgs e)
        {
            richTextBox.Clear();
        }
    }
}
