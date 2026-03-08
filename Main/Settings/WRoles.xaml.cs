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
    /// Logique d'interaction pour WRoles.xaml
    /// </summary>
    public partial class WRoles : Window
    {
        public WRoles(List<Role> lr, List<User> lu,CUserManagment CUM)
        {
            InitializeComponent();
            this.lu = lu;
            this.lr = lr;
            this.CUM = CUM;
            foreach (Role r in CUM.sp.main.lr)
            {
                if (CUM.u.RoleID == r.RoleID)
                {
                    if (r.ViewRoles == true)
                    {
                        LoadRoles();
                    }
                    if (r.AddRoles == true)
                    {
                        AddRoles.IsEnabled = true;
                    }
                    break;
                }
            }
            

        }
        List<Role> lr; List<User> lu; public CUserManagment CUM;
        public void LoadRoles()
        {
            RolesList.Children.Clear();
            foreach (Role role in lr)
            {
                
                CSingleRole cSingleRole = new CSingleRole(role, this, lr, lu);
                RolesList.Children.Add(cSingleRole);
            }
        }
        private void AddRoleButton_Click(object sender, RoutedEventArgs e)
        {
            WAddRole addRoleWindow = new WAddRole(this, lr,null,0);
            addRoleWindow.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
