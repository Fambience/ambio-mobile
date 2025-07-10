namespace Services
{
    public static class PasswordResetSession
    {
        public static string Email;
        public static string ResetToken;
        public static int ResendTriggerTime = 180;
    }
}