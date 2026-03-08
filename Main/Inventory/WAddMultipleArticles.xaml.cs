using Superete;
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

namespace GestionComerce.Main.Inventory
{
    /// <summary>
    /// Interaction logic for WAddMultipleArticles.xaml
    /// </summary>
    public partial class WAddMultipleArticles : Window
    {
        public CMainI main;
        public Fournisseur fo;

        public WAddMultipleArticles(CMainI main)
        {
            InitializeComponent();
            this.main = main;

            foreach (Fournisseur f in main.main.lfo)
            {
                SupplierComboBox.Items.Add(f.Nom);
                fo = main.main.lfo[0];
                SupplierComboBox.SelectedIndex = 0;
            }

            foreach (Role r in main.main.lr)
            {
                if (main.u.RoleID == r.RoleID)
                {
                    if (r.SolderFournisseur == false)
                    {
                        CreditButton.IsEnabled = false;
                        HalfButton.IsEnabled = false;
                    }
                    if (r.CashFournisseur == false)
                    {
                        CashButton.IsEnabled = false;
                    }
                    break;
                }
            }

            LoadPayments(main.main.lp);
            SelectDefaultPaymentMethod();
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

        private int GetSelectedPaymentMethodID()
        {
            if (PaymentMethodComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return (int)selectedItem.Tag;
            }
            return 0;
        }

        private void AddArticleButton_Click(object sender, RoutedEventArgs e)
        {
            if (SupplierComboBox.Text == "")
            {
                MessageBox.Show("Veuillez selectionner un fournisseur ");
                return;
            }

            foreach (Fournisseur f in main.main.lfo)
            {
                if (f.Nom == SupplierComboBox.SelectedValue)
                {
                    fo = f;
                    break;
                }
            }

            WNouveauStock wNouveauStock = new WNouveauStock(main.lf, main.la, main.lfo, main, 5, fo, this);
            wNouveauStock.ShowDialog();
        }

        private void SelectDefaultPaymentMethod()
        {
            try
            {
                var parametres = ParametresGeneraux.ObtenirParametresParUserId(main.u.UserID, "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;");

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

        private void CashButton_Click(object sender, RoutedEventArgs e)
        {
            if (ArticlesContainer.Children.Count == 0)
            {
                MessageBox.Show("There is no Aticles");
                return;
            }
            if (SupplierComboBox.Text == "")
            {
                MessageBox.Show("Veuillez selectionner un fournisseur ");
                return;
            }
            if (PaymentMethodComboBox.SelectedItem == null)
            {
                MessageBox.Show("Veuillez selectionner un mode de paiement, si il y aacun method de payment ajouter la depuis parametres ");
                return;
            }

            int MethodID = GetSelectedPaymentMethodID();
            WConfirmTransaction wConfirmTransaction = new WConfirmTransaction(null, null, this, null, 0, MethodID);
            wConfirmTransaction.ShowDialog();
        }

        private void HalfButton_Click(object sender, RoutedEventArgs e)
        {
            if (ArticlesContainer.Children.Count == 0)
            {
                MessageBox.Show("There is no Aticles");
                return;
            }
            if (SupplierComboBox.Text == "")
            {
                MessageBox.Show("Veuillez selectionner un fournisseur ");
                return;
            }
            if (PaymentMethodComboBox.SelectedItem == null)
            {
                MessageBox.Show("Veuillez selectionner un mode de paiement, si il y aacun method de payment ajouter la depuis parametres ");
                return;
            }

            int MethodID = GetSelectedPaymentMethodID();
            WConfirmTransaction wConfirmTransaction = new WConfirmTransaction(null, null, this, null, 1, MethodID);
            wConfirmTransaction.ShowDialog();
        }

        private void CreditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ArticlesContainer.Children.Count == 0)
            {
                MessageBox.Show("There is no Aticles");
                return;
            }
            if (SupplierComboBox.Text == "")
            {
                MessageBox.Show("Veuillez selectionner un fournisseur ");
                return;
            }
            if (PaymentMethodComboBox.SelectedItem == null)
            {
                MessageBox.Show("Veuillez selectionner un mode de paiement, si il y aacun method de payment ajouter la depuis parametres ");
                return;
            }

            int MethodID = GetSelectedPaymentMethodID();
            WConfirmTransaction wConfirmTransaction = new WConfirmTransaction(null, null, this, null, 2, MethodID);
            wConfirmTransaction.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SupplierComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool thereis = false;
            var combo = sender as ComboBox;

            foreach (Fournisseur f in main.main.lfo)
            {
                if (f.Nom == SupplierComboBox.SelectedValue)
                {
                    fo = f;

                    for (int i = ArticlesContainer.Children.Count - 1; i >= 0; i--)
                    {
                        if (ArticlesContainer.Children[i] is CSingleRowArticle csra)
                        {
                            if (csra.Fournisseur.Text == "Ajout de quantite")
                            {
                                thereis = true;
                            }
                        }
                    }

                    if (thereis == false) return;

                    if (ArticlesContainer.Children.Count > 0)
                    {
                        MessageBoxResult result = MessageBox.Show(
                            "Changement de Fournisseur va supprimer les article de type 'Ajout de quantite', vous voulez Continuer?",
                            "Confirmation",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            for (int i = ArticlesContainer.Children.Count - 1; i >= 0; i--)
                            {
                                if (ArticlesContainer.Children[i] is CSingleRowArticle csra)
                                {
                                    if (csra.Fournisseur.Text == "Ajout de quantite")
                                    {
                                        ArticlesContainer.Children.RemoveAt(i);
                                    }
                                }
                            }
                        }
                        else
                        {
                            SupplierComboBox.SelectionChanged -= SupplierComboBox_SelectionChanged;
                            SupplierComboBox.SelectedValue = e.RemovedItems[0];
                            SupplierComboBox.SelectionChanged += SupplierComboBox_SelectionChanged;
                        }
                        return;
                    }
                }
            }
        }
    }
}