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

// NOTE you need an enviromental variable called CAPIID set up with your Frontier CAPI ID BEFORE RUNNING visual studio.
// use the control panel system | Enviromental variables to initialise this
// also expects a c:\code to store the ceritificates.

namespace CAPIDemo
{
    public partial class CapiDemoForm : Form
    {
        CompanionAPI capi;
        DDEServer ddeserver;
        const string rootpath = @"c:\code\";            // where to dump files
        const string rootpathnoslash = @"c:\code";            // where to dump files

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

            System.Diagnostics.Debug.Assert(CapiClientIdentity.id.Contains("-"));

            capi = new CompanionAPI(rootpathnoslash, CapiClientIdentity.id, $"EDCD-Program-1.2.3.4", uri);

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

        private void buttonProfile_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Profile();
                File.WriteAllText(rootpath+"profile.json", p);

                richTextBox.AppendText( "-------------------------" + Environment.NewLine);

                Profile pf = new Profile(p);
                if (pf.IsValid)
                {
                    richTextBox.AppendText( "Commander " + pf.Commander + " " + pf.ID + Environment.NewLine);
                    richTextBox.AppendText( "Credits " + pf.Credits + Environment.NewLine);
                    richTextBox.AppendText( "Combat " + pf.RankCombat + Environment.NewLine);
                    richTextBox.AppendText("Explore " + pf.RankExplore + Environment.NewLine);
                    richTextBox.AppendText("Soldier " + pf.RankSoldier + Environment.NewLine);
                    richTextBox.AppendText("Exobio " + pf.RankExoBiologist + Environment.NewLine);
                    richTextBox.AppendText("Odyssey " + pf.Odyssey+ Environment.NewLine);

                    richTextBox.AppendText( "Starport " + pf.StarPort + Environment.NewLine);
                    var srv = pf.StarPortServices;
                    richTextBox.AppendText( "Starport Major Faction " + pf.StarPortMajorFaction + Environment.NewLine);
                    richTextBox.AppendText( "Starport Minor Faction " + pf.StarPortMinorFaction + Environment.NewLine);

                    richTextBox.AppendText("Suit " + pf.SuitName + " " + pf.SuitId + " " + pf.SuitHealth + Environment.NewLine);

                    richTextBox.AppendText("Ship " + pf.Ship + Environment.NewLine);
                    richTextBox.AppendText( "Name " + pf.ShipName + Environment.NewLine);
                    richTextBox.AppendText( "Ident " + pf.ShipIdent + Environment.NewLine);
                    richTextBox.AppendText( "Value " + pf.ShipTotalValue + Environment.NewLine);

                    var ml = pf.GetModules();
                    if (ml != null)
                    {
                        richTextBox.AppendText("Modules " + ml.Count + Environment.NewLine);
                        foreach (var m in ml)
                        {
                            richTextBox.AppendText(" " + m.Name + " " + m.Value + Environment.NewLine);
                        }
                    }
                    var sh = pf.GetShips();
                    if (sh != null)
                    {
                        richTextBox.AppendText("Ships " + sh.Count + Environment.NewLine);
                        foreach (var s in sh)
                        {
                            richTextBox.AppendText(" " + s.Name + " C:" + s.Cargo + " S:" + s.Station + Environment.NewLine);
                        }
                    }

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

                    var cld = pf.GetSuitCurrentLoadout();
                    if (cld != null)
                    {
                        richTextBox.AppendText("Current Suit Loadouts " + cld.Count + Environment.NewLine);
                        foreach (var s in cld)
                        {
                            richTextBox.AppendText("     " + s.SlotName + " " + s.Name + " " + s.LocName + " " + s.Health + " " + s.Value +" " + s.AmmoClip + " " + s.HopperSize + Environment.NewLine);
                        }
                    }



                }
                else
                    richTextBox.AppendText( "No profile" + Environment.NewLine);

                richTextBox.ScrollToCaret();
            }
        }

        private void buttonLogout_Click(object sender, EventArgs e)
        {
            capi.LogOut();
            richTextBox.AppendText( "Logout" + Environment.NewLine);
        }

        private void buttonMarket_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Market();
                File.WriteAllText(rootpath+"market.json", p);

                richTextBox.AppendText( "-------------------------" + Environment.NewLine);

                Market mk = new Market(p);

                if (mk.IsValid)
                {
                    richTextBox.AppendText( mk.Name + " " + mk.ID + " " + mk.Type + Environment.NewLine);
                    var imports = mk.Imports;
                    var exports = mk.Exports;
                    var services = mk.Services;
                    var prohibit = mk.Prohibited;
                    var economies = mk.Economies;
                    var commodities = mk.GetCommodities();
                    if (commodities != null)
                    {
                        richTextBox.AppendText("Commds " + commodities.Count + Environment.NewLine);
                        foreach (var s in commodities)
                        {
                            richTextBox.AppendText(" " + s.Name + " S:" + s.Sell + " B:" + s.Buy + " Stock:" + s.Stock +  Environment.NewLine);
                        }
                    }
                }
                else
                    richTextBox.AppendText( "No market data" + Environment.NewLine);

                richTextBox.ScrollToCaret();
            }
        }

        private void buttonShipyard_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Shipyard();
                File.WriteAllText(rootpath+"shipyard.json", p);
                richTextBox.AppendText( "-------------------------" + Environment.NewLine);

                Shipyard sy = new Shipyard(p);
                if (sy.IsValid)
                {
                    richTextBox.AppendText( sy.Name + " " + sy.ID + " " + sy.Type + Environment.NewLine);
                    var imports = sy.Imports;
                    var exports = sy.Exports;
                    var services = sy.Services;
                    var economies = sy.Economies;
                    var modules = sy.GetModules();
                    if (modules != null)
                        richTextBox.AppendText( "Modules " + modules.Count + Environment.NewLine);
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
                    File.WriteAllText(rootpath+"journal.json", p);
                }

                richTextBox.ScrollToCaret();
            }

        }

        private void buttonFleetCarrier_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.FleetCarrier();

                richTextBox.AppendText("-------------------------" + Environment.NewLine);
                richTextBox.AppendText( "Fleet Carrier" + Environment.NewLine);

                if (p != null)
                {
                    File.WriteAllText(rootpath+"fleetcarrier.json", p);
                    richTextBox.AppendText("Response" + p + Environment.NewLine);
                    richTextBox.ScrollToCaret();
                }
                else
                    richTextBox.AppendText("No Data" + Environment.NewLine);
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
                    File.WriteAllText(rootpath+"communitygoals.json", p);
                    richTextBox.AppendText( "Community Goals" + p + Environment.NewLine);
                    richTextBox.ScrollToCaret();
                }

            }

        }

        private void checkBoxBeta_CheckedChanged(object sender, EventArgs e)
        {
            capi.GameIsBeta = checkBoxBeta.Checked;
        }
    }
}
