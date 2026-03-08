using GestionComerce.Main.Facturation;
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

namespace GestionComerce.Main.ProjectManagment
{
    /// <summary>
    /// Interaction logic for WPlus.xaml
    /// </summary>
    public partial class WPlus : Window
    {
        public WPlus(CSingleOperation so)
        {
            InitializeComponent();
            this.so = so;
            TotalPrice.Text = so.op.PrixOperation.ToString("0.00") + " DH";
            OperationDate.Text = so.op.DateOperation.ToString();
            OperationId.Text=so.op.OperationID.ToString();
            Remise.Text=so.op.Remise.ToString();
            ArticlesTotalValue.Text= "Total : "+so.op.PrixOperation.ToString("0.00") + " DH";
            Total.Text = "Sous-Total : "+so.op.PrixOperation.ToString();
            Remise2.Text="Remise : -"+so.op.Remise.ToString("0.00")+" DH";
            TotalFinal.Text = "Total Final : "+(so.op.PrixOperation - so.op.Remise).ToString();
            CreditValueLabel.Visibility = Visibility.Collapsed;
            CreditValue.Visibility = Visibility.Collapsed;
            foreach (Role r in so.main.main.lr)
            {
                if (so.main.u.RoleID == r.RoleID)
                {
                    if (r.ReverseOperation == false)
                    {
                        ReverseAritcles.IsEnabled = false;
                    }
                    break;
                }
            }
            foreach (User u in so.main.main.lu)
            {
                if (so.op.UserID == u.UserID)
                {
                    UserName.Text = u.UserName;
                    break;
                }
            }
            if (so.op.OperationType.StartsWith("V"))
            {
                Imprimer.Visibility = Visibility.Visible;
                IconColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#10B981"));
                Start.Text = "V";
                OperationType1.Text = "Vente #" + so.op.OperationID.ToString();
                OperationType2.Text = "Vente";
                foreach(PaymentMethod p in so.main.main.lp)
                {
                    if(p.PaymentMethodID == so.op.PaymentMethodID)
                    {
                        PaymentType.Text = p.PaymentMethodName;
                    }
                }
                if (so.op.OperationType.EndsWith("Ca"))
                {
                    Transaction.Text = "Cash";
                }else if (so.op.OperationType.EndsWith("50"))
                {
                    Transaction.Text = "50/50";
                    RRightSideContainer.Width = new GridLength(1, GridUnitType.Star);
                    CreditValueLabel.Visibility = Visibility.Visible;
                    CreditValue.Visibility = Visibility.Visible;
                    CreditValue.Text = so.op.CreditValue.ToString();
                }
                else if(so.op.OperationType.EndsWith("Cr"))
                {
                    Transaction.Text = "Credit";
                }
                foreach (Client c in so.main.main.lc)
                {
                    if (so.op.ClientID == c.ClientID)
                    {
                        FC.Text = "Client";
                        FCName.Text = c.Nom;
                        break;
                    }
                }
                if (FCName.Text == "")
                {
                    FC.Visibility = Visibility.Collapsed;
                    FCName.Visibility = Visibility.Collapsed;
                }

            }
            else if (so.op.OperationType.StartsWith("A"))
            {
                IconColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff7614"));
                Start.Text = "A";
                OperationType1.Text = "Achat #" + so.op.OperationID.ToString();
                OperationType2.Text = "Achat";
                foreach (PaymentMethod p in so.main.main.lp)
                {
                    if (p.PaymentMethodID == so.op.PaymentMethodID)
                    {
                        PaymentType.Text = p.PaymentMethodName;
                    }
                }
                if (so.op.OperationType.EndsWith("Ca"))
                {
                    Transaction.Text = "Cash";
                }
                else if (so.op.OperationType.EndsWith("50"))
                {
                    Transaction.Text = "50/50";
                    RRightSideContainer.Width = new GridLength(1, GridUnitType.Star);
                    CreditValueLabel.Visibility = Visibility.Visible;
                    CreditValue.Visibility = Visibility.Visible;
                    CreditValue.Text = so.op.CreditValue.ToString();
                }
                else if (so.op.OperationType.EndsWith("Cr"))
                {
                    Transaction.Text = "Credit";
                }
                foreach (Fournisseur f in so.main.main.lfo)
                {
                    if (so.op.FournisseurID == f.FournisseurID)
                    {
                        FC.Text = "Fournisseur";
                        FCName.Text = f.Nom;
                        break;
                    }
                }
            }
            else if (so.op.OperationType.StartsWith("M"))
            {
                IconColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2d42fc"));
                Start.Text = "M";
                OperationType1.Text = "Modification #" + so.op.OperationID.ToString();
                OperationType2.Text = "Modification";
                PaymentTypeLabel.Visibility = Visibility.Collapsed;
                PaymentType.Visibility = Visibility.Collapsed;
                FC.Visibility = Visibility.Collapsed;
                FCName.Visibility = Visibility.Collapsed;
                ArticleOperation.Visibility = Visibility.Collapsed;
                TotalPriceLabel.Text = "Article Id";
                RemiseLabel.Text = "Quantite Avant Modification";
                ArticlesCountLabel.Visibility = Visibility.Collapsed;   
                ArticlesCount.Visibility = Visibility.Collapsed;
                ReverseAritcles.Content = "Reverse";
                RightSideContainer.Width = new GridLength(0);
                this.Height= 520;
                this.Width = 600;

            }
            else if (so.op.OperationType.StartsWith("D"))
            {
                IconColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff3224"));
                Start.Text = "S";
                OperationType1.Text = "Suppression #" + so.op.OperationID.ToString();
                OperationType2.Text = "Suppression";
                PaymentTypeLabel.Visibility = Visibility.Collapsed;
                PaymentType.Visibility = Visibility.Collapsed;
                FC.Visibility = Visibility.Collapsed;
                FCName.Visibility = Visibility.Collapsed;
                ArticleOperation.Visibility = Visibility.Collapsed;
                TotalPriceLabel.Text = "Article Id";
                RemiseLabel.Visibility = Visibility.Collapsed;
                Remise.Visibility = Visibility.Collapsed;
                ArticlesCountLabel.Visibility = Visibility.Collapsed;
                ArticlesCount.Visibility = Visibility.Collapsed;
                ReverseAritcles.Content = "Reverse";
                RightSideContainer.Width = new GridLength(0);
                this.Height = 520;
                this.Width = 600;

            }
            else if (so.op.OperationType.StartsWith("S"))
            {
                IconColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d3f705"));
                Start.Text = "P";
                OperationType1.Text = "Payement de Credit Fournisseur #" + so.op.OperationID.ToString();
                OperationType2.Text = "Payement de Credit Fournisseur";
                foreach (Fournisseur f in so.main.main.lfo)
                {
                    if (so.op.FournisseurID == f.FournisseurID)
                    {
                        RemiseLabel.Text = "Fournisseur";
                        Remise.Text = f.Nom;
                        break;
                    }
                }
                PaymentTypeLabel.Visibility = Visibility.Collapsed;
                PaymentType.Visibility = Visibility.Collapsed;
                ArticleOperation.Visibility = Visibility.Collapsed;
                TotalPriceLabel.Text = "Payement Value";
                ArticlesCountLabel.Visibility = Visibility.Collapsed;
                ArticlesCount.Visibility = Visibility.Collapsed;
                ReverseAritcles.Content = "Reverse";
                RightSideContainer.Width = new GridLength(0);

                this.Height = 520;
                this.Width = 600;

            }
            else if (so.op.OperationType.StartsWith("P"))
            {
                IconColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d3f705"));
                Start.Text = "P";
                OperationType1.Text = "Payement de Credit Client#" + so.op.OperationID.ToString();
                OperationType2.Text = "Payement de Credit Client";
                PaymentTypeLabel.Visibility = Visibility.Collapsed;
                PaymentType.Visibility = Visibility.Collapsed;
                foreach (Client c in so.main.main.lc)
                {
                    if (so.op.ClientID == c.ClientID)
                    {
                        RemiseLabel.Text = "Client";
                        Remise.Text = c.Nom;
                        break;
                    }
                }
                ArticleOperation.Visibility = Visibility.Collapsed;
                TotalPriceLabel.Text = "Payement Value";
                ArticlesCountLabel.Visibility = Visibility.Collapsed;
                ArticlesCount.Visibility = Visibility.Collapsed;
                ReverseAritcles.Content = "Reverse";
                RightSideContainer.Width = new GridLength(0);
                this.Height = 520;
                this.Width = 600;

            }
            LoadArticles();
            ArticlesCount.Text = count.ToString()+" articles";
            if (so.op.Reversed == true)
            {
                IconColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#828181"));
                OperationType1.Text += " (Reversed)";
                // Keep button visible so user can unreverse — just update the label
                ReverseAritcles.Content = "Unreverse Articles";
                ReverseAritcles.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4d4d4d"));
            }
        }
        public CSingleOperation so;int count;

        public void LoadArticles()
        {
            ArticlesContainer.Children.Clear();
            foreach (OperationArticle oa in so.main.main.loa)
            {

                if (oa.OperationID == so.op.OperationID)
                {
                    count++;
                    CSingleArticle cSingleArticle = new CSingleArticle(this, oa);
                    ArticlesContainer.Children.Add(cSingleArticle);
                    if (so.op.OperationType.StartsWith("M"))
                    {
                        TotalPrice.Text = oa.ArticleID.ToString();
                        Remise.Text = oa.QteArticle.ToString();
                    }
                    else if (so.op.OperationType.StartsWith("D"))
                    {
                        TotalPrice.Text = oa.ArticleID.ToString();
                    }
                }
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void ReverseAritcles_Click(object sender, RoutedEventArgs e)
        {
            if (so.op.OperationType.StartsWith("A") || so.op.OperationType.StartsWith("V"))
            {
                WArticlesReverse wArticlesReverse = new WArticlesReverse(this);
                wArticlesReverse.ShowDialog();
            } else if(so.op.OperationType.StartsWith("M") || so.op.OperationType.StartsWith("D") || so.op.OperationType.StartsWith("S") || so.op.OperationType.StartsWith("P"))
            {
                WReverseConfirmation wReverseConfirmation = new WReverseConfirmation(this, null);
                wReverseConfirmation.ShowDialog();
            }
                
        }

        // Replace the existing Imprimer_Click method in WPlus.xaml.cs with this:

        private void Imprimer_Click(object sender, RoutedEventArgs e)
        {
            // Use the centralized load_facture method from MainWindow
            so.main.main.load_facture(so.main.u, so.op);
            this.Close();
        }
    }
}
