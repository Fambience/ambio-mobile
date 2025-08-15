using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class CreatePostDropdownHandler : MonoBehaviour
{
    private VisualElement root;
    
    public System.Action OnSelectionChanged;

    private VisualElement roomTypeDropdown;
    private VisualElement roomTypeDropdownTrigger;
    private VisualElement roomTypeDropdownContent;
    private Label roomTypeSelectedText;
    private Label roomTypeDropdownArrow;
    private TextField roomTypeSearchField;
    private ScrollView roomTypeOptionsList;

    private VisualElement designStyleDropdown;
    private VisualElement designStyleDropdownTrigger;
    private VisualElement designStyleDropdownContent;
    private Label designStyleSelectedText;
    private Label designStyleDropdownArrow;
    private TextField designStyleSearchField;
    private ScrollView designStyleOptionsList;

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
            "Terrace"
        });

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
            "Transitional"
        });

        filteredRoomTypes = new List<string>(roomTypes);
        filteredDesignStyles = new List<string>(designStyles);
    }

    private void BindUIElements()
    {
        roomTypeDropdown = root.Q<VisualElement>("roomTypeDropdown");
        roomTypeDropdownTrigger = root.Q<VisualElement>("roomTypeDropdownTrigger");
        roomTypeDropdownContent = root.Q<VisualElement>("roomTypeDropdownContent");
        roomTypeSelectedText = root.Q<Label>("roomTypeSelectedText");
        roomTypeDropdownArrow = roomTypeDropdownTrigger?.Q<Label>();
        roomTypeSearchField = root.Q<TextField>("roomTypeSearchField");
        roomTypeOptionsList = root.Q<ScrollView>("roomTypeOptionsList");

        designStyleDropdown = root.Q<VisualElement>("designStyleDropdown");
        designStyleDropdownTrigger = root.Q<VisualElement>("DesignStyleDropdownTrigger");
        designStyleDropdownContent = root.Q<VisualElement>("designStyleDropdownContent");
        designStyleSelectedText = root.Q<Label>("designStyleSelectedText");
        designStyleDropdownArrow = designStyleDropdownTrigger?.Q<Label>();
        designStyleSearchField = root.Q<TextField>("designStyleSearchField");
        designStyleOptionsList = root.Q<ScrollView>("designStyleOptionsList");
    }

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
            roomTypeDropdownTrigger.RegisterCallback<ClickEvent>(_ => ToggleRoomTypeDropdown());

        if (roomTypeSearchField != null)
            roomTypeSearchField.RegisterValueChangedCallback(evt => FilterRoomTypeOptions(evt.newValue));

        root.RegisterCallback<ClickEvent>(evt =>
        {
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

            if (roomType == selectedRoomType)
            {
                // Debug.Log($"Himanshu:  {selectedRoomType}");
                option.AddToClassList("selected");
            }

            option.RegisterCallback<ClickEvent>(_ => SelectRoomType(roomType));
            roomTypeOptionsList.Add(option);
        }
    }

    private void FilterRoomTypeOptions(string searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
            filteredRoomTypes = new List<string>(roomTypes);
        else
            filteredRoomTypes = roomTypes.Where(roomType =>
                roomType.ToLower().Contains(searchQuery.ToLower())).ToList();

        PopulateRoomTypeDropdownOptions();
    }

    private void ToggleRoomTypeDropdown()
    {
        if (roomTypeDropdownContent == null) return;

        bool isOpen = roomTypeDropdownContent.style.display == DisplayStyle.Flex;

        if (isOpen) CloseRoomTypeDropdown();
        else OpenRoomTypeDropdown();
    }

    private void OpenRoomTypeDropdown()
    {
        if (roomTypeDropdownContent == null || roomTypeDropdownTrigger == null) return;

        CloseDesignStyleDropdown();

        var triggerBounds = roomTypeDropdownTrigger.worldBound;
        var rootBounds = root.worldBound;

        float leftPosition = triggerBounds.x - rootBounds.x;
        float topPosition = triggerBounds.y + triggerBounds.height - rootBounds.y;

        roomTypeDropdownContent.style.position = Position.Absolute;
        roomTypeDropdownContent.style.left = leftPosition;
        roomTypeDropdownContent.style.top = topPosition;
        roomTypeDropdownContent.style.width = triggerBounds.width;
        roomTypeDropdownContent.style.display = DisplayStyle.Flex;

        if (roomTypeDropdownArrow != null)
            roomTypeDropdownArrow.AddToClassList("rotated");

        if (roomTypeDropdownContent.parent != root)
        {
            roomTypeDropdownContent.RemoveFromHierarchy();
            root.Add(roomTypeDropdownContent);
        }

        if (roomTypeSearchField != null)
            roomTypeSearchField.Focus();
    }

    private void CloseRoomTypeDropdown()
    {
        if (roomTypeDropdownContent == null) return;

        roomTypeDropdownContent.style.display = DisplayStyle.None;

        if (roomTypeDropdownArrow != null)
            roomTypeDropdownArrow.RemoveFromClassList("rotated");

        if (roomTypeSearchField != null)
        {
            roomTypeSearchField.value = "";
            filteredRoomTypes = new List<string>(roomTypes);
            PopulateRoomTypeDropdownOptions();
        }
    }

    private void SelectRoomType(string roomType)
    {
        selectedRoomType = roomType;
        Debug.Log($"Selected Room Type 1: {roomType}");
        if (roomTypeSelectedText != null)
        {
            roomTypeSelectedText.text = roomType;
            roomTypeSelectedText.AddToClassList("has-selection");
        }

        CloseRoomTypeDropdown();
        OnSelectionChanged?.Invoke(); 
    }

    private void SetupDesignStyleDropdown()
    {
        if (designStyleDropdownContent != null)
            designStyleDropdownContent.style.display = DisplayStyle.None;

        PopulateDesignStyleDropdownOptions();

        if (designStyleDropdownTrigger != null)
            designStyleDropdownTrigger.RegisterCallback<ClickEvent>(_ => ToggleDesignStyleDropdown());

        if (designStyleSearchField != null)
            designStyleSearchField.RegisterValueChangedCallback(evt => FilterDesignStyleOptions(evt.newValue));

        root.RegisterCallback<ClickEvent>(evt =>
        {
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
            
            if (designStyle == selectedDesignStyle)
                option.AddToClassList("selected");

            option.RegisterCallback<ClickEvent>(_ => SelectDesignStyle(designStyle));
            designStyleOptionsList.Add(option);
        }
    }

    private void FilterDesignStyleOptions(string searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
            filteredDesignStyles = new List<string>(designStyles);
        else
            filteredDesignStyles = designStyles.Where(designStyle =>
                designStyle.ToLower().Contains(searchQuery.ToLower())).ToList();

        PopulateDesignStyleDropdownOptions();
    }

    private void ToggleDesignStyleDropdown()
    {
        if (designStyleDropdownContent == null) return;

        bool isOpen = designStyleDropdownContent.style.display == DisplayStyle.Flex;

        if (isOpen) CloseDesignStyleDropdown();
        else OpenDesignStyleDropdown();
    }

    private void OpenDesignStyleDropdown()
    {
        if (designStyleDropdownContent == null || designStyleDropdownTrigger == null) return;

        CloseRoomTypeDropdown();

        var triggerBounds = designStyleDropdownTrigger.worldBound;
        var rootBounds = root.worldBound;

        float leftPosition = triggerBounds.x - rootBounds.x;
        float topPosition = triggerBounds.y + triggerBounds.height - rootBounds.y;

        designStyleDropdownContent.style.position = Position.Absolute;
        designStyleDropdownContent.style.left = leftPosition;
        designStyleDropdownContent.style.top = topPosition;
        designStyleDropdownContent.style.width = triggerBounds.width;
        designStyleDropdownContent.style.display = DisplayStyle.Flex;

        if (designStyleDropdownArrow != null)
            designStyleDropdownArrow.AddToClassList("rotated");

        if (designStyleDropdownContent.parent != root)
        {
            designStyleDropdownContent.RemoveFromHierarchy();
            root.Add(designStyleDropdownContent);
        }

        if (designStyleSearchField != null)
            designStyleSearchField.Focus();
    }

    public void CloseDesignStyleDropdown()
    {
        if (designStyleDropdownContent == null) return;

        designStyleDropdownContent.style.display = DisplayStyle.None;

        if (designStyleDropdownArrow != null)
            designStyleDropdownArrow.RemoveFromClassList("rotated");

        if (designStyleSearchField != null)
        {
            designStyleSearchField.value = "";
            filteredDesignStyles = new List<string>(designStyles);
            PopulateDesignStyleDropdownOptions();
        }
    }

    private void SelectDesignStyle(string designStyle)
    {
        selectedDesignStyle = designStyle;
        Debug.Log($"Selected Design Style: {designStyle}");
        if (designStyleSelectedText != null)
        {
            designStyleSelectedText.text = designStyle;
            designStyleSelectedText.AddToClassList("has-selection");
        }

        CloseDesignStyleDropdown();
        OnSelectionChanged?.Invoke(); 
    }

    public string GetSelectedRoomType()
    {
        Debug.Log($"Selected Room Type 2: {selectedRoomType}");
        return selectedRoomType;
    }

    public string GetSelectedDesignStyle() => selectedDesignStyle;

    public void UpdateRoomTypes(List<string> newRoomTypes)
    {
        roomTypes.Clear();
        roomTypes.AddRange(newRoomTypes);
        filteredRoomTypes = new List<string>(roomTypes);
        PopulateRoomTypeDropdownOptions();
    }

    public void UpdateDesignStyles(List<string> newDesignStyles)
    {
        designStyles.Clear();
        designStyles.AddRange(newDesignStyles);
        filteredDesignStyles = new List<string>(designStyles);
        PopulateDesignStyleDropdownOptions();
    }

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

        CloseRoomTypeDropdown();
        CloseDesignStyleDropdown();
        OnSelectionChanged?.Invoke();
    }

    public void CloseAllDropdowns()
    {
        CloseRoomTypeDropdown();
        CloseDesignStyleDropdown();
    }
}
