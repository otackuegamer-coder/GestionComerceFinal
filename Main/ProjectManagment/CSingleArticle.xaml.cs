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
    /// Interaction logic for CSingleArticle.xaml
    /// </summary>
    public partial class CSingleArticle : UserControl
    {
        public CSingleArticle(WPlus plus,OperationArticle oa)
        {
            InitializeComponent();
            this.plus = plus;
            this.oa = oa;
            foreach(Article a in plus.so.main.main.laa)
            {
                if (a.ArticleID == oa.ArticleID)
                {
                    ArticleName.Text = a.ArticleName;
                    if (a.Etat == false)
                    {
                        ArticleName.Text = a.ArticleName + " (Supprime)";
                    }
                    if (oa.Reversed == true)
                    {
                        ArticleName.Text += " (Reversed)";
                    }
                    
                    ArticleCode.Text=a.Code.ToString();  
                    ArticleQuantity.Text=oa.QteArticle.ToString();
                    if (plus.so.op.OperationType.StartsWith("V"))
                    {
                        ArticleUnitaryPrice.Text = a.PrixVente.ToString();
                        ArticleTotalPrice.Text = (oa.QteArticle * a.PrixVente).ToString();
                    }else if (plus.so.op.OperationType.StartsWith("A"))
                    {
                        ArticleUnitaryPrice.Text = a.PrixAchat.ToString();
                        ArticleTotalPrice.Text = (oa.QteArticle * a.PrixAchat).ToString();
                    }
                    else
                    {
                        ArticleUnitaryPrice.Text = a.PrixVente.ToString();
                        ArticleTotalPrice.Text = (oa.QteArticle * a.PrixVente).ToString();
                    }
                        break;
                }
            }
        }
        WPlus plus; public OperationArticle oa;
    }
}
