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
    /// Logique d'interaction pour WDeleteRole.xaml
    /// </summary>
    public partial class WDeleteRole : Window
    {
        public WDeleteRole(Role r, WRoles Roles, List<Role> lr, List<User> lu)
        {
            InitializeComponent();
            this.r = r; this.Roles = Roles; this.lr = lr; this.lu = lu;

        }
        Role r; WRoles Roles; List<Role> lr; List<User> lu;
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void ConfirmButton_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lu.Where(u => u.RoleID == r.RoleID).Count() > 0)
                {
                    MessageBox.Show("Impossible de supprimer ce rôle car des utilisateurs y sont encore associés.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                List<Role> newR = lr;
                await r.DeleteRoleAsync();
                newR.Remove(r);
                Roles.LoadRoles();

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
