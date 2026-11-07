using System;

namespace LightLeaderboard
{
    // =========================================================================
    // Score Submission
    // =========================================================================

    [Serializable]
    public class SubmitScoreOptions
    {
        public double score;
        public string playerRefId;
        public string playerName;
        public long playTimeMs;
        public string seasonId;
        public string teamId;
        public string submissionId;
        public string metadata; // JSON-encoded string or payload
        public string gameStatTxt1;
        public string gameStatTxt2;
        public string gameStatTxt3;
        public int gameStatInt1;
        public int gameStatInt2;
        public int gameStatInt3;
    }

    [Serializable]
    public class SubmitScoreResult
    {
        public int id;
        public int? rank;
        public bool isPersonalBest;
        public int? totalPlayers;
        public bool deduped;
    }

    // =========================================================================
    // Leaderboard
    // =========================================================================

    [Serializable]
    public class LeaderboardEntry
    {
        public int id;
        public int rank;
        public string playerRefId;
        public string playerName;
        public double score;
        public long? playTimeMs;
        public string seasonId;
        public string teamId;
        public string metadata;
        public string createdAt;
    }

    [Serializable]
    public class GetLeaderboardOptions
    {
        public int limit = 20;
        public int offset = 0;
        public string period = "all"; // "all" | "weekly" | "monthly"
        public string season;
        public string team;
        public bool allEntries = false;
    }

    [Serializable]
    public class GetLeaderboardResult
    {
        public LeaderboardEntry[] entries;
        public string scoreOrder;
        public string period;
        public string season;
        public string team;
        public int limit;
        public int offset;
    }

    // =========================================================================
    // Player Rank & Percentile
    // =========================================================================

    [Serializable]
    public class GetRankOptions
    {
        public string period = "all";
        public string season;
        public string team;
    }

    [Serializable]
    public class PlayerRankResult
    {
        public int rank;
        public double score;
        public int totalPlayers;
        public double percentile; // 0.0 to 100.0, higher is better
    }

    // =========================================================================
    // Centric ("You vs Neighbors") Leaderboard
    // =========================================================================

    [Serializable]
    public class GetCentricOptions
    {
        public int limit = 11;
        public string period = "all";
        public string season;
        public string team;
    }

    [Serializable]
    public class CentricLeaderboardResult
    {
        public LeaderboardEntry[] entries;
        public int? playerRank;
        public string period;
        public string season;
        public string team;
        public string scoreOrder;
    }

    // =========================================================================
    // Player Profiles
    // =========================================================================

    [Serializable]
    public class PlayerProfile
    {
        public string playerRefId;
        public string playerName;
        public string avatarUrl;
        public string country;
        public int? level;
        public string teamId;
        public string device;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class UpdatePlayerOptions
    {
        public string playerName;
        public string avatarUrl;
        public string country;
        public int? level;
        public string teamId;
        public string device;
    }

    // =========================================================================
    // Player Score History
    // =========================================================================

    [Serializable]
    public class PlayerScoreEntry
    {
        public int id;
        public double score;
        public long? playTimeMs;
        public string seasonId;
        public string teamId;
        public string metadata;
        public string createdAt;
    }

    [Serializable]
    public class GetPlayerScoresOptions
    {
        public int limit = 20;
        public int offset = 0;
    }

    [Serializable]
    public class GetPlayerScoresResult
    {
        public PlayerScoreEntry[] entries;
        public int total;
        public double bestScore;
    }
}
