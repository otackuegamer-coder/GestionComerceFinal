using GestionComerce.Main.Facturation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestionComerce.Main.Facturation.FacturesEnregistrees
{
    // ════════════════════════════════════════════════════════════════════════════
    // VIEW-MODEL ROW
    // Wraps FactureEnregistreeArticle and adds INotifyPropertyChanged so the
    // computed TotalHT / TotalTTC columns refresh as the user types, and
    // IsDirty tracks unsaved changes without polluting the domain model.
    // ════════════════════════════════════════════════════════════════════════════
    public class ArticleRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        // ── backing fields ────────────────────────────────────────────────────
        private int _savedInvoiceArticleId;
        private int _savedInvoiceId;
        private int? _articleId;
        private string _articleName;
        private decimal _prixUnitaire;
        private decimal _quantite;
        private decimal _tva;
        private bool _isDirty;

        // ── properties ────────────────────────────────────────────────────────
        public int SavedInvoiceArticleId
        {
            get => _savedInvoiceArticleId;
            set { _savedInvoiceArticleId = value; Notify(nameof(SavedInvoiceArticleId)); }
        }

        public int SavedInvoiceId
        {
            get => _savedInvoiceId;
            set { _savedInvoiceId = value; Notify(nameof(SavedInvoiceId)); }
        }

        public int? ArticleId
        {
            get => _articleId;
            set { _articleId = value; Notify(nameof(ArticleId)); }
        }

        public string ArticleName
        {
            get => _articleName;
            set
            {
                if (_articleName == value) return;
                _articleName = value;
                Notify(nameof(ArticleName));
                MarkDirty();
            }
        }

        public decimal PrixUnitaire
        {
            get => _prixUnitaire;
            set
            {
                if (_prixUnitaire == value) return;
                _prixUnitaire = value;
                Notify(nameof(PrixUnitaire));
                Notify(nameof(TotalHT));
                Notify(nameof(MontantTVA));
                Notify(nameof(TotalTTC));
                MarkDirty();
            }
        }

        public decimal Quantite
        {
            get => _quantite;
            set
            {
                if (_quantite == value) return;
                _quantite = value;
                Notify(nameof(Quantite));
                Notify(nameof(TotalHT));
                Notify(nameof(MontantTVA));
                Notify(nameof(TotalTTC));
                MarkDirty();
            }
        }

        public decimal Tva
        {
            get => _tva;
            set
            {
                if (_tva == value) return;
                _tva = value;
                Notify(nameof(Tva));
                Notify(nameof(MontantTVA));
                Notify(nameof(TotalTTC));
                MarkDirty();
            }
        }

        // ── computed (read-only, recalculate automatically) ───────────────────
        public decimal TotalHT => PrixUnitaire * Quantite;
        public decimal MontantTVA => TotalHT * (Tva / 100m);
        public decimal TotalTTC => TotalHT + MontantTVA;

        // ── dirty tracking ────────────────────────────────────────────────────
        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty == value) return;
                _isDirty = value;
                Notify(nameof(IsDirty));
                Notify(nameof(SaveBtnVisibility));
            }
        }

        /// <summary>Drives the 💾 button — Visible only when the row has unsaved edits.</summary>
        public Visibility SaveBtnVisibility =>
            IsDirty ? Visibility.Visible : Visibility.Collapsed;

        public void MarkDirty() => IsDirty = true;
        public void MarkClean() => IsDirty = false;

        // ── factory ───────────────────────────────────────────────────────────
        public static ArticleRow FromModel(FactureEnregistreeArticle m) => new ArticleRow
        {
            _savedInvoiceArticleId = m.SavedInvoiceArticleId,
            _savedInvoiceId = m.SavedInvoiceId,
            _articleId = m.ArticleId,
            _articleName = m.ArticleName,
            _prixUnitaire = m.PrixUnitaire,
            _quantite = m.Quantite,
            _tva = m.Tva,
            _isDirty = false   // freshly loaded → clean
        };

        /// <summary>Projects back to the domain model for UpdateAsync() / DeleteAsync().</summary>
        public FactureEnregistreeArticle ToModel() => new FactureEnregistreeArticle
        {
            SavedInvoiceArticleId = SavedInvoiceArticleId,
            SavedInvoiceId = SavedInvoiceId,
            ArticleId = ArticleId,
            ArticleName = ArticleName,
            PrixUnitaire = PrixUnitaire,
            Quantite = Quantite,
            Tva = Tva
        };
    }

    // ════════════════════════════════════════════════════════════════════════════
    // WINDOW
    // ════════════════════════════════════════════════════════════════════════════
    public partial class WInvoiceArticles : Window
    {
        // ── public event so CMainEnregistrees can refresh its badge ─────────────
        public event EventHandler ArticlesChanged;

        // ── state ────────────────────────────────────────────────────────────────
        private readonly int _savedInvoiceId;
        private readonly string _invoiceReference;
        private ObservableCollection<ArticleRow> _rows = new ObservableCollection<ArticleRow>();

        // ── ctor ─────────────────────────────────────────────────────────────────
        public WInvoiceArticles(int savedInvoiceId, string invoiceReference)
        {
            InitializeComponent();
            _savedInvoiceId = savedInvoiceId;
            _invoiceReference = invoiceReference;

            lblTitle.Text = $"Articles — Facture {invoiceReference}";
            lblSubtitle.Text = $"ID: {savedInvoiceId}";

            dgArticles.ItemsSource = _rows;

            this.Loaded += async (_, __) => await LoadArticlesAsync();
        }

        // ════════════════════════════════════════════════════════════════════════
        // LOAD
        // ════════════════════════════════════════════════════════════════════════

        private async Task LoadArticlesAsync()
        {
            SetBusy(true);
            try
            {
                var models = await FactureEnregistreeArticle.GetBySavedInvoiceIdAsync(_savedInvoiceId);
                _rows.Clear();
                foreach (var m in models)
                    _rows.Add(ArticleRow.FromModel(m));

                RefreshTotals();
                RefreshBadge();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Erreur chargement: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CELL EDIT ENDING
        // The DataGrid binding hasn't flushed yet when CellEditEnding fires,
        // so we manually push the TextBox value to the source first.
        // ════════════════════════════════════════════════════════════════════════

        private void dgArticles_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (!(e.Row.Item is ArticleRow row)) return;

            if (e.EditingElement is TextBox tb)
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

            // MarkDirty() is already called inside the property setters,
            // but this ensures the row turns yellow even if the value is unchanged.
            row.MarkDirty();
            RefreshTotals();
        }

        // ════════════════════════════════════════════════════════════════════════
        // SAVE SINGLE ROW  (💾 button)
        // ════════════════════════════════════════════════════════════════════════

        private async void SaveArticle_Click(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)sender).DataContext is ArticleRow row)) return;

            if (string.IsNullOrWhiteSpace(row.ArticleName))
            {
                MessageBox.Show("La désignation ne peut pas être vide.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (row.Quantite <= 0)
            {
                MessageBox.Show("La quantité doit être supérieure à 0.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true);
            try
            {
                await row.ToModel().UpdateAsync();
                row.MarkClean();
                RefreshTotals();
                ArticlesChanged?.Invoke(this, EventArgs.Empty);
                lblStatus.Text = $"Article « {row.ArticleName} » sauvegardé.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // ADD ARTICLE  (form at top)
        // ════════════════════════════════════════════════════════════════════════

        private async void AddArticle_Click(object sender, RoutedEventArgs e)
        {
            string name = txtArtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Veuillez saisir la désignation de l'article.",
                    "Champ requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtArtName.Focus();
                return;
            }

            if (!decimal.TryParse(txtArtPrix.Text.Trim(), out decimal prix) || prix < 0)
            {
                MessageBox.Show("Veuillez saisir un prix unitaire valide.",
                    "Champ requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtArtPrix.Focus();
                return;
            }

            if (!decimal.TryParse(txtArtQte.Text.Trim(), out decimal qte) || qte <= 0)
            {
                MessageBox.Show("Veuillez saisir une quantité valide (> 0).",
                    "Champ requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtArtQte.Focus();
                return;
            }

            decimal.TryParse(txtArtTva.Text.Trim(), out decimal tva);

            SetBusy(true);
            try
            {
                var article = new FactureEnregistreeArticle
                {
                    SavedInvoiceId = _savedInvoiceId,
                    ArticleName = name,
                    PrixUnitaire = prix,
                    Quantite = qte,
                    Tva = tva
                };

                await article.InsertAsync();   // sets SavedInvoiceArticleId

                var newRow = ArticleRow.FromModel(article);
                _rows.Add(newRow);

                txtArtName.Clear();
                txtArtPrix.Clear();
                txtArtQte.Clear();
                txtArtTva.Text = "0";

                RefreshTotals();
                RefreshBadge();
                ArticlesChanged?.Invoke(this, EventArgs.Empty);
                lblStatus.Text = $"Article « {name} » ajouté.";

                dgArticles.ScrollIntoView(newRow);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // DELETE ARTICLE  (🗑 button)
        // ════════════════════════════════════════════════════════════════════════

        private async void DeleteArticle_Click(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)sender).DataContext is ArticleRow row)) return;

            if (MessageBox.Show(
                    $"Supprimer l'article « {row.ArticleName} » ?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            SetBusy(true);
            try
            {
                await row.ToModel().DeleteAsync();
                _rows.Remove(row);
                RefreshTotals();
                RefreshBadge();
                ArticlesChanged?.Invoke(this, EventArgs.Empty);
                lblStatus.Text = $"Article « {row.ArticleName} » supprimé.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur suppression: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════════════

        private void RefreshTotals()
        {
            decimal ht = _rows.Sum(r => r.TotalHT);
            decimal tva = _rows.Sum(r => r.MontantTVA);
            decimal ttc = _rows.Sum(r => r.TotalTTC);

            lblTotalHT.Text = ht.ToString("N2") + " DH";
            lblTotalTVA.Text = tva.ToString("N2") + " DH";
            lblTotalTTC.Text = ttc.ToString("N2") + " DH";
        }

        private void RefreshBadge()
        {
            int count = _rows.Count;
            lblArticleCount.Text = count == 0 ? "Aucun article"
                : count == 1 ? "1 article"
                                               : $"{count} articles";
        }

        private void SetBusy(bool busy)
        {
            btnAddArticle.IsEnabled = !busy;
            this.Cursor = busy ? Cursors.Wait : Cursors.Arrow;
        }

        private void Decimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;
            e.Handled = e.Text == "."
                ? (tb != null && tb.Text.Contains("."))
                : !e.Text.All(char.IsDigit);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}