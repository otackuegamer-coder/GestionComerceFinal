using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GestionComerce.Main.Inventory
{
    public partial class WArticleDetails : Window
    {
        public WArticleDetails(Article article, List<Famille> lf, List<Fournisseur> lfo)
        {
            InitializeComponent();

            // Header
            HeaderArticleName.Text = article.ArticleName ?? "Article";
            HeaderSubtitle.Text = $"Code: {article.Code} • ID: {article.ArticleID}";

            // Basic Information
            ArticleID.Text = article.ArticleID.ToString();
            Code.Text = article.Code.ToString();

            // SKU
            SKU.Text = string.IsNullOrWhiteSpace(article.SKU) ? "N/A" : article.SKU;

            // Famille
            string familleName = "N/A";
            foreach (Famille f in lf)
            {
                if (f.FamilleID == article.FamillyID)
                {
                    familleName = f.FamilleName;
                    break;
                }
            }
            Famille.Text = familleName;

            // Fournisseur
            string fournisseurName = "N/A";
            foreach (Fournisseur fo in lfo)
            {
                if (fo.FournisseurID == article.FournisseurID)
                {
                    fournisseurName = fo.Nom;
                    break;
                }
            }
            Fournisseur.Text = fournisseurName;

            // Additional Product Info
            Marque.Text = string.IsNullOrWhiteSpace(article.marque) ? "N/A" : article.marque;
            Manufacturer.Text = string.IsNullOrWhiteSpace(article.Manufacturer) ? "N/A" : article.Manufacturer;
            CountryOfOrigin.Text = string.IsNullOrWhiteSpace(article.CountryOfOrigin) ? "N/A" : article.CountryOfOrigin;
            Description.Text = string.IsNullOrWhiteSpace(article.Description) ? "Aucune description disponible" : article.Description;

            // Prices
            decimal prixAchat = article.PrixAchat;
            decimal prixVente = article.PrixVente;
            decimal prixMP = article.PrixMP;

            PrixAchat.Text = prixAchat.ToString("0.00") + " DH";
            PrixVente.Text = prixVente.ToString("0.00") + " DH";
            DisplayPrixVente.Text = prixVente.ToString("0.00") + " DH";
            PrixMP.Text = prixMP.ToString("0.00") + " DH";
            TVA.Text = article.tva.ToString("0.00") + " %";

            // Wholesale Pricing
            if (article.PrixGros > 0)
            {
                PrixGros.Text = article.PrixGros.ToString("0.00") + " DH";
                MinQuantityForGros.Text = article.MinQuantityForGros.ToString() + " unités";
            }
            else
            {
                PrixGros.Text = "N/A";
                MinQuantityForGros.Text = "N/A";
            }

            // Calculate Profit Margin
            if (prixAchat > 0)
            {
                decimal margin = ((prixVente - prixAchat) / prixAchat) * 100;
                ProfitMargin.Text = $"Marge: {margin:0.0}%";
            }
            else
            {
                ProfitMargin.Text = "Marge: N/A";
            }

            // Stock Information
            if (article.IsUnlimitedStock)
            {
                StockQuantite.Text = "∞";
                Quantite.Text = "Illimité";
                StockStatus.Text = "Stock illimité";
                UnlimitedStockBadge.Visibility = Visibility.Visible;
                TotalValue.Text = "∞";
                ValueSubtext.Text = "Stock illimité";
            }
            else
            {
                int quantity = article.Quantite;
                StockQuantite.Text = quantity.ToString();
                Quantite.Text = quantity.ToString();

                // Stock status with color coding
                if (article.MinimumStock > 0 && quantity <= article.MinimumStock)
                {
                    StockStatus.Text = "⚠️ Stock faible";
                    Quantite.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                }
                else if (article.MaximumStock > 0 && quantity >= article.MaximumStock)
                {
                    StockStatus.Text = "📦 Stock plein";
                    Quantite.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                }
                else
                {
                    StockStatus.Text = "✓ En stock";
                    Quantite.Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                }

                // Calculate total value
                decimal totalValue = quantity * prixVente;
                TotalValue.Text = totalValue.ToString("0.00") + " DH";
                ValueSubtext.Text = "Valeur totale en stock";
            }

            MinimumStock.Text = article.MinimumStock > 0 ? article.MinimumStock.ToString() : "N/A";
            MaximumStock.Text = article.MaximumStock > 0 ? article.MaximumStock.ToString() : "N/A";
            StorageLocation.Text = string.IsNullOrWhiteSpace(article.StorageLocation) ? "N/A" : article.StorageLocation;
            UnitOfMeasure.Text = string.IsNullOrWhiteSpace(article.UnitOfMeasure) ? "Pièce" : article.UnitOfMeasure;

            // Perishable badge
            if (article.IsPerishable)
            {
                PerishableBadge.Visibility = Visibility.Visible;
            }

            // Packaging Information
            PiecesPerPackage.Text = article.PiecesPerPackage > 0 ? article.PiecesPerPackage.ToString() : "N/A";
            PackageType.Text = string.IsNullOrWhiteSpace(article.PackageType) ? "N/A" : article.PackageType;
            PackageWeight.Text = article.PackageWeight > 0 ? article.PackageWeight.ToString("0.00") + " Kg" : "N/A";
            PackageDimensions.Text = string.IsNullOrWhiteSpace(article.PackageDimensions) ? "N/A" : article.PackageDimensions;

            // Calculate total packages
            if (article.PiecesPerPackage > 0 && !article.IsUnlimitedStock)
            {
                int totalPackages = (int)Math.Ceiling((double)article.Quantite / article.PiecesPerPackage);
                TotalPackages.Text = $"{totalPackages} emballages";
            }
            else
            {
                TotalPackages.Text = "N/A";
            }

            // Lot Information
            NumeroLot.Text = string.IsNullOrWhiteSpace(article.numeroLot) ? "N/A" : article.numeroLot;
            BonLivraison.Text = string.IsNullOrWhiteSpace(article.bonlivraison) ? "N/A" : article.bonlivraison;

            // Dates
            DateArticle.Text = article.Date.HasValue ? article.Date.Value.ToString("dd/MM/yyyy") : "N/A";
            DateLivraison.Text = article.DateLivraison.HasValue ? article.DateLivraison.Value.ToString("dd/MM/yyyy") : "N/A";
            LastRestockDate.Text = article.LastRestockDate.HasValue ? article.LastRestockDate.Value.ToString("dd/MM/yyyy") : "N/A";

            // Date Expiration with color coding
            if (article.DateExpiration.HasValue)
            {
                DateExpiration.Text = article.DateExpiration.Value.ToString("dd/MM/yyyy");

                TimeSpan timeUntilExpiration = article.DateExpiration.Value - DateTime.Now;

                if (timeUntilExpiration.TotalDays < 0)
                {
                    DateExpiration.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    DateExpiration.FontWeight = FontWeights.Bold;
                    DateExpiration.Text += " ❌ EXPIRÉ";
                }
                else if (timeUntilExpiration.TotalDays <= 30)
                {
                    DateExpiration.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    DateExpiration.FontWeight = FontWeights.SemiBold;
                    int daysLeft = (int)Math.Ceiling(timeUntilExpiration.TotalDays);
                    DateExpiration.Text += $" ⚠️ Expire dans {daysLeft} jours";
                }
                else if (timeUntilExpiration.TotalDays <= 90)
                {
                    DateExpiration.Foreground = new SolidColorBrush(Color.FromRgb(234, 179, 8));
                    DateExpiration.FontWeight = FontWeights.Medium;
                    int daysLeft = (int)Math.Ceiling(timeUntilExpiration.TotalDays);
                    DateExpiration.Text += $" 🔔 {daysLeft} jours restants";
                }
                else
                {
                    DateExpiration.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    DateExpiration.Text += " ✓ Valide";
                }
            }
            else
            {
                DateExpiration.Text = "N/A";
                DateExpiration.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            }

            // Notes
            Notes.Text = string.IsNullOrWhiteSpace(article.Notes) ? "Aucune note disponible" : article.Notes;

            // Load Image
            if (article.ArticleImage != null && article.ArticleImage.Length > 0)
            {
                try
                {
                    // AFTER
                    BitmapImage bitmap = new BitmapImage();
                    using (var ms = new MemoryStream(article.ArticleImage))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                    }
                    bitmap.Freeze();
                    ArticleImageDisplay.Source = bitmap;
                }
                catch (Exception ex)
                {
                    // If image loading fails, show placeholder
                    SetPlaceholderImage();
                }
            }
            else
            {
                SetPlaceholderImage();
            }
        }

        private void SetPlaceholderImage()
        {
            // Create a placeholder TextBlock
            var placeholder = new TextBlock
            {
                Text = "📦",
                FontSize = 80,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };

            // Note: In a real application, you might want to use a proper placeholder image
            // For now, we'll just leave the Image control empty with the background showing
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}