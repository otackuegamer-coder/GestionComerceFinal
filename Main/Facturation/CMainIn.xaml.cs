using GestionComerce;
using GestionComerce.Main.Facturation.CreateFacture;
using GestionComerce.Main.Facturation.FacturesEnregistrees;
using GestionComerce.Main.Facturation.HistoriqueFacture;
using GestionComerce.Main.Facturation.VerifierHistorique;
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

namespace GestionComerce.Main.Facturation
{
    /// <summary>
    /// Interaction logic for CMainI.xaml
    /// </summary>
    public partial class CMainIn : UserControl
    {
        MainWindow main;
        User user;
        Operation operation;

        public CMainIn(User u, MainWindow main, Operation op)
        {
            InitializeComponent();
            this.main = main;
            this.user = u;
            this.operation = op;

            // Apply permissions and load initial content
            ApplyPermissions();
        }

        private void ApplyPermissions()
        {
            // Find user's role
            Role userRole = null;
            foreach (Role r in main.lr)
            {
                if (r.RoleID == user.RoleID)
                {
                    userRole = r;
                    break;
                }
            }

            if (userRole == null) return;

            // Track which permissions are enabled
            bool hasCreateFacture = userRole.CreateFacture;
            bool hasHistoriqueFacture = userRole.HistoriqueFacture;
            bool hasHistoryCheck = userRole.HistoryCheck;
            bool hasFactureEnregistrees = userRole.FactureEnregistrees;

            // Apply permissions to buttons
            if (!hasCreateFacture)
            {
                CreeFacture.IsEnabled = false;
                SetButtonDisabled(CreeFacture);
            }

            if (!hasHistoriqueFacture)
            {
                HistoriqueFacture.IsEnabled = false;
                SetButtonDisabled(HistoriqueFacture);
            }

            if (!hasHistoryCheck)
            {
                VerifierHistorique.IsEnabled = false;
                SetButtonDisabled(VerifierHistorique);
            }

            if (!hasFactureEnregistrees)
            {
                FacturesEnregistrees.IsEnabled = false;
                SetButtonDisabled(FacturesEnregistrees);
            }

            // Load the first available content based on permissions
            LoadFirstAvailableContent(hasCreateFacture, hasHistoriqueFacture, hasHistoryCheck, hasFactureEnregistrees);
        }

        private void LoadFirstAvailableContent(bool hasCreate, bool hasHistorique, bool hasCheck, bool hasEnregistrees)
        {
            ContentContainer.Children.Clear();

            if (hasCreate)
            {
                // Load Créer Facture
                CreeFacture.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                CreeFacture.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));

                CMainFa loginPage = new CMainFa(user, main, this, null);
                loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
                loginPage.VerticalAlignment = VerticalAlignment.Stretch;
                loginPage.Margin = new Thickness(0);
                ContentContainer.Children.Add(loginPage);
            }
            else if (hasHistorique)
            {
                // Load Historique Facture
                HistoriqueFacture.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                HistoriqueFacture.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));

                CMainHf loginPage = new CMainHf(user, main);
                loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
                loginPage.VerticalAlignment = VerticalAlignment.Stretch;
                loginPage.Margin = new Thickness(0);
                ContentContainer.Children.Add(loginPage);
            }
            else if (hasCheck)
            {
                // Load Vérifier Historique
                VerifierHistorique.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                VerifierHistorique.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));

                CMainVerifier verifierPage = new CMainVerifier(user, main);
                verifierPage.HorizontalAlignment = HorizontalAlignment.Stretch;
                verifierPage.VerticalAlignment = VerticalAlignment.Stretch;
                verifierPage.Margin = new Thickness(0);
                ContentContainer.Children.Add(verifierPage);
            }
            else if (hasEnregistrees)
            {
                // Load Factures Enregistrées
                FacturesEnregistrees.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                FacturesEnregistrees.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));

                CMainEnregistrees enregistreesPage = new CMainEnregistrees(user, main);
                enregistreesPage.HorizontalAlignment = HorizontalAlignment.Stretch;
                enregistreesPage.VerticalAlignment = VerticalAlignment.Stretch;
                enregistreesPage.Margin = new Thickness(0);
                ContentContainer.Children.Add(enregistreesPage);
            }
            else
            {
                // No permissions - show message
                TextBlock noAccessMessage = new TextBlock
                {
                    Text = "Vous n'avez accès à aucune section de facturation.\nVeuillez contacter votre administrateur.",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 16,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                ContentContainer.Children.Add(noAccessMessage);
            }
        }

        private void SetButtonDisabled(Button button)
        {
            button.Opacity = 0.5;
            button.Cursor = Cursors.No;
            button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
        }

        private void CreeFacture_Click(object sender, RoutedEventArgs e)
        {
            if (!CreeFacture.IsEnabled) return;

            // Reset all button styles
            ResetButtonStyles();

            // Set active style for this button
            CreeFacture.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
            CreeFacture.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));

            ContentContainer.Children.Clear();
            CMainFa loginPage = new CMainFa(user, main, this, null);
            Grid.SetRow(loginPage, 0);
            Grid.SetColumn(loginPage, 0);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            ContentContainer.Children.Add(loginPage);
        }

        private void HistoriqueFacture_Click(object sender, RoutedEventArgs e)
        {
            if (!HistoriqueFacture.IsEnabled) return;

            // Reset all button styles
            ResetButtonStyles();

            // Set active style for this button
            HistoriqueFacture.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
            HistoriqueFacture.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));

            ContentContainer.Children.Clear();
            CMainHf loginPage = new CMainHf(user, main);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            ContentContainer.Children.Add(loginPage);
        }

        private void VerifierHistorique_Click(object sender, RoutedEventArgs e)
        {
            if (!VerifierHistorique.IsEnabled) return;

            // Reset all button styles
            ResetButtonStyles();

            // Set active style for this button
            VerifierHistorique.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
            VerifierHistorique.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));

            ContentContainer.Children.Clear();
            CMainVerifier verifierPage = new CMainVerifier(user, main);
            verifierPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            verifierPage.VerticalAlignment = VerticalAlignment.Stretch;
            verifierPage.Margin = new Thickness(0);
            ContentContainer.Children.Add(verifierPage);
        }

        private void FacturesEnregistrees_Click(object sender, RoutedEventArgs e)
        {
            if (!FacturesEnregistrees.IsEnabled) return;

            // Reset all button styles
            ResetButtonStyles();

            // Set active style for this button
            FacturesEnregistrees.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
            FacturesEnregistrees.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));

            ContentContainer.Children.Clear();
            CMainEnregistrees enregistreesPage = new CMainEnregistrees(user, main);
            enregistreesPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            enregistreesPage.VerticalAlignment = VerticalAlignment.Stretch;
            enregistreesPage.Margin = new Thickness(0);
            ContentContainer.Children.Add(enregistreesPage);
        }

        private void RetourButton_Click(object sender, RoutedEventArgs e)
        {
            main.load_main(user);
        }

        // Helper method to reset all button styles to inactive state
        private void ResetButtonStyles()
        {
            var inactiveColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
            var transparentBrush = new SolidColorBrush(Colors.Transparent);

            // Only reset enabled buttons
            if (CreeFacture.IsEnabled)
            {
                CreeFacture.Foreground = inactiveColor;
                CreeFacture.BorderBrush = transparentBrush;
            }

            if (HistoriqueFacture.IsEnabled)
            {
                HistoriqueFacture.Foreground = inactiveColor;
                HistoriqueFacture.BorderBrush = transparentBrush;
            }

            if (VerifierHistorique.IsEnabled)
            {
                VerifierHistorique.Foreground = inactiveColor;
                VerifierHistorique.BorderBrush = transparentBrush;
            }

            if (FacturesEnregistrees.IsEnabled)
            {
                FacturesEnregistrees.Foreground = inactiveColor;
                FacturesEnregistrees.BorderBrush = transparentBrush;
            }
        }
    }
}