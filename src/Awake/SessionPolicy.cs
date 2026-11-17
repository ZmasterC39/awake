namespace Awake;

public static class SessionPolicy
{
    public static bool ShouldStop(DateTimeOffset now, DateTimeOffset? deadline, bool onBattery, float charge)
        => (deadline.HasValue && now >= deadline.Value) || (onBattery && charge >= 0 && charge < 0.10f);

    public static string Remaining(DateTimeOffset now, DateTimeOffset? deadline)
    {
        if (!deadline.HasValue) return "Sin límite";
        var left = deadline.Value - now;
        if (left <= TimeSpan.Zero) return "00:00";
        return $"{(int)left.TotalMinutes:00}:{left.Seconds:00}";
    }
}
