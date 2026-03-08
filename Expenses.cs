using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Expenses
    {
        [JsonPropertyName("expenseId")]
        public int ExpenseID { get; set; }

        [JsonPropertyName("expenseName")]
        public string ExpenseName { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("paymentStatus")]
        public string PaymentStatus { get; set; } = "Pending";

        [JsonPropertyName("lastPaidDate")]
        public DateTime? LastPaidDate { get; set; }

        [JsonPropertyName("recurringType")]
        public string RecurringType { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("modifiedDate")]
        public DateTime ModifiedDate { get; set; }

        // Computed properties (unchanged)
        public int DaysUntilDue
        {
            get { return (int)(DueDate - DateTime.Today).TotalDays; }
        }

        public string FormattedAmount
        {
            get { return Amount.ToString("C"); }
        }

        public string FormattedDueDate
        {
            get { return DueDate.ToString("dd/MM/yyyy"); }
        }

        private static readonly string BaseUrl = "http://localhost:5050/api/expenses";

        // GET all expenses
        public static async Task<List<Expenses>> GetAllAsync()
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<Expenses>>(BaseUrl)
                       ?? new List<Expenses>();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors du chargement des dépenses: {0}", ex.Message), ex);
            }
        }

        // GET by ID
        public static async Task<Expenses> GetByIdAsync(int id)
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<Expenses>(
                    string.Format("{0}/{1}", BaseUrl, id));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de la récupération de la dépense: {0}", ex.Message), ex);
            }
        }

        // POST — insert
        public async Task<bool> Add()
        {
            try
            {
                var payload = new
                {
                    expenseName = this.ExpenseName,
                    category = this.Category,
                    amount = this.Amount,
                    dueDate = this.DueDate,
                    recurringType = this.RecurringType,
                    notes = this.Notes
                };

                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ExpenseIdResult>();
                    this.ExpenseID = result?.ExpenseId ?? 0;
                    return this.ExpenseID > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de l'ajout de la dépense: {0}", ex.Message), ex);
            }
        }

        // PUT — update
        public async Task<bool> Update()
        {
            try
            {
                var payload = new
                {
                    expenseName = this.ExpenseName,
                    category = this.Category,
                    amount = this.Amount,
                    dueDate = this.DueDate,
                    recurringType = this.RecurringType,
                    notes = this.Notes
                };

                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/{1}", BaseUrl, this.ExpenseID), payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de la mise à jour de la dépense: {0}", ex.Message), ex);
            }
        }

        // DELETE
        public async Task<bool> Delete()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/{1}", BaseUrl, this.ExpenseID));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de la suppression de la dépense: {0}", ex.Message), ex);
            }
        }

        // Mark as paid
        public async Task<bool> MarkAsPaid(string paymentMethod = "Cash", string paymentNotes = null)
        {
            try
            {
                var payload = new
                {
                    amount = this.Amount,
                    paymentMethod = paymentMethod,
                    notes = paymentNotes
                };

                var response = await MainWindow.ApiClient.PostAsJsonAsync(
                    string.Format("{0}/{1}/payments", BaseUrl, this.ExpenseID), payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors du paiement: {0}", ex.Message), ex);
            }
        }


        // ── Sync wrapper: GetAll (called from non-async UI methods) ──────────────
        public static List<Expenses> GetAll()
        {
            return GetAllAsync().GetAwaiter().GetResult();
        }

        // ── GetUpcoming: returns expenses due within the next N days ─────────────
        public static List<Expenses> GetUpcoming(int daysAhead)
        {
            try
            {
                var all = GetAllAsync().GetAwaiter().GetResult();
                DateTime cutoff = DateTime.Today.AddDays(daysAhead);
                return all.FindAll(e =>
                    (e.PaymentStatus == "Pending" || e.PaymentStatus == "Overdue") &&
                    e.DueDate <= cutoff);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur GetUpcoming: {0}", ex.Message), ex);
            }
        }


        // ── Sync wrappers (for non-async UI event handlers) ───────────────────
        public bool AddSync() => Add().GetAwaiter().GetResult();
        public bool UpdateSync() => Update().GetAwaiter().GetResult();
        public bool DeleteSync() => Delete().GetAwaiter().GetResult();
        public bool MarkAsPaidSync(string paymentMethod = "Cash", string notes = null)
            => MarkAsPaid(paymentMethod, notes).GetAwaiter().GetResult();

        private class ExpenseIdResult
        {
            [JsonPropertyName("expenseId")]
            public int ExpenseId { get; set; }
        }
    }
}