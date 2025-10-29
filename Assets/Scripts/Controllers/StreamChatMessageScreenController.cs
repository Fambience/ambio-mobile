using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using StreamChat.Core;
using StreamChat.Core.StatefulModels;
using System.Globalization;

public class ChatMessageScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset messageBubbleReceivedTemplate;
    [SerializeField] private VisualTreeAsset messageBubbleSentTemplate;
    [SerializeField] private VisualTreeAsset dateSeparatorTemplate;
    [SerializeField] private GameObject homeScreenObject;

    private ScrollView messageList;
    private Button sendButton;
    private Button backButton;
    private TextField messageInput;
    private Label chatNameLabel;

    private IStreamChannel _currentChannel;
    private List<IStreamMessage> _displayedMessages = new List<IStreamMessage>();
    private const int MessagesPerPage = 10;
    private bool _isLoadingMore = false;
    private bool _hasMoreMessages = true;
    private float _lastScrollPosition = 0;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        messageList = root.Q<ScrollView>("message-list");
        sendButton = root.Q<Button>("send-button");
        backButton = root.Q<Button>("back-button");
        messageInput = root.Q<TextField>("message-input");
        chatNameLabel = root.Q<Label>("chat-name");

        if (messageList == null || sendButton == null || messageInput == null)
        {
            Debug.LogError("ChatMessageScreenController: Required UI elements not found!");
            return;
        }

        sendButton.clicked += OnSendButtonClicked;
        backButton.clicked += OnBackButtonClicked;
        
        messageList.RegisterCallback<WheelEvent>(OnScroll);
    }

    private void OnDisable()
    {
        if (sendButton != null)
            sendButton.clicked -= OnSendButtonClicked;
        if (backButton != null)
            backButton.clicked -= OnBackButtonClicked;
            
        if (messageList != null)
            messageList.UnregisterCallback<WheelEvent>(OnScroll);
            
        if (_currentChannel != null)
        {
            _currentChannel.MessageReceived -= OnMessageReceived;
        }
    }

    public async void LoadChannel(IStreamChannel channel)
    {
        if (channel == null)
        {
            Debug.LogError("Cannot load null channel");
            return;
        }

        if (_currentChannel != null)
        {
            _currentChannel.MessageReceived -= OnMessageReceived;
        }

        _currentChannel = channel;
        _displayedMessages.Clear();
        _hasMoreMessages = true;

        string displayName = GetChannelDisplayName(channel);
        chatNameLabel.text = displayName;

        _currentChannel.MessageReceived += OnMessageReceived;

        await LoadInitialMessages();
    }

    private void OnMessageReceived(IStreamChannel channel, IStreamMessage message)
    {
        if (channel.Id == _currentChannel?.Id)
        {
            _displayedMessages.Add(message);
            RefreshMessageList(scrollToBottom: true);
        }
    }

    private async Task LoadInitialMessages()
    {
        messageList.Clear();
        
        var messages = _currentChannel.Messages.OrderBy(m => m.CreatedAt).ToList();
        
        int startIndex = Math.Max(0, messages.Count - MessagesPerPage);
        _displayedMessages = messages.Skip(startIndex).ToList();
        
        _hasMoreMessages = startIndex > 0;

        RefreshMessageList(scrollToBottom: true);
    }

    private async Task LoadMoreMessages()
    {
        if (_isLoadingMore || !_hasMoreMessages)
            return;

        _isLoadingMore = true;

        try
        {
            var allMessages = _currentChannel.Messages.OrderBy(m => m.CreatedAt).ToList();
            
            if (_displayedMessages.Count >= allMessages.Count)
            {
                _hasMoreMessages = false;
                _isLoadingMore = false;
                return;
            }

            var oldestDisplayedMessage = _displayedMessages.FirstOrDefault();
            if (oldestDisplayedMessage == null)
            {
                _isLoadingMore = false;
                return;
            }

            int oldestIndex = allMessages.FindIndex(m => m.Id == oldestDisplayedMessage.Id);
            
            if (oldestIndex <= 0)
            {
                _hasMoreMessages = false;
                _isLoadingMore = false;
                return;
            }

            int startIndex = Math.Max(0, oldestIndex - MessagesPerPage);
            int count = oldestIndex - startIndex;
            
            var olderMessages = allMessages.Skip(startIndex).Take(count).ToList();
            
            _displayedMessages.InsertRange(0, olderMessages);
            
            _hasMoreMessages = startIndex > 0;

            RefreshMessageList(scrollToBottom: false);
            
            await Task.Delay(100);
            
            messageList.scrollOffset = new Vector2(0, 200);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading more messages: {e.Message}");
        }
        finally
        {
            _isLoadingMore = false;
        }
    }

    private void OnScroll(WheelEvent evt)
    {
        if (messageList.scrollOffset.y <= 50 && !_isLoadingMore && _hasMoreMessages)
        {
            _ = LoadMoreMessages();
        }
    }

    private void RefreshMessageList(bool scrollToBottom = false)
    {
        messageList.Clear();

        var sortedMessages = _displayedMessages.OrderBy(m => m.CreatedAt).ToList();
        
        DateTime? lastDate = null;

        foreach (var message in sortedMessages)
        {
            DateTime messageDate = message.CreatedAt.Date;

            if (lastDate == null || lastDate.Value.Date != messageDate)
            {
                AddDateSeparator(messageDate);
                lastDate = messageDate;
            }

            AddMessageBubble(message);
        }

        if (scrollToBottom)
        {
            StartCoroutine(ScrollToBottomNextFrame());
        }
    }

    private void AddDateSeparator(DateTime date)
    {
        if (dateSeparatorTemplate == null)
            return;

        var separator = dateSeparatorTemplate.CloneTree();
        var dateLabel = separator.Q<Label>("date-label");

        if (dateLabel != null)
        {
            dateLabel.text = FormatDateForSeparator(date);
        }

        messageList.Add(separator);
    }

    private string FormatDateForSeparator(DateTime date)
    {
        DateTime today = DateTime.Today;
        DateTime yesterday = today.AddDays(-1);

        if (date.Date == today)
        {
            return "TODAY";
        }
        else if (date.Date == yesterday)
        {
            return "YESTERDAY";
        }
        else
        {
            return date.ToString("dddd d MMMM", CultureInfo.InvariantCulture).ToUpper();
        }
    }

    private void AddMessageBubble(IStreamMessage message)
    {
        var myUserId = StreamChatManager.Instance?.Client?.LocalUserData?.User?.Id;
        bool isSent = message.User.Id == myUserId;

        VisualTreeAsset template = isSent ? messageBubbleSentTemplate : messageBubbleReceivedTemplate;

        if (template == null)
            return;

        var bubble = template.CloneTree();

        var messageText = bubble.Q<Label>("message-text");
        if (messageText != null)
        {
            messageText.text = message.Text;
        }

        var messageTime = bubble.Q<Label>("message-time");
        if (messageTime != null)
        {
            messageTime.text = message.CreatedAt.LocalDateTime.ToString("HH:mm");
        }

        if (isSent)
        {
            var checkmark = bubble.Q<Label>("checkmark");
            if (checkmark != null)
            {
                checkmark.text = "✓✓";
            }
        }

        messageList.Add(bubble);
    }

    private async void OnSendButtonClicked()
    {
        string messageText = messageInput?.value;

        if (string.IsNullOrWhiteSpace(messageText) || _currentChannel == null)
            return;

        try
        {
            await _currentChannel.SendNewMessageAsync(messageText);
            
            messageInput.value = "";
            
            Debug.Log($"Message sent: {messageText}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending message: {e.Message}");
        }
    }

    private void OnBackButtonClicked()
    {
        if (homeScreenObject != null)
        {
            gameObject.SetActive(false);
            homeScreenObject.SetActive(true);
        }
    }

    private System.Collections.IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        yield return null;
        if (messageList != null)
        {
            messageList.scrollOffset = new Vector2(0, messageList.contentContainer.layout.height);
        }
    }

    private string GetChannelDisplayName(IStreamChannel channel)
    {
        var manager = StreamChatManager.Instance;
        if (manager == null || manager.Client == null || manager.Client.LocalUserData == null)
        {
            return channel.Name ?? channel.Id;
        }

        var myUserId = manager.Client.LocalUserData.User.Id;
        var otherMember = channel.Members.FirstOrDefault(m => m.User.Id != myUserId);
        if (otherMember != null && !string.IsNullOrEmpty(otherMember.User.Name))
            return otherMember.User.Name;
        if (!string.IsNullOrEmpty(channel.Name))
            return channel.Name;
        if (channel.Members.Count > 1)
        {
            var names = channel.Members
                .Where(m => m.User.Id != myUserId)
                .Select(m => m.User.Name ?? m.User.Id)
                .ToList();
            return string.Join(", ", names);
        }
        return channel.Id;
    }
}