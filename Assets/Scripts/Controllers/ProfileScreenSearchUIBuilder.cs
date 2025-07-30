using UnityEngine;
using UnityEngine.UIElements;
using System;

public static class ProfileScreenSearchUIBuilder
{
    public static VisualElement CreateHeaderSection(Action onFilterClicked)
    {
        VisualElement headerSection = new VisualElement();
        headerSection.name = "headerSection";
        headerSection.AddToClassList("headerSection");
        headerSection.style.marginTop = 20;

        // Create search section
        VisualElement searchSection = CreateSearchSection(onFilterClicked);
        headerSection.Add(searchSection);

        return headerSection;
    }

    private static VisualElement CreateSearchSection(Action onFilterClicked)
    {
        VisualElement searchSection = new VisualElement();
        searchSection.AddToClassList("searchSection");
        searchSection.style.flexDirection = FlexDirection.Row;
        searchSection.style.alignItems = Align.Center;

        // Search wrapper and field
        VisualElement searchWrapper = CreateSearchWrapper();
        searchSection.Add(searchWrapper);

        return searchSection;
    }

    private static VisualElement CreateSearchWrapper()
    {
        VisualElement searchWrapper = new VisualElement();
        searchWrapper.AddToClassList("search-wrapper");
        searchWrapper.style.paddingLeft = 16;
        searchWrapper.style.paddingRight = 16;
        searchWrapper.style.paddingTop = 16;
        searchWrapper.style.paddingBottom = 16;
        searchWrapper.style.width = Length.Percent(90);

        TextField searchField = CreateSearchField();
        searchWrapper.Add(searchField);

        return searchWrapper;
    }

    private static TextField CreateSearchField()
    {
        TextField searchField = new TextField();
        searchField.name = "searchField";
        searchField.AddToClassList("search-field");
        searchField.value = "";
        searchField.SetValueWithoutNotify("");

        // Styling
        searchField.style.width = Length.Percent(100);
        searchField.style.height = 100;
        searchField.style.borderBottomWidth = 2;
        searchField.style.borderTopWidth = 2;
        searchField.style.borderLeftWidth = 2;
        searchField.style.borderRightWidth = 2;
        searchField.style.marginLeft = Length.Percent(5);
        searchField.style.paddingLeft = 40;
        searchField.style.borderBottomColor = Color.black;
        searchField.style.borderTopColor = Color.black;
        searchField.style.borderLeftColor = Color.black;
        searchField.style.borderRightColor = Color.black;
        searchField.style.borderTopLeftRadius = 50;
        searchField.style.borderTopRightRadius = 50;
        searchField.style.borderBottomLeftRadius = 50;
        searchField.style.borderBottomRightRadius = 50;
        searchField.style.fontSize = 35;

        // Add placeholder functionality
        SetupSearchFieldPlaceholder(searchField);

        return searchField;
    }

    private static void SetupSearchFieldPlaceholder(TextField searchField)
    {
        string placeholderText = "Search";
        bool isPlaceholderActive = true;

        // Set initial placeholder
        searchField.SetValueWithoutNotify(placeholderText);
        searchField.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);

        searchField.RegisterCallback<FocusInEvent>(evt =>
        {
            if (isPlaceholderActive)
            {
                searchField.SetValueWithoutNotify("");
                searchField.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                isPlaceholderActive = false;
            }
        });

        searchField.RegisterCallback<FocusOutEvent>(evt =>
        {
            if (string.IsNullOrEmpty(searchField.value.Trim()))
            {
                searchField.SetValueWithoutNotify(placeholderText);
                searchField.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                isPlaceholderActive = true;
            }
        });
    }
}