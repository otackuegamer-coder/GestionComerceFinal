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

namespace GestionComerce.Main.Vente
{
    /// <summary>
    /// Interaction logic for CSingleClient.xaml
    /// </summary>
    public partial class CSingleClient : UserControl
    {
        public CSingleClient(Client c,WSelectCient sc)
        {
            InitializeComponent();
            Name.Text = c.Nom;
            ClientID.Text = c.ClientID.ToString();
            Telephone.Text = c.Telephone;
            this.sc = sc;
            this.c = c;
        }
        WSelectCient sc;Client c;
        public void ClientClicked(object sender, RoutedEventArgs e)
        {
            sc.selected = c.ClientID;
            sc.SelectedClient.Text = Name.Text;
        }
    }
    
}
