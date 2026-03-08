using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GestionComerce.Main.Inventory
{
    public partial class WDevisCustomization : Window
    {
        private List<Article> selectedArticles;
        private List<Famille> allFamilles;
        private List<Fournisseur> allFournisseurs;

        public WDevisCustomization(List<Article> articles, List<Famille> familles, List<Fournisseur> fournisseurs)
        {
            InitializeComponent();
            this.selectedArticles = articles;
            this.allFamilles = familles;
            this.allFournisseurs = fournisseurs;
        }

        private void ClientSection_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (ClientFieldsPanel != null)
            {
                ClientFieldsPanel.IsEnabled = ShowClientSectionCheckBox.IsChecked == true;
            }
        }

        private void NotesSection_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (NotesTextBox != null)
            {
                NotesTextBox.IsEnabled = ShowNotesCheckBox.IsChecked == true;
            }
        }

        private void PaymentTerms_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (PaymentTermsTextBox != null)
            {
                PaymentTermsTextBox.IsEnabled = ShowPaymentTermsCheckBox.IsChecked == true;
            }
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            // Validate validity days
            if (!int.TryParse(ValidityDaysTextBox.Text, out int validityDays) || validityDays <= 0)
            {
                MessageBox.Show("Veuillez entrer un nombre valide de jours pour la validité.",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create configuration object
            DevisConfiguration config = new DevisConfiguration
            {
                // Company info
                ShowLogo = ShowLogoCheckBox.IsChecked == true,
                ShowCompanyName = ShowCompanyNameCheckBox.IsChecked == true,
                ShowICE = ShowICECheckBox.IsChecked == true,
                ShowVAT = ShowVATCheckBox.IsChecked == true,
                ShowCompanyId = ShowCompanyIdCheckBox.IsChecked == true,
                ShowEtatJuridic = ShowEtatJuridicCheckBox.IsChecked == true,
                ShowSiege = ShowSiegeCheckBox.IsChecked == true,
                ShowTelephone = ShowTelephoneCheckBox.IsChecked == true,
                ShowAdresse = ShowAdresseCheckBox.IsChecked == true,

                // Article fields
                ShowCode = ShowCodeCheckBox.IsChecked == true,
                ShowArticleName = ShowArticleNameCheckBox.IsChecked == true,
                ShowQuantity = ShowQuantityCheckBox.IsChecked == true,
                ShowUnitPrice = ShowUnitPriceCheckBox.IsChecked == true,
                ShowTotalPrice = ShowTotalPriceCheckBox.IsChecked == true,
                ShowTVA = ShowTVACheckBox.IsChecked == true,
                ShowFamille = ShowFamilleCheckBox.IsChecked == true,
                ShowFournisseur = ShowFournisseurCheckBox.IsChecked == true,
                ShowMarque = ShowMarqueCheckBox.IsChecked == true,
                ShowLot = ShowLotCheckBox.IsChecked == true,
                ShowBonLivraison = ShowBonLivraisonCheckBox.IsChecked == true,
                ShowExpiration = ShowExpirationCheckBox.IsChecked == true,

                // Devis info
                ShowDevisNumber = ShowDevisNumberCheckBox.IsChecked == true,
                ShowDevisDate = ShowDevisDateCheckBox.IsChecked == true,
                ShowValidity = ShowValidityCheckBox.IsChecked == true,
                ValidityDays = validityDays,

                // Client info
                ShowClientSection = ShowClientSectionCheckBox.IsChecked == true,
                ClientName = ClientNameTextBox.Text,
                ClientICE = ClientICETextBox.Text,
                ClientAddress = ClientAddressTextBox.Text,

                // Totals
                ShowSubtotal = ShowSubtotalCheckBox.IsChecked == true,
                ShowTVATotal = ShowTVATotalCheckBox.IsChecked == true,
                ShowGrandTotal = ShowGrandTotalCheckBox.IsChecked == true,

                // Additional
                ShowNotes = ShowNotesCheckBox.IsChecked == true,
                Notes = NotesTextBox.Text,
                ShowPaymentTerms = ShowPaymentTermsCheckBox.IsChecked == true,
                PaymentTerms = PaymentTermsTextBox.Text
            };

            // Open preview window
            WDevisPreview previewWindow = new WDevisPreview(
                selectedArticles, allFamilles, allFournisseurs, config);

            bool? result = previewWindow.ShowDialog();

            // After preview closes, close this window and complete the flow
            this.DialogResult = true;
            this.Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    // Configuration class to hold all settings
    public class DevisConfiguration
    {
        // Company Information
        public bool ShowLogo { get; set; }
        public bool ShowCompanyName { get; set; }
        public bool ShowICE { get; set; }
        public bool ShowVAT { get; set; }
        public bool ShowCompanyId { get; set; }
        public bool ShowEtatJuridic { get; set; }
        public bool ShowSiege { get; set; }
        public bool ShowTelephone { get; set; }
        public bool ShowAdresse { get; set; }

        // Article Fields
        public bool ShowCode { get; set; }
        public bool ShowArticleName { get; set; }
        public bool ShowQuantity { get; set; }
        public bool ShowUnitPrice { get; set; }
        public bool ShowTotalPrice { get; set; }
        public bool ShowTVA { get; set; }
        public bool ShowFamille { get; set; }
        public bool ShowFournisseur { get; set; }
        public bool ShowMarque { get; set; }
        public bool ShowLot { get; set; }
        public bool ShowBonLivraison { get; set; }
        public bool ShowExpiration { get; set; }

        // Devis Information
        public bool ShowDevisNumber { get; set; }
        public bool ShowDevisDate { get; set; }
        public bool ShowValidity { get; set; }
        public int ValidityDays { get; set; }

        // Client Information
        public bool ShowClientSection { get; set; }
        public string ClientName { get; set; }
        public string ClientICE { get; set; }
        public string ClientAddress { get; set; }

        // Totals
        public bool ShowSubtotal { get; set; }
        public bool ShowTVATotal { get; set; }
        public bool ShowGrandTotal { get; set; }

        // Additional Options
        public bool ShowNotes { get; set; }
        public string Notes { get; set; }
        public bool ShowPaymentTerms { get; set; }
        public string PaymentTerms { get; set; }
    }
}