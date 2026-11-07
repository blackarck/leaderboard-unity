using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LightLeaderboard
{
    /// <summary>
    /// Official Unity C# SDK for LightLeaderboard (https://leaderboard.goproso.com).
    /// Provides async/await and Coroutine-based methods for score submission,
    /// leaderboard queries, player-centric views, profiles, and HMAC-SHA256 anti-cheat.
    /// </summary>
    public static class LightLeaderboard
    {
        public const string DefaultBaseUrl = "https://leaderboard.goproso.com";

        private static string _apiKey;
        private static string _gameId;
        private static string _baseUrl = DefaultBaseUrl;
        private static string _scoreSecret;
        private static bool _isConfigured;

        public static bool IsConfigured => _isConfigured;

        /// <summary>
        /// Configure the LightLeaderboard client with your game credentials.
        /// Call this once during game startup (e.g. in Awake() or Start()).
        /// </summary>
        /// <param name="apiKey">Your game API key from the LightLeaderboard developer dashboard.</param>
        /// <param name="gameId">Your game reference ID.</param>
        /// <param name="scoreSecret">Optional signature secret for HMAC-SHA256 score signing.</param>
        /// <param name="baseUrl">Optional custom base URL (defaults to https://leaderboard.goproso.com).</param>
        public static void Configure(string apiKey, string gameId, string scoreSecret = null, string baseUrl = null)
        {
            if (string.IsNullOrEmpty(apiKey)) throw new ArgumentException("apiKey cannot be null or empty", nameof(apiKey));
            if (string.IsNullOrEmpty(gameId)) throw new ArgumentException("gameId cannot be null or empty", nameof(gameId));

            _apiKey = apiKey;
            _gameId = gameId;
            _scoreSecret = scoreSecret;
            _baseUrl = string.IsNullOrEmpty(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
            _isConfigured = true;
        }

        private static void EnsureConfigured()
        {
            if (!_isConfigured)
            {
                throw new InvalidOperationException("LightLeaderboard is not configured. Call LightLeaderboard.Configure(apiKey, gameId) first.");
            }
        }

        private static string BuildGameUrl(string endpoint, Dictionary<string, string> queryParams = null)
        {
            var sb = new StringBuilder();
            sb.Append(_baseUrl);
            sb.Append("/api/v1/games/");
            sb.Append(UnityWebRequest.EscapeURL(_gameId));
            sb.Append(endpoint);

            if (queryParams != null && queryParams.Count > 0)
            {
                sb.Append("?");
                bool first = true;
                foreach (var kvp in queryParams)
                {
                    if (string.IsNullOrEmpty(kvp.Value)) continue;
                    if (!first) sb.Append("&");
                    sb.Append(UnityWebRequest.EscapeURL(kvp.Key));
                    sb.Append("=");
                    sb.Append(UnityWebRequest.EscapeURL(kvp.Value));
                    first = false;
                }
            }

            return sb.ToString();
        }

        private static string ComputeHmacSha256(string secret, string data)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static async Task<string> SendRequestAsync(string method, string url, string jsonBody = null, Dictionary<string, string> headers = null)
        {
            EnsureConfigured();

            using (var request = new UnityWebRequest(url, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

                if (!string.IsNullOrEmpty(jsonBody))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                if (headers != null)
                {
                    foreach (var h in headers)
                    {
                        request.SetRequestHeader(h.Key, h.Value);
                    }
                }

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                string responseText = request.downloadHandler?.text;

#if UNITY_2020_1_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isNetworkError || request.isHttpError)
#endif
                {
                    string errorMsg = $"LightLeaderboard request failed ({method} {url}): {request.error}";
                    throw new LightLeaderboardException(errorMsg, request.responseCode, responseText);
                }

                return responseText;
            }
        }

        // =========================================================================
        // Score Submission
        // =========================================================================

        /// <summary>
        /// Submits a score asynchronously and immediately returns the player's updated rank and personal best flag.
        /// </summary>
        public static async Task<SubmitScoreResult> SubmitScoreAsync(SubmitScoreOptions options)
        {
            string url = BuildGameUrl("/scores");
            string json = JsonUtility.ToJson(options);

            Dictionary<string, string> headers = null;
            if (!string.IsNullOrEmpty(_scoreSecret))
            {
                string sig = ComputeHmacSha256(_scoreSecret, json);
                headers = new Dictionary<string, string>
                {
                    { "x-score-signature", "sha256=" + sig }
                };
            }

            string responseJson = await SendRequestAsync("POST", url, json, headers);
            return JsonUtility.FromJson<SubmitScoreResult>(responseJson);
        }

        /// <summary>
        /// Coroutine-friendly helper to submit a score with onSuccess and onError callbacks.
        /// </summary>
        public static IEnumerator SubmitScore(SubmitScoreOptions options, Action<SubmitScoreResult> onSuccess, Action<LightLeaderboardException> onError = null)
        {
            var task = SubmitScoreAsync(options);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                var ex = task.Exception?.InnerException as LightLeaderboardException 
                         ?? new LightLeaderboardException(task.Exception?.Message ?? "Unknown error", 0);
                onError?.Invoke(ex);
            }
            else
            {
                onSuccess?.Invoke(task.Result);
            }
        }

        // =========================================================================
        // Leaderboard
        // =========================================================================

        /// <summary>
        /// Fetches the global or filtered leaderboard rankings asynchronously.
        /// </summary>
        public static async Task<GetLeaderboardResult> GetLeaderboardAsync(GetLeaderboardOptions options = null)
        {
            options ??= new GetLeaderboardOptions();
            var q = new Dictionary<string, string>
            {
                { "limit", options.limit.ToString() },
                { "offset", options.offset.ToString() },
                { "period", options.period },
                { "season", options.season },
                { "team", options.team },
                { "allEntries", options.allEntries ? "true" : null }
            };

            string url = BuildGameUrl("/leaderboard", q);
            string responseJson = await SendRequestAsync("GET", url);
            return JsonUtility.FromJson<GetLeaderboardResult>(responseJson);
        }

        /// <summary>
        /// Coroutine-friendly helper to fetch the leaderboard.
        /// </summary>
        public static IEnumerator GetLeaderboard(GetLeaderboardOptions options, Action<GetLeaderboardResult> onSuccess, Action<LightLeaderboardException> onError = null)
        {
            var task = GetLeaderboardAsync(options);
            while (!task.IsCompleted) yield return null;

            if (task.IsFaulted)
            {
                var ex = task.Exception?.InnerException as LightLeaderboardException 
                         ?? new LightLeaderboardException(task.Exception?.Message ?? "Unknown error", 0);
                onError?.Invoke(ex);
            }
            else
            {
                onSuccess?.Invoke(task.Result);
            }
        }

        // =========================================================================
        // Player Rank & Percentile
        // =========================================================================

        /// <summary>
        /// Retrieves a player's exact rank, score, and percentile standing.
        /// </summary>
        public static async Task<PlayerRankResult> GetPlayerRankAsync(string playerRefId, GetRankOptions options = null)
        {
            if (string.IsNullOrEmpty(playerRefId)) throw new ArgumentException("playerRefId is required", nameof(playerRefId));

            var q = options != null ? new Dictionary<string, string>
            {
                { "period", options.period },
                { "season", options.season },
                { "team", options.team }
            } : null;

            string url = BuildGameUrl($"/players/{UnityWebRequest.EscapeURL(playerRefId)}/rank", q);
            string responseJson = await SendRequestAsync("GET", url);
            return JsonUtility.FromJson<PlayerRankResult>(responseJson);
        }

        // =========================================================================
        // Centric ("You vs Neighbors") Leaderboard
        // =========================================================================

        /// <summary>
        /// Fetches a slice of the leaderboard centered on the specified player.
        /// </summary>
        public static async Task<CentricLeaderboardResult> GetCentricLeaderboardAsync(string playerRefId, GetCentricOptions options = null)
        {
            if (string.IsNullOrEmpty(playerRefId)) throw new ArgumentException("playerRefId is required", nameof(playerRefId));

            options ??= new GetCentricOptions();
            var q = new Dictionary<string, string>
            {
                { "limit", options.limit.ToString() },
                { "period", options.period },
                { "season", options.season },
                { "team", options.team }
            };

            string url = BuildGameUrl($"/players/{UnityWebRequest.EscapeURL(playerRefId)}/centric", q);
            string responseJson = await SendRequestAsync("GET", url);
            return JsonUtility.FromJson<CentricLeaderboardResult>(responseJson);
        }

        // =========================================================================
        // Player Profile
        // =========================================================================

        /// <summary>
        /// Fetches player profile details such as display name, avatar, country, and level.
        /// </summary>
        public static async Task<PlayerProfile> GetPlayerAsync(string playerRefId)
        {
            if (string.IsNullOrEmpty(playerRefId)) throw new ArgumentException("playerRefId is required", nameof(playerRefId));

            string url = BuildGameUrl($"/players/{UnityWebRequest.EscapeURL(playerRefId)}");
            string responseJson = await SendRequestAsync("GET", url);

            // The API returns { ok: true, player: { ... } }
            // Let's parse the wrapped object or direct player object
            var wrapper = JsonUtility.FromJson<PlayerProfileWrapper>(responseJson);
            return wrapper?.player;
        }

        [Serializable]
        private class PlayerProfileWrapper
        {
            public bool ok;
            public PlayerProfile player;
        }

        /// <summary>
        /// Creates or updates a player's profile data.
        /// </summary>
        public static async Task UpdatePlayerAsync(string playerRefId, UpdatePlayerOptions options)
        {
            if (string.IsNullOrEmpty(playerRefId)) throw new ArgumentException("playerRefId is required", nameof(playerRefId));

            string url = BuildGameUrl($"/players/{UnityWebRequest.EscapeURL(playerRefId)}");
            string json = JsonUtility.ToJson(options);
            await SendRequestAsync("PUT", url, json);
        }

        // =========================================================================
        // Player Score History
        // =========================================================================

        /// <summary>
        /// Fetches historical score submissions made by a player.
        /// </summary>
        public static async Task<GetPlayerScoresResult> GetPlayerScoresAsync(string playerRefId, GetPlayerScoresOptions options = null)
        {
            if (string.IsNullOrEmpty(playerRefId)) throw new ArgumentException("playerRefId is required", nameof(playerRefId));

            options ??= new GetPlayerScoresOptions();
            var q = new Dictionary<string, string>
            {
                { "limit", options.limit.ToString() },
                { "offset", options.offset.ToString() }
            };

            string url = BuildGameUrl($"/players/{UnityWebRequest.EscapeURL(playerRefId)}/scores", q);
            string responseJson = await SendRequestAsync("GET", url);
            return JsonUtility.FromJson<GetPlayerScoresResult>(responseJson);
        }
    }
}
