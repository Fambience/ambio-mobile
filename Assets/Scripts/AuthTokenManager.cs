using UnityEngine;

public static class AuthTokenManager
{
    private const string TokenKey = "token";

    // Call this after login to save the token
    public static void SetToken(string token)
    {
        PlayerPrefs.SetString(TokenKey, token);
        PlayerPrefs.Save();
        Debug.Log("Token Saved: " + token);
    }

    // Call this from any script to get the saved token
    public static string GetToken()
    {
        string token = PlayerPrefs.GetString(TokenKey, string.Empty);
        Debug.Log("Token Retrieved: " + token);
        return token;
    }

    // Optionally: Clear token on logout
    public static void ClearToken()
    {
        PlayerPrefs.DeleteKey(TokenKey);
        Debug.Log("Token Cleared.");
    }
}