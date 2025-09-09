/*using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiniJSON;

public class UserFollowerFollowingController : MonoBehaviour
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
    
    // Data with UUID support
    private List<FollowingUserData> followersData = new List<FollowingUserData>();
    private List<FollowingUserData> followingData = new List<FollowingUserData>();
    private List<FollowingUserData> filteredUsers = new List<FollowingUserData>();

    // API endpoint
    private string baseURL => baseScript.baseURL;

    private void Start()
    {
        StartCoroutine(WaitForFollowerData());
    }
    
    private IEnumerator WaitForFollowerData()
    {
        while (ProfileDataHandlers.FollowersList.Count == 0 && ProfileDataHandlers.FollowingList.Count == 0)
        {
            yield return null;
        }

        InitializeData();
        SetupUI();
        SetupEventListeners();
        DisplayUsers();
    }

    private void InitializeData()
    {
        // Convert FollowersList from DataHandler to FollowingUserData (include UUID)
        followersData = ProfileDataHandlers.FollowersList
            .Select(u => new FollowingUserData(
                u.userId,           // Store UUID
                u.userName,
                $"{u.firstName} {u.lastName}".Trim(),
                "Following"         // They're following you, so you might be following them back
            )).ToList();

        followingData = ProfileDataHandlers.FollowingList
            .Select(u => new FollowingUserData(
                u.userId,           // Store UUID
                u.userName,
                $"{u.firstName} {u.lastName}".Trim(),
                "Following"         // You're following them
            )).ToList();
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

    // NEW: API Integration for Follow/Unfollow
    private void OnFollowButtonClicked(FollowingUserData userData)
    {
        // Start the API call coroutine
        StartCoroutine(ToggleFollowUser(userData));
    }

    private IEnumerator ToggleFollowUser(FollowingUserData userData)
    {
        Debug.Log($"[FollowAPI] Toggling follow status for user: {userData.username} (UUID: {userData.userId})");
        
        string token = AuthTokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[FollowAPI] No authentication token available");
            yield break;
        }

        if (string.IsNullOrEmpty(userData.userId))
        {
            Debug.LogError("[FollowAPI] User UUID is missing");
            yield break;
        }
        
        string apiUrl = $"{baseURL}/api/v1/profile/toggle-follow/{userData.userId}";
        Debug.Log($"[FollowAPI] Request URL: {apiUrl}");

        // Create POST request
        UnityWebRequest request = UnityWebRequest.PostWwwForm(apiUrl, "");
        request.SetRequestHeader("Authorization", token);
        request.SetRequestHeader("Content-Type", "application/json");

        // Send request
        yield return request.SendWebRequest();

        // Handle response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[FollowAPI] Success! Response: {request.downloadHandler.text}");
            
            try
            {
                // Parse response to get follow status
                var response = JSON.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;
                
                if (response != null && response.ContainsKey("followed"))
                {
                    bool isFollowed = (bool)response["followed"];
                    string message = response.ContainsKey("message") ? response["message"].ToString() : "";
                    
                    Debug.Log($"[FollowAPI] {message} - User is now {(isFollowed ? "followed" : "unfollowed")}");
                    
                    // Update UI based on API response
                    userData.followStatus = isFollowed ? "Following" : "Follow";
                    
                    // Refresh the display
                    DisplayUsers();
                    
                    // Update the data lists to maintain consistency
                    UpdateUserDataLists(userData, isFollowed);
                }
                else
                {
                    Debug.LogWarning("[FollowAPI] Invalid response format");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FollowAPI] Error parsing response: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[FollowAPI] Request failed - Result: {request.result}, Error: {request.error}");
            Debug.LogError($"[FollowAPI] Response code: {request.responseCode}");
            Debug.LogError($"[FollowAPI] Response text: {request.downloadHandler?.text ?? "No response text"}");
        }
    }

    private void UpdateUserDataLists(FollowingUserData userData, bool isFollowed)
    {
        // Update in followersData if present
        var followerEntry = followersData.FirstOrDefault(u => u.userId == userData.userId);
        if (followerEntry != null)
        {
            followerEntry.followStatus = isFollowed ? "Following" : "Follow";
        }

        // Update in followingData if present
        var followingEntry = followingData.FirstOrDefault(u => u.userId == userData.userId);
        if (followingEntry != null)
        {
            followingEntry.followStatus = isFollowed ? "Following" : "Follow";
        }

        // If unfollowed, you might want to remove from followingData
        // and add to potential "suggested users" or handle as per your app logic
        if (!isFollowed && followingEntry != null)
        {
            Debug.Log($"[FollowAPI] User {userData.username} unfollowed - consider updating following list");
            // Optionally: followingData.Remove(followingEntry);
        }
    }

    // Keep all your existing styling methods...
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

// Updated FollowingUserData class to include UUID
[System.Serializable]
public class FollowingUserData
{
    public string userId;  
    public string username;
    public string name;
    public string followStatus;

    public FollowingUserData(string userId, string username, string name, string followStatus)
    {
        this.userId = userId;
        this.username = username;
        this.name = name;
        this.followStatus = followStatus;
    }
}*/