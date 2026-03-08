using Superete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GestionComerce.Main.Inventory
{
    /// <summary>
    /// Interaction logic for WAjoutQuantite.xaml
    /// </summary>
    public partial class WAjoutQuantite : Window
    {
        public Article a;
        public CSingleRowArticle sa;
        int s;
        public WNouveauStock ns;
        public int qte;

        public WAjoutQuantite(CSingleRowArticle sa, int s, WNouveauStock ns)
        {
            InitializeComponent();
            this.a = sa.a;
            this.sa = sa;
            this.s = s;
            this.ns = ns;

            if (s == 5)
            {
                TicketCheckBox.Visibility = Visibility.Collapsed;
                ButtonsContainer.Visibility = Visibility.Collapsed;
                AjouterButton.Visibility = Visibility.Visible;
            }

            LoadPayments(ns.main.main.lp);
            SelectDefaultPaymentMethod();

            foreach (Role r in ns.main.main.lr)
            {
                if (ns.main.u.RoleID == r.RoleID)
                {
                    if (r.SolderFournisseur == false)
                    {
                        HalfButton.IsEnabled = false;
                        CreditButton.IsEnabled = false;
                    }
                    if (r.CashFournisseur == false)
                    {
                        CashButton.IsEnabled = false;
                    }
                    break;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void LoadPayments(List<PaymentMethod> lp)
        {
            PaymentMethodComboBox.Items.Clear();
            foreach (PaymentMethod pm in lp)
            {
                ComboBoxItem item = new ComboBoxItem
                {
                    Content = pm.PaymentMethodName,
                    Tag = pm.PaymentMethodID
                };
                PaymentMethodComboBox.Items.Add(item);
            }
        }

        private void SelectDefaultPaymentMethod()
        {
            try
            {
                var parametres = ParametresGeneraux.ObtenirParametresParUserId(ns.main.u.UserID, "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;");

                if (parametres != null && !string.IsNullOrEmpty(parametres.MethodePaiementParDefaut))
                {
                    for (int i = 0; i < PaymentMethodComboBox.Items.Count; i++)
                    {
                        if (PaymentMethodComboBox.Items[i] is ComboBoxItem item)
                        {
                            if (item.Content.ToString() == parametres.MethodePaiementParDefaut)
                            {
                                PaymentMethodComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If it fails, just leave it unselected
            }
        }

        private int GetSelectedPaymentMethodID()
        {
            if (PaymentMethodComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return (int)selectedItem.Tag;
            }
            return 0;
        }

        private void CashButton_Click(object sender, RoutedEventArgs e)
        {
            if (Quantite.Text != "")
            {
                if (Convert.ToInt32(Quantite.Text) == 0)
                {
                    MessageBox.Show("s'il vous plais donner une quantite");
                    return;
                }
            }
            else
            {
                MessageBox.Show("s'il vous plais donner une quantite");
                return;
            }

            qte = Convert.ToInt32(Quantite.Text);
            int MethodID = GetSelectedPaymentMethodID();
            WConfirmTransaction wConfirmTransaction = new WConfirmTransaction(null, this, null, a, 0, MethodID);
            wConfirmTransaction.ShowDialog();
        }

        private void HalfButton_Click(object sender, RoutedEventArgs e)
        {
            if (Quantite.Text != "")
            {
                if (Convert.ToInt32(Quantite.Text) == 0)
                {
                    MessageBox.Show("s'il vous plais donner une quantite");
                    return;
                }
            }
            else
            {
                MessageBox.Show("s'il vous plais donner une quantite");
                return;
            }

            qte = Convert.ToInt32(Quantite.Text);
            int MethodID = GetSelectedPaymentMethodID();
            WConfirmTransaction wConfirmTransaction = new WConfirmTransaction(null, this, null, a, 1, MethodID);
            wConfirmTransaction.ShowDialog();
        }

        private void CreditButton_Click(object sender, RoutedEventArgs e)
        {
            if (Quantite.Text != "")
            {
                if (Convert.ToInt32(Quantite.Text) == 0)
                {
                    MessageBox.Show("s'il vous plais donner une quantite");
                    return;
                }
            }
            else
            {
                MessageBox.Show("s'il vous plais donner une quantite");
                return;
            }

            qte = Convert.ToInt32(Quantite.Text);
            int MethodID = GetSelectedPaymentMethodID();
            WConfirmTransaction wConfirmTransaction = new WConfirmTransaction(null, this, null, a, 2, MethodID);
            wConfirmTransaction.ShowDialog();
        }

        private void EnregistrerButton_Click(object sender, RoutedEventArgs e)
        {
            CSingleRowArticle cSingleRowArticle = new CSingleRowArticle(a, ns.main.la, null, ns.main, 6, sa.ea, ns, Convert.ToInt32(Quantite.Text));

            if (s == 5)
            {
                foreach (CSingleRowArticle csra in ns.AMA.ArticlesContainer.Children)
                {
                    if (csra.a.ArticleID == a.ArticleID)
                    {
                        csra.Quantite.Text = "x" + (Convert.ToInt32(Quantite.Text) + Convert.ToInt32(csra.Quantite.Text.Substring(1))).ToString();
                        cSingleRowArticle.ea.Close();
                        ns.Close();
                        this.Close();
                        return;
                    }
                }
            }

            ns.AMA.ArticlesContainer.Children.Add(cSingleRowArticle);
            cSingleRowArticle.ea.Close();
            ns.Close();
            this.Close();
        }

        private static readonly Regex _regex = new Regex("^[0-9]+$"); // Only numbers

        private void Quantite_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_regex.IsMatch(e.Text);
        }

        private void Quantite_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!_regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void Quantite_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}