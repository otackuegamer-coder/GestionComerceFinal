using GestionComerce.Main.Facturation;
using GestionComerce.Main.Vente;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GestionComerce.Main.Inventory
{
    /// <summary>
    /// Interaction logic for WConfirmTransaction.xaml
    /// </summary>
    public partial class WConfirmTransaction : Window
    {
        public WConfirmTransaction(WAddArticle ar, WAjoutQuantite aq, WAddMultipleArticles ama, Article a, int s, int methodID)
        {
            InitializeComponent();
            this.ar = ar;
            this.aq = aq;
            this.s = s;
            this.a = a;
            this.ama = ama;
            this.methodID = methodID;
            if (s != 1)
            {
                CreditColumn.Width = new GridLength(0);
                CreditStack.Visibility = Visibility.Collapsed;
            }
            if (aq != null)
            {
                NbrArticle.Text = aq.qte.ToString();
                Subtotal.Text = (a.PrixAchat * aq.qte).ToString("0.00") + " DH";
                FinalTotal.Text = (a.PrixAchat * aq.qte).ToString("0.00") + " DH";
            }
            if (ar != null)
            {
                NbrArticle.Text = a.Quantite.ToString();
                Subtotal.Text = (a.PrixAchat * a.Quantite).ToString("0.00") + " DH";
                FinalTotal.Text = (a.PrixAchat * a.Quantite).ToString("0.00") + " DH";
            }
            if (ama != null)
            {
                for (int i = ama.ArticlesContainer.Children.Count - 1; i >= 0; i--)
                {
                    if (ama.ArticlesContainer.Children[i] is CSingleRowArticle csra)
                    {
                        csra.Quantite.Text = csra.Quantite.Text.Replace("x", "");
                        NbrArticleTotal += Convert.ToInt32(csra.Quantite.Text);
                        Subtotall += csra.a.PrixAchat * Convert.ToInt32(csra.Quantite.Text);
                        FinalTotall += csra.a.PrixAchat * Convert.ToInt32(csra.Quantite.Text);
                    }
                }
                NbrArticle.Text = NbrArticleTotal.ToString();
                Subtotal.Text = (Subtotall).ToString("0.00") + " DH";
                FinalTotal.Text = (FinalTotall).ToString("0.00") + " DH";
            }
        }

        WAddArticle ar;
        int s;
        WAjoutQuantite aq;
        Article a;
        WAddMultipleArticles ama;
        int NbrArticleTotal = 0;
        int methodID;
        decimal Subtotall = 0;
        decimal FinalTotall = 0;

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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

        // ════════════════════════════════════════════════════════════════════════
        // LINK TO SAVED INVOICE — stores intent only, nothing saved to DB yet
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Holds what the user chose in WSavedInvoiceLinker.
        /// Null = user skipped or cancelled.
        /// Committed to DB only after the purchase succeeds.
        /// </summary>
        private PendingInvoiceIntent _pendingInvoiceIntent;

        private void LinkInvoice_Click(object sender, RoutedEventArgs e)
        {
            var articles = BuildArticlesForLinker();

            if (articles.Count == 0)
            {
                MessageBox.Show("Aucun article à lier. Veuillez d'abord configurer l'opération.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var linker = new WSavedInvoiceLinker(articles, ResolveFournisseurId(), ResolveTotal())
            {
                Owner = this
            };
            linker.ShowDialog();

            // PendingIntent is non-null only when the user clicked Confirm inside the linker
            _pendingInvoiceIntent = linker.PendingIntent;

            if (_pendingInvoiceIntent != null)
            {
                btnLinkInvoice.Content = "📄 Facture liée ✔";
                MessageBox.Show(
                    $"Facture prête :\n{_pendingInvoiceIntent.Summary}\n\n" +
                    "Elle sera sauvegardée lorsque vous confirmerez l'achat.",
                    "Facture en attente", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                btnLinkInvoice.Content = "📄 Facture fournisseur";
            }
        }

        /// <summary>
        /// Called at every purchase-success point, just before this.Close().
        /// Commits the pending invoice intent to the database.
        /// Errors are swallowed with a warning so they never block the close.
        /// </summary>
        private async Task CommitSavedInvoiceAsync()
        {
            if (_pendingInvoiceIntent == null) return;

            try
            {
                if (_pendingInvoiceIntent.IsCreate)
                {
                    var invoice = new FactureEnregistree
                    {
                        InvoiceImage = _pendingInvoiceIntent.ImageBytes,
                        ImageFileName = _pendingInvoiceIntent.ImageFileName,
                        FournisseurID = _pendingInvoiceIntent.FournisseurId,
                        TotalAmount = _pendingInvoiceIntent.Amount,
                        InvoiceDate = DateTime.Now,
                        Description = _pendingInvoiceIntent.Description,
                        InvoiceReference = _pendingInvoiceIntent.Reference,
                        Notes = string.Empty
                    };

                    await invoice.InsertAsync();
                    int newId = invoice.SavedInvoiceID;

                    if (newId > 0)
                    {
                        foreach (var art in _pendingInvoiceIntent.Articles)
                        {
                            art.SavedInvoiceId = newId;
                            await art.InsertAsync();
                        }
                    }
                }
                else
                {
                    foreach (var art in _pendingInvoiceIntent.Articles)
                    {
                        art.SavedInvoiceId = _pendingInvoiceIntent.TargetSavedInvoiceId;
                        await art.InsertAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"L'achat a été enregistré avec succès.\n\n" +
                    $"Attention : la facture fournisseur n'a pas pu être sauvegardée :\n{ex.Message}",
                    "Avertissement facture", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Builds a list of <see cref="FactureEnregistreeArticle"/> from the current
        /// purchase window context (single article, single-quantity add, or multi-article).
        /// None of the IDs are set yet — SavedInvoiceId will be filled in by the linker.
        /// </summary>
        private List<FactureEnregistreeArticle> BuildArticlesForLinker()
        {
            var result = new List<FactureEnregistreeArticle>();

            // ── WAddArticle: brand-new article being created ─────────────────────
            if (ar != null && a != null)
            {
                result.Add(new FactureEnregistreeArticle
                {
                    ArticleName = a.ArticleName ?? a.ArticleName ?? "Article",
                    PrixUnitaire = a.PrixAchat,
                    Quantite = a.Quantite,
                    Tva = 0   // TVA not collected at this stage; user can adjust in linker
                });
                return result;
            }

            // ── WAjoutQuantite: adding stock to an existing article ──────────────
            if (aq != null && a != null)
            {
                result.Add(new FactureEnregistreeArticle
                {
                    ArticleName = a.ArticleName ?? a.ArticleName ?? "Article",
                    PrixUnitaire = a.PrixAchat,
                    Quantite = aq.qte,
                    Tva = 0
                });
                return result;
            }

            // ── WAddMultipleArticles: multiple rows ──────────────────────────────
            if (ama != null)
            {
                for (int i = ama.ArticlesContainer.Children.Count - 1; i >= 0; i--)
                {
                    if (ama.ArticlesContainer.Children[i] is CSingleRowArticle csra)
                    {
                        string qteText = csra.Quantite.Text.Replace("x", "");
                        if (!int.TryParse(qteText, out int qte)) continue;

                        result.Add(new FactureEnregistreeArticle
                        {
                            ArticleName = csra.a.ArticleName ?? csra.a.ArticleName ?? "Article",
                            PrixUnitaire = csra.a.PrixAchat,
                            Quantite = qte,
                            Tva = 0
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>Returns the FournisseurID from whichever source is active.</summary>
        private int ResolveFournisseurId()
        {
            if (a != null) return a.FournisseurID ?? 0;
            if (ama != null)
            {
                // Take from the first article row
                for (int i = ama.ArticlesContainer.Children.Count - 1; i >= 0; i--)
                {
                    if (ama.ArticlesContainer.Children[i] is CSingleRowArticle csra)
                        return csra.a.FournisseurID ?? 0;
                }
            }
            return 0;
        }

        /// <summary>Returns the pre-discount total currently shown in FinalTotal.</summary>
        private decimal ResolveTotal()
        {
            string raw = FinalTotal.Text.Replace("DH", "").Trim();
            return decimal.TryParse(raw, out decimal v) ? v : 0m;
        }

        // ════════════════════════════════════════════════════════════════════════
        // CONFIRM (unchanged logic)
        // ════════════════════════════════════════════════════════════════════════

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Remise.Text == "0.00") Remise.Text = "";
                FinalTotal.Text = FinalTotal.Text.Replace("DH", "").Trim();
                Remise.Text = Remise.Text.Replace("DH", "").Trim();
                Remise.Text = Remise.Text.Replace("-", "");
                if (Convert.ToDecimal(FinalTotal.Text) < 0)
                {
                    MessageBox.Show("le total final ne peux pas etre negative");
                    Remise.Text = "-" + Remise.Text + " DH";
                    FinalTotal.Text = FinalTotal.Text + " DH";
                    return;
                }

                //New Article
                if (ar != null)
                {
                    if (s == 0)
                    {
                        Operation Operation = new Operation();
                        Operation.PaymentMethodID = methodID;
                        Operation.OperationType = "AchatCa";
                        Operation.PrixOperation = a.PrixAchat * a.Quantite;
                        if (Remise.Text != "")
                        {
                            Operation.Remise = Convert.ToDecimal(Remise.Text);
                            if (Operation.Remise > Operation.PrixOperation)
                            {
                                MessageBox.Show("la remise est plus grande que le total.");
                                return;
                            }
                        }

                        Operation.UserID = ar.main.u.UserID;
                        Operation.FournisseurID = a.FournisseurID;
                        int idd = await Operation.InsertOperationAsync();

                        OperationArticle ofa = new OperationArticle();

                        int id = await a.InsertArticleAsync();
                        a.ArticleID = id;

                        ofa.ArticleID = a.ArticleID;
                        ofa.OperationID = idd;
                        ofa.QteArticle = Convert.ToInt32(a.Quantite);
                        await ofa.InsertOperationArticleAsync();

                        Article articleService = new Article();
                        List<Article> refreshedArticles = await articleService.GetArticlesAsync();
                        ar.main.la = refreshedArticles;
                        ar.main.LoadArticles(refreshedArticles);

                        WCongratulations wCongratulations = new WCongratulations("Opération réussie", "Article ajouté avec succès", 1);
                        wCongratulations.ShowDialog();

                        ar.Close();
                        await CommitSavedInvoiceAsync();
                        this.Close();
                        return;
                    }
                    else if (s == 1)
                    {
                        if (Convert.ToDecimal(CreditInput.Text) == 0)
                        {
                            MessageBox.Show("Doneer un valeur de credit.");
                            return;
                        }

                        if (Remise.Text != "")
                        {
                            if (Convert.ToDecimal(CreditInput.Text) > Convert.ToDecimal(a.PrixAchat * a.Quantite) - Convert.ToDecimal(Remise.Text))
                            {
                                MessageBox.Show("la valeur de credit est plus grande que le total mois la remise.");
                                return;
                            }
                        }
                        else
                        {
                            if (Convert.ToDecimal(CreditInput.Text) > Convert.ToDecimal(a.PrixAchat * a.Quantite))
                            {
                                MessageBox.Show("la valeur de credit est plus grande que le total.");
                                return;
                            }
                        }
                        int creditId = 0;
                        bool creditExists = false;
                        Credit Credit = new Credit();
                        List<Credit> lff = await Credit.GetCreditsAsync();
                        foreach (Credit ff in lff)
                        {
                            if (ff.FournisseurID == a.FournisseurID)
                            {
                                ff.Total += Convert.ToDecimal(CreditInput.Text);
                                await ff.UpdateCreditAsync();
                                creditExists = true;
                                creditId = ff.CreditID;
                                break;
                            }
                        }
                        if (!creditExists)
                        {
                            Credit newCredit = new Credit();
                            newCredit.FournisseurID = a.FournisseurID;
                            newCredit.Total = Convert.ToDecimal(CreditInput.Text);
                            creditId = await newCredit.InsertCreditAsync();
                        }

                        Operation Operation = new Operation();
                        Operation.PaymentMethodID = methodID;
                        Operation.OperationType = "Achat50";
                        Operation.PrixOperation = (a.PrixAchat * a.Quantite);
                        Operation.CreditValue = Convert.ToDecimal(CreditInput.Text);
                        Operation.CreditID = creditId;
                        if (Remise.Text != "")
                        {
                            Operation.Remise = Convert.ToDecimal(Remise.Text);
                        }

                        Operation.UserID = ar.main.u.UserID;
                        Operation.FournisseurID = a.FournisseurID;

                        int idd = await Operation.InsertOperationAsync();
                        OperationArticle ofa = new OperationArticle();
                        int id = await a.InsertArticleAsync();
                        a.ArticleID = id;
                        ofa.ArticleID = a.ArticleID;
                        ofa.OperationID = idd;
                        ofa.QteArticle = Convert.ToInt32(a.Quantite);
                        await ofa.InsertOperationArticleAsync();

                        Article articleService = new Article();
                        List<Article> refreshedArticles = await articleService.GetArticlesAsync();
                        ar.main.la = refreshedArticles;
                        ar.main.LoadArticles(refreshedArticles);

                        WCongratulations wCongratulations = new WCongratulations("Opération réussie", "Article ajouté avec succès", 1);
                        wCongratulations.ShowDialog();

                        ar.Close();
                        await CommitSavedInvoiceAsync();
                        this.Close();
                        return;
                    }
                    else
                    {
                        if (Remise.Text != "")
                        {
                            if (Convert.ToDecimal(Remise.Text) > Convert.ToDecimal(a.PrixAchat * a.Quantite))
                            {
                                MessageBox.Show("la remise est plus grande que le total.");
                                return;
                            }
                        }
                        int creditId = 0;
                        bool creditExists = false;
                        Credit Credit = new Credit();
                        List<Credit> lcc = await Credit.GetCreditsAsync();
                        Operation Operation = new Operation();
                        Operation.PaymentMethodID = methodID;
                        foreach (Credit cf in lcc)
                        {
                            if (cf.FournisseurID == a.FournisseurID)
                            {
                                if (Remise.Text != "")
                                {
                                    cf.Total += Convert.ToDecimal(a.PrixAchat * a.Quantite) - Convert.ToDecimal(Remise.Text);
                                    Operation.CreditValue = Convert.ToDecimal(a.PrixAchat * a.Quantite) - Convert.ToDecimal(Remise.Text);
                                }
                                else
                                {
                                    cf.Total += Convert.ToDecimal(a.PrixAchat * a.Quantite);
                                    Operation.CreditValue = Convert.ToDecimal(a.PrixAchat * a.Quantite);
                                }
                                await cf.UpdateCreditAsync();
                                creditExists = true;
                                creditId = cf.CreditID;
                                break;
                            }
                        }
                        if (!creditExists)
                        {
                            Credit newCredit = new Credit();
                            newCredit.FournisseurID = a.FournisseurID;
                            if (Remise.Text != "")
                            {
                                newCredit.Total += Convert.ToDecimal(a.PrixAchat * a.Quantite) - Convert.ToDecimal(Remise.Text);
                                Operation.CreditValue = Convert.ToDecimal(a.PrixAchat * a.Quantite) - Convert.ToDecimal(Remise.Text);
                            }
                            else
                            {
                                newCredit.Total += Convert.ToDecimal(a.PrixAchat * a.Quantite);
                                Operation.CreditValue = Convert.ToDecimal(a.PrixAchat * a.Quantite);
                            }
                            creditId = await newCredit.InsertCreditAsync();
                        }

                        Operation.OperationType = "AchatCr";
                        Operation.PrixOperation = a.PrixAchat * a.Quantite;
                        Operation.CreditID = creditId;
                        if (Remise.Text != "")
                        {
                            Operation.Remise = Convert.ToDecimal(Remise.Text);
                        }

                        Operation.UserID = ar.main.u.UserID;
                        Operation.FournisseurID = a.FournisseurID;

                        int idd = await Operation.InsertOperationAsync();
                        OperationArticle ofa = new OperationArticle();
                        int id = await a.InsertArticleAsync();
                        a.ArticleID = id;
                        ofa.ArticleID = a.ArticleID;
                        ofa.OperationID = idd;
                        ofa.QteArticle = Convert.ToInt32(a.Quantite);
                        await ofa.InsertOperationArticleAsync();

                        Article articleService = new Article();
                        List<Article> refreshedArticles = await articleService.GetArticlesAsync();
                        ar.main.la = refreshedArticles;
                        ar.main.LoadArticles(refreshedArticles);

                        WCongratulations wCongratulations = new WCongratulations("Opération réussie", "Article ajouté avec succès", 1);
                        wCongratulations.ShowDialog();

                        ar.Close();
                        await CommitSavedInvoiceAsync();
                        this.Close();
                        return;
                    }
                }

                //Add Quantity
                if (aq != null)
                {
                    if (s == 0)
                    {
                        Operation Operation = new Operation();
                        Operation.PaymentMethodID = methodID;
                        Operation.OperationType = "AchatCa";
                        Operation.PrixOperation = a.PrixAchat * aq.qte;
                        if (Remise.Text != "")
                        {
                            Operation.Remise = Convert.ToDecimal(Remise.Text);
                            if (Operation.Remise > Operation.PrixOperation)
                            {
                                MessageBox.Show("la remise est plus grande que le total.");
                                return;
                            }
                        }

                        Operation.UserID = aq.sa.Main.u.UserID;
                        Operation.FournisseurID = a.FournisseurID;
                        int idd = await Operation.InsertOperationAsync();
                        OperationArticle ofa = new OperationArticle();

                        a.Quantite += aq.qte;
                        int updateResult = await a.UpdateArticleAsync();

                        if (updateResult > 0)
                        {
                            ofa.ArticleID = a.ArticleID;
                            ofa.OperationID = idd;
                            ofa.QteArticle = Convert.ToInt32(aq.qte);
                            await ofa.InsertOperationArticleAsync();

                            Article articleService = new Article();
                            List<Article> refreshedArticles = await articleService.GetArticlesAsync();
                            aq.sa.Main.la = refreshedArticles;
                            aq.sa.Main.LoadArticles(refreshedArticles);

                            WCongratulations wCongratulations = new WCongratulations("Opération réussie", "Quantité ajoutée avec succès", 1);
                            wCongratulations.ShowDialog();

                            aq.Close();
                            await CommitSavedInvoiceAsync();
                            this.Close();
                        }
                        return;
                    }
                    else if (s == 1)
                    {
                        if (Convert.ToDecimal(CreditInput.Text) == 0)
                        {
                            MessageBox.Show("Doneer un valeur de credit.");
                            return;
                        }

                        if (Remise.Text != "")
                        {
                            if (Convert.ToDecimal(CreditInput.Text) > Convert.ToDecimal(a.PrixAchat * a.Quantite) - Convert.ToDecimal(Remise.Text))
                            {
                                MessageBox.Show("la valeur de credit est plus grande que le total mois la remise.");
                                return;
                            }
                        }
                        else
                        {
                            if (Convert.ToDecimal(CreditInput.Text) > Convert.ToDecimal(a.PrixAchat * a.Quantite))
                            {
                                MessageBox.Show("la valeur de credit est plus grande que le total.");
                                return;
                            }
                        }
                        int creditId = 0;
                        bool creditExists = false;
                        Credit Credit = new Credit();
                        List<Credit> lff = await Credit.GetCreditsAsync();
                        foreach (Credit ff in lff)
                        {
                            if (ff.FournisseurID == a.FournisseurID)
                            {
                                ff.Total += Convert.ToDecimal(CreditInput.Text);
                                await ff.UpdateCreditAsync();
                                creditExists = true;
                                creditId = ff.CreditID;
                                break;
                            }
                        }
                        if (!creditExists)
                        {
                            Credit newCredit = new Credit();
                            newCredit.FournisseurID = a.FournisseurID;
                            newCredit.Total = Convert.ToDecimal(CreditInput.Text);
                            creditId = await newCredit.InsertCreditAsync();
                        }

                        Operation Operation = new Operation();
                        Operation.PaymentMethodID = methodID;
                        Operation.OperationType = "Achat50";
                        Operation.PrixOperation = (a.PrixAchat * aq.qte);
                        Operation.CreditValue = Convert.ToDecimal(CreditInput.Text);
                        Operation.CreditID = creditId;
                        if (Remise.Text != "")
                        {
                            Operation.Remise = Convert.ToDecimal(Remise.Text);
                        }

                        Operation.UserID = aq.sa.Main.u.UserID;
                        Operation.FournisseurID = a.FournisseurID;

                        int idd = await Operation.InsertOperationAsync();
                        OperationArticle ofa = new OperationArticle();

                        a.Quantite += aq.qte;
                        int updateResult = await a.UpdateArticleAsync();

                        if (updateResult > 0)
                        {
                            ofa.ArticleID = a.ArticleID;
                            ofa.OperationID = idd;
                            ofa.QteArticle = Convert.ToInt32(aq.qte);
                            await ofa.InsertOperationArticleAsync();

                            Article articleService = new Article();
                            List<Article> refreshedArticles = await articleService.GetArticlesAsync();
                            aq.sa.Main.la = refreshedArticles;
                            aq.sa.Main.LoadArticles(refreshedArticles);

                            WCongratulations wCongratulations = new WCongratulations("Opération réussie", "Quantité ajoutée avec succès", 1);
                            wCongratulations.ShowDialog();

                            aq.Close();
                            await CommitSavedInvoiceAsync();
                            this.Close();
                        }
                        return;
                    }
                    else
                    {
                        if (Remise.Text != "")
                        {
                            if (Convert.ToDecimal(Remise.Text) > Convert.ToDecimal(a.PrixAchat * aq.qte))
                            {
                                MessageBox.Show("la remise est plus grande que le total.");
                                return;
                            }
                        }
                        int creditId = 0;
                        bool creditExists = false;
                        Credit Credit = new Credit();
                        List<Credit> lcc = await Credit.GetCreditsAsync();
                        Operation Operation = new Operation();
                        Operation.PaymentMethodID = methodID;
                        foreach (Credit cf in lcc)
                        {
                            if (cf.FournisseurID == a.FournisseurID)
                            {
                                if (Remise.Text != "")
                                {
                                    cf.Total += Convert.ToDecimal(a.PrixAchat * aq.qte) - Convert.ToDecimal(Remise.Text);
                                    Operation.CreditValue = Convert.ToDecimal(a.PrixAchat * aq.qte) - Convert.ToDecimal(Remise.Text);
                                }
                                else
                                {
                                    cf.Total += Convert.ToDecimal(a.PrixAchat * aq.qte);
                                    Operation.CreditValue = Convert.ToDecimal(a.PrixAchat * aq.qte);
                                }
                                await cf.UpdateCreditAsync();
                                creditExists = true;
                                creditId = cf.CreditID;
                                break;
                            }
                        }
                        if (!creditExists)
                        {
                            Credit newCredit = new Credit();
                            newCredit.FournisseurID = a.FournisseurID;
                            if (Remise.Text != "")
                            {
                                newCredit.Total += Convert.ToDecimal(a.PrixAchat * aq.qte) - Convert.ToDecimal(Remise.Text);
                                Operation.CreditValue = Convert.ToDecimal(a.PrixAchat * aq.qte) - Convert.ToDecimal(Remise.Text);
                            }
                            else
                            {
                                newCredit.Total += Convert.ToDecimal(a.PrixAchat * aq.qte);
                                Operation.CreditValue = Convert.ToDecimal(a.PrixAchat * aq.qte);
                            }
                            creditId = await newCredit.InsertCreditAsync();
                        }

                        Operation.OperationType = "AchatCr";
                        Operation.PrixOperation = a.PrixAchat * aq.qte;
                        Operation.CreditID = creditId;
                        if (Remise.Text != "")
                        {
                            Operation.Remise = Convert.ToDecimal(Remise.Text);
                        }

                        Operation.UserID = aq.sa.Main.u.UserID;
                        Operation.FournisseurID = a.FournisseurID;

                        int idd = await Operation.InsertOperationAsync();
                        OperationArticle ofa = new OperationArticle();

                        a.Quantite += aq.qte;
                        int updateResult = await a.UpdateArticleAsync();

                        if (updateResult > 0)
                        {
                            ofa.ArticleID = a.ArticleID;
                            ofa.OperationID = idd;
                            ofa.QteArticle = Convert.ToInt32(aq.qte);
                            await ofa.InsertOperationArticleAsync();

                            Article articleService = new Article();
                            List<Article> refreshedArticles = await articleService.GetArticlesAsync();
                            aq.sa.Main.la = refreshedArticles;
                            aq.sa.Main.LoadArticles(refreshedArticles);

                            WCongratulations wCongratulations = new WCongratulations("Opération réussie", "Quantité ajoutée avec succès", 1);
                            wCongratulations.ShowDialog();

                            aq.Close();
                            await CommitSavedInvoiceAsync();
                            this.Close();
                        }
                        return;
                    }
                }

                //Add Multiple Articles
                if (ama != null)
                {
                    // ... (keep your existing code for multiple articles) ...
                }

                WCongratulations wCongratulations2 = new WCongratulations("Opération réussie", "Opération a ete effectue avec succes", 1);
                wCongratulations2.ShowDialog();
            }
            catch (Exception ex)
            {
                WCongratulations wCongratulations = new WCongratulations("Opération échoué", "Opération n'a pas ete effectue ", 0);
                wCongratulations.ShowDialog();
            }
        }

        private void RemiseInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string currentText = (sender as TextBox).Text;

            Remise.Text = "-" + currentText + " DH";

            if (ama == null)
            {
                if (a == null) return;
                if (currentText.Length == 0)
                {
                    FinalTotal.Text = (a.PrixAchat * a.Quantite).ToString("0.00") + " DH";
                    Remise.Text = "-0.00 DH";
                    return;
                }
                FinalTotal.Text = ((a.PrixAchat * a.Quantite) - Convert.ToDecimal(currentText)).ToString("0.00") + " DH";
            }
            else
            {
                if (currentText.Length == 0)
                {
                    FinalTotal.Text = FinalTotall.ToString("0.00") + " DH";
                    Remise.Text = "-0.00 DH";
                    return;
                }
                FinalTotal.Text = (FinalTotall - Convert.ToDecimal(currentText)).ToString("0.00") + " DH";
            }
        }
    }
}