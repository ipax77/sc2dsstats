using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows;

namespace sc2dsstats_rc1
{
    public class dsimage : INotifyPropertyChanged
    {
        //public BitmapImage _ImageLocation = new BitmapImage(new Uri("dummy.png", UriKind.Relative));
        private string _ImageLocation;
        public event PropertyChangedEventHandler PropertyChanged;

        public string ImageLocation
        {
            get
            {
                return _ImageLocation;
            }
            set
            {
                _ImageLocation = value;
                OnPropertyChanged(ImageLocation);
            }
        }

        public dsimage()
        {
            ImageLocation = "dummy.png";
        }

        public dsimage(string cmdr)
        {
            ImageLocation = GetImage(cmdr);
        }

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public Image CreateImage (string path)
        {
            Image img = new Image();
            BitmapImage bimg = new BitmapImage();
            bimg.BeginInit();
            bimg.UriSource = new Uri(path, UriKind.Relative);
            bimg.EndInit();
            img.Source = bimg;
            return img;
        }

        public void ShowImages (MainWindow mw, List<KeyValuePair<string, double>> list)
        {

            mw.gr_images.Children.Clear();
            mw.gr_images.RowDefinitions.Clear();
            mw.gr_images.ColumnDefinitions.Clear();
            RowDefinition gridRow1 = new RowDefinition();
            gridRow1.Height = new GridLength(60);
            mw.gr_images.RowDefinitions.Add(gridRow1);
            double max = 0;
            foreach (var bab in list)
            {
                ColumnDefinition gridCol1 = new ColumnDefinition();
                mw.gr_images.ColumnDefinitions.Add(gridCol1);
                if (bab.Value > max) max = bab.Value;
            }

            max /= 10;
            int i = 0;
            foreach (var bab in list) {
                Image img = CreateImage(GetImage(bab.Key.ToString()));
                if (bab.Value < max && bab.Value != 0) img = CreateImage(GetImage("bab"));
                Grid.SetRow(img, 0);
                Grid.SetColumn(img, i);
                mw.gr_images.Children.Add(img);
                i++;
            }
            mw.gr_images.Visibility = System.Windows.Visibility.Visible;


        }

        public string GetImage(string cmdr)
        {
            BitmapImage imgsrc = null;
            string png = "dummy.png";
            if (cmdr.Contains("Abathur"))
            {
                png = "btn-unit-hero-abathur.png";
            } else if (cmdr.Contains("Alarak")) {
                png = "btn-unit-hero-alarak.png";
            }
            else if (cmdr.Contains("Artanis"))
            {
                png = "btn-unit-hero-artanis.png";
            }
            else if (cmdr.Contains("Dehaka"))
            {
                png = "btn-unit-hero-dehaka.png";
            }
            else if (cmdr.Contains("Fenix"))
            {
                png = "btn-unit-hero-fenix.png";
            }
            else if (cmdr.Contains("Horner"))
            {
                png = "btn-unit-hero-horner.png";
            }
            else if (cmdr.Contains("Karax"))
            {
                png = "btn-unit-hero-karax.png";
            }
            else if (cmdr.Contains("Kerrigan"))
            {
                png = "btn-unit-hero-kerrigan.png";
            }
            else if (cmdr.Contains("Nova"))
            {
                png = "btn-unit-hero-nova.png";
            }
            else if (cmdr.Contains("Raynor"))
            {
                png = "btn-unit-hero-raynor.png";
            }
            else if (cmdr.Contains("Stukov"))
            {
                png = "btn-unit-hero-stukov.png";
            }
            else if (cmdr.Contains("Swann"))
            {
                png = "btn-unit-hero-swann.png";
            }
            else if (cmdr.Contains("Tychus"))
            {
                png = "btn-unit-hero-tychus.png";
            }
            else if (cmdr.Contains("Vorazun"))
            {
                png = "btn-unit-hero-vorazun.png";
            }
            else if (cmdr.Contains("Zagara"))
            {
                png = "btn-unit-hero-zagara.png";
            }
            else if (cmdr.Contains("Protoss"))
            {
                png = "dummy.png";
            }
            else if (cmdr.Contains("Terran"))
            {
                png = "dummy.png";
            }
            else if (cmdr.Contains("Zerg"))
            {
                png = "dummy.png";
            }
            imgsrc = new BitmapImage(new Uri(png, UriKind.Relative));
            //Console.WriteLine(png + " => " + cmdr);
            //ImageLocation = imgsrc;
            ImageLocation = "images\\" + png;
            
            return ImageLocation;
        }

        public static class MyExtensions
        {
            public static void AddValue(string arr, string bab)
            {

            }
        }

        public class StrToImageConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                //DateTime date = (DateTime)value;
                //return date.ToShortDateString();
                string bab = (string)value;
                dsimage dsimg = new dsimage();
                bab = dsimg.GetImage(bab);
                return bab;

            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                string strValue = value as string;
                DateTime resultDateTime;
                if (DateTime.TryParse(strValue, out resultDateTime))
                {
                    return resultDateTime;
                }
                return System.Windows.DependencyProperty.UnsetValue;
            }
        }


    }
}
