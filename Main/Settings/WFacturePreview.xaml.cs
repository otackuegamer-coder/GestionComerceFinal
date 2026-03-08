using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GestionComerce.Settings
{
    /// <summary>
    /// Preview window showing thermal receipt/ticket format (80mm style)
    /// Shows how the configured settings will appear on actual receipt
    /// </summary>
    public partial class WFacturePreview: Window
    {
        private FactureSettings settings;

        public WFacturePreview(FactureSettings factureSettings)
        {
            InitializeComponent();
            this.settings = factureSettings;
            LoadTicketPreview();
        }

        private void LoadTicketPreview()
        {
            try
            {
                // ===== COMPANY INFO =====
                txtCompanyName.Text = settings.CompanyName;
                txtCompanyAddress.Text = settings.CompanyAddress;
                txtCompanyPhone.Text = settings.CompanyPhone;
                txtCompanyEmail.Text = settings.CompanyEmail;

                // ===== LOGO =====
                if (!string.IsNullOrEmpty(settings.LogoPath) && File.Exists(settings.LogoPath))
                {
                    try
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(settings.LogoPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        imgLogo.Source = bitmap;
                        imgLogo.Visibility = Visibility.Visible;
                    }
                    catch
                    {
                        imgLogo.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    imgLogo.Visibility = Visibility.Collapsed;
                }

                // ===== INVOICE NUMBER & DATE =====
                txtInvoiceNumber.Text = $"{settings.InvoicePrefix}000001";
                txtInvoiceDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                // ===== CALCULATE SAMPLE TOTALS =====
                // Sample articles total: 100 + 75 + 75 = 250.00 DH
                decimal subtotal = 250.00m;
                decimal remise = 10.00m;
                decimal subtotalAfterRemise = subtotal - remise; // 240.00 DH

                // Calculate TAX based on configured percentage
                decimal taxAmount = subtotalAfterRemise * (settings.TaxPercentage / 100);
                decimal total = subtotalAfterRemise + taxAmount;

                // Display tax with percentage
                txtTaxLabel.Text = $"TVA ({settings.TaxPercentage:0.##}%):";
                txtTax.Text = $"{taxAmount:N2} DH";
                txtTotal.Text = $"{total:N2} DH";

                // ===== TERMS AND CONDITIONS =====
                if (!string.IsNullOrWhiteSpace(settings.TermsAndConditions))
                {
                    // For ticket format, show shortened version if too long
                    string terms = settings.TermsAndConditions;
                    if (terms.Length > 150)
                    {
                        terms = terms.Substring(0, 147) + "...";
                    }
                    txtTermsAndConditions.Text = terms;
                    txtTermsAndConditions.Visibility = Visibility.Visible;
                }
                else
                {
                    txtTermsAndConditions.Visibility = Visibility.Collapsed;
                }

                // ===== FOOTER MESSAGE =====
                if (!string.IsNullOrWhiteSpace(settings.FooterText))
                {
                    txtFooter.Text = settings.FooterText;
                    txtFooter.Visibility = Visibility.Visible;
                }
                else
                {
                    txtFooter.Text = "MERCI DE VOTRE VISITE";
                    txtFooter.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de l'aperçu du ticket: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}