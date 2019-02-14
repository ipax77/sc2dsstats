using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace sc2dsstats_rc1
{
    /// <summary>
    /// Interaktionslogik für Win_log.xaml
    /// </summary>
    public partial class Win_log : Window
    {
        public Win_log()
        {
            InitializeComponent();
        }

        public void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Console.WriteLine("Und es war Sommer");
            Console.WriteLine(e.Uri.AbsoluteUri);
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
