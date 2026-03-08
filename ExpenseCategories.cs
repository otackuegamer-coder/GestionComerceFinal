using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GestionComerce
{
    public class ExpenseCategories
    {
        [JsonPropertyName("categoryId")]
        public int CategoryID { get; set; }

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        private static readonly string BaseUrl = "http://localhost:5050/api/expenses/categories";

        // GET all active categories (async)
        public static async Task<List<ExpenseCategories>> GetAllActiveAsync()
        {
            try
            {
                var all = await MainWindow.ApiClient.GetFromJsonAsync<List<ExpenseCategories>>(BaseUrl)
                          ?? new List<ExpenseCategories>();
                // API returns only active — filter client-side just in case
                return all.FindAll(c => c.IsActive);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors du chargement des catégories: {0}", ex.Message), ex);
            }
        }

        // GET all categories (active + inactive) — same endpoint, API returns active only
        // Keep for compatibility; if you need inactive too, add a query param on the API later
        public static async Task<List<ExpenseCategories>> GetAll()
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<ExpenseCategories>>(BaseUrl)
                       ?? new List<ExpenseCategories>();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors du chargement des catégories: {0}", ex.Message), ex);
            }
        }

        // GET by ID
        public static async Task<ExpenseCategories> GetById(int categoryId)
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<ExpenseCategories>(
                    string.Format("{0}/{1}", BaseUrl, categoryId));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de la récupération de la catégorie: {0}", ex.Message), ex);
            }
        }

        // POST — insert
        public async Task<bool> Add()
        {
            try
            {
                var payload = new
                {
                    categoryName = this.CategoryName,
                    description = this.Description,
                    isActive = this.IsActive
                };

                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CategoryIdResult>();
                    this.CategoryID = result?.CategoryId ?? 0;
                    return this.CategoryID > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de l'ajout de la catégorie: {0}", ex.Message), ex);
            }
        }

        // PUT — update
        public async Task<bool> Update()
        {
            try
            {
                var payload = new
                {
                    categoryName = this.CategoryName,
                    description = this.Description,
                    isActive = this.IsActive
                };

                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/{1}", BaseUrl, this.CategoryID), payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de la mise à jour de la catégorie: {0}", ex.Message), ex);
            }
        }

        // DELETE — soft delete (sets IsActive=0)
        public async Task<bool> Delete()
        {
            return await Delete(this.CategoryID);
        }

        public static async Task<bool> Delete(int categoryId)
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/{1}", BaseUrl, categoryId));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Erreur lors de la suppression de la catégorie: {0}", ex.Message), ex);
            }
        }

        // HardDelete — not exposed by the API (soft-delete only); same as Delete
        public static async Task<bool> HardDelete(int categoryId)
        {
            return await Delete(categoryId);
        }


        // GET all active categories (sync - for non-async UI callers)
        public static List<ExpenseCategories> GetAllActive()
        {
            return GetAllActiveAsync().GetAwaiter().GetResult();
        }


        // ── Sync wrapper for Add (called from non-async UI) ───────────────────
        public bool AddSync() => Add().GetAwaiter().GetResult();

        private class CategoryIdResult
        {
            [JsonPropertyName("categoryId")]
            public int CategoryId { get; set; }
        }
    }
}