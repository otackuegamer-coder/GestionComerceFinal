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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestionComerce.Main.Settings
{
    /// <summary>
    /// Logique d'interaction pour CSingleRole.xaml
    /// </summary>
    public partial class CSingleRole : UserControl
    {
        public CSingleRole(Role r,WRoles Roles,List<Role> lr, List<User> lu)
        {
            InitializeComponent();
            this.r = r; this.Roles = Roles; this.lr = lr; this.lu = lu;
            RoleName.Text = r.RoleName;
            foreach (Role rr in Roles.CUM.sp.main.lr)
            {
                if (Roles.CUM.u.RoleID == rr.RoleID)
                {
                    if (rr.DeleteRoles == false)
                    {
                        DeleteButton.IsEnabled = false;
                    }
                    break;
                }
            }

        }
        Role r; WRoles Roles; List<Role> lr; List<User> lu;


        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            WDeleteRole deleteRoleWindow = new WDeleteRole(r, Roles, lr, lu);
            deleteRoleWindow.ShowDialog();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {

            WAddRole addRoleWindow = new WAddRole(Roles, lr,r, 1);
            addRoleWindow.ShowDialog();
        }
    }
}
