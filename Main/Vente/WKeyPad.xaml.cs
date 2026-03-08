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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GestionComerce.Main.Vente
{
    /// <summary>
    /// Interaction logic for WKeyPad.xaml
    /// </summary>
    public partial class WKeyPad : Window
    {
        public WKeyPad(CMainV main)
        {
            InitializeComponent();
            this.main = main;
            Key0.Click += Key_Click;
            Key1.Click += Key_Click;
            Key2.Click += Key_Click;
            Key3.Click += Key_Click;
            Key4.Click += Key_Click;
            Key5.Click += Key_Click;
            Key6.Click += Key_Click;
            Key7.Click += Key_Click;
            Key8.Click += Key_Click;
            Key9.Click += Key_Click;

            KeyBackspace.Click += KeyBackspace_Click;
            KeyDot.Click += KeyDot_Click;
        }
        CMainV main;

        private void Key_Click(object sender, RoutedEventArgs e)
        {
            main.ArticleQuantity.Text = main.ArticleQuantity.Text.Length > 9 ? main.ArticleQuantity.Text.Substring(0, 9) : main.ArticleQuantity.Text;
            if (sender is Button btn)
            {
                main.ArticleQuantity.Text += btn.Content.ToString();
            }
        }

        // Backspace
        private void KeyBackspace_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(main.ArticleQuantity.Text))
            {
                main.ArticleQuantity.Text = main.ArticleQuantity.Text.Substring(0, main.ArticleQuantity.Text.Length - 1);
            }
        }
        private void KeyDot_Click(object sender, RoutedEventArgs e)
        {
            if (main.ArticleQuantity.Text.Equals(""))
            {
                main.ArticleQuantity.Text = "0";
            }
            this.Close();
        }
    }
}
