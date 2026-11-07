using System;
using UnityEngine;
using LightLeaderboard;

namespace LightLeaderboard.Samples
{
    /// <summary>
    /// Example script demonstrating how to use the LightLeaderboard Unity SDK.
    /// Attach this script to any GameObject in your scene.
    /// </summary>
    public class LeaderboardExample : MonoBehaviour
    {
        [Header("LightLeaderboard Credentials")]
        [SerializeField] private string apiKey = "YOUR_API_KEY";
        [SerializeField] private string gameId = "YOUR_GAME_REF_ID";
        [SerializeField] private string scoreSecret = ""; // Optional HMAC signature secret

        [Header("Test Player")]
        [SerializeField] private string playerId = "player-unity-001";
        [SerializeField] private string playerName = "Alex";

        private void Start()
        {
            // 1. Configure the SDK once on startup
            LightLeaderboard.Configure(apiKey, gameId, string.IsNullOrEmpty(scoreSecret) ? null : scoreSecret);
            Debug.Log("[LightLeaderboard] Configured client for game: " + gameId);
        }

        /// <summary>
        /// Example: Submit a score and inspect rank and personal best flags immediately.
        /// </summary>
        [ContextMenu("Submit Test Score")]
        public async void SubmitTestScore()
        {
            try
            {
                var options = new SubmitScoreOptions
                {
                    score = UnityEngine.Random.Range(1000, 9999),
                    playerRefId = playerId,
                    playerName = playerName,
                    playTimeMs = 45000,
                    submissionId = Guid.NewGuid().ToString(),
                    gameStatTxt1 = "Hero: Knight",
                    gameStatInt1 = 15 // e.g. coins collected
                };

                Debug.Log($"[LightLeaderboard] Submitting score: {options.score} for {options.playerName}...");
                var result = await LightLeaderboard.SubmitScoreAsync(options);

                Debug.Log($"[LightLeaderboard] Score Submitted! " +
                          $"Rank: #{result.rank} / {result.totalPlayers} players | " +
                          $"Personal Best: {result.isPersonalBest}");
            }
            catch (LightLeaderboardException ex)
            {
                Debug.LogError($"[LightLeaderboard] Submission failed (HTTP {ex.StatusCode}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LightLeaderboard] Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example: Fetch top 10 global leaderboard scores.
        /// </summary>
        [ContextMenu("Fetch Top 10 Leaderboard")]
        public async void FetchLeaderboard()
        {
            try
            {
                var options = new GetLeaderboardOptions
                {
                    limit = 10,
                    period = "weekly" // "all" | "weekly" | "monthly"
                };

                var lb = await LightLeaderboard.GetLeaderboardAsync(options);
                Debug.Log($"[LightLeaderboard] Top {lb.entries.Length} scores ({lb.period}):");

                foreach (var entry in lb.entries)
                {
                    Debug.Log($"#{entry.rank} - {entry.playerName}: {entry.score}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LightLeaderboard] Failed to fetch leaderboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Example: Fetch a player-centric slice of the leaderboard ("You vs Neighbors").
        /// </summary>
        [ContextMenu("Fetch Centric Leaderboard")]
        public async void FetchCentricLeaderboard()
        {
            try
            {
                var centric = await LightLeaderboard.GetCentricLeaderboardAsync(playerId, new GetCentricOptions { limit = 7 });
                Debug.Log($"[LightLeaderboard] Leaderboard around rank #{centric.playerRank}:");

                foreach (var entry in centric.entries)
                {
                    string youTag = entry.playerRefId == playerId ? " (YOU)" : "";
                    Debug.Log($"#{entry.rank} - {entry.playerName}: {entry.score}{youTag}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LightLeaderboard] Failed to fetch centric leaderboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Example: Update player metadata and fetch their standing.
        /// </summary>
        [ContextMenu("Update & Fetch Player Profile")]
        public async void UpdateAndFetchProfile()
        {
            try
            {
                await LightLeaderboard.UpdatePlayerAsync(playerId, new UpdatePlayerOptions
                {
                    playerName = playerName,
                    country = "US",
                    level = 10,
                    device = "desktop"
                });

                var rank = await LightLeaderboard.GetPlayerRankAsync(playerId);
                Debug.Log($"[LightLeaderboard] Player {playerName} Rank #{rank.rank} of {rank.totalPlayers} (Top {(100.0 - rank.percentile):F1}%)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LightLeaderboard] Profile update failed: {ex.Message}");
            }
        }
    }
}
