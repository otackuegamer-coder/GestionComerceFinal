using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestionComerce.Main.Vente
{
    /// <summary>
    /// Shared helper that shows stock-warning confirmation dialogs.
    /// The "don't remind me" flag is session-only (resets when the app restarts).
    /// </summary>
    public static partial class StockWarningHelper   // ← partial added so CSingleArticle1 can read the flag
    {
        // ── Shared session flag ────────────────────────────────────────────────
        // Becomes true when the user ticks "Ne plus me rappeler pour aucun article"
        // in ANY stock warning dialog (zero-stock or over-stock).
        public static bool IsGloballySuppressed { get; private set; } = false;

        /// <summary>Called by any dialog when the user ticks the global checkbox.</summary>
        public static void SetGloballySuppressed() => IsGloballySuppressed = true;

        // ── Zero-stock dialog ──────────────────────────────────────────────────
        /// <summary>
        /// Shows the zero-stock warning dialog if needed.
        /// Returns true  → user confirmed (or warnings are suppressed).
        /// Returns false → user cancelled.
        /// </summary>
        public static bool ConfirmAddOutOfStock(string articleName)
        {
            // If user already suppressed warnings this session, silently allow
            if (IsGloballySuppressed)
                return true;

            // Build custom dialog
            var dialog = new Window
            {
                Title = "Rupture de stock",
                Width = 460,
                Height = 260,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255))
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ── Message area ─────────────────────────────────────────────────
            var messagePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 20, 20, 10),
                VerticalAlignment = VerticalAlignment.Top
            };

            var icon = new TextBlock
            {
                Text = "⚠️",
                FontSize = 32,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 14, 0)
            };
            messagePanel.Children.Add(icon);

            var textPanel = new StackPanel { Orientation = Orientation.Vertical };
            textPanel.Children.Add(new TextBlock
            {
                Text = $"L'article \"{articleName}\" est en rupture de stock.",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(120, 53, 15)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            textPanel.Children.Add(new TextBlock
            {
                Text = "Si vous continuez :\n  • Sa quantité en stock ne sera PAS réduite.\n  • Il ne sera PAS supprimé automatiquement.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(92, 64, 14)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            });
            messagePanel.Children.Add(textPanel);
            Grid.SetRow(messagePanel, 0);
            root.Children.Add(messagePanel);

            // ── Checkbox row ──────────────────────────────────────────────────
            var checkBox = new CheckBox
            {
                Content = "Ne plus me rappeler pour aucun article (session en cours)",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(20, 0, 20, 12),
                IsChecked = false
            };
            Grid.SetRow(checkBox, 1);
            root.Children.Add(checkBox);

            // ── Button row ────────────────────────────────────────────────────
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 16)
            };

            bool confirmed = false;

            var cancelButton = new Button
            {
                Content = "Annuler",
                Width = 90,
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelButton.Click += (s, e) => { confirmed = false; dialog.Close(); };

            var confirmButton = new Button
            {
                Content = "Ajouter quand même",
                Width = 150,
                Height = 34,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            confirmButton.Click += (s, e) =>
            {
                confirmed = true;
                if (checkBox.IsChecked == true)
                    SetGloballySuppressed();   // ← shared flag, covers all dialogs
                dialog.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(confirmButton);
            Grid.SetRow(buttonPanel, 2);
            root.Children.Add(buttonPanel);

            dialog.Content = root;
            dialog.ShowDialog();
            return confirmed;
        }
    }
}
