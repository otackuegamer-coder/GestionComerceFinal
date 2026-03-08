using Superete.Main.Comptabilite.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Superete.Main.Comptabilite
{
    public partial class SalaireDetailsWindow : Window
    {
        private SalaireModel salaire;

        public SalaireDetailsWindow(SalaireModel salaire)
        {
            InitializeComponent();
            this.salaire = salaire;
            LoadSalaireDetails();
        }

        private void LoadSalaireDetails()
        {
            if (salaire == null) return;

            // Header
            txtPeriode.Text = $"Période: {GetMonthName(salaire.Mois)} {salaire.Annee}";
            txtStatut.Text = salaire.Statut;

            // Update status color
            UpdateStatutColor(salaire.Statut);

            // Employee Information
            txtNomComplet.Text = salaire.NomComplet ?? "--";
            txtCIN.Text = salaire.CIN ?? "--";
            txtCNSS.Text = salaire.CNSS ?? "--";

            // Base Salary
            txtSalaireBase.Text = $"{salaire.SalaireBase:N2} DH";
            txtTauxHoraire.Text = $"{salaire.TauxHoraire:N2} DH/h";

            // Overtime Hours
            txtHeuresSupp25.Text = $"{salaire.HeuresSupp25:N0} h";
            txtHeuresSupp50.Text = $"{salaire.HeuresSupp50:N0} h";
            txtHeuresSupp100.Text = $"{salaire.HeuresSupp100:N0} h";
            txtMontantHeuresSupp.Text = $"{salaire.MontantHeuresSupp:N2} DH";

            // Allowances and Bonuses
            txtPrimeAnciennete.Text = $"{salaire.PrimeAnciennete:N2} DH";
            txtPrimeRendement.Text = $"{salaire.PrimeRendement:N2} DH";
            txtPrimeResponsabilite.Text = $"{salaire.PrimeResponsabilite:N2} DH";
            txtIndemniteTransport.Text = $"{salaire.IndemniteTransport:N2} DH";
            txtIndemniteLogement.Text = $"{salaire.IndemniteLogement:N2} DH";
            txtAutresPrimes.Text = $"{salaire.AutresPrimes:N2} DH";

            // Deductions
            txtCotisationCNSS.Text = $"{salaire.CotisationCNSS:N2} DH";
            txtCotisationAMO.Text = $"{salaire.CotisationAMO:N2} DH";
            txtMontantIR.Text = $"{salaire.MontantIR:N2} DH";
            txtCIMR.Text = $"{salaire.CotisationCIMR:N2} DH";
            txtAvance.Text = $"{salaire.AvanceSurSalaire:N2} DH";
            txtPret.Text = $"{salaire.PretPersonnel:N2} DH";
            txtPenalites.Text = $"{salaire.Penalites:N2} DH";
            txtAutresRetenues.Text = $"{salaire.AutresRetenues:N2} DH";

            // Remarks
            if (!string.IsNullOrWhiteSpace(salaire.Remarques))
            {
                txtRemarques.Text = salaire.Remarques;
                remarquesPanel.Visibility = Visibility.Visible;
            }

            // Summary
            txtSalaireBrut.Text = $"{salaire.SalaireBrut:N2} DH";
            txtTotalRetenues.Text = $"{salaire.TotalRetenues:N2} DH";
            txtSalaireNet.Text = $"{salaire.SalaireNet:N2} DH";
        }

        private void UpdateStatutColor(string statut)
        {
            var statutBorder = (txtStatut.Parent as StackPanel)?.Parent as System.Windows.Controls.Border;
            if (statutBorder == null) return;

            switch (statut)
            {
                case "Payé":
                    statutBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"));
                    txtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#047857"));
                    break;
                case "Validé":
                    statutBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE"));
                    txtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF"));
                    break;
                case "En Attente":
                    statutBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                    txtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E"));
                    break;
            }
        }

        private string GetMonthName(int month)
        {
            string[] months = { "", "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                              "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };
            return month > 0 && month <= 12 ? months[month] : "";
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnImprimer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Print the window content
                    printDialog.PrintVisual(this, $"Bulletin de Paie - {salaire.NomComplet}");
                }
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression: {ex.Message}",
                              "Erreur",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }
    }
}