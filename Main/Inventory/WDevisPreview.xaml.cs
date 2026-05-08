using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Superete;

namespace GestionComerce.Main.Inventory
{
    public partial class WDevisPreview : Window
    {
        private List<Article> selectedArticles;
        private List<Famille> allFamilles;
        private List<Fournisseur> allFournisseurs;
        private DevisConfiguration config;
        private Facture companyInfo;
        private string _devisNumber;
        private const int ARTICLES_PER_PAGE = 20;

        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color CNavy  = Color.FromRgb(30,  58,  95);
        private static readonly Color CBlue  = Color.FromRgb(59,  130, 246);
        private static readonly Color CLBlue = Color.FromRgb(239, 246, 255);
        private static readonly Color CDark  = Color.FromRgb(30,  41,  59);
        private static readonly Color CMuted = Color.FromRgb(100, 116, 139);
        private static readonly Color CBdr   = Color.FromRgb(226, 232, 240);
        private static readonly Color CAlt   = Color.FromRgb(248, 250, 252);

        private static SolidColorBrush B(Color c) => new SolidColorBrush(c);
        private static LinearGradientBrush GH(Color a, Color b) => new LinearGradientBrush(a, b, 0);
        private static Border Gap(double h) => new Border { Height = h };

        public WDevisPreview(List<Article> articles, List<Famille> familles,
            List<Fournisseur> fournisseurs, DevisConfiguration configuration)
        {
            InitializeComponent();
            selectedArticles = articles;
            allFamilles      = familles;
            allFournisseurs  = fournisseurs;
            config           = configuration;
            ArticleCountText.Text = $"({articles.Count} article{(articles.Count > 1 ? "s" : "")})";
            LoadCompanyInfo();
        }

        private async void LoadCompanyInfo()
        {
            try
            {
                companyInfo = await new Facture().GetFactureAsync();
                GenerateDevis();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateDevis()
        {
            _devisNumber = $"DEV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            DevisContent.Children.Clear();

            int totalPages = Math.Max(1, (int)Math.Ceiling((double)selectedArticles.Count / ARTICLES_PER_PAGE));

            for (int p = 1; p <= totalPages; p++)
            {
                if (p > 1)
                    DevisContent.Children.Add(new Border
                    {
                        Height = 40,
                        Background = B(Color.FromRgb(243, 244, 246)),
                        Margin = new Thickness(0, 12, 0, 12)
                    });

                DevisContent.Children.Add(BuildPage(p, totalPages));
            }
        }

        // ── Page ──────────────────────────────────────────────────────────────

        private StackPanel BuildPage(int pageNum, int totalPages)
        {
            StackPanel page = new StackPanel();

            // Top gradient accent bar
            page.Children.Add(new Border
            {
                Height = 8,
                Background = GH(CNavy, CBlue)
            });

            StackPanel body = new StackPanel { Margin = new Thickness(52, 40, 52, 40) };

            body.Children.Add(BuildHeader(pageNum));
            body.Children.Add(Gap(28));
            body.Children.Add(new Border { Height = 1, Background = GH(CNavy, CBlue) });
            body.Children.Add(Gap(28));

            if (pageNum == 1 && config.ShowClientSection && !string.IsNullOrWhiteSpace(config.ClientName))
            {
                body.Children.Add(BuildClientCard());
                body.Children.Add(Gap(24));
            }

            body.Children.Add(BuildArticlesTable(pageNum));

            if (pageNum == totalPages)
            {
                body.Children.Add(Gap(32));
                body.Children.Add(BuildTotalsSection());

                bool hasNotes    = config.ShowNotes && !string.IsNullOrWhiteSpace(config.Notes);
                bool hasPayment  = config.ShowPaymentTerms && !string.IsNullOrWhiteSpace(config.PaymentTerms);
                if (hasNotes || hasPayment)
                {
                    body.Children.Add(Gap(24));
                    body.Children.Add(BuildFooterSection());
                }
            }

            body.Children.Add(Gap(36));
            body.Children.Add(new TextBlock
            {
                Text = $"Page {pageNum} / {totalPages}",
                FontSize = 9,
                Foreground = B(CMuted),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            page.Children.Add(body);
            return page;
        }

        // ── Header ────────────────────────────────────────────────────────────

        private Grid BuildHeader(int pageNum)
        {
            // Proportional columns: company takes ~56%, DEVIS card ~44%
            // Both scale together on any page width or print scale
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.56, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.44, GridUnitType.Star) });

            // ── Company panel (left) ──────────────────────────────────────────
            StackPanel company = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            if (config.ShowLogo && !string.IsNullOrWhiteSpace(companyInfo?.LogoPath) &&
                File.Exists(companyInfo.LogoPath))
            {
                try
                {
                    company.Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri(companyInfo.LogoPath, UriKind.Absolute)),
                        Width = 130, Height = 60,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(0, 0, 0, 14)
                    });
                }
                catch { }
            }

            if (config.ShowCompanyName && !string.IsNullOrWhiteSpace(companyInfo?.Name))
                company.Children.Add(new TextBlock
                {
                    Text = companyInfo.Name,
                    FontSize = 20, FontWeight = FontWeights.Bold,
                    Foreground = B(CNavy),
                    Margin = new Thickness(0, 0, 0, 10)
                });

            void AddInfo(bool show, string lbl, string val)
            {
                if (!show || string.IsNullOrWhiteSpace(val)) return;
                StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                row.Children.Add(new TextBlock { Text = lbl + ": ", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = B(CMuted) });
                row.Children.Add(new TextBlock { Text = val,         FontSize = 10, Foreground = B(CDark) });
                company.Children.Add(row);
            }

            AddInfo(config.ShowICE,        "ICE",     companyInfo?.ICE);
            AddInfo(config.ShowVAT,        "TVA",     companyInfo?.VAT);
            AddInfo(config.ShowCompanyId,  "RC",      companyInfo?.CompanyId);
            AddInfo(config.ShowEtatJuridic,"Forme",   companyInfo?.EtatJuridic);
            AddInfo(config.ShowSiege,      "Siège",   companyInfo?.SiegeEntreprise);
            AddInfo(config.ShowTelephone,  "Tél",     companyInfo?.Telephone);
            AddInfo(config.ShowAdresse,    "Adresse", companyInfo?.Adresse);

            // Wrap company info in a card so the left column is visually filled
            Border companyCard = new Border
            {
                Background = B(CAlt),
                BorderBrush = B(CBdr),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 18, 20, 18),
                Child = company,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetColumn(companyCard, 0);
            g.Children.Add(companyCard);

            // ── DEVIS card (right, first page only) ───────────────────────────
            if (pageNum == 1)
            {
                Border card = new Border
                {
                    Background = B(CLBlue),
                    BorderBrush = B(CBlue),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(22, 20, 22, 20),
                    VerticalAlignment = VerticalAlignment.Top
                };

                StackPanel right = new StackPanel();

                // "DEVIS" badge
                right.Children.Add(new Border
                {
                    Background = B(CNavy),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(0, 10, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 0, 0, 16),
                    Child = new TextBlock
                    {
                        Text = "DEVIS",
                        FontSize = 22, FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    }
                });

                void AddRow(string lbl, string val, bool bold = false)
                {
                    Grid row = new Grid { Margin = new Thickness(0, 5, 0, 0) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    TextBlock l = new TextBlock { Text = lbl + ":", FontSize = 10, Foreground = B(CMuted), VerticalAlignment = VerticalAlignment.Center };
                    TextBlock v = new TextBlock
                    {
                        Text = val, FontSize = bold ? 12 : 10,
                        FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                        Foreground = bold ? B(CNavy) : B(CDark),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(l, 0); Grid.SetColumn(v, 1);
                    row.Children.Add(l); row.Children.Add(v);
                    right.Children.Add(row);
                }

                if (config.ShowDevisNumber) AddRow("N°",             _devisNumber, true);
                if (config.ShowDevisDate)   AddRow("Date",           DateTime.Now.ToString("dd/MM/yyyy"));
                if (config.ShowValidity)    AddRow("Valable jusqu'au",
                    DateTime.Now.AddDays(config.ValidityDays).ToString("dd/MM/yyyy"));

                card.Child = right;
                Grid.SetColumn(card, 2);
                g.Children.Add(card);
            }

            return g;
        }

        // ── Client Card ───────────────────────────────────────────────────────

        private Border BuildClientCard()
        {
            Border outer = new Border
            {
                BorderBrush = B(CBdr), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Background = B(CAlt)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border bar = new Border { Background = B(CBlue), CornerRadius = new CornerRadius(10, 0, 0, 10) };
            Grid.SetColumn(bar, 0);

            StackPanel info = new StackPanel { Margin = new Thickness(18, 14, 18, 14) };

            info.Children.Add(new TextBlock
            {
                Text = "DESTINATAIRE",
                FontSize = 9, FontWeight = FontWeights.Bold,
                Foreground = B(CBlue),
                Margin = new Thickness(0, 0, 0, 6)
            });

            if (!string.IsNullOrWhiteSpace(config.ClientName))
                info.Children.Add(new TextBlock
                {
                    Text = config.ClientName,
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = B(CDark), Margin = new Thickness(0, 0, 0, 4)
                });

            if (!string.IsNullOrWhiteSpace(config.ClientICE))
                info.Children.Add(new TextBlock { Text = $"ICE: {config.ClientICE}", FontSize = 10, Foreground = B(CMuted) });

            if (!string.IsNullOrWhiteSpace(config.ClientAddress))
                info.Children.Add(new TextBlock
                {
                    Text = config.ClientAddress, FontSize = 10, Foreground = B(CMuted),
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0)
                });

            Grid.SetColumn(info, 1);
            g.Children.Add(bar);
            g.Children.Add(info);
            outer.Child = g;
            return outer;
        }

        // ── Articles Table ────────────────────────────────────────────────────

        private Grid BuildArticlesTable(int pageNum)
        {
            int startIdx = (pageNum - 1) * ARTICLES_PER_PAGE;
            int endIdx   = Math.Min(startIdx + ARTICLES_PER_PAGE, selectedArticles.Count);

            // (header, width, rightAlign)
            var cols = new List<(string h, GridLength w, bool r)>();
            if (config.ShowCode)         cols.Add(("Code",        new GridLength(58),                  false));
            if (config.ShowArticleName)  cols.Add(("Article",     new GridLength(1, GridUnitType.Star), false));
            if (config.ShowFamille)      cols.Add(("Famille",     new GridLength(80),                  false));
            if (config.ShowFournisseur)  cols.Add(("Fournisseur", new GridLength(90),                  false));
            if (config.ShowMarque)       cols.Add(("Marque",      new GridLength(72),                  false));
            if (config.ShowLot)          cols.Add(("N° Lot",      new GridLength(72),                  false));
            if (config.ShowBonLivraison) cols.Add(("Bon Liv.",    new GridLength(72),                  false));
            if (config.ShowExpiration)   cols.Add(("Expiration",  new GridLength(82),                  false));
            if (config.ShowQuantity)     cols.Add(("Qté",         new GridLength(44),                  true));
            if (config.ShowUnitPrice)    cols.Add(("P.U. HT",    new GridLength(82),                  true));
            if (config.ShowTVA)          cols.Add(("TVA %",       new GridLength(55),                  true));
            if (config.ShowTotalPrice)   cols.Add(("Total HT",    new GridLength(88),                  true));

            if (cols.Count == 0) return new Grid();

            Grid table = new Grid();
            foreach (var c in cols)
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = c.w });

            // ── Header row ────────────────────────────────────────────────────
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid hGrid = MakeRowGrid(cols);
            for (int i = 0; i < cols.Count; i++)
            {
                TextBlock hCell = new TextBlock
                {
                    Text = cols[i].h,
                    FontSize = 10, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 0, 10, 0),
                    HorizontalAlignment = cols[i].r ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    TextAlignment      = cols[i].r ? TextAlignment.Right        : TextAlignment.Left
                };
                Grid.SetColumn(hCell, i);
                hGrid.Children.Add(hCell);
            }

            Border hBorder = new Border
            {
                Background = B(CNavy),
                Padding = new Thickness(0, 12, 0, 12),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Child = hGrid
            };
            Grid.SetRow(hBorder, 0);
            Grid.SetColumnSpan(hBorder, cols.Count);
            table.Children.Add(hBorder);

            // ── Data rows ─────────────────────────────────────────────────────
            bool isLastPage = (pageNum * ARTICLES_PER_PAGE >= selectedArticles.Count);

            for (int idx = startIdx; idx < endIdx; idx++)
            {
                Article art = selectedArticles[idx];
                int rowIdx  = idx - startIdx + 1;
                bool isLast = (idx == endIdx - 1) && isLastPage;
                bool alt    = rowIdx % 2 == 0;

                table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid dGrid = MakeRowGrid(cols);
                int col = 0;

                decimal qty = config.ArticleQuantities.TryGetValue(art.ArticleID, out decimal q) ? q : art.Quantite;

                if (config.ShowCode)         AddCell(dGrid, art.Code.ToString(),                                     col++, false, false);
                if (config.ShowArticleName)  AddCell(dGrid, art.ArticleName ?? "",                                   col++, false, false);
                if (config.ShowFamille)      AddCell(dGrid, GetFamilleName(art.FamillyID),                            col++, false, false);
                if (config.ShowFournisseur)  AddCell(dGrid, GetFournisseurName(art.FournisseurID ?? 0),               col++, false, false);
                if (config.ShowMarque)       AddCell(dGrid, art.marque ?? "",                                         col++, false, false);
                if (config.ShowLot)          AddCell(dGrid, art.numeroLot ?? "",                                      col++, false, false);
                if (config.ShowBonLivraison) AddCell(dGrid, art.bonlivraison ?? "",                                   col++, false, false);
                if (config.ShowExpiration)   AddCell(dGrid, art.DateExpiration.HasValue
                                                 ? art.DateExpiration.Value.ToString("dd/MM/yy") : "—",               col++, false, false);
                if (config.ShowQuantity)     AddCell(dGrid, qty.ToString("0.##"),                                     col++, false, true);
                if (config.ShowUnitPrice)    AddCell(dGrid, $"{art.PrixVente:N2} DH",                                 col++, false, true);
                if (config.ShowTVA)          AddCell(dGrid, $"{art.tva:0.##}%",                                       col++, false, true);
                if (config.ShowTotalPrice)   AddCell(dGrid, $"{art.PrixVente * qty:N2} DH",                           col++, true,  true);

                Border dBorder = new Border
                {
                    Background = alt ? B(CAlt) : Brushes.White,
                    BorderBrush = B(CBdr),
                    BorderThickness = isLast ? new Thickness(0, 0, 0, 0) : new Thickness(0, 0, 0, 1),
                    CornerRadius = isLast ? new CornerRadius(0, 0, 8, 8) : new CornerRadius(0),
                    Padding = new Thickness(0, 10, 0, 10),
                    Child = dGrid
                };
                Grid.SetRow(dBorder, rowIdx);
                Grid.SetColumnSpan(dBorder, cols.Count);
                table.Children.Add(dBorder);
            }

            // Outer rounded border wrapper
            Border tableBorder = new Border
            {
                BorderBrush = B(CBdr), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
            tableBorder.Child = table;

            // Wrap in a Grid so we can return Grid type
            Grid wrapper = new Grid();
            wrapper.Children.Add(tableBorder);
            return wrapper;
        }

        private Grid MakeRowGrid(List<(string h, GridLength w, bool r)> cols)
        {
            Grid g = new Grid();
            foreach (var c in cols)
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = c.w });
            return g;
        }

        private void AddCell(Grid grid, string text, int col, bool bold, bool rightAlign)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = bold ? B(CNavy) : B(CDark),
                Padding = new Thickness(10, 0, 10, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = rightAlign ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                TextAlignment      = rightAlign ? TextAlignment.Right        : TextAlignment.Left
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        // ── Totals ────────────────────────────────────────────────────────────

        private Grid BuildTotalsSection()
        {
            decimal subtotal = 0, totalTVA = 0;
            foreach (Article art in selectedArticles)
            {
                decimal qty = config.ArticleQuantities.TryGetValue(art.ArticleID, out decimal q) ? q : art.Quantite;
                decimal ht  = art.PrixVente * qty;
                subtotal  += ht;
                totalTVA  += ht * art.tva / 100m;
            }
            decimal grandTotal = subtotal + totalTVA;

            // Full-width Grid: left=spacer, right=card (320px)
            Grid outer = new Grid();
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });

            Border card = new Border
            {
                BorderBrush = B(CBdr), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Background = Brushes.White
            };

            StackPanel sp = new StackPanel();

            // Title strip
            sp.Children.Add(new Border
            {
                Background = B(CAlt),
                CornerRadius = new CornerRadius(10, 10, 0, 0),
                Padding = new Thickness(20, 12, 20, 12),
                Child = new TextBlock
                {
                    Text = "RÉCAPITULATIF",
                    FontSize = 9, FontWeight = FontWeights.Bold,
                    Foreground = B(CMuted)
                }
            });

            sp.Children.Add(new Border { Height = 1, Background = B(CBdr) });

            // Line rows
            StackPanel lines = new StackPanel { Margin = new Thickness(20, 14, 20, 14) };

            void AddLine(string lbl, string val, bool accent = false)
            {
                Grid row = new Grid { Margin = new Thickness(0, 5, 0, 5) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                TextBlock l = new TextBlock { Text = lbl, FontSize = 12, Foreground = B(CMuted) };
                TextBlock v = new TextBlock
                {
                    Text = val, FontSize = 12,
                    FontWeight = accent ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = B(CDark),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(l, 0); Grid.SetColumn(v, 1);
                row.Children.Add(l); row.Children.Add(v);
                lines.Children.Add(row);
            }

            if (config.ShowSubtotal)  AddLine("Sous-total HT", $"{subtotal:N2} DH");
            if (config.ShowTVATotal)  AddLine("Total TVA",     $"{totalTVA:N2} DH");

            sp.Children.Add(lines);

            // Grand total band
            if (config.ShowGrandTotal)
            {
                sp.Children.Add(new Border { Height = 1, Background = B(CBdr) });

                Grid gtRow = new Grid { Margin = new Thickness(0) };
                gtRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                gtRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock gtL = new TextBlock
                {
                    Text = "TOTAL TTC",
                    FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock gtV = new TextBlock
                {
                    Text = $"{grandTotal:N2} DH",
                    FontSize = 16, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(gtL, 0); Grid.SetColumn(gtV, 1);
                gtRow.Children.Add(gtL); gtRow.Children.Add(gtV);

                sp.Children.Add(new Border
                {
                    Background = B(CNavy),
                    CornerRadius = new CornerRadius(0, 0, 10, 10),
                    Padding = new Thickness(20, 16, 20, 16),
                    Child = gtRow
                });
            }

            card.Child = sp;
            Grid.SetColumn(card, 1);
            outer.Children.Add(card);
            return outer;
        }

        // ── Footer ────────────────────────────────────────────────────────────

        private StackPanel BuildFooterSection()
        {
            StackPanel footer = new StackPanel();

            bool hasPayment = config.ShowPaymentTerms && !string.IsNullOrWhiteSpace(config.PaymentTerms);
            bool hasNotes   = config.ShowNotes && !string.IsNullOrWhiteSpace(config.Notes);

            if (hasPayment && hasNotes)
            {
                // Side by side
                Grid twoCol = new Grid();
                twoCol.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                twoCol.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                twoCol.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Border pc = MakeFooterCard("CONDITIONS DE PAIEMENT", config.PaymentTerms, CBlue, Color.FromRgb(239, 246, 255));
                Border nc = MakeFooterCard("NOTES / REMARQUES",       config.Notes,        Color.FromRgb(180, 120, 20), Color.FromRgb(255, 251, 235));

                Grid.SetColumn(pc, 0); Grid.SetColumn(nc, 2);
                twoCol.Children.Add(pc); twoCol.Children.Add(nc);
                footer.Children.Add(twoCol);
            }
            else
            {
                if (hasPayment)
                    footer.Children.Add(MakeFooterCard("CONDITIONS DE PAIEMENT", config.PaymentTerms,
                        CBlue, Color.FromRgb(239, 246, 255)));
                if (hasNotes)
                    footer.Children.Add(MakeFooterCard("NOTES / REMARQUES", config.Notes,
                        Color.FromRgb(180, 120, 20), Color.FromRgb(255, 251, 235)));
            }

            return footer;
        }

        private Border MakeFooterCard(string title, string text, Color barColor, Color bgColor)
        {
            Border outer = new Border
            {
                BorderBrush = B(CBdr), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Background = B(bgColor)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border bar = new Border { Background = B(barColor), CornerRadius = new CornerRadius(10, 0, 0, 10) };
            Grid.SetColumn(bar, 0);

            StackPanel sp = new StackPanel { Margin = new Thickness(16, 14, 16, 14) };
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 9, FontWeight = FontWeights.Bold,
                Foreground = B(barColor),
                Margin = new Thickness(0, 0, 0, 8)
            });
            sp.Children.Add(new TextBlock
            {
                Text = text, FontSize = 11,
                Foreground = B(CDark),
                TextWrapping = TextWrapping.Wrap
            });

            Grid.SetColumn(sp, 1);
            g.Children.Add(bar);
            g.Children.Add(sp);
            outer.Child = g;
            return outer;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GetFamilleName(int familleId)
        {
            foreach (var f in allFamilles)
                if (f.FamilleID == familleId) return f.FamilleName;
            return "";
        }

        private string GetFournisseurName(int fournisseurId)
        {
            foreach (var f in allFournisseurs)
                if (f.FournisseurID == fournisseurId) return f.Nom;
            return "";
        }

        // ── Toolbar actions ───────────────────────────────────────────────────

        private async void SaveDevis_Click(object sender, RoutedEventArgs e)
        {
            SaveDevisButton.IsEnabled = false;
            SaveDevisButton.Content   = "Enregistrement...";

            try
            {
                decimal subtotal = 0, totalTVA = 0;
                foreach (var art in selectedArticles)
                {
                    decimal qty = config.ArticleQuantities.TryGetValue(art.ArticleID, out decimal q) ? q : art.Quantite;
                    decimal ht  = art.PrixVente * qty;
                    subtotal  += ht;
                    totalTVA  += ht * art.tva / 100m;
                }
                decimal grandTotal = subtotal + totalTVA;

                var invoice = new Invoice
                {
                    InvoiceNumber    = _devisNumber,
                    InvoiceDate      = DateTime.Now,
                    InvoiceType      = "Devis",
                    ClientName       = config.ShowClientSection ? config.ClientName    : "",
                    ClientICE        = config.ShowClientSection ? config.ClientICE     : "",
                    ClientAddress    = config.ShowClientSection ? config.ClientAddress  : "",
                    UserName         = companyInfo?.Name,
                    UserICE          = companyInfo?.ICE,
                    UserVAT          = companyInfo?.VAT,
                    UserPhone        = companyInfo?.Telephone,
                    UserAddress      = companyInfo?.Adresse,
                    UserEtatJuridique= companyInfo?.EtatJuridic,
                    UserIdSociete    = companyInfo?.CompanyId,
                    UserSiegeEntreprise = companyInfo?.SiegeEntreprise,
                    TotalHT          = subtotal,
                    TotalTVA         = totalTVA,
                    TotalTTC         = grandTotal,
                    TotalAfterRemise = grandTotal,
                    Description      = config.ShowNotes ? config.Notes : "",
                    EtatFacture      = 1
                };

                var svc = new Invoice();
                int invoiceId = await svc.CreateInvoiceAsync(invoice);
                if (invoiceId <= 0)
                {
                    SaveDevisButton.IsEnabled = true;
                    SaveDevisButton.Content   = "💾 Enregistrer";
                    return;
                }

                foreach (var art in selectedArticles)
                {
                    decimal qty = config.ArticleQuantities.TryGetValue(art.ArticleID, out decimal q2) ? q2 : art.Quantite;
                    decimal ht  = art.PrixVente * qty;
                    decimal tva = ht * art.tva / 100m;

                    await svc.AddInvoiceArticleAsync(new Invoice.InvoiceArticle
                    {
                        InvoiceID    = invoiceId,
                        ArticleID    = art.ArticleID,
                        ArticleName  = art.ArticleName ?? art.Code.ToString(),
                        PrixUnitaire = art.PrixVente,
                        Quantite     = qty,
                        TVA          = art.tva,
                        TotalHT      = ht,
                        MontantTVA   = tva,
                        TotalTTC     = ht + tva
                    });
                }

                SaveDevisButton.Content = "✓ Enregistré";
                MessageBox.Show(
                    $"Devis {_devisNumber} enregistré avec succès!\nAccessible dans Historique Facture.",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SaveDevisButton.IsEnabled = true;
                SaveDevisButton.Content   = "💾 Enregistrer";
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // A4 at 96 dpi: 794 × 1123 px
        private const double A4_W = 793.7;
        private const double A4_H = 1122.5;

        private void PrintToDialog(PrintDialog pd)
        {
            double pageW = pd.PrintableAreaWidth;
            double pageH = pd.PrintableAreaHeight;

            var savedEffect = PrintArea.Effect;
            PrintArea.Effect = null;

            double scale = Math.Min(pageW / PrintArea.ActualWidth,
                                    pageH / PrintArea.ActualHeight);
            if (scale > 1.0) scale = 1.0;

            PrintArea.LayoutTransform = new ScaleTransform(scale, scale);
            PrintArea.Measure(new Size(pageW, pageH));
            PrintArea.Arrange(new Rect(0, 0, pageW, pageH));

            pd.PrintVisual(PrintArea, $"Devis {_devisNumber}");

            PrintArea.LayoutTransform = null;
            PrintArea.Effect = savedEffect;
            PrintArea.InvalidateMeasure();
            PrintArea.UpdateLayout();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog pd = new PrintDialog();
                try
                {
                    pd.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(
                        System.Printing.PageMediaSizeName.ISOA4);
                    pd.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;
                }
                catch { /* ignore if System.Printing not available */ }

                if (pd.ShowDialog() != true) return;
                PrintToDialog(pd);
                MessageBox.Show("Impression lancée avec succès!", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog pd = new PrintDialog();
                try
                {
                    pd.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(
                        System.Printing.PageMediaSizeName.ISOA4);
                    pd.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;
                }
                catch { /* ignore if System.Printing not available */ }

                MessageBox.Show(
                    "Dans la fenêtre d'impression, choisissez\n'Microsoft Print to PDF' comme imprimante\npour enregistrer le devis au format A4.",
                    "Enregistrer PDF", MessageBoxButton.OK, MessageBoxImage.Information);

                if (pd.ShowDialog() != true) return;
                PrintToDialog(pd);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
