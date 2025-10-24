using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamChat.Core.StatefulModels;

public class NewChatScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private GameObject messageScreenObject;

    private VisualElement root;
    private VisualElement newChatContainer;
    private VisualElement newChatPanel; // new-chat-ui
    private ChatHomeScreenController homeController;
    private ScrollView scrollView;
    private ChatAPIService chatAPIService;
    private VisualElement loadingContainer;
    private Label loadingDotsLabel;
    private int dotCount = 0;
    private bool isLoading = false;

    private List<ChatAPIService.FollowingUserData> unMessagedUsers = new List<ChatAPIService.FollowingUserData>();

    private async void OnEnable()
    {
        root = uiDocument.rootVisualElement;

        // Get references
        newChatContainer = root.Q<VisualElement>("new-chat-container");
        newChatPanel = root.Q<VisualElement>("new-chat-ui");

        homeController = FindObjectOfType<ChatHomeScreenController>();
        chatAPIService = FindObjectOfType<ChatAPIService>();

        if (chatAPIService == null)
        {
            Debug.LogError("ChatAPIService not found!");
        }

        // Setup empty scroll view first
        SetupScrollView();

        // Fetch and filter following list
        await LoadUnmessagedUsers();

        newChatContainer.RegisterCallback<ClickEvent>(evt =>
        {
            // Check if the click target or any of its parents is inside newChatPanel
            VisualElement target = evt.target as VisualElement;

            // Walk up the hierarchy to see if we're inside newChatPanel
            VisualElement current = target;
            bool isInsidePanel = false;

            while (current != null)
            {
                if (current == newChatPanel)
                {
                    isInsidePanel = true;
                    break;
                }
                current = current.parent;
            }

            // Only close if the click is outside the panel
            if (!isInsidePanel)
            {
                homeController.HideNewChatSheet();
            }
        }, TrickleDown.TrickleDown); // Use TrickleDown to catch it before children
    }

    private void SetupScrollView()
    {
        // Remove existing static chat item
        var existingChatUi = root.Q<VisualElement>("chat-ui");
        existingChatUi?.RemoveFromHierarchy();

        scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.style.flexGrow = 1;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;

        newChatPanel.Add(scrollView);

        // Setup loading container
        SetupLoadingUI();
    }

    private void SetupLoadingUI()
    {
        loadingContainer = new VisualElement();
        loadingContainer.AddToClassList("loading-container");

        var loadingText = new Label("Searching new users");
        loadingText.AddToClassList("loading-text");

        loadingDotsLabel = new Label("...");
        loadingDotsLabel.AddToClassList("loading-dots");

        loadingContainer.Add(loadingText);
        loadingContainer.Add(loadingDotsLabel);

        scrollView.Add(loadingContainer);
        loadingContainer.style.display = DisplayStyle.None; // Hidden by default
    }

    private async Task LoadUnmessagedUsers()
    {
        // Show loading state
        ShowLoading();

        if (chatAPIService == null)
        {
            Debug.LogError("ChatAPIService is null, cannot load following list");
            HideLoading();
            return;
        }

        // Get current user's username
        var username = Services.UserData.userName;
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogError("Username is empty");
            HideLoading();
            return;
        }

        // Fetch following list from API
        var followingList = await chatAPIService.GetFollowingListAsync(username);
        Debug.Log($"Fetched {followingList.Count} following users");

        // Get existing channel user IDs
        var channelUserIds = new HashSet<string>();
        if (StreamChatManager.Instance != null && StreamChatManager.Instance.IsConnected)
        {
            var channels = StreamChatManager.Instance.GetAllChannels();
            foreach (var channel in channels)
            {
                var myUserId = StreamChatManager.Instance.Client.LocalUserData.User.Id;
                foreach (var member in channel.Members)
                {
                    if (member.User.Id != myUserId)
                    {
                        channelUserIds.Add(member.User.Id);
                    }
                }
            }
        }

        Debug.Log($"Found {channelUserIds.Count} users already in channels");

        // Filter out users who are already in channels
        unMessagedUsers = followingList.Where(user => !channelUserIds.Contains(user.userId)).ToList();
        Debug.Log($"Showing {unMessagedUsers.Count} unmessaged users");

        // Hide loading and populate the scroll view
        HideLoading();
        PopulateUserList();
    }

    private void ShowLoading()
    {
        isLoading = true;
        if (loadingContainer != null)
        {
            loadingContainer.style.display = DisplayStyle.Flex;
            StartLoadingAnimation();
        }
    }

    private void HideLoading()
    {
        isLoading = false;
        if (loadingContainer != null)
        {
            loadingContainer.style.display = DisplayStyle.None;
        }
    }

    private async void StartLoadingAnimation()
    {
        while (isLoading)
        {
            dotCount = (dotCount % 3) + 1;
            if (loadingDotsLabel != null)
            {
                loadingDotsLabel.text = new string('.', dotCount);
            }
            await Task.Delay(500); // Update every 500ms
        }
        // Reset dots when animation stops
        if (loadingDotsLabel != null)
        {
            loadingDotsLabel.text = "...";
        }
    }

    private void PopulateUserList()
    {
        var chatUiContainer = new VisualElement { name = "chat-list-container" };
        scrollView.Add(chatUiContainer);

        if (unMessagedUsers.Count == 0)
        {
            var emptyStateContainer = new VisualElement();
            emptyStateContainer.AddToClassList("empty-state-container");

            var emptyLabel = new Label("No users to message");
            emptyLabel.AddToClassList("empty-state-text");

            var findUserButton = new Button(() => OnFindNewUserClicked());
            findUserButton.text = "Find New User";
            findUserButton.AddToClassList("find-user-button");

            emptyStateContainer.Add(emptyLabel);
            emptyStateContainer.Add(findUserButton);
            chatUiContainer.Add(emptyStateContainer);
            return;
        }

        foreach (var user in unMessagedUsers)
        {
            chatUiContainer.Add(CreateUserChatItem(user));
        }
    }

    private void OnFindNewUserClicked()
    {
        homeController.HideNewChatSheet();
        UIManager.Instance.TransitionScreens(UIScreenType.Messages, UIScreenType.Explore);
    }

    private VisualElement CreateUserChatItem(ChatAPIService.FollowingUserData user)
    {
        var chatUi = new VisualElement();
        chatUi.AddToClassList("chat-ui");

        var iconContainer = new VisualElement();
        iconContainer.AddToClassList("icon-container");

        // Get display name for icon
        string displayName = !string.IsNullOrEmpty(user.firstName) ? user.firstName :
                           !string.IsNullOrEmpty(user.userName) ? user.userName : "?";
        var iconLabel = new Label(displayName[0].ToString().ToUpper());
        iconLabel.AddToClassList("icon-label");
        iconContainer.Add(iconLabel);

        var textContainer = new VisualElement();
        textContainer.AddToClassList("text-container");

        var header = new VisualElement();
        header.AddToClassList("text-container-header");

        // Display full name or username
        string fullName = "";
        if (!string.IsNullOrEmpty(user.firstName) || !string.IsNullOrEmpty(user.lastName))
        {
            fullName = $"{user.firstName} {user.lastName}".Trim();
        }
        else if (!string.IsNullOrEmpty(user.userName))
        {
            fullName = user.userName;
        }
        else
        {
            fullName = "Unknown User";
        }

        var chatName = new Label(fullName);
        chatName.AddToClassList("chat-name");
        header.Add(chatName);

        var lastMessage = new Label("Start a new conversation");
        lastMessage.AddToClassList("last-message");

        textContainer.Add(header);
        textContainer.Add(lastMessage);

        chatUi.Add(iconContainer);
        chatUi.Add(textContainer);

        // Click creates new channel and opens chat
        chatUi.RegisterCallback<ClickEvent>(evt =>
        {
            OnUserClicked(user);
        });

        return chatUi;
    }

    private async void OnUserClicked(ChatAPIService.FollowingUserData user)
    {
        Debug.Log($"User clicked: {user.userName}");

        if (StreamChatManager.Instance == null || !StreamChatManager.Instance.IsConnected)
        {
            Debug.LogError("StreamChatManager not connected");
            return;
        }

        // Create or get existing channel with this user
        var channel = await StreamChatManager.Instance.CreateOrGetDirectMessageChannelAsync(user.userId);

        if (channel != null && messageScreenObject != null)
        {
            var messageController = messageScreenObject.GetComponent<ChatMessageScreenController>();
            if (messageController != null)
            {
                // Close new chat modal
                homeController.HideNewChatSheet();

                // Hide chat home screen
                if (homeController != null)
                {
                    homeController.gameObject.SetActive(false);
                }

                // Open message screen with the channel
                messageScreenObject.SetActive(true);
                messageController.LoadChannel(channel);
            }
        }
    }
}

[System.Serializable]
public class ChatData
{
    public string name;
    public string lastMessage;

    public ChatData(string name, string lastMessage)
    {
        this.name = name;
        this.lastMessage = lastMessage;
    }
}
