using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GestionComerce
{
    public class User
    {
        [JsonPropertyName("userId")]
        public int UserID { get; set; }

        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("roleId")]
        public int RoleID { get; set; }

        [JsonPropertyName("etat")]
        public int Etat { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        public int TenantId { get; set; }

        // ── Helper: attach JWT token to every protected request ──────────────
        private void SetAuthHeader(HttpClient _http)
        {
            if (!string.IsNullOrEmpty(Token))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Token);
        }

        // 1. LOGIN (POST) — no token needed
        public async Task<User> LoginAsync(HttpClient _http, string username, string password, string appUserName, string pin)
        {
            var loginData = new
            {
                username = username,
                password = password,
                appUserName = appUserName,
                pin = pin
            };

            var response = await _http.PostAsJsonAsync("http://localhost:5050/api/auth/login", loginData);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<User>();

            return null;
        }

        // 2. GET ALL USERS (GET) — requires token
        public async Task<List<User>> GetUsersAsync(HttpClient _http)
        {
            SetAuthHeader(_http);
            return await _http.GetFromJsonAsync<List<User>>("http://localhost:5050/api/users");
        }

        // 3. INSERT USER (POST) — requires token
        public async Task<int> InsertUserAsync(HttpClient _http)
        {
            SetAuthHeader(_http);
            var response = await _http.PostAsJsonAsync("http://localhost:5050/api/users", this);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<User>();
                return result.UserID;
            }
            return 0;
        }

        // 4. UPDATE USER (PUT) — requires token
        public async Task<int> UpdateUserAsync(HttpClient _http)
        {
            SetAuthHeader(_http);
            var response = await _http.PutAsJsonAsync($"http://localhost:5050/api/users/{this.UserID}", this);
            return response.IsSuccessStatusCode ? 1 : 0;
        }

        // 5. DELETE USER (DELETE) — requires token
        public async Task<int> DeleteUserAsync(HttpClient _http)
        {
            SetAuthHeader(_http);
            var response = await _http.DeleteAsync($"http://localhost:5050/api/users/{this.UserID}");
            return response.IsSuccessStatusCode ? 1 : 0;
        }
    }
}