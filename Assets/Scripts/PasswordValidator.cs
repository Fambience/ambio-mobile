public static class PasswordValidator
{
    public static bool IsValidPassword(string password, out string errorMessage)
    {
        if (string.IsNullOrEmpty(password))
        {
            errorMessage = "Password cannot be empty.";
            return false;
        }

        if (password.Length < 8 )
        {
            errorMessage = "Password must be at least 8 characters long.";
            return false;
        }

        if (password.Length > 16)
        {
            errorMessage = "Password must be at most 16 characters long.";
        }

        errorMessage = string.Empty;
        return true;
    }
}