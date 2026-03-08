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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestionComerce.Main.Facturation.CreateFacture
{
    /// <summary>
    /// Interaction logic for CSingleRowClient.xaml
    /// </summary>
    public partial class CSingleRowClient : UserControl
    {
        public WSelectClient sc;
        public Client c;
        public CSingleRowClient(Client c, WSelectClient sc)
        {
            InitializeComponent();
            this.sc = sc;
            ClientName.Text = c.Nom;
            ClientNumber.Text = c.Telephone;
            this.c = c;

        }
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            sc.main.txtClientName.Text = c.Nom;
            sc.main.txtClientPhone.Text = c.Telephone;
            sc.Close();
        }
    }
}
