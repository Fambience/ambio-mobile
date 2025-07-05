using System;
using System.Collections.Generic;
using Services;

public static class BackendQuestionToScreenMapper
{
    private static readonly Dictionary<BackendQuestion, UIScreenType> questionToScreen = new()
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

    public static UIScreenType? GetFirstMatchingScreen(List<string> remainingQuestionsRaw)
    {
        foreach (string questionStr in remainingQuestionsRaw)
        {
            if (Enum.TryParse(questionStr, ignoreCase: true, out BackendQuestion question))
            {
                if (questionToScreen.TryGetValue(question, out UIScreenType screen))
                    return screen;
            }
        }

        return null;
    }
}