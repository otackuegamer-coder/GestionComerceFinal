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
    /// Logique d'interaction pour CUserManagment.xaml
    /// </summary>
    public partial class CUserManagment : UserControl
    {
        public CUserManagment(User u, List<User> lu, List<Role> lr,SettingsPage sp)
        {
            InitializeComponent();
            UserGrid.Children.Clear();
            this.u = u;
            this.lu = lu;
            this.lr = lr;
            this.sp = sp;
            
            foreach (Role r in sp.main.lr)
            {
                if (u.RoleID == r.RoleID)
                {
                    if (r.ViewUsers == false)
                    {
                        ViewUsers.Child=null;
                    }
                    else
                    {
                        Load_users();
                    }
                    if(r.AddUsers == false)
                    {
                        NewUserBtn.IsEnabled = false;
                    }
                    if (r.ViewRoles == false && r.AddRoles==false)
                    {
                        Roles.IsEnabled = false;
                    }
                    break;
                }
            }
        }
        public User u; List<User> lu; List<Role> lr; public SettingsPage sp;

        public void Load_users()
        {
            UserGrid.Children.Clear();  
            string RoleName = "";
            foreach (User user in lu)
            {
                foreach (Role role in lr)
                {
                    if (user.RoleID == role.RoleID)
                    {
                        RoleName = role.RoleName;
                        break;
                    }
                }
                CSingleUser singleUser = new CSingleUser(lu,lr, this,user, RoleName);
                UserGrid.Children.Add(singleUser);

            }
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            WAddUser addUserWindow = new WAddUser(lu,lr,this);
            addUserWindow.ShowDialog();
        }

        private void UserDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void RolesButton_Click(object sender, RoutedEventArgs e)
        {
            WRoles rolesWindow = new WRoles(lr,lu,this);
            rolesWindow.ShowDialog();
        }
    }
}
