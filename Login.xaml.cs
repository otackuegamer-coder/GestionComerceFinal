using GestionComerce.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

namespace GestionComerce
{
    public partial class Login : UserControl
    {
        private StringBuilder passwordBuilder = new StringBuilder();

        public Login(MainWindow main)
        {
            InitializeComponent();
            this.main = main;

            Btn0.Click += NumericButton_Click;
            Btn1.Click += NumericButton_Click;
            Btn2.Click += NumericButton_Click;
            Btn3.Click += NumericButton_Click;
            Btn4.Click += NumericButton_Click;
            Btn5.Click += NumericButton_Click;
            Btn6.Click += NumericButton_Click;
            Btn7.Click += NumericButton_Click;
            Btn8.Click += NumericButton_Click;
            Btn9.Click += NumericButton_Click;
            BtnClear.Click += BtnClear_Click;
            BtnDelete.Click += BtnDelete_Click;

            PasswordInput.KeyDown += PasswordInput_KeyDown_Enter;

            this.Loaded += (s, e) =>
            {
                PasswordInput.Focus();
                var win = Window.GetWindow(this);
                if (win != null)
                    win.StateChanged += (ws, we) => UpdateWinStateIcon();
                UpdateWinStateIcon();
            };
        }

        MainWindow main;

        private void NumericButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string digit)
            {
                passwordBuilder.Append(digit);
                PasswordInput.Password = passwordBuilder.ToString();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            passwordBuilder.Clear();
            PasswordInput.Password = string.Empty;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (passwordBuilder.Length > 0)
            {
                passwordBuilder.Remove(passwordBuilder.Length - 1, 1);
                PasswordInput.Password = passwordBuilder.ToString();
            }
        }

        private void PasswordInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void PasswordInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void PasswordInput_KeyDown_Enter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnEnter_Click(sender, e);
                e.Handled = true;
            }
        }

        private async void BtnEnter_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameInput.Text.Trim();
            string password = UserPasswordInput.Password.Trim();
            string appUserName = AppUsernameInput.Text.Trim();
            string pin = PasswordInput.Password;

            // ── basic field validation ─────────────────────────────────────────
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Veuillez entrer un nom d'utilisateur.");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Veuillez entrer un mot de passe.");
                return;
            }
            if (string.IsNullOrEmpty(appUserName))
            {
                MessageBox.Show("Veuillez entrer le nom d'utilisateur de l'application.");
                return;
            }
            if (string.IsNullOrEmpty(pin))
            {
                MessageBox.Show("Veuillez entrer votre code PIN.");
                return;
            }

            // ── disable button while request is in flight ──────────────────────
            BtnEnter.IsEnabled = false;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    User u = new User();
                    User loggedUser = await u.LoginAsync(client, username, password, appUserName, pin);

                    if (loggedUser != null)
                    {
                        main.load_main(loggedUser);
                    }
                    else
                    {
                        // Server responded but credentials were wrong
                        ClearPin();
                        MessageBox.Show(
                            "Identifiants incorrects ou abonnement inactif.",
                            "Connexion refusée",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Network-level error — could be no internet OR API server down.
                // We check internet to give the right message.
                ClearPin();

                bool hasInternet = await HasInternetAsync();

                if (!hasInternet)
                {
                    MessageBox.Show(
                        "Impossible de se connecter — aucun accès Internet détecté.\n\n" +
                        "Vérifiez votre connexion Wi-Fi ou réseau, puis réessayez.",
                        "Pas de connexion Internet",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(
                        "Internet est disponible, mais le serveur de l'application est inaccessible.\n\n" +
                        "Il s'agit probablement d'un problème de notre côté.\n" +
                        "Veuillez réessayer dans quelques instants ou contacter le support.",
                        "Serveur inaccessible",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (TaskCanceledException)
            {
                // Request timed out
                ClearPin();
                MessageBox.Show(
                    "La connexion au serveur a expiré (timeout).\n\n" +
                    "Vérifiez votre réseau et réessayez.",
                    "Délai dépassé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                // Unexpected error — show details without crashing
                ClearPin();
                MessageBox.Show(
                    $"Une erreur inattendue s'est produite lors de la connexion :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                BtnEnter.IsEnabled = true;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private void ClearPin()
        {
            passwordBuilder.Clear();
            PasswordInput.Password = string.Empty;
        }

        /// <summary>
        /// Quick check: can we ping 8.8.8.8?
        /// Used in the catch block to tell the user whether it's their network or our server.
        /// </summary>
        private static async Task<bool> HasInternetAsync()
        {
            try
            {
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = await ping.SendPingAsync("8.8.8.8", 3000);
                    return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // WINDOW CHROME
        // ─────────────────────────────────────────────────────────────────────

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var win = Window.GetWindow(this);
            if (win != null && win.WindowState == WindowState.Normal)
                win.DragMove();
        }

        private void WinStateBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);
            if (win == null) return;

            if (win.WindowState == WindowState.Maximized)
            {
                var area = SystemParameters.WorkArea;
                win.WindowState = WindowState.Normal;
                win.Width = area.Width * 0.90;
                win.Height = area.Height * 0.90;
                win.Left = area.Left + (area.Width - win.Width) / 2;
                win.Top = area.Top + (area.Height - win.Height) / 2;
            }
            else
            {
                win.WindowState = WindowState.Maximized;
            }
        }

        private void UpdateWinStateIcon()
        {
            var win = Window.GetWindow(this);
            if (win == null) return;

            var icon = WinStateBtn.Template?.FindName("WinStateIcon", WinStateBtn) as TextBlock;
            var label = WinStateBtn.Template?.FindName("WinStateLabel", WinStateBtn) as TextBlock;
            if (icon == null || label == null) return;

            if (win.WindowState == WindowState.Maximized)
            {
                icon.Text = "\uE923";
                label.Text = "Fenêtré";
            }
            else
            {
                icon.Text = "\uE922";
                label.Text = "Plein écran";
            }
        }

        private void BtnShutdown_Click(object sender, RoutedEventArgs e)
        {
            Exit exit = new Exit(null, 0);
            exit.ShowDialog();
        }
    }
}