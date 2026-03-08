using GestionComerce;
using GestionComerce.Main.Facturation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using Microsoft.Win32;
using System.Windows.Markup;
using System.Globalization;
using System.Printing;

namespace GestionComerce.Main.Facturation.CreateFacture
{
    public partial class WFacturePage : Window
    {
        public CMainFa main;
        Dictionary<string, string> FactureInfo;
        private InvoiceRepository _invoiceRepository;
        private bool _hideEmptyLabels = false;
        private List<List<InvoiceArticle>> _paginatedArticles;
        private Dictionary<int, Canvas> _canvasCache = new Dictionary<int, Canvas>();
        private List<InvoiceArticle> _invoiceArticles; // flat list used for saving

        int PageCounte = 1;
        int TotalPageCount = 1;

        public WFacturePage(CMainFa main, Dictionary<string, string> FactureInfo, List<InvoiceArticle> invoiceArticles = null)
        {
            InitializeComponent();
            this.main = main;
            this.FactureInfo = FactureInfo;

            LoadEmptyLabelsSetting();

            _invoiceRepository = new InvoiceRepository("");

            // Store the flat article list – used by SaveInvoiceArticlesAsync
            _invoiceArticles = invoiceArticles
                               ?? main?.InvoiceArticles
                               ?? new List<InvoiceArticle>();

            List<InvoiceArticle> articlesToUse = _invoiceArticles;

            if (articlesToUse != null && articlesToUse.Count > 0)
            {
                // **MODIFIED: Invoice status is now independent of reversed articles**
                // We show ALL articles regardless of their reversed status
                // The invoice status (Normal/Cancelled) is just a label chosen by the user
                var filteredArticles = articlesToUse.ToList();

                string invoiceType = GetDictionaryValue("Type", "").ToLower();
                if (invoiceType != "expedition")
                {
                    // Only filter out articles with quantity 0 for non-expedition invoices
                    filteredArticles = filteredArticles.Where(ia => ia.Quantite > 0).ToList();
                }

                List<InvoiceArticle> mergedArticles = MergeArticlesForDisplay(filteredArticles);
                _paginatedArticles = PaginateArticles(mergedArticles);
            }
            else
            {
                _paginatedArticles = new List<List<InvoiceArticle>>();
            }

            this.Loaded += async (s, e) =>
            {
                await LoadArticlesAsync();
            };
        }

        private async Task LoadArticlesAsync()
        {
            FacturesContainer.Children.Clear();

            string invoiceType = GetDictionaryValue("Type", "").ToLower();
            bool isCheckType = (invoiceType == "credit" || invoiceType == "cheque");

            // ✅ FIX: Always ensure at least one (possibly empty) page exists.
            // This handles: credit/cheque invoices, invoices with no articles loaded,
            // and invoices viewed from history where articles may be empty.
            if (_paginatedArticles == null || _paginatedArticles.Count == 0)
            {
                _paginatedArticles = new List<List<InvoiceArticle>> { new List<InvoiceArticle>() };
            }

            TotalPageCount = _paginatedArticles.Count;
            TotalPageNbr.Text = "/" + TotalPageCount.ToString();

            // Load first page immediately
            await Task.Run(() => Dispatcher.Invoke(() => LoadPage(0, isCheckType)));

            // Load remaining pages in background
            if (TotalPageCount > 1)
            {
                for (int i = 1; i < TotalPageCount; i++)
                {
                    int pageIndex = i;
                    await Task.Run(() => Dispatcher.Invoke(() => LoadPage(pageIndex, isCheckType)));
                }
            }
        }

        private void LoadPage(int pageIndex, bool isCheckType)
        {
            if (_canvasCache.ContainsKey(pageIndex))
            {
                var existingCanvas = _canvasCache[pageIndex];
                existingCanvas.Visibility = (pageIndex == 0) ? Visibility.Visible : Visibility.Collapsed;
                if (!FacturesContainer.Children.Contains(existingCanvas))
                {
                    FacturesContainer.Children.Add(existingCanvas);
                }
                return;
            }

            var currentPage = _paginatedArticles[pageIndex];
            string[] templates = GetTemplateSet();
            string template = GetTemplateForPage(pageIndex, _paginatedArticles.Count, templates);
            bool isFirstPage = (pageIndex == 0);
            bool isLastPage = (pageIndex == _paginatedArticles.Count - 1);
            bool isSinglePage = (_paginatedArticles.Count == 1);
            string invoiceType = GetDictionaryValue("Type", "").ToLower();
            bool isExpeditionType = (invoiceType == "expedition");

            Canvas mainCanvas = new Canvas
            {
                Height = (invoiceType == "credit") ? 750 : 1050,
                Width = 720,
                Name = $"Canvas{pageIndex + 1}",
                Visibility = (pageIndex == 0) ? Visibility.Visible : Visibility.Collapsed
            };

            Image image = new Image
            {
                Source = new BitmapImage(new Uri($"/Main/images/{template}", UriKind.Relative)),
                Stretch = Stretch.Fill,
                Height = (invoiceType == "credit") ? 750 : 1050,
                Width = 720
            };
            mainCanvas.Children.Add(image);

            if (isFirstPage || isSinglePage)
            {
                TextBlock topRightHeader = CreateTopRightHeader();
                mainCanvas.Children.Add(topRightHeader);

                Grid logoContainer = CreateLogoPlaceholder();
                mainCanvas.Children.Add(logoContainer);

                Grid headerGrid = CreateHeaderGrid(isCheckType);
                mainCanvas.Children.Add(headerGrid);

                PopulateHeaderData(mainCanvas, isCheckType);

                StackPanel objectPanel = CreateObjectPanel(isCheckType);
                mainCanvas.Children.Add(objectPanel);
                PopulateObjectData(objectPanel);

                if (invoiceType == "credit")
                {
                    StackPanel creditPanel = CreateCreditInfoPanel();
                    Canvas.SetLeft(creditPanel, 91);
                    Canvas.SetTop(creditPanel, 430);
                    mainCanvas.Children.Add(creditPanel);
                    PopulateCreditData(creditPanel);
                }
            }

            if (isSinglePage || isLastPage)
            {
                StackPanel montantEnLettresPanel = CreateMontantEnLettresPanel(isCheckType);
                mainCanvas.Children.Add(montantEnLettresPanel);
                PopulateMontantEnLettresData(montantEnLettresPanel);

                StackPanel summaryPanel = CreateSummaryPanel(isCheckType);
                mainCanvas.Children.Add(summaryPanel);
                PopulateSummaryData(summaryPanel);
            }

            if (!isCheckType)
            {
                (double top, double height) = GetStackPanelLayoutForTemplate(template);

                StackPanel articlesContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Background = new SolidColorBrush(Colors.White),
                    Width = 562,
                    Height = height,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Name = "ArticlesContainer"
                };

                Canvas.SetLeft(articlesContainer, 82);
                Canvas.SetTop(articlesContainer, top);
                mainCanvas.Children.Add(articlesContainer);

                foreach (var invoiceArticle in currentPage)
                {
                    bool showExpeditionTotal = isExpeditionType;

                    articlesContainer.Children.Add(
                        CreateArticleRow(
                            invoiceArticle.ArticleName,
                            (double)invoiceArticle.Prix,
                            (double)invoiceArticle.Quantite,
                            (double)invoiceArticle.TVA,
                            (double)invoiceArticle.InitialQuantity,
                            showExpeditionTotal
                        )
                    );
                }
            }

            StackPanel footerPanel = CreateFooterPanel();
            Canvas.SetTop(footerPanel, isCheckType ? 680 : 980);
            mainCanvas.Children.Add(footerPanel);
            PopulateFooterData(footerPanel);

            _canvasCache[pageIndex] = mainCanvas;
            FacturesContainer.Children.Add(mainCanvas);
        }

        private void LoadEmptyLabelsSetting()
        {
            try
            {
                if (main?.u != null)
                {
                    var parametres = Superete.ParametresGeneraux.ObtenirParametresParUserId(
                        main.u.UserID,
                        "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;"
                    );

                    if (parametres != null)
                    {
                        _hideEmptyLabels = parametres.MasquerEtiquettesVides;
                    }
                }
            }
            catch
            {
                _hideEmptyLabels = false;
            }
        }

        private List<InvoiceArticle> MergeArticlesForDisplay(List<InvoiceArticle> articles)
        {
            var mergedArticles = new List<InvoiceArticle>();
            var processedGroups = new HashSet<string>();

            foreach (var article in articles)
            {
                string groupKey = $"{article.ArticleID}_{article.ArticleName}_{article.Prix}_{article.TVA}";

                if (processedGroups.Contains(groupKey))
                    continue;

                var identicalArticles = articles
                    .Where(ia => ia.ArticleID == article.ArticleID &&
                                ia.ArticleName == article.ArticleName &&
                                ia.Prix == article.Prix &&
                                ia.TVA == article.TVA)
                    .ToList();

                if (identicalArticles.Count > 1)
                {
                    var mergedArticle = new InvoiceArticle
                    {
                        OperationID = identicalArticles.First().OperationID,
                        ArticleID = article.ArticleID,
                        ArticleName = article.ArticleName,
                        Prix = article.Prix,
                        TVA = article.TVA,
                        Reversed = article.Reversed,
                        Quantite = identicalArticles.Sum(a => a.Quantite),
                        InitialQuantity = identicalArticles.Sum(a => a.InitialQuantity),
                        ExpeditionTotal = identicalArticles.Sum(a => a.ExpeditionTotal)
                    };

                    mergedArticles.Add(mergedArticle);
                }
                else
                {
                    mergedArticles.Add(identicalArticles.First());
                }

                processedGroups.Add(groupKey);
            }

            return mergedArticles;
        }

        private List<List<InvoiceArticle>> PaginateArticles(List<InvoiceArticle> articles)
        {
            List<List<InvoiceArticle>> pages = new List<List<InvoiceArticle>>();
            List<InvoiceArticle> currentPage = new List<InvoiceArticle>();
            bool isFirstPage = true;

            foreach (var article in articles)
            {
                currentPage.Add(article);

                int pageLimit = isFirstPage ? 11 : 23;

                if (currentPage.Count >= pageLimit)
                {
                    pages.Add(currentPage);
                    currentPage = new List<InvoiceArticle>();
                    isFirstPage = false;
                }
            }

            if (currentPage.Count > 0)
            {
                pages.Add(currentPage);
            }

            return pages;
        }

        private string[] GetTemplateSet()
        {
            string invoiceType = GetDictionaryValue("Type", "").ToLower();

            if (invoiceType == "expedition")
            {
                return new string[] { "1E.png", "2E.png", "3E.png", "4E.png" };
            }
            else if (invoiceType == "credit" || invoiceType == "cheque")
            {
                return new string[] { "check.png" };
            }
            else if (invoiceType == "bon livraison" || invoiceType == "bon de livraison")
            {
                return new string[] { "10.png", "2.png", "3.png", "13.png" };
            }
            else
            {
                return new string[] { "1.png", "2.png", "3.png", "4.png" };
            }
        }

        private string GetInvoiceNumberLabel()
        {
            string invoiceType = GetDictionaryValue("Type", "").ToLower();

            if (invoiceType == "credit")
                return "Numéro : ";
            else if (invoiceType == "bon commande" || invoiceType == "bon de commande")
                return "Numéro : ";
            else if (invoiceType == "bon livraison" || invoiceType == "bon de livraison")
                return "Numéro : ";
            else
                return "Numéro : ";
        }

        private TextBlock CreateTopRightHeader()
        {
            TextBlock header = new TextBlock
            {
                Name = "txtDisplayType",
                Text = GetDictionaryValue("Type", ""),
                FontSize = 50,
                Background = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                Width = 500,
                Height = 100,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextAlignment = TextAlignment.Left
            };

            Canvas.SetLeft(header, 20);
            Canvas.SetTop(header, 50);

            return header;
        }

        private void PopulateCreditData(StackPanel panel)
        {
            string creditClientName = GetDictionaryValue("CreditClientName");
            string creditMontant = GetDictionaryValue("CreditMontant");

            SetTextBlockValue(panel, "txtCreditClientName", creditClientName);
            SetTextBlockValue(panel, "txtCreditMontant", creditMontant);
        }

        private void PopulateObjectData(StackPanel panel)
        {
            TextBlock objectBlock = FindLogicalChild<TextBlock>(panel, "txtDisplayObject");
            if (objectBlock != null)
            {
                objectBlock.Text = GetDictionaryValue("Object");

                if (_hideEmptyLabels && string.IsNullOrWhiteSpace(objectBlock.Text))
                {
                    panel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void PopulateMontantEnLettresData(StackPanel panel)
        {
            TextBlock descriptionBlock = FindLogicalChild<TextBlock>(panel, "txtDescription");
            if (descriptionBlock != null)
            {
                string description = GetDictionaryValue("Description");
                if (string.IsNullOrWhiteSpace(description))
                {
                    descriptionBlock.Visibility = Visibility.Collapsed;
                    var parent = descriptionBlock.Parent as StackPanel;
                    if (parent?.Children.Count > 0 && parent.Children[0] is TextBlock label)
                    {
                        label.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    descriptionBlock.Text = description;
                }
            }

            TextBlock amountBlock = FindLogicalChild<TextBlock>(panel, "txtDisplayAmountInLetters");
            if (amountBlock != null)
            {
                amountBlock.Text = GetDictionaryValue("AmountInLetters");
            }
        }

        private Grid CreateArticleRow(string name, double price, double actualQuantity, double tvaRate, double initialQuantity = 0, bool showExpeditionTotal = false)
        {
            Grid articleRow = new Grid
            {
                Width = 562,
                Height = 18,
                Margin = new Thickness(0, 3, 0, 3)
            };

            if (showExpeditionTotal)
            {
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.5, GridUnitType.Star) });
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) });
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });
            }
            else
            {
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.6, GridUnitType.Star) });
                articleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
            }

            TextBlock nameBlock = new TextBlock
            {
                Text = name,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            TextBlock priceBlock = new TextBlock
            {
                Text = price.ToString("0.00"),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 11
            };

            TextBlock qtyBlock = new TextBlock
            {
                Text = showExpeditionTotal ? initialQuantity.ToString("0.##") : actualQuantity.ToString("0.##"),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 11
            };

            TextBlock tvaBlock = new TextBlock
            {
                Text = tvaRate.ToString("0.##") + "%",
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 11
            };

            int totalColumnIndex = showExpeditionTotal ? 5 : 4;

            TextBlock totalBlock = new TextBlock
            {
                Text = (actualQuantity * price).ToString("0.00"),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 11
            };

            Grid.SetColumn(nameBlock, 0);
            Grid.SetColumn(priceBlock, 1);
            Grid.SetColumn(qtyBlock, 2);
            Grid.SetColumn(tvaBlock, 3);
            Grid.SetColumn(totalBlock, totalColumnIndex);

            articleRow.Children.Add(nameBlock);
            articleRow.Children.Add(priceBlock);
            articleRow.Children.Add(qtyBlock);
            articleRow.Children.Add(tvaBlock);

            if (showExpeditionTotal)
            {
                TextBlock expeditionBlock = new TextBlock
                {
                    Text = actualQuantity.ToString("0.##"),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 11
                };
                Grid.SetColumn(expeditionBlock, 4);
                articleRow.Children.Add(expeditionBlock);
            }

            articleRow.Children.Add(totalBlock);

            return articleRow;
        }

        private Grid CreateLogoPlaceholder()
        {
            Grid logoContainer = new Grid
            {
                Width = 100,
                Height = 100,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };

            Canvas.SetRight(logoContainer, 40);
            Canvas.SetTop(logoContainer, 40);

            Border border = new Border
            {
                Name = "logoBorder",
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1)
            };

            TextBlock defaultText = new TextBlock
            {
                Text = "Logo",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = defaultText;
            logoContainer.Children.Add(border);

            Image logoImage = new Image
            {
                Name = "imgLogo",
                Width = 100,
                Height = 100,
                Stretch = Stretch.Uniform
            };

            string logoPath = GetDictionaryValue("Logo");
            if (!string.IsNullOrEmpty(logoPath))
            {
                try
                {
                    logoImage.Source = new BitmapImage(new Uri(logoPath, UriKind.RelativeOrAbsolute));
                    border.Visibility = Visibility.Collapsed;
                }
                catch
                {
                }
            }

            logoContainer.Children.Add(logoImage);
            return logoContainer;
        }

        private string GetDictionaryValue(string key, string defaultValue = "")
        {
            return FactureInfo.ContainsKey(key) ? FactureInfo[key] : defaultValue;
        }

        private void PopulateHeaderData(Canvas canvas, bool isCheckType)
        {
            SetTextBlockValue(canvas, "txtDisplayNom", GetDictionaryValue("NomC"));
            SetTextBlockValue(canvas, "txtDisplayICE", GetDictionaryValue("ICEC"));
            SetTextBlockValue(canvas, "txtDisplayVAT", GetDictionaryValue("VATC"));
            SetTextBlockValue(canvas, "txtDisplayTelephone", GetDictionaryValue("TelephoneC"));
            SetTextBlockValue(canvas, "txtDisplayEtatJuridique", GetDictionaryValue("EtatJuridiqueC"));
            SetTextBlockValue(canvas, "txtDisplayIdSociete", GetDictionaryValue("IdSocieteC"));
            SetTextBlockValue(canvas, "txtDisplaySiegeSociete", GetDictionaryValue("SiegeEntrepriseC"));
            SetTextBlockValue(canvas, "txtDisplayAdresse", GetDictionaryValue("AdressC"));

            SetTextBlockValue(canvas, "txtDisplayFacture", GetDictionaryValue("NFacture"));
            SetTextBlockValue(canvas, "txtDisplayDate", GetDictionaryValue("Date"));

            SetTextBlockValue(canvas, "txtDisplayPaymentMethod", GetDictionaryValue("PaymentMethod"));
            SetTextBlockValue(canvas, "txtDisplayGivenBy", GetDictionaryValue("GivenBy"));
            SetTextBlockValue(canvas, "txtDisplayReceivedBy", GetDictionaryValue("ReceivedBy"));
            SetTextBlockValue(canvas, "txtDisplayEtatFacture", GetDictionaryValue("Reversed"));
            SetTextBlockValue(canvas, "txtDisplayDevice", GetDictionaryValue("Device"));
            SetTextBlockValue(canvas, "txtDisplayType", GetDictionaryValue("Type"));
            SetTextBlockValue(canvas, "txtDisplayIndex", GetDictionaryValue("IndexDeFacture"));

            if (isCheckType)
            {
                SetTextBlockValue(canvas, "txtDisplayMontant", GetDictionaryValue("MontantApresRemise"));
            }

            Grid headerGrid = FindLogicalChild<Grid>(canvas, "ArticlesContainer");
            if (headerGrid != null)
            {
                foreach (UIElement child in headerGrid.Children)
                {
                    if (child is StackPanel panel)
                    {
                        HideEmptyLabelsInPanel(panel);
                    }
                }
            }
        }

        private StackPanel CreateObjectPanel(bool isCheckType = false)
        {
            StackPanel panel = new StackPanel
            {
                Background = new SolidColorBrush(Colors.White),
                Width = 544,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            double topPosition = isCheckType ? 355 : 340;
            Canvas.SetLeft(panel, 91);
            Canvas.SetTop(panel, topPosition);

            StackPanel objectPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5, 0, 5, 0)
            };

            TextBlock objectLabel = new TextBlock
            {
                Text = "Objet : ",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.Black),
                VerticalAlignment = VerticalAlignment.Top
            };

            TextBlock objectValue = new TextBlock
            {
                Name = "txtDisplayObject",
                Text = "",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Black),
                Width = 480,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                MaxHeight = 40
            };

            objectPanel.Children.Add(objectLabel);
            objectPanel.Children.Add(objectValue);
            panel.Children.Add(objectPanel);

            return panel;
        }

        private StackPanel CreateMontantEnLettresPanel(bool isCheckType = false)
        {
            StackPanel panel = new StackPanel
            {
                Background = new SolidColorBrush(Colors.White),
                Width = 350,
                Height = isCheckType ? 170 : 200,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            double topPosition = isCheckType ? 500 : 700;
            Canvas.SetLeft(panel, 72);
            Canvas.SetTop(panel, topPosition);

            StackPanel descriptionPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5, 0, 5, 10)
            };

            TextBlock descriptionLabel = new TextBlock
            {
                Text = "Description : ",
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.Black),
                Width = 340,
                VerticalAlignment = VerticalAlignment.Top
            };

            TextBlock descriptionValue = new TextBlock
            {
                Name = "txtDescription",
                Text = "",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Black),
                Width = 340,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                MaxHeight = 80
            };

            descriptionPanel.Children.Add(descriptionLabel);
            descriptionPanel.Children.Add(descriptionValue);
            panel.Children.Add(descriptionPanel);

            StackPanel amountPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5, 0, 5, 0)
            };

            TextBlock amountLabel = new TextBlock
            {
                Text = "Montant en Lettres : ",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.Black),
                Width = 340,
                VerticalAlignment = VerticalAlignment.Top
            };

            TextBlock amountValue = new TextBlock
            {
                Name = "txtDisplayAmountInLetters",
                Text = "",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Black),
                Width = 340,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                MaxHeight = 60
            };

            amountPanel.Children.Add(amountLabel);
            amountPanel.Children.Add(amountValue);
            panel.Children.Add(amountPanel);

            return panel;
        }

        private StackPanel CreateInfoRowWithWrap(string label, string textBlockName, double labelWidth, double valueWidth, bool wrap = true)
        {
            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Top,
                Name = $"row_{textBlockName}"
            };

            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                Width = labelWidth,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0),
                Name = $"lbl_{textBlockName}"
            });

            sp.Children.Add(new TextBlock
            {
                Name = textBlockName,
                Text = "",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                VerticalAlignment = VerticalAlignment.Top,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0),
                Width = valueWidth,
                MaxHeight = 40,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            return sp;
        }

        private void PopulateSummaryData(StackPanel panel)
        {
            if (panel.Children.Count < 6) return;

            // Parse stored values with InvariantCulture
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("MontantTotal", "0")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal storedHT);
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("TVA", "0")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal storedTVARate);
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("MontantTVA", "0")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal storedTVA);
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("MontantApresTVA", "0")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal storedTTC);
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("MontantApresRemise", "0")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal storedFinal);
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("Remise", "0")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal storedRemise);

            // If stored totals are all zero, recompute from the articles we have
            if (storedHT == 0 && storedFinal == 0 && _invoiceArticles != null && _invoiceArticles.Count > 0)
            {
                decimal calcHT = _invoiceArticles.Where(a => a.Quantite > 0).Sum(a => a.Prix * a.Quantite);
                decimal calcTVA = _invoiceArticles.Where(a => a.Quantite > 0).Sum(a => (a.TVA / 100m) * a.Prix * a.Quantite);
                decimal calcTTC = calcHT + calcTVA;
                decimal calcFinal = calcTTC - storedRemise;
                decimal tvaRatePct = calcHT > 0 ? (calcTVA / calcHT) * 100m : 0m;

                storedHT = calcHT;
                storedTVA = calcTVA;
                storedTTC = calcTTC;
                storedFinal = calcFinal;
                storedTVARate = tvaRatePct;
            }

            string cur = GetDictionaryValue("Device", "DH");

            if (panel.Children[0] is StackPanel sp0 && sp0.Children.Count >= 2 && sp0.Children[1] is TextBlock tb0)
                tb0.Text = storedHT.ToString("0.00") + " " + cur;

            if (panel.Children[1] is StackPanel sp1 && sp1.Children.Count >= 2 && sp1.Children[1] is TextBlock tb1)
                tb1.Text = storedTVARate.ToString("0.00") + " %";

            if (panel.Children[2] is StackPanel sp2 && sp2.Children.Count >= 2 && sp2.Children[1] is TextBlock tb2)
                tb2.Text = storedTVA.ToString("0.00") + " " + cur;

            if (panel.Children[3] is StackPanel sp3 && sp3.Children.Count >= 2 && sp3.Children[1] is TextBlock tb3)
                tb3.Text = storedTTC.ToString("0.00") + " " + cur;

            if (panel.Children[4] is StackPanel sp4 && sp4.Children.Count >= 2 && sp4.Children[1] is TextBlock tb4)
                tb4.Text = "- " + storedRemise.ToString("0.00") + " " + cur;

            if (panel.Children[5] is StackPanel sp5 && sp5.Children.Count >= 2 && sp5.Children[1] is TextBlock tb5)
                tb5.Text = storedFinal.ToString("0.00") + " " + cur;
        }

        private void PopulateFooterData(StackPanel panel)
        {
            // Existing fields
            SetTextBlockValue(panel, "txtDisplayNomU", GetDictionaryValue("NomU"));
            SetTextBlockValue(panel, "txtDisplayICEU", GetDictionaryValue("ICEU"));
            SetTextBlockValue(panel, "txtDisplayVATU", GetDictionaryValue("VATU"));
            SetTextBlockValue(panel, "txtDisplayTelephoneU", GetDictionaryValue("TelephoneU"));
            SetTextBlockValue(panel, "txtDisplayEtatJuridiqueU", GetDictionaryValue("EtatJuridiqueU"));
            SetTextBlockValue(panel, "txtDisplayIdSocieteU", GetDictionaryValue("IdSocieteU"));
            SetTextBlockValue(panel, "txtDisplaySeigeU", GetDictionaryValue("SiegeEntrepriseU"));
            SetTextBlockValue(panel, "txtDisplayAdresseU", GetDictionaryValue("AdressU"));

            // New fields
            SetTextBlockValue(panel, "txtDisplayIFU", GetDictionaryValue("IFU"));
            SetTextBlockValue(panel, "txtDisplayCNSSU", GetDictionaryValue("CNSS_U"));
            SetTextBlockValue(panel, "txtDisplayRCU", GetDictionaryValue("RC_U"));
            SetTextBlockValue(panel, "txtDisplayTPU", GetDictionaryValue("TP_U"));
            SetTextBlockValue(panel, "txtDisplayRIBU", GetDictionaryValue("RIB_U"));
            SetTextBlockValue(panel, "txtDisplayEmailU", GetDictionaryValue("EmailU"));
            SetTextBlockValue(panel, "txtDisplaySiteWebU", GetDictionaryValue("SiteWebU"));
            SetTextBlockValue(panel, "txtDisplayPatenteU", GetDictionaryValue("PatenteU"));
            SetTextBlockValue(panel, "txtDisplayCapitalU", GetDictionaryValue("CapitalU"));
            SetTextBlockValue(panel, "txtDisplayFaxU", GetDictionaryValue("FaxU"));
            SetTextBlockValue(panel, "txtDisplayVilleU", GetDictionaryValue("VilleU"));
            SetTextBlockValue(panel, "txtDisplayCodePostalU", GetDictionaryValue("CodePostalU"));
            SetTextBlockValue(panel, "txtDisplayBankNameU", GetDictionaryValue("BankNameU"));
            SetTextBlockValue(panel, "txtDisplayAgencyCodeU", GetDictionaryValue("AgencyCodeU"));

            foreach (UIElement child in panel.Children)
            {
                if (child is StackPanel row)
                    HideEmptyLabelsInPanel(row);
            }
        }

        private void SetTextBlockValue(DependencyObject parent, string name, string value)
        {
            TextBlock textBlock = FindLogicalChild<TextBlock>(parent, name);
            if (textBlock != null)
            {
                textBlock.Text = value;
            }
        }

        // Logical tree walker — works on freshly created elements that are NOT yet in the visual tree.
        private T FindLogicalChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null) return null;

            // Check direct FrameworkElement children via LogicalChildren
            if (parent is FrameworkElement fe)
            {
                foreach (object child in LogicalTreeHelper.GetChildren(fe))
                {
                    if (child is T typedChild && (child as FrameworkElement)?.Name == name)
                        return typedChild;

                    if (child is DependencyObject depChild)
                    {
                        var result = FindLogicalChild<T>(depChild, name);
                        if (result != null) return result;
                    }
                }
            }

            // Fallback: Panel.Children for panels not exposed via LogicalChildren fully
            if (parent is Panel panel)
            {
                foreach (UIElement child in panel.Children)
                {
                    if (child is T typedChild && (child as FrameworkElement)?.Name == name)
                        return typedChild;

                    if (child is DependencyObject depChild)
                    {
                        var result = FindLogicalChild<T>(depChild, name);
                        if (result != null) return result;
                    }
                }
            }

            if (parent is ContentControl cc && cc.Content is DependencyObject ccChild)
            {
                if (ccChild is T typedCcChild && (ccChild as FrameworkElement)?.Name == name)
                    return typedCcChild;
                return FindLogicalChild<T>(ccChild, name);
            }

            if (parent is Border border && border.Child is DependencyObject borderChild)
            {
                if (borderChild is T typedBorderChild && (borderChild as FrameworkElement)?.Name == name)
                    return typedBorderChild;
                return FindLogicalChild<T>(borderChild, name);
            }

            return null;
        }

        // Visual tree walker — only use for elements already rendered/attached to the visual tree.
        private T FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && (child as FrameworkElement)?.Name == name)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private Grid CreateHeaderGrid(bool isCheckType = false)
        {
            Grid headerGrid = new Grid
            {
                Background = new SolidColorBrush(Colors.Transparent),
                Width = 544,
                Height = isCheckType ? 230 : 230,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Name = "ArticlesContainer"
            };

            Canvas.SetLeft(headerGrid, 91);
            Canvas.SetTop(headerGrid, 190);

            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.88, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel leftPanel = new StackPanel { Margin = new Thickness(0) };
            Grid.SetColumn(leftPanel, 0);

            leftPanel.Children.Add(CreateInfoRow("Nom : ", "txtDisplayNom", 44));
            leftPanel.Children.Add(CreateInfoRow("ICE : ", "txtDisplayICE", 33));
            leftPanel.Children.Add(CreateInfoRow("VAT : ", "txtDisplayVAT", 41));
            leftPanel.Children.Add(CreateInfoRow("Téléphone : ", "txtDisplayTelephone", 85));
            leftPanel.Children.Add(CreateInfoRow("État Juridique : ", "txtDisplayEtatJuridique", 107));
            leftPanel.Children.Add(CreateInfoRow("ID de Société : ", "txtDisplayIdSociete", 99));
            leftPanel.Children.Add(CreateInfoRow("Siège de Société : ", "txtDisplaySiegeSociete", 122));
            leftPanel.Children.Add(CreateInfoRowWithWrap("Adresse : ", "txtDisplayAdresse", 65, 290));

            headerGrid.Children.Add(leftPanel);

            StackPanel rightPanel = new StackPanel { Margin = new Thickness(0) };
            Grid.SetColumn(rightPanel, 1);

            string invoiceNumberLabel = GetInvoiceNumberLabel();
            rightPanel.Children.Add(CreateInfoRow(invoiceNumberLabel, "txtDisplayFacture", 70, true));
            rightPanel.Children.Add(CreateInfoRow("Date : ", "txtDisplayDate", 50, true));
            rightPanel.Children.Add(CreateInfoRow("Mode de Paiement : ", "txtDisplayPaymentMethod", 135, true));
            rightPanel.Children.Add(CreateInfoRow("Donné par : ", "txtDisplayGivenBy", 85, true));
            rightPanel.Children.Add(CreateInfoRow("Reçu par : ", "txtDisplayReceivedBy", 75, true));
            rightPanel.Children.Add(CreateInfoRow("État Facture : ", "txtDisplayEtatFacture", 90, true));
            rightPanel.Children.Add(CreateInfoRow("Device : ", "txtDisplayDevice", 60, true));
            rightPanel.Children.Add(CreateInfoRow("Index : ", "txtDisplayIndex", 55, true));

            if (isCheckType)
            {
                rightPanel.Children.Add(CreateInfoRow("Montant : ", "txtDisplayMontant", 65, true));
            }

            headerGrid.Children.Add(rightPanel);

            return headerGrid;
        }

        private StackPanel CreateInfoRow(string label, string textBlockName, double labelWidth, bool wrap = false)
        {
            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0),
                Name = $"row_{textBlockName}"
            };

            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                Width = labelWidth,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0),
                Name = $"lbl_{textBlockName}"
            });

            TextBlock valueBlock = new TextBlock
            {
                Name = textBlockName,
                Text = "",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            if (wrap)
            {
                valueBlock.TextWrapping = TextWrapping.Wrap;
                valueBlock.MaxWidth = 150;
            }

            sp.Children.Add(valueBlock);

            return sp;
        }

        private StackPanel CreateCreditInfoPanel()
        {
            StackPanel creditPanel = new StackPanel
            {
                Background = new SolidColorBrush(Colors.White),
                Width = 544,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Orientation = Orientation.Horizontal
            };

            TextBlock clientNameBlock = new TextBlock
            {
                Name = "txtCreditClientName",
                Text = "",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(10, 0, 10, 0),
                Width = 350
            };

            TextBlock montantBlock = new TextBlock
            {
                Name = "txtCreditMontant",
                Text = "",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(10, 0, 10, 0),
                Width = 150
            };

            creditPanel.Children.Add(clientNameBlock);
            creditPanel.Children.Add(montantBlock);

            return creditPanel;
        }

        private StackPanel CreateFooterInfoSection(string label, string textBlockName, double labelWidth)
        {
            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0),
                Name = $"row_{textBlockName}"
            };

            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                Width = labelWidth,
                VerticalAlignment = VerticalAlignment.Center,
                Name = $"lbl_{textBlockName}"
            });

            sp.Children.Add(new TextBlock
            {
                Name = textBlockName,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 150
            });

            return sp;
        }

        private void HideEmptyLabelsInPanel(Panel panel)
        {
            if (!_hideEmptyLabels) return;

            foreach (UIElement child in panel.Children)
            {
                if (child is StackPanel row)
                {
                    TextBlock valueBlock = null;
                    foreach (UIElement rowChild in row.Children)
                    {
                        if (rowChild is TextBlock tb && !tb.Name.StartsWith("lbl_"))
                        {
                            valueBlock = tb;
                            break;
                        }
                    }

                    if (valueBlock != null && string.IsNullOrWhiteSpace(valueBlock.Text))
                    {
                        row.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private string GetTemplateForPage(int index, int totalPages, string[] templates)
        {
            if (totalPages == 1) return templates[0];
            if (index == 0) return templates[1];
            if (index == totalPages - 1) return templates[3];
            return templates[2];
        }

        private (double top, double height) GetStackPanelLayoutForTemplate(string template)
        {
            switch (template)
            {
                case "1.png":
                case "1E.png":
                case "10.png":
                    return (420, 250);
                case "2.png":
                case "2E.png":
                    return (490, 265);
                case "3.png":
                case "3E.png":
                    return (210, 570);
                case "4.png":
                case "4E.png":
                case "13.png":
                    return (75, 570);
                default:
                    return (100, 700);
            }
        }

        private StackPanel CreateSummaryPanel(bool isCheckType = false)
        {
            string invoiceType = GetDictionaryValue("Type", "").ToLower();
            StackPanel summaryPanel = new StackPanel
            {
                Background = new SolidColorBrush(Colors.White),
                Width = 180,
                Height = 140,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = (invoiceType == "credit") ? Visibility.Collapsed : Visibility.Visible,
            };

            double topPosition = isCheckType ? 650 : 700;
            Canvas.SetLeft(summaryPanel, 470);
            Canvas.SetTop(summaryPanel, topPosition);

            summaryPanel.Children.Add(CreateSummaryRow("Prix HT :", "0.00 DH", false));
            summaryPanel.Children.Add(CreateSummaryRow("TVA :", "0.00 %", false));
            summaryPanel.Children.Add(CreateSummaryRow("Valeur TVA :", "0.00 DH", false));
            summaryPanel.Children.Add(CreateSummaryRow("Prix TTC :", "0.00 DH", false));
            summaryPanel.Children.Add(CreateSummaryRow("Remise :", "- 0.00 DH", false));
            summaryPanel.Children.Add(CreateSummaryRow("Total :", "0.00 DH", true));

            return summaryPanel;
        }

        private StackPanel CreateSummaryRow(string label, string value, bool isBold)
        {
            StackPanel row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };

            TextBlock labelBlock = new TextBlock
            {
                Text = label,
                FontSize = 13,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                Width = 90,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock valueBlock = new TextBlock
            {
                Text = value,
                FontSize = 13,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                Width = 90,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right
            };

            row.Children.Add(labelBlock);
            row.Children.Add(valueBlock);

            return row;
        }

        private StackPanel CreateFooterPanel()
        {
            StackPanel footerPanel = new StackPanel
            {
                Background = new SolidColorBrush(Colors.White),
                Width = 642,
                Height = 85,  // tall enough for 4 rows at font size 10
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Canvas.SetLeft(footerPanel, 49);

            // Row 1: Nom | ICE | VAT | IF | Tel | Fax | Email
            StackPanel firstRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            firstRow.Children.Add(CreateFooterInfoSection("Nom : ", "txtDisplayNomU", 30));
            firstRow.Children.Add(CreateFooterInfoSection("| ICE : ", "txtDisplayICEU", 25));
            firstRow.Children.Add(CreateFooterInfoSection("| VAT : ", "txtDisplayVATU", 23));
            firstRow.Children.Add(CreateFooterInfoSection("| IF : ", "txtDisplayIFU", 15));
            firstRow.Children.Add(CreateFooterInfoSection("| Tel : ", "txtDisplayTelephoneU", 22));
            firstRow.Children.Add(CreateFooterInfoSection("| Fax : ", "txtDisplayFaxU", 20));
            firstRow.Children.Add(CreateFooterInfoSection("| Email : ", "txtDisplayEmailU", 30));
            footerPanel.Children.Add(firstRow);

            // Row 2: RC | CNSS | TP | RIB | Banque | Agence | Capital
            StackPanel secondRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            secondRow.Children.Add(CreateFooterInfoSection("RC : ", "txtDisplayRCU", 20));
            secondRow.Children.Add(CreateFooterInfoSection("| CNSS : ", "txtDisplayCNSSU", 30));
            secondRow.Children.Add(CreateFooterInfoSection("| TP : ", "txtDisplayTPU", 18));
            secondRow.Children.Add(CreateFooterInfoSection("| RIB : ", "txtDisplayRIBU", 20));
            secondRow.Children.Add(CreateFooterInfoSection("| Banque : ", "txtDisplayBankNameU", 38));
            secondRow.Children.Add(CreateFooterInfoSection("| Agence : ", "txtDisplayAgencyCodeU", 38));
            secondRow.Children.Add(CreateFooterInfoSection("| Capital : ", "txtDisplayCapitalU", 38));
            footerPanel.Children.Add(secondRow);

            // Row 3: Etat Juridique | Id Societe | Siege | Patente | Site Web
            StackPanel thirdRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            thirdRow.Children.Add(CreateFooterInfoSection("Etat Juridique : ", "txtDisplayEtatJuridiqueU", 69));
            thirdRow.Children.Add(CreateFooterInfoSection("| Id Societe : ", "txtDisplayIdSocieteU", 57));
            thirdRow.Children.Add(CreateFooterInfoSection("| Siege : ", "txtDisplaySeigeU", 40));
            thirdRow.Children.Add(CreateFooterInfoSection("| Patente : ", "txtDisplayPatenteU", 40));
            thirdRow.Children.Add(CreateFooterInfoSection("| Site : ", "txtDisplaySiteWebU", 25));
            footerPanel.Children.Add(thirdRow);

            // Row 4: Adresse | Ville | Code Postal
            // Use CreateFooterInfoSection for txtDisplayAdresseU so SetTextBlockValue can find it
            StackPanel fourthRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            // Address section — custom width but same helper so the name is registered
            StackPanel adresseSp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0),
                Name = "row_txtDisplayAdresseU"
            };
            adresseSp.Children.Add(new TextBlock
            {
                Text = "Adresse : ",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                Width = 42,
                VerticalAlignment = VerticalAlignment.Top,
                Name = "lbl_txtDisplayAdresseU"
            });
            adresseSp.Children.Add(new TextBlock
            {
                Name = "txtDisplayAdresseU",
                Text = "",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                VerticalAlignment = VerticalAlignment.Top,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0),
                MaxWidth = 200,
                MaxHeight = 20,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            fourthRow.Children.Add(adresseSp);
            fourthRow.Children.Add(CreateFooterInfoSection("| Ville : ", "txtDisplayVilleU", 25));
            fourthRow.Children.Add(CreateFooterInfoSection("| CP : ", "txtDisplayCodePostalU", 18));
            footerPanel.Children.Add(fourthRow);

            return footerPanel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (PageCounte == TotalPageCount)
                return;

            PageCounte++;
            PageNbr.Text = PageCounte.ToString();

            foreach (Canvas canvas in _canvasCache.Values)
            {
                canvas.Visibility = (canvas.Name == $"Canvas{PageCounte}") ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (PageCounte == 1)
                return;

            PageCounte--;
            PageNbr.Text = PageCounte.ToString();

            foreach (Canvas canvas in _canvasCache.Values)
            {
                canvas.Visibility = (canvas.Name == $"Canvas{PageCounte}") ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintInvoice();
        }

        private List<Invoice.InvoiceArticle> ConvertToInvoiceArticles(List<InvoiceArticle> articles)
        {
            var invoiceArticles = new List<Invoice.InvoiceArticle>();

            foreach (var article in articles)
            {
                invoiceArticles.Add(new Invoice.InvoiceArticle
                {
                    OperationID = article.OperationID,
                    ArticleID = article.ArticleID,
                    ArticleName = article.ArticleName,
                    PrixUnitaire = article.Prix,
                    Quantite = article.Quantite,
                    TVA = article.TVA,
                    IsReversed = article.Reversed
                });
            }

            return invoiceArticles;
        }

        private Invoice CreateInvoiceFromFactureInfo()
        {
            // Use InvariantCulture so "46.00" parses correctly regardless of system locale
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("TVA")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal tvaRate);
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("MontantTotal")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal totalHT);
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("MontantTVA")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal totalTVA);
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("MontantApresTVA")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal totalTTC);
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("Remise")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal remise);
            decimal.TryParse(CleanNumericValue(GetDictionaryValue("MontantApresRemise")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal totalAfterRemise);

            int.TryParse(GetDictionaryValue("EtatFature"), out int etatFacture);
            bool isReversed = GetDictionaryValue("Reversed", "").ToLower() == "reversed";

            decimal.TryParse(CleanNumericValue(GetDictionaryValue("CreditMontant")), out decimal creditMontant);
            string creditClientName = GetDictionaryValue("CreditClientName");


            var invoice = new Invoice
            {
                InvoiceNumber = GetDictionaryValue("NFacture"),
                InvoiceDate = DateTime.TryParse(GetDictionaryValue("Date"), out DateTime date) ? date.Date : DateTime.Now.Date,
                InvoiceType = GetDictionaryValue("Type"),
                InvoiceIndex = GetDictionaryValue("IndexDeFacture"),

                Objet = GetDictionaryValue("Object"),
                NumberLetters = GetDictionaryValue("AmountInLetters"),
                NameFactureGiven = GetDictionaryValue("GivenBy"),
                NameFactureReceiver = GetDictionaryValue("ReceivedBy"),
                ReferenceClient = GetDictionaryValue("ClientReference"),
                PaymentMethod = GetDictionaryValue("PaymentMethod"),

                UserName = GetDictionaryValue("NomU"),
                UserICE = GetDictionaryValue("ICEU"),
                UserVAT = GetDictionaryValue("VATU"),
                UserPhone = GetDictionaryValue("TelephoneU"),
                UserAddress = GetDictionaryValue("AdressU"),
                UserEtatJuridique = GetDictionaryValue("EtatJuridiqueU"),
                UserIdSociete = GetDictionaryValue("IdSocieteU"),
                UserSiegeEntreprise = GetDictionaryValue("SiegeEntrepriseU"),

                ClientName = GetDictionaryValue("NomC"),
                ClientICE = GetDictionaryValue("ICEC"),
                ClientVAT = GetDictionaryValue("VATC"),
                ClientPhone = GetDictionaryValue("TelephoneC"),
                ClientAddress = GetDictionaryValue("AdressC"),
                ClientEtatJuridique = GetDictionaryValue("EtatJuridiqueC"),
                ClientIdSociete = GetDictionaryValue("IdSocieteC"),
                ClientSiegeEntreprise = GetDictionaryValue("SiegeEntrepriseC"),


                Currency = GetDictionaryValue("Device", "DH"),
                TVARate = tvaRate,
                TotalHT = totalHT,
                TotalTVA = totalTVA,
                TotalTTC = totalTTC,
                Remise = remise,
                TotalAfterRemise = totalAfterRemise,

                CreditClientName = creditClientName,
                CreditMontant = creditMontant,

                EtatFacture = etatFacture,
                IsReversed = isReversed,

                Description = GetDictionaryValue("Description"),
                LogoPath = GetDictionaryValue("Logo"),

                CreatedBy = main?.u?.UserID,
                CreatedDate = DateTime.Now
            };

            if (main?.InvoiceArticles != null)
            {
                invoice.Articles = ConvertToInvoiceArticles(main.InvoiceArticles);
            }

            return invoice;
        }

        private string CleanNumericValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "0";

            return new string(value.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
                .Replace(',', '.');
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Save.IsEnabled = false;

                Invoice invoice = CreateInvoiceFromFactureInfo();

                bool exists = await _invoiceRepository.InvoiceNumberExistsAsync(invoice.InvoiceNumber);
                if (exists)
                {
                    MessageBox.Show(
                        $"Ce nombre de facture : {invoice.InvoiceNumber}, deja exist.",
                        "Attention",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                int invoiceId = await _invoiceRepository.CreateInvoiceAsync(invoice);

                if (invoiceId > 0)
                {
                    // ✅ FIX: Save each article to the API (InvoiceArticle table)
                    await SaveInvoiceArticlesAsync(invoiceId);

                    await ReduceStockForInvoiceArticles();

                    MessageBox.Show(
                        $"Facture sauvegardée avec succès!\nNuméro: {invoice.InvoiceNumber}",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    this.Close();
                }
                else
                {
                    MessageBox.Show(
                        "Erreur lors de la sauvegarde de la facture.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la sauvegarde:\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Save.IsEnabled = true;
            }
        }

        private async void btnSaveAndPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveAndPrint.IsEnabled = false;

                Invoice invoice = CreateInvoiceFromFactureInfo();

                bool exists = await _invoiceRepository.InvoiceNumberExistsAsync(invoice.InvoiceNumber);
                if (exists)
                {
                    MessageBox.Show(
                        $"رقم الفاتورة {invoice.InvoiceNumber} موجود مسبقاً!\nالرجاء استخدام رقم آخر.",
                        "تنبيه",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                int invoiceId = await _invoiceRepository.CreateInvoiceAsync(invoice);

                if (invoiceId > 0)
                {
                    // ✅ FIX: Save each article to the API (InvoiceArticle table)
                    await SaveInvoiceArticlesAsync(invoiceId);

                    await ReduceStockForInvoiceArticles();

                    MessageBox.Show(
                        $"Facture sauvegardée avec succès!\nNuméro: {invoice.InvoiceNumber}",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    PrintInvoice();
                }
                else
                {
                    MessageBox.Show(
                        "Erreur lors de la sauvegarde de la facture.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la sauvegarde:\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SaveAndPrint.IsEnabled = true;
            }
        }

        // Saves all invoice articles to the API after invoice header is created
        private async System.Threading.Tasks.Task SaveInvoiceArticlesAsync(int invoiceId)
        {
            // Use stored flat list (set in constructor from the invoiceArticles parameter,
            // or from main.InvoiceArticles as fallback)
            List<InvoiceArticle> articlesToSave = _invoiceArticles;

            // Last-resort fallback: try main.InvoiceArticles if _invoiceArticles is empty
            if ((articlesToSave == null || articlesToSave.Count == 0) && main?.InvoiceArticles?.Count > 0)
                articlesToSave = main.InvoiceArticles;

            if (articlesToSave == null || articlesToSave.Count == 0)
            {
                MessageBox.Show(
                    "Aucun article à sauvegarder (liste vide).",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int saved = 0;
            int failed = 0;
            var failedNames = new System.Collections.Generic.List<string>();

            foreach (var ia in articlesToSave)
            {
                // Skip articles with zero quantity
                if (ia.Quantite <= 0) continue;

                var apiArticle = new Invoice.InvoiceArticle
                {
                    InvoiceID = invoiceId,
                    OperationID = ia.OperationID > 0 ? (int?)ia.OperationID : null,
                    // Negative ArticleID = custom article → send 0 (no FK on InvoiceArticle)
                    ArticleID = ia.ArticleID > 0 ? ia.ArticleID : 0,
                    ArticleName = ia.ArticleName ?? string.Empty,
                    PrixUnitaire = ia.Prix,
                    Quantite = ia.Quantite,
                    TVA = ia.TVA,
                    IsReversed = ia.Reversed
                };

                bool ok = await _invoiceRepository.AddInvoiceArticleAsync(apiArticle);
                if (ok)
                    saved++;
                else
                {
                    failed++;
                    failedNames.Add(ia.ArticleName ?? $"ArticleID={ia.ArticleID}");
                }
            }

            if (failed > 0)
            {
                string names = string.Join(", ", failedNames);
                MessageBox.Show(
                    $"{failed} article(s) n'ont pas pu être sauvegardés: {names}\n\n" +
                    $"Sauvegardés avec succès: {saved}",
                    "Erreur articles",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async System.Threading.Tasks.Task ReduceStockForInvoiceArticles()
        {
            if (main?.InvoiceArticles == null || main.InvoiceArticles.Count == 0)
                return;

            List<string> errors = new List<string>();
            List<string> successes = new List<string>();

            string invoiceType = GetDictionaryValue("Type", "").ToLower();
            bool isExpeditionInvoice = (invoiceType == "expedition");

            foreach (var invoiceArticle in main.InvoiceArticles)
            {
                if (!invoiceArticle.ReduceStock)
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping {invoiceArticle.ArticleName} - ReduceStock = false");
                    continue;
                }

                if (invoiceArticle.ArticleID < 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping {invoiceArticle.ArticleName} - Custom article");
                    continue;
                }

                try
                {
                    var article = main.main?.la?.FirstOrDefault(a => a.ArticleID == invoiceArticle.ArticleID);

                    if (article == null)
                    {
                        errors.Add($"Article '{invoiceArticle.ArticleName}' non trouvé dans la base de données");
                        continue;
                    }

                    decimal currentStock = GetArticleQuantity(article);

                    decimal quantityToReduce;
                    if (isExpeditionInvoice)
                    {
                        quantityToReduce = invoiceArticle.InitialQuantity;
                        System.Diagnostics.Debug.WriteLine($"=== Reducing Stock (EXPEDITION) ===");
                        System.Diagnostics.Debug.WriteLine($"  Article: {invoiceArticle.ArticleName}");
                        System.Diagnostics.Debug.WriteLine($"  Current Stock: {currentStock}");
                        System.Diagnostics.Debug.WriteLine($"  Quantity Ordered: {invoiceArticle.Quantite}");
                        System.Diagnostics.Debug.WriteLine($"  Quantity Expedited (to reduce): {quantityToReduce}");
                    }
                    else
                    {
                        quantityToReduce = invoiceArticle.Quantite;
                        System.Diagnostics.Debug.WriteLine($"=== Reducing Stock (REGULAR) ===");
                        System.Diagnostics.Debug.WriteLine($"  Article: {invoiceArticle.ArticleName}");
                        System.Diagnostics.Debug.WriteLine($"  Quantity to Reduce: {quantityToReduce}");
                    }

                    decimal newStock = currentStock - quantityToReduce;

                    if (newStock < 0)
                    {
                        errors.Add($"Stock insuffisant pour '{invoiceArticle.ArticleName}'. Disponible: {currentStock}, Requis: {quantityToReduce}");
                        continue;
                    }

                    SetArticleQuantity(article, newStock);

                    await article.UpdateArticleAsync();

                    successes.Add($"'{invoiceArticle.ArticleName}': {currentStock} → {newStock}");

                    System.Diagnostics.Debug.WriteLine($"  New Stock: {newStock}");
                    System.Diagnostics.Debug.WriteLine($"  Status: SUCCESS");
                }
                catch (Exception ex)
                {
                    errors.Add($"Erreur lors de la mise à jour de '{invoiceArticle.ArticleName}': {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"  Status: ERROR - {ex.Message}");
                }
            }

            if (successes.Count > 0 || errors.Count > 0)
            {
                string message = "";

                if (successes.Count > 0)
                {
                    message += "✓ Stock mis à jour:\n" + string.Join("\n", successes);
                }

                if (errors.Count > 0)
                {
                    if (message.Length > 0) message += "\n\n";
                    message += "✗ Erreurs:\n" + string.Join("\n", errors);
                }

                MessageBox.Show(
                    message,
                    errors.Count > 0 ? "Mise à jour partielle du stock" : "Stock mis à jour",
                    MessageBoxButton.OK,
                    errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
        }

        private decimal GetArticleQuantity(Article article)
        {
            var properties = article.GetType().GetProperties();

            foreach (var prop in properties)
            {
                string propName = prop.Name.ToLower();
                if (propName == "qte" || propName == "quantity" || propName == "quantite" ||
                    propName == "stock" || propName == "qtearticle")
                {
                    var value = prop.GetValue(article);
                    if (value != null)
                    {
                        try
                        {
                            if (value is int intValue)
                            {
                                return (decimal)intValue;
                            }
                            else if (value is decimal decValue)
                            {
                                return decValue;
                            }
                            else if (decimal.TryParse(value.ToString(), out decimal qty))
                            {
                                return qty;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }

            return 0;
        }

        private void SetArticleQuantity(Article article, decimal newQuantity)
        {
            var properties = article.GetType().GetProperties();

            foreach (var prop in properties)
            {
                string propName = prop.Name.ToLower();
                if (propName == "qte" || propName == "quantity" || propName == "quantite" ||
                    propName == "stock" || propName == "qtearticle")
                {
                    if (prop.CanWrite)
                    {
                        try
                        {
                            if (prop.PropertyType == typeof(int))
                            {
                                prop.SetValue(article, (int)newQuantity);
                            }
                            else if (prop.PropertyType == typeof(decimal))
                            {
                                prop.SetValue(article, newQuantity);
                            }
                            else if (prop.PropertyType == typeof(double))
                            {
                                prop.SetValue(article, (double)newQuantity);
                            }
                            else if (prop.PropertyType == typeof(float))
                            {
                                prop.SetValue(article, (float)newQuantity);
                            }
                            return;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
        }

        private void PrintInvoice()
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    FixedDocument fixedDoc = new FixedDocument();
                    int originalPage = PageCounte;

                    List<Canvas> allCanvases = new List<Canvas>();
                    foreach (Canvas canvas in _canvasCache.Values)
                    {
                        allCanvases.Add(canvas);
                        canvas.Visibility = Visibility.Visible;
                    }

                    FacturesContainer.UpdateLayout();
                    this.UpdateLayout();
                    Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                    foreach (Canvas canvas in allCanvases)
                    {
                        FixedPage fixedPage = new FixedPage
                        {
                            Width = 720,
                            Height = 1000,
                            Background = Brushes.White
                        };

                        canvas.Measure(new Size(720, 1000));
                        canvas.Arrange(new Rect(0, 0, 720, 1000));
                        canvas.UpdateLayout();

                        RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                            720, 1000, 96d, 96d, PixelFormats.Pbgra32);
                        renderBitmap.Render(canvas);

                        Image img = new Image
                        {
                            Source = renderBitmap,
                            Width = 720,
                            Height = 1000,
                            Stretch = Stretch.Fill
                        };
                        fixedPage.Children.Add(img);

                        PageContent pageContent = new PageContent();
                        ((IAddChild)pageContent).AddChild(fixedPage);
                        fixedDoc.Pages.Add(pageContent);
                    }

                    foreach (Canvas canvas in allCanvases)
                    {
                        canvas.Visibility = (canvas.Name == $"Canvas{originalPage}") ? Visibility.Visible : Visibility.Collapsed;
                    }

                    printDialog.PrintDocument(fixedDoc.DocumentPaginator, $"Facture - {GetDictionaryValue("NFacture")}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'impression: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.Close();
            }
        }
    }
}