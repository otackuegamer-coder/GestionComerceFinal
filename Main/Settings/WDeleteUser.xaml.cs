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

namespace GestionComerce.Main.Settings
{
    /// <summary>
    /// Logique d'interaction pour WDeleteUser.xaml
    /// </summary>
    public partial class WDeleteUser : Window
    {
        public WDeleteUser(List<User> lu, CUserManagment CUM,User u)
        {
            InitializeComponent();
            this.lu = lu;
            this.CUM = CUM;
            this.u = u;
        }
        List<User> lu;
        CUserManagment CUM;
        User u;


        private void ConfirmButton_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<User> newU = lu;
                await u.DeleteUserAsync(MainWindow.ApiClient);
                newU.Remove(u);
                CUM.Load_users();
                //this.Close();

                WCongratulations wCongratulations = new WCongratulations("Suppression réussite", "Suppression a ete effectue avec succes", 1);
                wCongratulations.ShowDialog();

            }
            catch (Exception ex)
            {
                WCongratulations wCongratulations = new WCongratulations("Suppression échoué", "Suppression n'a pas ete effectue ", 0);
                wCongratulations.ShowDialog();
            }
        }
    }
}
