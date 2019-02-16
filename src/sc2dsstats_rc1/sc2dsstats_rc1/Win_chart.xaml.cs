using System;
using System.Collections.Generic;
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

namespace sc2dsstats_rc1
{
    /// <summary>
    /// Interaktionslogik für Win_chart.xaml
    /// </summary>
    public partial class Win_chart : Window
    {
        MainWindow MW { get; set; }

        public Win_chart()
        {
            InitializeComponent();
            
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
    }


}
