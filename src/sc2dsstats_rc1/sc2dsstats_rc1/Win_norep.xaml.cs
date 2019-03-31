using System;
using System.Collections.Generic;
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
    /// Interaktionslogik für Win_norep.xaml
    /// </summary>
    public partial class Win_norep : Window
    {
        Win_mm WM { get; set; }

        public Win_norep()
        {
            InitializeComponent();
        }

        public Win_norep(Win_mm wm, string mmid) : this()
        {
            WM = wm;
            tb_mmid.Text = mmid;
        }

        private void bt_send_Click(object sender, RoutedEventArgs e)
        {
            string blame = "";
            try
            {
                blame = ((ComboBoxItem)cb_blame.SelectedItem).Content.ToString();
            }
            catch
            {
                blame = ((TextBox)cb_blame.SelectedItem).Text;
            }


            WM.SendResult("mmid: " + tb_mmid.Text + "; blame:" + blame);
        }
    }
}
