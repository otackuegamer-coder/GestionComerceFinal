using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;

namespace GestionComerce
{
    public partial class WKeyboard : Window
    {
        // ─── State ───────────────────────────────────────────────────────────
        private bool isShiftPressed = false;
        private bool isCapsLockOn   = false;
        private bool isDragging     = false;
        private Point dragStartPoint;

        /// <summary>Current keyboard language: "FR", "EN", or "AR"</summary>
        private string _currentLang = "FR";

        private static WKeyboard _instance;
        private static bool _autoShowEnabled = false;
        private int _currentUserId;
        private DispatcherTimer enableTimer;

        // ─── MessageBox-on-top support ───────────────────────────────────────
        private HookProc  _cbtHookProc;          // keep ref — prevents GC collecting the delegate
        private IntPtr    _cbtHookHandle = IntPtr.Zero;
        private IntPtr    _activeDialogHwnd = IntPtr.Zero; // hwnd of the open MessageBox (if any)

        // ─── Singleton helpers ───────────────────────────────────────────────
        public static void ShowKeyboard(int userId = 0)
        {
            if (_instance == null)
            {
                _instance = new WKeyboard();
                _instance.Closed += (s, e) => _instance = null;
                _instance.Show();
            }
            else if (!_instance.IsVisible)
            {
                _instance.Show();
            }
        }

        // ─── Constructor / Loaded ────────────────────────────────────────────
        public WKeyboard()
        {
            InitializeComponent();
            this.SourceInitialized += Window_SourceInitialized;
            this.Loaded += Window_Loaded;

            // Default: FR layout visible, AR hidden
            SetLayout("FR");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            enableTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            enableTimer.Tick += (s, args) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                EnableWindow(hwnd, true);
            };
            enableTimer.Start();
        }

        // ─── Language switching ──────────────────────────────────────────────
        private void BtnLangFR_Click(object sender, RoutedEventArgs e) => SetLayout("FR");
        private void BtnLangEN_Click(object sender, RoutedEventArgs e) => SetLayout("EN");
        private void BtnLangAR_Click(object sender, RoutedEventArgs e) => SetLayout("AR");

        private void SetLayout(string lang)
        {
            _currentLang = lang;

            bool isArabic = lang == "AR";
            LatinKeyboard.Visibility  = isArabic ? Visibility.Collapsed : Visibility.Visible;
            ArabicKeyboard.Visibility = isArabic ? Visibility.Visible   : Visibility.Collapsed;

            // AZERTY for FR, QWERTY for EN (only Q/W and Y differ visually)
            if (!isArabic)
            {
                if (lang == "FR")
                {
                    // AZERTY: Q→A, W→Z, Y→Y
                    KeyQ.Content = "A"; KeyQ.Tag = "a";
                    KeyW.Content = "Z"; KeyW.Tag = "z";
                    KeyY.Content = "Y"; KeyY.Tag = "y";
                }
                else // EN
                {
                    KeyQ.Content = "Q"; KeyQ.Tag = "q";
                    KeyW.Content = "W"; KeyW.Tag = "w";
                    KeyY.Content = "Y"; KeyY.Tag = "y";
                }
            }

            // Update active button style
            var inactive = (Style)FindResource("LangButtonStyle");
            var active   = (Style)FindResource("LangButtonActiveStyle");
            BtnLangFR.Style = lang == "FR" ? active : inactive;
            BtnLangEN.Style = lang == "EN" ? active : inactive;
            BtnLangAR.Style = lang == "AR" ? active : inactive;

            // RTL for Arabic
            this.FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        // ─── Key clicks ──────────────────────────────────────────────────────
        private void Key_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                string tag = button.Tag?.ToString();
                if (string.IsNullOrEmpty(tag)) return;

                switch (tag)
                {
                    case "Shift":    ToggleShift();    return;
                    case "CapsLock": ToggleCapsLock(); return;
                }

                SendCharacterToFocusedControl(tag);
            }
        }

        // ─── Shift / Caps ────────────────────────────────────────────────────
        private void ToggleShift()
        {
            isShiftPressed = !isShiftPressed;
            UpdateShiftButtonAppearance();
        }

        private void ToggleCapsLock()
        {
            isCapsLockOn = !isCapsLockOn;
            UpdateCapsLockButtonAppearance();
        }

        private void UpdateShiftButtonAppearance()
        {
            var color = isShiftPressed
                ? new SolidColorBrush(Color.FromRgb(100, 150, 255))
                : new SolidColorBrush(Color.FromRgb(240, 240, 240));
            ShiftButton1.Background  = color;
            ShiftButton2.Background  = color;
            ArabicShiftButton1.Background = color;
            ArabicShiftButton2.Background = color;
        }

        private void UpdateCapsLockButtonAppearance()
        {
            var color = isCapsLockOn
                ? new SolidColorBrush(Color.FromRgb(100, 150, 255))
                : new SolidColorBrush(Color.FromRgb(240, 240, 240));
            CapsButton.Background       = color;
            ArabicCapsButton.Background = color;
        }

        // ─── Dragging ────────────────────────────────────────────────────────
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.Border  ||
                e.OriginalSource is System.Windows.Controls.Grid    ||
                e.OriginalSource is System.Windows.Controls.TextBlock)
            {
                isDragging    = true;
                dragStartPoint = e.GetPosition(this);
                this.CaptureMouse();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point cur = PointToScreen(e.GetPosition(this));
                this.Left = cur.X - dragStartPoint.X;
                this.Top  = cur.Y - dragStartPoint.Y;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                this.ReleaseMouseCapture();
            }
        }

        // ─── Close ───────────────────────────────────────────────────────────
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            enableTimer?.Stop();
            UninstallCbtHook();
            this.Close();
        }

        // ─── Send key to focused control ─────────────────────────────────────
        private void SendCharacterToFocusedControl(string character)
        {
            IntPtr focusedHandle = GetFocusedControlHandle();
            if (focusedHandle == IntPtr.Zero) return;

            switch (character)
            {
                case "Backspace": SendKeyToHandle(focusedHandle, 0x08); return;
                case "Enter":     SendKeyToHandle(focusedHandle, 0x0D); return;
                case "Tab":       SendKeyToHandle(focusedHandle, 0x09); return;
                case "Space":
                    if (_currentLang == "AR") SendUnicodeCharViaInput(' ');
                    else                      SendUnicodeCharToHandle(focusedHandle, ' ');
                    return;
            }

            // Arabic characters — use SendInput + KEYEVENTF_UNICODE (WM_CHAR is unreliable for Arabic)
            // SendInput sends to the currently focused window at hardware-input level, so
            // focusedHandle is not needed here.
            if (_currentLang == "AR")
            {
                foreach (char c in character)
                    SendUnicodeCharViaInput(c);
                return;
            }

            // Multi-char Latin ligatures (fallback)
            if (character.Length > 1)
            {
                foreach (char c in character)
                    SendUnicodeCharToHandle(focusedHandle, c);
                return;
            }

            // Latin
            char ch = character[0];
            bool upper = isShiftPressed ^ isCapsLockOn;
            if (char.IsLetter(ch))
                ch = upper ? char.ToUpper(ch) : char.ToLower(ch);
            else if (isShiftPressed)
                ch = GetShiftedCharacter(ch);

            SendUnicodeCharToHandle(focusedHandle, ch);

            if (isShiftPressed)
            {
                isShiftPressed = false;
                UpdateShiftButtonAppearance();
            }
        }

        private char GetShiftedCharacter(char ch)
        {
            switch (ch)
            {
                case '`':  return '~';
                case '1':  return '!';
                case '2':  return '@';
                case '3':  return '#';
                case '4':  return '$';
                case '5':  return '%';
                case '6':  return '^';
                case '7':  return '&';
                case '8':  return '*';
                case '9':  return '(';
                case '0':  return ')';
                case '-':  return '_';
                case '=':  return '+';
                case '[':  return '{';
                case ']':  return '}';
                case '\\': return '|';
                case ';':  return ':';
                case '\'': return '"';
                case ',':  return '<';
                case '.':  return '>';
                case '/':  return '?';
                default:   return ch;
            }
        }

        // ─── P/Invoke ────────────────────────────────────────────────────────
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetFocus();
        [DllImport("user32.dll")] private static extern bool   AttachThreadInput(uint a, uint b, bool f);
        // FIX: return value is the thread ID; out param is the process ID
        [DllImport("user32.dll")] private static extern uint   GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] private static extern bool   EnableWindow(IntPtr hWnd, bool b);

        // SendInput — used for Arabic Unicode injection (reliable for all control types)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint      type;
            public InputUnion u;
        }
        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT   ki;
            [FieldOffset(0)] public MOUSEINPUT   mi;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint   dwFlags;
            public uint   time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx, dy, mouseData;
            public uint dwFlags, time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL, wParamH;
        }
        private const uint INPUT_KEYBOARD    = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_KEYUP   = 0x0002;

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

        // CBT hook — thread-local, no unmanaged DLL needed
        private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        private const uint WM_CHAR  = 0x0102;
        private const int  GWL_EXSTYLE   = -20;
        private const int  WS_EX_NOACTIVATE = 0x08000000;
        private const int  WS_EX_TOPMOST    = 0x00000008;
        private static readonly IntPtr HWND_TOPMOST    = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST  = new IntPtr(-2);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOMOVE     = 0x0002;
        private const uint SWP_NOSIZE     = 0x0001;
        // CBT hook codes
        private const int WH_CBT         = 5;
        private const int HCBT_ACTIVATE   = 5;   // a window is about to be activated
        private const int HCBT_DESTROYWND = 4;   // a window is about to be destroyed

        private IntPtr GetFocusedControlHandle()
        {
            IntPtr fgWindow = GetForegroundWindow();
            if (fgWindow == IntPtr.Zero) return IntPtr.Zero;

            // FIX: GetWindowThreadProcessId RETURNS the thread ID; out param is process ID
            uint fgThreadId     = GetWindowThreadProcessId(fgWindow, out uint _);
            uint currentThreadId = GetCurrentThreadId();
            IntPtr focusedHandle = IntPtr.Zero;

            if (fgThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, fgThreadId, true);
                focusedHandle = GetFocus();
                AttachThreadInput(currentThreadId, fgThreadId, false);
            }
            else
            {
                focusedHandle = GetFocus();
            }
            return focusedHandle;
        }

        // Latin path: WM_CHAR via SendMessage (works for same-process WPF controls)
        private void SendUnicodeCharToHandle(IntPtr hWnd, char ch)
            => SendMessage(hWnd, WM_CHAR, (IntPtr)ch, IntPtr.Zero);

        // Arabic path: SendInput with KEYEVENTF_UNICODE
        // This is the only reliable way to inject Arabic/Unicode characters into ANY
        // control type (WPF, WinForms, Win32 EDIT) — WM_CHAR is ANSI-only in many contexts.
        private void SendUnicodeCharViaInput(char ch)
        {
            var inputs = new INPUT[]
            {
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u    = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk        = 0,
                            wScan      = ch,
                            dwFlags    = KEYEVENTF_UNICODE,
                            time       = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                },
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u    = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk        = 0,
                            wScan      = ch,
                            dwFlags    = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                            time       = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void SendKeyToHandle(IntPtr hWnd, ushort vkCode)
        {
            SendMessage(hWnd, 0x0100, (IntPtr)vkCode, IntPtr.Zero); // WM_KEYDOWN
            SendMessage(hWnd, 0x0101, (IntPtr)vkCode, IntPtr.Zero); // WM_KEYUP
        }

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
            => IntPtr.Size == 8 ? GetWindowLong64(hWnd, nIndex) : new IntPtr(GetWindowLong32(hWnd, nIndex));

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr val)
            => IntPtr.Size == 8 ? SetWindowLong64(hWnd, nIndex, val) : new IntPtr(SetWindowLong32(hWnd, nIndex, val.ToInt32()));

        // ─── MessageBox-on-top: CBT hook methods ────────────────────────────
        private void InstallCbtHook()
        {
            _cbtHookProc   = CbtHookProc;   // hold strong ref — GC must not collect this
            _cbtHookHandle = SetWindowsHookEx(WH_CBT, _cbtHookProc,
                                              IntPtr.Zero, GetCurrentThreadId());
        }

        private void UninstallCbtHook()
        {
            if (_cbtHookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_cbtHookHandle);
                _cbtHookHandle = IntPtr.Zero;
            }
        }

        private IntPtr CbtHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                if (code == HCBT_ACTIVATE)
                {
                    // wParam = hwnd of the window being activated
                    var cls = new System.Text.StringBuilder(64);
                    GetClassName(wParam, cls, 64);

                    if (cls.ToString() == "#32770")  // Win32 dialog / MessageBox class
                    {
                        _activeDialogHwnd = wParam;
                        SetKeyboardTopmost(false);   // let the dialog sit above us
                    }
                }
                else if (code == HCBT_DESTROYWND)
                {
                    // wParam = hwnd of the window being destroyed
                    if (wParam == _activeDialogHwnd)
                    {
                        _activeDialogHwnd = IntPtr.Zero;
                        // Restore topmost after the dialog has fully closed
                        Dispatcher.BeginInvoke(new Action(() => SetKeyboardTopmost(true)),
                            System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
            }
            return CallNextHookEx(_cbtHookHandle, code, wParam, lParam);
        }

        private void SetKeyboardTopmost(bool topmost)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd,
                         topmost ? HWND_TOPMOST : HWND_NOTOPMOST,
                         0, 0, 0, 0,
                         SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd   = new WindowInteropHelper(this).Handle;
            var exStyle = new IntPtr(GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt32() | WS_EX_NOACTIVATE | WS_EX_TOPMOST);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(WndProc);

            // Install thread-local CBT hook so MessageBoxes appear above the keyboard
            InstallCbtHook();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            const int MA_NOACTIVATE    = 0x0003;
            const int WM_ENABLE        = 0x000A;

            if (msg == WM_MOUSEACTIVATE) { handled = true; return new IntPtr(MA_NOACTIVATE); }
            if (msg == WM_ENABLE && wParam == IntPtr.Zero)
            {
                EnableWindow(hwnd, true);
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
