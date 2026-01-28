public enum DayState
{
    Idle = 0,
    Running = 1,
    Ending = 2
}

public enum DayEndReason
{
    TimeOver = 0,
    ManualEnd = 1,
    AllDead = 2
}

public enum PlayerLocation
{
    Room = 0,
    InGame = 1
}

public static class MatchKeys
{
    public const string DayState = "DayState";
    public const string DayStartTime = "DayStartTime";
    public const string DayDuration = "DayDuration";
    public const string DayEndReason = "DayEndReason";

    public const string EndingUntil = "EndingUntil";

    public const string Ready = "Ready";
    public const string Loc = "Loc";

    public const string WasDeadThisDay = "WasDead";
    public const string NextDayHpRatio = "NextHp";

    public const string PlayerState = "State";
}
