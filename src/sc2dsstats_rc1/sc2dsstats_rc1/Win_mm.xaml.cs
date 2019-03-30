using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace sc2dsstats_rc1
{
    /// <summary>
    /// Interaktionslogik für Win_mm.xaml
    /// </summary>
    public partial class Win_mm : Window
    {

        MainWindow MW { get; set; }
        public Socket CLIENT { get; set; }
        SoundPlayer SP { get; set; }
        public bool ACCEPTED { get; set; }
        public bool GAME_READY { get; set; }

        public DispatcherTimer _timer;
        public TimeSpan _time;
        public int downtime = 0;
        public bool doit = true;
        

        public Win_mm()
        {
            InitializeComponent();
            tb_mmid.Text = "6175";
            if (Properties.Settings.Default.MM_CREDENTIAL == true)
            {
                mmcb_credential.IsChecked = true;
            }
            else
            {
                mmcb_credential.IsChecked = false;
            }


            if (Properties.Settings.Default.MM_Server == "NA")
            {
                mmcb_server.SelectedItem = mmcb_server.Items[2];
            }
            else if (Properties.Settings.Default.MM_Server == "EU")
            {
                mmcb_server.SelectedItem = mmcb_server.Items[1];
            }
            else if (Properties.Settings.Default.MM_Server == "KOR")
            {
                mmcb_server.SelectedItem = mmcb_server.Items[0];
            }

            if (Properties.Settings.Default.MM_Skill == "Beginner")
            {
                mmcb_skill.SelectedItem = mmcb_skill.Items[0];
            }
            else if (Properties.Settings.Default.MM_Skill == "Intermediate")
            {
                mmcb_skill.SelectedItem = mmcb_skill.Items[1];

            }
            else if (Properties.Settings.Default.MM_Skill == "Advanced")
            {
                mmcb_skill.SelectedItem = mmcb_skill.Items[2];
            }

            if (Properties.Settings.Default.MM_Mode == "Standard")
            {
                mmcb_mode.SelectedItem = mmcb_mode.Items[0];
            } else if (Properties.Settings.Default.MM_Mode == "Commander") {
                mmcb_mode.SelectedItem = mmcb_mode.Items[1];
            }



        }

        public Win_mm(MainWindow mw) : this()
        {
            MW = mw;
            foreach (string pl in MW.player_list)
            {
                mmcb_player.Items.Add(pl);
            }
            mmcb_player.SelectedItem = mmcb_player.Items[0];
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mmcb_credential.IsChecked == true)
            {

                gr_accept.Visibility = Visibility.Hidden;
                tb_accepted.Text = "";
                
                lb_pl1.Content = "Player";
                lb_pl2.Content = "Player";
                lb_pl3.Content = "Player";
                lb_pl4.Content = "Player";
                lb_pl5.Content = "Player";
                lb_pl6.Content = "Player";
                tb_mmid.Text = "0";

                mmcb_randoms.IsEnabled = true;
                mmcb_randoms.IsChecked = false;
                GAME_READY = false;

                string player = mmcb_player.SelectedItem.ToString();
                string mode = ((ComboBoxItem)mmcb_mode.SelectedItem).Content.ToString();
                string num = ((ComboBoxItem)mmcb_num.SelectedItem).Content.ToString();
                string skill = ((ComboBoxItem)mmcb_skill.SelectedItem).Content.ToString();
                string server = ((ComboBoxItem)mmcb_server.SelectedItem).Content.ToString();

                Properties.Settings.Default.MM_Server = server;
                Properties.Settings.Default.MM_Skill = skill;
                Properties.Settings.Default.MM_Mode = mode;
                Properties.Settings.Default.Save();

                if (sender != null)
                {
                    tb_gamefound.Text = "Connecting ...";
                    mmcb_ample.IsChecked = true;
                    mmcb_ample.Content = "Online";
                    Task FindGame = Task.Factory.StartNew(() =>
                    {
                        dsmmclient mm = new dsmmclient(this);
                        Socket sock = mm.StartClient(this, player, mode, num, skill, server);
                        MW.Dispatcher.Invoke(() =>
                        {
                            mmcb_ample.IsChecked = false;
                            mmcb_ample.Content = "Offline";
                            try
                            {
                                _timer.Stop();
                            }
                            catch
                            {

                            }
                        });
                    }, TaskCreationOptions.AttachedToParent);

                    //tb_info.Text = mm.INFO;

                    _time = TimeSpan.FromSeconds(1);

                    _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
                    {
                        tb_time.Text = _time.ToString("c");

                        // fill with randoms after ~5min
                        if (_time == TimeSpan.FromMinutes(1))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                //mmcb_randoms.IsEnabled = true;
                                //tb_gamefound.Text += " You can fill your lobby with randoms now if you want (check 'allow Randoms' right to the clock)";
                            });
                        }
                        _time = _time.Add(TimeSpan.FromSeconds(+1));
                    }, Application.Current.Dispatcher);

                    _timer.Start();

                    bt_show.IsEnabled = false;
                } else
                {
                    try
                    {
                        _timer.Start();
                    }
                    catch
                    {

                    }
                }

            } else
            {
                mmcb_credential_Click(sender, e);
                if (mmcb_credential.IsChecked == true)
                {
                    Button_Click(sender, e);
                }
            }

        }

        public void pb_Accept(bool game_accept, string mmid)
        {
            tb_gamefound.Visibility = Visibility.Visible;
            if (game_accept)
            {
                if (CLIENT != null)
                {

                    try
                    {
                        string player = mmcb_player.SelectedItem.ToString();
                        string string1 = "Hello from [" + player + "]: accept: " + mmid + ";" + "\r\n";
                        byte[] preBuf = Encoding.UTF8.GetBytes(string1);
                        CLIENT.Send(preBuf);
                    }
                    catch
                    {

                    }
                }
            } else
            {
                if (CLIENT != null)
                {

                    try
                    {
                        string player = mmcb_player.SelectedItem.ToString();
                        string string1 = "Hello from [" + player + "]: decline: " + mmid + ";" + "\r\n";
                        byte[] preBuf = Encoding.UTF8.GetBytes(string1);
                        CLIENT.Send(preBuf);
                    }
                    catch
                    {

                    }
                }
            }


        }

        private void bt_decline_Click(object sender, RoutedEventArgs e)
        {
            if (SP != null) SP.Stop();
            doit = false;
            ACCEPTED = false;
            gr_accept.Visibility = Visibility.Hidden;
            pb_Accept(false, tb_mmid.Text);
        }

        private void bt_accept_Click(object sender, RoutedEventArgs e)
        {
            if (SP != null) SP.Stop();
            ACCEPTED = true;
            pb_Accept(true, tb_mmid.Text);
            tb_accepted.Text = "TY! Waiting for other players ..";
        }

        public void GameFound(string mmid)
        {

            ProgressBar progbar = new ProgressBar();
            progbar.IsIndeterminate = false;
            progbar.Orientation = Orientation.Vertical;
            progbar.Width = 50;
            progbar.Height = 250;
            Duration duration = new Duration(TimeSpan.FromSeconds(35));
            DoubleAnimation doubleanimation = new DoubleAnimation(100.0, duration);
            progbar.BeginAnimation(ProgressBar.ValueProperty, doubleanimation);
            progbar.HorizontalAlignment = HorizontalAlignment.Left;
            gr_accept.Children.Add(progbar);
            gr_accept.Visibility = Visibility.Visible;
            tb_gamefound.Visibility = Visibility.Hidden;
            tb_mmid.Text = mmid;
            doit = true;
            Task ts_accept = Task.Factory.StartNew(() =>
            {
                bool mydoit = doit;                
                while (mydoit) {
                    Thread.Sleep(1000);
                    Dispatcher.Invoke(() =>
                    {
                        if (progbar.Value == 100)
                        {
                            gr_accept.Visibility = Visibility.Hidden;
                            if (ACCEPTED == false)
                            {
                                //pb_Accept(false, mmid);
                                bt_decline_Click(null, null);
                                //Exit_Click(null, null);
                            }

                            else if (GAME_READY == false)
                            {
                                Button_Click(null, null);
                            }

                            doit = false;

                        }
                        if (GAME_READY == true)
                        {
                            doit = false;
                        }
                        mydoit = doit;
                    });
                }
            }, TaskCreationOptions.AttachedToParent);

            SP = new SoundPlayer(@"audio\ready.wav");
            SP.Play();
        }

        public void GameReady(int mypos, string creator, string mmid)
        {
            _timer.Stop();
            GAME_READY = true;
            string msg = "";
            if (creator == mypos.ToString())
            {
                msg = "Game found! You have been elected to be the lobby creator. Please open your Starcraft 2 client on the " + tb_server.Text + " server and create a private Direct Strike Lobby. " +
                    "Join the Channel sc2dsmm by typing ‘/join sc2dsmm’ in the Starcraft 2 chat and post the lobby link combined with the MMID by typing ‘/lobbylink " +
                    mmid + "’ (without the quotes). Have fun! :)";
            }
            else
            {
                msg = "Game found! Player " + creator + " has been elected to be the lobby creator. Please open your Starcraft 2 client on the " + tb_server.Text + " server and join the Channel" +
                    " sc2dsmm by typing ‘/join sc2dsmm’ in the Starcraft 2 chat. Wait for the lobby link combined with the MMID " +
                    mmid + " and click on it. Have fun! :)";
            }
            tb_gamefound.Text = msg;
            tb_gamefound.Visibility = Visibility.Visible;
        }

        private void lb_switch_CLick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("To switch the server you can either set the region in the Battlenet.App before starting the game (right above the Play button)" +
                " or in game open the Menu (bottom right) and then click on the little globe Button top right.", "sc2dsmm");
        }

        private void mmcb_credential_Click(object sender, RoutedEventArgs e)
        {

            string info = "";
            info = "By using this matchmaking system you agree, that your SC2-username and skill information will be stored to generate the games" +
                " and show rankings on https://www.pax77.org/sc2dsladder." +
                " The data will not be used for any other purpose and will not be disclosed to third parties." +
                " You can decline to the agreement at any time by unchecking the Credential and delete all data (File->MM->Delete me)."
                + Environment.NewLine + Environment.NewLine;

            if (MessageBox.Show(info + "Do you agree to these Terms of Use?", "sc2dsmm", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
            {
                //do no stuff
                mmcb_credential.IsChecked = false;
                Properties.Settings.Default.MM_CREDENTIAL = false;
            }
            else
            {
                //do yes stuff
                Properties.Settings.Default.MM_CREDENTIAL = true;

                DateTime d1 = DateTime.Today;
                DateTime d2 = new DateTime(2019, 3, 3, 0, 0, 0);
                if (Properties.Settings.Default.MM_Deleted != null)
                {
                    d2 = Properties.Settings.Default.MM_Deleted;
                }
                TimeSpan diff1 = d1.Subtract(d2);

                if (diff1.TotalDays >= downtime)
                {
                    mmcb_credential.IsChecked = true;
                    Properties.Settings.Default.MM_CREDENTIAL = true;
                } else
                {
                    mmcb_credential.IsChecked = false;
                    int uptime = 3 - (int)diff1.TotalDays;
                    MessageBox.Show("You can rejoin the MM-System in " + uptime.ToString() + " Days.", "sc2dsmm");
                    Properties.Settings.Default.MM_CREDENTIAL = false;
                }
            }
            Properties.Settings.Default.Save();
        }

        public void mmcb_randoms_Click(object sender, RoutedEventArgs e)
        {
            string rng = "0";
            if (mmcb_randoms.IsChecked == true)
            {
                rng = "1";
            }
            if (CLIENT != null)
            {
                   
                try
                {
                    string player = mmcb_player.SelectedItem.ToString();
                    string string1 = "Hello from [" + player + "]: allowRandoms: " + rng + "\r\n";
                    byte[] preBuf = Encoding.UTF8.GetBytes(string1);
                    CLIENT.Send(preBuf);
                }
                catch
                {

                }
            }
        }

        public void Delete()
        {
            // delete all users - keep delete info for 14 days
            DateTime d1 = DateTime.Today;
            DateTime d2 = new DateTime(2019, 3, 3, 0, 0, 0);
            if (Properties.Settings.Default.MM_Deleted != null)
            {
                d2 = Properties.Settings.Default.MM_Deleted;
            }
            TimeSpan diff1 = d1.Subtract(d2);
            if (diff1.TotalDays > 3)
            {
                Properties.Settings.Default.MM_CREDENTIAL = false;
                Properties.Settings.Default.MM_Deleted = DateTime.Today;
                Properties.Settings.Default.Save();

                Task senddel = Task.Factory.StartNew(() =>
                {
                    foreach (string player in MW.player_list)
                    {
                        dsmmclient result = new dsmmclient();
                        Socket sock = result.StartClient(player, "Deleteme;");
                    }

                }, TaskCreationOptions.AttachedToParent);

                MessageBox.Show("Deleted. You will be able to rejoin the mm-system in 3 days.", "sc2dsmm");

            }
        }

        private void btn_report_Click(object sender, RoutedEventArgs e)
        {
            MW.mnu_Scan(sender, e);
            MessageBox.Show("Scanning replays - please wait ..", "sc2dsmm");

            try
            {
                Task showit = MW.tsscan.ContinueWith((antecedent) => {
                    Dispatcher.Invoke(() =>
                    {
                        Win_mmselect msel = new Win_mmselect(MW, this);
                        msel.Show();
                    });
                });
            }
            catch
            {
                SelectReplay();
            }

            Console.WriteLine("Scan complete.");
        }

        public void SelectReplay()
        {
            Dispatcher.Invoke(() =>
            {
                Win_mmselect msel = new Win_mmselect(MW, this);
                msel.Show();
            });
        }

        public void SendResult(string res)
        {
            string player = mmcb_player.SelectedItem.ToString();
            Task sendres = Task.Factory.StartNew(() =>
            {
                dsmmclient result = new dsmmclient();
                Socket sock = result.StartClient(player, res);


            }, TaskCreationOptions.AttachedToParent);

            MessageBox.Show("Result sent. Thank you!", "sc2dsmm");
            bt_show.IsEnabled = true;

            tb_gamefound.Text = res;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            bt_show.IsEnabled = true;
            tb_gamefound.Text = "";


            if (CLIENT != null)
            {
                // Release the socket.    
                try
                {
                    string string1 = "Exit. Have fun." + "\r\n";
                    byte[] preBuf = Encoding.UTF8.GetBytes(string1);
                    CLIENT.Send(preBuf);
                    CLIENT.Shutdown(SocketShutdown.Both);
                    CLIENT.Close();
                } 
                catch
                {

                }
            }

            try
            {
                _timer.Stop();
            }
            catch
            {

            }
        }

        
        public void mm_Exit(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Exit_Click(sender, null);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //GameFound(1, "1", "1");
        }

        private void Tb_mmid_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
