using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Operation
    {
        public DateTime Date { get; set; }
        public int OperationID { get; set; }
        public decimal PrixOperation { get; set; }
        public decimal Remise { get; set; }
        public decimal CreditValue { get; set; }
        public int UserID { get; set; }
        public int? ClientID { get; set; }
        public int? FournisseurID { get; set; }
        public int? CreditID { get; set; }
        public int? PaymentMethodID { get; set; }
        public DateTime DateOperation { get; set; }
        public bool Etat { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public bool Reversed { get; set; }

        // Set these once at app startup:
        //   Operation.ApiBaseUrl = "http://localhost:5050";
        //   Operation.BearerToken = "<token from /api/auth/login>";
        public static string ApiBaseUrl { get; set; } = "http://localhost:5050";
        public static string BearerToken { get; set; } = string.Empty;

        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(ApiBaseUrl);
            if (!string.IsNullOrEmpty(BearerToken))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BearerToken);
            return client;
        }

        // Maps an OperationApiModel (from API envelope) to a flat Operation object
        private static Operation FromApiModel(OperationApiModel m)
        {
            return new Operation
            {
                OperationID = m.OperationID,
                PrixOperation = m.PrixOperation,
                Remise = m.Remise,
                CreditValue = m.CreditValue,
                UserID = m.UserID,
                ClientID = m.ClientID,
                FournisseurID = m.FournisseurID,
                CreditID = m.CreditID,
                PaymentMethodID = m.PaymentMethodID,
                OperationType = m.OperationType ?? string.Empty,
                Reversed = m.Reversed,
                Date = m.Date ?? DateTime.MinValue,
                DateOperation = m.Date ?? DateTime.MinValue,
                Etat = true
            };
        }

        // GET ALL — returns List<Operation> (same shape as before the API migration)
        public async Task<List<Operation>> GetOperationsAsync(
            string type = null,
            int? clientId = null,
            int? fournisseurId = null)
        {
            try
            {
                var query = new StringBuilder("/api/operations?");
                if (!string.IsNullOrEmpty(type)) query.Append("type=" + Uri.EscapeDataString(type) + "&");
                if (clientId.HasValue) query.Append("clientId=" + clientId + "&");
                if (fournisseurId.HasValue) query.Append("fournisseurId=" + fournisseurId + "&");

                using (var client = CreateClient())
                {
                    var response = await client.GetAsync(query.ToString().TrimEnd('&', '?'));
                    response.EnsureSuccessStatusCode();
                    var raw = await response.Content.ReadAsStringAsync();
                    var envelopes = JsonSerializer.Deserialize<List<OperationWithArticles>>(raw, _json);
                    var result = new List<Operation>();
                    if (envelopes != null)
                        foreach (var env in envelopes)
                            if (env != null && env.Operation != null)
                                result.Add(FromApiModel(env.Operation));
                    return result;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Operation fetch failed: " + err.Message);
                return new List<Operation>();
            }
        }

        // GET SINGLE — returns the operation with its articles embedded
        public static async Task<OperationWithArticles> GetByIdAsync(int id)
        {
            try
            {
                using (var client = CreateClient())
                {
                    var response = await client.GetAsync("/api/operations/" + id);
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                    response.EnsureSuccessStatusCode();
                    var raw = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<OperationWithArticles>(raw, _json);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Operation fetch failed: " + err.Message);
                return null;
            }
        }

        // INSERT
        public async Task<int> InsertOperationAsync()
        {
            try
            {
                using (var client = CreateClient())
                {
                    var dto = new OperationDto
                    {
                        PrixOperation = PrixOperation,
                        Remise = Remise,
                        CreditValue = CreditValue,
                        UserID = UserID,
                        ClientID = ClientID,
                        FournisseurID = FournisseurID,
                        CreditID = CreditID,
                        PaymentMethodID = PaymentMethodID,
                        OperationType = OperationType,
                        Reversed = Reversed
                    };
                    var content = new StringContent(
                        JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("/api/operations", content);
                    response.EnsureSuccessStatusCode();
                    var raw = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(raw, _json);
                    JsonElement idEl;
                    if (result != null && result.TryGetValue("operationId", out idEl))
                        return idEl.GetInt32();
                    return 0;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Operation not inserted, error: " + err.Message);
                return 0;
            }
        }

        // UPDATE
        public async Task<int> UpdateOperationAsync()
        {
            try
            {
                using (var client = CreateClient())
                {
                    var dto = new OperationDto
                    {
                        PrixOperation = PrixOperation,
                        Remise = Remise,
                        CreditValue = CreditValue,
                        UserID = UserID,
                        ClientID = ClientID,
                        FournisseurID = FournisseurID,
                        CreditID = CreditID,
                        PaymentMethodID = PaymentMethodID,
                        OperationType = OperationType,
                        Reversed = Reversed
                    };
                    var content = new StringContent(
                        JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                    var response = await client.PutAsync("/api/operations/" + OperationID, content);
                    response.EnsureSuccessStatusCode();
                    return 1;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Operation not updated: " + err.Message);
                return 0;
            }
        }

        // DELETE (soft)
        public async Task<int> DeleteOperationAsync()
        {
            try
            {
                using (var client = CreateClient())
                {
                    var response = await client.DeleteAsync("/api/operations/" + OperationID);
                    response.EnsureSuccessStatusCode();
                    return 1;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Operation not deleted: " + err.Message);
                return 0;
            }
        }
    }

    // ── DTO for serializing requests ──────────────────────────────────────────
    internal class OperationDto
    {
        public decimal PrixOperation { get; set; }
        public decimal Remise { get; set; }
        public decimal CreditValue { get; set; }
        public int UserID { get; set; }
        public int? ClientID { get; set; }
        public int? FournisseurID { get; set; }
        public int? CreditID { get; set; }
        public int? PaymentMethodID { get; set; }
        public string OperationType { get; set; }
        public bool Reversed { get; set; }
    }

    // ── API response wrappers (kept for GetByIdAsync) ─────────────────────────
    public class OperationWithArticles
    {
        [JsonPropertyName("operation")]
        public OperationApiModel Operation { get; set; } = new OperationApiModel();

        [JsonPropertyName("articles")]
        public List<OperationArticleApiModel> Articles { get; set; } = new List<OperationArticleApiModel>();
    }

    public class OperationApiModel
    {
        [JsonPropertyName("operationId")] public int OperationID { get; set; }
        [JsonPropertyName("prixOperation")] public decimal PrixOperation { get; set; }
        [JsonPropertyName("remise")] public decimal Remise { get; set; }
        [JsonPropertyName("creditValue")] public decimal CreditValue { get; set; }
        [JsonPropertyName("userId")] public int UserID { get; set; }
        [JsonPropertyName("clientId")] public int? ClientID { get; set; }
        [JsonPropertyName("fournisseurId")] public int? FournisseurID { get; set; }
        [JsonPropertyName("creditId")] public int? CreditID { get; set; }
        [JsonPropertyName("paymentMethodId")] public int? PaymentMethodID { get; set; }
        [JsonPropertyName("operationType")] public string OperationType { get; set; } = string.Empty;
        [JsonPropertyName("reversed")] public bool Reversed { get; set; }
        [JsonPropertyName("date")] public DateTime? Date { get; set; }
    }

    public class OperationArticleApiModel
    {
        [JsonPropertyName("operationArticleId")] public int OperationArticleID { get; set; }
        [JsonPropertyName("articleId")] public int ArticleID { get; set; }
        [JsonPropertyName("articleName")] public string ArticleName { get; set; } = string.Empty;
        [JsonPropertyName("qteArticle")] public int QteArticle { get; set; }
        [JsonPropertyName("reversed")] public bool Reversed { get; set; }
    }
}