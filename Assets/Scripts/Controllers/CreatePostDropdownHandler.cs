using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class CreatePostDropdownHandler : MonoBehaviour
{
    private VisualElement root;
    
    // Room Type Dropdown elements
    private VisualElement roomTypeDropdown;
    private VisualElement roomTypeDropdownTrigger;
    private VisualElement roomTypeDropdownContent;
    private Label roomTypeSelectedText;
    private Label roomTypeDropdownArrow;
    private TextField roomTypeSearchField;
    private ScrollView roomTypeOptionsList;
    
    // Design Style Dropdown elements
    private VisualElement designStyleDropdown;
    private VisualElement designStyleDropdownTrigger;
    private VisualElement designStyleDropdownContent;
    private Label designStyleSelectedText;
    private Label designStyleDropdownArrow;
    private TextField designStyleSearchField;
    private ScrollView designStyleOptionsList;
    
    // Other input elements
    private VisualElement otherRoomTypeWrapper;
    private TextField otherRoomTypeField;
    private VisualElement otherDesignStyleWrapper;
    private TextField otherDesignStyleField;
    
    // Data storage
    private List<string> roomTypes = new List<string>();
    private List<string> designStyles = new List<string>();
    private List<string> filteredRoomTypes = new List<string>();
    private List<string> filteredDesignStyles = new List<string>();
    private string selectedRoomType = "";
    private string selectedDesignStyle = "";

    public void Initialize(VisualElement rootElement)
    {
        root = rootElement;
        InitializeDummyData();
        BindUIElements();
        SetupDropdowns();
    }
    
    private void InitializeDummyData()
    {
        // Dummy data for room types - replace with API data later
        roomTypes.AddRange(new string[]
        {
            "Living Room",
            "Bedroom",
            "Kitchen",
            "Bathroom",
            "Dining Room",
            "Home Office",
            "Guest Room",
            "Master Bedroom",
            "Kids Room",
            "Basement",
            "Attic",
            "Garage",
            "Balcony",
            "Terrace",
            "Others"
        });
        
        // Dummy data for design styles - replace with API data later
        designStyles.AddRange(new string[]
        {
            "Modern",
            "Contemporary",
            "Traditional",
            "Minimalist",
            "Scandinavian",
            "Industrial",
            "Bohemian",
            "Mid-Century Modern",
            "Rustic",
            "Mediterranean",
            "Art Deco",
            "Farmhouse",
            "Eclectic",
            "Transitional",
            "Others"
        });
        
        // Initialize filtered lists
        filteredRoomTypes = new List<string>(roomTypes);
        filteredDesignStyles = new List<string>(designStyles);
    }
    
    private void BindUIElements()
    {
        // Room Type Dropdown
        roomTypeDropdown = root.Q<VisualElement>("roomTyeDropdown");
        roomTypeDropdownTrigger = root.Q<VisualElement>("roomTypeDropdownTrigger");
        roomTypeDropdownContent = root.Q<VisualElement>("roomTypeDropdownContent");
        roomTypeSelectedText = root.Q<Label>("roomTypeSelectedText");
        roomTypeDropdownArrow = roomTypeDropdownTrigger?.Q<Label>();
        roomTypeSearchField = root.Q<TextField>("roomTypeSearchField");
        roomTypeOptionsList = root.Q<ScrollView>("roomTypeOptionsList");
        
        // Design Style Dropdown
        designStyleDropdown = root.Q<VisualElement>("designStyleDropdown");
        designStyleDropdownTrigger = root.Q<VisualElement>("DesignStyleDropdownTrigger");
        designStyleDropdownContent = root.Q<VisualElement>("designStyleDropdownContent");
        designStyleSelectedText = root.Q<Label>("designStyleSelectedText");
        designStyleDropdownArrow = designStyleDropdownTrigger?.Q<Label>();
        designStyleSearchField = root.Q<TextField>("designStyleSearchField");
        designStyleOptionsList = root.Q<ScrollView>("designStyleOptionsList");
        
        // Other input elements
        otherRoomTypeWrapper = root.Q<VisualElement>("other-room-type-wrapper");
        otherRoomTypeField = root.Q<TextField>("otherRoomType");
        otherDesignStyleWrapper = root.Q<VisualElement>("other-design-style-wrapper");
        otherDesignStyleField = root.Q<TextField>("otherDesignStyle");
        
        // Initially hide the other input fields
        if (otherRoomTypeWrapper != null)
            otherRoomTypeWrapper.style.display = DisplayStyle.None;
        if (otherDesignStyleWrapper != null)
            otherDesignStyleWrapper.style.display = DisplayStyle.None;
        
        // Debug: Check if elements are found
        Debug.Log($"Room Type Dropdown Found: {roomTypeDropdown != null}");
        Debug.Log($"Design Style Dropdown Found: {designStyleDropdown != null}");
        Debug.Log($"Other Room Type Field Found: {otherRoomTypeField != null}");
        Debug.Log($"Other Design Style Field Found: {otherDesignStyleField != null}");
    }
    
    #region Dropdown Setup
    
    private void SetupDropdowns()
    {
        SetupRoomTypeDropdown();
        SetupDesignStyleDropdown();
    }
    
    private void SetupRoomTypeDropdown()
    {
        if (roomTypeDropdownContent != null)
            roomTypeDropdownContent.style.display = DisplayStyle.None;
            
        PopulateRoomTypeDropdownOptions();
        
        if (roomTypeDropdownTrigger != null)
        {
            roomTypeDropdownTrigger.RegisterCallback<ClickEvent>(_ => ToggleRoomTypeDropdown());
        }
        
        if (roomTypeSearchField != null)
        {
            roomTypeSearchField.RegisterValueChangedCallback(evt => FilterRoomTypeOptions(evt.newValue));
        }
        
        // Close dropdown when clicking outside
        root.RegisterCallback<ClickEvent>(evt => {
            if (roomTypeDropdownContent != null && 
                roomTypeDropdownContent.style.display == DisplayStyle.Flex && 
                roomTypeDropdown != null &&
                !roomTypeDropdown.worldBound.Contains(evt.position) &&
                !roomTypeDropdownContent.worldBound.Contains(evt.position))
            {
                CloseRoomTypeDropdown();
            }
        });
    }
    
    private void PopulateRoomTypeDropdownOptions()
    {
        if (roomTypeOptionsList == null) return;
        
        roomTypeOptionsList.Clear();
        foreach (var roomType in filteredRoomTypes)
        {
            var option = new Label(roomType);
            option.AddToClassList("option-text");
            
            // Add selection state
            if (roomType == selectedRoomType)
            {
                option.parent?.AddToClassList("selected");
            }
            
            option.RegisterCallback<ClickEvent>(_ => SelectRoomType(roomType));
            roomTypeOptionsList.Add(option);
        }
    }
    
    private void FilterRoomTypeOptions(string searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
        {
            filteredRoomTypes = new List<string>(roomTypes);
        }
        else
        {
            filteredRoomTypes = roomTypes.Where(roomType => 
                roomType.ToLower().Contains(searchQuery.ToLower())).ToList();
        }
        
        PopulateRoomTypeDropdownOptions();
    }
    
    private void ToggleRoomTypeDropdown()
    {
        if (roomTypeDropdownContent == null) return;
        
        bool isOpen = roomTypeDropdownContent.style.display == DisplayStyle.Flex;
        
        if (isOpen)
        {
            CloseRoomTypeDropdown();
        }
        else
        {
            OpenRoomTypeDropdown();
        }
    }
    
    private void OpenRoomTypeDropdown()
    {
        if (roomTypeDropdownContent == null || roomTypeDropdownTrigger == null) return;
        
        // Close other dropdown first
        CloseDesignStyleDropdown();
        
        // Position the dropdown content relative to the trigger
        var triggerBounds = roomTypeDropdownTrigger.worldBound;
        var rootBounds = root.worldBound;
        
        // Calculate position relative to root
        float leftPosition = triggerBounds.x - rootBounds.x;
        float topPosition = triggerBounds.y + triggerBounds.height - rootBounds.y;
        
        roomTypeDropdownContent.style.position = Position.Absolute;
        roomTypeDropdownContent.style.left = leftPosition;
        roomTypeDropdownContent.style.top = topPosition;
        roomTypeDropdownContent.style.width = triggerBounds.width;
        roomTypeDropdownContent.style.display = DisplayStyle.Flex;
        
        if (roomTypeDropdownArrow != null)
            roomTypeDropdownArrow.AddToClassList("rotated");
        
        // Move dropdown to root level to ensure it appears on top
        if (roomTypeDropdownContent.parent != root)
        {
            roomTypeDropdownContent.RemoveFromHierarchy();
            root.Add(roomTypeDropdownContent);
        }
        
        // Focus search field
        if (roomTypeSearchField != null)
            roomTypeSearchField.Focus();
        
        Debug.Log("Room Type Dropdown Opened");
    }
    
    private void CloseRoomTypeDropdown()
    {
        if (roomTypeDropdownContent == null) return;
        
        roomTypeDropdownContent.style.display = DisplayStyle.None;
        
        if (roomTypeDropdownArrow != null)
            roomTypeDropdownArrow.RemoveFromClassList("rotated");
        
        // Clear search and reset options
        if (roomTypeSearchField != null)
        {
            roomTypeSearchField.value = "";
            filteredRoomTypes = new List<string>(roomTypes);
            PopulateRoomTypeDropdownOptions();
        }
        
        Debug.Log("Room Type Dropdown Closed");
    }
    
    private void SelectRoomType(string roomType)
    {
        selectedRoomType = roomType;
        
        if (roomTypeSelectedText != null)
        {
            roomTypeSelectedText.text = roomType;
            roomTypeSelectedText.AddToClassList("has-selection");
        }
        
        // Show/hide other room type input based on selection
        ToggleOtherRoomTypeInput(roomType.ToLower() == "others");
        
        CloseRoomTypeDropdown();
        Debug.Log($"Selected room type: {roomType}");
    }
    
    #endregion
    
    #region Other Input Handlers
    
    private void ToggleOtherRoomTypeInput(bool show)
    {
        if (otherRoomTypeWrapper == null) return;
        
        if (show)
        {
            otherRoomTypeWrapper.style.display = DisplayStyle.Flex;
            // Focus the input field when shown
            if (otherRoomTypeField != null)
            {
                otherRoomTypeField.Focus();
            }
            Debug.Log("Other Room Type input shown");
        }
        else
        {
            otherRoomTypeWrapper.style.display = DisplayStyle.None;
            // Clear the field when hidden
            if (otherRoomTypeField != null)
            {
                otherRoomTypeField.value = "";
            }
            Debug.Log("Other Room Type input hidden");
        }
    }
    
    private void ToggleOtherDesignStyleInput(bool show)
    {
        if (otherDesignStyleWrapper == null) return;
        
        if (show)
        {
            otherDesignStyleWrapper.style.display = DisplayStyle.Flex;
            // Focus the input field when shown
            if (otherDesignStyleField != null)
            {
                otherDesignStyleField.Focus();
            }
            Debug.Log("Other Design Style input shown");
        }
        else
        {
            otherDesignStyleWrapper.style.display = DisplayStyle.None;
            // Clear the field when hidden
            if (otherDesignStyleField != null)
            {
                otherDesignStyleField.value = "";
            }
            Debug.Log("Other Design Style input hidden");
        }
    }
    
    #endregion
    
    #region Design Style Dropdown Setup
    
    private void SetupDesignStyleDropdown()
    {
        if (designStyleDropdownContent != null)
            designStyleDropdownContent.style.display = DisplayStyle.None;
            
        PopulateDesignStyleDropdownOptions();
        
        if (designStyleDropdownTrigger != null)
        {
            designStyleDropdownTrigger.RegisterCallback<ClickEvent>(_ => ToggleDesignStyleDropdown());
        }
        
        if (designStyleSearchField != null)
        {
            designStyleSearchField.RegisterValueChangedCallback(evt => FilterDesignStyleOptions(evt.newValue));
        }
        
        // Close dropdown when clicking outside
        root.RegisterCallback<ClickEvent>(evt => {
            if (designStyleDropdownContent != null && 
                designStyleDropdownContent.style.display == DisplayStyle.Flex && 
                designStyleDropdown != null &&
                !designStyleDropdown.worldBound.Contains(evt.position) &&
                !designStyleDropdownContent.worldBound.Contains(evt.position))
            {
                CloseDesignStyleDropdown();
            }
        });
    }
    
    private void PopulateDesignStyleDropdownOptions()
    {
        if (designStyleOptionsList == null) return;
        
        designStyleOptionsList.Clear();
        foreach (var designStyle in filteredDesignStyles)
        {
            var option = new Label(designStyle);
            option.AddToClassList("option-text");
            
            // Add selection state
            if (designStyle == selectedDesignStyle)
            {
                option.parent?.AddToClassList("selected");
            }
            
            option.RegisterCallback<ClickEvent>(_ => SelectDesignStyle(designStyle));
            designStyleOptionsList.Add(option);
        }
    }
    
    private void FilterDesignStyleOptions(string searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
        {
            filteredDesignStyles = new List<string>(designStyles);
        }
        else
        {
            filteredDesignStyles = designStyles.Where(designStyle => 
                designStyle.ToLower().Contains(searchQuery.ToLower())).ToList();
        }
        
        PopulateDesignStyleDropdownOptions();
    }
    
    private void ToggleDesignStyleDropdown()
    {
        if (designStyleDropdownContent == null) return;
        
        bool isOpen = designStyleDropdownContent.style.display == DisplayStyle.Flex;
        
        if (isOpen)
        {
            CloseDesignStyleDropdown();
        }
        else
        {
            OpenDesignStyleDropdown();
        }
    }
    
    private void OpenDesignStyleDropdown()
    {
        if (designStyleDropdownContent == null || designStyleDropdownTrigger == null) return;
        
        // Close other dropdown first
        CloseRoomTypeDropdown();
        
        // Position the dropdown content relative to the trigger
        var triggerBounds = designStyleDropdownTrigger.worldBound;
        var rootBounds = root.worldBound;
        
        // Calculate position relative to root
        float leftPosition = triggerBounds.x - rootBounds.x;
        float topPosition = triggerBounds.y + triggerBounds.height - rootBounds.y;
        
        designStyleDropdownContent.style.position = Position.Absolute;
        designStyleDropdownContent.style.left = leftPosition;
        designStyleDropdownContent.style.top = topPosition;
        designStyleDropdownContent.style.width = triggerBounds.width;
        designStyleDropdownContent.style.display = DisplayStyle.Flex;
        
        if (designStyleDropdownArrow != null)
            designStyleDropdownArrow.AddToClassList("rotated");
        
        // Move dropdown to root level to ensure it appears on top
        if (designStyleDropdownContent.parent != root)
        {
            designStyleDropdownContent.RemoveFromHierarchy();
            root.Add(designStyleDropdownContent);
        }
        
        // Focus search field
        if (designStyleSearchField != null)
            designStyleSearchField.Focus();
        
        Debug.Log("Design Style Dropdown Opened");
    }
    
    public void CloseDesignStyleDropdown()
    {
        if (designStyleDropdownContent == null) return;
        
        designStyleDropdownContent.style.display = DisplayStyle.None;
        
        if (designStyleDropdownArrow != null)
            designStyleDropdownArrow.RemoveFromClassList("rotated");
        
        // Clear search and reset options
        if (designStyleSearchField != null)
        {
            designStyleSearchField.value = "";
            filteredDesignStyles = new List<string>(designStyles);
            PopulateDesignStyleDropdownOptions();
        }
        
        Debug.Log("Design Style Dropdown Closed");
    }
    
    private void SelectDesignStyle(string designStyle)
    {
        selectedDesignStyle = designStyle;
        
        if (designStyleSelectedText != null)
        {
            designStyleSelectedText.text = designStyle;
            designStyleSelectedText.AddToClassList("has-selection");
        }
        
        // Show/hide other design style input based on selection
        ToggleOtherDesignStyleInput(designStyle.ToLower() == "others");
        
        CloseDesignStyleDropdown();
        Debug.Log($"Selected design style: {designStyle}");
    }
    
    #endregion
    
    #region Public API Methods
    
    // Public method to get selected room type
    public string GetSelectedRoomType()
    {
        // If "Others" is selected, return the custom input value
        if (selectedRoomType.ToLower() == "others" && otherRoomTypeField != null && !string.IsNullOrEmpty(otherRoomTypeField.value))
        {
            return otherRoomTypeField.value.Trim();
        }
        return selectedRoomType;
    }
    
    // Public method to get selected design style
    public string GetSelectedDesignStyle()
    {
        // If "Others" is selected, return the custom input value
        if (selectedDesignStyle.ToLower() == "others" && otherDesignStyleField != null && !string.IsNullOrEmpty(otherDesignStyleField.value))
        {
            return otherDesignStyleField.value.Trim();
        }
        return selectedDesignStyle;
    }
    
    // Method to update room types from API (call this when you get data from API)
    public void UpdateRoomTypes(List<string> newRoomTypes)
    {
        roomTypes.Clear();
        roomTypes.AddRange(newRoomTypes);
        filteredRoomTypes = new List<string>(roomTypes);
        PopulateRoomTypeDropdownOptions();
    }
    
    // Method to update design styles from API (call this when you get data from API)
    public void UpdateDesignStyles(List<string> newDesignStyles)
    {
        designStyles.Clear();
        designStyles.AddRange(newDesignStyles);
        filteredDesignStyles = new List<string>(designStyles);
        PopulateDesignStyleDropdownOptions();
    }
    
    // Public method to reset selections
    public void ResetSelections()
    {
        selectedRoomType = "";
        selectedDesignStyle = "";
        
        if (roomTypeSelectedText != null)
        {
            roomTypeSelectedText.text = "Select Room Type";
            roomTypeSelectedText.RemoveFromClassList("has-selection");
        }
        
        if (designStyleSelectedText != null)
        {
            designStyleSelectedText.text = "Select Design Style";
            designStyleSelectedText.RemoveFromClassList("has-selection");
        }
        
        // Hide and clear other input fields
        ToggleOtherRoomTypeInput(false);
        ToggleOtherDesignStyleInput(false);
        
        CloseRoomTypeDropdown();
        CloseDesignStyleDropdown();
        
        Debug.Log("Dropdown selections reset");
    }
    
    // Public method to close all dropdowns
    public void CloseAllDropdowns()
    {
        CloseRoomTypeDropdown();
        CloseDesignStyleDropdown();
    }
    
    // Public method to get other room type value
    public string GetOtherRoomTypeValue()
    {
        return otherRoomTypeField?.value?.Trim() ?? "";
    }
    
    // Public method to get other design style value
    public string GetOtherDesignStyleValue()
    {
        return otherDesignStyleField?.value?.Trim() ?? "";
    }
    
    // Public method to check if room type is "Others"
    public bool IsRoomTypeOthers()
    {
        return selectedRoomType.ToLower() == "others";
    }
    
    // Public method to check if design style is "Others"
    public bool IsDesignStyleOthers()
    {
        return selectedDesignStyle.ToLower() == "others";
    }
    
    #endregion
}