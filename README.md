# LightLeaderboard — Unity C# SDK

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity&logoColor=white&style=flat-square)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE)

Official Unity C# package for **[LightLeaderboard](https://leaderboard.goproso.com)** — fast, lightweight leaderboard-as-a-service for indie game developers and studios.

---

## Features

- ⚡ **Instant Rank on Submit** — Get player rank, total player count, and personal best status immediately from `SubmitScoreAsync`.
- 🔒 **Native Anti-Cheat** — Automatic HMAC-SHA256 score signing using .NET's `System.Security.Cryptography.HMACSHA256`.
- 🎯 **Player-Centric Views** — Fetch centered leaderboards ("You vs. Neighbors") in a single call.
- ⏱️ **Time & Custom Buckets** — Filter by `all`, `weekly`, `monthly`, as well as custom seasons and teams.
- 👤 **Player Profiles & History** — Store player country, avatar, level, and fetch full score submission history.
- 🔄 **Async/Await & Coroutines** — Seamless integration with modern C# `Task` async/await as well as Unity `IEnumerator` coroutines.
- 📦 **UPM Ready** — Clean Unity Package Manager layout with assembly definitions (`.asmdef`).

---

## Installation

### Option 1: Unity Package Manager via Git URL (Recommended)

1. In Unity, open **Window → Package Manager**.
2. Click the **+** button in the top-left corner.
3. Select **Add package from git URL...**
4. Enter the repository URL:
   ```text
   https://github.com/blackarck/leaderboard-unity.git
   ```
   To pin a release, append a tag, e.g. `https://github.com/blackarck/leaderboard-unity.git#v1.0.0`.
5. Click **Add**. Unity will download and import the package.

### Option 2: Manual Drop-In

1. Copy the `Runtime/` folder from this repository directly into your project's `Assets/Plugins/LightLeaderboard/` folder.
2. Unity will automatically compile the assembly definition `LightLeaderboard.asmdef`.

---

## Quick Start

```csharp
using UnityEngine;
using LightLeaderboard;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        // 1. Initialize once with credentials from the developer dashboard
        LightLeaderboard.Configure("YOUR_API_KEY", "YOUR_GAME_REF_ID");
    }

    public async void OnGameOver(int finalScore)
    {
        // 2. Submit score and get rank immediately
        var result = await LightLeaderboard.SubmitScoreAsync(new SubmitScoreOptions
        {
            score = finalScore,
            playerRefId = "player-123",
            playerName = "Alice",
            playTimeMs = 42000
        });

        Debug.Log($"Rank #{result.rank} of {result.totalPlayers}! Personal Best: {result.isPersonalBest}");

        // 3. Fetch top 10 scores
        var lb = await LightLeaderboard.GetLeaderboardAsync(new GetLeaderboardOptions
        {
            limit = 10,
            period = "weekly"
        });

        foreach (var entry in lb.entries)
        {
            Debug.Log($"#{entry.rank}  {entry.playerName}: {entry.score}");
        }
    }
}
```

---

## API Reference

### `LightLeaderboard.Configure(apiKey, gameId, scoreSecret?, baseUrl?)`

Initializes the static client. Call this once during startup before invoking any other methods.

```csharp
LightLeaderboard.Configure(
    apiKey: "YOUR_API_KEY",
    gameId: "YOUR_GAME_REF_ID",
    scoreSecret: "optional-signature-secret", // Enables HMAC-SHA256 signing
    baseUrl: "https://leaderboard.goproso.com" // Optional custom host
);
```

---

### `SubmitScoreAsync(options)`

Submits a score to the leaderboard.

```csharp
var result = await LightLeaderboard.SubmitScoreAsync(new SubmitScoreOptions
{
    score = 12500,
    playerRefId = "player-123",
    playerName = "Alice",
    playTimeMs = 65200,
    seasonId = "season-1",
    teamId = "clan-red",
    submissionId = System.Guid.NewGuid().ToString(), // Idempotency key
    gameStatTxt1 = "Hero: Knight",
    gameStatInt1 = 50 // e.g. coins collected
});

Debug.Log($"Submitted! Rank: #{result.rank}, PB: {result.isPersonalBest}");
```

#### Coroutine Alternative

```csharp
StartCoroutine(LightLeaderboard.SubmitScore(
    options,
    onSuccess: (res) => Debug.Log($"Rank #{res.rank}"),
    onError: (err) => Debug.LogError(err.Message)
));
```

---

### `GetLeaderboardAsync(options?)`

Fetches leaderboard rankings. By default, returns one entry per player (their personal best).

```csharp
var lb = await LightLeaderboard.GetLeaderboardAsync(new GetLeaderboardOptions
{
    limit = 20,
    offset = 0,
    period = "weekly", // "all" | "weekly" | "monthly"
    season = "season-1",
    team = "clan-red",
    allEntries = false // true for every run, false for best-per-player
});

foreach (var entry in lb.entries)
{
    Debug.Log($"#{entry.rank} {entry.playerName}: {entry.score}");
}
```

---

### `GetPlayerRankAsync(playerRefId, options?)`

Retrieves a player's rank, personal best score, and **percentile** standing.

```csharp
var rank = await LightLeaderboard.GetPlayerRankAsync("player-123", new GetRankOptions
{
    period = "weekly"
});

Debug.Log($"Rank: #{rank.rank} of {rank.totalPlayers} (Top {(100f - rank.percentile):F1}%)");
```

---

### `GetCentricLeaderboardAsync(playerRefId, options?)`

Fetches a slice of the leaderboard centered on the player (e.g. 5 ranks above, the player, and 5 ranks below).

```csharp
var centric = await LightLeaderboard.GetCentricLeaderboardAsync("player-123", new GetCentricOptions
{
    limit = 11,
    period = "all"
});

Debug.Log($"Around your rank #{centric.playerRank}:");
foreach (var entry in centric.entries)
{
    string tag = entry.playerRefId == "player-123" ? " (YOU)" : "";
    Debug.Log($"#{entry.rank} {entry.playerName}: {entry.score}{tag}");
}
```

---

### `GetPlayerAsync(playerRefId)` / `UpdatePlayerAsync(playerRefId, options)`

Manage player metadata.

```csharp
// Update profile
await LightLeaderboard.UpdatePlayerAsync("player-123", new UpdatePlayerOptions
{
    playerName = "Alice Wonder",
    country = "US",
    level = 25,
    device = "mobile"
});

// Fetch profile
var profile = await LightLeaderboard.GetPlayerAsync("player-123");
Debug.Log($"Player: {profile.playerName}, Country: {profile.country}, Level: {profile.level}");
```

---

### `GetPlayerScoresAsync(playerRefId, options?)`

Fetches historical runs submitted by a player, ordered newest first.

```csharp
var history = await LightLeaderboard.GetPlayerScoresAsync("player-123", new GetPlayerScoresOptions
{
    limit = 20
});

Debug.Log($"Total runs: {history.total}, Best: {history.bestScore}");
foreach (var run in history.entries)
{
    Debug.Log($"{run.createdAt} - Score: {run.score}");
}
```

---

## Anti-Cheat & Score Signing

To prevent client-side score tampering, enable **Require signed scores** in your game settings on the LightLeaderboard developer dashboard.

Pass your secret key when configuring the client:

```csharp
LightLeaderboard.Configure("YOUR_API_KEY", "YOUR_GAME_REF_ID", scoreSecret: "YOUR_SIGNATURE_SECRET");
```

Every subsequent `SubmitScoreAsync` request automatically computes an HMAC-SHA256 signature and includes the `x-score-signature` HTTP header.

---

## Error Handling

Failed requests throw a `LightLeaderboardException` containing the HTTP status code and server response:

```csharp
try
{
    var res = await LightLeaderboard.SubmitScoreAsync(options);
}
catch (LightLeaderboardException ex)
{
    Debug.LogError($"API Error (HTTP {ex.StatusCode}): {ex.Message}");
    if (ex.IsAuthError) Debug.LogError("Check your API key or Game ID.");
    if (ex.IsRateLimitError) Debug.LogWarning("Rate limit reached. Please back off.");
}
catch (System.Exception ex)
{
    Debug.LogError($"Network or parsing error: {ex.Message}");
}
```

---

## Requirements

- **Unity 2021.3 LTS or newer** (tested on 2021.3, 2022.3, and Unity 6)
- Standard .NET Standard 2.1 or .NET 4.x runtime profile
- Supported on all platforms: WebGL, iOS, Android, Windows, macOS, Linux, Consoles

---

## License

[MIT](LICENSE) © [LightLeaderboard](https://leaderboard.goproso.com)
