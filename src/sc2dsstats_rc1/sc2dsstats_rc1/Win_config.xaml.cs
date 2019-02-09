using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace sc2dsstats_rc1
{
    /// <summary>
    /// Interaktionslogik für Win_config.xaml
    /// </summary>
    public partial class Win_config : Window
    {
        public Win_config()
        {
            InitializeComponent();
            RoutedEventArgs e = new RoutedEventArgs();
            Win_config_global_button_Click(this, e);
        }

        List<string> myKeys = new List<string>();

        private void Win_config_global_button_Click(object sender, RoutedEventArgs e)
        {
            var appSettings = ConfigurationManager.AppSettings;


            win3_dataGrid.ItemsSource = appSettings.Keys;
            win3_dataGrid.ItemsSource = LoadCollectionData();

            var column = win3_dataGrid.Columns[0];

            // Clear current sort descriptions
            win3_dataGrid.Items.SortDescriptions.Clear();

            // Add the new sort description
            win3_dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Ascending));

            // Apply sort
            foreach (var col in win3_dataGrid.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = ListSortDirection.Ascending;

            // Refresh items to display sort
            win3_dataGrid.Items.Refresh();

            foreach (var key in appSettings.AllKeys) {

                myKeys.Add(key.ToString() + " => " + appSettings[key].ToString());

                Label myLabel = new Label();
                myLabel.Content = appSettings[key].ToString();






            }
        }

        public class myConfig
        {
            public string key { get; set; }
            public string value { get; set; }
            public string info { get; set; }
        }

        private List<myConfig> LoadCollectionData()
        {
            List<myConfig> configs = new List<myConfig>();
            var appSettings = ConfigurationManager.AppSettings;
            string cdesc = "Und es war Sommer";

            foreach (var ckey in appSettings.AllKeys)
            {
                cdesc = "";
                if (String.Equals(ckey, "REPLAY_PATH"))
                    cdesc = "# Path where all the replays are located";
                if (String.Equals(ckey, "PLAYER"))
                    cdesc = "# Starcraft 2 Player name (without Clan tags)";
                if (String.Equals(ckey, "SKIP_STD"))
                    cdesc = "# If you want to skip stats for normal games (Zerg, Terran, Protoss) set this to 1";
                if (String.Equals(ckey, "SKIP"))
                    cdesc = "# Skip games with gametime below 240sec";
                if (String.Equals(ckey, "START_DATE"))
                    cdesc = "# start_date - only compute replays with timestamp greater than it - format YYYYmmDDHHMMSS";
                if (String.Equals(ckey, "END_DATE"))
                    cdesc = "# end_date - only compute replays with timestamp lower than it - format YYYYmmDDHHMMSS";
                if (String.Equals(ckey, "DEBUG"))
                    cdesc = "# Debug level (2=all, 1=info, 0=error only)";
                if (String.Equals(ckey, "SKIP_MSG"))
                    cdesc = "# Activate Skipmessage - If you type 'skipdsstats' in the ingame chat in the first 60sec of the game it will not be computed.";
                if (String.Equals(ckey, "FIRST_RUN"))
                    cdesc = "# Enables first run welcome message";
                if (String.Equals(ckey, "BETA"))
                    cdesc = "# Skip old DS replays (Desert Strike Co-op Beta) (0 to disable)";
                if (String.Equals(ckey, "HOTS"))
                    cdesc = "# Skip old DS replays (Desert Strike HotS) (0 to disable)";
                if (String.Equals(ckey, "DURATION"))
                    cdesc = "# Skip games with game duration below. 53776 => 240sec (/22.4) (0 to disable)";
                if (String.Equals(ckey, "LEAVER"))
                    cdesc = "# Skip games with at least one player left the given time before the game endss. 2000 => 90sec (/22.4) (0 to disable)";
                if (String.Equals(ckey, "KILLSUM"))
                    cdesc = "# Skip games with at least one player has less killed army value (0 to disable)";
                if (String.Equals(ckey, "INCOME"))
                    cdesc = "# Skip games with at least one player has less income (0 to disable)";
                if (String.Equals(ckey, "ARMY"))
                    cdesc = "# Skip games with at least one player has lass army value (0 to disable)";
                if (String.Equals(ckey, "KEEP"))
                    cdesc = "# Keep decoded replays (requires a lot of discspace! but you might not have to rescan after an update) (0 to disable)";
                if (String.Equals(ckey, "CORES"))
                    cdesc = "# Number of CPU cores used to decode replays";
                if (String.Equals(ckey, "STATS_FILE"))
                    cdesc = "# Databasefile";
                if (String.Equals(ckey, "SKIP_FILE"))
                    cdesc = "# Skipfile";
                if (String.Equals(ckey, "STORE_PATH"))
                    cdesc = "# Directory for the encoded replays (expect ~700MB for every 100 DS-Replays if KEEP=1)";
                configs.Add(new myConfig()
                {
                    key = ckey,
                    value = appSettings[ckey],
                    info = cdesc

                });
            }
            return configs;
        }

        private void Win_config_save_button_Click(object sender, RoutedEventArgs e)
        {
            win3_dataGrid.SelectAll();

            Int32 selectedCellCount = win3_dataGrid.SelectedItems.Count;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var appSettings = ConfigurationManager.AppSettings;
            for (int i = 0; i < selectedCellCount; i++)
            {

                string row = "bab";
                row = win3_dataGrid.SelectedItems[i].ToString();
                if (string.Equals("{NewItemPlaceholder}", row))
                {

                }
                else
                {

                    myConfig myCfg = (myConfig)win3_dataGrid.SelectedItems[i];

                    sb.Append(myCfg.key.ToString());
                    sb.Append(" => ");
                    sb.Append(myCfg.value.ToString());
                    sb.Append(Environment.NewLine);


                    if (String.Equals(myCfg.value.ToString(), appSettings[myCfg.key]))
                    {


                    }
                    else
                    {
                        config.AppSettings.Settings.Remove(myCfg.key);
                        config.AppSettings.Settings.Add(myCfg.key, myCfg.value);
                        config.Save();

                    }
                }
            }
            ConfigurationManager.RefreshSection("appSettings");

            MessageBox.Show("Successfuly saved. :)");
        }
    }
}
