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
    /// Logique d'interaction pour CSingleUser.xaml
    /// </summary>
    public partial class CSingleUser : UserControl
    {
        public CSingleUser(List<User> lu, List<Role> lr, CUserManagment CUM, User u,string roleName)
        {
            InitializeComponent();
            this.u= u;
            UserID.Text = u.UserID.ToString();
            UserName.Text = u.UserName ?? string.Empty;
            Code.Text = u.Code ?? string.Empty;   // ← was crashing here
            Role.Text = roleName ?? string.Empty;
            this.lu = lu;
            this.lr = lr;
            this.CUM = CUM;
            this.u = u;
            if (u.UserName== "root")
            {
                DeleteBtn.Visibility = Visibility.Collapsed;
            }
            foreach (Role r in CUM.sp.main.lr)
            {
                if (CUM.u.RoleID == r.RoleID)
                {
                    if (r.EditUsers == false)
                    {
                        EditUsers.IsEnabled = false;
                    }
                    if (r.DeleteUsers == false)
                    {
                        DeleteBtn.IsEnabled=false;
                    }
                    break;
                }
            }
        }
        List<User> lu;
        List<Role> lr;
        CUserManagment CUM;
        User u;string roleName;

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            WUpdateUser updateUserWindow = new WUpdateUser(lu,lr,CUM,u);
            updateUserWindow.ShowDialog();
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            WDeleteUser deleteUserWindow = new WDeleteUser(lu,CUM,u);
            deleteUserWindow.ShowDialog();  
        }
    }
}
