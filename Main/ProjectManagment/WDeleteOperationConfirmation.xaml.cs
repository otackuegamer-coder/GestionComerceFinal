using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace GestionComerce.Main.ProjectManagment
{
    public partial class WDeleteOperationConfirmation : Window
    {
        private readonly CSingleOperation _so;

        public WDeleteOperationConfirmation(CSingleOperation so)
        {
            InitializeComponent();
            _so = so;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfirmBtn.IsEnabled = false;

                // Work out which main we're coming from
                CMainP main  = _so.main;
                CMainR mainR = _so.mainR;

                List<OperationArticle> loa = main?.main.loa ?? mainR?.main.loa ?? new List<OperationArticle>();
                List<Article>          laa = main?.main.laa ?? mainR?.main.laa ?? new List<Article>();
                List<Operation>        lo  = main?.main.lo  ?? mainR?.main.lo  ?? new List<Operation>();

                // 1. Reverse + delete all OperationArticles for this operation
                var toDelete = loa.Where(oa => oa.OperationID == _so.op.OperationID).ToList();

                foreach (OperationArticle oa in toDelete)
                {
                    // Adjust stock if not already reversed
                    if (!oa.Reversed)
                    {
                        if (_so.op.OperationType.StartsWith("V"))
                        {
                            foreach (Article a in laa)
                            {
                                if (a.ArticleID == oa.ArticleID)
                                {
                                    a.Quantite += oa.QteArticle;
                                    a.UpdateArticleAsync();
                                    break;
                                }
                            }
                        }
                        else if (_so.op.OperationType.StartsWith("A"))
                        {
                            foreach (Article a in laa)
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
                    }

                    // Delete the OperationArticle record
                    oa.DeleteOperationArticleAsync();

                    // Remove from in-memory list
                    loa.Remove(oa);
                }

                // 2. Soft-delete: mark reversed then set Etat=0 in DB
                _so.op.Reversed = true;
                _so.op.UpdateOperationAsync();  // saves Reversed = true
                _so.op.DeleteOperationAsync();  // sets Etat = 0

                // 3. Remove from in-memory list so it disappears from the UI
                lo.Remove(_so.op);

                // 4. Reload the operations list
                if (main != null)
                {
                    main.LoadOperations(main.main.lo);
                    main.LoadMouvments(main.main.loa);
                    main.LoadStats();
                    main.LoadStatistics();
                }

                this.Close();

                WCongratulations wCongratulations = new WCongratulations(
                    "Opération supprimée",
                    "L'opération a été supprimée avec succès.", 1);
                wCongratulations.ShowDialog();
            }
            catch (Exception ex)
            {
                ConfirmBtn.IsEnabled = true;
                WCongratulations wCongratulations = new WCongratulations(
                    "Erreur", ex.Message, 0);
                wCongratulations.ShowDialog();
            }
        }
    }
}
