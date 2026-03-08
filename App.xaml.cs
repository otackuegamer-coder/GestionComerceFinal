using Superete;
using System;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace GestionComerce
{
    public partial class App : Application
    {
        private GlobalButtonWindow _globalButton;
        private int _currentUserId;
        private string _keyboardSetting = "Manuel";

        // ── ADD THIS: exposes the current language to any window that needs it ──
        public string CurrentLanguage { get; private set; } = "Français";
        // ─────────────────────────────────────────────────────────────────────────

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ── GLOBAL EXCEPTION HANDLERS ─────────────────────────────────────
            // Catches any unhandled exception from any of the 40+ pages/classes
            // without needing try/catch in every file.
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            // ─────────────────────────────────────────────────────────────────

            // CHECK 1: Handle registration command from installer
            if (e.Args.Length > 0 && e.Args[0] == "/register")
            {
                try { MachineLock.RegisterInstallation(); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Registration failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Current.Shutdown();
                return;
            }

            // CHECK 2: Handle database setup command from installer
            if (e.Args.Length > 0 && e.Args[0] == "/setupdb")
            {
                try { DatabaseSetup.EnsureDatabaseExists(); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database setup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Current.Shutdown();
                return;
            }

            // CHECK 3: Ensure database exists before starting app
            if (!DatabaseSetup.EnsureDatabaseExists())
            {
                MessageBox.Show("Cannot start application without database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            // ─── APPLY SAVED LANGUAGE BEFORE ANYTHING IS SHOWN ───────────────
            ApplyLanguage(GestionComerce.Properties.Settings.Default.AppLanguage);
            // ─────────────────────────────────────────────────────────────────

            // CHECK 4: Normal startup - create main window
            MainWindow main = new MainWindow();
            if (main == null || Current.MainWindow == null) return;

            main.Show();

            // Create global button window
            _globalButton = new GlobalButtonWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Topmost = true,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent
            };

            PositionBottomRight(main, _globalButton);
            _globalButton.Show();

            main.LocationChanged += (s, ev) => PositionBottomRight(main, _globalButton);
            main.SizeChanged += (s, ev) => PositionBottomRight(main, _globalButton);

            main.StateChanged += (s, ev) =>
            {
                if (!_globalButton.IsLoaded) return;
                if (main.WindowState == WindowState.Minimized) _globalButton.Hide();
                else _globalButton.Show();
            };

            main.Closed += (s, ev) =>
            {
                if (_globalButton.IsLoaded) _globalButton.Close();
            };
        }

        public void SetUserForKeyboard(int userId)
        {
            _currentUserId = userId;

            try
            {
                var parametres = Superete.ParametresGeneraux.ObtenirParametresParUserId(
                    userId, "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;");

                if (parametres != null)
                {
                    _keyboardSetting = parametres.AfficherClavier;
                    ApplyLanguage(parametres.Langue);
                }
            }
            catch { }

            if (_globalButton != null)
                _globalButton.SetUser(userId);

            if (_keyboardSetting == "Oui")
            {
                EventManager.RegisterClassHandler(typeof(System.Windows.Controls.TextBox),
                    System.Windows.Controls.TextBox.GotFocusEvent,
                    new RoutedEventHandler(OnTextBoxGotFocus));

                EventManager.RegisterClassHandler(typeof(System.Windows.Controls.PasswordBox),
                    System.Windows.Controls.PasswordBox.GotFocusEvent,
                    new RoutedEventHandler(OnTextBoxGotFocus));

                EventManager.RegisterClassHandler(typeof(System.Windows.Controls.TextBox),
                    System.Windows.Controls.TextBox.PreviewMouseDownEvent,
                    new MouseButtonEventHandler(OnTextBoxMouseDown));

                EventManager.RegisterClassHandler(typeof(System.Windows.Controls.PasswordBox),
                    System.Windows.Controls.PasswordBox.PreviewMouseDownEvent,
                    new MouseButtonEventHandler(OnTextBoxMouseDown));
            }
        }

        /// <summary>
        /// Apply language, expose it via CurrentLanguage, and persist it.
        /// </summary>
        public void ApplyLanguage(string languageName)
        {
            if (string.IsNullOrEmpty(languageName))
                languageName = "Français";

            // ── STORE so any window can read Application.Current as App and check this ──
            CurrentLanguage = languageName;
            // ─────────────────────────────────────────────────────────────────────────────

            CultureInfo culture = GetCultureFromLanguageName(languageName);

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Update the resource class culture so x:Static bindings resolve correctly
            GestionComerce.Resources.Resources.Culture = culture;

            // Persist so the next cold start uses the same language
            GestionComerce.Properties.Settings.Default.AppLanguage = languageName;
            GestionComerce.Properties.Settings.Default.Save();

            RefreshAllWindows();
        }

        /// <summary>Public alias kept for backwards compatibility (called from settings page).</summary>
        public void ChangeLanguage(string languageName) => ApplyLanguage(languageName);

        private CultureInfo GetCultureFromLanguageName(string languageName)
        {
            switch (languageName)
            {
                case "English":
                    return new CultureInfo("en-US");
                case "العربية":
                case "Arabic":
                    return new CultureInfo("ar-MA");
                case "Français":
                case "French":
                default:
                    return new CultureInfo("fr-FR");
            }
        }

        private void RefreshAllWindows()
        {
            if (Current == null) return;

            Current.Dispatcher.Invoke(() =>
            {
                bool isRtl = Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft;

                foreach (Window window in Current.Windows)
                {
                    if (window == null) continue;

                    window.FlowDirection = isRtl
                        ? FlowDirection.RightToLeft
                        : FlowDirection.LeftToRight;

                    var context = window.DataContext;
                    window.DataContext = null;
                    window.DataContext = context;

                    if (window is ILanguageAware languageAware)
                        languageAware.OnLanguageChanged();
                }
            });
        }

        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (_keyboardSetting == "Oui") WKeyboard.ShowKeyboard(_currentUserId);
        }

        private void OnTextBoxMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_keyboardSetting == "Oui") WKeyboard.ShowKeyboard(_currentUserId);
        }

        private void PositionBottomRight(Window main, Window floating)
        {
            var workingArea = SystemParameters.WorkArea;
            if (main.WindowState == WindowState.Maximized)
            {
                floating.Left = workingArea.Right - floating.Width - 10;
                floating.Top = workingArea.Bottom - floating.Height - 10;
            }
            else
            {
                floating.Left = main.Left + main.Width - floating.Width - 10;
                floating.Top = main.Top + main.Height - floating.Height - 10;
            }
        }

        // ── GLOBAL EXCEPTION HANDLER METHODS ─────────────────────────────────

        // UI thread crashes (button clicks, data binding, control events, etc.)
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ShowErrorAndExit(e.Exception);
        }

        // Background thread crashes
        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowErrorAndExit(e.ExceptionObject as Exception);
        }

        // Async/await Task exceptions that were never caught
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            ShowErrorAndExit(e.Exception?.InnerException ?? e.Exception);
        }

        private void ShowErrorAndExit(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[CRASH] {DateTime.Now:HH:mm:ss} | {ex?.GetType().Name}\n{ex}");

            string message = BuildUserMessage(ex);

            // Must run on UI thread — background exceptions come from another thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    message,
                    "Erreur inattendue",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown(1);
            });
        }

        private string BuildUserMessage(Exception ex)
        {
            if (ex == null)
                return "Une erreur inconnue s'est produite.";

            string typeName = ex.GetType().Name;
            string friendly;

            if (typeName == "HttpRequestException")
                friendly = "❌ Impossible de contacter le serveur.\nVérifiez que l'API est démarrée et que le réseau fonctionne.";
            else if (typeName == "TaskCanceledException")
                friendly = "⏱ La requête a expiré.\nLe serveur ne répond pas dans les délais.";
            else if (typeName == "OperationCanceledException")
                friendly = "⏱ Une opération a été annulée (timeout réseau).";
            else if (typeName == "SqlException")
                friendly = "🗄 Erreur de base de données. Contactez l'administrateur.";
            else if (typeName == "UnauthorizedAccessException")
                friendly = "🔒 Accès refusé. Vérifiez vos permissions.";
            else if (typeName == "NullReferenceException")
                friendly = "⚠ Une valeur requise est manquante dans l'application.";
            else if (typeName == "InvalidOperationException")
                friendly = "⚠ Opération invalide : " + ex.Message;
            else
                friendly = "⚠ " + ex.Message;

            return friendly + "\n\n─────────────────────────\nDétail technique :\n" + typeName + ": " + ex.Message;
        }

        // ─────────────────────────────────────────────────────────────────────
    }

    public interface ILanguageAware
    {
        void OnLanguageChanged();
    }
}