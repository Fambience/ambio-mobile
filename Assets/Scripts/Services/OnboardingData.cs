using System.Collections.Generic;

public class OnboardingData
{
    public static string FirstName;
    public static string LastName;
    public static string HomeLocation;
    public static string Tagline { get; set; }
    public static string Website { get; set; }
    public static Dictionary<string, string> SocialLinks { get; set; } = new();
    public static int BudgetMin;
    public static int BudgetMax;
    public static List<string> SelectedCities = new List<string>();
    public static List<string> DesignInspoScreen1 = new();
    public static List<string> DesignInspoScreen2 = new();
    public static List<string> ColorScheme;
    public static List<string> HomeSharingWith = new();
    public static string TypeOfDesigner;
    public static string YearsOfExperience;
    public static string DesignerName;
}