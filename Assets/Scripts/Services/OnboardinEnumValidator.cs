using System.Collections.Generic;

public static class OnboardingEnumValidator
{
    public static readonly HashSet<string> CreativeStyles = new()
    {
        "ARTDECO", "BOHEMIAN", "COASTAL", "ECLECTIC", "SCANDINAVIAN", "RUSTIC"
    };

    public static readonly HashSet<string> ModernStyles = new()
    {
        "JAPANDI", "MIDCENTURYMODERN", "MINIMALIST", "MODERN", "INDUSTRIAL", "CONTEMPORARY"
    };

    public static readonly HashSet<string> ColorSchemes = new()
    {
        "WARM_PALETTE", "CALM_PALETTE", "NEUTRAL_PALETTE", "BOLD_PALETTE"

    };
}