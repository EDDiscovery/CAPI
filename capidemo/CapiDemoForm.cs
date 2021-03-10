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

        public CapiDemoForm()
        {
            InitializeComponent();
            for( int i = 0; i < Environment.GetCommandLineArgs().Length; i++)
                richTextBox.Text += Environment.GetCommandLineArgs()[i] + Environment.NewLine;

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
                        richTextBox.Text += "DDE Server setup" + Environment.NewLine;
                    }
                }
            }

            System.Diagnostics.Debug.Assert(CapiClientIdentity.id.Contains("-"));

            capi = new CompanionAPI(@"c:\code", CapiClientIdentity.id, $"EDCD-Program-1.2.3.4", uri);

            dateTimePicker.Value = DateTime.UtcNow;
        }

        private void handleCallbackUrl(IntPtr hurl)
        {
            string url = DDEServer.FromDdeStringHandle(hurl);
            richTextBox.Text += "URL Callback " + url + Environment.NewLine;
            if (capi != null)
                capi.URLCallBack(url);
        }


        private void buttonLoginOne_Click(object sender, EventArgs e)
        {
            capi.LogIn("one");
            richTextBox.Text += "-------------------------" + Environment.NewLine;
            richTextBox.Text += "Login One" + Environment.NewLine;
        }

        private void buttonLoginTwo_Click(object sender, EventArgs e)
        {
            capi.LogIn("two:user");
            richTextBox.Text += "-------------------------" + Environment.NewLine;
            richTextBox.Text += "Login Two" + Environment.NewLine;
        }

        private void buttonProfile_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Profile();
            //    File.WriteAllText(@"c:\code\profile.json", p);

                richTextBox.Text += "-------------------------" + Environment.NewLine;

                Profile pf = new Profile(p);
                if (pf.IsValid)
                {
                    richTextBox.Text += "Commander " + pf.Commander + " " + pf.ID + Environment.NewLine;
                    richTextBox.Text += "Credits " + pf.Credits + Environment.NewLine;
                    richTextBox.Text += "Combat " + pf.RankCombat + Environment.NewLine;
                    richTextBox.Text += "Explore " + pf.RankExplore + Environment.NewLine;

                    richTextBox.Text += "Starport " + pf.StarPort + Environment.NewLine;
                    var srv = pf.StarPortServices;
                    richTextBox.Text += "Starport Major Faction " + pf.StarPortMajorFaction + Environment.NewLine;
                    richTextBox.Text += "Starport Minor Faction " + pf.StarPortMinorFaction + Environment.NewLine;


                    richTextBox.Text += "Ship " + pf.Ship + Environment.NewLine;
                    richTextBox.Text += "Name " + pf.ShipName + Environment.NewLine;
                    richTextBox.Text += "Ident " + pf.ShipIdent + Environment.NewLine;
                    richTextBox.Text += "Value " + pf.ShipTotalValue + Environment.NewLine;

                    var ml = pf.GetModules();
                    if (ml != null)
                        richTextBox.Text += "Modules " + ml.Count + Environment.NewLine;
                    var sh = pf.GetShips();
                    if (sh != null)
                        richTextBox.Text += "Ships " + sh.Count + Environment.NewLine;
                }
                else
                    richTextBox.Text += "No profile" + Environment.NewLine;

                richTextBox.Select(richTextBox.Text.Length - 1, 1);
                richTextBox.ScrollToCaret();
            }
        }

        private void buttonLogout_Click(object sender, EventArgs e)
        {
            capi.LogOut();
            richTextBox.Text += "Logout" + Environment.NewLine;
        }

        private void buttonMarket_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Market();
              //  File.WriteAllText(@"c:\code\market.json", p);

                richTextBox.Text += "-------------------------" + Environment.NewLine;

                Market mk = new Market(p);

                if (mk.IsValid)
                {
                    richTextBox.Text += mk.Name + " " + mk.ID + " " + mk.Type + Environment.NewLine;
                    var imports = mk.Imports;
                    var exports = mk.Exports;
                    var services = mk.Services;
                    var prohibit = mk.Prohibited;
                    var economies = mk.Economies;
                    var commodities = mk.GetCommodities();
                    if (commodities != null)
                        richTextBox.Text += "Commds " + commodities.Count + Environment.NewLine;
                }
                else
                    richTextBox.Text += "No market data" + Environment.NewLine;

                richTextBox.Select(richTextBox.Text.Length - 1, 1);
                richTextBox.ScrollToCaret();
            }
        }

        private void buttonShipyard_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Shipyard();
              //  File.WriteAllText(@"c:\code\shipyard.json", p);
                richTextBox.Text += "-------------------------" + Environment.NewLine;

                Shipyard sy = new Shipyard(p);
                if (sy.IsValid)
                {
                    richTextBox.Text += sy.Name + " " + sy.ID + " " + sy.Type + Environment.NewLine;
                    var imports = sy.Imports;
                    var exports = sy.Exports;
                    var services = sy.Services;
                    var economies = sy.Economies;
                    var modules = sy.GetModules();
                    if (modules != null)
                        richTextBox.Text += "Modules " + modules.Count + Environment.NewLine;
                }
                else
                    richTextBox.Text += "No Ship data" + Environment.NewLine;

                richTextBox.Select(richTextBox.Text.Length - 1, 1);
                richTextBox.ScrollToCaret();
            }

        }

        private void buttonjournal_Click(object sender, EventArgs e)
        {
            if (capi.Active)
            {
                string p = capi.Journal(dateTimePicker.Value, out System.Net.HttpStatusCode status);

                richTextBox.Text += "-------------------------" + Environment.NewLine;
                richTextBox.Text += "Journal Response " + status +  Environment.NewLine;

                if ( p != null )
                {
                    File.WriteAllText(@"c:\code\journal.json", p);
                }

                richTextBox.Select(richTextBox.Text.Length - 1, 1);
                richTextBox.ScrollToCaret();
            }

        }
    }
}
