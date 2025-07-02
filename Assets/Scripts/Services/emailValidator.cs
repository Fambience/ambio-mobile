using UnityEngine;
using System.Text.RegularExpressions;
public class emailValidator : MonoBehaviour
{
    public static bool isValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        email = email.Trim(); // Remove leading and trailing spaces

        string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, emailPattern);
    }
}