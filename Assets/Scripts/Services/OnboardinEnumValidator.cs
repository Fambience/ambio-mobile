using System.Collections.Generic;

public static class OnboardingEnumValidator
{
    public static readonly HashSet<string> CreativeStyles = new()
    {
        "ARTDECO", "BOHEMIAN", "COASTAL", "ECLACTIC", "SCANDINAVIAN", "RUSTIC"
    };

    public static readonly HashSet<string> ModernStyles = new()
    {
        "JAPANDI", "MIDCENTURYMODERN", "MINIMALIST", "MODERN", "INDUSTRIAL", "CONTEMPORARY"
    };

    public static readonly HashSet<string> ColorSchemes = new()
    {
        "WARM_PALETTE", "NEUTRAL_PALETTE", "EARTHY_PALETTE", "MONOCHROME", "PASTEL_PALETTE", "VIBRANT_PALETTE"
    };
}