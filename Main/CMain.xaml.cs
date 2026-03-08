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

namespace GestionComerce.Main
{
    /// <summary>
    /// Logique d'interaction pour CMain.xaml
    /// </summary>
    public partial class CMain : UserControl
    {
        public CMain(MainWindow main, User u)
        {
            InitializeComponent();
            this.main = main;
            this.u = u;
            Name.Text = u.UserName;

            // Keep the button icon in sync whenever the window state changes externally
            this.Loaded += (s, e) =>
            {
                var win = Window.GetWindow(this);
                if (win != null)
                    win.StateChanged += (ws, we) => UpdateWinStateIcon();
                UpdateWinStateIcon();
            };

            foreach (Role r in main.lr)
            {
                if (r.RoleID == u.RoleID)
                {
                    if (r.ViewSettings == false)
                    {
                        SettingsBtn.IsEnabled = false;
                        SetButtonGrayedOut(SettingsBtn);
                    }
                    if (r.ViewProjectManagment == false)
                    {
                        ProjectManagmentBtn.IsEnabled = false;
                        SetButtonGrayedOut(ProjectManagmentBtn);
                    }
                    if (r.ViewVente == false)
                    {
                        VenteBtn.IsEnabled = false;
                        SetButtonGrayedOut(VenteBtn);
                    }
                    if (r.ViewInventrory == false)
                    {
                        InventoryBtn.IsEnabled = false;
                        SetButtonGrayedOut(InventoryBtn);
                    }
                    if (r.ViewClientsPage == false)
                    {
                        ClientBtn.IsEnabled = false;
                        SetButtonGrayedOut(ClientBtn);
                    }
                    if (r.ViewFournisseurPage == false)
                    {
                        FournisseurBtn.IsEnabled = false;
                        SetButtonGrayedOut(FournisseurBtn);
                    }

                    // NEW: Check Facturation permission
                    if (r.AccessFacturation == false)
                    {
                        FacturationBtn.IsEnabled = false;
                        SetButtonGrayedOut(FacturationBtn);
                    }

                    // NEW: Check Livraison permission
                    if (r.AccessLivraison == false)
                    {
                        LivraisonBtn.IsEnabled = false;
                        SetButtonGrayedOut(LivraisonBtn);
                    }
                }
            }
        }

        // Helper method to set button appearance when disabled
        private void SetButtonGrayedOut(Button button)
        {
            if (button != null)
            {
                // Create a gray overlay effect
                var grayBrush = new SolidColorBrush(Color.FromArgb(180, 128, 128, 128));

                // Apply grayscale effect
                button.Opacity = 0.4;
                button.Cursor = Cursors.No;
            }
        }

        public MainWindow main;
        public User u;

        private void LivraisonBtn_Click(object sender, RoutedEventArgs e)
        {
            main.load_livraison(u);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            main.load_settings(u);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Article a = new Article();
            List<Article> la = await a.GetArticlesAsync();
            main.load_vente(u, la);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            main.load_inventory(u);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            main.load_ProjectManagement(u);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            main.load_client(u);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            main.load_fournisseur(u);
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            Exit exit = new Exit(this, 1);
            exit.ShowDialog();
        }

        // ── Header drag (works because WindowStyle="None") ───────────────────
        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var win = Window.GetWindow(this);
            if (win != null && win.WindowState == WindowState.Normal)
                win.DragMove();
        }

        // ── Windowed ↔ Full-Screen toggle ────────────────────────────────────
        private void WinStateBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);
            if (win == null) return;

            if (win.WindowState == WindowState.Maximized)
            {
                // Go to windowed: 90% of working area, centered
                var area = SystemParameters.WorkArea;
                win.WindowState = WindowState.Normal;
                win.Width  = area.Width  * 0.90;
                win.Height = area.Height * 0.90;
                win.Left   = area.Left + (area.Width  - win.Width)  / 2;
                win.Top    = area.Top  + (area.Height - win.Height) / 2;
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

            var icon  = WinStateBtn.Template?.FindName("WinStateIcon",  WinStateBtn) as System.Windows.Controls.TextBlock;
            var label = WinStateBtn.Template?.FindName("WinStateLabel", WinStateBtn) as System.Windows.Controls.TextBlock;
            if (icon == null || label == null) return;

            if (win.WindowState == WindowState.Maximized)
            {
                icon.Text  = "\uE923"; // BackToWindow
                label.Text = "Fenêtré";
            }
            else
            {
                icon.Text  = "\uE922"; // Maximize
                label.Text = "Plein écran";
            }
        }
        // ─────────────────────────────────────────────────────────────────────

        private void FacturationBtn_Click(object sender, RoutedEventArgs e)
        {
            main.load_facturation(u);
        }

        private void AccountabilityBtn_Click(object sender, RoutedEventArgs e)
        {
            main.load_accountibility(u);
        }
    }
}