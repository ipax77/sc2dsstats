using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace sc2dsstats_rc1
{
    /// <summary>
    /// Interaktionslogik für Win_regex.xaml
    /// </summary>
    /// 

    public partial class Win_regex: Window
    {
        MainWindow mw = new MainWindow();
        public Hashtable hash = new Hashtable()
            {
                { "ID", 1 },
                { "REPLAY", 1 },
                { "GAMETIME", 1 },
                { "WINNER", 1 },
                { "DURATION", 1 },
                { "MAXLEAVER", 1 },
                { "MINKILLSUM", 1 },
                { "MININCOME", 1 },
                { "MINARMY", 1 },
                { "RACE", 1 },
                { "MATCHUP", 1 },
                { "MATCHUP PLAYER", 1}
            };
        public string player_name = "";
        public List<string> player_list = new List<string>();
        List<myReplay> replays = new List<myReplay>();
        List<cmdr_data> cmdrs = new List<cmdr_data>();
        public List<myGame> games = new List<myGame>();
        List<gamePlayer> players = new List<gamePlayer>();
        dsdbfilter dbfil = null;
        List<dsreplay> fil_games = new List<dsreplay>();
        public ObservableCollection<string> DSDBITEMS { get; set; }
        public string DSDBTEXT { get; set; }
        public Win_mm WMM { get; set; }
        public Grid[] mygrids { get; set; }
        public DataGrid[] mydatagrids { get; set; }

        public Win_regex()
        {
            InitializeComponent();
            WMM = new Win_mm(mw);

            mygrids =  new Grid[] {
                gr_database_units1,
                gr_database_units2,
                gr_database_units3,
                gr_database_units4,
                gr_database_units5,
                gr_database_units6
            };
            mydatagrids = new DataGrid[]
            {
                dg_units1,
                dg_units2,
                dg_units3,
                dg_units4,
                dg_units5,
                dg_units6
            };

            player_name = Properties.Settings.Default.PLAYER;
            if (player_name.Contains(";"))
            {
                player_name = string.Concat(player_name.Where(c => !char.IsWhiteSpace(c)));
                if (player_name.EndsWith(";")) player_name = player_name.Remove(player_name.Length - 1);
                player_list = player_name.Split(';').ToList();
            }
            else
            {
                player_list.Add(player_name);
            }

            ContextMenu dg_games_cm = new ContextMenu();
            MenuItem win_saveas = new MenuItem();
            win_saveas.Header = "Copy result ...";
            win_saveas.Click += new RoutedEventHandler(dg_games_cm_Copy_Click);
            dg_games_cm.Items.Add(win_saveas);
            dg_games.ContextMenu = dg_games_cm;
            dg_games.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(dg_games_DClick);

            dg_player.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(Dg_player_MouseDoubleClick);

            dbfil = new dsdbfilter(this, mw);

            tb_filter.KeyUp += new System.Windows.Input.KeyEventHandler(tb_filter_KeyDown);

            DSDBITEMS = new ObservableCollection<string>(mw.s_races);

            //tb_filter.SetBinding(DSDBITEMS);


            foreach (var bab in hash.Keys.Cast<String>().OrderBy(c => c))
            {
                cb_filter.Items.Add(bab);
            }
            cb_filter.SelectedItem = cb_filter.Items[4];
            
        }

        public class myReplay
        {
            /**
             <DataGridTextColumn Header = "PLAYERID"  Binding="{Binding PLAYERID}" Width="120" IsReadOnly="True"/>
            <DataGridTextColumn Header = "RACE"  Binding="{Binding RACE}" Width="120" IsReadOnly="True"/>
            <DataGridTextColumn Header = "TEAM"  Binding="{Binding TEAM}" Width="120" IsReadOnly="True"/>
            <DataGridTextColumn Header = "RESULT"  Binding="{Binding RESULT}" Width="120" IsReadOnly="True"/>
            <DataGridTextColumn Header = "INCOME"  Binding="{Binding INCOME}" Width="120" IsReadOnly="True"/>
            <DataGridTextColumn Header = "ARMY"  Binding="{Binding ARMY}" Width="120" IsReadOnly="True"/>
            <DataGridTextColumn Header = "KILLSUM"  Binding="{Binding KILLSUM}" Width="120" IsReadOnly="True"/>
            <DataGridTextColumn Header = "DURATION"  Binding="{Binding DURATION}" Width="120" IsReadOnly="True"/>
            <DataGridTextColumn Header = "GAMETIME"  Binding="{Binding GAMETIME}" Width="120" IsReadOnly="True"/>
            **/
            public int ID { get; set; }
            public int PLAYERID { get; set; }
            public string REPLAY { get; set; }
            public string NAME { get; set; }
            public string RACE { get; set; }
            public int TEAM { get; set; }
            public int RESULT { get; set; }
            public double INCOME { get; set; }
            public int ARMY { get; set; }
            public int KILLSUM { get; set; }
            public int DURATION { get; set; }
            public double GAMETIME { get; set; }
        }


        public class myGame : IEquatable<myGame>
        {
            public int ID { get; set; }
            public string REPLAY { get; set; }
            public double GAMETIME { get; set; }
            public int WINNER { get; set; }
            public int DURATION { get; set; }
            public List<gamePlayer> PLAYERS { get; set; }
            public List<string> RACES { get; set; }
            public int MINKILLSUM { get; set; }
            public int MINARMY { get; set; }
            public double MININCOME { get; set; }
            public int MAXLEAVER { get; set; }
            public int PLAYERCOUNT { get; set; }

            public myGame()
            {

            }

            public myGame (List<gamePlayer> player)
            {
                PLAYERS = player;
            }

            public override string ToString()
            {
                return "ID: " + ID + "   Name: " + REPLAY;
            }
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                myGame objAsPart = obj as myGame;
                if (objAsPart == null) return false;
                else return Equals(objAsPart);
            }
            public override int GetHashCode()
            {
                return ID;
            }
            public bool Equals(myGame other)
            {
                if (other == null) return false;
                return (this.ID.Equals(other.ID));
            }

        }

        public class gamePlayer : myGame
        {
            public int POS { get; set; }
            public string RACE { get; set; }
            public int TEAM { get; set; }
            public int KILLSUM { get; set; }
            public double INCOME { get; set; }
            public int PDURATION { get; set; }
            public string NAME { get; set; }
            public int ARMY { get; set; }
            public int RESULT { get; set; }
            
        }

        public class cmdr_data
        {
            public string RACE { get; set; }
            public int GWIN { get; set; }
            public int GGAMES { get; set; }
            public int PWIN { get; set; }
            public int PGAMES { get; set; }
            public int GMVP { get; set; }
            public int PMVP { get; set; }
            public double GDPS_ARMY { get; set; }
            public double GDPS_INCOME { get; set; }
            public double GDPS_DURATION { get; set; }
            public double PDPS_ARMY { get; set; }
            public double PDPS_INCOME { get; set; }
            public double PDPS_DURATION { get; set; }
            public List<cmdr_data> VS { get; set; }
            public Hashtable MATCHUP { get; set; }

            public cmdr_data()
            {
                this.GGAMES = 0;
                this.PGAMES = 0;
                this.GWIN = 0;
                this.PWIN = 0;
                this.GMVP = 0;
                this.PMVP = 0;
                this.VS = null;
                this.RACE = "";
            }

            public cmdr_data(List<cmdr_data> cmdrs_vs)
            {
                VS = cmdrs_vs;
            }

            public double GetGDPS_ARMY()
            {
                if (this.GGAMES == 0) return 0;
                double dps = this.GDPS_ARMY * 100 / this.GGAMES;
                return dps;
            }

            public double GetGDPS_INCOME()
            {
                if (this.GGAMES == 0) return 0;
                double dps = this.GDPS_INCOME * 100 / this.GGAMES;
                return dps;
            }
            public double GetGDPS_DURATION()
            {
                if (this.GGAMES == 0) return 0;
                double dps = this.GDPS_DURATION * 100 / this.GGAMES;
                return dps;
            }
            public double GetPDPS_ARMY()
            {
                if (this.GGAMES == 0) return 0;
                double dps = this.PDPS_ARMY * 100 / this.GGAMES;
                return dps;
            }
            public double GetPDPS_INCOME()
            {
                if (this.GGAMES == 0) return 0;
                double dps = this.PDPS_INCOME * 100 / this.GGAMES;
                return dps;
            }
            public double GetPDPS_DURATION()
            {
                if (this.GGAMES == 0) return 0;
                double dps = this.PDPS_DURATION * 100 / this.GGAMES;
                return dps;
            }

            public double GetGWR()
            {
                if (this.GGAMES == 0) return 0;
                double wr = this.GWIN * 100 / this.GGAMES;
                wr = Math.Round(wr, 2);
                return wr;
            }

            public double GetPWR()
            {
                if (this.PGAMES == 0) return 0;
                double wr = this.PWIN * 100 / this.PGAMES;
                wr = Math.Round(wr, 2);
                return wr;
            }

            public double GetGMVP()
            {
                if (this.GGAMES == 0) return 0;
                double wr = this.GMVP * 100 / this.GGAMES;
                wr = Math.Round(wr, 2);
                return wr;
            }

            public double GetPMVP()
            {
                if (this.PGAMES == 0) return 0;
                double wr = this.PMVP * 100 / this.PGAMES;
                wr = Math.Round(wr, 2);
                return wr;
            }

            public double GetVSWR(string cmdr, int player)
            {
                double wr = 0;
                cmdr_data mycmdr = new cmdr_data();
                mycmdr = this.VS.Find(x => x.RACE == cmdr);
                if (player == 0)
                {
                    wr = mycmdr.GetGWR();
                } else if (player == 1)
                {
                    wr = mycmdr.GetPWR();
                }
                return wr;
            }

            public double GetVSMVP(string cmdr, int player)
            {
                double wr = 0;
                cmdr_data mycmdr = new cmdr_data();
                mycmdr = this.VS.Find(x => x.RACE == cmdr);
                if (player == 0)
                {
                    wr = mycmdr.GetGMVP();
                }
                else if (player == 1)
                {
                    wr = mycmdr.GetPMVP();
                }
                return wr;
            }

        }

        public List<dsreplay> LoadCollectionData()
        {
            List<dsreplay> rep_filtered = new List<dsreplay>();

            return rep_filtered;
        }

        public List<myGame> LoadCollectionData_deprecated()
        {

            string csv = mw.myStats_csv;
            string line;
            ///string pattern = @"^(\d+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+);";
            string[] myline = new string[12];
            
            string id = null;
            char[] cTrim = { ' ' };
            List<myReplay> single_replays = new List<myReplay>();

            if (File.Exists(csv))
            {

                System.IO.StreamReader file_c = new System.IO.StreamReader(csv);
                int i = 0;
                while (file_c.ReadLine() != null) { i++; ; }
                int j = 0;
                System.IO.StreamReader file = new System.IO.StreamReader(csv);
                while ((line = file.ReadLine()) != null)
                {
                    j++;
                    myline = line.Split(';');

                    for (int k = 0; k <= 12; k++)
                    {
                        string result = myline[k].Trim(cTrim);
                        myline[k] = result;
                    }

                    myReplay rep = new myReplay()
                    {
                        ID = int.Parse(myline[0]),
                        REPLAY = myline[1],
                        NAME = myline[2],
                        RACE = myline[4],
                        TEAM = int.Parse(myline[5]),
                        RESULT = int.Parse(myline[6]),
                        KILLSUM = int.Parse(myline[7]),
                        DURATION = int.Parse(myline[8]),
                        GAMETIME = double.Parse(myline[9]),
                        PLAYERID = int.Parse(myline[10]),
                        INCOME = double.Parse(myline[11], CultureInfo.InvariantCulture),
                        ARMY = int.Parse(myline[12])
                    };
                    replays.Add(rep);

                    if (id == null)
                    {
                        id = rep.REPLAY;
                    }

                    if (String.Equals(id, rep.REPLAY))
                    {
                        single_replays.Add(rep);
                    }
                    else
                    {
                        collectData(single_replays);
                        id = rep.REPLAY;
                        single_replays.Clear();
                        single_replays.Add(rep);
                    }

                    if (j == i)
                    {
                        collectData(single_replays);
                    }
                    /**
                    foreach (Match m in Regex.Matches(line, pattern))
                    {
                        string value1 = m.Groups[2].ToString() + ".SC2Replay";


                    }
                    **/
                    ///if (i > 2000)
                    ///break;

                }

                file.Close();
            } else
            {
                MessageBox.Show("No data found :(", "sc2dsstats");
            }


        

  


        
            return games;
        }

        public void collectData(List<myReplay> single_replays)
        {
            myGame game = new myGame();
            gamePlayer player = new gamePlayer();
            List<gamePlayer> gameplayer = new List<gamePlayer>();

            foreach (myReplay srep in single_replays)
            {
                if (String.Equals(srep.NAME, player_name))
                {

                    game.ID = srep.ID;
                    game.REPLAY = srep.REPLAY;
                    game.GAMETIME = srep.GAMETIME;

                    player.POS = srep.PLAYERID;
                    player.RACE = srep.RACE;
                    player.NAME = srep.NAME;
                    player.KILLSUM = srep.KILLSUM;
                    player.PDURATION = srep.DURATION;
                    player.INCOME = srep.INCOME;
                    player.ARMY = srep.ARMY;
                    player.RESULT = 2;
                    player.REPLAY = srep.REPLAY;
                    player.ID = srep.ID;

                    game.DURATION = srep.DURATION;
                    int result = srep.RESULT;
                    if (srep.PLAYERID <= 3)
                    {
                        player.TEAM = 0;
                        if (srep.RESULT == 1)
                        {
                            player.RESULT = 1;
                            game.WINNER = 0;
                        }
                        else
                        {
                            game.WINNER = 1;
                        }
                    }
                    else if (srep.PLAYERID > 3)
                    {
                        player.TEAM = 1;
                        if (srep.RESULT == 1)
                        {
                            player.RESULT = 1;
                            game.WINNER = 1;
                        }
                        else
                        {
                            game.WINNER = 0;
                        }
                    }
                }
                else
                {

                }


            }

            int minkillsum = 0;
            int minarmy = 0;
            double minincome = 0;
            int maxleaver = 0;
            List<string> races = new List<string>();

            foreach (myReplay srep in single_replays)
            {

                game.PLAYERCOUNT++;

                if (minkillsum == 0)
                {
                    minkillsum = srep.KILLSUM;
                } else
                {
                    if (srep.KILLSUM < minkillsum)
                    {
                        minkillsum = srep.KILLSUM;
                    }
                }

                if (minincome == 0)
                {
                    minincome = srep.INCOME;
                }
                else
                {
                    if (srep.INCOME < minincome)
                    {
                        minincome = srep.INCOME;
                    }
                }

                if (minarmy == 0)
                {
                    minarmy = srep.ARMY;
                }
                else
                {
                    if (srep.ARMY < minarmy)
                    {
                        minarmy = srep.ARMY;
                    }
                }

                int leaver = game.DURATION - srep.DURATION;

                if (maxleaver == 0)
                {
                    maxleaver = leaver;
                } else
                {
                    if (leaver > maxleaver)
                    {
                        maxleaver = leaver;
                    }
                }

                races.Add(srep.RACE);

                if (String.Equals(srep.NAME, player_name))
                {
                    gameplayer.Add(player);
                    players.Add(player);
                }
                else
                {
                    gamePlayer mplayer = new gamePlayer();
                    mplayer.POS = srep.PLAYERID;
                    mplayer.RACE = srep.RACE;
                    mplayer.NAME = srep.NAME;
                    mplayer.KILLSUM = srep.KILLSUM;
                    mplayer.PDURATION = srep.DURATION;
                    mplayer.INCOME = srep.INCOME;
                    mplayer.ARMY = srep.ARMY;
                    mplayer.REPLAY = srep.REPLAY;
                    mplayer.ID = srep.ID;
                    mplayer.RESULT = 2;
                    if (srep.PLAYERID <= 3)
                    {
                        mplayer.TEAM = 0;
                        if (game.WINNER == 0)
                        {
                            mplayer.RESULT = 1;
                        }
                    }
                    else if (srep.PLAYERID > 3)
                    {
                        mplayer.TEAM = 1;
                        if (game.WINNER == 1)
                        {
                            mplayer.RESULT = 1;
                        }
                    }

                    gameplayer.Add(mplayer);
                    players.Add(mplayer);
                }
            }

            game.MAXLEAVER = maxleaver;
            game.MINARMY = minarmy;
            game.MININCOME = minincome;
            game.MINKILLSUM = minkillsum;
            game.RACES = new List<string>(races);
            game.PLAYERS = new List<gamePlayer>(gameplayer);
            games.Add(game);

            
            gameplayer.Clear();

            
        }

        public void ReadCSV()
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            /**
            if (cb_killsum.IsChecked == false)
            {
                dg_win_regex.Columns[9].Visibility = Visibility.Hidden;
            }
            dg_win_regex.ItemsSource = LoadCollectionData();
            **/
            games.Clear();
            //LoadCollectionData();
            mw.replays = mw.LoadData(mw.myStats_csv);
            //dg_games.ItemsSource = games;
            dg_games.ItemsSource = mw.replays;
            lb_filter.Content = "Games: " + mw.replays.Count();

            ///Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(ProcessRows));

        }


        private void bt_filter_Click(object sender, RoutedEventArgs e)
        {
            dg_games.ItemsSource = dbfil.OTF_Filter();
        }

        private void tb_filter_KeyDown(object sender, RoutedEventArgs e)
        {
            if (tb_filter.Text.Length >= 3 && !cb_filter.SelectedItem.ToString().StartsWith("MATCHUP"))
            {
                fil_games.Clear();
                fil_games = dbfil.OTF_Filter();
                dg_games.ItemsSource = fil_games;
            }
        }

        private void dg_games_DClick(object sender, RoutedEventArgs e)
        {
            List<dsplayer> temp = new List<dsplayer>();
            Dictionary<string, int> units = new Dictionary<string, int>();
            List<rxunit> rxunits = new List<rxunit>();
            dsplayer pltemp = new dsplayer();
            dsreplay replay = new dsreplay();
            foreach (var dataItem in dg_games.SelectedItems)
            {
                //myGame game = dataItem as myGame;
                dsreplay game = dataItem as dsreplay;
                if (replay.ID == 0) replay = game;
                pltemp.RACE = game.REPLAY;
                temp.Add(pltemp);
                foreach (dsplayer pl in game.PLAYERS)
                {
                    temp.Add(pl);
                    if (pl.UNITS.ContainsKey("ALL")) {
                        foreach (string unit in pl.UNITS["ALL"].Keys)
                        {
                            if (units.ContainsKey(unit)) units[unit] += pl.UNITS["ALL"][unit];
                            else units.Add(unit, pl.UNITS["ALL"][unit]);
                        }
                    }
                }
                
            }
            foreach (string unit in units.Keys)
            {
                rxunits.Add(new rxunit(unit, units[unit]));
            }

            dg_units.ItemsSource = rxunits.OrderByDescending(o => o.COUNT);

            if (temp.Count > 300)
            {
                pltemp.RACE = "Visibility limit is 300. Sorry.";
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
            if (replay.ID != 0)
            {
                dsmmid mmid = new dsmmid();
                if (!WMM.MMIDS.ContainsKey(replay.ID))
                {

                    mmid.REPLAY = replay;
                    mmid.DURATION = replay.DURATION;
                    mmid.REPORTS = replay.PLAYERS;
                    WMM.MMIDS.Add(replay.ID, mmid);
                }
                WMM.PresentResult(replay.ID);
                try
                {
                    WMM.Show();
                } catch { }
            }

            foreach (var ent in mygrids)
            {
                ent.Visibility = Visibility.Collapsed;
            }

        }

        private void Dg_player_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Dictionary<string, int> units = new Dictionary<string, int>();
            List<rxunit> rxunits = new List<rxunit>();

            foreach (var dataItem in dg_player.SelectedItems)
            {
                dsplayer pl = new dsplayer();
                pl = dataItem as dsplayer;
                if (pl.UNITS.ContainsKey("ALL"))
                {
                    List<rxunit> rxunits_pl = new List<rxunit>();
                    foreach (string unit in pl.UNITS["ALL"].Keys)
                    {
                        if (units.ContainsKey(unit)) units[unit] += pl.UNITS["ALL"][unit];
                        else units.Add(unit, pl.UNITS["ALL"][unit]);

                        rxunits_pl.Add(new rxunit(unit, pl.UNITS["ALL"][unit]));
                    }
                    ShowUnits(pl, rxunits_pl);
                }
            }
            foreach (string unit in units.Keys)
            {
                rxunits.Add(new rxunit(unit, units[unit]));
            }

            dg_units.ItemsSource = rxunits.OrderByDescending(o => o.COUNT);

        }

        public void ShowUnits(dsplayer pl, List<rxunit> units)
        {
            Grid mygrid = new Grid();
            DataGrid mydatagrid = new DataGrid();
            mygrid = mygrids[pl.REALPOS - 1];
            mydatagrid = mydatagrids[pl.REALPOS - 1];
            mydatagrid.ItemsSource = units.OrderByDescending(o => o.COUNT);
            mygrid.Visibility = Visibility.Visible;
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
                    if (player_list.Contains(pl.NAME))
                    {
                        row.Background = Brushes.YellowGreen;
                    }
                    else if (pl.NAME == null)
                    {
                        row.Background = Brushes.Azure;
                    } else 
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
                            if (player.POS <= game.PLAYERCOUNT / 2)
                            {
                                result1 += "(" + player.NAME + ", " + player.RACE + ", " + player.KILLSUM + ")";
                                if (player.POS != 3)
                                {
                                    result1 += ", ";
                                }
                            }
                            else if (player.POS > game.PLAYERCOUNT / 2)
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

        

        private void cb_POS_Click(object sender, RoutedEventArgs e)
        {
            if (cb_pl_POS.IsChecked == false)
            {
                dg_player.Columns[0].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_player.Columns[0].Visibility = Visibility.Visible;
            }
        }
        private void cb_NAME_Click(object sender, RoutedEventArgs e)
        {
            if (cb_pl_NAME.IsChecked == false)
            {
                dg_player.Columns[1].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_player.Columns[1].Visibility = Visibility.Visible;
            }
        }
        private void cb_RACE_Click(object sender, RoutedEventArgs e)
        {
            if (cb_pl_RACE.IsChecked == false)
            {
                dg_player.Columns[2].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_player.Columns[2].Visibility = Visibility.Visible;
            }
        }
        private void cb_TEAM_Click(object sender, RoutedEventArgs e)
        {
            if (cb_pl_TEAM.IsChecked == false)
            {
                dg_player.Columns[3].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_player.Columns[3].Visibility = Visibility.Visible;
            }
        }
        private void cb_KILLSUM_Click(object sender, RoutedEventArgs e)
        {
            if (cb_pl_KILLSUM.IsChecked == false)
            {
                dg_player.Columns[4].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_player.Columns[4].Visibility = Visibility.Visible;
            }
        }
        private void cb_ARMY_Click(object sender, RoutedEventArgs e)
        {
            if (cb_pl_ARMY.IsChecked == false)
            {
                dg_player.Columns[5].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_player.Columns[5].Visibility = Visibility.Visible;
            }
        }
        private void cb_INCOME_Click(object sender, RoutedEventArgs e)
        {
            if (cb_pl_INCOME.IsChecked == false)
            {
                dg_player.Columns[6].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_player.Columns[6].Visibility = Visibility.Visible;
            }
        }
        private void cb_DURATION_Click(object sender, RoutedEventArgs e)
        {
            if (cb_pl_DURATION.IsChecked == false)
            {
                dg_player.Columns[7].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_player.Columns[7].Visibility = Visibility.Visible;
            }
        }
        private void cb_RESULT_Click(object sender, RoutedEventArgs e)
        {
            if (cb_pl_RESULT.IsChecked == false)
            {
                dg_player.Columns[8].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_player.Columns[8].Visibility = Visibility.Visible;
            }
        }

        private void cb_ga_ID_Click(object sender, RoutedEventArgs e)
        {
            if (cb_ga_ID.IsChecked == false)
            {
                dg_games.Columns[0].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_games.Columns[0].Visibility = Visibility.Visible;
            }
        }

        private void cb_ga_REPLAY_Click(object sender, RoutedEventArgs e)
        {
            if (cb_ga_REPLAY.IsChecked == false)
            {
                dg_games.Columns[1].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_games.Columns[1].Visibility = Visibility.Visible;
            }
        }

        private void cb_ga_GAMETIME_Click(object sender, RoutedEventArgs e)
        {
            if (cb_ga_GAMETIME.IsChecked == false)
            {
                dg_games.Columns[2].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_games.Columns[2].Visibility = Visibility.Visible;
            }
        }

        private void cb_ga_WINTEAM_Click(object sender, RoutedEventArgs e)
        {
            if (cb_ga_WINTEAM.IsChecked == false)
            {
                dg_games.Columns[3].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_games.Columns[3].Visibility = Visibility.Visible;
            }
        }

        private void cb_ga_DURATION_Click(object sender, RoutedEventArgs e)
        {
            if (cb_ga_DURATION.IsChecked == false)
            {
                dg_games.Columns[4].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_games.Columns[4].Visibility = Visibility.Visible;
            }
        }

        private void cb_ga_MAXLEAVER_Click(object sender, RoutedEventArgs e)
        {
            if (cb_ga_MAXLEAVER.IsChecked == false)
            {
                dg_games.Columns[5].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_games.Columns[5].Visibility = Visibility.Visible;
            }
        }

        private void cb_ga_MINKILLSUM_Click(object sender, RoutedEventArgs e)
        {
            if (cb_ga_MINKILLSUM.IsChecked == false)
            {
                dg_games.Columns[6].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_games.Columns[6].Visibility = Visibility.Visible;
            }
        }

        private void cb_ga_MININCOME_Click(object sender, RoutedEventArgs e)
        {
            if (cb_ga_MININCOME.IsChecked == false)
            {
                dg_games.Columns[7].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_games.Columns[7].Visibility = Visibility.Visible;
            }
        }

        private void cb_ga_MINARMY_Click(object sender, RoutedEventArgs e)
        {
            if (cb_ga_MINARMY.IsChecked == false)
            {
                dg_games.Columns[8].Visibility = Visibility.Hidden;
            }
            else
            {
                dg_games.Columns[8].Visibility = Visibility.Visible;
            }
        }

        private void Dg_win_regex_CleanUpVirtualizedItem(object sender, CleanUpVirtualizedItemEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(ProcessRows_player));
        }


    }

    public class rxunit
    {
        public string UNIT { get; set; }
        public int COUNT { get; set; }

        public rxunit ()
        {

        }
        public rxunit(string unit, int count) : this()
        {
            UNIT = unit;
            COUNT = count;
        }
    }
}

