using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GestionComerce.Main.Vente
{
    public partial class CSingleArticle1 : UserControl
    {
        CMainV mainv;
        public Article a;
        List<Famille> lf;
        List<Fournisseur> lfo;
        private string iconSize;

        // ── Session-level per-article suppression ──────────────────────────────
        // Once the user clicks OK on a stock warning for a specific article
        // (zero-stock OR over-stock), that article's warning is suppressed for
        // the rest of the session — no checkbox required.
        private static readonly HashSet<int> _sessionSuppressedArticles = new HashSet<int>();

        public CSingleArticle1(Article a, CMainV mainv, List<Famille> lf, List<Fournisseur> lfo, int s, string iconSize = "Moyennes")
        {
            InitializeComponent();

            this.a = a;
            this.mainv = mainv;
            this.lf = lf;
            this.lfo = lfo;
            this.iconSize = iconSize;

            // Load article image
            LoadArticleImage();

            // Set common data
            string familleName = "";
            foreach (Famille f in lf)
            {
                if (f.FamilleID == a.FamillyID)
                {
                    familleName = f.FamilleName;
                    break;
                }
            }

            string fournisseurName = "";
            foreach (Fournisseur fo in lfo)
            {
                if (a.FournisseurID == fo.FournisseurID)
                {
                    fournisseurName = fo.Nom;
                    break;
                }
            }

            if (s == 0) // Row layout (normal)
            {
                ApplyRowLayout();

                ArticleID.Text = a.ArticleID.ToString();
                ArticleName.Text = a.ArticleName;
                PrixVente.Text = a.PrixVente.ToString("F2") + " DH";
                Quantite.Text = a.Quantite.ToString();
                PrixAchat.Text = a.PrixAchat.ToString("F2") + " DH";
                PrixMP.Text = a.PrixMP.ToString("F2") + " DH";
                Famille.Text = familleName;
                FournisseurName.Text = fournisseurName;
                Code.Text = a.Code.ToString();
            }
            else if (s == 1) // Row layout (compact - for selected article preview)
            {
                ApplyRowLayout();

                ArticleID.Visibility = Visibility.Collapsed;
                ArticleIDC.Width = new GridLength(0);
                PrixAchat.Visibility = Visibility.Collapsed;
                PrixAchatC.Width = new GridLength(0);
                PrixVente.Visibility = Visibility.Collapsed;
                PrixVenteC.Width = new GridLength(0);
                PrixMP.Visibility = Visibility.Collapsed;
                PrixMPC.Width = new GridLength(0);
                Famille.Visibility = Visibility.Collapsed;
                FamilleC.Width = new GridLength(0);
                ImageColumnRow.Width = new GridLength(0);

                ArticleIDC.MinWidth = 0;
                PrixAchatC.MinWidth = 0;
                PrixVenteC.MinWidth = 0;
                PrixMPC.MinWidth = 0;
                FamilleC.MinWidth = 0;
                ImageColumnRow.MinWidth = 0;

                ArticleNameC.MinWidth = 30;
                QuantiteC.MinWidth = 30;
                FournisseurNameC.MinWidth = 30;
                CodeC.MinWidth = 30;

                ArticleNameC.Width = new GridLength(1, GridUnitType.Star);
                QuantiteC.Width = new GridLength(1, GridUnitType.Star);
                FournisseurNameC.Width = new GridLength(1, GridUnitType.Star);
                CodeC.Width = new GridLength(1, GridUnitType.Star);

                ArticleName.Text = a.ArticleName;
                Quantite.Text = a.Quantite.ToString();
                FournisseurName.Text = fournisseurName;
                Code.Text = a.Code.ToString();
            }
            else if (s == 2) // Card layout
            {
                ApplyCardLayout();

                CardArticleName.Text = a.ArticleName;
                CardPrixAchat.Text = a.PrixAchat.ToString("F2") + " DH";
                CardPrixVente.Text = a.PrixVente.ToString("F2") + " DH";
                CardFournisseur.Text = fournisseurName;
                CardFamille.Text = familleName;
                CardCode.Text = a.Code.ToString();
                CardQuantite.Text = a.Quantite.ToString();
            }
        }

        // ── Stock warning helpers ───────────────────────────────────────────────

        /// <summary>
        /// Shows a stock warning dialog for the given article and reason.
        /// Returns true if the user confirms (and suppresses future warnings for
        /// this article in the current session).
        /// Returns true immediately if the article was already suppressed.
        /// </summary>
        private static bool ConfirmStockWarning(int articleId, string articleName, string message)
        {
            // Already confirmed once this session — skip the dialog
            if (_sessionSuppressedArticles.Contains(articleId))
                return true;

            // Check the global "ne plus me rappeler" flag from StockWarningHelper
            // (covers the "never show again for any article" checkbox)
            if (StockWarningHelper.IsGloballySuppressed)
                return true;

            // Build the warning window
            var dlg = new Window
            {
                Title = "Avertissement de stock",
                Width = 380,
                Height = 240,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var root = new StackPanel { Margin = new Thickness(20) };

            root.Children.Add(new TextBlock
            {
                Text = $"⚠  {articleName}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 8)
            });

            root.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var chk = new CheckBox
            {
                Content = "Ne plus me rappeler pour aucun article",
                Margin = new Thickness(0, 0, 0, 12)
            };
            root.Children.Add(chk);

            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            bool confirmed = false;

            var btnOk = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };
            btnOk.Click += (s, e) => { confirmed = true; dlg.Close(); };

            var btnCancel = new Button
            {
                Content = "Annuler",
                Width = 80,
                Height = 30,
                IsCancel = true
            };
            btnCancel.Click += (s, e) => { confirmed = false; dlg.Close(); };

            btnRow.Children.Add(btnOk);
            btnRow.Children.Add(btnCancel);
            root.Children.Add(btnRow);

            dlg.Content = root;
            dlg.ShowDialog();

            if (confirmed)
            {
                // Suppress globally if checkbox was ticked
                if (chk.IsChecked == true)
                    StockWarningHelper.SetGloballySuppressed();

                // Always suppress this specific article for the rest of the session
                _sessionSuppressedArticles.Add(articleId);
            }

            return confirmed;
        }

        // ── Article click handler ──────────────────────────────────────────────

        private void ArticleClicked(object sender, MouseButtonEventArgs e)
        {
            // ── Case 1 : Zero stock ────────────────────────────────────────────
            if (a.Quantite <= 0)
            {
                bool proceed = ConfirmStockWarning(
                    a.ArticleID,
                    a.ArticleName,
                    "Cet article est en rupture de stock. Voulez-vous quand même l'ajouter au panier ?");

                if (!proceed)
                    return;
            }

            foreach (UIElement element in mainv.SelectedArticles.Children)
            {
                if (element is CSingleArticle2 item && item.a.ArticleID == a.ArticleID)
                {
                    // ── Case 2 : Cart quantity has reached available stock ──────
                    if (a.Quantite > 0 && a.Quantite <= Convert.ToInt32(item.Quantite.Text))
                    {
                        bool proceed = ConfirmStockWarning(
                            a.ArticleID,
                            a.ArticleName,
                            $"La quantité dans le panier ({item.Quantite.Text}) a atteint le stock disponible ({a.Quantite}). Voulez-vous quand même continuer ?");

                        if (!proceed)
                            return;
                    }

                    item.Quantite.Text = (Convert.ToInt32(item.Quantite.Text) + 1).ToString();
                    item.qte++;
                    mainv.TotalNett += a.PrixVente;
                    mainv.TotalNet.Text = mainv.TotalNett.ToString("F2") + " DH";
                    mainv.NbrA += 1;
                    mainv.ArticleCount.Text = mainv.NbrA.ToString();
                    return;
                }
            }

            mainv.TotalNett += a.PrixVente;
            mainv.TotalNet.Text = mainv.TotalNett.ToString("F2") + " DH";
            mainv.NbrA += 1;
            mainv.ArticleCount.Text = mainv.NbrA.ToString();
            CSingleArticle2 sa = new CSingleArticle2(a, 1, mainv);
            mainv.SelectedArticles.Children.Add(sa);
            mainv.UpdateCartEmptyState();
            mainv.SelectedArticle.Child = new CSingleArticle1(a, mainv, lf, lfo, 1);
        }

        // ── Layout helpers (unchanged) ─────────────────────────────────────────

        private void ApplyRowLayout()
        {
            this.Width = double.NaN;
            this.Height = 48;
            this.Margin = new Thickness(0, 0, 0, 0);

            RowLayout.Visibility = Visibility.Visible;
            CardLayout.Visibility = Visibility.Collapsed;
        }

        private void ApplyCardLayout()
        {
            int cardHeight = 0;
            int imageHeight = 0;

            switch (iconSize)
            {
                case "Grandes":
                    cardHeight = 320;
                    imageHeight = 160;
                    break;
                case "Moyennes":
                    cardHeight = 310;
                    imageHeight = 140;
                    break;
                case "Petites":
                    cardHeight = 180;
                    imageHeight = 100;
                    break;
                default:
                    cardHeight = 340;
                    imageHeight = 140;
                    break;
            }

            this.Width = double.NaN;
            this.Height = cardHeight;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;

            if (CardLayout != null && CardLayout.Child is Grid cardGrid)
            {
                if (cardGrid.RowDefinitions.Count > 0)
                    cardGrid.RowDefinitions[0].Height = new GridLength(imageHeight);
            }

            RowLayout.Visibility = Visibility.Collapsed;
            CardLayout.Visibility = Visibility.Visible;

            UpdateCardVisibility();
        }

        private void UpdateCardVisibility()
        {
            if (CardLayout?.Child is Grid cardGrid && cardGrid.Children.Count > 1)
            {
                var infoPanel = cardGrid.Children[1] as StackPanel;
                if (infoPanel != null)
                {
                    if (iconSize == "Petites")
                    {
                        foreach (UIElement child in infoPanel.Children)
                        {
                            if (child is StackPanel panel)
                            {
                                panel.Visibility = (panel.Name == "FournisseurPanel" || panel.Name == "StockPanel")
                                    ? Visibility.Collapsed
                                    : Visibility.Visible;
                            }
                            else if (child is Grid grid)
                            {
                                grid.Visibility = grid.Name == "FamilleCodeGrid"
                                    ? Visibility.Collapsed
                                    : Visibility.Visible;
                            }
                            else
                            {
                                child.Visibility = Visibility.Visible;
                            }
                        }
                    }
                    else
                    {
                        foreach (UIElement child in infoPanel.Children)
                            child.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        // BEFORE — duplicated check + stream may be collected before render
        // AFTER — clean, safe, Freeze() added
        private void LoadArticleImage()
        {
            try
            {
                if (a.ArticleImage != null && a.ArticleImage.Length > 0)
                {
                    BitmapImage bitmap = new BitmapImage();
                    using (var ms = new MemoryStream(a.ArticleImage))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                    }
                    bitmap.Freeze();

                    RowArticleImage.Source = bitmap;
                    CardArticleImage.Source = bitmap;
                }
                // No else needed — leaving Source as null shows nothing, which is correct
            }
            catch
            {
                // Image loading failed, leave empty
            }
        }
    }
}
