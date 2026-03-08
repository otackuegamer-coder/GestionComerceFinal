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

namespace GestionComerce.Main
{
    /// <summary>
    /// Interaction logic for WCongratulations.xaml
    /// </summary>
    public partial class WCongratulations : Window
    {
        public WCongratulations(string Headerr,string Messagee,int s)
        {
            InitializeComponent();
            if (s == 0)
            {
                SuccessIcon.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#fa1302");
                btn.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#fa1302");
            }
            else if(s == 1) {


                SuccessIcon.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#10B981");
                btn.Background =(SolidColorBrush)new BrushConverter().ConvertFromString("#10B981");
            }

            Header.Text = Headerr;
            Message.Text = Messagee;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Window win in Application.Current.Windows.Cast<Window>().ToList())
            {
                if (win != Application.Current.MainWindow) // keep main window open
                {
                    win.Close();
                }
            }
        }
    }
}
