using GestionComerce.Main.Facturation;
using GestionComerce.Main.Facturation.CreateFacture;
using Microsoft.Win32;
using Superete;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestionComerce.Main.Facturation.CreateFacture
{
    public class InvoiceArticle
    {
        public int OperationID { get; set; }
        public int ArticleID { get; set; }
        public string ArticleName { get; set; }
        public decimal Prix { get; set; }
        public decimal Quantite { get; set; }
        public decimal TVA { get; set; }
        public bool Reversed { get; set; }
        public decimal InitialQuantity { get; set; }

        // Property to determine if stock should be reduced
        public bool ReduceStock { get; set; } = false;

        public decimal TotalHT => Prix * Quantite;
        public decimal MontantTVA => (TVA / 100) * TotalHT;
        public decimal TotalTTC => TotalHT + MontantTVA;
        public decimal ExpeditionTotal { get; set; } = 0;
    }

    public partial class CMainFa : UserControl
    {
        private const int ETAT_FACTURE_NORMAL = 0;
        private const int ETAT_FACTURE_REVERSED = 1;

        public MainWindow main;
        public User u;
        private decimal currentTotalHT = 0;
        private string _invoiceType = "Facture";

        private Client _selectedClient;
        public Client SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                UpdateClientFields();
            }
        }

        public string InvoiceType
        {
            get
            {
                if (cmbInvoiceType?.SelectedItem is ComboBoxItem selectedItem)
                {
                    _invoiceType = selectedItem.Content?.ToString() ?? "Facture";
                }
                return _invoiceType;
            }
            set
            {
                _invoiceType = value;
            }
        }

        public List<InvoiceArticle> InvoiceArticles = new List<InvoiceArticle>();
        public List<Operation> SelectedOperations = new List<Operation>();

        public CMainFa(User u, MainWindow main, CMainIn In, Operation op)
        {
            InitializeComponent();
            SelectedOperations = new List<Operation>();
            InvoiceArticles = new List<InvoiceArticle>();
            this.main = main;
            this.u = u;

            this.Loaded += async (s, e) =>
            {
                await LoadPaymentMethods();

                if (op != null)
                {
                    InitializeWithOperation(op);

                    // Automatically set the client if the operation has one
                    if (op.ClientID.HasValue && op.ClientID.Value > 0)
                    {
                        // Find the client from the main window's client list
                        Client operationClient = main.lc?.FirstOrDefault(c => c.ClientID == op.ClientID.Value);

                        if (operationClient != null)
                        {
                            // Set the selected client
                            SelectedClient = operationClient;

                            // Update the UI fields
                            UpdateClientFields();

                            System.Diagnostics.Debug.WriteLine($"Auto-selected client: {GetClientName(operationClient)}");
                        }
                    }
                }
            };

            LoadFacture();
        }

        private async Task LoadPaymentMethods()
        {
            try
            {
                PaymentMethod pm = new PaymentMethod();
                List<PaymentMethod> methods = await pm.GetPaymentMethodsAsync();

                if (cmbPaymentMethod != null)
                {
                    cmbPaymentMethod.Items.Clear();
                    foreach (var method in methods)
                    {
                        ComboBoxItem item = new ComboBoxItem
                        {
                            Content = method.PaymentMethodName,
                            Tag = method.PaymentMethodID
                        };
                        cmbPaymentMethod.Items.Add(item);
                    }

                    if (cmbPaymentMethod.Items.Count > 0)
                    {
                        cmbPaymentMethod.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payment methods: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper method to get client name based on your actual Client class structure
        private string GetClientName(Client client)
        {
            if (client == null) return "";

            var properties = client.GetType().GetProperties();

            // Look for properties that might contain the name
            foreach (var prop in properties)
            {
                if (prop.Name.ToLower().Contains("name") ||
                    prop.Name.ToLower().Contains("nom") ||
                    prop.Name.ToLower().Contains("clientname") ||
                    prop.Name.ToLower().Contains("fullname"))
                {
                    var value = prop.GetValue(client) as string;
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            // Fallback to ToString() if no name property found
            return client.ToString();
        }

        // Helper method to get client address
        private string GetClientAddress(Client client)
        {
            if (client == null) return "";

            var properties = client.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.Name.ToLower().Contains("address") ||
                    prop.Name.ToLower().Contains("adress") ||
                    prop.Name.ToLower().Contains("adresse"))
                {
                    var value = prop.GetValue(client) as string;
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            return "";
        }

        // Helper method to get client VAT/TVA
        private string GetClientVAT(Client client)
        {
            if (client == null) return "";

            var properties = client.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.Name.ToLower().Contains("vat") ||
                    prop.Name.ToLower().Contains("tva"))
                {
                    var value = prop.GetValue(client) as string;
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            return "";
        }

        // Helper method to get client ICE
        private string GetClientICE(Client client)
        {
            if (client == null) return "";

            var properties = client.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.Name.ToLower().Contains("ice"))
                {
                    var value = prop.GetValue(client) as string;
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            return "";
        }

        // Helper method to get client telephone
        private string GetClientTelephone(Client client)
        {
            if (client == null) return "";

            var properties = client.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.Name.ToLower().Contains("phone") ||
                    prop.Name.ToLower().Contains("telephone") ||
                    prop.Name.ToLower().Contains("tel"))
                {
                    var value = prop.GetValue(client) as string;
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            return "";
        }

        // Helper method to get client EtatJuridique
        private string GetClientEtatJuridique(Client client)
        {
            if (client == null) return "";

            var properties = client.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.Name.ToLower().Contains("etat") ||
                    prop.Name.ToLower().Contains("juridique") ||
                    prop.Name.ToLower().Contains("etatjuridique"))
                {
                    var value = prop.GetValue(client) as string;
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            return "";
        }

        // Helper method to get client IdSociete
        private string GetClientIdSociete(Client client)
        {
            if (client == null) return "";

            var properties = client.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.Name.ToLower().Contains("idsociete") ||
                    prop.Name.ToLower().Contains("companyid") ||
                    prop.Name.ToLower().Contains("idsociety"))
                {
                    var value = prop.GetValue(client) as string;
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            return "";
        }

        private void UpdateClientFields()
        {
            if (_selectedClient != null)
            {
                txtClientName.Text = GetClientName(_selectedClient);
                txtClientICE.Text = GetClientICE(_selectedClient);
                txtClientVAT.Text = GetClientVAT(_selectedClient);
                txtClientPhone.Text = GetClientTelephone(_selectedClient);
                txtClientAddress.Text = GetClientAddress(_selectedClient);
                txtClientEtatJuridique.Text = GetClientEtatJuridique(_selectedClient);
                txtClientIdSociete.Text = GetClientIdSociete(_selectedClient);

                if (txtClientReference != null)
                {
                    txtClientReference.Text = _selectedClient.ClientID.ToString();
                    System.Diagnostics.Debug.WriteLine($"Client Reference set to: {_selectedClient.ClientID}");
                }
            }
            else
            {
                // Clear all client fields if no client selected
                if (txtClientName != null) txtClientName.Text = "";
                if (txtClientICE != null) txtClientICE.Text = "";
                if (txtClientVAT != null) txtClientVAT.Text = "";
                if (txtClientPhone != null) txtClientPhone.Text = "";
                if (txtClientAddress != null) txtClientAddress.Text = "";
                if (txtClientEtatJuridique != null) txtClientEtatJuridique.Text = "";
                if (txtClientIdSociete != null) txtClientIdSociete.Text = "";
                if (txtClientReference != null) txtClientReference.Text = "";
            }
        }

        private string ConvertToArabicLetters(decimal amount)
        {
            string[] ones = { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة" };
            string[] tens = { "", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };
            string[] hundreds = { "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };
            string[] teens = { "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر",
                             "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };

            int integerPart = (int)amount;
            int decimalPart = (int)((amount - integerPart) * 100);

            string result = "";

            // Process thousands
            int thousands = integerPart / 1000;
            if (thousands > 0)
            {
                if (thousands == 1)
                    result += "ألف ";
                else if (thousands == 2)
                    result += "ألفان ";
                else if (thousands <= 10)
                    result += ones[thousands] + " آلاف ";
                else
                    result += ConvertHundreds(thousands) + " ألف ";
            }

            // Process remaining hundreds
            int remainder = integerPart % 1000;
            result += ConvertHundreds(remainder);

            result += " درهم";

            if (decimalPart > 0)
            {
                result += " و " + ConvertHundreds(decimalPart) + " سنتيم";
            }

            return result.Trim();
        }

        private string ConvertHundreds(int num)
        {
            string[] ones = { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة" };
            string[] tens = { "", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };
            string[] hundreds = { "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };
            string[] teens = { "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر",
                             "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };

            string result = "";

            int h = num / 100;
            int t = (num % 100) / 10;
            int o = num % 10;

            if (h > 0)
                result += hundreds[h] + " ";

            if (t == 1)
            {
                result += teens[o];
            }
            else
            {
                if (t > 0)
                    result += tens[t] + " ";
                if (o > 0)
                    result += ones[o] + " ";
            }

            return result.Trim();
        }
        
        private string ConvertToFrenchLetters(decimal amount)
        {
            string[] ones = { "", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf",
                      "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize",
                      "dix-sept", "dix-huit", "dix-neuf" };

            string[] tens = { "", "", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante", "quatre-vingt", "quatre-vingt" };

            int integerPart = (int)amount;
            int decimalPart = (int)((amount - integerPart) * 100);

            string result = "";

            // Traiter les milliers
            int thousands = integerPart / 1000;
            if (thousands > 0)
            {
                if (thousands == 1)
                    result += "mille ";
                else
                    result += ConvertHundredsFrench(thousands) + " mille ";
            }

            // Traiter le reste (centaines, dizaines, unités)
            int remainder = integerPart % 1000;
            result += ConvertHundredsFrench(remainder);

            // Ajouter "dirhams" avec accord
            if (integerPart > 1)
                result += " dirhams";
            else if (integerPart == 1)
                result += " dirham";
            else
                result += " dirham";

            // Ajouter les centimes si présents
            if (decimalPart > 0)
            {
                result += " et " + ConvertHundredsFrench(decimalPart);
                if (decimalPart > 1)
                    result += " centimes";
                else
                    result += " centime";
            }

            return result.Trim();
        }
        
        private string ConvertHundredsFrench(int num)
        {
            if (num == 0) return "";

            string[] ones = { "", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf",
                      "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize",
                      "dix-sept", "dix-huit", "dix-neuf" };

            string[] tens = { "", "", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante", "quatre-vingt", "quatre-vingt" };

            string result = "";

            // Centaines
            int h = num / 100;
            if (h > 0)
            {
                if (h == 1)
                    result += "cent ";
                else
                    result += ones[h] + " cent ";

                // Accord de "cent" au pluriel si pas suivi d'autres chiffres
                if (num % 100 == 0 && h > 1)
                    result = result.TrimEnd() + "s ";
            }

            int remainder = num % 100;

            // Nombres de 1 à 19
            if (remainder < 20)
            {
                result += ones[remainder];
            }
            else
            {
                int t = remainder / 10;
                int o = remainder % 10;

                // Cas spéciaux pour 70-79 et 90-99
                if (t == 7) // 70-79
                {
                    result += "soixante";
                    if (o == 1)
                        result += " et onze";
                    else if (o == 11)
                        result += "-onze";
                    else
                        result += "-" + ones[10 + o];
                }
                else if (t == 9) // 90-99
                {
                    result += "quatre-vingt";
                    if (o == 0)
                        result += "s";
                    else
                        result += "-" + ones[10 + o];
                }
                else // 20-69, 80-89
                {
                    result += tens[t];

                    if (o == 1 && (t == 2 || t == 3 || t == 4 || t == 5 || t == 6))
                        result += " et un";
                    else if (o > 0)
                        result += "-" + ones[o];
                    else if (t == 8) // quatre-vingts
                        result += "s";
                }
            }

            return result.Trim();
        }

        // **MODIFIED: Invoice status is now always a user choice, independent of reversed operations**
        private void InitializeWithOperation(Operation op)
        {
            // Invoice status is now always a user choice - default to Normal
            if (EtatFacture != null)
            {
                EtatFacture.SelectedIndex = ETAT_FACTURE_NORMAL;
                EtatFacture.IsEnabled = true; // Always allow user to change status
            }

            // Auto-select client if operation has one
            if (op.ClientID.HasValue && op.ClientID.Value > 0 && main?.lc != null)
            {
                Client operationClient = main.lc.FirstOrDefault(c => c.ClientID == op.ClientID.Value);

                if (operationClient != null)
                {
                    SelectedClient = operationClient;
                    UpdateClientFields();
                }
            }

            AddOperation(op);

            if (OperationContainer != null)
            {
                CSingleOperation cSingleOperation = new CSingleOperation(this, null, op);
                OperationContainer.Children.Add(cSingleOperation);
            }

            if (Remise != null)
            {
                Remise.Text = op.Remise.ToString("0.00");
            }
        }

        public void AddOperation(Operation op)
        {
            if (SelectedOperations.Any(o => o.OperationID == op.OperationID))
            {
                MessageBox.Show(
                    "Cette opération a déjà été ajoutée !",
                    "Opération dupliquée",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SelectedOperations.Add(op);
            LoadArticlesFromOperation(op);

            if (txtTotalAmount != null && txtTVAAmount != null)
            {
                RecalculateTotals();
            }
        }

        public void RemoveOperation(Operation op)
        {
            SelectedOperations.RemoveAll(o => o.OperationID == op.OperationID);
            InvoiceArticles.RemoveAll(ia => ia.OperationID == op.OperationID);

            string invoiceType = InvoiceType.ToLower();
            if (invoiceType != "credit" && invoiceType != "cheque")
            {
                RecalculateTotals();
            }
        }

        private void LoadArticlesFromOperation(Operation op)
        {
            if (main?.loa == null || main?.la == null)
                return;

            foreach (OperationArticle oa in main.loa)
            {
                if (oa.OperationID == op.OperationID)
                {
                    var article = main.la.FirstOrDefault(a => a.ArticleID == oa.ArticleID);
                    if (article != null)
                    {
                        var existingArticle = InvoiceArticles.FirstOrDefault(ia =>
                            ia.OperationID == op.OperationID &&
                            ia.ArticleID == article.ArticleID);

                        if (existingArticle != null)
                            continue;

                        decimal quantity = oa.QteArticle;
                        InvoiceArticle invoiceArticle = new InvoiceArticle
                        {
                            OperationID = op.OperationID,
                            ArticleID = article.ArticleID,
                            ArticleName = article.ArticleName,
                            Prix = article.PrixVente,
                            Quantite = quantity,
                            TVA = article.tva,
                            Reversed = oa.Reversed,
                            InitialQuantity = oa.QteArticle
                        };

                        InvoiceArticles.Add(invoiceArticle);
                    }
                }
            }
        }

        private void AddOrMergeArticle(InvoiceArticle newArticle, bool showMessage)
        {
            // Find if same article exists (same ID, name, price, TVA, reversed status)
            var existingArticle = InvoiceArticles.FirstOrDefault(ia =>
                ia.ArticleID == newArticle.ArticleID &&
                ia.ArticleName == newArticle.ArticleName &&
                ia.Prix == newArticle.Prix &&
                ia.TVA == newArticle.TVA &&
                ia.Reversed == newArticle.Reversed);

            if (existingArticle != null)
            {
                // Merge quantities
                existingArticle.Quantite += newArticle.Quantite;
                existingArticle.InitialQuantity += newArticle.InitialQuantity;
            }
            else
            {
                // Add new article
                InvoiceArticles.Add(newArticle);
            }
        }

        // Public method to add articles directly
        public void AddArticlesToInvoice(List<InvoiceArticle> articles, bool showMessages = true)
        {
            if (InvoiceArticles == null)
            {
                InvoiceArticles = new List<InvoiceArticle>();
            }

            foreach (var article in articles)
            {
                // For expedition invoices, handle differently
                if (InvoiceType == "Expedition")
                {
                    // For expedition, find exact match (OperationID + ArticleID)
                    var existingArticle = InvoiceArticles.FirstOrDefault(a =>
                        a.OperationID == article.OperationID &&
                        a.ArticleID == article.ArticleID);

                    if (existingArticle != null)
                    {
                        // Update existing article
                        existingArticle.Quantite = article.Quantite;
                    }
                    else
                    {
                        // Add new article
                        InvoiceArticles.Add(article);
                    }
                }
                else
                {
                    // For regular invoices, merge articles
                    AddOrMergeArticle(article, showMessages);
                }
            }

            RecalculateTotals();
        }

        // Method to update article quantity for expedition
        public bool UpdateArticleQuantityForExpedition(int operationId, int articleId, decimal expeditionQuantity)
        {
            // For expedition invoices, find article by OperationID and ArticleID
            var invoiceArticle = InvoiceArticles.FirstOrDefault(ia =>
                ia.OperationID == operationId &&
                ia.ArticleID == articleId);

            if (invoiceArticle != null)
            {
                // Always save the quantity, even if 0
                invoiceArticle.Quantite = expeditionQuantity;
                RecalculateTotals();
                return true;
            }

            return false;
        }

        public void UpdateArticleQuantity(int operationId, int articleId, decimal newQuantity)
        {
            if (InvoiceType == "Expedition")
            {
                // For expedition invoices, update specific article
                UpdateArticleQuantityForExpedition(operationId, articleId, newQuantity);
            }
            else
            {
                // For regular invoices, this is more complex since articles are merged
                var similarArticles = InvoiceArticles
                    .Where(ia => ia.ArticleID == articleId)
                    .ToList();

                if (similarArticles.Count > 0)
                {
                    // This is a simplified approach
                    var articleToUpdate = similarArticles.First();
                    articleToUpdate.Quantite = newQuantity;

                    // Remove other similar articles (they should have been merged)
                    for (int i = 1; i < similarArticles.Count; i++)
                    {
                        InvoiceArticles.Remove(similarArticles[i]);
                    }

                    RecalculateTotals();
                }
            }
        }

        // **MODIFIED: Removed all reversed-based filtering logic - now calculates all articles regardless of reversed status**
        public void RecalculateTotals()
        {
            // Check if UI elements are initialized
            if (txtTotalAmount == null || txtTVAAmount == null || txtApresTVAAmount == null ||
                txtApresRemiseAmount == null || txtTVARate == null)
                return;

            // Ignore recalculation for Credit invoices - they manage totals manually
            string invoiceType = InvoiceType.ToLower();
            if (invoiceType == "credit" || invoiceType == "cheque")
            {
                System.Diagnostics.Debug.WriteLine("Recalcul ignoré pour facture Crédit/Chèque");
                return;
            }

            // Calculate using only articles with quantity > 0
            var articlesForCalculation = InvoiceArticles.Where(ia => ia.Quantite > 0).ToList();

            // **MODIFIED: Calculate all articles together, ignore reversed status**
            decimal totalHT = 0;
            decimal totalTVA = 0;

            foreach (var invoiceArticle in articlesForCalculation)
            {
                totalHT += invoiceArticle.TotalHT;
                totalTVA += invoiceArticle.MontantTVA;
            }

            // Enable Remise only when there are articles
            if (Remise != null)
            {
                if (articlesForCalculation.Count > 0)
                {
                    Remise.IsEnabled = true;
                }
                else
                {
                    Remise.IsEnabled = false;
                    Remise.Text = "";
                }
            }

            // Get discount value
            decimal remiseValue = 0;
            if (Remise != null && !string.IsNullOrWhiteSpace(Remise.Text))
            {
                string cleanedRemise = CleanNumericInput(Remise.Text);
                decimal.TryParse(cleanedRemise, out remiseValue);
            }

            // **MODIFIED: Simple calculation without reversed-based logic**
            currentTotalHT = totalHT;

            // Validate remise against total
            if (remiseValue > totalHT)
            {
                remiseValue = 0;
                if (Remise != null)
                {
                    Remise.TextChanged -= Remise_TextChanged;
                    Remise.Text = "";
                    Remise.TextChanged += Remise_TextChanged;
                }
            }

            decimal totalAfterRemise = totalHT - remiseValue;
            decimal tvaAfterRemise = totalHT > 0 ? (totalTVA / totalHT) * totalAfterRemise : 0;

            txtTotalAmount.Text = totalHT.ToString("0.00") + " DH";
            txtTVAAmount.Text = tvaAfterRemise.ToString("0.00") + " DH";
            txtApresTVAAmount.Text = (totalHT + tvaAfterRemise).ToString("0.00") + " DH";
            txtApresRemiseAmount.Text = (totalAfterRemise + tvaAfterRemise).ToString("0.00") + " DH";

            decimal tvaPercentage = totalHT > 0 ? (totalTVA / totalHT) * 100 : 0;
            txtTVARate.Text = tvaPercentage.ToString("0.00");

            // Update amount in letters after recalculating totals
            UpdateAmountInLetters();
        }

        // Get filtered articles for WFacturePage (exclude articles with quantity 0)
        public List<InvoiceArticle> GetFilteredInvoiceArticles()
        {
            // Return only articles with quantity > 0 for display
            return InvoiceArticles.Where(ia => ia.Quantite > 0).ToList();
        }

        // Get article by OperationID and ArticleID (for expedition)
        public InvoiceArticle GetArticleForExpedition(int operationId, int articleId)
        {
            return InvoiceArticles.FirstOrDefault(ia =>
                ia.OperationID == operationId &&
                ia.ArticleID == articleId);
        }

        public async Task LoadFacture()
        {
            try
            {
                Facture facturee = await new Facture().GetFactureAsync();
                txtUserName.Text = facturee.Name ?? "";
                txtUserICE.Text = facturee.ICE ?? "";
                txtUserVAT.Text = facturee.VAT ?? "";
                txtUserPhone.Text = facturee.Telephone ?? "";
                txtUserAddress.Text = facturee.Adresse ?? "";
                txtClientIdSociete.Text = facturee.CompanyId ?? "";
                txtClientEtatJuridique.Text = facturee.EtatJuridic ?? "";
                cmbClientSiegeEntreprise.Text = facturee.SiegeEntreprise ?? "";
                txtLogoPath.Text = facturee.LogoPath ?? "";

                // New fields
                txtUserIF.Text = facturee.IF ?? "";
                txtUserCNSS.Text = facturee.CNSS ?? "";
                txtUserRC.Text = facturee.RC ?? "";
                txtUserTP.Text = facturee.TP ?? "";
                txtUserRIB.Text = facturee.RIB ?? "";
                txtUserEmail.Text = facturee.Email ?? "";
                txtUserSiteWeb.Text = facturee.SiteWeb ?? "";
                txtUserPatente.Text = facturee.Patente ?? "";
                txtUserCapital.Text = facturee.Capital ?? "";
                txtUserFax.Text = facturee.Fax ?? "";
                txtUserVille.Text = facturee.Ville ?? "";
                txtUserCodePostal.Text = facturee.CodePostal ?? "";
                txtUserBankName.Text = facturee.BankName ?? "";
                txtUserAgencyCode.Text = facturee.AgencyCode ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading facture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSelectClient_Click(object sender, RoutedEventArgs e)
        {
            WSelectClient wSelectClient = new WSelectClient(this);
            wSelectClient.ShowDialog();
        }

        private void btnSelectOperation_Click(object sender, RoutedEventArgs e)
        {
            WSelectOperation wSelectOperation = new WSelectOperation(this);
            wSelectOperation.ShowDialog();
        }

        private void txtTotalAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtTotalAmount == null || txtTVARate == null || txtTVAAmount == null || txtApresTVAAmount == null)
                return;

            if (string.IsNullOrWhiteSpace(txtTotalAmount.Text) || string.IsNullOrWhiteSpace(txtTVARate.Text))
                return;

            try
            {
                string cleanedTotal = CleanNumericInput(txtTotalAmount.Text);
                string cleanedTVA = CleanNumericInput(txtTVARate.Text);

                if (!decimal.TryParse(cleanedTotal, out decimal total))
                    return;

                if (!decimal.TryParse(cleanedTVA, out decimal tvaRate))
                    return;

                currentTotalHT = total;

                decimal remiseValue = 0;
                if (Remise != null && !string.IsNullOrWhiteSpace(Remise.Text))
                {
                    string cleanedRemise = CleanNumericInput(Remise.Text);
                    decimal.TryParse(cleanedRemise, out remiseValue);
                }

                decimal totalAfterRemise = total - remiseValue;
                decimal tvaMultiplier = tvaRate * 0.01m;
                decimal tvaAmount = totalAfterRemise * tvaMultiplier;
                decimal totalWithTVA = totalAfterRemise + tvaAmount;

                txtTVAAmount.Text = tvaAmount.ToString("0.00") + " DH";
                txtApresTVAAmount.Text = (total + tvaAmount).ToString("0.00") + " DH";

                if (txtApresRemiseAmount != null)
                {
                    txtApresRemiseAmount.Text = totalWithTVA.ToString("0.00") + " DH";
                }

                UpdateAmountInLetters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans txtTotalAmount_TextChanged: {ex.Message}");
            }
        }

        private void txtTVARate_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
                return;

            string cleanedText = CleanNumericInput(textBox.Text);

            if (!decimal.TryParse(cleanedText, out decimal tvaRate))
                return;

            if (tvaRate > 100)
            {
                if (textBox.Text.Length > 0)
                {
                    int caretPosition = textBox.CaretIndex;
                    textBox.Text = textBox.Text.Remove(textBox.Text.Length - 1);
                    textBox.CaretIndex = Math.Min(caretPosition, textBox.Text.Length);
                }
                return;
            }

            if (txtTotalAmount == null || string.IsNullOrWhiteSpace(txtTotalAmount.Text))
                return;

            string cleanedTotal = CleanNumericInput(txtTotalAmount.Text);
            if (!decimal.TryParse(cleanedTotal, out decimal total))
                return;

            decimal remiseValue = 0;
            if (Remise != null && !string.IsNullOrWhiteSpace(Remise.Text))
            {
                string cleanedRemise = CleanNumericInput(Remise.Text);
                decimal.TryParse(cleanedRemise, out remiseValue);
            }

            decimal totalAfterRemise = total - remiseValue;
            decimal tvaMultiplier = tvaRate * 0.01m;
            decimal tvaAmount = totalAfterRemise * tvaMultiplier;
            decimal totalWithTVA = totalAfterRemise + tvaAmount;

            if (txtTVAAmount != null)
                txtTVAAmount.Text = tvaAmount.ToString("0.00") + " DH";

            if (txtApresTVAAmount != null)
                txtApresTVAAmount.Text = (total + tvaAmount).ToString("0.00") + " DH";

            if (txtApresRemiseAmount != null)
                txtApresRemiseAmount.Text = totalWithTVA.ToString("0.00") + " DH";

            UpdateAmountInLetters();
        }

        private void Remise_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (textBox == null)
                return;

            // If empty, reset to 0 and recalculate
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (txtTotalAmount != null && txtTVAAmount != null && txtApresRemiseAmount != null)
                {
                    // Recalculate without discount
                    RecalculateWithRemise(0);
                }
                return;
            }

            string cleanedText = CleanNumericInput(textBox.Text);

            if (!decimal.TryParse(cleanedText, out decimal remiseValue))
                return;

            // Get the current total HT
            decimal totalHT = 0;
            if (txtTotalAmount != null && !string.IsNullOrWhiteSpace(txtTotalAmount.Text))
            {
                string cleanedTotal = CleanNumericInput(txtTotalAmount.Text);
                decimal.TryParse(cleanedTotal, out totalHT);
            }

            // Validate: remise cannot be greater than total
            if (remiseValue > totalHT)
            {
                // Remove the last character and reset caret position
                if (textBox.Text.Length > 0)
                {
                    int caretPosition = textBox.CaretIndex;
                    textBox.TextChanged -= Remise_TextChanged;
                    textBox.Text = textBox.Text.Remove(textBox.Text.Length - 1);
                    textBox.CaretIndex = Math.Min(caretPosition, textBox.Text.Length);
                    textBox.TextChanged += Remise_TextChanged;
                }

                MessageBox.Show(
                    $"La remise ne peut pas dépasser le montant total ({totalHT:0.00} DH).",
                    "Remise invalide",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Recalculate with the new discount
            RecalculateWithRemise(remiseValue);
        }
        
        private void RecalculateWithRemise(decimal remiseValue)
        {
            if (txtTotalAmount == null || txtTVARate == null || txtTVAAmount == null ||
                txtApresTVAAmount == null || txtApresRemiseAmount == null)
                return;

            try
            {
                // Get total HT
                string cleanedTotal = CleanNumericInput(txtTotalAmount.Text);
                if (!decimal.TryParse(cleanedTotal, out decimal totalHT))
                    return;

                // Get TVA rate
                string cleanedTVA = CleanNumericInput(txtTVARate.Text);
                if (!decimal.TryParse(cleanedTVA, out decimal tvaRate))
                    tvaRate = 0;

                // Calculate total after discount
                decimal totalAfterRemise = totalHT - remiseValue;

                // Calculate TVA on the discounted amount
                decimal tvaMultiplier = tvaRate * 0.01m;
                decimal tvaAmount = totalAfterRemise * tvaMultiplier;

                // Calculate final total with TVA
                decimal totalWithTVA = totalAfterRemise + tvaAmount;

                // Update all fields
                txtTVAAmount.Text = tvaAmount.ToString("0.00") + " DH";
                txtApresTVAAmount.Text = (totalHT + tvaAmount).ToString("0.00") + " DH";
                txtApresRemiseAmount.Text = totalWithTVA.ToString("0.00") + " DH";

                // Update amount in letters
                UpdateAmountInLetters();

                System.Diagnostics.Debug.WriteLine($"Recalculated with Remise: {remiseValue:0.00}");
                System.Diagnostics.Debug.WriteLine($"  Total HT: {totalHT:0.00}");
                System.Diagnostics.Debug.WriteLine($"  After Remise: {totalAfterRemise:0.00}");
                System.Diagnostics.Debug.WriteLine($"  TVA Amount: {tvaAmount:0.00}");
                System.Diagnostics.Debug.WriteLine($"  Final Total: {totalWithTVA:0.00}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RecalculateWithRemise: {ex.Message}");
            }
        }

        private string CleanNumericInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "0";

            string cleaned = new string(input.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            cleaned = cleaned.Replace(',', '.');

            int firstDecimalIndex = cleaned.IndexOf('.');
            if (firstDecimalIndex != -1)
            {
                cleaned = cleaned.Substring(0, firstDecimalIndex + 1) +
                          cleaned.Substring(firstDecimalIndex + 1).Replace(".", "");
            }

            return string.IsNullOrWhiteSpace(cleaned) ? "0" : cleaned;
        }

        private void IntegerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
            {
                e.Handled = true;
                return;
            }

            if (e.Text == "." || e.Text == ",")
            {
                e.Handled = textBox.Text.Contains(".") || textBox.Text.Contains(",");
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
                var textBox = sender as TextBox;

                int dotCount = text.Count(c => c == '.' || c == ',');
                bool hasExistingDot = textBox?.Text.Contains(".") == true || textBox?.Text.Contains(",") == true;

                if ((dotCount > 1) || (dotCount == 1 && hasExistingDot) || text.Any(c => !char.IsDigit(c) && c != '.' && c != ','))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Facture facture = new Facture
                {
                    Name = txtUserName.Text,
                    ICE = txtUserICE.Text,
                    VAT = txtUserVAT.Text,
                    Telephone = txtUserPhone.Text,
                    Adresse = txtUserAddress.Text,
                    CompanyId = txtClientIdSociete.Text,
                    EtatJuridic = txtClientEtatJuridique.Text,
                    SiegeEntreprise = cmbClientSiegeEntreprise.Text,
                    LogoPath = txtLogoPath.Text,

                    // Missing fields - add these:
                    IF = txtUserIF.Text,
                    CNSS = txtUserCNSS.Text,
                    RC = txtUserRC.Text,
                    TP = txtUserTP.Text,
                    RIB = txtUserRIB.Text,
                    Email = txtUserEmail.Text,
                    SiteWeb = txtUserSiteWeb.Text,
                    Patente = txtUserPatente.Text,
                    Capital = txtUserCapital.Text,
                    Fax = txtUserFax.Text,
                    Ville = txtUserVille.Text,
                    CodePostal = txtUserCodePostal.Text,
                    BankName = txtUserBankName.Text,
                    AgencyCode = txtUserAgencyCode.Text,
                };

                await facture.InsertOrUpdateFactureAsync();
                WCongratulations wCongratulations = new WCongratulations("Informations saved successfully!", "", 1);
                wCongratulations.ShowDialog();
            }
            catch (Exception ex)
            {
                WCongratulations wCongratulations = new WCongratulations($"Error: {ex.Message}\r\nInformations not saved!", "", 0);
                wCongratulations.ShowDialog();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            main.load_main(u);
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

        // ──────────────────────────────────────────────────────────────────────
        // FRENCH TRANSLATION HELPERS
        // These translate combobox-selected values to French for WFacturePage.
        // The comboboxes themselves stay in the app language (EN/AR); only the
        // data passed to the invoice preview is forced to French.
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the French invoice-type label based on the combobox index,
        /// which is language-independent (EN or AR items, same order).
        /// Indices match cmbInvoiceType in CMainFa.xaml:
        ///   0 Invoice | 1 Quote | 2 ProForma | 3 Shipment | 4 Credit | 5 PurchaseOrder | 6 DeliveryNote
        /// </summary>
        private static string GetFrenchInvoiceType(int index)
        {
            switch (index)
            {
                case 0: return "Facture";
                case 1: return "Devis";
                case 2: return "Pro Forma";
                case 3: return "Expédition";
                case 4: return "Avoir";
                case 5: return "Bon de Commande";
                case 6: return "Bon de Livraison";
                default: return "Facture";
            }
        }

        /// <summary>
        /// Returns the French invoice-status label based on the EtatFacture combobox index.
        ///   0 Normal | 1 Cancelled
        /// </summary>
        private static string GetFrenchInvoiceStatus(int index)
        {
            switch (index)
            {
                case 0: return "Normal";
                case 1: return "Annulé";
                default: return "Normal";
            }
        }

        /// <summary>
        /// Translates a payment-method name (stored in DB, possibly in EN/AR) to French.
        /// Falls back to the original value if no mapping is found, so custom DB names
        /// that are already in French are returned as-is.
        /// </summary>
        private static string TranslatePaymentMethodToFrench(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // English → French
                { "Cash",               "Espèces" },
                { "Check",              "Chèque" },
                { "Cheque",             "Chèque" },
                { "Bank Transfer",      "Virement Bancaire" },
                { "Wire Transfer",      "Virement Bancaire" },
                { "Transfer",           "Virement" },
                { "Credit Card",        "Carte Bancaire" },
                { "Debit Card",         "Carte Bancaire" },
                { "Bank Card",          "Carte Bancaire" },
                { "Mobile Payment",     "Paiement Mobile" },
                { "Online Payment",     "Paiement en Ligne" },

                // Arabic → French
                { "نقدا",               "Espèces" },
                { "نقد",                "Espèces" },
                { "شيك",                "Chèque" },
                { "تحويل بنكي",         "Virement Bancaire" },
                { "بطاقة بنكية",        "Carte Bancaire" },
                { "دفع عبر الهاتف",    "Paiement Mobile" },
            };

            return map.TryGetValue(value.Trim(), out string french) ? french : value;
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            if (OperationContainer.Children.Count == 0)
            {
                MessageBox.Show("il ya pas aucun article ou operatiob selectione", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (InvoiceArticles != null && InvoiceArticles.Count > 0)
            {
                bool allQuantitiesZero = InvoiceArticles.All(ia => ia.Quantite == 0);

                if (allQuantitiesZero)
                {
                    MessageBox.Show("Tous les articles ont une quantité de 0. Veuillez ajouter au moins un article avec une quantité supérieure à 0.",
                        "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            try
            {
                string dateValue =
    (dpInvoiceDate?.SelectedDate?.Date ?? DateTime.Now.Date)
    .ToString("dd/MM/yy", CultureInfo.InvariantCulture);

                // --- Combobox values translated to French for the invoice preview ---
                // We use the selected INDEX (language-independent) for fixed-item comboboxes,
                // so the facture always shows French regardless of app language (EN or AR).
                string frenchInvoiceType   = GetFrenchInvoiceType(cmbInvoiceType?.SelectedIndex ?? 0);
                string frenchInvoiceStatus = GetFrenchInvoiceStatus(EtatFacture?.SelectedIndex ?? 0);

                string rawPaymentMethod = (cmbPaymentMethod?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                string paymentMethod = TranslatePaymentMethodToFrench(rawPaymentMethod);

                string chequeReference = (rawPaymentMethod.ToLower() == "cheque" && txtChequeReference != null)
                    ? txtChequeReference.Text : "";

                // Calculate total from articles
                decimal totalAmount = 0;
                if (InvoiceArticles != null && InvoiceArticles.Count > 0)
                {
                    totalAmount = InvoiceArticles.Sum(ia => ia.TotalTTC);
                }

                // Get credit information
                string creditClientName = "";
                string creditMontant = "";
                string creditRest = "";

                // Keep the app-language invoiceType for internal logic (Credit/Cheque checks etc.)
                // but use frenchInvoiceType when storing in FactureInfo for display.
                string invoiceType = cmbInvoiceType?.Text ?? "";

                if (invoiceType.ToLower() == "credit" && SelectedOperations.Count > 0)
                {
                    var operation = SelectedOperations.First();

                    // Get client name from SelectedClient or from text box
                    if (SelectedClient != null)
                    {
                        creditClientName = GetClientName(SelectedClient);
                    }
                    else if (!string.IsNullOrEmpty(txtClientName?.Text))
                    {
                        creditClientName = txtClientName.Text;
                    }

                    if (!string.IsNullOrEmpty(txtApresTVAAmount?.Text))
                    {
                        string cleanAmount = txtApresTVAAmount.Text.Replace("DH", "").Replace(" ", "").Trim();
                        if (decimal.TryParse(cleanAmount, out decimal parsedAmount))
                        {
                            creditMontant = parsedAmount.ToString("0.00") + " DH";
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Credit Montant: {creditMontant}");
                }

                Dictionary<string, string> FactureInfo = new Dictionary<string, string>()
        {
            { "NFacture", txtInvoiceNumber?.Text ?? "" },
            { "Date", dateValue },
            { "Type", frenchInvoiceType },
            { "NomU", txtUserName?.Text ?? "" },
            { "ICEU", txtUserICE?.Text ?? "" },
            { "VATU", txtUserVAT?.Text ?? "" },
            { "TelephoneU", txtUserPhone?.Text ?? "" },
            { "EtatJuridiqueU", txtUserEtatJuridique?.Text ?? "" },
            { "IdSocieteU", txtUserIdSociete?.Text ?? "" },
            { "SiegeEntrepriseU", cmbUserSiegeEntreprise?.Text ?? "" },
            { "AdressU", txtUserAddress?.Text ?? "" },
            { "NomC", txtClientName?.Text ?? "" },
            { "ICEC", txtClientICE?.Text ?? "" },
            { "VATC", txtClientVAT?.Text ?? "" },
            { "TelephoneC", txtClientPhone?.Text ?? "" },
            { "EtatJuridiqueC", txtClientEtatJuridique?.Text ?? "" },
            { "IdSocieteC", txtClientIdSociete?.Text ?? "" },
            { "SiegeEntrepriseC", cmbClientSiegeEntreprise?.Text ?? "" },
            { "AdressC", txtClientAddress?.Text ?? "" },
            { "EtatFature", frenchInvoiceStatus },
            { "Device", txtCurrency?.Text ?? "" },
            { "TVA", txtTVARate?.Text ?? "" },
            { "MontantTotal", txtTotalAmount?.Text ?? "" },
            { "MontantTVA", txtTVAAmount?.Text ?? "" },
            { "MontantApresTVA", txtApresTVAAmount?.Text ?? "" },
            { "MontantApresRemise", totalAmount.ToString("0.00") },
            { "IndexDeFacture", IndexFacture?.Text ?? "" },
            { "Description", txtDescription?.Text ?? "" },
            { "Logo", txtLogoPath?.Text ?? "" },
            { "Reversed", frenchInvoiceStatus },
            { "Remise", Remise?.Text ?? "" },
            { "Object", txtObject?.Text ?? "" },
            { "PaymentMethod", paymentMethod },
            { "AmountInLetters", txtAmountInLetters?.Text ?? "" },
            { "GivenBy", txtGivenBy?.Text ?? "" },
            { "ReceivedBy", txtReceivedBy?.Text ?? "" },
            { "ChequeReference", chequeReference },
            { "ClientReference", txtClientReference?.Text ?? "" },
            { "CreditClientName", creditClientName },
            { "CreditMontant", creditMontant },
            { "CreditRest", creditRest },
            { "IFU",                txtUserIF?.Text          ?? "" },
            { "CNSS_U",             txtUserCNSS?.Text        ?? "" },
            { "RC_U",               txtUserRC?.Text          ?? "" },
            { "TP_U",               txtUserTP?.Text          ?? "" },
            { "RIB_U",              txtUserRIB?.Text         ?? "" },
            { "EmailU",             txtUserEmail?.Text       ?? "" },
            { "SiteWebU",           txtUserSiteWeb?.Text     ?? "" },
            { "PatenteU",           txtUserPatente?.Text     ?? "" },
            { "CapitalU",           txtUserCapital?.Text     ?? "" },
            { "FaxU",               txtUserFax?.Text         ?? "" },
            { "VilleU",             txtUserVille?.Text       ?? "" },
            { "CodePostalU",        txtUserCodePostal?.Text  ?? "" },
            { "BankNameU",          txtUserBankName?.Text    ?? "" },
            { "AgencyCodeU",        txtUserAgencyCode?.Text  ?? "" },
        };

                // For credit/cheque invoices, ensure we have client info and amount in letters
                if ((invoiceType == "Credit" || invoiceType == "Cheque") && SelectedClient != null)
                {
                    // Update client information from selected client
                    FactureInfo["NomC"] = GetClientName(SelectedClient);
                    FactureInfo["AdressC"] = GetClientAddress(SelectedClient);
                    FactureInfo["VATC"] = GetClientVAT(SelectedClient);
                    FactureInfo["TelephoneC"] = GetClientTelephone(SelectedClient);
                    FactureInfo["ICEC"] = GetClientICE(SelectedClient);

                    // Convert amount to Arabic letters for credit/cheque
                    FactureInfo["AmountInLetters"] = ConvertToArabicLetters(totalAmount);

                    // Set GivenBy to current user
                    if (u != null)
                    {
                        FactureInfo["GivenBy"] = u.UserName ?? "";
                    }
                }
                WFacturePage wFacturePage = new WFacturePage(this, FactureInfo, GetFilteredInvoiceArticles());
                wFacturePage.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtInvoiceNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void btnGenerateInvoiceNumber_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string year = DateTime.Now.Year.ToString();
                string month = DateTime.Now.Month.ToString("D2");
                string day = DateTime.Now.Day.ToString("D2");

                Random random = new Random();
                int randomNumber = random.Next(10000, 99999);

                string invoiceNumber = $"{year}{month}{day}-{randomNumber}";

                txtInvoiceNumber.Text = invoiceNumber;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating invoice number: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EtatFacture_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Invoice status is now just a user choice - no special logic needed
            // This event handler is kept for potential future use
        }

        private void cmbInvoiceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            OperationContainer?.Children.Clear();
            SelectedOperations.Clear();
            InvoiceArticles.Clear();

            // Reset all totals when changing invoice type
            if (txtTotalAmount != null) txtTotalAmount.Text = "0.00 DH";
            if (txtTVAAmount != null) txtTVAAmount.Text = "0.00 DH";
            if (txtApresTVAAmount != null) txtApresTVAAmount.Text = "0.00 DH";
            if (txtApresRemiseAmount != null) txtApresRemiseAmount.Text = "0.00 DH";
            if (txtTVARate != null) txtTVARate.Text = "0.00";

            if (comboBox == null || comboBox.SelectedItem == null)
                return;

            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                InvoiceType = selectedItem.Content.ToString();
            }
        }

        // Handle payment method change to show/hide cheque reference
        private void cmbPaymentMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPaymentMethod == null || txtChequeReference == null || lblChequeReference == null)
                return;

            ComboBoxItem selectedItem = cmbPaymentMethod.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Content.ToString().ToLower() == "cheque")
            {
                txtChequeReference.Visibility = Visibility.Visible;
                lblChequeReference.Visibility = Visibility.Visible;
            }
            else
            {
                txtChequeReference.Visibility = Visibility.Collapsed;
                lblChequeReference.Visibility = Visibility.Collapsed;
                txtChequeReference.Text = "";
            }
        }

        // Method to update or add article separately (used by other windows)
        public void UpdateOrAddArticleSeparate(int operationID, int articleID, string articleName,
            decimal quantity, decimal price, decimal tva, decimal initialQuantity, decimal expeditionTotal = 0)
        {
            if (InvoiceArticles == null)
            {
                InvoiceArticles = new List<InvoiceArticle>();
            }

            System.Diagnostics.Debug.WriteLine($"=== UpdateOrAddArticleSeparate called ===");
            System.Diagnostics.Debug.WriteLine($"  OperationID: {operationID}, ArticleID: {articleID}");
            System.Diagnostics.Debug.WriteLine($"  ArticleName: {articleName}, Quantity: {quantity}");
            System.Diagnostics.Debug.WriteLine($"  Price: {price}, TVA: {tva}, ExpeditionTotal: {expeditionTotal}");
            System.Diagnostics.Debug.WriteLine($"  InvoiceType: {InvoiceType}");

            var existingArticle = InvoiceArticles.FirstOrDefault(ia =>
                ia.OperationID == operationID &&
                ia.ArticleID == articleID);

            if (existingArticle != null)
            {
                existingArticle.Quantite = quantity;
                existingArticle.Prix = price;
                existingArticle.TVA = tva;
                existingArticle.ExpeditionTotal = expeditionTotal;

                System.Diagnostics.Debug.WriteLine($"  Updated existing article. New quantity: {quantity}");
            }
            else
            {
                // For Expedition invoices, keep track of ALL articles, even with 0 quantity
                if (InvoiceType == "Expedition" || quantity > 0)
                {
                    var newArticle = new InvoiceArticle
                    {
                        OperationID = operationID,
                        ArticleID = articleID,
                        ArticleName = articleName,
                        Quantite = quantity,
                        Prix = price,
                        TVA = tva,
                        InitialQuantity = initialQuantity,
                        ExpeditionTotal = expeditionTotal
                    };

                    InvoiceArticles.Add(newArticle);
                    System.Diagnostics.Debug.WriteLine($"  Added new article with quantity: {quantity}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  Skipped adding article (quantity = 0 and not Expedition type)");
                }
            }

            System.Diagnostics.Debug.WriteLine($"  Current InvoiceArticles count: {InvoiceArticles.Count}");
            foreach (var article in InvoiceArticles.Where(a => a.OperationID == operationID && a.ArticleID == articleID))
            {
                System.Diagnostics.Debug.WriteLine($"    - Found: {article.ArticleName}: Quantity={article.Quantite}, ExpeditionTotal={article.ExpeditionTotal}");
            }

            RecalculateTotals();
        }

        // New method to handle Expedition article updates specifically
        public void UpdateExpeditionArticle(int operationID, int articleID, string articleName,
            decimal quantity, decimal expeditionTotal, decimal price, decimal tva, decimal initialQuantity)
        {
            if (InvoiceArticles == null)
            {
                InvoiceArticles = new List<InvoiceArticle>();
            }

            var existingArticle = InvoiceArticles.FirstOrDefault(ia =>
                ia.OperationID == operationID &&
                ia.ArticleID == articleID);

            System.Diagnostics.Debug.WriteLine($"=== UpdateExpeditionArticle ===");
            System.Diagnostics.Debug.WriteLine($"  OperationID: {operationID}, ArticleID: {articleID}");
            System.Diagnostics.Debug.WriteLine($"  Quantity: {quantity}, ExpeditionTotal: {expeditionTotal}");

            if (existingArticle != null)
            {
                // Always update the quantity, even if it's 0
                existingArticle.Quantite = quantity;
                existingArticle.ExpeditionTotal = expeditionTotal;
                existingArticle.Prix = price;
                existingArticle.TVA = tva;
                System.Diagnostics.Debug.WriteLine($"  Updated existing article: {existingArticle.ArticleName} = {quantity}");
            }
            else
            {
                // Add new article - for Expedition, we add even with 0 quantity
                InvoiceArticles.Add(new InvoiceArticle
                {
                    OperationID = operationID,
                    ArticleID = articleID,
                    ArticleName = articleName,
                    Quantite = quantity,
                    Prix = price,
                    TVA = tva,
                    InitialQuantity = initialQuantity,
                    ExpeditionTotal = expeditionTotal
                });
                System.Diagnostics.Debug.WriteLine($"  Added new article: {articleName} = {quantity}");
            }

            RecalculateTotals();
        }

        private void MergeIdenticalArticles()
        {
            if (InvoiceArticles == null || InvoiceArticles.Count == 0)
                return;

            var mergedArticles = new List<InvoiceArticle>();
            var processedGroups = new HashSet<string>();

            foreach (var article in InvoiceArticles)
            {
                string groupKey = $"{article.ArticleID}_{article.Prix}_{article.TVA}_{article.Reversed}";

                if (processedGroups.Contains(groupKey))
                    continue;

                var similarArticles = InvoiceArticles
                    .Where(ia => ia.ArticleID == article.ArticleID &&
                                ia.Prix == article.Prix &&
                                ia.TVA == article.TVA &&
                                ia.Reversed == article.Reversed)
                    .ToList();

                if (similarArticles.Count > 1)
                {
                    var mergedArticle = new InvoiceArticle
                    {
                        OperationID = similarArticles.First().OperationID,
                        ArticleID = article.ArticleID,
                        ArticleName = article.ArticleName,
                        Prix = article.Prix,
                        TVA = article.TVA,
                        Reversed = article.Reversed,
                        Quantite = similarArticles.Sum(a => a.Quantite),
                        InitialQuantity = similarArticles.Sum(a => a.InitialQuantity)
                    };

                    mergedArticles.Add(mergedArticle);
                }
                else
                {
                    mergedArticles.Add(similarArticles.First());
                }

                processedGroups.Add(groupKey);
            }

            InvoiceArticles.Clear();
            InvoiceArticles.AddRange(mergedArticles);
        }
        
        private void UpdateAmountInLetters()
        {
            if (txtAmountInLetters == null || txtApresRemiseAmount == null)
                return;

            try
            {
                // Get the total amount after discount
                string amountText = txtApresRemiseAmount.Text.Replace("DH", "").Replace(" ", "").Trim();

                if (decimal.TryParse(amountText, out decimal amount))
                {
                    string amountInFrench = ConvertToFrenchLetters(amount);
                    txtAmountInLetters.Text = amountInFrench;

                    System.Diagnostics.Debug.WriteLine($"Amount in letters updated: {amountInFrench}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating amount in letters: {ex.Message}");
            }
        }
        
        // Method to handle adding manual articles
        public void AddManualArticle(InvoiceArticle article)
        {
            if (InvoiceArticles == null)
            {
                InvoiceArticles = new List<InvoiceArticle>();
            }

            InvoiceArticles.Add(article);

            CSingleArticle articleControl = new CSingleArticle(this, article);
            OperationContainer.Children.Add(articleControl);

            RecalculateTotals();
        }

        // Event handler for the Add Article button
        private void btnAddArticle_Click(object sender, RoutedEventArgs e)
        {
            WAddArticle wAddArticle = new WAddArticle(this);
            wAddArticle.ShowDialog();
        }
        
        public void ForceUpdateArticlesFromOperation(int operationId)
        {
            System.Diagnostics.Debug.WriteLine($"=== ForceUpdateArticlesFromOperation called for OperationID: {operationId} ===");

            // Recalculate totals
            RecalculateTotals();
        }

    }
}
