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
                        MW.gr_sb_grid.Children.Remove(ent);
                    }
                }
                catch { }
            });

            ProgressBar progbar = new ProgressBar();
            progbar.IsIndeterminate = false;
            progbar.Orientation = Orientation.Horizontal;
            progbar.Width = 150;
            progbar.Height = 20;
            progbar.HorizontalAlignment = HorizontalAlignment.Left;
            progbar.Name = "pb_scan";
            MW.gr_sb_grid.Children.Add(progbar);
            return progbar;
        }

    }
}
