﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using sc2dsstats_t1;

namespace sc2dsstats
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
                { "PLAYERID", 1 },
                { "REPLAY", 1 },
                { "NAME", 1 },
                { "RACE", 1 },
                { "TEAM", 1 },
                { "RESULT", 1 },
                { "INCOME", 1 },
                { "ARMY", 1 },
                { "KILLSUM", 1 },
                { "DURATION", 1 },
                { "GAMETIME", 1 }
            };
        public string player_name = "";
        List<myReplay> replays = new List<myReplay>();
        List<cmdr_data> cmdrs = new List<cmdr_data>();
        List<myGame> games = new List<myGame>();
        List<gamePlayer> players = new List<gamePlayer>();

        public Win_regex()
        {
            InitializeComponent();
            var appSettings = ConfigurationManager.AppSettings;
            player_name = appSettings["PLAYER"];

            ContextMenu dg_games_cm = new ContextMenu();
            MenuItem win_saveas = new MenuItem();
            win_saveas.Header = "Copy result ...";
            win_saveas.Click += new RoutedEventHandler(dg_games_cm_Copy_Click);
            dg_games_cm.Items.Add(win_saveas);
            dg_games.ContextMenu = dg_games_cm;
            dg_games.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(dg_games_DClick);
            
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
            public string TEAM { get; set; }
            public string RESULT { get; set; }
            public string INCOME { get; set; }
            public string ARMY { get; set; }
            public string KILLSUM { get; set; }
            public string DURATION { get; set; }
            public string GAMETIME { get; set; }
        }


        public class myGame : IEquatable<myGame>
        {
            public int ID { get; set; }
            public string REPLAY { get; set; }
            public string GAMETIME { get; set; }
            public int WINNER { get; set; }
            public int DURATION { get; set; }
            public List<gamePlayer> PLAYERS { get; set; }

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
            public string KILLSUM { get; set; }
            public string INCOME { get; set; }
            public string PDURATION { get; set; }
            public string NAME { get; set; }
            public string ARMY { get; set; }
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

        }


        private List<myReplay> LoadCollectionData()
        {

            string csv = mw.GetmyVAR("myStats_csv");
            string line;
            ///string pattern = @"^(\d+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+); ([^;]+);";
            string[] myline = new string[12];
            
            string id = null;
            char[] cTrim = { ' ' };
            List<myReplay> single_replays = new List<myReplay>();
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
                    ID = Int32.Parse(myline[0]),
                    REPLAY = myline[1],
                    NAME = myline[2],
                    RACE = myline[4],
                    TEAM = myline[5],
                    RESULT = myline[6],
                    KILLSUM = myline[7],
                    DURATION = myline[8],
                    GAMETIME = myline[9],
                    PLAYERID = int.Parse(myline[10]),
                    INCOME = myline[11],
                    ARMY = myline[12],
                };
                replays.Add(rep);

                if (id == null)
                {
                    id = rep.REPLAY;
                }

                if (String.Equals(id, rep.REPLAY))
                {
                    single_replays.Add(rep);
                } else
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


        

  


        
            return replays;
        }

        public void collectData(List<myReplay> single_replays)
        {
            myGame game = new myGame();
            cmdr_data cmdr = new cmdr_data();
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

                    game.DURATION = int.Parse(srep.DURATION);
                    int result = int.Parse(srep.RESULT);
                    if (srep.PLAYERID <= 3)
                    {
                        player.TEAM = 0;
                        if (int.Parse(srep.RESULT) == 1)
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
                        if (int.Parse(srep.RESULT) == 1)
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


            foreach (myReplay srep in single_replays)
            {
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
            LoadCollectionData();
            dg_games.ItemsSource = games;

            ///Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(ProcessRows));

        }

        private void dg_games_DClick(object sender, RoutedEventArgs e)
        {
            List<gamePlayer> temp = new List<gamePlayer>();
            gamePlayer pltemp = new gamePlayer();
            foreach (var dataItem in dg_games.SelectedItems)
            {
                myGame game = dataItem as myGame;
                pltemp.RACE = game.REPLAY;
                temp.Add(pltemp);
                foreach (gamePlayer pl in game.PLAYERS)
                {
                    temp.Add(pl);
                }
                
            }

            if (temp.Count > 300)
            {
                pltemp.RACE = "Visibility ilmit is 300. Sorry.";
                List<gamePlayer> bab = new List<gamePlayer>();
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

                gamePlayer pl = dg_player.Items[i] as gamePlayer;

                var row = dg_player.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row != null)
                {
                    if (pl.NAME == "PAX")
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


        private void ProcessRows()
        {
            foreach (var dataItem in dg_win_regex.ItemsSource)
            {

                myReplay rep = dataItem as myReplay;
                if (String.Equals(rep.NAME, "PAX"))
                {
                    DataGridRow gridRow = dg_win_regex.ItemContainerGenerator.ContainerFromItem(dataItem) as DataGridRow;
                    if (gridRow != null)
                    {
                        gridRow.Background = Brushes.YellowGreen;
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

                foreach (myGame game in dg_games.SelectedItems)
                {
                    if (game != null)
                    {

                        foreach (gamePlayer player in game.PLAYERS)
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
  

        private void Dg_win_regex_CleanUpVirtualizedItem(object sender, CleanUpVirtualizedItemEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(ProcessRows_player));
        }
    }
}

