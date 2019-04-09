using System;
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

namespace sc2dsstats_rc1
{
    /// <summary>
    /// Interaktionslogik für Win_mmselect.xaml
    /// </summary>
    public partial class Win_mmselect : Window
    {

        MainWindow MW { get; set; }
        Win_mm WM { get; set; }

        public Win_mmselect()
        {
            InitializeComponent();
            ContextMenu dg_games_cm = new ContextMenu();
            MenuItem win_saveas = new MenuItem();
            win_saveas.Header = "Copy result ...";
            win_saveas.Click += new RoutedEventHandler(dg_games_cm_Copy_Click);
            dg_games_cm.Items.Add(win_saveas);
            dg_games.ContextMenu = dg_games_cm;
            dg_games.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(dg_games_DClick);

        }

        public Win_mmselect(MainWindow mw, Win_mm wm)  : this()
        {
            MW = mw;
            WM = wm;
            foreach (dsmmid id in WM.MMIDS.Values)
            {
                mmcb_mmids.Items.Add(id.MMID);
            }
            bt_load_Click(null, null);




        }

        private void bt_scan_Click(object sender, RoutedEventArgs e)
        {
            MW.mnu_Scan(null, null);
        }

        private void bt_load_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Now;
            var yesterday = today.AddDays(-2);
            string sd = yesterday.ToString("yyyyMMdd");
            sd += "000000";
            double sd_int = double.Parse(sd);

            MW.LoadData(MW.myStats_csv);
            List<dsreplay> fil_replays = new List<dsreplay>(MW.replays);
            List<dsreplay> tmprep = new List<dsreplay>();
            tmprep = new List<dsreplay>(fil_replays.Where(x => (x.GAMETIME > sd_int)).ToList());

            tb_rep_mmid.Text = WM.tb_mmid.Text;

            CheckValidRep(tmprep);
            //dg_games.ItemsSource = tmprep;
        }

        public void CheckValidRep (List<dsreplay> reps)
        {

            List<dsreplay> glvalid1 = new List<dsreplay>();
            List<dsreplay> glvalid2 = new List<dsreplay>();
            List<dsmmid> leftover = new List<dsmmid>(WM.MMIDS.Values);

            foreach (dsmmid id in WM.MMIDS.Values)
            {
                if (id.REPORTED == 1) continue;

                List<dsreplay> lvalid1 = new List<dsreplay>();
                List<dsreplay> lvalid2 = new List<dsreplay>();

                
                foreach (dsreplay rep in reps)
                {
                    int valid = 0;
                    foreach (dsplayer plrep in rep.PLAYERS)
                    {
                        foreach (KeyValuePair<int, string> plmm in id.PLAYERS)
                        {
                            if (plmm.Value == plrep.NAME)
                            {
                                valid++;
                            }
                        }
                    }

                    if (valid >= 2)
                    {
                        lvalid2.Add(rep);
                    }

                    if (valid == id.NEED)
                    {
                        lvalid1.Add(rep);
                    }
                }

                // one valid replay found :)
                if (lvalid1.Count == 1)
                {
                    lvalid1.ElementAt(0).MMID = id.MMID.ToString();
                    SendRep(lvalid1.ElementAt(0));
                    leftover.Remove(id);
                }
                else if (lvalid1.Count > 1)
                {
                    dg_games.ItemsSource = lvalid1;
                    glvalid1.AddRange(lvalid1);
                }
                else if (lvalid2.Count == 1)
                {
                    lvalid2.ElementAt(0).MMID = id.MMID.ToString();
                    SendRep(lvalid2.ElementAt(0));
                    leftover.Remove(id);
                }
                else if (lvalid2.Count > 1)
                {
                    dg_games.ItemsSource = lvalid2;
                    glvalid2.AddRange(lvalid2);
                }
            }

            if (WM.MMIDS.Count > 0 && leftover.Count == 0)
            {
                //MessageBox.Show("Report sent - TY!", "sc2dsmm2");
                //this.Close();
            }

            else if (leftover.Count == 1)
            {
                if (glvalid1.Count == 1)
                {
                    SendRep(glvalid1.ElementAt(0));
                }
                else if (glvalid1.Count > 1)
                {
                    dg_games.ItemsSource = glvalid1;
                }
                else if (glvalid2.Count == 1)
                {
                    SendRep(glvalid2.ElementAt(0));
                }
                else if (glvalid2.Count > 1)
                {
                    dg_games.ItemsSource = glvalid2;
                } else
                {
                    dg_games.ItemsSource = reps;
                }

            } else
            {
                dg_games.ItemsSource = reps;
            }
        }

        public void bt_send_Click(object sender, RoutedEventArgs e)
        {
            foreach (var dataItem in dg_games.SelectedItems)
            {
                dsreplay game = dataItem as dsreplay;
                game.MMID = tb_rep_mmid.Text;
                if (int.Parse(game.MMID) > 0)
                {
                    SendRep(game);
                } else
                {
                    MessageBox.Show("No MMID found - you might want to enter it manually in the MMID Textbox)", "sc2dsmm");
                }
            }

        }

        public void SendRep (dsreplay game)
        {
            string result1 = "";
            string result2 = "";
            string result = "unknown";
            if (game != null)
            {
                if (game.REPORTED == 0)
                {
                    int mmid = 0;
                    try
                    {
                        mmid = int.Parse(tb_rep_mmid.Text);
                    } catch { }


                    if (mmid > 0 && WM.MMIDS.Count > 0)
                    {
                        if (WM.MMIDS.ContainsKey(mmid) == true)
                        {
                        }
                        else
                        {
                            dsmmid id = new dsmmid();
                            id.MMID = mmid;
                            WM.MMIDS.Add(mmid, id);
                        }
                    }
                    else
                    {
                        dsmmid id = new dsmmid();
                        id.MMID = mmid;
                        WM.MMIDS.Add(mmid, id);
                    }

                    foreach (dsplayer player in game.PLAYERS)
                    {
                        if (player.POS <= 3)
                        {
                            result1 += "(" + player.NAME + ", " + player.RACE + ", " + player.KILLSUM + ")";
                            if (player.POS != 3)
                            {
                                result1 += ", ";
                            }
                        }
                        else if (player.POS > 3)
                        {
                            result2 += "(" + player.NAME + ", " + player.RACE + ", " + player.KILLSUM + ")";
                            if (player.POS != 6)
                            {
                                result2 += ", ";
                            }

                        }

                        dsplayer plid = new dsplayer();
                        List<dsplayer> ltemp = new List<dsplayer>(WM.MMIDS[mmid].REPORTS.Where(x => x.NAME == player.NAME).ToList());
                        if (ltemp.Count > 0)
                        {
                            plid = ltemp.ElementAt(0);
                            plid.ARMY = player.ARMY;
                            plid.INCOME = player.INCOME;
                            plid.PDURATION = player.PDURATION;
                            plid.POS = player.POS;
                        }
                        else
                        {
                            WM.MMIDS[mmid].REPORTS.Add(player);
                        }

                    }
                    if (game.WINNER == 0)
                    {
                        result = result1 + " vs " + result2;
                    }
                    else if (game.WINNER == 1)
                    {
                        result = result2 + " vs " + result1;
                    }
                    result1 = "";
                    result2 = "";

                    WM.MMIDS[mmid].REPLAY = game;
                    WM.MMIDS[mmid].DURATION = game.DURATION;

                    string smmid = game.MMID;
                    if (smmid == "0") smmid = tb_rep_mmid.Text;
                    string response = WM.SendResult("mmid: " + mmid + "; result: " + result, game);
                    if (response == "sc2dsmm: Result: 0")
                    {

                    }
                    else
                    {
                        this.Close();
                    }
                }
            }
        }

        private void dg_games_DClick(object sender, RoutedEventArgs e)
        {
            List<dsplayer> temp = new List<dsplayer>();
            dsplayer pltemp = new dsplayer();
            foreach (var dataItem in dg_games.SelectedItems)
            {
                //myGame game = dataItem as myGame;
                dsreplay game = dataItem as dsreplay;
                pltemp.RACE = game.REPLAY;
                temp.Add(pltemp);
                foreach (dsplayer pl in game.PLAYERS)
                {
                    temp.Add(pl);
                }

            }

            if (temp.Count > 300)
            {
                pltemp.RACE = "Visibility ilmit is 300. Sorry.";
                List<dsplayer> bab = new List<dsplayer>();
                bab.Add(pltemp);

                dg_player.ItemsSource = bab;
            }
            else
            {
                dg_player.ItemsSource = temp;
            }

            if (temp.Count < 120)
            {
                dg_player.EnableRowVirtualization = false;

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(ProcessRows_player));
            }
            //ProcessRows_player();
        }

        private void ProcessRows_player()
        {

            int itct = dg_player.Items.Count;
            for (int i = 0; i < itct; i++)
            {
                ///var row = dg_player.ItemContainerGenerator.ContainerFromItem(pl) as DataGridRow;

                dsplayer pl = dg_player.Items[i] as dsplayer;

                var row = dg_player.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row != null)
                {
                    if (MW.player_list.Contains(pl.NAME))
                    {
                        row.Background = Brushes.YellowGreen;
                    }
                    else if (pl.NAME == null)
                    {
                        row.Background = Brushes.Azure;
                    }
                    else
                    {
                        row.Background = Brushes.Yellow;
                    }
                }
            }

        }

        private void dg_games_cm_Copy_Click(object sender, RoutedEventArgs e)
        {
            string result1 = "";
            string result2 = "";
            string result = "unknown";

            if (dg_games.SelectedItems.Count > 100)
            {
                MessageBox.Show("Too many replays to handle. We can only handle 100 with this :(", "Failed");
            }
            else
            {

                foreach (dsreplay game in dg_games.SelectedItems)
                {
                    if (game != null)
                    {

                        foreach (dsplayer player in game.PLAYERS)
                        {
                            if (player.POS <= 3)
                            {
                                result1 += "(" + player.NAME + ", " + player.RACE + ", " + player.KILLSUM + ")";
                                if (player.POS != 3)
                                {
                                    result1 += ", ";
                                }
                            }
                            else if (player.POS > 3)
                            {
                                result2 += "(" + player.NAME + ", " + player.RACE + ", " + player.KILLSUM + ")";
                                if (player.POS != 6)
                                {
                                    result2 += ", ";
                                }

                            }
                        }
                    }

                    if (game.WINNER == 0)
                    {
                        result = result1 + " vs " + result2;
                    }
                    else if (game.WINNER == 1)
                    {
                        result = result2 + " vs " + result1;
                    }
                    result1 = "";
                    result2 = "";

                }
                Clipboard.SetText(result);
                MessageBox.Show(result, "Sent to clipboard");
            }
        }

        private void bt_norep_Click(object sender, RoutedEventArgs e)
        {
            Win_norep norep = new Win_norep(WM, tb_rep_mmid.Text);
            norep.Show();
        }

        private void Mmcb_mmids_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                tb_rep_mmid.Text = mmcb_mmids.SelectedItem.ToString();
            } catch { }
        }
    }
}
