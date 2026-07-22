namespace GitDesktop.Git
{
    public static class Logger
    {
        private static Action<string> logAction = null!;

        public static void Initialize(Action<string> logAction)
        {
            Logger.logAction = logAction;
        }

        public static void Log(string message)
        {
            logAction?.Invoke(message);
        }
    }
}
