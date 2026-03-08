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
    /// Interaction logic for Exit.xaml
    /// </summary>
    public partial class Exit : Window
    {
        public Exit(CMain main,int i)
        {
            InitializeComponent();
            this.i = i;
            this.main = main;
            if(i == 0)
            {
                Logout.Visibility = Visibility.Collapsed;
                this.Height = 280;
            }
            else
            {
                foreach (Role r in main.main.lr)
                {
                    if (r.RoleID == main.u.RoleID)
                    {
                        if (r.Logout == false)
                        {
                            Logout.IsEnabled = false;
                        }
                        if (r.ViewShutDown == false)
                        {
                            ShutDown.IsEnabled = false;
                        }
                        if (r.ViewExit == false)
                        {
                            Exitt.IsEnabled = false;
                        }
                    }
                }
            }
                
        }
        int i; CMain main;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Logout_Click_1(object sender, RoutedEventArgs e)
        {
            main.main.load_Login();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("shutdown", "/s /t 0");

        }
    }
}
