using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static sc2dsstats_rc1.dsimage;

namespace sc2dsstats_rc1
{
    /// <summary>
    /// Interaktionslogik für Win_chart.xaml
    /// </summary>
    public partial class Win_chart : Window
    {
        MainWindow MW { get; set; }
        dsimage dsimg = new dsimage();
        public string DisplayedImage
        {
            get { return "Und es war SOmmer"; }
        }


        public Win_chart()
        {
            InitializeComponent();
            /**
            this.DataContext = new AuctionItem("Type your description here",
                ProductCategory.DVDs, 
                1,
                DateTime.Now,
                SpecialFeatures.None);
            **/
//            dsimg.GetImage("Swann (103)");
// this.DataContext = dsimg;

            //MessageBox.Show(dsimage._ImageLocation.ToString());



        }

        public Win_chart(MainWindow mw) : this()
        {
            MW = mw;
            LoadAreaChartData();
        }

        private void LoadAreaChartData()
        {
            //chart_area.ItemsSource = mw.Items;
            chart_area.ItemsSource = MW.Items;

            //Series ds = new Series();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            dsimg.GetImage("Swann (103)");
            img_test1.UpdateLayout();
            img_test2.UpdateLayout();
        }
    }


}
