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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestionComerce.Main.ProjectManagment
{
    public partial class CSingleMouvment : UserControl
    {
        public CSingleMouvment(CMainP main, OperationArticle opa)
        {
            InitializeComponent();
            this.main = main;
            this.opa = opa;

            // Allow toggling — show "Unreverse" if already reversed
            if (opa.Reversed == true)
            {
                ReverseButton.Content = "Unreverse";
                ReverseButton.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4d4d4d"));
            }

            foreach (Article a in main.main.laa)
            {
                if (a.ArticleID == opa.ArticleID)
                {
                    ArticleName.Text = a.ArticleName;
                    if (a.Etat == false) ArticleName.Text += " (Supprime)";
                    break;
                }
            }
            foreach (Operation o in main.main.lo)
            {
                if (o.OperationID == opa.OperationID)
                {
                    OperationType.Text = o.OperationType + " • ";
                    OperationId.Text = o.OperationID.ToString();

                    if (o.OperationType.StartsWith("A"))
                    {
                        foreach (Fournisseur f in main.main.lfo)
                        {
                            if (o.FournisseurID == f.FournisseurID) { Fournisseur.Text = f.Nom; break; }
                        }
                        IndicatorBorder.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff7614"));
                        IndicatorIcon.Text = "🛒";
                        Quantity.Text = "+ " + opa.QteArticle.ToString();
                    }
                    else if (o.OperationType.StartsWith("V"))
                    {
                        if (o.ClientID != null)
                        {
                            foreach (Client c in main.main.lc)
                            {
                                if (o.ClientID == c.ClientID) { Fournisseur.Text = c.Nom; break; }
                            }
                        }
                        else { Fournisseur.Text = "No Client"; }
                        IndicatorBorder.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#10B981"));
                        IndicatorIcon.Text = "🏷️";
                        Quantity.Text = "- " + opa.QteArticle.ToString();
                    }
                    else if (o.OperationType.StartsWith("M"))
                    {
                        foreach (User u in main.main.lu)
                        {
                            if (o.UserID == u.UserID) { Fournisseur.Text = u.UserName; break; }
                        }
                        IndicatorBorder.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2d42fc"));
                        IndicatorIcon.Text = "✏️";
                        Quantity.Visibility = Visibility.Collapsed;
                        Quantite.Visibility = Visibility.Collapsed;
                    }
                    else if (o.OperationType.StartsWith("D"))
                    {
                        foreach (User u in main.main.lu)
                        {
                            if (o.UserID == u.UserID) { Fournisseur.Text = u.UserName; break; }
                        }
                        IndicatorBorder.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#828181"));
                        IndicatorIcon.Text = "🗑️";
                        Quantity.Visibility = Visibility.Collapsed;
                        Quantite.Visibility = Visibility.Collapsed;
                    }
                    break;
                }
            }
        }

        public CSingleMouvment(CMainR mainR, OperationArticle opa)
        {
            InitializeComponent();
            this.mainR = mainR;
            this.opa = opa;

            // Allow toggling — show "Unreverse" if already reversed
            if (opa.Reversed == true)
            {
                ReverseButton.Content = "Unreverse";
                ReverseButton.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4d4d4d"));
            }

            foreach (Article a in mainR.main.laa)
            {
                if (a.ArticleID == opa.ArticleID)
                {
                    ArticleName.Text = a.ArticleName;
                    if (a.Etat == false) ArticleName.Text += " (Supprime)";
                    break;
                }
            }
            foreach (Operation o in mainR.main.lo)
            {
                if (o.OperationID == opa.OperationID)
                {
                    OperationType.Text = o.OperationType + " • ";
                    OperationId.Text = o.OperationID.ToString();

                    if (o.OperationType.StartsWith("A"))
                    {
                        foreach (Fournisseur f in mainR.main.lfo)
                        {
                            if (o.FournisseurID == f.FournisseurID) { Fournisseur.Text = f.Nom; break; }
                        }
                        IndicatorBorder.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff7614"));
                        IndicatorIcon.Text = "🛒";
                        Quantity.Text = "+ " + opa.QteArticle.ToString();
                    }
                    else if (o.OperationType.StartsWith("V"))
                    {
                        if (o.ClientID != null)
                        {
                            foreach (Client c in mainR.main.lc)
                            {
                                if (o.ClientID == c.ClientID) { Fournisseur.Text = c.Nom; break; }
                            }
                        }
                        else { Fournisseur.Text = "No Client"; }
                        IndicatorBorder.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#10B981"));
                        IndicatorIcon.Text = "🏷️";
                        Quantity.Text = "- " + opa.QteArticle.ToString();
                    }
                    else if (o.OperationType.StartsWith("M"))
                    {
                        foreach (User u in mainR.main.lu)
                        {
                            if (o.UserID == u.UserID) { Fournisseur.Text = u.UserName; break; }
                        }
                        IndicatorBorder.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2d42fc"));
                        IndicatorIcon.Text = "✏️";
                        Quantity.Visibility = Visibility.Collapsed;
                        Quantite.Visibility = Visibility.Collapsed;
                    }
                    else if (o.OperationType.StartsWith("D"))
                    {
                        foreach (User u in mainR.main.lu)
                        {
                            if (o.UserID == u.UserID) { Fournisseur.Text = u.UserName; break; }
                        }
                        IndicatorBorder.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#828181"));
                        IndicatorIcon.Text = "🗑️";
                        Quantity.Visibility = Visibility.Collapsed;
                        Quantite.Visibility = Visibility.Collapsed;
                    }
                    break;
                }
            }
        }

        public CMainP main;
        public CMainR mainR;
        public OperationArticle opa;

        private void ReverseButton_Click(object sender, RoutedEventArgs e)
        {
            WReverseMouvmentConfirmation wReverseMouvmentConfirmation = new WReverseMouvmentConfirmation(this);
            wReverseMouvmentConfirmation.ShowDialog();
        }
    }
}
