using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
        public MainWindow MW { get; set; }
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
            string curDir = Directory.GetCurrentDirectory();

            string myhtml =
"<!DOCTYPE html>" + Environment.NewLine +
"<html>" + Environment.NewLine +
"<head>" + Environment.NewLine +
"<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" charset=\"UTF-8\" />" + Environment.NewLine +
"<title> externes JavaScript in HTML einbinden</title>" + Environment.NewLine +
"</head>" + Environment.NewLine +
"<script src=\"" + curDir + "/Scripts/Chart.min.js" + "\"></script>" + Environment.NewLine +
"<body>" + Environment.NewLine +
"<div id=\"chartContainer\"></div >" + Environment.NewLine +
"<div style=\"width:100%\">" + Environment.NewLine +
"<canvas id=\"cannvas\"></canvas>" + Environment.NewLine +
"</div>" + Environment.NewLine +
"<script type=\"text/javascript\">" + Environment.NewLine +
"var randomScalingFactor = function() {" + Environment.NewLine +
"return Math.round(Math.random() * 100);" + Environment.NewLine +
"};" + Environment.NewLine +
"var ctx=document.getElementById(\"cannvas\").getContext(\"2d\");" + Environment.NewLine +
"var color = Chart.helpers.color;" + Environment.NewLine +
"var config = {" + Environment.NewLine +
"type: 'radar'," + Environment.NewLine +
"data:" + Environment.NewLine +
"{" + Environment.NewLine +
"labels: [['Eating', 'Dinner'], ['Drinking', 'Water'], 'Sleeping', ['Designing', 'Graphics'], 'Coding', 'Cycling', 'Running']," + Environment.NewLine +
"datasets: [{" + Environment.NewLine +
"label: 'My First dataset'," + Environment.NewLine +
"backgroundColor: 'rgba(255, 99, 132, 0.2)'," + Environment.NewLine +
"borderColor: 'rgba(255,99,132,1)'," + Environment.NewLine +
"pointBackgroundColor: 'rgba(255, 159, 64, 0.2)'," + Environment.NewLine +
"data: [" + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()" + Environment.NewLine +
"]" + Environment.NewLine +
"}, {" + Environment.NewLine +
"label: 'My Second dataset'," + Environment.NewLine +
"backgroundColor: 'rgba(54, 162, 235, 0.2)'," + Environment.NewLine +
"borderColor: 'rgba(54, 162, 235, 1)'," + Environment.NewLine +
"pointBackgroundColor: 'rgba(153, 102, 255, 0.2)'," + Environment.NewLine +
"data: [" + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()," + Environment.NewLine +
"randomScalingFactor()" + Environment.NewLine +
"]" + Environment.NewLine +
"}]" + Environment.NewLine +
"}," + Environment.NewLine +
"options: {" + Environment.NewLine +
"legend: {" + Environment.NewLine +
"position: 'top'," + Environment.NewLine +
"}," + Environment.NewLine +
"title: {" + Environment.NewLine +
"display: true," + Environment.NewLine +
"text: 'Chart.js Radar Chart'" + Environment.NewLine +
"}," + Environment.NewLine +
"scale: {" + Environment.NewLine +
"ticks: {" + Environment.NewLine +
"beginAtZero: true" + Environment.NewLine +
"}" + Environment.NewLine +
"}" + Environment.NewLine +
"}" + Environment.NewLine +
"};	" + Environment.NewLine +
"var myChart = new Chart(ctx, config);" + Environment.NewLine +
"</script>" + Environment.NewLine +
"</body>" + Environment.NewLine +
"</html>" + Environment.NewLine;


            //dsradar myradar = new dsradar();
            //myhtml = myradar.GenerateHTML(MW.Items.ToList());
            //wb_chart.NavigateToString(myhtml);

        }

        public Win_chart(MainWindow mw) : this()
        {
            MW = mw;
            //LoadAreaChartData();

            LoadHTML();

        }

        private void LoadAreaChartData()
        {
            //chart_area.ItemsSource = mw.Items;
            chart_area.ItemsSource = MW.Items;

            //Series ds = new Series();
        }

        public void LoadHTML()
        {


            DSfilter dsfil = new DSfilter(MW);
            List<dsreplay> filtered_replays = new List<dsreplay>();
            filtered_replays = dsfil.Filter(MW.replays);

            dsradar myradar = new dsradar();
            //string myhtml = myradar.GetHTML("Swann", filtered_replays);
            //Console.WriteLine(myhtml);
            //wb_chart.NavigateToString(myhtml);



        }

    }


}
