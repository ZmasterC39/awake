using Awake;
var now = DateTimeOffset.Parse("2026-11-17T12:00:00Z");
void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
Check(!SessionPolicy.ShouldStop(now, null, false, 1), "unlimited session");
Check(SessionPolicy.ShouldStop(now, now, false, 1), "exact deadline");
Check(SessionPolicy.ShouldStop(now, now.AddSeconds(-1), false, 1), "expired deadline");
Check(!SessionPolicy.ShouldStop(now, now.AddMinutes(15), true, .10f), "ten percent boundary");
Check(SessionPolicy.ShouldStop(now, null, true, .09f), "low battery");
Check(!SessionPolicy.ShouldStop(now, null, false, .09f), "low battery with external power");
Check(!SessionPolicy.ShouldStop(now, null, true, -1), "unknown battery");
Check(SessionPolicy.Remaining(now, now.AddMinutes(120)) == "120:00", "two hour display");
Check(SessionPolicy.Remaining(now, now.AddSeconds(-10)) == "00:00", "no negative countdown");
Check(SessionPolicy.Remaining(now, null) == "Sin límite", "unlimited display");
