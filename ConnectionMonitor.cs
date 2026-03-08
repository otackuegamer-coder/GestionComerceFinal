using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    // ══════════════════════════════════════════════════════════════════════════
    // CONNECTION STATE
    // Three possible states so the banner can show the right message.
    // ══════════════════════════════════════════════════════════════════════════
    public enum ConnectionState
    {
        /// <summary>Everything is fine — internet reachable AND API responding.</summary>
        Online,

        /// <summary>Device has no internet / Wi-Fi at all.</summary>
        NoInternet,

        /// <summary>Internet is present but the API server is down or unreachable.</summary>
        ApiDown
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CONNECTION MONITOR
    // ══════════════════════════════════════════════════════════════════════════
    public class ConnectionMonitor
    {
        // ── config ────────────────────────────────────────────────────────────
        private readonly string _apiUrl;

        /// <summary>Seconds between checks.</summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>Per-request timeout.</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(4);

        // ── state ─────────────────────────────────────────────────────────────
        private ConnectionState _state = ConnectionState.Online;   // optimistic start
        private CancellationTokenSource _cts;
        private static readonly HttpClient _http = new HttpClient();

        // ── public events — always fired on the UI thread ─────────────────────
        /// <summary>Fired whenever the state changes. Passes the new state.</summary>
        public event Action<ConnectionState> StateChanged;

        public ConnectionState CurrentState => _state;

        public ConnectionMonitor(string apiUrl)
        {
            _apiUrl = apiUrl ?? throw new ArgumentNullException(nameof(apiUrl));
        }

        // ─────────────────────────────────────────────────────────────────────
        // START / STOP
        // ─────────────────────────────────────────────────────────────────────

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => RunLoopAsync(_cts.Token));
        }

        public void Stop() => _cts?.Cancel();

        // ─────────────────────────────────────────────────────────────────────
        // BACKGROUND LOOP
        // ─────────────────────────────────────────────────────────────────────

        private async Task RunLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                ConnectionState newState = await DetectStateAsync();

                if (newState != _state)
                {
                    _state = newState;
                    Dispatch(() => StateChanged?.Invoke(newState));
                }

                try { await Task.Delay(Interval, ct); }
                catch (TaskCanceledException) { break; }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // TWO-STEP DETECTION
        // Step 1 — check basic internet with a lightweight ping to 8.8.8.8
        //          (Google DNS — always reachable if the device is online)
        // Step 2 — only if internet is OK, try to reach the API
        // ─────────────────────────────────────────────────────────────────────

        private async Task<ConnectionState> DetectStateAsync()
        {
            // ── Step 1: does the device have internet at all? ──────────────────
            bool hasInternet = await PingInternetAsync();
            if (!hasInternet)
                return ConnectionState.NoInternet;

            // ── Step 2: is our API server responding? ──────────────────────────
            bool apiUp = await PingApiAsync();
            return apiUp ? ConnectionState.Online : ConnectionState.ApiDown;
        }

        /// <summary>
        /// ICMP ping to 8.8.8.8 — succeeds as long as the device has any internet
        /// access, regardless of whether our API is running.
        /// </summary>
        private async Task<bool> PingInternetAsync()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync("8.8.8.8", (int)Timeout.TotalMilliseconds);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// HTTP HEAD to the API base URL — succeeds only when the server is up.
        /// Any HTTP response (even 401/404) counts as "API is running".
        /// </summary>
        private async Task<bool> PingApiAsync()
        {
            try
            {
                using (var cts = new CancellationTokenSource(Timeout))
                {
                    var req = new HttpRequestMessage(HttpMethod.Head, _apiUrl);
                    await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────────────────────────────────

        private static void Dispatch(Action action)
        {
            var app = Application.Current;
            if (app == null) return;
            if (app.Dispatcher.CheckAccess()) action();
            else app.Dispatcher.Invoke(action);
        }
    }
}