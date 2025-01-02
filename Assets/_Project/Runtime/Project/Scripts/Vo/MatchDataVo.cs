using System;
using System.Collections.Generic;

[Serializable]
public class MatchDataVo
{
    public string MatchId { get; set; }
    public List<PlayerMatchData> Players { get; set; }
    public GameState State { get; set; }
    public MatchType Type { get; set; }
    public int CurrentRound { get; set; }
    public Dictionary<string, int> TeamScores { get; set; }
    public float RoundStartTime { get; set; }
    public float RoundEndTime { get; set; }

    public MatchDataVo()
    {
        Players = new List<PlayerMatchData>();
        TeamScores = new Dictionary<string, int>();
        State = GameState.Waiting;
        CurrentRound = 0;
    }
}

[Serializable]
public class PlayerMatchData
{
    public string PlayerId { get; set; }
    public string TeamId { get; set; }
    public string Username { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public float TotalDamageDealt { get; set; }
    public float TotalDamageTaken { get; set; }
    public int Score { get; set; }
    public bool IsReady { get; set; }
}

public enum GameState
{
    Waiting,
    Starting,
    InProgress,
    RoundEnd,
    MatchEnd
}

public enum MatchType
{
    FreeForAll,
    TeamVsTeam
} 