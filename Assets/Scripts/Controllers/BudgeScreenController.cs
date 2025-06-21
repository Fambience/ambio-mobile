using UnityEngine;
using UnityEngine.UIElements;

public class BudgeScreenController
{
    public void Initialize(VisualElement root, UIManager manager)
    {
        var title = root.Q<Label>("budgetTitle");
        if (title != null)
        {
            title.style.fontSize = 22;
            title.style.color = Color.black;
        }

        var backButton = root.Q<Button>("backButton");
        if (backButton != null)
        {
            backButton.clicked += () =>
            {
                Debug.Log("[BudgetScreen] Back button clicked");
                manager.ShowCompleteProfile();
            };
        }

        var group = root.Q<VisualElement>("budgetOptions");
        if (group != null)
        {
            string[] options = { "$0–$500", "$500–$1000", "$1000+" };

            foreach (var option in options)
            {
                var radio = new RadioButton
                {
                    text = option,
                    name = "radio_" + option
                };
                radio.AddToClassList("radio");

                radio.RegisterValueChangedCallback(evt =>
                {
                    Debug.Log($"[BudgetScreen] Radio '{option}' changed to: {evt.newValue}");

                    if (evt.newValue)
                    {
                        // Deselect other radios
                        foreach (var sibling in group.Children())
                        {
                            if (sibling is RadioButton rb && rb != radio)
                            {
                                rb.value = false;
                            }
                        }

                        Debug.Log("[BudgetScreen] Navigating to FamilyScreen...");
                        manager.ShowFamilyScreen();
                    }
                });

                group.Add(radio);
            }

            Debug.Log("[BudgetScreen] Radio options initialized.");
        }
        else
        {
            Debug.LogWarning("[BudgetScreen] 'budgetOptions' element not found!");
        }
    }
}
