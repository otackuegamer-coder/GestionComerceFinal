using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GestionComerce;
using GestionComerce.Models;
using GestionComerce;

namespace Superete.Main.Comptabilite.Views
{
    public partial class DashboardFinancierView : UserControl
    {
        private User currentUser;
        private ComptabiliteService comptabiliteService;

        public DashboardFinancierView(User u)
        {
            InitializeComponent();
            currentUser = u;
            comptabiliteService = new ComptabiliteService();
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                DashboardContent.Visibility = Visibility.Collapsed;

                DateTime dateDebut = new DateTime(DateTime.Now.Year, 1, 1);
                DateTime dateFin = DateTime.Now;

                DashboardFinancierDTO dashboard = comptabiliteService.GenererDashboardFinancier(dateDebut, dateFin);

                // Update UI
                TxtTotalVentes.Text = string.Format("{0:N2} DH", dashboard.TotalVentes);
                TxtBeneficeNet.Text = string.Format("{0:N2} DH", dashboard.BeneficeNet);
                TxtTresorerie.Text = string.Format("{0:N2} DH", dashboard.TresorerieTotale);
                TxtBanque.Text = string.Format("{0:N2} DH", dashboard.TresorerieBanque);
                TxtCaisse.Text = string.Format("{0:N2} DH", dashboard.TresorerieCaisse);

                decimal totalCharges = dashboard.TotalAchats + dashboard.TotalSalaires + dashboard.TotalDepenses;
                TxtTotalCharges.Text = string.Format("{0:N2} DH", totalCharges);
                TxtAchats.Text = string.Format("{0:N2} DH", dashboard.TotalAchats);
                TxtSalaires.Text = string.Format("{0:N2} DH", dashboard.TotalSalaires);
                TxtAutresDepenses.Text = string.Format("{0:N2} DH", dashboard.TotalDepenses);

                // Color for net profit
                if (dashboard.BeneficeNet >= 0)
                {
                    TxtBeneficeNet.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#10B981"));
                }
                else
                {
                    TxtBeneficeNet.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#EF4444"));
                }

                // Animate progress bars
                if (totalCharges > 0)
                {
                    AnimateBar(AchatsBar, (double)(dashboard.TotalAchats / totalCharges) * 100);
                    AnimateBar(SalairesBar, (double)(dashboard.TotalSalaires / totalCharges) * 100);
                    AnimateBar(AutresBar, (double)(dashboard.TotalDepenses / totalCharges) * 100);
                }

                LoadingPanel.Visibility = Visibility.Collapsed;
                DashboardContent.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show(string.Format("Erreur: {0}", ex.Message), "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AnimateBar(Border bar, double percentage)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = percentage * 4,
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            bar.BeginAnimation(FrameworkElement.WidthProperty, animation);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
        }

        private void OpenJournal_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Navigation vers Journal Général", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenGrandLivre_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Navigation vers Grand Livre", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenBilan_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Navigation vers Bilan", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenCPC_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Navigation vers CPC", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}