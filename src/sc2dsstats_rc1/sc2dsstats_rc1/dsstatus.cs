using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace sc2dsstats_rc1
{
    class dsstatus
    {

        MainWindow MW { get; set; }
        StatusBar SB { get; set; }

        public dsstatus(MainWindow mw)
        {
            MW = mw;
            StatusBar sb = mw.sb_status;
            SB = sb;
        }
        public ProgressBar Set ()
        {
            MW.Dispatcher.Invoke(() =>
            {
                try
                {
                    foreach (UIElement ent in MW.gr_sb_grid.Children)
                    {
                        //MW.gr_sb_grid.Children.Remove(ent);
                        MW.progbar.Value = 0;
                        MW.progbar.Visibility = Visibility.Visible;
                    }
                }
                catch { }
            });
            return MW.progbar;
        }
    }
}
