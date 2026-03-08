using GestionComerce.Main;
using GestionComerce.Main.ClientPage;
using GestionComerce.Main.Delivery;
using GestionComerce.Main.Facturation;
using GestionComerce.Main.Facturation.CreateFacture;
using GestionComerce.Main.FournisseurPage;
using GestionComerce.Main.Inventory;
using GestionComerce.Main.ProjectManagment;
using GestionComerce.Main.Settings;
using GestionComerce.Main.Vente;
using Superete;
using Superete.Main.Comptabilite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestionComerce
{
    public partial class MainWindow : Window
    {
        public static readonly HttpClient ApiClient = new HttpClient();
        private Login loginPage;

        // ── Connection monitor ────────────────────────────────────────────────
        // Change this URL to any lightweight endpoint your API exposes.
        // A HEAD request to the root or /ping is ideal.
        // Using the FactureController base because it's already in the project.
        private const string HealthCheckUrl = "http://localhost:5050/api/facture";
        private ConnectionMonitor _connectionMonitor;

        public MainWindow()
        {
            // STEP 1: Check if running on authorized machine
            if (!MachineLock.ValidateInstallation())
            {
                System.Windows.MessageBox.Show(
                    "This application is not properly installed on this computer.\n\n" +
                    "Please install the application using the official installer.\n\n" +
                    "Copying the executable to another machine is not allowed.",
                    "Installation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                System.Windows.Application.Current.Shutdown();
                return;
            }

            // STEP 2: Check expiry date
            DateTime expiryDate = new DateTime(2030, 3, 5, 0, 0, 0);
            if (DateTime.Now > expiryDate)
            {
                System.Windows.MessageBox.Show(
                    "This version has expired.",
                    "Expired",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                System.Windows.Application.Current.Shutdown();
                return;
            }

            // STEP 3: Continue normal initialization
            InitializeComponent();
            MainGrid.Children.Clear();
            loginPage = new Login(this);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            MainGrid.Children.Add(loginPage);
            this.Closed += MainWindow_Closed;

            // STEP 4: Start background connection monitor
            StartConnectionMonitor();
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONNECTION MONITOR
        // ─────────────────────────────────────────────────────────────────────

        private void StartConnectionMonitor()
        {
            _connectionMonitor = new ConnectionMonitor(HealthCheckUrl)
            {
                Interval = TimeSpan.FromSeconds(5),
                Timeout = TimeSpan.FromSeconds(4)
            };

            _connectionMonitor.StateChanged += OnConnectionStateChanged;
            _connectionMonitor.Start();
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            // Already on UI thread — monitor dispatches for us
            switch (state)
            {
                case ConnectionState.NoInternet:
                    BannerIcon.Text = "📶";
                    BannerMessage.Text = "Pas de connexion Internet — vérifiez votre Wi-Fi ou réseau";
                    BannerSpinner.Visibility = Visibility.Visible;
                    ShowBanner(offline: true);
                    break;

                case ConnectionState.ApiDown:
                    BannerIcon.Text = "🖥";
                    BannerMessage.Text = "Internet OK — serveur API inaccessible (problème de notre côté)";
                    BannerSpinner.Visibility = Visibility.Visible;
                    ShowBanner(offline: true);
                    break;

                case ConnectionState.Online:
                    BannerIcon.Text = "✅";
                    BannerMessage.Text = "Connexion rétablie";
                    BannerSpinner.Visibility = Visibility.Collapsed;
                    ShowBanner(offline: false);

                    // Auto-hide after 3 s
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(3)
                    };
                    timer.Tick += (_, __) => { timer.Stop(); HideBanner(); };
                    timer.Start();
                    break;
            }
        }

        private void ShowBanner(bool offline)
        {
            ConnectionBanner.Background = offline
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))   // red
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));  // green

            ConnectionBanner.Visibility = Visibility.Visible;

            // Animate height from 0 to 40
            var anim = new DoubleAnimation(0, 40, TimeSpan.FromMilliseconds(200));
            ConnectionBanner.BeginAnimation(HeightProperty, anim);
        }

        private void HideBanner()
        {
            var anim = new DoubleAnimation(40, 0, TimeSpan.FromMilliseconds(200));
            anim.Completed += (_, __) => ConnectionBanner.Visibility = Visibility.Collapsed;
            ConnectionBanner.BeginAnimation(HeightProperty, anim);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DATA LISTS
        // ─────────────────────────────────────────────────────────────────────

        public List<User> lu;
        public List<Role> lr;
        public List<Famille> lf;
        public List<Article> la;
        public List<Article> laa;
        public List<Fournisseur> lfo;
        public List<Client> lc;
        public List<Operation> lo;
        public List<OperationArticle> loa;
        public List<Credit> credits;
        public List<PaymentMethod> lp;

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _connectionMonitor?.Stop();

            foreach (Window w in Application.Current.Windows)
            {
                if (w != this)
                    w.Close();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PAGE LOADERS  (unchanged)
        // ─────────────────────────────────────────────────────────────────────

        public void load_facture(User u, Operation op)
        {
            MainGrid.Children.Clear();
            CMainIn factureControl = new CMainIn(u, this, op);
            factureControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            factureControl.VerticalAlignment = VerticalAlignment.Stretch;
            factureControl.Margin = new Thickness(0);
            MainGrid.Children.Add(factureControl);
        }

           public async void load_main(User u)
        {
            try
            {
                // Set JWT bearer token on the shared ApiClient for all API calls
                MainWindow.ApiClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", u.Token);

                // Also set it on the static Operation/OperationArticle HTTP client
                Operation.BearerToken = u.Token;

                List<User> lu = await u.GetUsersAsync(MainWindow.ApiClient);
                Role r = new Role();
                List<Role> lr = await r.GetRolesAsync();
                Famille f = new Famille();
                List<Famille> lf = await f.GetFamillesAsync();
                Article a = new Article();
                List<Article> la = await a.GetArticlesAsync();
                List<Article> laa = await a.GetAllArticlesAsync();
                Fournisseur fo = new Fournisseur();
                List<Fournisseur> lfo = await fo.GetFournisseursAsync();
                Client c = new Client();
                List<Client> lc = await c.GetClientsAsync();
                List<Operation> lo = await (new Operation()).GetOperationsAsync();
                List<OperationArticle> loa = await OperationArticle.GetAllOperationArticlesAsync();
                List<PaymentMethod> lp = await (new PaymentMethod()).GetPaymentMethodsAsync();

                foreach (OperationArticle oa in loa)
                {
                    foreach (Operation o in lo)
                    {
                        if (o.OperationID == oa.OperationID)
                        {
                            oa.Date = o.DateOperation;
                            break;
                        }
                    }
                }
                loa = loa.OrderByDescending(oa => oa.Date).ToList();

                List<Credit> credits = await (new Credit()).GetCreditsAsync();

                this.lu = lu;
                this.lr = lr;
                this.lf = lf;
                this.la = la;
                this.laa = laa;
                this.lfo = lfo;
                this.lc = lc;
                this.lo = lo;
                this.loa = loa;
                this.lp = lp;
                this.credits = credits;

                MainGrid.Children.Clear();
                CMain mainPage = new CMain(this, u);
                mainPage.HorizontalAlignment = HorizontalAlignment.Stretch;
                mainPage.VerticalAlignment = VerticalAlignment.Stretch;
                mainPage.Margin = new Thickness(0);
                MainGrid.Children.Add(mainPage);

                var app = Application.Current as App;
                if (app != null)
                {
                    app.SetUserForKeyboard(u.UserID);
                }
            }
            catch (HttpRequestException)
            {
                MessageBox.Show(
                    "❌ Impossible de charger les données.\nVérifiez que le serveur API est démarré.",
                    "Erreur de connexion",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors du chargement : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        

        public void load_settings(User u)
        {
            MainGrid.Children.Clear();
            SettingsPage loginPage = new SettingsPage(u, lu, lr, lf, this);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            MainGrid.Children.Add(loginPage);
        }

        public void load_vente(User u, List<Article> la)
        {
            MainGrid.Children.Clear();
            CMainV loginPage = new CMainV(u, lf, lu, lr, this, la, lfo);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            MainGrid.Children.Add(loginPage);
        }

        public void load_inventory(User u)
        {
            MainGrid.Children.Clear();
            CMainI loginPage = new CMainI(u, la, lf, lfo, this);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            MainGrid.Children.Add(loginPage);
        }

        public void load_fournisseur(User u)
        {
            MainGrid.Children.Clear();
            CMainF loginPage = new CMainF(u, this);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            MainGrid.Children.Add(loginPage);
        }

        public void load_client(User u)
        {
            MainGrid.Children.Clear();
            CMainC loginPage = new CMainC(u, this);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            MainGrid.Children.Add(loginPage);
        }

        public void load_ProjectManagement(User u)
        {
            MainGrid.Children.Clear();
            CMainP loginPage = new CMainP(u, this);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            MainGrid.Children.Add(loginPage);
        }

        public void load_Login()
        {
            MainGrid.Children.Clear();
            Login loginPage = new Login(this);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            MainGrid.Children.Add(loginPage);
        }

        public void load_facturation(User u)
        {
            MainGrid.Children.Clear();
            CMainIn loginPage = new CMainIn(u, this, null);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            MainGrid.Children.Add(loginPage);
        }

        public void load_accountibility(User u)
        {
            MainGrid.Children.Clear();
            CMainCo loginPage = new CMainCo(u, this);
            loginPage.HorizontalAlignment = HorizontalAlignment.Stretch;
            loginPage.VerticalAlignment = VerticalAlignment.Stretch;
            loginPage.Margin = new Thickness(0);
            MainGrid.Children.Add(loginPage);
        }

        public void load_livraison(User u)
        {
            Main.Delivery.CLivraison livraison = new Main.Delivery.CLivraison(this, u);
            MainGrid.Children.Add(livraison);
        }
    }
}