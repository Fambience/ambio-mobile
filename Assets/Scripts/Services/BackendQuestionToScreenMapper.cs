using System;
using System.Collections.Generic;
using Services;

public static class BackendQuestionToScreenMapper
{
    private static readonly Dictionary<BackendQuestion, UIScreenType> userQuestionToScreen = new()
    {
        { BackendQuestion.Q_FIRST_NAME, UIScreenType.UserDetails },
        { BackendQuestion.Q_LAST_NAME, UIScreenType.UserDetails },
        { BackendQuestion.Q_HOME_LOCATED, UIScreenType.Location },
        { BackendQuestion.Q_YOUR_BUDGET, UIScreenType.Budget },
        { BackendQuestion.Q_CREATIVE_AND_CHARACTERFUL_STYLE, UIScreenType.CreativeStyles },
        { BackendQuestion.Q_MODERN_AND_MINIMAL_STYLE, UIScreenType.ModernStyles },
        { BackendQuestion.Q_COLOR_SCHEME, UIScreenType.ColorTone },
        { BackendQuestion.Q_HOME_SHARING_WITH, UIScreenType.Family },
    };

    private static readonly Dictionary<BackendQuestion, UIScreenType> creatorQuestionToScreen = new()
    {
        { BackendQuestion.Q_FIRST_NAME, UIScreenType.CreatorBasicDetails },
        { BackendQuestion.Q_LAST_NAME, UIScreenType.CreatorBasicDetails },
        { BackendQuestion.Q_REGION, UIScreenType.CreatorLocation },
        { BackendQuestion.Q_CREATOR_TYPE, UIScreenType.CreatorType },
        { BackendQuestion.Q_YEARS_OF_EXPERIENCE, UIScreenType.CeatorExperience },
        { BackendQuestion.Q_TAGLINE, UIScreenType.taglineSocials },
        { BackendQuestion.Q_SOCIALS, UIScreenType.taglineSocials },
        { BackendQuestion.Q_WEBSITE, UIScreenType.taglineSocials },
    };

    public static UIScreenType? GetFirstMatchingScreen(string role, List<string> remainingQuestionsRaw)
    {
        Dictionary<BackendQuestion, UIScreenType> questionToScreen = role switch
        {
            "USER" => userQuestionToScreen,
            "CREATOR" => creatorQuestionToScreen,
            _ => null
        };

        if (questionToScreen == null)
        {
            UnityEngine.Debug.LogWarning($"Unknown role: {role}");
            return null;
        }

        foreach (string questionStr in remainingQuestionsRaw)
        {
            if (Enum.TryParse(questionStr, ignoreCase: true, out BackendQuestion question))
            {
                if (questionToScreen.TryGetValue(question, out UIScreenType screen))
                {
                    return screen;
                }
            }
        }

        return null;
    }
}
