using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class OperationArticle
    {
        public int OperationArticleID { get; set; }
        public int ArticleID { get; set; }
        public int OperationID { get; set; }
        public int QteArticle { get; set; }
        public DateTime Date { get; set; }
        public bool Etat { get; set; }
        public bool Reversed { get; set; }
        public string ArticleName { get; set; } = string.Empty;

        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // ── GET articles for one operation ────────────────────────────────────
        public async Task<List<OperationArticle>> GetOperationArticlesAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.GetAsync(Operation.ApiBaseUrl + "/api/operations/" + OperationID + "/articles");
                response.EnsureSuccessStatusCode();
                var raw = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<JsonElement>>(raw, _json);
                var list = new List<OperationArticle>();
                if (items != null)
                    foreach (var item in items)
                        list.Add(MapFromJson(item));
                return list;
            }
            catch (Exception err)
            {
                MessageBox.Show("OperationArticle fetch failed: " + err.Message);
                return new List<OperationArticle>();
            }
        }

        // ── GET ALL articles across ALL operations ────────────────────────────
        public static async Task<List<OperationArticle>> GetAllOperationArticlesAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.GetAsync(Operation.ApiBaseUrl + "/api/operations?");
                response.EnsureSuccessStatusCode();
                var raw = await response.Content.ReadAsStringAsync();
                var envelopes = JsonSerializer.Deserialize<List<OperationWithArticles>>(raw, _json);
                var result = new List<OperationArticle>();
                if (envelopes != null)
                {
                    foreach (var env in envelopes)
                    {
                        if (env == null || env.Articles == null) continue;
                        int opId = env.Operation != null ? env.Operation.OperationID : 0;
                        foreach (var a in env.Articles)
                        {
                            result.Add(new OperationArticle
                            {
                                OperationArticleID = a.OperationArticleID,
                                ArticleID = a.ArticleID,
                                OperationID = opId,
                                QteArticle = a.QteArticle,
                                Reversed = a.Reversed,
                                ArticleName = a.ArticleName ?? string.Empty,
                                Etat = true
                            });
                        }
                    }
                }
                return result;
            }
            catch (Exception err)
            {
                MessageBox.Show("OperationArticle (all) fetch failed: " + err.Message);
                return new List<OperationArticle>();
            }
        }

        // ── INSERT ────────────────────────────────────────────────────────────
        public async Task<int> InsertOperationArticleAsync()
        {
            try
            {
                var dto = new OperationArticleDto
                {
                    ArticleID = ArticleID,
                    OperationID = OperationID,
                    QteArticle = QteArticle,
                    Reversed = Reversed
                };
                var content = new StringContent(
                    JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await MainWindow.ApiClient.PostAsync(Operation.ApiBaseUrl + "/api/operations/articles", content);
                response.EnsureSuccessStatusCode();

                var raw = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(raw, _json);
                if (result != null && result.TryGetValue("operationArticleId", out JsonElement idEl))
                    return idEl.GetInt32();
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show("OperationArticle not inserted, error: " + err.Message);
                return 0;
            }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<int> UpdateOperationArticleAsync()
        {
            try
            {
                var dto = new OperationArticleDto
                {
                    ArticleID = ArticleID,
                    OperationID = OperationID,
                    QteArticle = QteArticle,
                    Reversed = Reversed
                };
                var content = new StringContent(
                    JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await MainWindow.ApiClient.PutAsync(
                    Operation.ApiBaseUrl + "/api/operations/articles/" + OperationArticleID, content);
                response.EnsureSuccessStatusCode();
                return 1;
            }
            catch (Exception err)
            {
                MessageBox.Show("OperationArticle not updated: " + err.Message);
                return 0;
            }
        }

        // ── DELETE (soft) ─────────────────────────────────────────────────────
        public async Task<int> DeleteOperationArticleAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    Operation.ApiBaseUrl + "/api/operations/articles/" + OperationArticleID);
                response.EnsureSuccessStatusCode();
                return 1;
            }
            catch (Exception err)
            {
                MessageBox.Show("OperationArticle not deleted: " + err.Message);
                return 0;
            }
        }

        // ── REVERSE ───────────────────────────────────────────────────────────
        public async Task<int> ReverseAsync()
        {
            try
            {
                Reversed = true;
                return await UpdateOperationArticleAsync();
            }
            catch (Exception err)
            {
                MessageBox.Show("OperationArticle not reversed: " + err.Message);
                return 0;
            }
        }

        // ── MAPPER ────────────────────────────────────────────────────────────
        private static OperationArticle MapFromJson(JsonElement e)
        {
            return new OperationArticle
            {
                OperationArticleID = e.TryGetProperty("operationArticleId", out JsonElement id) ? id.GetInt32() : 0,
                ArticleID = e.TryGetProperty("articleId", out JsonElement aid) ? aid.GetInt32() : 0,
                OperationID = e.TryGetProperty("operationId", out JsonElement oid) ? oid.GetInt32() : 0,
                QteArticle = e.TryGetProperty("qteArticle", out JsonElement qty) ? qty.GetInt32() : 0,
                Reversed = e.TryGetProperty("reversed", out JsonElement rev) && rev.GetBoolean(),
                Etat = true
            };
        }
    }

    internal class OperationArticleDto
    {
        public int ArticleID { get; set; }
        public int OperationID { get; set; }
        public int QteArticle { get; set; }
        public bool Reversed { get; set; }
        public string ArticleName { get; set; } = string.Empty;
    }
}