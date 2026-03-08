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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace GestionComerce
{
    /// <summary>
    /// Interaction logic for GlobalButtonWindow.xaml
    /// </summary>
    public partial class GlobalButtonWindow : Window
    {
        private DispatcherTimer enableTimer;
        private int _currentUserId;
        public GlobalButtonWindow()
        {
            InitializeComponent();

            // Position the window at the top-middle of the primary screen
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            this.Left = SystemParameters.WorkArea.Right - this.Width - 20; // 20px from right edge
            this.Top = 100; // Or whatever Y position you want

            this.SourceInitialized += Window_SourceInitialized;
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateKeyboardButtonVisibility(); // Add this line

            // Timer to constantly re-enable the window in case dialogs disable it
            enableTimer = new DispatcherTimer();
            enableTimer.Interval = TimeSpan.FromMilliseconds(100);
            enableTimer.Tick += (s, args) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                EnableWindow(hwnd, true);
            };
            enableTimer.Start();
        }
        public void SetUser(int userId)
        {
            _currentUserId = userId;
            UpdateKeyboardButtonVisibility();
        }
        private void UpdateKeyboardButtonVisibility()
        {
            try
            {
                var parametres = Superete.ParametresGeneraux.ObtenirParametresParUserId(_currentUserId, "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;");

                if (parametres != null)
                {
                    string setting = parametres.AfficherClavier;

                    if (setting == "Non")
                    {
                        KeyboardButton.Visibility = Visibility.Collapsed;
                    }
                    else // "Oui" or "Manuel"
                    {
                        KeyboardButton.Visibility = Visibility.Visible;
                    }
                }
            }
            catch
            {
                // Default to visible if settings can't be loaded
                KeyboardButton.Visibility = Visibility.Visible;
            }
        }
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            // Set NOACTIVATE and TOPMOST flags
            IntPtr exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            exStyle = new IntPtr(exStyle.ToInt32() | WS_EX_NOACTIVATE | WS_EX_TOPMOST);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);

            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            const int MA_NOACTIVATE = 0x0003;
            const int WM_ENABLE = 0x000A;

            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
            }

            // If Windows tries to disable us (because of a modal dialog), re-enable immediately
            if (msg == WM_ENABLE)
            {
                if (wParam == IntPtr.Zero) // Being disabled
                {
                    EnableWindow(hwnd, true);
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void GlobalButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            WKeyboard.ShowKeyboard(_currentUserId);
        }

        private void KeyboardButton_MouseEnter(object sender, MouseEventArgs e)
        {
            var sb = (Storyboard)KeyboardButton.Resources["ExpandAnimation"];
            sb.Begin(KeyboardButton);
        }

        private void KeyboardButton_MouseLeave(object sender, MouseEventArgs e)
        {
            var sb = (Storyboard)KeyboardButton.Resources["CollapseAnimation"];
            sb.Begin(KeyboardButton);
        }

        #region P/Invoke
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLong64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLong64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLong64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLong64(hWnd, nIndex);
            else
                return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }
        #endregion
    }
}