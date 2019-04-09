using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;

namespace sc2dsstats_rc1
{
    /// <summary>
    /// Interaktionslogik für Win_ladder.xaml
    /// </summary>
    public partial class Win_ladder : Window
    {

        MainWindow MW { get; set; }

        public Win_ladder()
        {
            InitializeComponent();
        }

        public Win_ladder(MainWindow mw) : this()
        {
            MW = mw;
        }

        private void bt_save_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            /// MessageBox.Show("Thank you. Now we need to know where the SC2Replays are - please select one Replay in your folder. Usually it is something like C:\\Users\\<username>\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer");
            string filename = "";
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".txt";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();



            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                filename = dlg.FileName;
                File.WriteAllText(filename, tb_hist.Text);
            }
        }

        private void bt_load_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            /// MessageBox.Show("Thank you. Now we need to know where the SC2Replays are - please select one Replay in your folder. Usually it is something like C:\\Users\\<username>\\Documents\\StarCraft II\\Accounts\\107095918\\2-S2-1-226401\\Replays\\Multiplayer");
            string filename = "";
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".txt";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();



            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                filename = dlg.FileName;
                if (File.Exists(filename))
                {
                    StreamReader sr = new StreamReader(filename, System.Text.Encoding.UTF8, true);
                    tb_hist.Text = sr.ReadToEnd();
                }
            }

        }

        private void bt_gen_Click(object sender, RoutedEventArgs e)
        {
            Win_mm wm = new Win_mm(MW);
            string player = MW.player_list.ElementAt(0);

            for (int i = 0; i < tb_hist.LineCount; i++)
            {
                if (tb_hist.GetLineText(i).StartsWith("(")) {
                    string rep = "mmid: " + (i + 1).ToString() + "; result: " + tb_hist.GetLineText(i);
                    wm.SendResult(rep, null);
                    //Thread.Sleep(20);
                }
            }

            //Thread.Sleep(20);
            string players = tb_player.Text;
            string matchup = wm.SendResult("Matchup: " + players, null);
            tb_best.Text = matchup;

            string ladder = wm.SendResult("Ladder: 0", null);

            if (ladder != null && ladder.Length > 0)
            {
                PresentLadder(ladder);
            }
            wm.Show();
        }

        public void PresentLadder (string ladder)
        {
            List<dsmmladder> llist = new List<dsmmladder>();
            foreach (string pos in ladder.Split(';')) {
                string pattern = @"pos(\d+): (.*)";
                foreach (Match m in Regex.Matches(pos, pattern))
                {
                    string i = m.Groups[1].Value.ToString();
                    int j = 0;
                    dsmmladder lad = new dsmmladder();
                    lad.POS = int.Parse(i);
                    foreach (string ent in m.Groups[2].Value.Split(','))
                    {
                        j++;
                        
                        if (j == 1) lad.NAME = ent;
                        try
                        {
                            if (j == 2) lad.GAMES = int.Parse(ent);
                            if (j == 3) lad.EXP = double.Parse(ent, new CultureInfo("en-US"));
                            if (j == 4) lad.ELO = double.Parse(ent, new CultureInfo("en-US"));
                            if (j == 5) lad.SIGMA = double.Parse(ent, new CultureInfo("en-US"));

                        }catch { }
                        
                    }
                    llist.Add(lad);
                }
                
            }
            dg_ladder.ItemsSource = llist;
        }
    }
}
