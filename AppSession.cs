namespace GestionComerce
{
    /// <summary>
    /// Holds data that belongs to the current login session.
    /// Cleared when the user logs out or the session is invalidated.
    /// </summary>
    public static class AppSession
    {
        // Subscription credentials — needed to pass to the API when creating users
        public static string SubscriptionUsername { get; set; } = string.Empty;
        public static string SubscriptionPassword { get; set; } = string.Empty;

        // Pages the current subscription/licence allows (null = all pages)
        public static string[] AllowedPages { get; set; } = null;

        // Maximum number of app users the plan allows
        public static int MaxUsers { get; set; } = int.MaxValue;

        // Days remaining on the subscription (shown as a warning when low)
        public static int? DaysRemaining { get; set; }

        public static void Clear()
        {
            SubscriptionUsername = string.Empty;
            SubscriptionPassword = string.Empty;
            AllowedPages         = null;
            MaxUsers             = int.MaxValue;
            DaysRemaining        = null;
        }
    }
}
