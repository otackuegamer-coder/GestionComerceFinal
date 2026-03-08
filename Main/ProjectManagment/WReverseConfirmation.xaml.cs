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
    public partial class WReverseConfirmation : Window
    {
        public WReverseConfirmation(WPlus plus, WArticlesReverse arts)
        {
            InitializeComponent();
            this.plus = plus;
            this.arts = arts;
        }

        WPlus plus;
        WArticlesReverse arts;

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ── Article-level operations (V and A types) ──────────────────
                if (arts != null)
                {
                    // Split changed articles into newly-reversed and newly-unreversed
                    var newlyReversed   = new List<OperationArticle>();
                    var newlyUnreversed = new List<OperationArticle>();

                    int countRev = 0;

                    foreach (CSingleArticleReverse sar in arts.ArticlesContainer.Children)
                    {
                        if (sar.oa.Reversed) countRev++;

                        if (sar.inittialStat != sar.oa.Reversed)
                        {
                            if (sar.oa.Reversed)
                                newlyReversed.Add(sar.oa);
                            else
                                newlyUnreversed.Add(sar.oa);
                        }
                    }

                    // Determine operation-level reversed status:
                    // ALL articles reversed → op reversed | ANY unreversed → op unreversed
                    bool allReversed = (countRev == arts.ArticlesContainer.Children.Count);
                    plus.so.op.Reversed = allReversed;

                    // ── Stock adjustments ────────────────────────────────────
                    if (plus.so.op.OperationType.StartsWith("V"))
                    {
                        // Newly reversed Vente → add qty back to stock
                        foreach (OperationArticle oa in newlyReversed)
                        {
                            foreach (Article a in plus.so.main.main.laa)
                            {
                                if (a.ArticleID == oa.ArticleID)
                                {
                                    a.Quantite += oa.QteArticle;
                                    a.UpdateArticleAsync();
                                    break;
                                }
                            }
                        }
                        // Newly unreversed Vente → remove qty from stock again
                        foreach (OperationArticle oa in newlyUnreversed)
                        {
                            foreach (Article a in plus.so.main.main.laa)
                            {
                                if (a.ArticleID == oa.ArticleID)
                                {
                                    a.Quantite -= oa.QteArticle;
                                    a.UpdateArticleAsync();
                                    break;
                                }
                            }
                        }

                        // ── Credit adjustments for Vente ────────────────────
                        decimal raReversed   = GetTotalValue(newlyReversed,   plus.so.op, plus.so.main.main.laa, "V");
                        decimal raUnreversed = GetTotalValue(newlyUnreversed, plus.so.op, plus.so.main.main.laa, "V");

                        if (plus.so.op.OperationType.EndsWith("50") || plus.so.op.OperationType.EndsWith("Cr"))
                        {
                            foreach (Client c in plus.so.main.main.lc)
                            {
                                if (c.ClientID == plus.so.op.ClientID)
                                {
                                    foreach (Credit cr in plus.so.main.main.credits)
                                    {
                                        if (cr.ClientID == plus.so.op.ClientID)
                                        {
                                            // Reversing articles → reduce credit debt
                                            if (raReversed > 0) cr.Total -= raReversed;
                                            // Unreversing articles → restore credit debt
                                            if (raUnreversed > 0) cr.Total += raUnreversed;
                                            if (cr.Total < 0) cr.Total = 0;
                                            cr.UpdateCreditAsync();
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    else if (plus.so.op.OperationType.StartsWith("A"))
                    {
                        // Newly reversed Achat → remove qty from stock
                        foreach (OperationArticle oa in newlyReversed)
                        {
                            foreach (Article a in plus.so.main.main.laa)
                            {
                                if (a.ArticleID == oa.ArticleID)
                                {
                                    a.Quantite -= oa.QteArticle;
                                    if (a.Quantite <= 0)
                                    {
                                        a.Quantite = 0;
                                        a.Etat = false;
                                        a.DeleteArticleAsync();
                                    }
                                    else
                                    {
                                        a.UpdateArticleAsync();
                                    }
                                    break;
                                }
                            }
                        }
                        // Newly unreversed Achat → add qty back to stock
                        foreach (OperationArticle oa in newlyUnreversed)
                        {
                            foreach (Article a in plus.so.main.main.laa)
                            {
                                if (a.ArticleID == oa.ArticleID)
                                {
                                    a.Quantite += oa.QteArticle;
                                    if (!a.Etat) a.Etat = true;
                                    a.UpdateArticleAsync();
                                    break;
                                }
                            }
                        }

                        // ── Credit adjustments for Achat ────────────────────
                        decimal raReversed   = GetTotalValue(newlyReversed,   plus.so.op, plus.so.main.main.laa, "A");
                        decimal raUnreversed = GetTotalValue(newlyUnreversed, plus.so.op, plus.so.main.main.laa, "A");

                        if (plus.so.op.OperationType.EndsWith("50") || plus.so.op.OperationType.EndsWith("Cr"))
                        {
                            foreach (Fournisseur f in plus.so.main.main.lfo)
                            {
                                if (f.FournisseurID == plus.so.op.FournisseurID)
                                {
                                    foreach (Credit cr in plus.so.main.main.credits)
                                    {
                                        if (cr.FournisseurID == plus.so.op.FournisseurID)
                                        {
                                            if (raReversed   > 0) cr.Total -= raReversed;
                                            if (raUnreversed > 0) cr.Total += raUnreversed;
                                            if (cr.Total < 0) cr.Total = 0;
                                            cr.UpdateCreditAsync();
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }

                // ── Non-article operations (M, D, S, P) ──────────────────────
                else if (plus.so.op.OperationType.StartsWith("M"))
                {
                    foreach (OperationArticle cs in plus.so.main.main.loa)
                    {
                        if (plus.so.op.OperationID == cs.OperationID)
                        {
                            foreach (Article a in plus.so.main.main.laa)
                            {
                                if (a.ArticleID == cs.ArticleID)
                                {
                                    a.Quantite = cs.QteArticle;
                                    plus.so.op.Reversed = true;
                                    cs.Reversed = true;
                                    cs.UpdateOperationArticleAsync();
                                    a.UpdateArticleAsync();
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (plus.so.op.OperationType.StartsWith("D"))
                {
                    foreach (OperationArticle cs in plus.so.main.main.loa)
                    {
                        if (plus.so.op.OperationID == cs.OperationID)
                        {
                            foreach (Article a in plus.so.main.main.laa)
                            {
                                if (a.ArticleID == cs.ArticleID)
                                {
                                    a.Etat = true;
                                    plus.so.op.Reversed = true;
                                    cs.Reversed = true;
                                    cs.UpdateOperationArticleAsync();
                                    a.BringBackArticleAsync();
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (plus.so.op.OperationType.StartsWith("S"))
                {
                    foreach (Credit cr in plus.so.main.main.credits)
                    {
                        if (plus.so.op.CreditID == cr.CreditID)
                        {
                            cr.Paye += plus.so.op.CreditValue;
                            plus.so.op.Reversed = true;
                            cr.UpdateCreditAsync();
                        }
                    }
                }
                else if (plus.so.op.OperationType.StartsWith("P"))
                {
                    foreach (Credit cr in plus.so.main.main.credits)
                    {
                        if (plus.so.op.CreditID == cr.CreditID)
                        {
                            cr.Paye += plus.so.op.CreditValue;
                            plus.so.op.Reversed = true;
                            cr.UpdateCreditAsync();
                        }
                    }
                }

                plus.so.op.UpdateOperationAsync();
                plus.so.main.LoadOperations(plus.so.main.main.lo);
                plus.so.main.LoadMouvments(plus.so.main.main.loa);
                plus.so.main.LoadStats();
                plus.so.main.LoadStatistics();
                plus.LoadArticles();

                WCongratulations wCongratulations = new WCongratulations("Reverse réussie", "Reverse a ete effectue avec succes", 1);
                wCongratulations.ShowDialog();
            }
            catch (Exception ex)
            {
                WCongratulations wCongratulations = new WCongratulations("Reverse échoué", ex.Message, 0);
                wCongratulations.ShowDialog();
            }
        }

        // Helper: sum price of articles based on operation type
        private decimal GetTotalValue(List<OperationArticle> oas, Operation op, List<Article> laa, string opType)
        {
            decimal total = 0;
            foreach (OperationArticle oa in oas)
            {
                foreach (Article a in laa)
                {
                    if (a.ArticleID == oa.ArticleID)
                    {
                        total += oa.QteArticle * (opType == "V" ? a.PrixVente : a.PrixAchat);
                        break;
                    }
                }
            }
            return total;
        }
    }
}
