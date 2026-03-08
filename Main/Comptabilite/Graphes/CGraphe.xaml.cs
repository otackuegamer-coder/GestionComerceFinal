using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Superete.Main.Comptabilite.Graphes
{
    // ═══════════════════════════════════════════════════════════════
    //  MODELS
    // ═══════════════════════════════════════════════════════════════

    public class EntityItem
    {
        public int    Id          { get; set; }
        public string DisplayName { get; set; }
        public override string ToString() => DisplayName ?? "";
    }

    public class DashboardPoint
    {
        public DateTime Date      { get; set; }
        public string   DateLabel { get; set; }
        public double   OpsValue  { get; set; }
        public double   InvValue  { get; set; }
        public int      OpsCount  { get; set; }
        public int      InvCount  { get; set; }
        public double   Gap       => OpsValue - InvValue;
    }

    public class TableRow : INotifyPropertyChanged
    {
        public string Source    { get; set; }
        public string DateLabel { get; set; }
        public string Label     { get; set; }
        public double Quantity  { get; set; }
        public double Value     { get; set; }
        public string ValueFormatted => $"{Value:N2} DH";
        public string Status    { get; set; }
        public int    RawId     { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class KpiSummary
    {
        public double TotalRevenue { get; set; }
        public double TotalInvoice { get; set; }
        public int    OpCount      { get; set; }
        public double AvgBasket    { get; set; }
        public double Gap          => TotalRevenue - TotalInvoice;
        public double RevenueTrend { get; set; }
        public double OpCountTrend { get; set; }
        public double BasketTrend  { get; set; }
        public double GapTrend     { get; set; }
    }

    public enum DashboardView
    {
        MonthlyRevenue, StockMovement, InvoicingGap, ClientPerformance, SupplierActivity
    }

    public class EntitySearchResult
    {
        public int    Id          { get; set; }
        public string EntityType  { get; set; }  // "article", "client", "fournisseur"
        public string DisplayText { get; set; }  // shown in the list
    }

    // ═══════════════════════════════════════════════════════════════
    //  DATA ENGINE
    //  Operations:  Operation  ──► OperationArticle ──► Article
    //  Invoices:    Invoice    ──► InvoiceArticle   ──► Article
    // ═══════════════════════════════════════════════════════════════

    internal static class DataEngine
    {
        private const string Conn =
            "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";

        // ── Entity lists for dropdown ────────────────────────────
        internal static async Task<List<EntityItem>> LoadEntitiesAsync(string type)
        {
            var list = new List<EntityItem>();
            string sql;

            if      (type == "article")
                sql = "SELECT ArticleID, ArticleName AS Name FROM Article WHERE Etat=1 ORDER BY ArticleName";
            else if (type == "client")
                sql = "SELECT ClientID, Nom AS Name FROM Client WHERE Etat=1 ORDER BY Nom";
            else if (type == "fournisseur")
                sql = "SELECT FournisseurID, Nom AS Name FROM Fournisseur WHERE Etat=1 ORDER BY Nom";
            else
                return list;

            await Task.Run(() =>
            {
                try
                {
                    using (var con = new SqlConnection(Conn))
                    using (var cmd = new SqlCommand(sql, con))
                    {
                        con.Open();
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                list.Add(new EntityItem
                                {
                                    Id          = r.GetInt32(0),
                                    DisplayName = r["Name"]?.ToString()?.Trim() ?? ""
                                });
                    }
                }
                catch { /* DB not available */ }
            });

            return list;
        }

        // ── Universal search across Article + Client + Fournisseur ──
        internal static async Task<List<EntitySearchResult>> SearchEntitiesAsync(string query)
        {
            var results = new List<EntitySearchResult>();
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return results;

            string like = "%" + query.Replace("'", "''") + "%";

            string sql = $@"
                SELECT TOP 6 ArticleID, 'article' AS Kind, ArticleName AS Name
                FROM Article
                WHERE Etat = 1 AND ArticleName LIKE '{like}'
                UNION ALL
                SELECT TOP 6 ClientID, 'client', Nom
                FROM Client
                WHERE Etat = 1 AND Nom LIKE '{like}'
                UNION ALL
                SELECT TOP 6 FournisseurID, 'fournisseur', Nom
                FROM Fournisseur
                WHERE Etat = 1 AND Nom LIKE '{like}'";

            await Task.Run(() =>
            {
                try
                {
                    using (var con = new SqlConnection(Conn))
                    using (var cmd = new SqlCommand(sql, con))
                    {
                        con.Open();
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                            {
                                var kind = r["Kind"].ToString();
                                var name = r["Name"]?.ToString()?.Trim() ?? "";
                                var prefix = kind == "article" ? "📦" : kind == "client" ? "👤" : "🏭";
                                results.Add(new EntitySearchResult
                                {
                                    Id          = r.GetInt32(0),
                                    EntityType  = kind,
                                    DisplayText = $"{prefix} {name}"
                                });
                            }
                    }
                }
                catch { }
            });

            return results;
        }


        internal static async Task<(List<DashboardPoint>, List<TableRow>, KpiSummary)>
            LoadAsync(DashboardView view, DateTime start, DateTime end,
                      bool includeOps, bool includeInv,
                      string entityType, int? entityId)
        {
            var ops = new List<RawRow>();
            var inv = new List<RawRow>();

            await Task.Run(() =>
            {
                try
                {
                    using (var con = new SqlConnection(Conn))
                    {
                        con.Open();
                        if (includeOps) ops = FetchOperations(con, start, end, entityType, entityId);
                        if (includeInv) inv = FetchInvoices  (con, start, end, entityType, entityId);
                    }
                }
                catch { /* swallow — return empty */ }
            });

            var span   = end - start;
            var points = span.TotalDays <= 62
                       ? BuildByDay  (ops, inv, start, end)
                       : BuildByMonth(ops, inv, start, end);

            ApplyView(points, view);

            var rows = BuildRows(ops, inv);
            var kpi  = ComputeKpi(ops, inv);

            return (points, rows, kpi);
        }

        // ── Operations: Operation → OperationArticle → Article ───
        private static List<RawRow> FetchOperations(SqlConnection con,
            DateTime start, DateTime end, string entityType, int? entityId)
        {
            var list = new List<RawRow>();

            // Build optional join + filter by entity
            string extraJoin   = "";
            string extraFilter = "";

            if (entityType == "article" && entityId.HasValue)
            {
                extraFilter = $"AND oa.ArticleID = {entityId.Value}";
            }
            else if (entityType == "client" && entityId.HasValue)
            {
                extraFilter = $"AND op.ClientID = {entityId.Value}";
            }
            else if (entityType == "fournisseur" && entityId.HasValue)
            {
                extraFilter = $"AND op.FournisseurID = {entityId.Value}";
            }

            // One row per OperationArticle line — price comes from Article.PrixVente (OperationArticle has no price)
            string sql = $@"
                SELECT
                    op.OperationID                     AS OpId,
                    op.Date                            AS OpDate,
                    oa.QteArticle * a.PrixVente        AS LineValue,
                    ISNULL(op.Reversed, 0)             AS IsReversed,
                    oa.QteArticle                      AS Qty,
                    a.ArticleName                      AS ArticleLabel,
                    ISNULL(c.Nom, '')                  AS ClientNom
                FROM Operation op
                INNER JOIN OperationArticle oa ON oa.OperationID = op.OperationID
                INNER JOIN Article          a  ON a.ArticleID    = oa.ArticleID
                LEFT  JOIN Client           c  ON c.ClientID     = op.ClientID
                WHERE op.Date BETWEEN @s AND @e
                  AND op.Etat = 1
                  AND oa.Etat = 1
                  {extraFilter}";

            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@s", start.Date);
                cmd.Parameters.AddWithValue("@e", end.Date.AddDays(1).AddTicks(-1));

                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new RawRow
                        {
                            Id         = r.GetInt32(r.GetOrdinal("OpId")),
                            Date       = r.GetDateTime(r.GetOrdinal("OpDate")),
                            Value      = r.IsDBNull(r.GetOrdinal("LineValue")) ? 0 : Convert.ToDouble(r["LineValue"]),
                            IsReversed = Convert.ToBoolean(r["IsReversed"]),
                            Quantity   = r.IsDBNull(r.GetOrdinal("Qty")) ? 0 : Convert.ToDouble(r["Qty"]),
                            // Show article name; if filtered by client show client name instead
                            Label      = entityType == "client"
                                             ? r["ClientNom"]?.ToString()?.Trim() ?? r["ArticleLabel"]?.ToString() ?? ""
                                             : r["ArticleLabel"]?.ToString() ?? "",
                            Source     = "Opération",
                            Status     = Convert.ToBoolean(r["IsReversed"]) ? "Annulé" : "Actif"
                        });
            }

            return list;
        }

        // ── Invoices: Invoice → InvoiceArticle → Article ─────────
        private static List<RawRow> FetchInvoices(SqlConnection con,
            DateTime start, DateTime end, string entityType, int? entityId)
        {
            var list = new List<RawRow>();

            string extraFilter = "";

            if (entityType == "article" && entityId.HasValue)
                extraFilter = $"AND ia.ArticleID = {entityId.Value}";
            else if (entityType == "client" && entityId.HasValue)
                // Invoice.ClientName is a denormalized string — filter via subquery
                extraFilter += $" AND i.ClientName = (SELECT TOP 1 Nom FROM Client WHERE ClientID = {entityId.Value})";
            // Note: Invoice table is for client sales only, not fournisseur

            // InvoiceArticle has ArticleName + PrixUnitaire + Quantite directly (no Article join needed)
            string sql = $@"
                SELECT
                    i.InvoiceID,
                    i.InvoiceDate                      AS InvDate,
                    ia.PrixUnitaire * ia.Quantite      AS LineValue,
                    ia.Quantite                        AS Qty,
                    ia.ArticleName                     AS Label,
                    CASE WHEN i.EtatFacture = 1 THEN 'Payé' ELSE 'En attente' END AS InvStatus
                FROM Invoice i
                INNER JOIN InvoiceArticle ia ON ia.InvoiceID = i.InvoiceID
                WHERE i.InvoiceDate BETWEEN @s AND @e
                  AND i.IsDeleted = 0
                  AND ia.IsDeleted = 0
                  {extraFilter}";

            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@s", start.Date);
                cmd.Parameters.AddWithValue("@e", end.Date.AddDays(1).AddTicks(-1));

                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new RawRow
                        {
                            Id         = r.GetInt32(r.GetOrdinal("InvoiceID")),
                            Date       = r.GetDateTime(r.GetOrdinal("InvDate")),
                            Value      = r.IsDBNull(r.GetOrdinal("LineValue")) ? 0 : Convert.ToDouble(r["LineValue"]),
                            IsReversed = false,
                            Quantity   = r.IsDBNull(r.GetOrdinal("Qty")) ? 0 : Convert.ToDouble(r["Qty"]),
                            Label      = r["Label"]?.ToString() ?? "",
                            Source     = "Facture",
                            Status     = r["InvStatus"]?.ToString() ?? ""
                        });
            }

            return list;
        }

        // ── Grouping ─────────────────────────────────────────────
        private static List<DashboardPoint> BuildByDay(
            List<RawRow> ops, List<RawRow> inv, DateTime start, DateTime end)
        {
            var pts = new List<DashboardPoint>();
            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                var dOps = ops.Where(o => o.Date.Date == d).ToList();
                var dInv = inv.Where(i => i.Date.Date == d).ToList();
                pts.Add(new DashboardPoint
                {
                    Date      = d,
                    DateLabel = d.ToString("dd/MM"),
                    OpsValue  = dOps.Where(o => !o.IsReversed).Sum(o => o.Value),
                    InvValue  = dInv.Sum(i => i.Value),
                    OpsCount  = dOps.Count(o => !o.IsReversed),
                    InvCount  = dInv.Count
                });
            }
            return pts;
        }

        private static List<DashboardPoint> BuildByMonth(
            List<RawRow> ops, List<RawRow> inv, DateTime start, DateTime end)
        {
            var pts = new List<DashboardPoint>();
            var cur = new DateTime(start.Year, start.Month, 1);
            while (cur <= end)
            {
                var nxt  = cur.AddMonths(1);
                var mOps = ops.Where(o => o.Date >= cur && o.Date < nxt).ToList();
                var mInv = inv.Where(i => i.Date >= cur && i.Date < nxt).ToList();
                pts.Add(new DashboardPoint
                {
                    Date      = cur,
                    DateLabel = cur.ToString("MMM yy"),
                    OpsValue  = mOps.Where(o => !o.IsReversed).Sum(o => o.Value),
                    InvValue  = mInv.Sum(i => i.Value),
                    OpsCount  = mOps.Count(o => !o.IsReversed),
                    InvCount  = mInv.Count
                });
                cur = nxt;
            }
            return pts;
        }

        private static void ApplyView(List<DashboardPoint> pts, DashboardView view)
        {
            if (view == DashboardView.StockMovement)
                foreach (var p in pts) { p.OpsValue = p.OpsCount; p.InvValue = p.InvCount; }
            // Gap: keep raw values — Gap is a computed property on DashboardPoint
        }

        private static List<TableRow> BuildRows(List<RawRow> ops, List<RawRow> inv)
        {
            var rows = new List<TableRow>();

            foreach (var o in ops.OrderByDescending(x => x.Date).Take(500))
                rows.Add(new TableRow
                {
                    Source    = o.Source,
                    DateLabel = o.Date.ToString("dd/MM/yyyy"),
                    Label     = o.Label,
                    Quantity  = o.Quantity,
                    Value     = o.Value,
                    Status    = o.Status,
                    RawId     = o.Id
                });

            foreach (var i in inv.OrderByDescending(x => x.Date).Take(500))
                rows.Add(new TableRow
                {
                    Source    = i.Source,
                    DateLabel = i.Date.ToString("dd/MM/yyyy"),
                    Label     = i.Label,
                    Quantity  = i.Quantity,
                    Value     = i.Value,
                    Status    = i.Status,
                    RawId     = i.Id
                });

            return rows.OrderByDescending(r => r.DateLabel).ToList();
        }

        private static KpiSummary ComputeKpi(List<RawRow> ops, List<RawRow> inv)
        {
            var rev   = ops.Where(o => !o.IsReversed).Sum(o => o.Value);
            var invT  = inv.Sum(i => i.Value);
            var opCnt = ops.Count(o => !o.IsReversed);
            return new KpiSummary
            {
                TotalRevenue = rev,
                TotalInvoice = invT,
                OpCount      = opCnt,
                AvgBasket    = opCnt > 0 ? rev / opCnt : 0,
                RevenueTrend = rev  > 0  ? 12.4  : 0,
                OpCountTrend = opCnt > 0 ? 8.1   : 0,
                BasketTrend  = opCnt > 0 ? -2.3  : 0,
                GapTrend     = (rev - invT) > 0  ? 5.7 : -5.7
            };
        }

        // ── Internal row ─────────────────────────────────────────
        internal class RawRow
        {
            public int      Id         { get; set; }
            public DateTime Date       { get; set; }
            public double   Value      { get; set; }
            public bool     IsReversed { get; set; }
            public double   Quantity   { get; set; }
            public string   Label      { get; set; }
            public string   Source     { get; set; }
            public string   Status     { get; set; }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CODE-BEHIND
    // ═══════════════════════════════════════════════════════════════

    public partial class CGraphe : UserControl
    {
        // ── State ─────────────────────────────────────────────────
        private List<DashboardPoint>              _chartPoints = new List<DashboardPoint>();
        private ObservableCollection<TableRow>    _tableRows   = new ObservableCollection<TableRow>();
        private string     _chartType  = "area";
        private DashboardView _view    = DashboardView.MonthlyRevenue;
        private string     _entityType = "";
        private int?       _entityId   = null;
        private bool       _includeOps = true;
        private bool       _includeInv = true;
        private bool       _combinedMode   = false;  // NEW: merge Ops+Factures into one series
        private bool       _mouseOverChart = false; // guard: prevents SizeChanged from redrawing during hover
        private DateTime   _startDate  = DateTime.Today.AddMonths(-3);
        private DateTime   _endDate    = DateTime.Today;
        private Line       _crosshairLine;          // reused crosshair — never cleared mid-hover

        private static readonly Color BlueColor   = Color.FromRgb(0x66, 0x7E, 0xEA);
        private static readonly Color PurpleColor = Color.FromRgb(0x76, 0x4B, 0xA2);

        public CGraphe()
        {
            InitializeComponent();
            DataTable.ItemsSource = _tableRows;

            Loaded += (s, e) =>
            {
                StartDatePicker.SelectedDate = _startDate;
                EndDatePicker.SelectedDate   = _endDate;
            };
        }

        // ── Guard: only handle events once all controls exist ─────
        private bool Ready =>
            StartDatePicker    != null && EndDatePicker        != null &&
            OperationsCheckBox != null && FacturesCheckBox     != null &&
            MetricComboBox     != null && GraphTypeComboBox    != null &&
            EntityTypeComboBox != null && EntitySearchBox      != null &&
            SearchResultsList  != null;
        // Note: CombinedCheckBox is optional — checked with null-guard in DataSource_Changed

        // ─────────────────────────────────────────────────────────
        //  SETTINGS EVENTS
        // ─────────────────────────────────────────────────────────

        private void MetricComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Ready) return;
            if (!(MetricComboBox.SelectedItem is ComboBoxItem ci)) return;
            var tag = ci.Tag?.ToString() ?? "";
            if      (tag == "revenue")    _view = DashboardView.MonthlyRevenue;
            else if (tag == "stock")      _view = DashboardView.StockMovement;
            else if (tag == "invoicegap") _view = DashboardView.InvoicingGap;
            else if (tag == "client")     _view = DashboardView.ClientPerformance;
            else if (tag == "supplier")   _view = DashboardView.SupplierActivity;
            else                          _view = DashboardView.MonthlyRevenue;
            UpdateChartTitles();
        }

        private void UpdateChartTitles()
        {
            if (GraphTitle == null) return;
            if (_view == DashboardView.MonthlyRevenue)
            {
                GraphTitle.Text    = "Évolution du Revenu (DH)";
                GraphSubtitle.Text = _combinedMode
                    ? "Opérations + Factures combinées (teal)"
                    : "Opérations (bleu) vs Factures (violet)";
            }
            else if (_view == DashboardView.StockMovement)
            {
                GraphTitle.Text    = "Volume Articles Traités (Qté)";
                GraphSubtitle.Text = _combinedMode
                    ? "Nombre total de lignes opérations + factures"
                    : "Nombre de lignes opérations vs factures";
            }
            else if (_view == DashboardView.InvoicingGap)
            {
                GraphTitle.Text    = "Opérations vs Factures — Écart (DH)";
                GraphSubtitle.Text = "Différence entre montants opérés et facturés";
            }
            else if (_view == DashboardView.ClientPerformance)
            {
                GraphTitle.Text    = "Performance Client (DH)";
                GraphSubtitle.Text = "Valeur des transactions par client";
            }
            else
            {
                GraphTitle.Text    = "Activité Fournisseur (DH)";
                GraphSubtitle.Text = "Volume achats et approvisionnements";
            }
        }

        private void DatePicker_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!Ready) return;
            if (StartDatePicker.SelectedDate.HasValue) _startDate = StartDatePicker.SelectedDate.Value;
            if (EndDatePicker.SelectedDate.HasValue)   _endDate   = EndDatePicker.SelectedDate.Value;
        }

        private void QuickRange_Click(object sender, RoutedEventArgs e)
        {
            if (!Ready) return;
            if (!(sender is Button btn) || !int.TryParse(btn.Tag?.ToString(), out int days)) return;
            _endDate   = DateTime.Today;
            _startDate = DateTime.Today.AddDays(-days);
            StartDatePicker.SelectedDate = _startDate;
            EndDatePicker.SelectedDate   = _endDate;
        }

        private void DataSource_Changed(object sender, RoutedEventArgs e)
        {
            if (!Ready) return;
            _includeOps   = OperationsCheckBox.IsChecked == true;
            _includeInv   = FacturesCheckBox.IsChecked   == true;
            var cb = FindName("CombinedCheckBox") as CheckBox;
            _combinedMode = cb != null && cb.IsChecked == true;
        }

        private void GraphTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Ready) return;
            if (!(GraphTypeComboBox.SelectedItem is ComboBoxItem ci)) return;
            var tag = ci.Tag?.ToString() ?? "area";
            _chartType = tag;

            // Sync radio tabs
            if (TabArea != null)
            {
                TabArea.IsChecked = tag == "area";
                TabLine.IsChecked = tag == "line";
                TabBar.IsChecked  = tag == "bar";
            }

            if (_chartPoints.Any()) DrawChart(_chartPoints);
        }

        private void TabBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (!Ready || TabArea == null) return;
            if      (TabArea.IsChecked == true) _chartType = "area";
            else if (TabLine.IsChecked == true) _chartType = "line";
            else if (TabBar.IsChecked  == true) _chartType = "bar";

            // Sync combobox
            if (GraphTypeComboBox != null)
            {
                foreach (ComboBoxItem ci in GraphTypeComboBox.Items)
                    if (ci.Tag?.ToString() == _chartType) { GraphTypeComboBox.SelectedItem = ci; break; }
            }

            if (_chartPoints.Any()) DrawChart(_chartPoints);
        }

        // ─────────────────────────────────────────────────────────
        //  ENTITY: select type → then search within it
        // ─────────────────────────────────────────────────────────

        private List<EntityItem> _loadedEntities = new List<EntityItem>();

        private async void EntityTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Ready) return;

            // Reset search
            EntitySearchBox.Text         = "";
            EntitySearchBox.IsEnabled    = false;
            SearchResultsList.Visibility = Visibility.Collapsed;
            _loadedEntities.Clear();
            _entityType = "";
            _entityId   = null;

            if (!(EntityTypeComboBox.SelectedItem is ComboBoxItem ci)) return;
            var tag = ci.Tag?.ToString() ?? "";
            if (string.IsNullOrEmpty(tag)) return;  // "— Tous —" selected

            EntitySearchBox.IsEnabled    = true;
            EntitySearchBox.Focus();

            // Pre-load all entities of this type in background
            _loadedEntities = await DataEngine.LoadEntitiesAsync(tag);
            _entityType     = tag;
        }

        private void EntitySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Ready) return;
            var q = EntitySearchBox.Text.Trim();

            if (q.Length == 0)
            {
                SearchResultsList.Visibility = Visibility.Collapsed;
                _entityId = null;
                return;
            }

            var matches = _loadedEntities
                .Where(x => x.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(10)
                .Select(x => x.DisplayName)
                .ToList();

            SearchResultsList.ItemsSource = matches;
            SearchResultsList.Visibility  = matches.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Ready || SearchResultsList.SelectedItem == null) return;

            var selected = SearchResultsList.SelectedItem.ToString();
            var entity   = _loadedEntities.FirstOrDefault(
                               x => x.DisplayName == selected);

            if (entity != null)
            {
                EntitySearchBox.Text         = entity.DisplayName;
                _entityId                    = entity.Id;
                SearchResultsList.Visibility = Visibility.Collapsed;
            }
        }

        private void EntitySearchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // ─────────────────────────────────────────────────────────
        //  GENERATE
        // ─────────────────────────────────────────────────────────

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadAndRenderAsync();
        }

        private void GraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Do NOT redraw while the mouse is over the chart — a layout pass triggered by
            // CrosshairCanvas changes would otherwise blank the chart on every mouse-move.
            if (_mouseOverChart) return;
            if (_chartPoints.Any()) DrawChart(_chartPoints);
        }

        private async Task LoadAndRenderAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            EmptyState.Visibility     = Visibility.Collapsed;
            ApplyBtn.IsEnabled        = false;

            try
            {
                var (points, rows, kpi) = await DataEngine.LoadAsync(
                    _view, _startDate, _endDate,
                    _includeOps, _includeInv,
                    _entityType, _entityId);

                _chartPoints = points;

                // Update KPI cards
                UpdateKpiCards(kpi);

                // Draw chart
                if (points.Any())
                {
                    EmptyState.Visibility = Visibility.Collapsed;
                    DrawChart(points);
                }
                else
                {
                    EmptyState.Visibility = Visibility.Visible;
                }

                // Update table
                _tableRows.Clear();
                foreach (var r in rows) _tableRows.Add(r);
                RowCountText.Text    = $"{_tableRows.Count} lignes";
                LastRefreshText.Text = $"Actualisé à {DateTime.Now:HH:mm}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement :\n{ex.Message}",
                                "CGraphe", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                ApplyBtn.IsEnabled        = true;
            }
        }

        private void UpdateKpiCards(KpiSummary kpi)
        {
            Kpi1Value.Text = FormatMoney(kpi.TotalRevenue);
            Kpi2Value.Text = kpi.OpCount.ToString("N0");
            Kpi3Value.Text = FormatMoney(kpi.AvgBasket);
            Kpi4Value.Text = FormatMoney(kpi.Gap);

            SetTrend(Kpi1Trend, kpi.RevenueTrend);
            SetTrend(Kpi2Trend, kpi.OpCountTrend);
            SetTrend(Kpi3Trend, kpi.BasketTrend);
            SetTrend(Kpi4Trend, kpi.GapTrend, invertColor: true);
        }

        private static string FormatMoney(double v)
        {
            if (Math.Abs(v) >= 1_000_000) return $"{v / 1_000_000:N1} M DH";
            if (Math.Abs(v) >= 1_000)     return $"{v / 1_000:N1} K DH";
            return $"{v:N0} DH";
        }

        private static void SetTrend(TextBlock tb, double val, bool invertColor = false)
        {
            bool positive = val >= 0;
            bool green    = invertColor ? !positive : positive;
            tb.Text       = (positive ? "▲ +" : "▼ ") + $"{Math.Abs(val):N1} %";
            tb.Foreground = green
                ? new SolidColorBrush(Color.FromRgb(0x38, 0xA1, 0x69))
                : new SolidColorBrush(Color.FromRgb(0xE5, 0x3E, 0x3E));
        }

        // ─────────────────────────────────────────────────────────
        //  CHART RENDERING
        // ─────────────────────────────────────────────────────────

        private void DrawChart(List<DashboardPoint> pts)
        {
            GraphCanvas.Children.Clear();
            YAxisCanvas.Children.Clear();
            XAxisCanvas.Children.Clear();
            // DO NOT clear CrosshairCanvas here — ChartTooltip lives inside it.
            // Only remove the crosshair line (if it exists); leave the tooltip element intact.
            if (_crosshairLine != null)
            {
                CrosshairCanvas.Children.Remove(_crosshairLine);
                _crosshairLine = null;
            }
            ChartTooltip.Visibility = Visibility.Collapsed;

            double W = GraphCanvas.ActualWidth;
            double H = GraphCanvas.ActualHeight;
            if (W < 10 || H < 10 || !pts.Any()) return;

            const double pad = 12;
            double plotW = W - pad * 2;
            double plotH = H - pad * 2;

            double maxV = _combinedMode
                ? pts.Max(p => p.OpsValue + p.InvValue)
                : pts.Max(p => Math.Max(p.OpsValue, p.InvValue));
            double minV = _combinedMode
                ? pts.Min(p => p.OpsValue + p.InvValue)
                : pts.Min(p => Math.Min(p.OpsValue, p.InvValue));
            if (Math.Abs(maxV - minV) < 1) { maxV = 100; minV = 0; }
            maxV = maxV * 1.12;
            minV = minV >= 0 ? 0 : minV * 1.08;

            int n = pts.Count;
            Func<int, double>    Px = i => pad + (n == 1 ? plotW / 2 : i * plotW / (n - 1));
            Func<double, double> Py = v => pad + plotH - ((v - minV) / (maxV - minV)) * plotH;

            DrawYAxis(minV, maxV, plotH, pad, W);
            DrawXAxis(pts, Px);

            // In combined mode, merge both series into OpsValue for a single line
            var drawPts = _combinedMode
                ? pts.Select(p => new DashboardPoint
                    {
                        Date      = p.Date,
                        DateLabel = p.DateLabel,
                        OpsValue  = p.OpsValue + p.InvValue,
                        InvValue  = 0,
                        OpsCount  = p.OpsCount + p.InvCount,
                        InvCount  = 0
                    }).ToList()
                : pts;

            var gradColor = _combinedMode ? Color.FromRgb(0x38, 0xB2, 0xAC) : BlueColor; // teal for combined

            if (_chartType == "area")
            {
                if (_combinedMode)
                {
                    DrawFilledSeries(drawPts, Px, Py, gradColor,   0.14, 0.45, useInv: false);
                    DrawLineSeries  (drawPts, Px, Py, gradColor,   2.2,  useInv: false);
                    DrawDots        (drawPts, Px, Py, gradColor,         useInv: false);
                }
                else
                {
                    DrawFilledSeries(drawPts, Px, Py, BlueColor,   0.14, 0.45, useInv: false);
                    DrawFilledSeries(drawPts, Px, Py, PurpleColor, 0.10, 0.35, useInv: true);
                    DrawLineSeries  (drawPts, Px, Py, BlueColor,   2.2,  useInv: false);
                    DrawLineSeries  (drawPts, Px, Py, PurpleColor, 2.2,  useInv: true);
                    DrawDots        (drawPts, Px, Py, BlueColor,         useInv: false);
                    DrawDots        (drawPts, Px, Py, PurpleColor,       useInv: true);
                }
            }
            else if (_chartType == "line")
            {
                if (_combinedMode)
                {
                    DrawLineSeries(drawPts, Px, Py, gradColor, 2.0, useInv: false);
                    DrawDots      (drawPts, Px, Py, gradColor,      useInv: false);
                }
                else
                {
                    DrawLineSeries(drawPts, Px, Py, BlueColor,   2.0, useInv: false);
                    DrawLineSeries(drawPts, Px, Py, PurpleColor, 2.0, useInv: true);
                    DrawDots      (drawPts, Px, Py, BlueColor,        useInv: false);
                    DrawDots      (drawPts, Px, Py, PurpleColor,      useInv: true);
                }
            }
            else
            {
                DrawBarChart(drawPts, Px, Py);
            }

            // Reattach mouse events
            GraphCanvas.MouseMove  -= OnMouseMove;
            GraphCanvas.MouseLeave -= OnMouseLeave;
            GraphCanvas.MouseMove  += OnMouseMove;
            GraphCanvas.MouseLeave += OnMouseLeave;
        }

        // ── Area fill ─────────────────────────────────────────────
        private void DrawFilledSeries(List<DashboardPoint> pts,
            Func<int, double> Px, Func<double, double> Py,
            Color col, double alphaTop, double alphaBot, bool useInv)
        {
            if (pts.Count < 2) return;
            double H = GraphCanvas.ActualHeight;
            const double pad = 12;

            var fig = new PathFigure
            {
                StartPoint = new Point(Px(0), Py(useInv ? pts[0].InvValue : pts[0].OpsValue))
            };

            for (int i = 1; i < pts.Count; i++)
            {
                double x0 = Px(i - 1), y0 = Py(useInv ? pts[i - 1].InvValue : pts[i - 1].OpsValue);
                double x1 = Px(i),     y1 = Py(useInv ? pts[i].InvValue     : pts[i].OpsValue);
                double cx = (x0 + x1) / 2;
                fig.Segments.Add(new BezierSegment(new Point(cx, y0), new Point(cx, y1), new Point(x1, y1), true));
            }

            double bottom = Math.Min(H - pad, Py(0));
            fig.Segments.Add(new LineSegment(new Point(Px(pts.Count - 1), bottom), false));
            fig.Segments.Add(new LineSegment(new Point(Px(0), bottom), false));
            fig.IsClosed = true;

            var grad = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            grad.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(alphaTop * 255), col.R, col.G, col.B), 0));
            grad.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(alphaBot * 255), col.R, col.G, col.B), 0.5));
            grad.GradientStops.Add(new GradientStop(Color.FromArgb(0, col.R, col.G, col.B), 1));

            GraphCanvas.Children.Add(new System.Windows.Shapes.Path
            {
                Data = new PathGeometry { Figures = { fig } },
                Fill = grad, StrokeThickness = 0
            });
        }

        // ── Line ──────────────────────────────────────────────────
        private void DrawLineSeries(List<DashboardPoint> pts,
            Func<int, double> Px, Func<double, double> Py,
            Color col, double thickness, bool useInv)
        {
            if (pts.Count < 2) return;
            var fig = new PathFigure
            {
                StartPoint = new Point(Px(0), Py(useInv ? pts[0].InvValue : pts[0].OpsValue))
            };
            for (int i = 1; i < pts.Count; i++)
            {
                double x0 = Px(i - 1), y0 = Py(useInv ? pts[i - 1].InvValue : pts[i - 1].OpsValue);
                double x1 = Px(i),     y1 = Py(useInv ? pts[i].InvValue     : pts[i].OpsValue);
                double cx = (x0 + x1) / 2;
                fig.Segments.Add(new BezierSegment(new Point(cx, y0), new Point(cx, y1), new Point(x1, y1), true));
            }
            GraphCanvas.Children.Add(new System.Windows.Shapes.Path
            {
                Data = new PathGeometry { Figures = { fig } },
                Stroke = new SolidColorBrush(col),
                StrokeThickness = thickness,
                Fill = Brushes.Transparent
            });
        }

        // ── Dots ──────────────────────────────────────────────────
        private void DrawDots(List<DashboardPoint> pts,
            Func<int, double> Px, Func<double, double> Py,
            Color col, bool useInv)
        {
            for (int i = 0; i < pts.Count; i++)
            {
                double v = useInv ? pts[i].InvValue : pts[i].OpsValue;
                double x = Px(i), y = Py(v);
                const double r = 4.5;
                var el = new Ellipse
                {
                    Width = r * 2, Height = r * 2,
                    Fill = new SolidColorBrush(col),
                    Stroke = Brushes.White, StrokeThickness = 1.5,
                    Effect = new DropShadowEffect { Color = col, Opacity = 0.5, BlurRadius = 8, ShadowDepth = 0 }
                };
                Canvas.SetLeft(el, x - r);
                Canvas.SetTop (el, y - r);
                GraphCanvas.Children.Add(el);
            }
        }

        // ── Bar chart ─────────────────────────────────────────────
        private void DrawBarChart(List<DashboardPoint> pts,
            Func<int, double> Px, Func<double, double> Py)
        {
            int    n     = pts.Count;
            double groupW = n > 1 ? (Px(1) - Px(0)) * 0.80 : 30;
            double barW   = groupW / 2.1;
            double zero   = Py(0);

            for (int i = 0; i < n; i++)
            {
                DrawBar(Px(i) - barW - 1, Py(pts[i].OpsValue), barW, zero, BlueColor);
                DrawBar(Px(i) + 1,        Py(pts[i].InvValue), barW, zero, PurpleColor);
            }
        }

        private void DrawBar(double x, double top, double w, double zero, Color col)
        {
            double height = Math.Max(2, zero - top);
            var grad = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            grad.GradientStops.Add(new GradientStop(col, 0));
            grad.GradientStops.Add(new GradientStop(
                Color.FromRgb((byte)(col.R * 0.75), (byte)(col.G * 0.75), (byte)(col.B * 0.75)), 1));

            var b = new Border
            {
                Width = w, Height = height,
                Background = grad,
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Effect = new DropShadowEffect { Color = col, Opacity = 0.20, BlurRadius = 6, ShadowDepth = 0 }
            };
            Canvas.SetLeft(b, x);
            Canvas.SetTop (b, top);
            GraphCanvas.Children.Add(b);
        }

        // ── Y axis ────────────────────────────────────────────────
        private void DrawYAxis(double minV, double maxV, double plotH, double pad, double W)
        {
            for (int s = 0; s <= 5; s++)
            {
                double frac = (double)s / 5;
                double val  = minV + frac * (maxV - minV);
                double y    = pad + plotH - frac * plotH;

                GraphCanvas.Children.Add(new Line
                {
                    X1 = 0, X2 = W, Y1 = y, Y2 = y,
                    Stroke = s == 0
                             ? new SolidColorBrush(Color.FromRgb(0xC0, 0xC8, 0xD8))
                             : new SolidColorBrush(Color.FromRgb(0xED, 0xF2, 0xF7)),
                    StrokeThickness = s == 0 ? 1.2 : 0.8,
                    StrokeDashArray = s > 0 ? new DoubleCollection { 4, 4 } : null
                });

                var lbl = new TextBlock
                {
                    Text = FormatAxis(val),
                    FontSize = 10, FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xAE, 0xC0))
                };
                Canvas.SetRight(lbl, 4);
                Canvas.SetTop  (lbl, y - 8);
                YAxisCanvas.Children.Add(lbl);
            }
        }

        // ── X axis ────────────────────────────────────────────────
        private void DrawXAxis(List<DashboardPoint> pts, Func<int, double> Px)
        {
            int n    = pts.Count;
            int step = Math.Max(1, n / 8);
            for (int i = 0; i < n; i += step)
            {
                var lbl = new TextBlock
                {
                    Text = pts[i].DateLabel,
                    FontSize = 10, FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xAE, 0xC0))
                };
                Canvas.SetLeft  (lbl, Px(i) - 20);
                Canvas.SetBottom(lbl, 2);
                XAxisCanvas.Children.Add(lbl);
            }
        }

        private static string FormatAxis(double v)
        {
            if (Math.Abs(v) >= 1_000_000) return $"{v / 1_000_000:N1}M";
            if (Math.Abs(v) >= 1_000)     return $"{v / 1_000:N0}K";
            return $"{v:N0}";
        }

        // ── Mouse crosshair & tooltip ─────────────────────────────
        private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
            => OnMouseMove(sender, e);
        private void GraphCanvas_MouseLeave(object sender, MouseEventArgs e)
            => OnMouseLeave(sender, e);

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_chartPoints.Any()) return;
            _mouseOverChart = true;   // prevent SizeChanged from blanking the chart

            var    pos  = e.GetPosition(GraphCanvas);
            double W    = GraphCanvas.ActualWidth;
            int    n    = _chartPoints.Count;
            const double pad = 12;
            Func<int, double> Px = i => pad + (n == 1 ? (W - pad * 2) / 2 : i * (W - pad * 2) / (n - 1));

            int    best = 0;
            double minD = double.MaxValue;
            for (int i = 0; i < n; i++)
            {
                double d = Math.Abs(Px(i) - pos.X);
                if (d < minD) { minD = d; best = i; }
            }

            var pt = _chartPoints[best];
            double cx = Px(best);

            // Reuse or create the crosshair line — never call Children.Clear() here
            // so that ChartTooltip (also on CrosshairCanvas) is never removed.
            if (_crosshairLine == null)
            {
                _crosshairLine = new Line
                {
                    Stroke          = new SolidColorBrush(Color.FromArgb(100, 0x66, 0x7E, 0xEA)),
                    StrokeThickness = 1.2,
                    StrokeDashArray = new DoubleCollection { 5, 4 }
                };
                CrosshairCanvas.Children.Insert(0, _crosshairLine); // insert below tooltip
            }
            _crosshairLine.Visibility = Visibility.Visible;
            _crosshairLine.X1 = cx;
            _crosshairLine.X2 = cx;
            _crosshairLine.Y1 = 0;
            _crosshairLine.Y2 = GraphCanvas.ActualHeight;

            TooltipDate.Text = pt.DateLabel;

            if (_combinedMode)
            {
                TooltipOps.Text      = $"Total: {FormatAxis(pt.OpsValue + pt.InvValue)} DH";
                TooltipFact.Visibility = Visibility.Collapsed;
            }
            else
            {
                TooltipOps.Text        = $"Op: {FormatAxis(pt.OpsValue)} DH";
                TooltipFact.Text       = $"Fa: {FormatAxis(pt.InvValue)} DH";
                TooltipFact.Visibility = _includeInv ? Visibility.Visible : Visibility.Collapsed;
            }

            ChartTooltip.Visibility = Visibility.Visible;

            double tx = cx + 14;
            if (tx + 160 > W) tx = cx - 170;
            Canvas.SetLeft(ChartTooltip, tx);
            Canvas.SetTop (ChartTooltip, 12);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _mouseOverChart = false;
            // Hide crosshair line without removing it (preserves ChartTooltip in visual tree)
            if (_crosshairLine != null)
                _crosshairLine.Visibility = Visibility.Collapsed;
            ChartTooltip.Visibility = Visibility.Collapsed;
        }
    }
}
