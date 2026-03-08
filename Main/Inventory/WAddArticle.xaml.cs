using Superete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;
using System.Globalization;

namespace GestionComerce.Main.Inventory
{
    public partial class WAddArticle : Window
    {
        public Article a;
        public List<Article> la;
        List<Famille> lf;
        List<Fournisseur> lfo;
        int s;
        public CMainI main;
        public WExistingArticles ea;
        WNouveauStock ns;
        private byte[] selectedImageBytes = null;

        public WAddArticle(Article a, List<Article> la, List<Famille> lf, List<Fournisseur> lfo, CMainI main, int s, WExistingArticles ea, WNouveauStock ns)
        {
            InitializeComponent();
            if (a == null)
            {
                a = new Article(); // Create new article if null
            }
            this.a = a;

            // CRITICAL: Always use main.la to ensure we're working with the same list
            this.la = main.la;

            this.lf = lf;
            this.lfo = lfo;
            this.s = s;
            this.main = main;
            this.ea = ea;
            this.ns = ns;

            LoadFamillies(lf, 0);
            LoadFournisseurs(lfo, 0);
            LoadPayments(main.main.lp);
            SelectDefaultPaymentMethod();
            // Set default dates
            DateArticle.SelectedDate = DateTime.Now;
            DateLivraison.SelectedDate = DateTime.Now;

            foreach (Role r in main.main.lr)
            {
                if (main.u.RoleID == r.RoleID)
                {
                    if (r.AddFamilly == false)
                    {
                        AjouterFamille.IsEnabled = false;
                    }
                    if (r.CreateFournisseur == false)
                    {
                        AjouterFournisseur.IsEnabled = false;
                    }
                    if (r.SolderFournisseur == false)
                    {
                        CreditButton.IsEnabled = false;
                        HalfButton.IsEnabled = false;
                    }
                    if (r.CashFournisseur == false)
                    {
                        CashButton.IsEnabled = false;
                    }
                    break;
                }
            }

            FournisseurList.SelectedIndex = 0;
            FamilliesList.SelectedIndex = 0;
            List<Fournisseur> lfoo = new List<Fournisseur>();

            if (s == 0) // Edit mode
            {
                ButtonsContainer.Visibility = Visibility.Collapsed;
                EnregistrerButton.Visibility = Visibility.Visible;
                HeaderText.Text = "Modifier Article Existante";

                Code.Text = a.Code.ToString();
                ArticleName.Text = a.ArticleName;
                PrixV.Text = a.PrixVente.ToString("0.00");
                PrixA.Text = a.PrixAchat.ToString("0.00");
                PrixMP.Text = a.PrixMP.ToString("0.00");
                Quantite.Text = a.Quantite.ToString();

                // Load existing fields
                Marque.Text = a.marque ?? string.Empty;
                TVA.Text = a.tva.ToString("0.00");
                NumeroLot.Text = a.numeroLot ?? string.Empty;
                BonLivraison.Text = a.bonlivraison ?? string.Empty;
                IsUnlimitedStock.IsChecked = a.IsUnlimitedStock;

                // Handle nullable dates
                if (a.Date.HasValue)
                    DateArticle.SelectedDate = a.Date.Value;
                if (a.DateLivraison.HasValue)
                    DateLivraison.SelectedDate = a.DateLivraison.Value;
                if (a.DateExpiration.HasValue)
                    DateExpiration.SelectedDate = a.DateExpiration.Value;
                if (a.LastRestockDate.HasValue)
                    LastRestockDate.SelectedDate = a.LastRestockDate.Value;

                // Load NEW fields
                PiecesPerPackage.Text = a.PiecesPerPackage.ToString();
                SelectComboBoxItem(PackageType, a.PackageType);
                PackageWeight.Text = a.PackageWeight.ToString("0.00");
                PackageDimensions.Text = a.PackageDimensions ?? string.Empty;

                MinimumStock.Text = a.MinimumStock.ToString();
                MaximumStock.Text = a.MaximumStock.ToString();
                StorageLocation.Text = a.StorageLocation ?? string.Empty;
                SKU.Text = a.SKU ?? string.Empty;
                Description.Text = a.Description ?? string.Empty;
                IsPerishable.IsChecked = a.IsPerishable;
                SelectComboBoxItem(UnitOfMeasure, a.UnitOfMeasure);
                PrixGros.Text = a.PrixGros.ToString("0.00");
                MinQuantityForGros.Text = a.MinQuantityForGros.ToString();
                CountryOfOrigin.Text = a.CountryOfOrigin ?? string.Empty;
                Manufacturer.Text = a.Manufacturer ?? string.Empty;
                Notes.Text = a.Notes ?? string.Empty;

                // Load image if exists (bytes returned by API as Base64)
                if (a.ArticleImage != null && a.ArticleImage.Length > 0)
                {
                    selectedImageBytes = a.ArticleImage;
                    try
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(a.ArticleImage);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        ArticleImagePreview.Source = bitmap;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors du chargement de l'image: {ex.Message}");
                    }
                }
                else if (!string.IsNullOrEmpty(a.ArticleImagePath) && File.Exists(a.ArticleImagePath))
                {
                    // Bytes not returned by API yet — try loading from stored local path (same machine only)
                    try
                    {
                        selectedImageBytes = File.ReadAllBytes(a.ArticleImagePath);
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(selectedImageBytes);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        ArticleImagePreview.Source = bitmap;
                        // Re-save so the bytes get persisted to DB on next update
                    }
                    catch { /* image file unreadable, leave preview blank */ }
                }
                // else: no image yet — preview stays blank, user can select one

                foreach (Article ar in la)
                {
                    if (ar.ArticleName == a.ArticleName)
                    {
                        foreach (Fournisseur fo in lfo)
                        {
                            if (fo.FournisseurID == ar.FournisseurID && ar.FournisseurID != a.FournisseurID)
                            {
                                lfoo.Add(fo);
                                break;
                            }
                        }
                    }
                }
                LoadFournisseurs(lfo.Except(lfoo).ToList(), 0);

                foreach (Famille f in lf)
                    if (f.FamilleID == a.FamillyID)
                    {
                        FamilliesList.SelectedItem = f.FamilleName;
                        break;
                    }
                foreach (Fournisseur fo in lfo)
                {
                    if (fo.FournisseurID == a.FournisseurID)
                    {
                        FournisseurList.SelectedItem = fo.Nom;
                        break;
                    }
                }
            }

            if (s == 5)
            {
                ButtonsContainer.Visibility = Visibility.Collapsed;
                EnregistrerButton.Visibility = Visibility.Collapsed;
                AjouterButton.Visibility = Visibility.Visible;
                foreach (Fournisseur fo in lfo)
                {
                    if (fo.FournisseurID == ns.AMA.fo.FournisseurID)
                    {
                        FournisseurList.SelectedItem = fo.Nom;
                        break;
                    }
                }
                FournisseurList.IsEnabled = false;
                AjouterFournisseur.Visibility = Visibility.Collapsed;
            }
        }
        private void SelectComboBoxItem(ComboBox comboBox, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }
        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Sélectionner une image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Read image as bytes
                    selectedImageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    // Save the path so it can be used as fallback if bytes aren't returned by API
                    a.ArticleImagePath = openFileDialog.FileName;

                    // Display preview
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.DecodePixelWidth = 200; // Optimize for display
                    bitmap.EndInit();

                    ArticleImagePreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du chargement de l'image: {ex.Message}");
                }
            }
        }

        private void AddSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            WAddFournisseur wAddFournisseur = new WAddFournisseur(main.main, null);
            wAddFournisseur.SupplierSaved += (s, args) =>
            {
                // Refresh the supplier list in this window
                LoadFournisseurs(main.main.lfo, 1);
            };
            wAddFournisseur.ShowDialog();
        }

        // ADD THIS METHOD
        private void SelectDefaultPaymentMethod()
        {
            try
            {
                // Get user's default payment method from settings
                var parametres = ParametresGeneraux.ObtenirParametresParUserId(main.u.UserID, "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;");

                if (parametres != null && !string.IsNullOrEmpty(parametres.MethodePaiementParDefaut))
                {
                    // Find and select the matching payment method
                    for (int i = 0; i < PaymentMethodComboBox.Items.Count; i++)
                    {
                        if (PaymentMethodComboBox.Items[i] is ComboBoxItem item)
                        {
                            if (item.Content.ToString() == parametres.MethodePaiementParDefaut)
                            {
                                PaymentMethodComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If it fails, just leave it unselected
            }
        }

        // ADD THIS METHOD
        public void LoadFournisseurs(List<Fournisseur> lfo, int i)
        {
            FournisseurList.Items.Clear();
            foreach (Fournisseur fo in lfo)
            {
                FournisseurList.Items.Add(fo.Nom);
            }
            if (i == 1)
            {
                FournisseurList.SelectedIndex = FournisseurList.Items.Count - 1;
            }
        }

        // ADD THIS METHOD
        public void LoadFamillies(List<Famille> lf, int i)
        {
            FamilliesList.Items.Clear();
            foreach (Famille f in lf)
            {
                FamilliesList.Items.Add(f.FamilleName);
            }
            if (i == 1)
            {
                FamilliesList.SelectedIndex = FamilliesList.Items.Count - 1;
            }
        }

        // ADD THIS METHOD
        public void LoadPayments(List<PaymentMethod> lp)
        {
            PaymentMethodComboBox.Items.Clear();
            foreach (PaymentMethod pm in lp)
            {
                ComboBoxItem item = new ComboBoxItem
                {
                    Content = pm.PaymentMethodName,
                    Tag = pm.PaymentMethodID
                };
                PaymentMethodComboBox.Items.Add(item);
            }
        }

        private async void EnregistrerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool modifier = false;

                // Validate required fields only
                if (string.IsNullOrWhiteSpace(ArticleName.Text) ||
                    string.IsNullOrWhiteSpace(Code.Text) ||
                    string.IsNullOrWhiteSpace(PrixV.Text) ||
                    string.IsNullOrWhiteSpace(PrixA.Text) ||
                    string.IsNullOrWhiteSpace(Quantite.Text) ||
                    FournisseurList.SelectedIndex == -1 ||
                    FamilliesList.SelectedIndex == -1)
                {
                    MessageBox.Show("Veuillez remplir tous les champs obligatoires (*).");
                    return;
                }

                if (ArticleName.Text != a.ArticleName || Code.Text != a.Code.ToString() ||
                    PrixV.Text != a.PrixVente.ToString() || PrixA.Text != a.PrixAchat.ToString() ||
                    PrixMP.Text != a.PrixMP.ToString() || Quantite.Text != a.Quantite.ToString())
                {
                    modifier = true;
                }

                if (ParseDecimal(PrixV.Text) < ParseDecimal(PrixA.Text))
                {
                    MessageBox.Show("Le prix de vente doit être Superieure ou égal au prix d'achat.");
                    return;
                }

                if (ArticleName.Text != a.ArticleName && Code.Text == a.Code.ToString())
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Changer le nom de cet article changera le nom de tous les articles avec le même code, voulez-vous continuer?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        foreach (Article ar in la)
                        {
                            if (ar.Code == a.Code)
                            {
                                ar.ArticleName = ArticleName.Text;
                                await ar.UpdateArticleAsync();
                            }
                        }
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                if (Code.Text != a.Code.ToString() && ArticleName.Text == a.ArticleName)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Changer le Code de cet article changera le Code de tous les articles avec le même nom, voulez-vous continuer?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        foreach (Article ar in la)
                        {
                            if (ar.ArticleName == a.ArticleName)
                            {
                                ar.Code = ParseLong(Code.Text);
                                await ar.UpdateArticleAsync();
                            }
                        }
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                a.Code = ParseLong(Code.Text);
                a.ArticleName = ArticleName.Text;
                a.PrixVente = ParseDecimal(PrixV.Text);
                a.PrixAchat = ParseDecimal(PrixA.Text);
                a.PrixMP = ParseDecimal(PrixMP.Text);

                // ADD THIS LINE: Update the image when editing
                a.ArticleImage = selectedImageBytes ?? a.ArticleImage;

                // Update new fields
                a.marque = string.IsNullOrWhiteSpace(Marque.Text) ? null : Marque.Text;
                a.tva = ParseDecimal(TVA.Text);
                a.numeroLot = string.IsNullOrWhiteSpace(NumeroLot.Text) ? null : NumeroLot.Text;
                a.bonlivraison = string.IsNullOrWhiteSpace(BonLivraison.Text) ? null : BonLivraison.Text;
                a.Date = DateArticle.SelectedDate ?? DateTime.Now;
                a.DateLivraison = DateLivraison.SelectedDate;
                a.DateExpiration = DateExpiration.SelectedDate;

                int newQte = ParseInt(Quantite.Text);
                if (a.Quantite != newQte)
                {
                    // Only create operation if the OLD quantity was greater than 0
                    if (a.Quantite > 0)
                    {
                        Operation Operation = new Operation();
                        Operation.OperationType = "ModificationQu";
                        Operation.PrixOperation = (a.Quantite - newQte) * a.PrixAchat;
                        Operation.UserID = main.u.UserID;

                        int idd = await Operation.InsertOperationAsync();
                        OperationArticle ofa = new OperationArticle();
                        ofa.ArticleID = a.ArticleID;
                        ofa.OperationID = idd;
                        ofa.QteArticle = a.Quantite; // Old quantity
                        await ofa.InsertOperationArticleAsync();
                    }
                    a.Quantite = newQte;
                }

                foreach (Famille f in lf)
                    if (f.FamilleName == FamilliesList.SelectedItem)
                    {
                        if (a.FamillyID != f.FamilleID)
                        {
                            modifier = true;
                            a.FamillyID = f.FamilleID;
                        }
                        break;
                    }

                foreach (Fournisseur fo in lfo)
                {
                    if (fo.Nom == FournisseurList.SelectedItem)
                    {
                        if (a.FournisseurID != fo.FournisseurID)
                        {
                            modifier = true;
                            a.FournisseurID = fo.FournisseurID;
                        }
                        break;
                    }
                }

                await a.UpdateArticleAsync();
                for (int i = 0; i < la.Count; i++)
                {
                    if (la[i].ArticleID == a.ArticleID)
                    {
                        la[i] = a;
                        break;
                    }
                }

                main.LoadArticles(la);
                WCongratulations wCongratulations = new WCongratulations("Modification avec succes", "La modification a ete effectue avec succes", 1);
                wCongratulations.ShowDialog();
                this.Close(); // Close the window after successful update
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}");
                WCongratulations wCongratulations = new WCongratulations("Modification a échoué", "La modification n'a pas ete effectue", 0);
                wCongratulations.ShowDialog();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddFamillyButton_Click(object sender, RoutedEventArgs e)
        {
            WAddFamille wAddFamille = new WAddFamille(lf, this, new Famille(), null, 0);
            wAddFamille.ShowDialog();
        }

        private void IntegerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (e.Text == ".")
            {
                e.Handled = textBox.Text.Contains(".");
            }
            else
            {
                e.Handled = !e.Text.All(char.IsDigit);
            }
        }

        private void IntegerTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(char.IsDigit))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void DecimalTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                int dotCount = text.Count(c => c == '.');
                if (dotCount > 1 || text.Any(c => !char.IsDigit(c) && c != '.'))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private bool ValidateRequiredFields()
        {
            if (string.IsNullOrWhiteSpace(ArticleName.Text) ||
                string.IsNullOrWhiteSpace(Code.Text) ||
                string.IsNullOrWhiteSpace(PrixV.Text) ||
                string.IsNullOrWhiteSpace(PrixA.Text) ||
                string.IsNullOrWhiteSpace(Quantite.Text) ||
                FournisseurList.SelectedIndex == -1 ||
                FamilliesList.SelectedIndex == -1)
            {
                MessageBox.Show("Veuillez remplir tous les champs obligatoires (*).");
                return false;
            }
            return true;
        }

        // ── Safe numeric parsers (handle both . and , decimal separator) ──────────
        private static decimal ParseDecimal(string text, decimal fallback = 0m)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            // Normalize: replace comma with dot for InvariantCulture
            string normalized = text.Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result)
                ? result : fallback;
        }

        private static int ParseInt(string text, int fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            return int.TryParse(text.Trim(), out int result) ? result : fallback;
        }

        private static long ParseLong(string text, long fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            return long.TryParse(text.Trim(), out long result) ? result : fallback;
        }

        private bool ValidatePrices()
        {
            decimal prixV = ParseDecimal(PrixV.Text);
            decimal prixA = ParseDecimal(PrixA.Text);
            int qte = ParseInt(Quantite.Text);

            if (prixV < prixA)
            {
                MessageBox.Show("Le prix de vente doit être Superieure ou égal au prix d'achat.");
                return false;
            }
            if (qte == 0)
            {
                MessageBox.Show("Donner une quantite.");
                return false;
            }
            return true;
        }

        // Update the PopulateArticleFromForm method
        private void PopulateArticleFromForm()
        {
            a.Code = ParseLong(Code.Text);
            a.ArticleName = ArticleName.Text;
            a.PrixVente = ParseDecimal(PrixV.Text);
            a.PrixAchat = ParseDecimal(PrixA.Text);
            a.PrixMP = ParseDecimal(PrixMP.Text);
            a.Quantite = ParseInt(Quantite.Text);
            a.ArticleImage = selectedImageBytes;

            // Existing fields
            a.marque = string.IsNullOrWhiteSpace(Marque.Text) ? null : Marque.Text;
            a.tva = ParseDecimal(TVA.Text);
            a.numeroLot = string.IsNullOrWhiteSpace(NumeroLot.Text) ? null : NumeroLot.Text;
            a.bonlivraison = string.IsNullOrWhiteSpace(BonLivraison.Text) ? null : BonLivraison.Text;
            a.Date = DateArticle.SelectedDate;
            a.DateLivraison = DateLivraison.SelectedDate;
            a.DateExpiration = DateExpiration.SelectedDate;
            a.IsUnlimitedStock = IsUnlimitedStock.IsChecked ?? false;


            // NEW PACKAGING FIELDS
            a.PiecesPerPackage = ParseInt(PiecesPerPackage.Text, 1);
            a.PackageType = (PackageType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
            a.PackageWeight = ParseDecimal(PackageWeight.Text);
            a.PackageDimensions = string.IsNullOrWhiteSpace(PackageDimensions.Text) ? null : PackageDimensions.Text;

            // NEW INVENTORY MANAGEMENT FIELDS
            a.MinimumStock = ParseInt(MinimumStock.Text);
            a.MaximumStock = ParseInt(MaximumStock.Text);
            a.StorageLocation = string.IsNullOrWhiteSpace(StorageLocation.Text) ? null : StorageLocation.Text;
            a.SKU = string.IsNullOrWhiteSpace(SKU.Text) ? null : SKU.Text;
            a.Description = string.IsNullOrWhiteSpace(Description.Text) ? null : Description.Text;
            a.IsPerishable = IsPerishable.IsChecked ?? false;
            a.UnitOfMeasure = (UnitOfMeasure.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pièce";
            a.PrixGros = ParseDecimal(PrixGros.Text);
            a.MinQuantityForGros = ParseInt(MinQuantityForGros.Text);
            a.CountryOfOrigin = string.IsNullOrWhiteSpace(CountryOfOrigin.Text) ? null : CountryOfOrigin.Text;
            a.Manufacturer = string.IsNullOrWhiteSpace(Manufacturer.Text) ? null : Manufacturer.Text;
            a.LastRestockDate = LastRestockDate.SelectedDate;
            a.Notes = string.IsNullOrWhiteSpace(Notes.Text) ? null : Notes.Text;

            foreach (Famille f in lf)
                if (f.FamilleName == FamilliesList.SelectedItem)
                {
                    a.FamillyID = f.FamilleID;
                    break;
                }

            foreach (Fournisseur fo in lfo)
            {
                if (fo.Nom == FournisseurList.SelectedItem)
                {
                    a.FournisseurID = fo.FournisseurID;
                    break;
                }
            }
        }

        private int GetSelectedPaymentMethodID()
        {
            if (PaymentMethodComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return (int)selectedItem.Tag;
            }
            return 0;
        }

        private void CashButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateRequiredFields()) return;

            PopulateArticleFromForm();

            if (!ValidatePrices()) return;
            if (FournisseurList.Text == "")
            {
                MessageBox.Show("Veuillez selectionner un fournisseur ");
                return;
            }
            if (PaymentMethodComboBox.SelectedItem == null)
            {
                MessageBox.Show("Veuillez selectionner un mode de paiement, si il y aacun method de payment ajouter la depuis parametres ");
                return;
            }


            foreach (Article aa in la)
            {
                if (aa.ArticleName.ToLower() == a.ArticleName.ToLower() && aa.Code != a.Code)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Le Nom de ce Article exist avec un different Code Vous voulez pozer le code de cette Article?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Code.Text = aa.Code.ToString();
                        a.Code = aa.Code;
                    }
                    return;
                }
                if (aa.ArticleName.ToLower() != a.ArticleName.ToLower() && aa.Code == a.Code)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Le Code de ce Article exist avec un different Nom Vous voulez pozer le Nom de cette Article?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ArticleName.Text = aa.ArticleName.ToString();
                        a.ArticleName = aa.ArticleName;
                    }
                    return;
                }

                if (aa.ArticleName.ToLower() == a.ArticleName.ToLower() && aa.Code == a.Code)
                {
                    if (aa.FournisseurID == a.FournisseurID)
                    {
                        MessageBox.Show("Ce Article deja exist sous ce fournisseur.");
                        return;
                    }
                }
            }

            int MethodID = GetSelectedPaymentMethodID();
            WConfirmTransaction w = new WConfirmTransaction(this, null, null, a, 0, MethodID);
            w.ShowDialog();
        }

        private void HalfButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateRequiredFields()) return;

            PopulateArticleFromForm();

            if (!ValidatePrices()) return;
            if (FournisseurList.Text == "")
            {
                MessageBox.Show("Veuillez selectionner un fournisseur ");
                return;
            }
            if (PaymentMethodComboBox.SelectedItem == null)
            {
                MessageBox.Show("Veuillez selectionner un mode de paiement, si il y aacun method de payment ajouter la depuis parametres ");
                return;
            }

            foreach (Article aa in la)
            {
                if (aa.ArticleName.ToLower() == a.ArticleName.ToLower() && aa.Code != a.Code)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Le Nom de ce Article exist avec un different Code Vous voulez pozer le code de cette Article?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Code.Text = aa.Code.ToString();
                        a.Code = aa.Code;
                    }
                    return;
                }
                if (aa.ArticleName.ToLower() != a.ArticleName.ToLower() && aa.Code == a.Code)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Le Code de ce Article exist avec un different Nom Vous voulez pozer le Nom de cette Article?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ArticleName.Text = aa.ArticleName.ToString();
                        a.ArticleName = aa.ArticleName;
                    }
                    return;
                }

                if (aa.ArticleName.ToLower() == a.ArticleName.ToLower() && aa.Code == a.Code)
                {
                    if (aa.FournisseurID == a.FournisseurID)
                    {
                        MessageBox.Show("Ce Article deja exist sous ce fournisseur.");
                        return;
                    }
                }
            }

            int MethodID = GetSelectedPaymentMethodID();
            WConfirmTransaction w = new WConfirmTransaction(this, null, null, a, 1, MethodID);
            w.ShowDialog();
        }

        private void CreditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateRequiredFields()) return;

            PopulateArticleFromForm();

            if (!ValidatePrices()) return;
            if (FournisseurList.Text == "")
            {
                MessageBox.Show("Veuillez selectionner un fournisseur ");
                return;
            }
            if (PaymentMethodComboBox.SelectedItem == null)
            {
                MessageBox.Show("Veuillez selectionner un mode de paiement, si il y aacun method de payment ajouter la depuis parametres ");
                return;
            }

            foreach (Article aa in la)
            {
                if (aa.ArticleName.ToLower() == a.ArticleName.ToLower() && aa.Code != a.Code)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Le Nom de ce Article exist avec un different Code Vous voulez pozer le code de cette Article?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Code.Text = aa.Code.ToString();
                        a.Code = aa.Code;
                    }
                    return;
                }
                if (aa.ArticleName.ToLower() != a.ArticleName.ToLower() && aa.Code == a.Code)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Le Code de ce Article exist avec un different Nom Vous voulez pozer le Nom de cette Article?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ArticleName.Text = aa.ArticleName.ToString();
                        a.ArticleName = aa.ArticleName;
                    }
                    return;
                }

                if (aa.ArticleName.ToLower() == a.ArticleName.ToLower() && aa.Code == a.Code)
                {
                    if (aa.FournisseurID == a.FournisseurID)
                    {
                        MessageBox.Show("Ce Article deja exist sous ce fournisseur.");
                        return;
                    }
                }
            }

            int MethodID = GetSelectedPaymentMethodID();
            WConfirmTransaction w = new WConfirmTransaction(this, null, null, a, 2, MethodID);
            w.ShowDialog();
        }

        private void AjouterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateRequiredFields()) return;

            PopulateArticleFromForm();

            if (!ValidatePrices()) return;

            foreach (Article aa in la)
            {
                if (aa.ArticleName.ToLower() == a.ArticleName.ToLower() && aa.Code != a.Code)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Le Nom de ce Article exist avec un different Code Vous voulez pozer le code de cette Article?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Code.Text = aa.Code.ToString();
                        a.Code = aa.Code;
                    }
                    return;
                }
                if (aa.ArticleName.ToLower() != a.ArticleName.ToLower() && aa.Code == a.Code)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Le Code de ce Article exist avec un different Nom Vous voulez pozer le Nom de cette Article?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ArticleName.Text = aa.ArticleName.ToString();
                        a.ArticleName = aa.ArticleName;
                    }
                    return;
                }

                if (aa.ArticleName.ToLower() == a.ArticleName.ToLower() && aa.Code == a.Code)
                {
                    if (aa.FournisseurID == a.FournisseurID)
                    {
                        MessageBox.Show("Ce Article deja exist sous ce fournisseur.");
                        return;
                    }
                }
            }

            foreach (CSingleRowArticle csar in ns.AMA.ArticlesContainer.Children)
            {
                if (csar.a.ArticleName == a.ArticleName)
                {
                    MessageBox.Show("Vous avez deja ajouter un article avec ce nom");
                    return;
                }
                else if (csar.a.Code == a.Code)
                {
                    MessageBox.Show("Vous avez deja ajouter un article avec ce code");
                    return;
                }
            }

            CSingleRowArticle cSingleRowArticle = new CSingleRowArticle(a, main.la, null, main, 7, ea, ns, 0);
            ns.AMA.ArticlesContainer.Children.Add(cSingleRowArticle);
            ns.Close();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}