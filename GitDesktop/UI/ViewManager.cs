namespace GitDesktop.UI
{
    internal static class ViewManager
    {
        public static Action<IRender>? AddNewView { get; set; }
        public static Action<IRender>? RemoveView { get; set; }

        public static void CreateLogPopupWindow(string message)
        {
            new PopupLogWindow(message);
        }
    }
}
