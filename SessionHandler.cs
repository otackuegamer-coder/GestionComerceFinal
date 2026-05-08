using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    /// <summary>
    /// Wraps every outgoing API request. When the server returns 401 SESSION_INVALIDATED
    /// (meaning the user logged in from another machine) this handler fires
    /// OnSessionInvalidated so the UI can redirect to the login screen immediately.
    /// </summary>
    public class SessionHandler : DelegatingHandler
    {
        /// <summary>Set this in load_main to handle forced logouts on the UI thread.</summary>
        public static Action OnSessionInvalidated { get; set; }

        public SessionHandler() : base(new HttpClientHandler()) { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                try
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (body.Contains("SESSION_INVALIDATED"))
                    {
                        Application.Current?.Dispatcher.InvokeAsync(
                            () => OnSessionInvalidated?.Invoke());
                    }
                }
                catch { }
            }

            return response;
        }
    }
}
