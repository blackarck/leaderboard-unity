using System;

namespace LightLeaderboard
{
    /// <summary>
    /// Exception thrown when an API request to LightLeaderboard fails.
    /// </summary>
    public class LightLeaderboardException : Exception
    {
        /// <summary>
        /// The HTTP status code returned by the server (e.g. 401, 404, 429).
        /// </summary>
        public long StatusCode { get; }

        /// <summary>
        /// The raw JSON response body from the server, if any.
        /// </summary>
        public string ResponseBody { get; }

        public bool IsAuthError => StatusCode == 401 || StatusCode == 403;
        public bool IsRateLimitError => StatusCode == 429;
        public bool IsValidationError => StatusCode == 400;
        public bool IsNotFoundError => StatusCode == 404;

        public LightLeaderboardException(string message, long statusCode, string responseBody = null)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public override string ToString()
        {
            return $"LightLeaderboardException: {Message} (HTTP {StatusCode})\nResponse: {ResponseBody}";
        }
    }
}
