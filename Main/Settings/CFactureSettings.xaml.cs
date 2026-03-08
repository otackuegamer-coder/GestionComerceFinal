using Microsoft.Win32;
using GestionComerce.Main;
using GestionComerce.Main.Settings;
using GestionComerce.Vente;
using System;
using System.Windows;
using System.Windows.Controls;

namespace GestionComerce.Settings
{
    public partial class CFactureSettings: UserControl
    {
        private FactureSettings currentSettings;

        public CFactureSettings(SettingsPage sp)
        {
            InitializeComponent();
            LoadSettings();
            foreach(Role r in sp.lr)
            {
                if (r.RoleID == sp.u.RoleID)
                {
                    if (r.ModifyFactureSettings == false)
                    {
                        btnSave.IsEnabled = false;

                    }
                    if (r.ViewFacture == false)
                    {
                        btnPreview.IsEnabled = false;
                    }
                }
            }
        }

        private async void LoadSettings()
        {
            try
            {
                // FIXED: Call static method correctly
                currentSettings = await FactureSettings.GetFactureSettingsAsync();

                // Populate form with current settings
                txtCompanyName.Text = currentSettings.CompanyName ?? "";
                txtCompanyAddress.Text = currentSettings.CompanyAddress ?? "";
                txtCompanyPhone.Text = currentSettings.CompanyPhone ?? "";
                txtCompanyEmail.Text = currentSettings.CompanyEmail ?? "";
                txtLogoPath.Text = currentSettings.LogoPath ?? "";
                txtInvoicePrefix.Text = currentSettings.InvoicePrefix ?? "FAC-";
                txtTaxPercentage.Text = currentSettings.TaxPercentage.ToString("0.00");
                txtTermsAndConditions.Text = currentSettings.TermsAndConditions ?? "";
                txtFooterText.Text = currentSettings.FooterText ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des paramètres: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBrowseLogo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Sélectionner le logo de l'entreprise"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtLogoPath.Text = openFileDialog.FileName;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(txtCompanyName.Text))
                {
                    MessageBox.Show("Le nom de l'entreprise est obligatoire.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtTaxPercentage.Text, out decimal taxPercentage))
                {
                    MessageBox.Show("Le pourcentage de TVA doit être un nombre valide.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update settings object
                currentSettings.CompanyName = txtCompanyName.Text.Trim();
                currentSettings.CompanyAddress = txtCompanyAddress.Text.Trim();
                currentSettings.CompanyPhone = txtCompanyPhone.Text.Trim();
                currentSettings.CompanyEmail = txtCompanyEmail.Text.Trim();
                currentSettings.LogoPath = txtLogoPath.Text.Trim();
                currentSettings.InvoicePrefix = txtInvoicePrefix.Text.Trim();
                currentSettings.TaxPercentage = taxPercentage;
                currentSettings.TermsAndConditions = txtTermsAndConditions.Text.Trim();
                currentSettings.FooterText = txtFooterText.Text.Trim();

                // Save to database
                int result = await currentSettings.SaveFactureSettingsAsync();

                if (result > 0)
                {
                    WCongratulations wCongratulations = new WCongratulations("Eregistrement succes", "l'eregistrement a ete effectue avec succes", 1);
                    wCongratulations.ShowDialog();

                    // Show preview of the facture with new settings


                    //this.DialogResult = true;
                    //this.Close();
                }
                else
                {
                    WCongratulations wCongratulations = new WCongratulations("Eregistrement échoué", "l'eregistrement n'a pas ete effectue ", 0);
                    wCongratulations.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            //this.DialogResult = false;
            //this.Close();
        }

        private void ShowFacturePreview()
        {
            try
            {
                // Show a sample/demo facture with the configured settings
                var previewWindow = new Vente.WFacturePreview(currentSettings);
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affichage de l'aperçu: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            ShowFacturePreview();
        }
    }
}