using sc2dsstats.lib.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace sc2dsstats.lib.Models
{
    public class DSdyn
    {
        public ObservableCollection<CmdrIcon> CmdrIcons { get; set; } = new ObservableCollection<CmdrIcon>();
        public List<CmdrIcon> ModifiedItems { get; set; }
        public string chartdata { get; set; }
        public static int gameid { get; set; } = 0;

        public DSdyn()
        {
            //CmdrIcons.CollectionChanged += CmdrIconsChanged;
            //foreach (string race in DSdata.s_races)
            foreach (string race in DSdata.s_races)
            {
                CmdrIcon icon = new CmdrIcon(race, false);
                CmdrIcons.Add(icon);
            }

        }

        public int GetChecked()
        {
            int i = 0;
            foreach (var icon in CmdrIcons)
            {
                i++;
                if (icon.IsChecked == true)
                {
                    return i;
                }
            }
            return i;
        }

        void CmdrIconsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CmdrIcon newItem in e.NewItems)
                {
                    ModifiedItems.Add(newItem);

                    //Add listener for each item on PropertyChanged event
                    newItem.PropertyChanged += this.OnItemPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (CmdrIcon oldItem in e.OldItems)
                {
                    ModifiedItems.Add(oldItem);

                    oldItem.PropertyChanged -= this.OnItemPropertyChanged;
                }
            }
        }

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CmdrIcon item = sender as CmdrIcon;
            if (item != null)
                ModifiedItems.Add(item);
        }



    }

    public class DSdyn_BuildChecked : INotifyPropertyChanged
    {
        private bool IsChecked_value = false;
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public bool IsChecked
        {
            get { return this.IsChecked_value; }
            set
            {
                if (value != this.IsChecked_value)
                {
                    this.IsChecked_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DSdyn_BuildChecked() { }
        public DSdyn_BuildChecked(bool ent)
        {
            IsChecked = ent;
        }
    }

    public class DSdyn_buildoptions : INotifyPropertyChanged
    {
        private string BUILD_value = String.Empty;
        private string BUILD_COMPARE_value = String.Empty;
        private string CMDR_value = String.Empty;
        private string CMDR_VS_value = String.Empty;
        private string BREAKPOINT_value = String.Empty;


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DSdyn_buildoptions()
        {

        }

        public string BUILD
        {
            get { return this.BUILD_value; }
            set
            {
                if (value != this.BUILD_value)
                {
                    this.BUILD_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string BUILD_COMPARE
        {
            get { return this.BUILD_COMPARE_value; }
            set
            {
                if (value != this.BUILD_COMPARE_value)
                {
                    this.BUILD_COMPARE_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string CMDR
        {
            get { return this.CMDR_value; }
            set
            {
                if (value != this.CMDR_value)
                {
                    this.CMDR_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string CMDR_VS
        {
            get { return this.CMDR_VS_value; }
            set
            {
                if (value != this.CMDR_VS_value)
                {
                    this.CMDR_VS_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string BREAKPOINT
        {
            get { return this.BREAKPOINT_value; }
            set
            {
                if (value != this.BREAKPOINT_value)
                {
                    this.BREAKPOINT_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }

    public class DSdyn_databaseoptions : INotifyPropertyChanged
    {
        private bool WINNER_value = false;
        private bool DURATION_value = false;
        private bool MAXLEAVER_value = false;
        private bool MAXKILLSUM_value = false;
        private bool REPLAY_value = false;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool ID { get; set; } = true;
        public bool Gametime { get; set; } = true;

        public bool WINNER
        {
            get { return this.WINNER_value; }
            set
            {
                if (value != this.WINNER_value)
                {
                    this.WINNER_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool DURATION
        {
            get { return this.DURATION_value; }
            set
            {
                if (value != this.DURATION_value)
                {
                    this.DURATION_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool MAXLEAVER
        {
            get { return this.MAXLEAVER_value; }
            set
            {
                if (value != this.MAXLEAVER_value)
                {
                    this.MAXLEAVER_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool MAXKILLSUM
        {
            get { return this.MAXKILLSUM_value; }
            set
            {
                if (value != this.MAXKILLSUM_value)
                {
                    this.MAXKILLSUM_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool REPLAY
        {
            get { return this.REPLAY_value; }
            set
            {
                if (value != this.REPLAY_value)
                {
                    this.REPLAY_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DSdyn_databaseoptions()
        {
            //var ent = this.GetType().GetProperty("REPLAY").GetValue("REPLAY");
            //ent = false;

        }
    }

    public class DSdyn_options : INotifyPropertyChanged
    {
        private string MODE_value = String.Empty;
        private string STARTDATE_value = String.Empty;
        private string ENDDATE_value = String.Empty;
        private string INTEREST_value = String.Empty;
        private int ICONS_value = 0;
        private bool PLAYER_value = false;
        private bool BEGINATZERO_value = false;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string MODE
        {
            get { return this.MODE_value; }
            set
            {
                if (value != this.MODE_value)
                {
                    this.MODE_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string STARTDATE
        {
            get { return this.STARTDATE_value; }
            set
            {
                if (value != this.STARTDATE_value)
                {
                    this.STARTDATE_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string ENDDATE
        {
            get { return this.ENDDATE_value; }
            set
            {
                if (value != this.ENDDATE_value)
                {
                    this.ENDDATE_value = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string INTEREST
        {
            get { return this.INTEREST_value; }
            set
            {
                if (value != this.INTEREST_value)
                {
                    this.INTEREST_value = value;
                    //NotifyPropertyChanged();
                }
            }
        }
        public int ICONS
        {
            get { return this.ICONS_value; }
            set
            {
                if (value != this.ICONS_value)
                {
                    this.ICONS_value = value;
                    //NotifyPropertyChanged();
                }
            }
        }
        public bool PLAYER
        {
            get { return this.PLAYER_value; }
            set
            {
                if (value != this.PLAYER_value)
                {
                    this.PLAYER_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool BEGINATZERO
        {
            get { return this.BEGINATZERO_value; }
            set
            {
                if (value != this.BEGINATZERO_value)
                {
                    this.BEGINATZERO_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DSdyn_options()
        {
            MODE = "Winrate";
            STARTDATE = "0";
            ENDDATE = "0";
        }
    }

    public class CmdrIcon : INotifyPropertyChanged
    {
        private bool IsChecked_value = false;
        public event PropertyChangedEventHandler PropertyChanged;

        public CmdrIcon(string _ID, bool _IsChecked)
        {
            ID = _ID;
            IsChecked = _IsChecked;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string ID { get; set; }
        public bool IsChecked
        {
            get
            {
                return this.IsChecked_value;
            }
            set
            {
                if (value != this.IsChecked_value)
                {
                    this.IsChecked_value = value;
                    NotifyPropertyChanged();
                }
            }
        }

    }
}
