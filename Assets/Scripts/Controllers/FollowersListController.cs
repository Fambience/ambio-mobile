using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class FollowersListController : MonoBehaviour
{
    [Header("UI Documents")]
    [SerializeField] private UIDocument uiDocument;
    
    private VisualElement mainContainer;
    private VisualElement tabBar;
    private Button followersTab;
    private Button followingTab;
    private VisualElement searchSection;
    private TextField searchField;
    private ScrollView userListContainer;
    
    // Current active tab
    private bool isFollowersTabActive = true;
    
    // Dummy data
    private List<FollowingUserData> followersData = new List<FollowingUserData>();
    private List<FollowingUserData> followingData = new List<FollowingUserData>();
    private List<FollowingUserData> filteredUsers = new List<FollowingUserData>();

    private void Start()
    {
        InitializeDummyData();
        SetupUI();
        SetupEventListeners();
        DisplayUsers();
    }

    private void InitializeDummyData()
    {
        // Dummy followers data
        followersData = new List<FollowingUserData>
        {
            new FollowingUserData("designstudio_pro", "Sarah Johnson", "Following"),
            new FollowingUserData("creativemind_88", "Alex Rivera", "Following"),
            new FollowingUserData("pixel_wizard", "Marcus Chen", "Follow"),
            new FollowingUserData("ui_master_2024", "Emily Davis", "Following"),
            new FollowingUserData("graphic_genius", "David Wilson", "Follow"),
            new FollowingUserData("color_theory_expert", "Jessica Brown", "Following"),
            new FollowingUserData("minimal_design_co", "Ryan Martinez", "Follow"),
            new FollowingUserData("brand_identity_pro", "Lisa Anderson", "Following"),
            new FollowingUserData("designstudio_pro", "Sarah Johnson", "Following"),
            new FollowingUserData("creativemind_88", "Alex Rivera", "Following"),
            new FollowingUserData("pixel_wizard", "Marcus Chen", "Follow"),
            new FollowingUserData("ui_master_2024", "Emily Davis", "Following"),
            new FollowingUserData("graphic_genius", "David Wilson", "Follow"),
            new FollowingUserData("color_theory_expert", "Jessica Brown", "Following"),
            new FollowingUserData("minimal_design_co", "Ryan Martinez", "Follow"),
            new FollowingUserData("brand_identity_pro", "Lisa Anderson", "Following")
        };

        // Dummy following data  
        followingData = new List<FollowingUserData>
        {
            new FollowingUserData("pixelperfectstudio", "Himanshu Mahto", "Following"),
            new FollowingUserData("creative_director_x", "Michael Thompson", "Following"),
            new FollowingUserData("design_inspiration", "Nina Patel", "Following"),
            new FollowingUserData("modern_ui_designs", "Carlos Rodriguez", "Following"),
            new FollowingUserData("typography_master", "Amanda White", "Following"),
            new FollowingUserData("web_design_guru", "Kevin Lee", "Following"),
            new FollowingUserData("designstudio_pro", "Sarah Johnson", "Following"),
            new FollowingUserData("creativemind_88", "Alex Rivera", "Following"),
            new FollowingUserData("pixel_wizard", "Marcus Chen", "Follow"),
            new FollowingUserData("ui_master_2024", "Emily Davis", "Following"),
            new FollowingUserData("graphic_genius", "David Wilson", "Follow"),
            new FollowingUserData("color_theory_expert", "Jessica Brown", "Following"),
            new FollowingUserData("minimal_design_co", "Ryan Martinez", "Follow"),
            new FollowingUserData("brand_identity_pro", "Lisa Anderson", "Following")
        };
    }

    private void SetupUI()
    {
        var root = uiDocument.rootVisualElement;
        
        // Get main container and tab bar
        mainContainer = root.Q<VisualElement>("main-container");
        tabBar = root.Q<VisualElement>("tabBar");
        followersTab = root.Q<Button>("followersTab");
        followingTab = root.Q<Button>("followingTab");

        // Create and add search section
        CreateSearchSection();
        
        // Create user list container
        CreateUserListContainer();
    }

    private void CreateSearchSection()
    {
        searchSection = ProfileScreenSearchUIBuilder.CreateHeaderSection(OnFilterClicked);
        searchSection.style.marginTop = 20;
        searchSection.style.marginBottom = 20;
        
        // Insert search section after tab bar
        int tabBarIndex = mainContainer.IndexOf(tabBar);
        mainContainer.Insert(tabBarIndex + 1, searchSection);
        
        // Get reference to search field for event handling
        searchField = searchSection.Q<TextField>("searchField");
        if (searchField != null)
        {
            searchField.RegisterValueChangedCallback(OnSearchValueChanged);
        }
    }

    private void CreateUserListContainer()
    {
        userListContainer = new ScrollView();
        userListContainer.name = "userListContainer";
        userListContainer.style.flexGrow = 1;
        userListContainer.style.marginTop = 10;
        userListContainer.style.marginBottom = 20;
        userListContainer.style.marginLeft = 10;
        userListContainer.style.marginRight = 10;
        
        // Hide scroll bars
        userListContainer.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        userListContainer.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        
        mainContainer.Add(userListContainer);
    }

    private void SetupEventListeners()
    {
        followersTab.clicked += () => SwitchTab(true);
        followingTab.clicked += () => SwitchTab(false);
    }

    private void SwitchTab(bool showFollowers)
    {
        isFollowersTabActive = showFollowers;
        
        // Update tab styling
        var followersTabParent = followersTab.parent;
        var followingTabParent = followingTab.parent;
        
        if (showFollowers)
        {
            followersTabParent.AddToClassList("selected");
            followingTabParent.RemoveFromClassList("selected");
        }
        else
        {
            followingTabParent.AddToClassList("selected");
            followersTabParent.RemoveFromClassList("selected");
        }
        
        // Clear search and display users
        ClearSearch();
        DisplayUsers();
    }

    private void DisplayUsers()
    {
        userListContainer.Clear();
        
        var currentData = isFollowersTabActive ? followersData : followingData;
        var usersToShow = string.IsNullOrEmpty(GetSearchText()) ? currentData : filteredUsers;
        
        foreach (var user in usersToShow)
        {
            var userElement = CreateUserListingElement(user);
            userListContainer.Add(userElement);
        }
    }

    private VisualElement CreateUserListingElement(FollowingUserData userData)
    {
        // Main container
        var mainContainer = new VisualElement();
        mainContainer.AddToClassList("main-container");
        ApplyUserListingStyles(mainContainer);

        // Sub container (icon + details)
        var subContainer = new VisualElement();
        subContainer.AddToClassList("sub-container");
        ApplySubContainerStyles(subContainer);

        // Designer icon
        var designerIcon = new VisualElement();
        designerIcon.AddToClassList("designer-icon");
        ApplyDesignerIconStyles(designerIcon);

        // User details container
        var userDetails = new VisualElement();
        userDetails.AddToClassList("user-details");
        ApplyUserDetailsStyles(userDetails);

        // Designer UID with truncation
        string displayUsername = TruncateUsername(userData.username, 15);
        var designerUID = new Label(displayUsername);
        designerUID.AddToClassList("designer-uid");
        ApplyDesignerUIDStyles(designerUID);

        // User basic details container
        var userBasicDetails = new VisualElement();
        userBasicDetails.AddToClassList("user-basic-details");
        ApplyUserBasicDetailsStyles(userBasicDetails);

        // Designer name
        var designerName = new Label(userData.name);
        designerName.AddToClassList("designer-name");
        ApplyDesignerNameStyles(designerName);

        // Follow button
        var followButton = new Button(() => OnFollowButtonClicked(userData)) { text = userData.followStatus };
        followButton.AddToClassList("follow-status");
        ApplyFollowButtonStyles(followButton, userData.followStatus);

        // Assemble the hierarchy
        userBasicDetails.Add(designerName);
        userDetails.Add(designerUID);
        userDetails.Add(userBasicDetails);
        subContainer.Add(designerIcon);
        subContainer.Add(userDetails);
        mainContainer.Add(subContainer);
        mainContainer.Add(followButton);

        return mainContainer;
    }

    private void ApplyUserListingStyles(VisualElement element)
    {
        element.style.marginTop = 20;
        element.style.backgroundColor = new Color(0.96f, 0.94f, 0.93f, 1f); // #F5F0ED
        element.style.width = Length.Percent(100);
        element.style.flexDirection = FlexDirection.Row;
        element.style.alignItems = Align.Center;
        element.style.justifyContent = Justify.SpaceBetween;
        element.style.paddingLeft = Length.Percent(10);
        element.style.paddingRight = Length.Percent(10);
    }

    private void ApplySubContainerStyles(VisualElement element)
    {
        element.style.backgroundColor = new Color(0.96f, 0.94f, 0.93f, 1f);
        element.style.flexDirection = FlexDirection.Row;
        element.style.alignItems = Align.Center;
        element.style.justifyContent = Justify.Center;
    }

    private void ApplyDesignerIconStyles(VisualElement element)
    {
        element.style.marginTop = 15;
        element.style.marginBottom = 15;
        element.style.width = 100;
        element.style.height = 100;
        element.style.backgroundColor = Color.black;
        element.style.borderTopLeftRadius = 50;
        element.style.borderTopRightRadius = 50;
        element.style.borderBottomLeftRadius = 50;
        element.style.borderBottomRightRadius = 50;
    }

    private void ApplyUserDetailsStyles(VisualElement element)
    {
        element.style.marginLeft = 30;
        element.style.flexDirection = FlexDirection.Column;
    }

    private void ApplyDesignerUIDStyles(Label element)
    {
        element.style.fontSize = 35;
        element.style.unityFontStyleAndWeight = FontStyle.Bold;
    }

    private void ApplyUserBasicDetailsStyles(VisualElement element)
    {
        element.style.flexDirection = FlexDirection.Row;
        element.style.marginTop = 10;
    }

    private void ApplyDesignerNameStyles(Label element)
    {
        element.style.fontSize = 30;
    }

    private void ApplyFollowButtonStyles(Button element, string status)
    {
        element.style.marginLeft = 20;
        element.style.paddingLeft = 20;
        element.style.paddingRight = 20;
        element.style.unityFontStyleAndWeight = FontStyle.Bold;
        element.style.fontSize = 30;
        element.style.borderBottomWidth = 5;
        element.style.borderTopWidth = 5;
        element.style.borderLeftWidth = 5;
        element.style.borderRightWidth = 5;
        element.style.borderTopLeftRadius = 20;
        element.style.borderTopRightRadius = 20;
        element.style.borderBottomLeftRadius = 20;
        element.style.borderBottomRightRadius = 20;
    
        // Fixed width for consistency
        element.style.width = 200;
        element.style.minWidth = 200;
    
        var borderColor = new Color(139f/255f, 76f/255f, 57f/255f, 1f); // rgb(139, 76, 57)
        var backgroundColor = new Color(0.96f, 0.94f, 0.93f, 1f); // #F5F0ED
    
        // Set border color (always the same)
        element.style.borderBottomColor = borderColor;
        element.style.borderTopColor = borderColor;
        element.style.borderLeftColor = borderColor;
        element.style.borderRightColor = borderColor;
    
        // Invert colors based on follow status
        if (status == "Following")
        {
            // Following: brown background, light text
            element.style.backgroundColor = borderColor;
            element.style.color = backgroundColor;
        }
        else
        {
            // Follow: light background, brown text
            element.style.backgroundColor = backgroundColor;
            element.style.color = borderColor;
        }
    }

    private void OnFollowButtonClicked(FollowingUserData userData)
    {
        // Toggle follow status
        userData.followStatus = userData.followStatus == "Follow" ? "Following" : "Follow";
        
        // Refresh the display
        DisplayUsers();
        
        Debug.Log($"Follow status changed for {userData.username}: {userData.followStatus}");
    }

    private void OnFilterClicked()
    {
        Debug.Log("Filter clicked");
        // Implement filter functionality here if needed
    }

    private void OnSearchValueChanged(ChangeEvent<string> evt)
    {
        FilterUsers(evt.newValue);
    }

    private void FilterUsers(string searchText)
    {
        if (string.IsNullOrEmpty(searchText) || searchText == "Search")
        {
            DisplayUsers();
            return;
        }

        var currentData = isFollowersTabActive ? followersData : followingData;
        filteredUsers = currentData.Where(user => 
            user.username.ToLower().Contains(searchText.ToLower()) ||
            user.name.ToLower().Contains(searchText.ToLower())
        ).ToList();

        DisplayUsers();
    }

    private string GetSearchText()
    {
        if (searchField == null) return "";
        
        string text = searchField.value;
        return text == "Search" ? "" : text;
    }

    private string TruncateUsername(string username, int maxLength)
    {
        if (string.IsNullOrEmpty(username) || username.Length <= maxLength)
            return username;
        
        return username.Substring(0, maxLength - 3) + "...";
    }

    private void ClearSearch()
    {
        if (searchField != null)
        {
            searchField.SetValueWithoutNotify("Search");
            searchField.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            filteredUsers.Clear();
        }
    }
}

[System.Serializable]
public class FollowingUserData
{
    public string username;
    public string name;
    public string followStatus;

    public FollowingUserData(string username, string name, string followStatus)
    {
        this.username = username;
        this.name = name;
        this.followStatus = followStatus;
    }
}