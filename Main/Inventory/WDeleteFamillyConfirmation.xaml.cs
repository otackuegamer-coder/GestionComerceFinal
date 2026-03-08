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
    /// Interaction logic for WDeleteFamillyConfirmation.xaml
    /// </summary>
    public partial class WDeleteFamillyConfirmation : Window
    {
        public WDeleteFamillyConfirmation(Famille f, List<Famille> lf, WManageFamillies main)
        {
            InitializeComponent();
            this.f = f;
            this.lf = lf;
            this.main = main;
        }
        Famille f; List<Famille> lf; WManageFamillies main;

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            lf.Remove(f);
            f.DeleteFamilleAsync();
            main.LoadFamillies(lf);
            this.Close();
        }
    }
}
