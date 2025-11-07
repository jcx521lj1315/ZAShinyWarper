namespace ZAShinyWarper
{
    public enum ShinyFoundAction
    {
        StopOnFound,
        StopAtFullCache,
        CacheAndContinue,
        ClearAndContinue
    }

    public enum IVType
    {
        Any,
        Perfect, // 31
        Zero     // 0
    }
    public enum Weather
    {
        None = -1,
        Clear = 0,
        Overcast = 1,
        Rain = 2,
        StrongWinds = 3,
        Windy = 5,
        MildWinds = 7,
        Fog = 8,
        IntenseSun = 9,
    }

    public enum TimeOfDay
    {
        None = -1,         // Unchanged
        Morning = 14400,   // Beginning of Morning
        Midday = 43200,    // Mid-day
        Night = 72000,     // Beggining of night
        LateNight = 86400  // Mid-Night
    }
}