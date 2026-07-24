namespace WindowsFormsApp1
{
    public static class UserSession
    {
        public static bool IsAdmin { get; set; }
        public static string Username { get; set; }
        public static bool IsCashierSessionActive { get; set; }

        public static void Clear()
        {
            IsAdmin = false;
            Username = null;
            IsCashierSessionActive = false;
        }
    }
}
