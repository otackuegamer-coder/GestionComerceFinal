using GestionComerce;
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

namespace GestionComerce.Main.Facturation.CreateFacture
{
    /// <summary>
    /// Interaction logic for WSelectClient.xaml
    /// </summary>
    public partial class WSelectClient : Window
    {
        public CMainFa main; 
        public WSelectClient(CMainFa main)
        {
            InitializeComponent();
            this.main = main;
            LoadClients(main.main.lc);
        }
        public void LoadClients(List <Client> lc)
        {
            ClientsContainer.Children.Clear();
            foreach (Client c in lc)
            { 
                CSingleRowClient cSingleRowClient = new CSingleRowClient(c, this);
                ClientsContainer.Children.Add(cSingleRowClient);
            }
        }
        private void btnClose_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();

        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            foreach (Client c in main.main.lc)
            {
                if (c.Nom.IndexOf(textBox.Text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.ClientID.ToString().IndexOf(textBox.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Match found, ensure the client is visible
                    foreach (CSingleRowClient src in ClientsContainer.Children)
                    {
                        if (src.c.ClientID == c.ClientID)
                        {
                            src.Visibility = Visibility.Visible;
                            break;
                        }
                    }
                }
                else
                {
                    // No match, hide the client
                    foreach (CSingleRowClient src in ClientsContainer.Children)
                    {
                        if (src.c.ClientID == c.ClientID)
                        {
                            src.Visibility = Visibility.Collapsed;
                            break;
                        }
                    }
                }
            }
        }
    }
}
