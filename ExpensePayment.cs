using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GestionComerce
{
    public class ExpensePayment
    {
        [JsonPropertyName("paymentId")]
        public int PaymentID { get; set; }

        [JsonPropertyName("expenseId")]
        public int ExpenseID { get; set; }

        [JsonPropertyName("paymentAmount")]
        public decimal PaymentAmount { get; set; }

        [JsonPropertyName("paymentDate")]
        public DateTime PaymentDate { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        // Computed properties (unchanged)
        public string FormattedAmount { get { return PaymentAmount.ToString("C"); } }
        public string FormattedDate { get { return PaymentDate.ToString("dd/MM/yyyy"); } }

        private static readonly string BaseUrl = "http://localhost:5050/api/expenses";

        // GET all payments for a specific expense
        public static async Task<List<ExpensePayment>> GetByExpenseId(int expenseId)
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<ExpensePayment>>(
                    string.Format("{0}/{1}/payments", BaseUrl, expenseId))
                       ?? new List<ExpensePayment>();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors du chargement de l'historique des paiements: {0}", ex.Message), ex);
            }
        }

        // GET all payments (across all expenses) — not a direct API endpoint,
        // so we return empty; use GetByExpenseId for specific expense payments
        public static async Task<List<ExpensePayment>> GetAll()
        {
            // The API doesn't have a global "all payments" endpoint.
            // Return empty list — use GetByExpenseId(expenseId) instead.
            return new List<ExpensePayment>();
        }

        // GET by ID — not a direct API endpoint; search locally from the expense
        public static async Task<ExpensePayment> GetById(int paymentId)
        {
            // The API doesn't expose GET /payments/{id} directly.
            // Return null — use GetByExpenseId to load the list, then find by PaymentID.
            return null;
        }

        // POST — add payment for an expense
        public async Task<bool> Add()
        {
            try
            {
                var payload = new
                {
                    paymentAmount = this.PaymentAmount,
                    paymentDate = this.PaymentDate,
                    paymentMethod = this.PaymentMethod,
                    notes = this.Notes
                };

                var response = await MainWindow.ApiClient.PostAsJsonAsync(
                    string.Format("{0}/{1}/payments", BaseUrl, this.ExpenseID), payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PaymentIdResult>();
                    this.PaymentID = result?.PaymentId ?? 0;
                    return this.PaymentID > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de l'ajout du paiement: {0}", ex.Message), ex);
            }
        }

        // PUT — update payment
        public async Task<bool> Update()
        {
            try
            {
                var payload = new
                {
                    paymentAmount = this.PaymentAmount,
                    paymentDate = this.PaymentDate,
                    paymentMethod = this.PaymentMethod,
                    notes = this.Notes
                };

                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/payments/{1}", BaseUrl, this.PaymentID), payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de la mise à jour du paiement: {0}", ex.Message), ex);
            }
        }

        // DELETE
        public async Task<bool> Delete()
        {
            return await Delete(this.PaymentID);
        }

        public static async Task<bool> Delete(int paymentId)
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/payments/{1}", BaseUrl, paymentId));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de la suppression du paiement: {0}", ex.Message), ex);
            }
        }

        // Get total payments for an expense
        public static async Task<decimal> GetTotalByExpenseId(int expenseId)
        {
            try
            {
                var payments = await GetByExpenseId(expenseId);
                decimal total = 0;
                foreach (var p in payments) total += p.PaymentAmount;
                return total;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors du calcul du total des paiements: {0}", ex.Message), ex);
            }
        }

        private class PaymentIdResult
        {
            [JsonPropertyName("paymentId")]
            public int PaymentId { get; set; }
        }
    }
}