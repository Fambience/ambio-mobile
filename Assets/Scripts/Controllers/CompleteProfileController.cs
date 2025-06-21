using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
public class CompleteProfileController
{
    public void Initialize(VisualElement root, UIManager manager)
    {
        var title = root.Q<Label>("title");
        if (title != null)
        {
            title.text = $"Complete your profile";
            title.style.fontSize = 22;
            title.style.color = Color.black;
        }

        var subtitle = root.Q<Label>("subtitle");
        if (subtitle != null)
        {
            subtitle.style.fontSize = 14;
            subtitle.style.color = new Color(0.2f, 0.2f, 0.2f);
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            subtitle.style.unityTextAlign = TextAnchor.MiddleLeft;
        }

        var backButton = root.Q<Button>("backButton");
        if (backButton != null)
        {
            backButton.clicked += () => Debug.Log("Back clicked");
        }
        var locations = new List<string>
        {
            "New York",
            "Los Angeles",
            "Chicago",
            "Houston",
            "San Francisco"
        };
        var dropdownWrapper = root.Q<VisualElement>("dropdownWrapper");
        if (dropdownWrapper != null)
        {
            var dropdown = new DropdownField(locations, 0);
            dropdown.name = "locationDropdown";
            dropdown.value = locations[0];

            dropdown.style.width = Length.Percent(100);
            dropdown.style.height = 36;
            dropdown.style.fontSize = 14;
            dropdown.style.unityFontStyleAndWeight = FontStyle.Normal;
            dropdown.style.color = Color.black;
            dropdown.style.paddingLeft = 10;
            dropdown.style.paddingRight = 10;
            dropdown.style.backgroundColor = Color.white;
            dropdown.style.borderTopLeftRadius = 8;
            dropdown.style.borderTopRightRadius = 8;
            dropdown.style.borderBottomLeftRadius = 8;
            dropdown.style.borderBottomRightRadius = 8;
            dropdown.style.borderTopWidth = 0;
            dropdown.style.borderBottomWidth = 0;
            dropdown.style.borderLeftWidth = 0;
            dropdown.style.borderRightWidth = 0;

            dropdown.RegisterValueChangedCallback(evt =>
            {
                Debug.Log("Location selected: " + evt.newValue);
                manager.ShowBudgetScreen();
            });

            dropdownWrapper.Add(dropdown);
        }
    }
}
