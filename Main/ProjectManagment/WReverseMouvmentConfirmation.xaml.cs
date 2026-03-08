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
    public partial class WReverseMouvmentConfirmation : Window
    {
        public WReverseMouvmentConfirmation(CSingleMouvment sm)
        {
            InitializeComponent();
            this.sm = sm;

            // Permission check — works for both CMainP and CMainR
            List<Role> roles = sm.main?.main.lr ?? sm.mainR?.main.lr;
            int userRoleID   = sm.main?.u.RoleID ?? sm.mainR?.u.RoleID ?? -1;

            if (roles != null)
            {
                foreach (Role r in roles)
                {
                    if (r.RoleID == userRoleID)
                    {
                        if (r.ReverseMouvment == false) ContinueBtn.IsEnabled = false;
                        break;
                    }
                }
            }

            // Update dialog message to reflect toggle direction
            if (sm.opa.Reversed)
            {
                if (MainMessage != null)
                    MainMessage.Text = "Êtes-vous sûr de vouloir annuler le reverse de ce mouvement ?";
                if (SubMessage != null)
                    SubMessage.Text = "Le mouvement sera remis en état normal dans l'opération.";
            }
        }

        CSingleMouvment sm;

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Toggle: reversed → unreversed, or unreversed → reversed
                sm.opa.Reversed = !sm.opa.Reversed;
                sm.opa.UpdateOperationArticleAsync();

                // Update button label in the movement row
                if (sm.opa.Reversed)
                {
                    sm.ReverseButton.Content = "Unreverse";
                    sm.ReverseButton.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4d4d4d"));
                }
                else
                {
                    sm.ReverseButton.Content = "Reverse";
                    sm.ReverseButton.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#EF4444"));
                }

                // Determine parent operation status:
                // ANY article unreversed → op unreversed | ALL reversed → op reversed
                List<OperationArticle> loa = sm.main?.main.loa ?? sm.mainR?.main.loa ?? new List<OperationArticle>();
                List<Operation>        lo  = sm.main?.main.lo  ?? sm.mainR?.main.lo  ?? new List<Operation>();

                var opArticles = loa.Where(oa => oa.OperationID == sm.opa.OperationID).ToList();
                bool allReversed  = opArticles.Count > 0 && opArticles.All(oa => oa.Reversed);
                bool anyUnreversed = opArticles.Any(oa => !oa.Reversed);

                Operation parentOp = lo.FirstOrDefault(o => o.OperationID == sm.opa.OperationID);
                if (parentOp != null)
                {
                    if      (allReversed)   parentOp.Reversed = true;
                    else if (anyUnreversed) parentOp.Reversed = false;
                    parentOp.UpdateOperationAsync();
                }

                // Reload UI
                if (sm.main != null)
                {
                    sm.main.LoadOperations(sm.main.main.lo);
                    sm.main.LoadMouvments(sm.main.main.loa);
                    sm.main.LoadStats();
                    sm.main.LoadStatistics();
                }

                string action = sm.opa.Reversed ? "Reverse réussie"   : "Unreverse réussie";
                string msg    = sm.opa.Reversed ? "Reverse a ete effectue avec succes" : "Unreverse a ete effectue avec succes";
                WCongratulations wCongratulations = new WCongratulations(action, msg, 1);
                wCongratulations.ShowDialog();
            }
            catch (Exception ex)
            {
                WCongratulations wCongratulations = new WCongratulations("Opération échouée", ex.Message, 0);
                wCongratulations.ShowDialog();
            }
        }
    }
}
