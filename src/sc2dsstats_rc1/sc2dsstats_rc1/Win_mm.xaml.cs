using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
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
        public bool ACCEPTED { get; set; } = false;
        public bool ALL_ACCEPTED { get; set; } = false;
        public bool DECLINED { get; set; } = false;
        public bool ALL_DECLINED { get; set; } = false;
        public bool WAITING { get; set; } = false;
        public List<Win_mm> TESTWIN { get; set; } = new List<Win_mm>();
        public Dictionary<int, dsmmid> MMIDS { get; set; } = new Dictionary<int, dsmmid>();
        public dsmmclient MM { get; set; }

        public DispatcherTimer _timer;
        public TimeSpan _time;
        public int downtime = 0;

        public Win_mm()
        {
            InitializeComponent();
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

            tb_elo.Text = Properties.Settings.Default.ELO;

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

        public static int ShowDialog1(string text, string caption)
        {
            int result = 0;

            Window prompt = new Window();
            prompt.Width = 250;
            prompt.Height = 250;
            prompt.Title = "credential";
            Grid panel = new Grid();
            TextBox tb = new TextBox();
            tb.Width = 230;
            tb.Height = 150;
            tb.Margin = new Thickness(5, 5, 5, 5);
            tb.Text = text;
            tb.HorizontalAlignment = HorizontalAlignment.Center;
            tb.VerticalAlignment = VerticalAlignment.Top;

            CheckBox chk = new CheckBox();
            chk.Content = "Allow ladder";
            chk.Margin = new Thickness(5, 0, 15, 5);
            chk.HorizontalAlignment = HorizontalAlignment.Center;
            chk.VerticalAlignment = VerticalAlignment.Bottom;
            Button ok = new Button();
            ok.Content = "Yes";
            ok.Width = 50;
            ok.Margin = new Thickness(5, 0, 5, 5);

            ok.Click += (sender, e) => { result++; prompt.Close(); };
            ok.HorizontalAlignment = HorizontalAlignment.Left;
            ok.VerticalAlignment = VerticalAlignment.Bottom;
            Button no = new Button();
            no.Content = "No";
            no.Width = 50;
            no.Margin = new Thickness(5, 0, 5, 5);
            no.Click += (sender, e) => { result++; prompt.Close(); };
            no.HorizontalAlignment = HorizontalAlignment.Right;
            no.VerticalAlignment = VerticalAlignment.Bottom;

            panel.Children.Add(tb);
            panel.Children.Add(chk);
            //panel.SetFlowBreak(chk, true);
            panel.Children.Add(ok);
            panel.Children.Add(no);
            prompt.Content = panel;
            prompt.Show();

            
            if (chk.IsChecked == true) result++;

            return result;
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mmcb_credential.IsChecked == true)
            {
                ClearReport();

                gr_accept.Visibility = Visibility.Hidden;
                tb_accepted.Text = "";
                
                lb_pl1.Content = "";
                lb_pl2.Content = "";
                lb_pl3.Content = "";
                lb_pl4.Content = "";
                lb_pl5.Content = "";
                lb_pl6.Content = "";
                tb_mmid.Text = "0";

                ACCEPTED = false;
                DECLINED = false;
                ALL_ACCEPTED = false;
                ALL_DECLINED = false;
                if (sender == null && MM != null) MM.STATUS = false;

                mmcb_randoms.IsEnabled = false;
                mmcb_randoms.IsChecked = false;
                mmcb_report.IsEnabled = false;

                mmcb_acc1.IsChecked = null;
                mmcb_acc2.IsChecked = null;
                mmcb_acc3.IsChecked = null;
                mmcb_acc4.IsChecked = null;
                mmcb_acc5.IsChecked = null;
                mmcb_acc6.IsChecked = null;

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
                        MM = mm;
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
                                mmcb_randoms.IsEnabled = true;
                                tb_gamefound.Text += " You can fill your lobby with randoms now if you want (check 'allow Randoms' right to the clock)";
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

        }

        public void pb_Accept_off(bool game_accept, string mmid)
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
            DECLINED = true;
            gr_accept.Visibility = Visibility.Hidden;
            pb_Accept(false, tb_mmid.Text);
        }

        private void bt_accept_Click(object sender, RoutedEventArgs e)
        {
            if (SP != null) SP.Stop();
            ACCEPTED = true;
            pb_Accept(true, tb_mmid.Text);
            tb_accepted.Text = "TY! Waiting for other players ..";
            bt_accept.IsEnabled = false;
            bt_decline.IsEnabled = false;
        }

        public void GameFound(string mmid)
        {

            if (gr_accept.Children.OfType<ProgressBar>().ToList().Count > 0)
            {
                var child = gr_accept.Children.OfType<ProgressBar>().First();
                gr_accept.Children.Remove(child);
                child = null;
            }

            ProgressBar progbar = new ProgressBar();
            progbar.IsIndeterminate = false;
            progbar.Orientation = Orientation.Vertical;
            progbar.Width = 50;
            progbar.Height = 250;
            Duration duration = new Duration(TimeSpan.FromSeconds(35));
            DoubleAnimation doubleanimation = new DoubleAnimation(100.0, duration);
            progbar.BeginAnimation(ProgressBar.ValueProperty, doubleanimation);
            progbar.HorizontalAlignment = HorizontalAlignment.Left;
            progbar.Name = "pb_accept";
            gr_accept.Children.Add(progbar);
            bt_accept.IsEnabled = true;
            bt_decline.IsEnabled = true;
            gr_accept.Visibility = Visibility.Visible;
            tb_gamefound.Visibility = Visibility.Hidden;
            tb_mmid.Text = mmid;

            string player = mmcb_player.SelectedItem.ToString();
            //Console.WriteLine(player + "waiting for accept (or decline)");
            ALL_DECLINED = false;
            ALL_ACCEPTED = false;
            ACCEPTED = false;
            DECLINED = false;

            WAITING = true;
            Task ts_accept = Task.Factory.StartNew(() =>
            {

                bool timeout = false;

                while (true)
                {
                    Thread.Sleep(600);

                    if (progbar == null)
                    {
                        break;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        // timed out
                        if (progbar.Value == 100)
                        {
                            timeout = true;                            
                        }
                    });

                    if (gr_accept.Visibility == Visibility.Hidden)
                    {
                        //Console.WriteLine(player + ": Break 0");
                        break;
                    }

                    if (timeout == true)
                    {
                        if (ACCEPTED == false)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                DECLINED = true;
                            });
                            if (MM != null)
                            {
                                int i = 0;
                                while (MM.STATUS == false)
                                {
                                    Thread.Sleep(100);
                                    i++;
                                    if (i > 20)
                                    {
                                        //Console.WriteLine(player + ": Timeout 1");
                                        break;
                                    }
                                    if (MM == null)
                                    {
                                        //Console.WriteLine(player + ": Timeout 2");
                                        break;
                                    }
                                }
                            }
                            //Console.WriteLine(player + ": Timeout 3");
                            break;
                        } else if (DECLINED == true)
                        {

                        } else  
                        {
                            int i = 0;
                            while (!ALL_ACCEPTED && !ALL_DECLINED)
                            {
                                Thread.Sleep(100);
                                i++;
                                if (i > 20)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        ALL_DECLINED = true;
                                        ACCEPTED = false;
                                    });
                                    Console.WriteLine(player + ": Timeout 4");
                                    break;
                                }
                            }
                            //Console.WriteLine(player + ": Timeout 5");
                            //break;
                        }
                    }

                    if (ACCEPTED == true)
                    {
                        if (ALL_ACCEPTED == true)
                        {
                            break;
                        }

                        if (ALL_DECLINED == true)
                        {
                            // reset
                            Dispatcher.Invoke(() =>
                            {
                                tb_gamefound.Text = "Someone declined :( - Searching again ..";
                                Button_Click(null, null);
                            });
                            break;
                        }
                    }

                    if (ALL_DECLINED == true)
                    {
                        if (ACCEPTED == true)
                        {
                            // reset
                            Dispatcher.Invoke(() =>
                            {
                                tb_gamefound.Text = "Someone declined :( - Searching again ..";
                                Button_Click(null, null);
                            });
                            //Console.WriteLine(player + ": Break 1");
                            break;
                        }

                        if (DECLINED == true)
                        {
                            if (MM != null)
                            {
                                int i = 0;
                                while (MM.STATUS == false)
                                {
                                    Thread.Sleep(100);
                                    i++;
                                    if (i > 20)
                                    {
                                        //Console.WriteLine(player + ": Break 2");
                                        break;
                                    }
                                    if (MM == null)
                                    {
                                        //Console.WriteLine(player + ": Break 3");
                                        break;
                                    }
                                }
                            }
                            //Console.WriteLine(player + ": Break 4");
                            break;
                        }
                    }

                    if (DECLINED == true)
                    {
                        if (MM != null)
                        {
                            int i = 0;
                            while (MM.STATUS == false)
                            {
                                Thread.Sleep(100);
                                i++;
                                if (i > 20)
                                {
                                    //Console.WriteLine(player + ": Break 4");
                                    break;
                                }
                                if (MM == null)
                                {
                                    //Console.WriteLine(player + ": Break 5");
                                    break;
                                }
                            }
                        }
                        //Console.WriteLine(player + ": Break 6");
                        break;
                    }




                }
                Dispatcher.Invoke(() =>
                {
                    if (gr_accept.Children.OfType<ProgressBar>().ToList().Count > 0)
                    {
                        var child = gr_accept.Children.OfType<ProgressBar>().First();
                        gr_accept.Children.Remove(child);
                        child = null;
                    }
                    gr_accept.Visibility = Visibility.Hidden;
                    tb_gamefound.Visibility = Visibility.Visible;
                    
                    WAITING = false;

                });

            }, TaskCreationOptions.AttachedToParent);



            SP = new SoundPlayer(@"audio\ready.wav");
            SP.Play();
        }

        public void GameReady(int mypos, string creator, string mmid)
        {
            _timer.Stop();
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

            tb_info.Text = "Please do not forget to report the game after it is finished (by pressing 'Report game')";

            dsmmid id = new dsmmid();
            try
            {
                id.MMID = int.Parse(tb_mmid.Text);
            } catch { }
            id.MOD = ((ComboBoxItem)mmcb_mode.SelectedItem).Content.ToString();
            id.NUM = ((ComboBoxItem)mmcb_num.SelectedItem).Content.ToString();
            id.SERVER = ((ComboBoxItem)mmcb_server.SelectedItem).Content.ToString();


            var labels = gr_mm_lb.Children.OfType<Label>().Where(x => x.Name.StartsWith("lb_pl"));
            foreach (Label lb in labels)
            {
                string pattern = @"^lb_pl(\d)";
                int i = 1;
                foreach (Match m in Regex.Matches(lb.Name, pattern))
                {
                    try
                    {
                        i = int.Parse(m.Groups[1].Value);
                    } catch { }
                }

                if (lb.Content.ToString().Length > 0)
                {
                    KeyValuePair<int, string> pl = new KeyValuePair<int, string>(i, lb.Content.ToString());
                    id.PLAYERS.Add(pl);

                    string pattern1 = @"^Random(\d)";
                    Match m2 = Regex.Match(lb.Content.ToString(), pattern1);
                    if (m2.Success)
                    {
                        // just a random
                    }
                    else
                    {
                        id.NEED++;
                    }
                }
            }
            if (!MMIDS.ContainsKey(id.MMID)) MMIDS.Add(id.MMID, id);
        }

        public void SetupPos(string msg, string player)
        {
            int mypos = 0;
            string mmid = "0";
            string creator = "1";
            string server = "NA";
            string elo = "0";

            gr_accept.Visibility = Visibility.Hidden;


            foreach (string p in msg.Split(';'))
            {
                ClearReport();
                string pt_pos = @"^pos(\d): (.*)";
                foreach (Match m2 in Regex.Matches(p, pt_pos))
                {
                    int pos = Int32.Parse(m2.Groups[1].ToString());
                    string teammate = m2.Groups[2].ToString();

                    if (teammate == player)
                    {
                        mypos = pos;
                    }

                    Label lb = null;
                    if (pos == 0)
                    {
                        mmid = m2.Groups[2].ToString();
                    }
                    else if (pos == 1)
                    {
                        lb = lb_pl1;
                    }
                    else if (pos == 2)
                    {
                        lb = lb_pl2;
                    }
                    else if (pos == 3)
                    {
                        lb = lb_pl3;
                    }
                    else if (pos == 4)
                    {
                        lb = lb_pl4;
                    }
                    else if (pos == 5)
                    {
                        lb = lb_pl5;
                    }
                    else if (pos == 6)
                    {
                        lb = lb_pl6;
                    }
                    else if (pos == 7)
                    {
                        creator = teammate;
                    }
                    else if (pos == 8)
                    {
                        server = teammate;
                    }
                    else if (pos == 9)
                    {
                        elo = teammate;
                    }
                    else
                    {
                        lb = lb_info;
                    }

                    if (mmid != "0") tb_mmid.Text = mmid;
                    if (lb != null) lb.Content = teammate;
                    if (server != "0") tb_server.Text = server;
                    tb_gamefound.Visibility = Visibility.Visible;
                }
            }
            GameReady(mypos, creator, mmid);
        }

        private void lb_switch_CLick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("To switch the server you can either set the region in the Battlenet.App before starting the game (right above the Play button)" +
                " or in game open the Menu (bottom right) and then click on the little globe Button top right.", "sc2dsmm");
        }

        private void mmcb_credential_Click(object sender, RoutedEventArgs e)
        {

            //int bab = ShowDialog1("bla", "blub");
            //Console.WriteLine("Credential: " + bab);

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
                        dsmmclient result = new dsmmclient(this);
                        result.StartClient(this, player, "Deleteme;");
                    }

                }, TaskCreationOptions.AttachedToParent);

                MessageBox.Show("Deleted. You will be able to rejoin the mm-system in 3 days.", "sc2dsmm");

            }
        }

        private void btn_report_Click(object sender, RoutedEventArgs e)
        {
            MW.mnu_Scan(sender, e);
            MessageBox.Show("Scanning replays - please wait ..", "sc2dsmm");
            Win_mmselect msel = new Win_mmselect(MW, this);
            try
            {
                Task showit = MW.tsscan.ContinueWith((antecedent) => {
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            msel.Show();
                        } catch { }
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
            Win_mmselect msel = new Win_mmselect(MW, this);
            msel.Show();
        }

        public string SendResult(string res, dsreplay rep)
        {
            string player = null;
            try
            {
                player = mmcb_player.SelectedItem.ToString();
            }catch { }

            if (player == null)
            {
                player = MW.player_list.ElementAt(0);
            }

            string report = null;
            Task sendres = Task.Factory.StartNew(() =>
            {
                dsmmclient result = new dsmmclient();
                report = result.StartClient(this, player, res);

            }, TaskCreationOptions.AttachedToParent);

            sendres.Wait();

            if (report != null)
            {
                if (rep != null) rep.REPORTED = 1;
                PresentResult(report);
            }

            //MessageBox.Show("Result sent. Thank you!", "sc2dsmm");
            bt_show.IsEnabled = true;
            tb_gamefound.Text = res;

            return report;
        }

        public void PresentResult(string report)
        {
            dsmmid id = new dsmmid();
            string mmid = "0";
            foreach (string p in report.Split(';'))
            {
                string pt_pos = @"^pos(\d): (.*)";
                foreach (Match m2 in Regex.Matches(p, pt_pos))
                {
                    string i = m2.Groups[1].Value.ToString();
                    string pos = m2.Groups[2].Value.ToString();
                    
                    if (i == "0")
                    {
                        mmid = pos;
                        if (MMIDS.ContainsKey(int.Parse(mmid))) 
                        {
                            id = MMIDS[int.Parse(mmid)];
                        } else
                        {
                            // :(
                            MMIDS.Add(int.Parse(mmid), id);
                            id = MMIDS[int.Parse(mmid)];
                        }
                        id.REPORTED = 1;
                    }
                    else if (int.Parse(i) <= 6) 
                    {
                        dsplayer pl = new dsplayer();
                        pl.POS = int.Parse(i);

                        // $response .= "pos" . $pl->POS . ": " . $pl->NAME . "|" . $pl_elo . "|" . $pl_elo_change . "|" . $pl->RACE . "|" . $pl->KILLSUM . ";";
                        int l = 0;
                        foreach (string ent in pos.Split('|'))
                        {
                            l++;
                            if (l == 1) pl.NAME = ent;
                            else if (l == 4) pl.RACE = ent;
                            else if (l == 2) pl.ELO = double.Parse(ent, new CultureInfo("en-US"));
                            else if (l == 3) pl.ELO_CHANGE = double.Parse(ent, new CultureInfo("en-US"));
                            else if (l == 5) pl.KILLSUM = int.Parse(ent);
                            
                        }

                        int c = 0;
                        dsplayer plrm = new dsplayer();
                        foreach (dsplayer plmm in id.REPORTS)
                        {
                            if (plmm.NAME == pl.NAME)
                            {
                                c++;
                                plmm.RACE = pl.RACE;
                                plmm.ELO = pl.ELO;
                                plmm.ELO_CHANGE = pl.ELO_CHANGE;
                                plmm.KILLSUM = pl.KILLSUM;
                                plmm.POS = pl.POS;
                            }

                            if (plmm.POS == pl.POS)
                            {
                                plrm = plmm;
                            }

                        }
                        if (c == 0)
                        {
                            if (plrm != null) id.REPORTS.Remove(plrm);
                            id.REPORTS.Add(pl);
                        }

                    } else if (int.Parse(i) == 7)
                    {
                        // TODO: Player MU|Expose|Sigma
                        string mu = pos;
                        double d = 0.0;
                        try
                        {
                            d = double.Parse(mu, new CultureInfo("en-US"));
                        } catch
                        {

                        }
                        if (d > 0)
                        {
                            Properties.Settings.Default.ELO = d.ToString();
                            Properties.Settings.Default.Save();
                            tb_elo.Text = Properties.Settings.Default.ELO;
                        }

                    }
                }
            }

            mmcb_report.Items.Clear();
            if (id != null && id.REPORTS.Count > 0 && MMIDS.Keys.Count > 0)
            {
                int i = 0;
                int j = 0;
                foreach (int mmid2 in MMIDS.Keys)
                {
                    mmcb_report.Items.Add(mmid2.ToString());
                    if (mmid2.ToString() == tb_mmid.Text)
                    {
                        j = i;
                    }
                    i++;
                }
                mmcb_report.SelectedItem = mmcb_report.Items[j];
                mmcb_report.IsEnabled = true;
                DisplayReport(mmcb_report.SelectedItem.ToString());
            } else
            {
                if (int.Parse(mmid) > 0) DisplayReport(mmid);
            }

                
           
        }

        public void ClearReport()
        {

            var labels = gr_mm_lb.Children.OfType<Label>().Where(x => x.Name.StartsWith("lb_elo"));
            foreach (Label l in labels)
            {
                l.Content = "";
            }
            labels = gr_mm_lb.Children.OfType<Label>().Where(x => x.Name.StartsWith("lb_race"));
            foreach (Label l in labels)
            {
                l.Content = "";
            }

            labels = gr_mm_lb.Children.OfType<Label>().Where(x => x.Name.StartsWith("lb_race"));
            foreach (Label l in labels)
            {
                l.Content = "";
            }
            labels = gr_mm_lb.Children.OfType<Label>().Where(x => x.Name.StartsWith("lb_racename"));
            foreach (Label l in labels)
            {
                l.Content = "";
            }
            var pl_images = gr_mm_lb.Children.OfType<System.Windows.Controls.Image>().Where(x => x.Name.StartsWith("img_pl"));
            foreach (System.Windows.Controls.Image i in pl_images)
            {
                i.Source = null;
            }
            //PressedBorderBrush
            var pl_labels = gr_mm_lb.Children.OfType<Label>().Where(x => x.Name.StartsWith("lb_pl"));
            foreach (Label l in pl_labels) {
                l.Background = this.Resources["PressedBorderBrush"] as LinearGradientBrush;
                //l.Content = "";
            }

            lb_duration.Content = "";
            tb_dmg1.Text = "0";
            tb_dmg2.Text = "0";
            tb_cash1.Text = "0";
            tb_cash2.Text = "0";
            tb_army1.Text = "0";
            tb_army2.Text = "0";
            lb_dmgdiff.Content = "0";
            lb_cashdiff.Content = "0";
            lb_armydiff.Content = "0";

            gr_summary.Visibility = Visibility.Hidden;

        }

        private void Mmcb_report_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mmcb_report.SelectedItem != null) DisplayReport(mmcb_report.SelectedItem.ToString());
        }

        public void DisplayReport (string mmid)
        {
            ClearReport();
            tb_info.Text = "Report for MMID " + mmid;
            var pl_labels = gr_mm_lb.Children.OfType<Label>().Where(x => x.Name.StartsWith("lb_pl"));
            var elo_labels = gr_mm_lb.Children.OfType<Label>().Where(x => x.Name.StartsWith("lb_elo"));
            var race_labels = gr_mm_lb.Children.OfType<Label>().Where(x => x.Name.StartsWith("lb_race"));
            var pl_images = gr_mm_lb.Children.OfType<System.Windows.Controls.Image>().Where(x => x.Name.StartsWith("img_pl"));
            var racename_lables = gr_mm_lb.Children.OfType<Label>().Where(x => x.Name.StartsWith("lb_racename"));

            dsimage myimg = new dsimage();
            Label lb_mvp = null;
            Label lb_player = null;
            double max_killsum = 0;
            double t1_dmg = 0;
            double t2_dmg = 0;
            double t1_cash = 0;
            double t2_cash = 0;
            double t1_army = 0;
            double t2_army = 0;

            foreach (dsplayer pl in MMIDS[int.Parse(mmid)].REPORTS)
            {
                if (pl.POS >= 1 && pl.POS <= 6)
                {
                    Label lpl = pl_labels.Where(x => x.Name.Contains(pl.POS.ToString())).ToList()[0];
                    Label lelo = elo_labels.Where(x => x.Name.Contains(pl.POS.ToString())).ToList()[0];
                    Label lrace = race_labels.Where(x => x.Name.Contains(pl.POS.ToString())).ToList()[0];
                    System.Windows.Controls.Image irace = pl_images.Where(x => x.Name.Contains(pl.POS.ToString())).ToList()[0];
                    Label lracename = racename_lables.Where(x => x.Name.Contains(pl.POS.ToString())).ToList()[0];

                    if (lelo != null && lrace != null && lpl != null && irace != null && lracename != null)
                    {

                        lpl.Background = this.Resources["PressedBorderBrush"] as LinearGradientBrush;
                        lpl.Content = pl.NAME + " MU: " + pl.ELO; 
                        lelo.Content = pl.ELO_CHANGE.ToString();
                        if (pl.ELO_CHANGE < 0)
                        {
                            lelo.Foreground = System.Windows.Media.Brushes.Red;
                        }
                        else
                        {
                            lelo.Foreground = System.Windows.Media.Brushes.Green;
                        }

                        string army = ((int)pl.ARMY / 1000).ToString() + "k";
                        string income = ((int)pl.INCOME / 1000).ToString() + "k";
                        string damage = ((int)pl.KILLSUM / 1000).ToString() + "k";

                        if (pl.POS <= 3) {
                            t1_army += pl.ARMY;
                            t1_cash += pl.INCOME;
                            t1_dmg += pl.KILLSUM;
                        } else if (pl.POS <= 6)
                        {
                            t2_army += pl.ARMY;
                            t2_cash += pl.INCOME;
                            t2_dmg += pl.KILLSUM;
                        }

                        if (pl.KILLSUM > max_killsum)
                        {
                            lb_mvp = lpl;
                            max_killsum = pl.KILLSUM;
                        }

                        if (MW.player_list.Contains(pl.NAME))
                        {
                            lb_player = lpl;
                        }

                        lrace.Content = "Army: " + army + " cash: " + income + " dmg: " + damage;
                        lracename.Content = pl.RACE;

                        BitmapImage bit_syn = new BitmapImage();
                        bit_syn.BeginInit();
                        bit_syn.UriSource = new Uri(myimg.GetImage(pl.RACE), UriKind.Relative);
                        bit_syn.EndInit();
                        irace.Source = bit_syn;





                    }

                    if (mmcb_player.SelectedItem.ToString() == pl.NAME)
                    {
                        double d = 0.0;
                        try
                        {
                            d = double.Parse(tb_elo.Text, new CultureInfo("en-US"));
                        } catch { }
           

                        if (d == 0) tb_elo.Text = pl.ELO.ToString();
                        lb_elodiff.Content = pl.ELO_CHANGE.ToString();
                        if (pl.ELO_CHANGE < 0)
                        {
                            lb_elodiff.Foreground = System.Windows.Media.Brushes.Red;
                        }
                        else
                        {
                            lb_elodiff.Foreground = System.Windows.Media.Brushes.Green;
                        }
                        Properties.Settings.Default.ELO = pl.ELO.ToString();
                        Properties.Settings.Default.Save();
                    }
                }
            }

            if (lb_player != null)
            {
                lb_player.Background = new SolidColorBrush(Colors.DarkBlue);
            }

            if (lb_mvp != null)
            {
                lb_mvp.Background = new SolidColorBrush(Colors.DarkViolet);
            }

            if (t1_dmg > 0 && t2_dmg > 0)
            {
                double diff = t1_dmg - t2_dmg;
                double per = diff / t1_dmg;
                lb_dmgdiff.Content = per.ToString("P", CultureInfo.InvariantCulture);
                tb_dmg1.Text = (t1_dmg / 1000).ToString("N", CultureInfo.CreateSpecificCulture("es-ES")) + "k";
                tb_dmg2.Text = (t2_dmg / 1000).ToString("N", CultureInfo.CreateSpecificCulture("es-ES")) + "k";

                if (per < 0)
                {
                    lb_dmgdiff.Foreground = System.Windows.Media.Brushes.Red;
                } else
                {
                    lb_dmgdiff.Foreground = System.Windows.Media.Brushes.Green;
                }
            }

            if (t1_cash > 0 && t2_cash > 0)
            {
                double diff = t1_cash - t2_cash;
                double per = diff / t1_cash;
                lb_cashdiff.Content = per.ToString("P", CultureInfo.InvariantCulture);
                tb_cash1.Text = (t1_cash / 1000).ToString("N", CultureInfo.CreateSpecificCulture("es-ES")) + "k";
                tb_cash2.Text = (t2_cash / 1000).ToString("N", CultureInfo.CreateSpecificCulture("es-ES")) + "k";

                if (per < 0)
                {
                    lb_cashdiff.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    lb_cashdiff.Foreground = System.Windows.Media.Brushes.Green;
                }
            }

            if (t1_army > 0 && t2_army > 0)
            {
                double diff = t1_army - t2_army;
                double per = diff / t1_army;
                lb_armydiff.Content = per.ToString("P", CultureInfo.InvariantCulture);
                tb_army1.Text = (t1_army / 1000).ToString("N", CultureInfo.CreateSpecificCulture("es-ES")) + "k";
                tb_army2.Text = (t2_army / 1000).ToString("N", CultureInfo.CreateSpecificCulture("es-ES")) + "k";

                if (per < 0)
                {
                    lb_armydiff.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    lb_armydiff.Foreground = System.Windows.Media.Brushes.Green;
                }
            }


            TimeSpan t = TimeSpan.FromSeconds(MMIDS[int.Parse(mmid)].DURATION / 22.4);
            lb_duration.Content = "Duration: " + t.Minutes + ":" + t.Seconds.ToString("D2") + " min";
            if (t.Hours > 0)
            {
                lb_duration.Content = "Duration: " + t.Hours + ":" + t.Minutes.ToString("D2") + ":" + t.Seconds.ToString("D2") + " min";
            }

            gr_summary.Visibility = Visibility.Visible;

        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            bt_show.IsEnabled = true;
            mmcb_report.IsEnabled = true;
            tb_gamefound.Text = "";


            if (CLIENT != null)
            {
                // Release the socket.    
                try
                {
                    string player = mmcb_player.SelectedItem.ToString();
                    string string1 = "Hello from [" + player + "]: fin" + "\r\n";
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

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {

                Win_mm mmtest = new Win_mm(MW);
                TESTWIN.Add(mmtest);

                if (mmcb_credential.IsChecked == true)
                {
                    mmtest.ClearReport();

                    mmtest.gr_accept.Visibility = Visibility.Hidden;
                    mmtest.tb_accepted.Text = "";

                    mmtest.lb_pl1.Content = "Player";
                    mmtest.lb_pl2.Content = "Player";
                    mmtest.lb_pl3.Content = "Player";
                    mmtest.lb_pl4.Content = "Player";
                    mmtest.lb_pl5.Content = "Player";
                    mmtest.lb_pl6.Content = "Player";
                    mmtest.tb_mmid.Text = "0";

                    mmtest.ACCEPTED = false;
                    mmtest.DECLINED = false;
                    mmtest.ALL_ACCEPTED = false;
                    mmtest.ALL_DECLINED = false;
                    if (sender == null && mmtest.MM != null) mmtest.MM.STATUS = false;

                    mmtest.mmcb_randoms.IsEnabled = false;
                    mmtest.mmcb_randoms.IsChecked = false;
                    mmtest.mmcb_report.IsEnabled = false;

                    mmtest.mmcb_acc1.IsChecked = null;
                    mmtest.mmcb_acc2.IsChecked = null;
                    mmtest.mmcb_acc3.IsChecked = null;
                    mmtest.mmcb_acc4.IsChecked = null;
                    mmtest.mmcb_acc5.IsChecked = null;
                    mmtest.mmcb_acc6.IsChecked = null;

                    string player = "player" + (i + 2).ToString();
                    mmtest.mmcb_player.Items.Clear();
                    mmtest.mmcb_player.Items.Add(player);
                    mmtest.mmcb_player.SelectedItem = mmtest.mmcb_player.Items[0];


                    string mode = ((ComboBoxItem)mmtest.mmcb_mode.SelectedItem).Content.ToString();
                    string num = ((ComboBoxItem)mmtest.mmcb_num.SelectedItem).Content.ToString();
                    string skill = ((ComboBoxItem)mmtest.mmcb_skill.SelectedItem).Content.ToString();
                    string server = ((ComboBoxItem)mmtest.mmcb_server.SelectedItem).Content.ToString();

                    if (sender != null)
                    {
                        mmtest.tb_gamefound.Text = "Connecting ...";
                        mmtest.mmcb_ample.IsChecked = true;
                        mmtest.mmcb_ample.Content = "Online";
                        Task FindGame = Task.Factory.StartNew(() =>
                        {
                            dsmmclient mm = new dsmmclient(mmtest);
                            mmtest.MM = mm;
                            Socket sock = mm.StartClient(mmtest, player, mode, num, skill, server);
                            MW.Dispatcher.Invoke(() =>
                            {
                                mmtest.mmcb_ample.IsChecked = false;
                                mmtest.mmcb_ample.Content = "Offline";
                                try
                                {
                                    mmtest._timer.Stop();
                                }
                                catch
                                {

                                }
                            });
                        }, TaskCreationOptions.AttachedToParent);

                        //tb_info.Text = mm.INFO;

                        mmtest._time = TimeSpan.FromSeconds(1);

                        mmtest._timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
                    {
                        mmtest.tb_time.Text = _time.ToString("c");

                        // fill with randoms after ~5min
                        if (mmtest._time == TimeSpan.FromMinutes(1))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                mmtest.mmcb_randoms.IsEnabled = true;
                                mmtest.tb_gamefound.Text += " You can fill your lobby with randoms now if you want (check 'allow Randoms' right to the clock)";
                            });
                        }
                        mmtest._time = _time.Add(TimeSpan.FromSeconds(+1));
                    }, Application.Current.Dispatcher);

                        mmtest._timer.Start();

                        mmtest.bt_show.IsEnabled = false;
                    }
                    else
                    {
                        try
                        {
                            mmtest._timer.Start();
                        }
                        catch
                        {

                        }
                    }

                }
                else
                {
                    mmcb_credential_Click(sender, e);
                    if (mmcb_credential.IsChecked == true)
                    {
                        Button_Click(sender, e);
                    }
                }

                mmtest.WindowStartupLocation = WindowStartupLocation.Manual;
                mmtest.Left = i * 850 - 1700;
                if (i >= 6)
                {
                    mmtest.Left = (i-7) * 850 - 1700;
                }
                if (i < 2 ) mmtest.Top = 150;
                else if  (i <= 6) mmtest.Top = 5;
                else mmtest.Top = 465;
                mmtest.Show();
            }

        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            foreach (Win_mm mmtest in TESTWIN)
            {
                mmtest.Button_Click(sender, e);
            }
        }
    }
}
