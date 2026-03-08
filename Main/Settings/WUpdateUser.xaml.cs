using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Logique d'interaction pour WUpdateUser.xaml
    /// </summary>
    public partial class WUpdateUser : Window
    {
        public WUpdateUser(List<User> lu, List<Role> lr, CUserManagment CUM, User u)
        {
            InitializeComponent();
            this.lr = lr;
            this.lu = lu;
            this.CUM = CUM;
            this.u = u;

            foreach (Role role in lr)
            {
                Roles.Items.Add(role.RoleName);
            }

            Name.Text = u.UserName;
            Code.Password = u.Code.ToString();
            Roles.SelectedItem = lr.Where(r => r.RoleID == u.RoleID).FirstOrDefault().RoleName;
        }

        List<Role> lr;
        List<User> lu;
        CUserManagment CUM;
        User u;

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PasswordInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void PasswordInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
            //if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            //{
            //    e.Handled = true;
            //}
        }

        private bool IsPasswordAlreadyUsed(string password)
        {
            // Check if any OTHER user (not the current user being updated) has the same password
            return lu.Any(user => user.UserID != u.UserID && user.Code == password);
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<User> newU = lu;

                // Validate empty fields
                if (Name.Text == "" || Code.Password == "")
                {
                    MessageBox.Show("Please fill all the fields", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if password is already used by another user (excluding current user)
                if (IsPasswordAlreadyUsed(Code.Password))
                {
                    MessageBox.Show("This password is already used by another user. Please choose a different password.",
                                    "Password Already Exists",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    Code.Clear();
                    Code.Focus();
                    return;
                }

                u.UserName = Name.Text;
                u.Code = Code.Password;

                foreach (Role role in lr)
                {
                    if (role.RoleName == Roles.SelectedItem.ToString())
                    {
                        u.RoleID = role.RoleID;
                        break;
                    }
                }

                u.Etat = 1;

                foreach (User user in newU)
                {
                    if (user.UserID == u.UserID)
                    {
                        user.UserName = u.UserName;
                        user.Code = u.Code;
                        user.RoleID = u.RoleID;
                        break;
                    }
                }

                await u.UpdateUserAsync(MainWindow.ApiClient);
                CUM.Load_users();

                WCongratulations wCongratulations = new WCongratulations("Modification avec succès", "la Modification a ete effectue avec succes", 1);
                wCongratulations.ShowDialog();
                this.Close();
            }
            catch (Exception ex)
            {
                WCongratulations wCongratulations = new WCongratulations("Modification échoué", "la Modification n'a pas ete effectue ", 0);
                wCongratulations.ShowDialog();
            }
        }

        private void addRole_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}