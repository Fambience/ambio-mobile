using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

public class FilterPopupHandler
{
    [SerializeField] private VisualTreeAsset filterPopupVisualTree;
    
    private VisualElement filterPopupOverlay;
    private VisualElement filterPopupContainer;
    private Button filterPopupCloseButton;
    private Button filterPopupApplyButton;
    
    // Dropdown references
    private DropdownController roomTypeDropdown;
    private DropdownController designStyleDropdown;
    private DropdownController sortByDropdown;
    
    // Budget level buttons
    private List<Button> budgetButtons = new List<Button>();
    private string selectedBudgetLevel = "Budget"; // Default selection
    
    public event Action OnFiltersApplied;
    
    // Dummy data - replace with API calls later
    private List<string> roomTypeOptions = new List<string>
    {
        "All Rooms", "Living Room", "Bedroom", "Kitchen", "Bathroom", 
        "Dining Room", "Office", "Kids Room", "Guest Room", "Outdoor"
    };
    
    private List<string> designStyleOptions = new List<string>
    {
        "All Styles", "Modern", "Contemporary", "Minimalist", "Scandinavian", 
        "Industrial", "Bohemian", "Traditional", "Rustic", "Art Deco", "Mid-Century"
    };
    
    private List<string> sortByOptions = new List<string>
    {
        "Most Popular", "Newest", "Highest Rated", "Price: Low to High", 
        "Price: High to Low", "Most Viewed"
    };
    
    public void Initialize(VisualTreeAsset popupVisualTree, VisualElement rootElement)
    {
        filterPopupVisualTree = popupVisualTree;
        
        if (filterPopupVisualTree == null)
        {
            Debug.LogWarning("Filter popup VisualTreeAsset not assigned!");
            return;
        }
        
        SetupPopupUI(rootElement);
        SetupDropdowns();
        SetupBudgetButtons();
        RegisterEvents();
        HidePopup();
        
        Debug.Log("Filter popup initialized successfully");
    }
    
    private void SetupPopupUI(VisualElement rootElement)
    {
        // Clone the visual tree and add it to the main UI
        filterPopupContainer = filterPopupVisualTree.CloneTree();
        filterPopupOverlay = filterPopupContainer.Q<VisualElement>("popupOverlay");
        
        if (filterPopupOverlay == null)
        {
            Debug.LogError("Filter popup overlay not found in the visual tree!");
            return;
        }
        
        // Position the popup
        filterPopupOverlay.style.position = Position.Absolute;
        filterPopupOverlay.style.top = -1350;
        filterPopupOverlay.style.left = 70;
        filterPopupOverlay.style.right = 0;
        filterPopupOverlay.style.bottom = 0;
        filterPopupOverlay.style.width = Length.Percent(100);
        filterPopupOverlay.style.height = Length.Percent(100);
        
        rootElement.Add(filterPopupContainer);
    }
    
    private void SetupDropdowns()
    {
        // Setup Room Type Dropdown
        roomTypeDropdown = new DropdownController(
            filterPopupContainer.Q<VisualElement>("roomTypeDropdown"),
            filterPopupContainer.Q<Label>("roomTypeSelectedText"),
            filterPopupContainer.Q<VisualElement>("roomTypeDropdownContent"),
            filterPopupContainer.Q<ScrollView>("roomTypeOptionsList"),
            filterPopupContainer.Q<TextField>("roomTypeSearchField"),
            GetUIDocument()
        );
        roomTypeDropdown.Initialize(roomTypeOptions, "All Rooms");
        
        // Setup Design Style Dropdown
        designStyleDropdown = new DropdownController(
            filterPopupContainer.Q<VisualElement>("designStyleDropdown"),
            filterPopupContainer.Q<Label>("designStyleSelectedText"),
            filterPopupContainer.Q<VisualElement>("designStyleDropdownContent"),
            filterPopupContainer.Q<ScrollView>("designStyleOptionsList"),
            filterPopupContainer.Q<TextField>("designStyleSearchField"),
            GetUIDocument()
        );
        designStyleDropdown.Initialize(designStyleOptions, "All Styles");
        
        // Setup Sort By Dropdown
        sortByDropdown = new DropdownController(
            filterPopupContainer.Q<VisualElement>("sortByDropdown"),
            filterPopupContainer.Q<Label>("sortBySelectedText"),
            filterPopupContainer.Q<VisualElement>("sortByDropdownContent"),
            filterPopupContainer.Q<ScrollView>("sortByOptionsList"),
            null, // No search field for sort by
            GetUIDocument()
        );
        sortByDropdown.Initialize(sortByOptions, "Most Popular");
    }
    
    private UIDocument GetUIDocument()
    {
        // Find the UIDocument from the root element
        var root = filterPopupContainer;
        while (root.parent != null)
        {
            root = root.parent;
        }
        
        // Try to find UIDocument component in the scene
        var uiDocuments = UnityEngine.Object.FindObjectsOfType<UIDocument>();
        foreach (var doc in uiDocuments)
        {
            if (doc.rootVisualElement == root)
            {
                return doc;
            }
        }
        
        return uiDocuments.Length > 0 ? uiDocuments[0] : null;
    }
    
    private void SetupBudgetButtons()
    {
        var budgetGroup = filterPopupContainer.Q<VisualElement>("budgetLevelGroup");
        if (budgetGroup == null) return;
        
        var budgetButton = budgetGroup.Q<Button>("budgetButton");
        var midRangeButton = budgetGroup.Q<Button>("midRangeButton");
        var luxuryButton = budgetGroup.Q<Button>("luxuryButton");
        
        if (budgetButton != null) budgetButtons.Add(budgetButton);
        if (midRangeButton != null) budgetButtons.Add(midRangeButton);
        if (luxuryButton != null) budgetButtons.Add(luxuryButton);
        
        // Set initial selection
        UpdateBudgetButtonStates("");
        
        // Register click events
        budgetButton?.RegisterCallback<ClickEvent>(evt => SelectBudgetLevel("Budget"));
        midRangeButton?.RegisterCallback<ClickEvent>(evt => SelectBudgetLevel("Mid-range"));
        luxuryButton?.RegisterCallback<ClickEvent>(evt => SelectBudgetLevel("Luxury"));
    }
    
    private void SelectBudgetLevel(string budgetLevel)
    {
        selectedBudgetLevel = budgetLevel;
        UpdateBudgetButtonStates(budgetLevel);
        Debug.Log($"Budget level selected: {budgetLevel}");
    }
    
    private void UpdateBudgetButtonStates(string selectedLevel)
    {
        foreach (var button in budgetButtons)
        {
            button.RemoveFromClassList("budget-option--active");
            if (button.text == selectedLevel)
            {
                button.AddToClassList("budget-option--active");
            }
        }
    }
    
    private void RegisterEvents()
    {
        // Get references to buttons
        filterPopupCloseButton = filterPopupContainer.Q<Button>("closeButton");
        filterPopupApplyButton = filterPopupContainer.Q<Button>("applyButton");
        
        // Register button events
        if (filterPopupCloseButton != null)
            filterPopupCloseButton.RegisterCallback<ClickEvent>(evt => HidePopup());
            
        if (filterPopupApplyButton != null)
            filterPopupApplyButton.RegisterCallback<ClickEvent>(evt => ApplyFilters());
        
        // Register overlay click to close popup
        filterPopupOverlay.RegisterCallback<ClickEvent>(evt => 
        {
            if (evt.target == filterPopupOverlay)
                HidePopup();
        });
    }
    
    public void ShowPopup()
    {
        if (filterPopupOverlay != null)
        {
            filterPopupOverlay.style.display = DisplayStyle.Flex;
            filterPopupOverlay.RemoveFromClassList("hidden");
            Debug.Log("Filter popup shown");
        }
    }
    
    public void HidePopup()
    {
        if (filterPopupOverlay != null)
        {
            filterPopupOverlay.style.display = DisplayStyle.None;
            filterPopupOverlay.AddToClassList("hidden");
            
            // Close all dropdowns when hiding popup
            roomTypeDropdown?.CloseDropdown();
            designStyleDropdown?.CloseDropdown();
            sortByDropdown?.CloseDropdown();
            
            Debug.Log("Filter popup hidden");
        }
    }
    
    private void ApplyFilters()
    {
        if (filterPopupContainer == null) return;
        
        var filterData = GetCurrentFilterData();
        LogFilterData(filterData);
        
        // Notify listeners that filters have been applied
        OnFiltersApplied?.Invoke();
        
        HidePopup();
    }
    
    public FilterData GetCurrentFilterData()
    {
        if (filterPopupContainer == null) return new FilterData();
        
        var filterData = new FilterData
        {
            RoomType = roomTypeDropdown?.GetSelectedValue() ?? "All Rooms",
            DesignStyle = designStyleDropdown?.GetSelectedValue() ?? "All Styles",
            BudgetLevel = selectedBudgetLevel,
            SortBy = sortByDropdown?.GetSelectedValue() ?? "Most Popular"
        };
        
        return filterData;
    }
    
    private void LogFilterData(FilterData filterData)
    {
        Debug.Log($"Filters Applied - Room Type: {filterData.RoomType}, Design Style: {filterData.DesignStyle}");
        Debug.Log($"Budget Level: {filterData.BudgetLevel}, Sort by: {filterData.SortBy}");
    }
    
    // Methods for API integration - call these when you get data from API
    public void UpdateRoomTypeOptions(List<string> newOptions)
    {
        roomTypeOptions = newOptions;
        roomTypeDropdown?.UpdateOptions(newOptions);
    }
    
    public void UpdateDesignStyleOptions(List<string> newOptions)
    {
        designStyleOptions = newOptions;
        designStyleDropdown?.UpdateOptions(newOptions);
    }
    
    public void UpdateSortByOptions(List<string> newOptions)
    {
        sortByOptions = newOptions;
        sortByDropdown?.UpdateOptions(newOptions);
    }
}

// Helper class to manage individual dropdowns
public class DropdownController
{
    private VisualElement dropdown;
    private VisualElement dropdownTrigger;
    private Label selectedText;
    private VisualElement dropdownContent;
    private ScrollView optionsList;
    private TextField searchField;
    private Label dropdownArrow;
    private UIDocument uiDocument;
    
    private List<string> allOptions = new List<string>();
    private string selectedValue;
    private bool isOpen = false;
    
    public DropdownController(VisualElement dropdown, Label selectedText, VisualElement dropdownContent, 
                            ScrollView optionsList, TextField searchField, UIDocument uiDocument)
    {
        this.dropdown = dropdown;
        this.selectedText = selectedText;
        this.dropdownContent = dropdownContent;
        this.optionsList = optionsList;
        this.searchField = searchField;
        this.uiDocument = uiDocument;
        
        // Find the dropdown trigger and arrow
        this.dropdownTrigger = dropdown.Q<VisualElement>("roomTypeDropdownTrigger") ?? 
                              dropdown.Q<VisualElement>("designStyleDropdownTrigger") ?? 
                              dropdown.Q<VisualElement>("sortByDropdownTrigger");
        this.dropdownArrow = dropdownTrigger?.Q<Label>();
    }
    
    public void Initialize(List<string> options, string defaultValue)
    {
        allOptions = new List<string>(options);
        selectedValue = defaultValue;
        selectedText.text = defaultValue;
        
        SetupDropdownContentStyles();
        SetupSearchFieldStyles();
        SetupEvents();
        PopulateOptions(allOptions);
    }
    
    private void SetupDropdownContentStyles()
    {
        if (dropdownContent == null) return;
        
        // Apply dropdown content styles directly
        dropdownContent.style.position = Position.Absolute;
        dropdownContent.style.backgroundColor = new Color(1f, 1f, 1f, 1f); // White background
        dropdownContent.style.borderTopWidth = 0;
        dropdownContent.style.borderLeftWidth = 2;
        dropdownContent.style.borderRightWidth = 2;
        dropdownContent.style.borderBottomWidth = 2;
        dropdownContent.style.borderTopColor = new Color(0.545f, 0.298f, 0.224f, 1f); // #8B4C39
        dropdownContent.style.borderLeftColor = new Color(0.545f, 0.298f, 0.224f, 1f);
        dropdownContent.style.borderRightColor = new Color(0.545f, 0.298f, 0.224f, 1f);
        dropdownContent.style.borderBottomColor = new Color(0.545f, 0.298f, 0.224f, 1f);
        dropdownContent.style.borderBottomLeftRadius = 12;
        dropdownContent.style.borderBottomRightRadius = 12;
        dropdownContent.style.maxHeight = 480;
        dropdownContent.style.overflow = Overflow.Hidden;
        dropdownContent.style.display = DisplayStyle.None;
    }
    
    private void SetupSearchFieldStyles()
    {
        if (searchField == null) return;
        
        // Style the search field wrapper
        var searchWrapper = searchField.parent;
        if (searchWrapper != null)
        {
            searchWrapper.style.height = 100;
            searchWrapper.style.paddingTop = 16;
            searchWrapper.style.paddingBottom = 16;
            searchWrapper.style.paddingLeft = 16;
            searchWrapper.style.paddingRight = 16;
            searchWrapper.style.borderBottomWidth = 1;
            searchWrapper.style.borderBottomColor = new Color(0.878f, 0.878f, 0.878f, 1f); // #E0E0E0
            searchWrapper.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f); // #FAFAFA
        }
        
        // Style the search field itself
        searchField.style.width = 525;
        searchField.style.height = 70;
        searchField.style.marginLeft = 0;
        searchField.style.marginRight = 0;
        searchField.style.paddingLeft = 10;
        searchField.style.paddingRight = 10;
        searchField.style.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparent
        searchField.style.borderBottomWidth = 0;
        searchField.style.borderLeftWidth = 0;
        searchField.style.borderRightWidth = 0;
        searchField.style.borderTopWidth = 0;
        searchField.style.fontSize = 20;
        searchField.style.color = new Color(0.2f, 0.2f, 0.2f, 1f); // #333333
        
        // Style the text input part
        var textInput = searchField.Q(className: "unity-text-input");
        if (textInput != null)
        {
            // textInput.style.borderWidth = 0;
            textInput.style.backgroundColor = Color.clear;
            textInput.style.fontSize = 20;
            textInput.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            textInput.style.paddingTop = 0;
            textInput.style.paddingBottom = 0;
            textInput.style.paddingLeft = 0;
            textInput.style.paddingRight = 0;
            textInput.style.marginTop = 0;
            textInput.style.marginBottom = 0;
            textInput.style.marginLeft = 0;
            textInput.style.marginRight = 0;
        }
    }
    
    private void SetupEvents()
    {
        // Toggle dropdown on trigger click
        dropdownTrigger?.RegisterCallback<ClickEvent>(evt => ToggleDropdown());
        
        // Setup search functionality if search field exists
        if (searchField != null)
        {
            searchField.RegisterCallback<ChangeEvent<string>>(evt => FilterOptions(evt.newValue));
            
            // Prevent search field clicks from closing dropdown
            searchField.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            
            // Clear search field when it gains focus
            searchField.RegisterCallback<FocusInEvent>(evt => 
            {
                if (searchField.value == selectedValue)
                {
                    searchField.value = "";
                }
            });
        }
        
        // Close dropdown when clicking outside
        uiDocument.rootVisualElement.RegisterCallback<ClickEvent>(evt => 
        {
            if (!IsClickInsideDropdown(evt.target as VisualElement))
            {
                CloseDropdown();
            }
        });
    }
    
    private bool IsClickInsideDropdown(VisualElement target)
    {
        var current = target;
        while (current != null)
        {
            if (current == dropdown || current == dropdownContent)
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }
    
    private void ToggleDropdown()
    {
        if (isOpen)
            CloseDropdown();
        else
            OpenDropdown();
    }
    
    private void OpenDropdown()
    {
        if (dropdownTrigger == null || dropdownContent == null) return;
        
        // Position the dropdown content relative to the trigger
        var triggerBounds = dropdownTrigger.worldBound;
        var rootBounds = uiDocument.rootVisualElement.worldBound;
        
        // Calculate position relative to root
        float leftPosition = triggerBounds.x - rootBounds.x;
        float topPosition = triggerBounds.y + triggerBounds.height - rootBounds.y;
        
        dropdownContent.style.position = Position.Absolute;
        dropdownContent.style.left = leftPosition;
        dropdownContent.style.top = topPosition;
        dropdownContent.style.width = triggerBounds.width;
        dropdownContent.style.display = DisplayStyle.Flex;
        
        // Add rotation to arrow
        if (dropdownArrow != null)
        {
            dropdownArrow.AddToClassList("rotated");
        }
        
        // Move dropdown to root level to ensure it appears on top
        if (dropdownContent.parent != uiDocument.rootVisualElement)
        {
            dropdownContent.RemoveFromHierarchy();
            uiDocument.rootVisualElement.Add(dropdownContent);
        }
        
        isOpen = true;
        
        // Focus search field if available, but don't put selected value in it
        if (searchField != null)
        {
            searchField.value = "";
            searchField.Focus();
        }
    }
    
    public void CloseDropdown()
    {
        if (dropdownContent == null) return;
        
        dropdownContent.style.display = DisplayStyle.None;
        isOpen = false;
        
        // Remove rotation from arrow
        if (dropdownArrow != null)
        {
            dropdownArrow.RemoveFromClassList("rotated");
        }
        
        // Clear search when closing and reset options
        if (searchField != null)
        {
            searchField.value = "";
            PopulateOptions(allOptions);
        }
        
        // Move dropdown back to its original parent if it was moved to root
        if (dropdownContent.parent == uiDocument.rootVisualElement)
        {
            dropdownContent.RemoveFromHierarchy();
            dropdown.Add(dropdownContent);
        }
    }
    
    private void FilterOptions(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            PopulateOptions(allOptions);
        }
        else
        {
            var filteredOptions = allOptions.Where(option => 
                option.ToLower().Contains(searchTerm.ToLower())).ToList();
            PopulateOptions(filteredOptions);
        }
    }
    
    private void PopulateOptions(List<string> options)
    {
        if (optionsList == null) return;
        
        optionsList.Clear();
        
        optionsList.style.maxHeight = 300;
        
        foreach (string option in options)
        {
            var optionContainer = new VisualElement();
            optionContainer.AddToClassList("option-item");
            optionContainer.style.flexDirection = FlexDirection.Row;
            optionContainer.style.alignItems = Align.Center;
            optionContainer.style.paddingTop = 12;
            optionContainer.style.paddingBottom = 12;
            optionContainer.style.paddingLeft = 16;
            optionContainer.style.paddingRight = 16;
            optionContainer.style.height = 80;
            optionContainer.style.borderBottomWidth = 1;
            optionContainer.style.borderBottomColor = new Color(0.941f, 0.941f, 0.941f, 1f); // #F0F0F0
            
            // Create option text
            var optionText = new Label(option);
            optionText.AddToClassList("option-text");
            optionText.style.fontSize = 25; // Adjusted from USS 35px
            optionText.style.color = Color.black;
            optionText.style.flexGrow = 1;
            
            // Highlight selected option
            if (option == selectedValue)
            {
                optionContainer.AddToClassList("selected");
                optionContainer.style.backgroundColor = new Color(0.545f, 0.298f, 0.224f, 1f); // #8B4C39
                optionText.style.color = Color.white;
            }
            
            // Add hover effects
            optionContainer.RegisterCallback<MouseEnterEvent>(evt => 
            {
                if (option != selectedValue)
                {
                    optionContainer.style.backgroundColor = new Color(0.973f, 0.973f, 0.973f, 1f); // #F8F8F8
                }
            });
            
            optionContainer.RegisterCallback<MouseLeaveEvent>(evt => 
            {
                if (option != selectedValue)
                {
                    optionContainer.style.backgroundColor = Color.clear;
                }
            });
            
            // Add click event
            optionContainer.RegisterCallback<ClickEvent>(evt => SelectOption(option));
            
            optionContainer.Add(optionText);
            optionsList.Add(optionContainer);
        }
    }
    
    private void SelectOption(string option)
    {
        selectedValue = option;
        selectedText.text = option;
        
        // Clear search field instead of showing selected value
        if (searchField != null)
        {
            searchField.value = "";
        }
        
        CloseDropdown();
        
        Debug.Log($"Selected option: {option}");
    }
    
    public string GetSelectedValue()
    {
        return selectedValue;
    }
    
    public void UpdateOptions(List<string> newOptions)
    {
        allOptions = new List<string>(newOptions);
        PopulateOptions(allOptions);
        
        // Reset selection if current selection is not in new options
        if (!allOptions.Contains(selectedValue) && allOptions.Count > 0)
        {
            SelectOption(allOptions[0]);
        }
    }
}

[System.Serializable]
public class FilterData
{
    public string RoomType = "All Rooms";
    public string DesignStyle = "All Styles";
    public string BudgetLevel = "Budget";
    public string SortBy = "Most Popular";
}