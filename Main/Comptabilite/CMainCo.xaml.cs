using GestionComerce;
using GestionComerce.Main;
using GestionComerce.Main.ProjectManagment;
using Superete.Main.Comptabilite.Expectation;
using Superete.Main.Comptabilite.Graphes;
using Superete.Main.Comptabilite.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Superete.Main.Comptabilite
{
    /// <summary>
    /// Interaction logic for CMainCo.xaml
    /// </summary>
    public partial class CMainCo : UserControl
    {
        User u;
        MainWindow main;

        public CMainCo(User u, MainWindow main)
        {
            InitializeComponent();
            this.u = u;
            this.main = main;

            // Set initial selection - Dashboard
            SetSelectedButtonStyle(DashboardButton);
            LoadDashboard();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            main.load_main(u);
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            LoadDashboard();
        }

        private void Journal_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            MainContentArea.Content = new JournalGeneralView(u);
        }

        private void GrandLivre_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            MainContentArea.Content = new GrandLivreView(u);
        }

        private void PlanComptable_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            MainContentArea.Content = new PlanComptableView(u);
        }

        private void Bilan_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            MainContentArea.Content = new BilanView(u);
        }

        private void CPC_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            // TODO: Create CPCView later
            MainContentArea.Content = new CPCView(u);
        }

        private void Graphes_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            LoadGraphes();
        }

        private void BtnGestionEmployes_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            MainContentArea.Content = new GestionEmployes();
        }

        private void BtnGestionSalaire_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            MainContentArea.Content = new GestionSalaires();
        }

        private void RapportStats_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            LoadRapportStats();
        }

        private void Expenses_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            MainContentArea.Content = new Expenses();
        }

        private void AI_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            // AI button keeps its own gradient style
            MessageBox.Show("Fonctionnalité IA en cours de développement", "Assistant IA",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadDashboard()
        {
            MainContentArea.Content = new DashboardFinancierView(u);
        }

        private void LoadGraphes()
        {
            MainContentArea.Content = new CGraphe();
        }

        private void LoadRapportStats()
        {
            MainContentArea.Content = new CMainR(u, main);
        }

        private void ResetButtonStyles()
        {
            ResetButton(DashboardButton);
            ResetButton(JournalButton);
            ResetButton(GrandLivreButton);
            ResetButton(PlanComptableButton);
            ResetButton(BilanButton);
            ResetButton(CPCButton);
            ResetButton(GraphesButton);
            ResetButton(GestionEmployesButton);
            ResetButton(RapportButton);
            ResetButton(ExpensesButton);
            // AI button keeps its gradient style
        }

        private void ResetButton(Button button)
        {
            button.Background = Brushes.Transparent;
            button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3748"));
            button.FontWeight = FontWeights.Medium;
        }

        private void SetSelectedButtonStyle(Button button)
        {
            // Don't change AI button style
            if (button == AIButton) return;

            // Selected style - light blue background with darker text
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            button.FontWeight = FontWeights.SemiBold;
        }

        private void Expectation_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStyles();
            SetSelectedButtonStyle((Button)sender);
            MainContentArea.Content = new CMainEx(u,main);
        }
    }
}