using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using StreamChat.Core;
using StreamChat.Core.StatefulModels;

public class ChatHomeScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset chatListItemTemplate;
    [SerializeField] private GameObject messageScreenObject;

    [SerializeField] private GameObject newChatScreenObject;

    private ScrollView chatListScrollView;
    private Button refreshButton;
    private Button newChatButton;
    private Button backButton;
    private Button refreshFab;
    private VisualElement refreshFabIcon;

    private List<IStreamChannel> channels = new List<IStreamChannel>();
    private List<VisualElement> skeletonItems = new List<VisualElement>();
    private bool isAnimating = false;
    private bool isRefreshing = false;

    private List<IStreamChannel> cachedChannels;
    private DateTime channelsCacheTime;
    private const int CHANNELS_CACHE_DURATION_MINUTES = 2;

    private async void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        chatListScrollView = root.Q<ScrollView>("chat-list");
        newChatButton = root.Q<Button>("new-chat");
        backButton = root.Q<Button>("back-button");
        refreshFab = root.Q<Button>("refresh-fab");
        refreshFabIcon = root.Q<VisualElement>("refresh-fab-icon");

        newChatButton.clicked += OnNewChatClicked;
        refreshFab.clicked += OnRefreshFabClicked;

        if (IsChannelsCacheValid())
        {
            channels = cachedChannels;
            PopulateChatList();
        }
        else
        {
            ShowLoadingSkeleton();
        }

        await WaitForStreamChatManager();

        if (StreamChatManager.Instance != null)
        {
            StreamChatManager.Instance.OnConnectionEstablished += LoadChannels;
            if (StreamChatManager.Instance.IsConnected)
            {
                LoadChannels();
            }
        }
        NavigationManager.ToggleNavigationBar(false);
        backButton?.RegisterCallback<ClickEvent>(evt => OnBackButtonClicked());
    }

    private void OnDisable()
    {
        if (newChatButton != null)
            newChatButton.clicked -= OnNewChatClicked;

        if (refreshFab != null)
            refreshFab.clicked -= OnRefreshFabClicked;

        if (StreamChatManager.Instance != null)
        {
            StreamChatManager.Instance.OnConnectionEstablished -= LoadChannels;
        }
    }

    private async Task WaitForStreamChatManager()
    {
        int attempts = 0;
        while (StreamChatManager.Instance == null && attempts < 50)
        {
            await Task.Delay(100);
            attempts++;
        }

        if (StreamChatManager.Instance == null)
        {
            Debug.LogError("StreamChatManager not found after waiting.");
        }
    }

    // Loads channels with cache support - silently updates if cache exists
    private void LoadChannels()
    {
        if (StreamChatManager.Instance == null || !StreamChatManager.Instance.IsConnected)
            return;

        var freshChannels = StreamChatManager.Instance.GetAllChannels();

        if (!IsChannelsCacheValid())
        {
            HideLoadingSkeleton();
        }

        channels = freshChannels;
        cachedChannels = new List<IStreamChannel>(freshChannels);
        channelsCacheTime = DateTime.Now;

        PopulateChatList();
    }

    public void InvalidateChannelsCache()
    {
        cachedChannels = null;
        channelsCacheTime = DateTime.MinValue;
    }

    private bool IsChannelsCacheValid()
    {
        if (cachedChannels == null) return false;

        var cacheAge = DateTime.Now - channelsCacheTime;
        return cacheAge.TotalMinutes < CHANNELS_CACHE_DURATION_MINUTES;
    }

    private void PopulateChatList()
    {
        chatListScrollView.Clear();

        if (channels.Count == 0)
        {
            ShowEmptyState();
            return;
        }

        foreach (var channel in channels)
            CreateChatItem(channel);
    }

    private void ShowLoadingSkeleton()
    {
        chatListScrollView.Clear();
        skeletonItems.Clear();

        // Create 4 skeleton items
        for (int i = 0; i < 4; i++)
        {
            var skeletonItem = CreateSkeletonItem();
            skeletonItems.Add(skeletonItem);
            chatListScrollView.Add(skeletonItem);
        }

        // Start shimmer animation
        StartShimmerAnimation();
    }

    private VisualElement CreateSkeletonItem()
    {
        var skeletonContainer = new VisualElement();
        skeletonContainer.AddToClassList("skeleton-chat-item");

        var iconSkeleton = new VisualElement();
        iconSkeleton.AddToClassList("skeleton-icon");

        var textContainer = new VisualElement();
        textContainer.AddToClassList("skeleton-text-container");

        var nameSkeleton = new VisualElement();
        nameSkeleton.AddToClassList("skeleton-name");

        var messageSkeleton = new VisualElement();
        messageSkeleton.AddToClassList("skeleton-message");

        textContainer.Add(nameSkeleton);
        textContainer.Add(messageSkeleton);

        skeletonContainer.Add(iconSkeleton);
        skeletonContainer.Add(textContainer);

        return skeletonContainer;
    }

    private async void StartShimmerAnimation()
    {
        isAnimating = true;
        float opacity = 0.3f;
        bool increasing = true;

        while (isAnimating && skeletonItems.Count > 0)
        {
            if (increasing)
            {
                opacity += 0.05f;
                if (opacity >= 1.0f)
                {
                    opacity = 1.0f;
                    increasing = false;
                }
            }
            else
            {
                opacity -= 0.05f;
                if (opacity <= 0.3f)
                {
                    opacity = 0.3f;
                    increasing = true;
                }
            }

            foreach (var skeleton in skeletonItems)
            {
                if (skeleton != null)
                {
                    skeleton.style.opacity = opacity;
                }
            }

            await Task.Delay(50); // Smooth animation
        }
    }

    private void HideLoadingSkeleton()
    {
        isAnimating = false;
        skeletonItems.Clear();
    }

    private void ShowEmptyState()
    {
        var emptyContainer = new VisualElement();
        emptyContainer.AddToClassList("empty-state-container");

        var emptyLabel = new Label("No messages yet");
        emptyLabel.AddToClassList("empty-state-text");

        var emptySubtext = new Label("Start a new conversation to get started");
        emptySubtext.AddToClassList("empty-state-subtext");

        emptyContainer.Add(emptyLabel);
        emptyContainer.Add(emptySubtext);
        chatListScrollView.Add(emptyContainer);
    }

    private void CreateChatItem(IStreamChannel channel)
    {
        var chatItem = chatListItemTemplate.CloneTree();

        var iconLabel = chatItem.Q<Label>("icon-label");
        var chatName = chatItem.Q<Label>("chat-name");
        var lastMessage = chatItem.Q<Label>("last-message");
        var timestamp = chatItem.Q<Label>("timestamp");
        var openChatButton = chatItem.Q<Button>("open-chat-arrow");

        string displayName = GetChannelDisplayName(channel);
        string iconLetter = displayName.Length > 0 ? displayName[0].ToString().ToUpper() : "?";

        string lastMsg = "No messages yet";
        string time = "";

        if (channel.Messages.Any())
        {
            var latestMessage = channel.Messages.Last();
            lastMsg = latestMessage.Text;
            time = FormatTimestamp(latestMessage.CreatedAt);
        }

        iconLabel.text = iconLetter;
        chatName.text = displayName;
        lastMessage.text = lastMsg;
        timestamp.text = time;

        openChatButton.clicked += () => OnChatItemClicked(channel);
        chatItem.Q<VisualElement>("chat-ui").RegisterCallback<ClickEvent>(evt => OnChatItemClicked(channel));

        chatListScrollView.Add(chatItem);
    }

    private string GetChannelDisplayName(IStreamChannel channel)
    {
        var manager = StreamChatManager.Instance;
        if (manager == null || manager.Client == null || manager.Client.LocalUserData == null)
            return channel.Name ?? channel.Id;

        var myUserId = manager.Client.LocalUserData.User.Id;
        var otherMember = channel.Members.FirstOrDefault(m => m.User.Id != myUserId);

        if (otherMember != null && !string.IsNullOrEmpty(otherMember.User.Name))
            return otherMember.User.Name;

        if (!string.IsNullOrEmpty(channel.Name))
            return channel.Name;

        return channel.Id;
    }

    // ✅ Opens the New Chat bottom sheet (modal)
    private void OnNewChatClicked()
    {
        newChatScreenObject.SetActive(true);
    }

    // ✅ Called by NewChatScreen to hide itself
    public void HideNewChatSheet()
    {
        newChatScreenObject.SetActive(false);
    }

    // Handles FAB click to refresh channels without showing skeleton
    private void OnRefreshFabClicked()
    {
        if (!isRefreshing)
        {
            _ = RefreshChannelsAsync();
        }
    }

    // Refreshes channels while keeping existing list visible, spins FAB icon for feedback
    private async Task RefreshChannelsAsync()
    {
        if (isRefreshing || StreamChatManager.Instance == null)
            return;

        isRefreshing = true;
        StartFabSpinAnimation();

        InvalidateChannelsCache();
        await StreamChatManager.Instance.RefreshChannelsAsync();
        LoadChannels();

        StopFabSpinAnimation();
        isRefreshing = false;
    }

    // Animates FAB icon rotation during refresh
    private async void StartFabSpinAnimation()
    {
        if (refreshFabIcon == null) return;

        float rotation = 0f;
        while (isRefreshing)
        {
            rotation += 10f;
            if (rotation >= 360f) rotation = 0f;

            refreshFabIcon.style.rotate = new Rotate(rotation);
            await Task.Delay(16);
        }
    }

    // Stops FAB icon rotation animation
    private void StopFabSpinAnimation()
    {
        if (refreshFabIcon == null) return;

        refreshFabIcon.style.rotate = new Rotate(0f);
    }

    private void OnChatItemClicked(IStreamChannel channel)
    {
        if (messageScreenObject != null)
        {
            var messageController = messageScreenObject.GetComponent<ChatMessageScreenController>();
            if (messageController != null)
            {
                gameObject.SetActive(false);
                messageScreenObject.SetActive(true);
                messageController.LoadChannel(channel);
            }
        }
    }

    private void OnBackButtonClicked()
    {
        UIManager.Instance.TransitionScreens(UIScreenType.Messages, UIScreenType.Home);
    }

    private string FormatTimestamp(DateTimeOffset? timestamp)
    {
        if (!timestamp.HasValue) return "";
        var diff = DateTimeOffset.Now - timestamp.Value;
        if (diff.TotalMinutes < 1) return "now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d";
        return timestamp.Value.ToString("dd/MM");
    }
}
