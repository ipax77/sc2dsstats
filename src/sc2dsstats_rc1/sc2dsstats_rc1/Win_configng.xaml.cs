using System;
using System.Collections.Generic;
using System.Configuration;
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
    /// Interaktionslogik für Win_configng.xaml
    /// </summary>
    public partial class Win_configng : Window
    {

        public static int INCOME = 1500;
        public static int ARMY = 1500;
        public static int LEAVER = 2000;
        public static int DURATION = 5376;
        public static int KILLSUM = 1500;
        public static int CORES = 2;
        public static DateTime START_DATE = new DateTime(2018, 01, 01);
        public static DateTime END_DATE = DateTime.Today;
        public static int DEBUG = 0;

        public Win_configng()
        {
            InitializeComponent();
            Load();
            DEBUG = Properties.Settings.Default.DEBUG;
        }

        public void Load()
        {
            gr_cfg_main.Children.Clear();
            gr_cfg_main.RowDefinitions.Clear();
            gr_cfg_main.ColumnDefinitions.Clear();
            ColumnDefinition gridCol1 = new ColumnDefinition();
            gr_cfg_main.ColumnDefinitions.Add(gridCol1);
            ColumnDefinition gridCol2 = new ColumnDefinition();
            gr_cfg_main.ColumnDefinitions.Add(gridCol2);
            ColumnDefinition gridCol3 = new ColumnDefinition();
            gr_cfg_main.ColumnDefinitions.Add(gridCol3);

            int i = 0;
            foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
            {
                if (cb_all.IsChecked == false)
                {
                    if (currentProperty.Name.ToString() == "UPLOAD") continue;
                    if (currentProperty.Name.ToString().StartsWith("GUI_")) continue;
                    if (currentProperty.Name.ToString().StartsWith("MM_")) continue;
                    if (currentProperty.Name.ToString() == "V8") continue;
                    if (currentProperty.Name.ToString() == "FIRSTRUN") continue;
                }

                RowDefinition gridRow1 = new RowDefinition();
                gridRow1.Height = new GridLength(25);
                gr_cfg_main.RowDefinitions.Add(gridRow1);

                Label lb = new Label();
                lb.Style = (Style)Application.Current.Resources["lb_Style"];
                lb.Content = "_" + currentProperty.Name.ToString();

                TextBox tb = new TextBox();
                tb.Name = currentProperty.Name.ToString();
                tb.Style = (Style)Application.Current.Resources["TextBoxStyle1"];
                tb.MaxWidth = 400;
                tb.Text = Properties.Settings.Default[currentProperty.Name].ToString();

                Label lbi = new Label();
                lbi.Style = (Style)Application.Current.Resources["lb_Style"];
                lbi.Content = Info(currentProperty.Name.ToString());

                Grid.SetRow(lb, i);
                Grid.SetColumn(lb, 0);
                gr_cfg_main.Children.Add(lb);

                Grid.SetRow(tb, i);
                Grid.SetColumn(tb, 1);
                gr_cfg_main.Children.Add(tb);

                Grid.SetRow(lbi, i);
                Grid.SetColumn(lbi, 2);
                gr_cfg_main.Children.Add(lbi);

                //Console.WriteLine(currentProperty.Name + " => " + Properties.Settings.Default[currentProperty.Name]);
                i++;
            }
        }

        private string Info(string label)
        {
            string cdesc = "";
            if (label == "JSON_FILE") cdesc = "# Databasefile";
            else if (label == "PLAYER") cdesc = "# In game Starcraft 2 Player name (without Clan tags) (you can define multiple names with semikolon separated)";
            else if (label == "REPLAY_PATH") cdesc = "# Path where all the replays are located (you can define multiple directories with semikolon separated)";
            else if (label == "CORES") cdesc = "# Number of CPU cores used to decode replays";
            else if (label == "UPDATE") cdesc = "# Check for updates at Application start. (True, False)";
            else if (label == "DEBUG") cdesc = "# Debug level (0 = off, 1=basic, 2=all, 3=reset user settings";
            else if (label == "SKIP_FILE") cdesc = "# Skip file (replays that could not be decoded for some reason)";
            else if (label == "AUTOSCAN") cdesc = "# Scan replays at Application start. (True, False)";

            //cdesc += "_";
            return cdesc;
        }

        public static void SetConfigDefault(MainWindow mw)
        {
            mw.tb_army.Text = ARMY.ToString();
            mw.tb_income.Text = INCOME.ToString();
            mw.tb_leaver.Text = LEAVER.ToString();
            mw.tb_duration.Text = DURATION.ToString();
            mw.tb_killsum.Text = KILLSUM.ToString();

            mw.cb_army.IsChecked = true;
            mw.cb_income.IsChecked = true;
            mw.cb_leaver.IsChecked = true;
            mw.cb_duration.IsChecked = true;
            mw.cb_killsum.IsChecked = true;

            mw.cb_all.IsChecked = true;
            mw.gr_date.IsEnabled = false;
            mw.otf_startdate.SelectedDate = START_DATE;
            mw.otf_enddate.SelectedDate = DateTime.Today;

            // TODO
            mw.cb_std.IsChecked = false;
            mw.cb_hots.IsChecked = true;
            mw.cb_beta.IsChecked = true;
            mw.cb_player.IsChecked = true;
            mw.cb_yscale.IsChecked = false;

        }

        public static void SetConfig(MainWindow mw)
        {
            if (Properties.Settings.Default.GUI_LEAVER == 0)
            {
                mw.cb_leaver.IsChecked = false;
                mw.tb_leaver.IsEnabled = true;
            } else
            {
                mw.cb_leaver.IsChecked = true;
                mw.tb_leaver.Text = Properties.Settings.Default.GUI_LEAVER.ToString();
            }

            if (Properties.Settings.Default.GUI_INCOME == 0)
            {
                mw.cb_income.IsChecked = false;
                mw.tb_income.IsEnabled = true;
            }
            else
            {
                mw.cb_income.IsChecked = true;
                mw.tb_income.Text = Properties.Settings.Default.GUI_INCOME.ToString();
            }

            if (Properties.Settings.Default.GUI_ARMY == 0)
            {
                mw.cb_army.IsChecked = false;
                mw.tb_army.IsEnabled = true;
            }
            else
            {
                mw.cb_army.IsChecked = true;
                mw.tb_army.Text = Properties.Settings.Default.GUI_ARMY.ToString();
            }

            if (Properties.Settings.Default.GUI_KILLSUM == 0)
            {
                mw.cb_killsum.IsChecked = false;
                mw.tb_killsum.IsEnabled = true;
            }
            else
            {
                mw.cb_killsum.IsChecked = true;
                mw.tb_killsum.Text = Properties.Settings.Default.GUI_KILLSUM.ToString();
            }

            if (Properties.Settings.Default.GUI_DURATION == 0)
            {
                mw.cb_duration.IsChecked = false;
                mw.tb_duration.IsEnabled = true;
            }
            else
            {
                mw.cb_duration.IsChecked = true;
                mw.tb_duration.Text = Properties.Settings.Default.GUI_DURATION.ToString();
            }

            if (Properties.Settings.Default.GUI_START_DATE == new DateTime(2018, 01, 01))
            {
                mw.cb_all.IsChecked = true;
            } else
            {
                mw.cb_all.IsChecked = false;
                mw.gr_date.IsEnabled = true;
                mw.otf_startdate.SelectedDate = Properties.Settings.Default.GUI_START_DATE;
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var ent in gr_cfg_main.Children)
            {
                if (ent is TextBox)
                {
                    TextBox tb = ent as TextBox;
                    if (tb.Text == Properties.Settings.Default[tb.Name].ToString())
                    {
                        if (DEBUG > 0) Console.WriteLine("Nothing changed for " + tb.Name);
                    } else {

                        dynamic value = null;
                        if (tb.Name == "MM_CREDENTIALS" || tb.Name == "V8" || tb.Name == "GUI_STD" || tb.Name == "FIRSTRUN" || tb.Name == "UPDATE")
                        {
                            try
                            {
                                value = Boolean.Parse(tb.Text);
                            }
                            catch { Console.WriteLine("Failed saving config for " + tb.Name + " => " + tb.Text); }
                        }

                        else if (tb.Name == "MM_Deleted" || tb.Name == "GUI_START_DATE" || tb.Name == "GUI_END_DATE" || tb.Name == "UPLOAD" || tb.Name == "AUTOSCAN")
                        {
                            try
                            {
                                value = DateTime.Parse(tb.Text);
                            } catch { Console.WriteLine("Failed saving config for " + tb.Name + " => " + tb.Text); }
                        }

                        else if (tb.Name.StartsWith("GUI") || tb.Name == "CORES" || tb.Name == "DEBUG")
                        {
                            try
                            {
                                value = int.Parse(tb.Text);
                            } catch { Console.WriteLine("Failed saving config for " + tb.Name + " => " + tb.Text); }
                        }

                        else
                        {
                            value = tb.Text;
                        }

                        if (value != null)
                        {
                            try
                            {
                                Properties.Settings.Default[tb.Name] = value;
                                if (DEBUG > 0) Console.WriteLine("New config value: " + tb.Name + " => " + tb.Text);
                            } catch
                            {
                                if (DEBUG > 0) Console.WriteLine("New config value failed: " + tb.Name + " => " + tb.Text);
                            }
                        }
                    }
                }
            }
            Properties.Settings.Default.Save();
            MessageBox.Show("Settings saved.", "sc2dsstats");
        }

        private void Cb_all_Click(object sender, RoutedEventArgs e)
        {
            Load();
        }
    }
}
