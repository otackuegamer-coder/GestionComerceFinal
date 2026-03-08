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
    /// <summary>
    /// Interaction logic for CSingleOperation.xaml
    /// </summary>
    public partial class CSingleOperation : UserControl
    {
        public CSingleOperation(CMainP main, Operation op)
        {
            InitializeComponent();
            this.main = main;
            this.op = op;

            OperationPrice.Text = op.PrixOperation.ToString("0.00") + " DH";
            OperationDate.Text = op.DateOperation.ToString();
            if (op.OperationType.StartsWith("V"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#10B981"));
                OperationType.Text = "Vente #" + op.OperationID.ToString();
                foreach (Client c in main.main.lc)
                {
                    if (op.ClientID == c.ClientID)
                    {
                        OperationName.Text = "Client : " + c.Nom;
                        break;
                    }
                }

            }
            else if (op.OperationType.StartsWith("A"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff7614"));
                OperationType.Text = "Achat #" + op.OperationID.ToString();
                foreach (Fournisseur f in main.main.lfo)
                {
                    if (op.FournisseurID == f.FournisseurID)
                    {
                        OperationName.Text = "Fournisseur : " + f.Nom;
                        break;
                    }
                }
            }
            else if (op.OperationType.StartsWith("M"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2d42fc"));
                OperationType.Text = "Modification #" + op.OperationID.ToString();
                foreach (User u in main.main.lu)
                {
                    if (op.UserID == u.UserID)
                    {
                        OperationName.Text = "User : " + u.UserName;
                        break;
                    }
                }
            }
            else if (op.OperationType.StartsWith("D"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff3224"));
                OperationType.Text = "Suppression #" + op.OperationID.ToString();
                foreach (User u in main.main.lu)
                {
                    if (op.UserID == u.UserID)
                    {
                        OperationName.Text = "User : " + u.UserName;
                        break;
                    }
                }
            }
            else if (op.OperationType.StartsWith("S"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d3f705"));
                OperationType.Text = "Payement de Credit Fournisseur #" + op.OperationID.ToString();
                foreach (Fournisseur f in main.main.lfo)
                {
                    if (op.FournisseurID == f.FournisseurID)
                    {
                        OperationName.Text = "Fournisseur : " + f.Nom;
                        break;
                    }
                }
            }
            else if (op.OperationType.StartsWith("L"))
            {
                OperationType.Text = "📦 Livraison Groupée";
                OperationType.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6"));
            }
            else if (op.OperationType.StartsWith("P"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d3f705"));
                OperationType.Text = "Payement de Credit Client#" + op.OperationID.ToString();
                foreach (Client c in main.main.lc)
                {
                    if (op.ClientID == c.ClientID)
                    {
                        OperationName.Text = "Client : " + c.Nom;
                        break;
                    }
                }
            }

            foreach (OperationArticle oa in main.main.loa)
            {
                if (op.Reversed == true) break;
                if (oa.OperationID == op.OperationID)
                {
                    if (oa.Reversed == true)
                    {
                        reversed++;
                    }
                    total++;
                }
            }

            if (total == reversed && !op.OperationType.StartsWith("S") && !op.OperationType.StartsWith("P"))
            {
                op.Reversed = true;
                op.UpdateOperationAsync();
            }
            if (op.Reversed == true)
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#828181"));
                OperationType.Text += " (Reversed)";
                main.LoadStats();
            }
        }

        // New constructor for CMainR
        public CSingleOperation(CMainR mainR, Operation op)
        {
            InitializeComponent();
            this.mainR = mainR;
            this.op = op;

            OperationPrice.Text = op.PrixOperation.ToString("0.00") + " DH";
            OperationDate.Text = op.DateOperation.ToString();
            if (op.OperationType.StartsWith("V"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#10B981"));
                OperationType.Text = "Vente #" + op.OperationID.ToString();
                foreach (Client c in mainR.main.lc)
                {
                    if (op.ClientID == c.ClientID)
                    {
                        OperationName.Text = "Client : " + c.Nom;
                        break;
                    }
                }

            }
            else if (op.OperationType.StartsWith("A"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff7614"));
                OperationType.Text = "Achat #" + op.OperationID.ToString();
                foreach (Fournisseur f in mainR.main.lfo)
                {
                    if (op.FournisseurID == f.FournisseurID)
                    {
                        OperationName.Text = "Fournisseur : " + f.Nom;
                        break;
                    }
                }
            }
            else if (op.OperationType.StartsWith("M"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2d42fc"));
                OperationType.Text = "Modification #" + op.OperationID.ToString();
                foreach (User u in mainR.main.lu)
                {
                    if (op.UserID == u.UserID)
                    {
                        OperationName.Text = "User : " + u.UserName;
                        break;
                    }
                }
            }
            else if (op.OperationType.StartsWith("D"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff3224"));
                OperationType.Text = "Suppression #" + op.OperationID.ToString();
                foreach (User u in mainR.main.lu)
                {
                    if (op.UserID == u.UserID)
                    {
                        OperationName.Text = "User : " + u.UserName;
                        break;
                    }
                }
            }
            else if (op.OperationType.StartsWith("S"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d3f705"));
                OperationType.Text = "Payement de Credit Fournisseur #" + op.OperationID.ToString();
                foreach (Fournisseur f in mainR.main.lfo)
                {
                    if (op.FournisseurID == f.FournisseurID)
                    {
                        OperationName.Text = "Fournisseur : " + f.Nom;
                        break;
                    }
                }
            }
            else if (op.OperationType.StartsWith("L"))
            {
                OperationType.Text = "📦 Livraison Groupée";
                OperationType.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6"));
            }
            else if (op.OperationType.StartsWith("P"))
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d3f705"));
                OperationType.Text = "Payement de Credit Client#" + op.OperationID.ToString();
                foreach (Client c in mainR.main.lc)
                {
                    if (op.ClientID == c.ClientID)
                    {
                        OperationName.Text = "Client : " + c.Nom;
                        break;
                    }
                }
            }

            foreach (OperationArticle oa in mainR.main.loa)
            {
                if (op.Reversed == true) break;
                if (oa.OperationID == op.OperationID)
                {
                    if (oa.Reversed == true)
                    {
                        reversed++;
                    }
                    total++;
                }
            }

            if (total == reversed && !op.OperationType.StartsWith("S") && !op.OperationType.StartsWith("P"))
            {
                op.Reversed = true;
                op.UpdateOperationAsync();
            }
            if (op.Reversed == true)
            {
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#828181"));
                OperationType.Text += " (Reversed)";
                // Note: mainR doesn't have LoadStats() method, so we skip this for CMainR
            }
        }

        public CMainP main;
        public CMainR mainR;
        public Operation op;
        public bool entered;
        public int reversed;
        public int total;

        private void MyBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WPlus wPlus = new WPlus(this);
            wPlus.ShowDialog();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop click from bubbling up and opening WPlus
            e.Handled = true;

            WDeleteOperationConfirmation dialog = new WDeleteOperationConfirmation(this);
            dialog.ShowDialog();
        }
    }
}