using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestionComerce;

namespace GestionComerce.Main.Facturation.HistoriqueFacture
{
    // ── Row model for the staging DataGrid ──────────────────────────────────
    public class SelectedArticleRow : INotifyPropertyChanged
    {
        public int ArticleID { get; set; }

        private string _articleName;
        public string ArticleName
        {
            get => _articleName;
            set { _articleName = value; OnPropertyChanged(nameof(ArticleName)); }
        }

        private decimal _quantite = 1;
        public decimal Quantite
        {
            get => _quantite;
            set
            {
                _quantite = value;
                OnPropertyChanged(nameof(Quantite));
                OnPropertyChanged(nameof(TotalHT));
            }
        }

        private decimal _prixUnitaire;
        public decimal PrixUnitaire
        {
            get => _prixUnitaire;
            set
            {
                _prixUnitaire = value;
                OnPropertyChanged(nameof(PrixUnitaire));
                OnPropertyChanged(nameof(TotalHT));
            }
        }

        private decimal _tva;
        public decimal TVA
        {
            get => _tva;
            set { _tva = value; OnPropertyChanged(nameof(TVA)); }
        }

        public decimal TotalHT => PrixUnitaire * Quantite;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Output container returned to the caller ──────────────────────────────
    public class InvoiceArticleResult
    {
        public int ArticleID { get; set; }
        public string ArticleName { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal Quantite { get; set; }
        public decimal TVA { get; set; }
    }

    // ── Window code-behind ───────────────────────────────────────────────────
    public partial class WInvoiceArticlePicker : Window
    {
        // Output: list of all confirmed rows
        public List<InvoiceArticleResult> SelectedArticles { get; private set; } = new List<InvoiceArticleResult>();

        // Internal state
        private List<Article> _allArticles;
        private List<Article> _filtered;
        private ObservableCollection<SelectedArticleRow> _stagingRows = new ObservableCollection<SelectedArticleRow>();

        // Prevent re-entrancy while programmatically changing list selection
        private bool _suppressSelectionChange = false;

        public WInvoiceArticlePicker(List<Article> articles)
        {
            InitializeComponent();

            _allArticles = articles ?? new List<Article>();
            _filtered = _allArticles.ToList();

            dgSelectedArticles.ItemsSource = _stagingRows;
            _stagingRows.CollectionChanged += (s, e) => RefreshFooter();

            RefreshList();
        }

        // ── Search ───────────────────────────────────────────────────────────
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = txtSearch.Text.Trim().ToLower();

            _filtered = string.IsNullOrEmpty(q)
                ? _allArticles.ToList()
                : _allArticles.Where(a =>
                    (a.ArticleName ?? "").ToLower().Contains(q) ||
                    a.Code.ToString().Contains(q)).ToList();

            RefreshList();
        }

        // ── Catalogue selection ───────────────────────────────────────────────
        private void lstArticles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionChange) return;

            // Added items → add to staging grid (if not already there)
            foreach (Article added in e.AddedItems.OfType<Article>())
            {
                if (!_stagingRows.Any(r => r.ArticleID == added.ArticleID))
                {
                    _stagingRows.Add(new SelectedArticleRow
                    {
                        ArticleID = added.ArticleID,
                        ArticleName = added.ArticleName,
                        Quantite = 1,
                        PrixUnitaire = added.PrixVente,
                        TVA = added.tva
                    });
                }
            }

            // Removed items → remove from staging grid
            foreach (Article removed in e.RemovedItems.OfType<Article>())
            {
                var row = _stagingRows.FirstOrDefault(r => r.ArticleID == removed.ArticleID);
                if (row != null) _stagingRows.Remove(row);
            }

            RefreshFooter();
        }

        // ── DataGrid: inline edit finished ───────────────────────────────────
        private void dgSelectedArticles_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            // Force the binding to commit before we read TotalHT
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(RefreshFooter));
        }

        // ── Remove a row via the ✕ button ─────────────────────────────────────
        private void btnRemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SelectedArticleRow row)
            {
                _stagingRows.Remove(row);

                // Deselect the corresponding article in the catalogue list
                _suppressSelectionChange = true;
                var article = lstArticles.Items.OfType<Article>()
                                  .FirstOrDefault(a => a.ArticleID == row.ArticleID);
                if (article != null)
                    lstArticles.SelectedItems.Remove(article);
                _suppressSelectionChange = false;

                RefreshFooter();
            }
        }

        // ── Close / Cancel ────────────────────────────────────────────────────
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ── Confirm ───────────────────────────────────────────────────────────
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Commit any active cell edit before reading values
            dgSelectedArticles.CommitEdit(DataGridEditingUnit.Row, true);

            if (_stagingRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner au moins un article.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var errors = new List<string>();
            foreach (var row in _stagingRows)
            {
                if (row.Quantite <= 0)
                    errors.Add($"• {row.ArticleName} : quantité invalide ({row.Quantite})");
                if (row.PrixUnitaire < 0)
                    errors.Add($"• {row.ArticleName} : prix invalide ({row.PrixUnitaire})");
                if (row.TVA < 0)
                    errors.Add($"• {row.ArticleName} : TVA invalide ({row.TVA})");
            }

            if (errors.Count > 0)
            {
                MessageBox.Show("Corrigez les erreurs suivantes :\n\n" + string.Join("\n", errors),
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedArticles = _stagingRows.Select(r => new InvoiceArticleResult
            {
                ArticleID = r.ArticleID,
                ArticleName = r.ArticleName,
                PrixUnitaire = r.PrixUnitaire,
                Quantite = r.Quantite,
                TVA = r.TVA
            }).ToList();

            DialogResult = true;
            Close();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void RefreshList()
        {
            // Rebuild items source, then re-apply selections for articles already staged
            _suppressSelectionChange = true;
            lstArticles.ItemsSource = null;
            lstArticles.ItemsSource = _filtered;

            foreach (var article in _filtered.Where(a => _stagingRows.Any(r => r.ArticleID == a.ArticleID)))
                lstArticles.SelectedItems.Add(article);

            _suppressSelectionChange = false;

            RefreshFooter();
        }

        private void RefreshFooter()
        {
            int count = _stagingRows.Count;
            lblCount.Text = count.ToString();
            btnAdd.IsEnabled = count > 0;

            decimal total = _stagingRows.Sum(r => r.TotalHT);
            lblTotalHT.Text = total.ToString("N2", CultureInfo.GetCultureInfo("fr-FR")) + " DH";
        }
    }
}