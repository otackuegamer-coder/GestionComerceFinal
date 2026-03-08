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
using System.Windows.Shapes;

namespace GestionComerce.Main.Settings
{
    /// <summary>
    /// Logique d'interaction pour WAddUser.xaml
    /// </summary>
    public partial class WAddUser : Window
    {
        public WAddUser(List<User> lu, List<Role> lr, CUserManagment CUM)
        {
            InitializeComponent();
            this.lr = lr;
            this.lu = lu;
            this.CUM = CUM;
            foreach (Role role in lr)
            {
                Roles.Items.Add(role.RoleName);
            }
            Roles.SelectedIndex = 0;
        }

        List<Role> lr;
        List<User> lu;
        CUserManagment CUM;

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
        }

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
            // Check if any existing user has the same password
            return lu.Any(user => user.Code == password);
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

                // Check if password is already used by another user
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

                User u = new User();
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
                int id = await u.InsertUserAsync(MainWindow.ApiClient);
                u.UserID = id;
                newU.Add(u);
                CUM.Load_users();

                WCongratulations wCongratulations = new WCongratulations("Ajout avec succès", "l'ajout a ete effectue avec succes", 1);
                wCongratulations.ShowDialog();
                this.Close();
            }
            catch (Exception ex)
            {
                WCongratulations wCongratulations = new WCongratulations("Ajout échoué", "l'ajout n'a pas ete effectue ", 0);
                wCongratulations.ShowDialog();
            }
        }
    }
}