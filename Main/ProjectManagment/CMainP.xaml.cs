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
using System.Xml.Linq;

namespace GestionComerce.Main.ProjectManagment
{
    /// <summary>
    /// Interaction logic for CMainP.xaml
    /// </summary>
    public partial class CMainP : UserControl
    {
        bool isInitialized = false;
        public CMainP(User u, MainWindow main)
        {
            InitializeComponent();

            this.Loaded += (s, e) => isInitialized = true;
            this.u = u;
            this.main = main;

            // ✅ Set default filter selections
            TypeOperationFilter.SelectedIndex = 0;
            StatusFilter.SelectedIndex = 0;
            DateFilter.SelectedIndex = 0;
            SearchTypeMouvmentFilter.SelectedIndex = 0;
            TypeMouvmentFilter.SelectedIndex = 0;
            StatusMouvmenttFilter.SelectedIndex = 0;
            StatusMouvmentReversedFilter.SelectedIndex = 0;
            DateMouvmentFilter.SelectedIndex = 0;

            // ✅ Reset counters
            VenteCount = 0;
            AchatCount = 0;
            MouvmentVenteCount = 0;
            MouvmentAchatCount = 0;
            MouvmentFinanceCount = 0;
            Finance = 0;

            LoadStats();

            foreach (Role r in main.lr)
            {
                if (u.RoleID == r.RoleID)
                {
                    if (r.ViewOperation == false)
                    {
                        OperationsButton.IsEnabled = false;
                        OperationsContent.Visibility = Visibility.Collapsed;
                        MouvmentContent.Visibility = Visibility.Visible;
                        OperationsButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                        OperationsButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent"));
                        StockButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E90FF"));
                        StockButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E90FF"));
                    }
                    if (r.ViewMouvment == false)
                    {
                        StockButton.IsEnabled = false;
                        MouvmentContent.Visibility = Visibility.Collapsed;
                    }
                    break;
                }
            }

            // ✅ Load data
            LoadOperations(main.lo);
            LoadMouvments(main.loa);
        }

        public User u;
        public MainWindow main;
        public int VenteCount;
        public int AchatCount;
        public Decimal Finance;
        public Decimal MouvmentVenteCount;
        public Decimal MouvmentAchatCount;
        public Decimal MouvmentFinanceCount;
        public int index = 10;
        public int index2 = 10;

        public void LoadOperations(List<Operation> lo)
        {
            int i = 1;
            OperationsContainer.Children.Clear();
            foreach (Operation operation in lo)
            {
                if (i > index) break;
                i++;
                CSingleOperation wSingleOperation = new CSingleOperation(this, operation);
                OperationsContainer.Children.Add(wSingleOperation);
            }
            if (i <= 10)
            {
                SeeMoreContainer.Visibility = Visibility.Collapsed;
            }
            else
            {
                SeeMoreContainer.Visibility = Visibility.Visible;
            }
        }

        public void LoadMouvments(List<OperationArticle> loa)
        {
            int i = 1;
            MouvmentContainer.Children.Clear();
            foreach (OperationArticle operationA in loa)
            {
                if (i > index2) break;
                i++;
                CSingleMouvment wSingleMouvment = new CSingleMouvment(this, operationA);
                MouvmentContainer.Children.Add(wSingleMouvment);
            }

            if (i <= 10)
            {
                ViewMoreContainer1.Visibility = Visibility.Collapsed;
            }
            else
            {
                ViewMoreContainer1.Visibility = Visibility.Visible;
            }
        }

        public void LoadStats()
        {
            VenteCount = 0;
            AchatCount = 0;
            Finance = 0;
            MouvmentVenteCount = 0;
            MouvmentAchatCount = 0;
            MouvmentFinanceCount = 0;
            foreach (Operation operation in main.lo)
            {
                if (operation.Reversed) continue; // skip reversed ops

                if (operation.OperationType.StartsWith("V", StringComparison.OrdinalIgnoreCase))
                {
                    VenteCount++;
                    Finance += operation.PrixOperation;

                    foreach (OperationArticle oa in main.loa.Where(x => x.OperationID == operation.OperationID && !x.Reversed))
                    {
                        Article article = main.la.FirstOrDefault(a => a.ArticleID == oa.ArticleID);
                        if (article != null)
                        {
                            MouvmentFinanceCount += article.PrixVente * oa.QteArticle;
                            MouvmentVenteCount++;
                        }
                    }
                }
                else if (operation.OperationType.StartsWith("A", StringComparison.OrdinalIgnoreCase))
                {
                    AchatCount++;
                    Finance -= operation.PrixOperation;

                    foreach (OperationArticle oa in main.loa.Where(x => x.OperationID == operation.OperationID && !x.Reversed))
                    {
                        Article article = main.la.FirstOrDefault(a => a.ArticleID == oa.ArticleID);
                        if (article != null)
                        {
                            MouvmentFinanceCount -= article.PrixAchat * oa.QteArticle;
                            MouvmentAchatCount++;
                        }
                    }
                }
            }
            VenteCountLabel.Text = VenteCount.ToString("0");
            AchatCountLabel.Text = AchatCount.ToString("0");
            FinanceLabel.Text = Finance.ToString("0.00") + " DH";
            MouvmentVente.Text = MouvmentVenteCount.ToString("0");
            MouvmentAchat.Text = MouvmentAchatCount.ToString("0");
            MouvmentFinance.Text = MouvmentFinanceCount.ToString("0.00") + " DH";
        }

        public void LoadStatistics()
        {
            LoadStats();
        }

        private void RetourButton_Click(object sender, RoutedEventArgs e)
        {
            main.load_main(u);
        }

        private void ToutButton_Click(object sender, RoutedEventArgs e)
        {
            SearchInput.Text = "";
            index = 10;
            TypeOperationFilter.SelectedIndex = 0;
            StatusFilter.SelectedIndex = 0;
            DateFilter.SelectedIndex = 0;
            SearchTypeFilter.SelectedIndex = 0;

            LoadOperations(main.lo);
        }

        private void OperationsButton_Click(object sender, RoutedEventArgs e)
        {
            OperationsContent.Visibility = Visibility.Visible;
            MouvmentContent.Visibility = Visibility.Collapsed;
            OperationsButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E90FF"));
            OperationsButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E90FF"));
            StockButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
            StockButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent"));
        }

        private void StockButton_Click(object sender, RoutedEventArgs e)
        {
            OperationsContent.Visibility = Visibility.Collapsed;
            MouvmentContent.Visibility = Visibility.Visible;
            OperationsButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
            OperationsButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent"));
            StockButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E90FF"));
            StockButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E90FF"));
        }

        private void RepportButton_Click(object sender, RoutedEventArgs e)
        {
            CMainR mainR = new CMainR(u, main);

            Window parentWindow = Window.GetWindow(this);
            if (parentWindow is MainWindow mainWindow)
            {
                var parent = this.Parent;
                while (parent != null && !(parent is ContentControl))
                {
                    parent = System.Windows.LogicalTreeHelper.GetParent(parent);
                }

                if (parent is ContentControl contentControl)
                {
                    contentControl.Content = mainR;
                }
            }
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
            => ApplyFilters();

        private void TypeOperationFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyFilters();

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyFilters();

        private void DateFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyFilters();

        private void ApplyFilters()
        {
            if (!isInitialized) return;

            IEnumerable<Operation> lo = main.lo;

            lo = lo.Where(op => op.OperationType != "Livraison Groupée" &&
                                op.OperationType != "VenteLiv");

            // --- Search filter (index-based) ---
            // SearchTypeFilter: 0=OperationID, 1=Client, 2=Supplier, 3=User
            string searchText = SearchInput.Text?.Trim() ?? "";
            int searchTypeIndex = SearchTypeFilter.SelectedIndex;

            if (!string.IsNullOrEmpty(searchText))
            {
                switch (searchTypeIndex)
                {
                    case 0: // Operation ID
                        lo = lo.Where(op => op.OperationID.ToString().Contains(searchText));
                        break;
                    case 1: // Client
                        lo = lo.Where(op => main.lc.Any(c => c.ClientID == op.ClientID && c.Nom.Contains(searchText)));
                        break;
                    case 2: // Supplier
                        lo = lo.Where(op => main.lfo.Any(f => f.FournisseurID == op.FournisseurID && f.Nom.Contains(searchText)));
                        break;
                    case 3: // User
                        lo = lo.Where(op => main.lu.Any(u => u.UserID == op.UserID && u.UserName.Contains(searchText)));
                        break;
                }
            }

            // --- Operation type filter (index-based) ---
            // TypeOperationFilter: 0=All, 1=Sale, 2=Purchase, 3=ClientCredit, 4=SupplierCredit, 5=Modification, 6=Deletion
            switch (TypeOperationFilter.SelectedIndex)
            {
                case 1: lo = lo.Where(op => op.OperationType.StartsWith("V", StringComparison.OrdinalIgnoreCase)); break;
                case 2: lo = lo.Where(op => op.OperationType.StartsWith("A", StringComparison.OrdinalIgnoreCase)); break;
                case 3: lo = lo.Where(op => op.OperationType.StartsWith("P", StringComparison.OrdinalIgnoreCase)); break;
                case 4: lo = lo.Where(op => op.OperationType.StartsWith("S", StringComparison.OrdinalIgnoreCase)); break;
                case 5: lo = lo.Where(op => op.OperationType.StartsWith("M", StringComparison.OrdinalIgnoreCase)); break;
                case 6: lo = lo.Where(op => op.OperationType.StartsWith("D", StringComparison.OrdinalIgnoreCase)); break;
                // case 0: All — no filter
            }

            // --- Status filter (index-based) ---
            // StatusFilter: 0=All, 1=Normal, 2=Reversed
            switch (StatusFilter.SelectedIndex)
            {
                case 1: lo = lo.Where(op => !op.Reversed); break;
                case 2: lo = lo.Where(op => op.Reversed);  break;
            }

            // --- Date filter (index-based) ---
            // DateFilter: 0=All, 1=Today, 2=Week, 3=Month, 4=SixMonths, 5=Year
            DateTime today = DateTime.Today;
            switch (DateFilter.SelectedIndex)
            {
                case 1: lo = lo.Where(op => op.DateOperation > today.AddDays(-1));    break;
                case 2: lo = lo.Where(op => op.DateOperation > today.AddDays(-7));    break;
                case 3: lo = lo.Where(op => op.DateOperation > today.AddMonths(-1));  break;
                case 4: lo = lo.Where(op => op.DateOperation > today.AddMonths(-6));  break;
                case 5: lo = lo.Where(op => op.DateOperation > today.AddYears(-1));   break;
            }

            LoadOperations(lo.ToList());
        }

        private void ApplyMouvmentFilters()
        {
            if (!isInitialized) return;

            var query =
                from oa in main.loa
                join o in main.lo on oa.OperationID equals o.OperationID
                select new { OA = oa, O = o };

            // --- Search filter (index-based) ---
            // SearchTypeMouvmentFilter: 0=Article, 1=Client, 2=Supplier, 3=User
            string searchText = SearchMouvmentInput.Text?.Trim() ?? "";
            int searchTypeIndex = SearchTypeMouvmentFilter.SelectedIndex;

            if (!string.IsNullOrEmpty(searchText))
            {
                string lowerSearch = searchText.ToLower();
                switch (searchTypeIndex)
                {
                    case 0: // Article
                        query = from q in query
                                join a in main.laa on q.OA.ArticleID equals a.ArticleID
                                where a.ArticleName != null && a.ArticleName.ToLower().Contains(lowerSearch)
                                select q;
                        break;
                    case 1: // Client
                        query = from q in query
                                join c in main.lc on q.O.ClientID equals c.ClientID
                                where c.Nom != null && c.Nom.ToLower().Contains(lowerSearch)
                                select q;
                        break;
                    case 2: // Supplier
                        query = from q in query
                                join f in main.lfo on q.O.FournisseurID equals f.FournisseurID
                                where f.Nom != null && f.Nom.ToLower().Contains(lowerSearch)
                                select q;
                        break;
                    case 3: // User
                        query = from q in query
                                join u in main.lu on q.O.UserID equals u.UserID
                                where u.UserName != null && u.UserName.ToLower().Contains(lowerSearch)
                                select q;
                        break;
                }
            }

            // --- Movement type filter (index-based) ---
            // TypeMouvmentFilter: 0=All, 1=Sale, 2=Purchase, 3=Modification, 4=Deletion
            switch (TypeMouvmentFilter.SelectedIndex)
            {
                case 1: query = query.Where(q => q.O.OperationType.StartsWith("V", StringComparison.OrdinalIgnoreCase)); break;
                case 2: query = query.Where(q => q.O.OperationType.StartsWith("A", StringComparison.OrdinalIgnoreCase)); break;
                case 3: query = query.Where(q => q.O.OperationType.StartsWith("M", StringComparison.OrdinalIgnoreCase)); break;
                case 4: query = query.Where(q => q.O.OperationType.StartsWith("D", StringComparison.OrdinalIgnoreCase)); break;
            }

            // --- Reversed filter (index-based) ---
            // StatusMouvmentReversedFilter: 0=All, 1=Normal, 2=Reversed
            switch (StatusMouvmentReversedFilter.SelectedIndex)
            {
                case 1: query = query.Where(q => !q.OA.Reversed); break;
                case 2: query = query.Where(q => q.OA.Reversed);  break;
            }

            // --- Article status filter (index-based) ---
            // StatusMouvmenttFilter: 0=All, 1=Normal (active), 2=Supprime (deleted)
            switch (StatusMouvmenttFilter.SelectedIndex)
            {
                case 1: query = query.Where(oa => main.laa.Any(a => a.ArticleID == oa.OA.ArticleID && a.Etat == true));  break;
                case 2: query = query.Where(oa => main.laa.Any(a => a.ArticleID == oa.OA.ArticleID && a.Etat == false)); break;
            }

            // --- Date filter (index-based) ---
            // DateMouvmentFilter: 0=All, 1=Today, 2=Last Week, 3=Last Month, 4=Last 6 Months, 5=Last Year
            DateTime today = DateTime.Today;
            switch (DateMouvmentFilter.SelectedIndex)
            {
                case 1: query = query.Where(q => q.O.DateOperation > today.AddDays(-1));   break;
                case 2: query = query.Where(q => q.O.DateOperation > today.AddDays(-7));   break;
                case 3: query = query.Where(q => q.O.DateOperation > today.AddMonths(-1)); break;
                case 4: query = query.Where(q => q.O.DateOperation > today.AddMonths(-6)); break;
                case 5: query = query.Where(q => q.O.DateOperation > today.AddYears(-1));  break;
            }

            LoadMouvments(query.Select(q => q.OA).ToList());
        }

        private void SearchMouvmentInput_TextChanged(object sender, TextChangedEventArgs e)
            => ApplyMouvmentFilters();

        private void TypeMouvmentFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyMouvmentFilters();

        private void DateMouvmentFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyMouvmentFilters();

        private void StatusMouvmentFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyMouvmentFilters();

        private void ReversedMouvmentFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyMouvmentFilters();

        private void ToutMouvmentButton_Click(object sender, RoutedEventArgs e)
        {
            SearchMouvmentInput.Text = "";
            index2 = 10;
            TypeMouvmentFilter.SelectedIndex = 0;
            StatusMouvmenttFilter.SelectedIndex = 0;
            StatusMouvmentReversedFilter.SelectedIndex = 0;
            SearchTypeFilter.SelectedIndex = 0;
            DateMouvmentFilter.SelectedIndex = 0;

            LoadMouvments(main.loa);
        }

        private void SearchTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            index = index + 10;
            ApplyFilters();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            index2 = index2 + 10;
            ApplyMouvmentFilters();
        }

        private void UserFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
        }
    }
}
