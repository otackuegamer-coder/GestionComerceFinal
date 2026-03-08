using GestionComerce.Main.Vente;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GestionComerce.Vente
{
    public partial class WFacturePreview : Window
    {
        private GestionComerce.FactureSettings settings;
        private bool isPreviewMode = false;
        public bool ShouldPrint { get; private set; } = false;

        public WFacturePreview(GestionComerce.FactureSettings factureSettings)
        {
            InitializeComponent();
            this.settings = factureSettings;
            this.isPreviewMode = true;
            LoadTicketPreview();
        }

        public WFacturePreview(
            GestionComerce.FactureSettings factureSettings,
            int operationId,
            DateTime operationDate,
            Client client,
            List<TicketArticleData> articles,
            decimal total,
            decimal remise,
            decimal credit,
            string paymentMethod,
            string operationType)
        {
            InitializeComponent();
            this.settings = factureSettings;
            this.isPreviewMode = false;
            LoadActualTicket(operationId, operationDate, client, articles, total, remise, credit, paymentMethod, operationType);
        }

        private void LoadTicketPreview()
        {
            try
            {
                txtCompanyName.Text = settings.CompanyName;
                txtCompanyAddress.Text = settings.CompanyAddress;
                txtCompanyPhone.Text = settings.CompanyPhone;
                txtCompanyEmail.Text = settings.CompanyEmail;

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

                txtInvoiceNumber.Text = $"{settings.InvoicePrefix}000001";
                txtInvoiceDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                decimal subtotal = 250.00m;
                decimal remise = 10.00m;
                decimal subtotalAfterRemise = subtotal - remise;
                decimal taxAmount = subtotalAfterRemise * (settings.TaxPercentage / 100);
                decimal total = subtotalAfterRemise + taxAmount;

                txtSubtotal.Text = $"{subtotal:N2} DH";
                txtRemise.Text = $"-{remise:N2} DH";
                txtTaxLabel.Text = $"TVA ({settings.TaxPercentage:0.##}%):";
                txtTax.Text = $"{taxAmount:N2} DH";
                txtTotal.Text = $"{total:N2} DH";

                if (!string.IsNullOrWhiteSpace(settings.TermsAndConditions))
                {
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

                if (!string.IsNullOrWhiteSpace(settings.FooterText))
                {
                    txtFooter.Text = settings.FooterText;
                }
                else
                {
                    txtFooter.Text = "MERCI DE VOTRE VISITE";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de l'aperçu du ticket: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadActualTicket(
            int operationId,
            DateTime operationDate,
            Client client,
            List<TicketArticleData> articles,
            decimal total,
            decimal remise,
            decimal credit,
            string paymentMethod,
            string operationType)
        {
            try
            {
                txtCompanyName.Text = settings.CompanyName;
                txtCompanyAddress.Text = settings.CompanyAddress;
                txtCompanyPhone.Text = settings.CompanyPhone;
                txtCompanyEmail.Text = settings.CompanyEmail;

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

                string invoiceNumber = operationId.ToString().PadLeft(6, '0');
                txtInvoiceNumber.Text = $"{settings.InvoicePrefix}{invoiceNumber}";
                txtInvoiceDate.Text = operationDate.ToString("dd/MM/yyyy HH:mm");

                if (client != null)
                {
                    txtClientInfo.Text = $"Client: {client.Nom}";
                }
                else
                {
                    txtClientInfo.Text = "Client: Vente Sans Client";
                }

                StackPanel ticketStackPanel = FindTicketStackPanel(ticketArea);
                if (ticketStackPanel != null)
                {
                    int insertIndex = -1;

                    for (int i = 0; i < ticketStackPanel.Children.Count; i++)
                    {
                        if (ticketStackPanel.Children[i] == articlesDivider)
                        {
                            insertIndex = i + 1;
                            break;
                        }
                    }

                    if (insertIndex != -1)
                    {
                        List<UIElement> toRemove = new List<UIElement>();
                        bool startRemoving = false;

                        foreach (UIElement element in ticketStackPanel.Children)
                        {
                            if (element == articlesDivider)
                            {
                                startRemoving = true;
                                continue;
                            }

                            if (element == totalsTopDivider)
                            {
                                break;
                            }

                            if (startRemoving)
                            {
                                toRemove.Add(element);
                            }
                        }

                        foreach (var item in toRemove)
                        {
                            ticketStackPanel.Children.Remove(item);
                        }

                        int currentIndex = insertIndex;
                        foreach (var article in articles)
                        {
                            Grid articleGrid = CreateArticleGrid(article);
                            ticketStackPanel.Children.Insert(currentIndex++, articleGrid);
                        }
                    }
                }

                decimal subtotal = total;

                decimal totalTVA = 0;
                Dictionary<decimal, decimal> tvaByRate = new Dictionary<decimal, decimal>();

                foreach (var article in articles)
                {
                    decimal articleTotal = article.Total;
                    decimal articleTVA = articleTotal * (article.TVA / 100);
                    totalTVA += articleTVA;

                    if (tvaByRate.ContainsKey(article.TVA))
                    {
                        tvaByRate[article.TVA] += articleTVA;
                    }
                    else
                    {
                        tvaByRate[article.TVA] = articleTVA;
                    }
                }

                decimal tvaAfterRemise = totalTVA;
                if (remise > 0 && subtotal > 0)
                {
                    tvaAfterRemise = totalTVA * ((subtotal - remise) / subtotal);
                }

                decimal subtotalAfterRemise = subtotal - remise;
                decimal finalTotal = subtotalAfterRemise + tvaAfterRemise;

                txtSubtotal.Text = $"{subtotal:N2} DH";
                gridSubtotal.Visibility = Visibility.Visible;

                if (remise > 0)
                {
                    txtRemise.Text = $"-{remise:N2} DH";
                    gridRemise.Visibility = Visibility.Visible;
                }
                else
                {
                    gridRemise.Visibility = Visibility.Collapsed;
                }

                if (tvaAfterRemise > 0)
                {
                    if (tvaByRate.Count == 1)
                    {
                        var tvaRate = tvaByRate.Keys.First();
                        txtTaxLabel.Text = $"TVA ({tvaRate:0.##}%):";
                    }
                    else if (tvaByRate.Count > 1)
                    {
                        string tvaLabel = "TVA (";
                        foreach (var kvp in tvaByRate)
                        {
                            tvaLabel += $"{kvp.Key:0.##}%, ";
                        }
                        tvaLabel = tvaLabel.TrimEnd(',', ' ') + "):";
                        txtTaxLabel.Text = tvaLabel;
                    }
                    else
                    {
                        txtTaxLabel.Text = "TVA:";
                    }

                    txtTax.Text = $"{tvaAfterRemise:N2} DH";
                    gridTax.Visibility = Visibility.Visible;
                }
                else
                {
                    gridTax.Visibility = Visibility.Collapsed;
                }

                txtTotal.Text = $"{finalTotal:N2} DH";

                txtPaymentMethod.Text = $"Mode de paiement: {paymentMethod}";

                if (credit > 0)
                {
                    TextBlock creditBlock = new TextBlock
                    {
                        Text = $"Crédit: {credit:N2} DH | Payé: {(finalTotal - credit):N2} DH",
                        FontSize = 10,
                        TextAlignment = TextAlignment.Center,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.Red,
                        Margin = new Thickness(0, 5, 0, 5)
                    };

                    if (ticketStackPanel != null)
                    {
                        int paymentIndex = ticketStackPanel.Children.IndexOf(txtPaymentMethod);
                        if (paymentIndex >= 0)
                        {
                            ticketStackPanel.Children.Insert(paymentIndex + 1, creditBlock);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(settings.TermsAndConditions))
                {
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

                if (!string.IsNullOrWhiteSpace(settings.FooterText))
                {
                    txtFooter.Text = settings.FooterText;
                }
                else
                {
                    txtFooter.Text = "MERCI DE VOTRE VISITE";
                }

                txtBarcode.Text = $"* {settings.InvoicePrefix}{invoiceNumber} *";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du ticket: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private StackPanel FindTicketStackPanel(Border ticketBorder)
        {
            if (ticketBorder.Child is StackPanel sp)
                return sp;
            return null;
        }

        private Grid CreateArticleGrid(TicketArticleData article)
        {
            Grid articleGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };

            articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

            TextBlock nameBlock = new TextBlock
            {
                Text = article.ArticleName,
                FontSize = 9,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameBlock, 0);
            articleGrid.Children.Add(nameBlock);

            TextBlock qtyBlock = new TextBlock
            {
                Text = article.Quantity.ToString(),
                FontSize = 9,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(qtyBlock, 1);
            articleGrid.Children.Add(qtyBlock);

            TextBlock priceBlock = new TextBlock
            {
                Text = article.UnitPrice.ToString("N2"),
                FontSize = 9,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(priceBlock, 2);
            articleGrid.Children.Add(priceBlock);

            TextBlock totalBlock = new TextBlock
            {
                Text = article.Total.ToString("N2"),
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(totalBlock, 3);
            articleGrid.Children.Add(totalBlock);

            return articleGrid;
        }

        public void PrintFacture()
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                LocalPrintServer localPrintServer = new LocalPrintServer();

                PrintQueue thermalPrinter = null;
                string[] thermalPrinterNames = { "thermal", "receipt", "pos", "80mm", "58mm", "xprinter", "epson tm" };

                foreach (PrintQueue pq in localPrintServer.GetPrintQueues())
                {
                    string printerNameLower = pq.Name.ToLower();

                    foreach (string keyword in thermalPrinterNames)
                    {
                        if (printerNameLower.Contains(keyword))
                        {
                            thermalPrinter = pq;
                            break;
                        }
                    }

                    if (thermalPrinter != null)
                        break;
                }

                if (thermalPrinter != null)
                {
                    printDialog.PrintQueue = thermalPrinter;
                    printDialog.PrintVisual(ticketArea, "Facture");
                }
                else
                {
                    if (printDialog.ShowDialog() == true)
                    {
                        printDialog.PrintVisual(ticketArea, "Facture");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression: {ex.Message}",
                    "Erreur d'impression", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            ShouldPrint = true;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ShouldPrint = false;
            this.DialogResult = false;
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}