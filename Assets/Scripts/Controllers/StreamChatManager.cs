using UnityEngine;
using System.Threading.Tasks;
using StreamChat.Core;
using System.Collections.Generic;
using System.Linq;
using StreamChat.Core.Exceptions;
using StreamChat.Core.StatefulModels;
using StreamChat.Core.QueryBuilders.Filters;
using StreamChat.Core.QueryBuilders.Filters.Users;
using StreamChat.Core.QueryBuilders.Sort;
using System;

public class StreamChatManager : MonoBehaviour
{ 
    public static StreamChatManager Instance { get; private set; }
    
    private string apiKey = "9xw8jsvunxha";

    private IStreamChatClient _client;
    public bool IsConnected { get; private set; } = false;

    private ChatAPIService _apiService;
    private List<IStreamChannel> _channels = new List<IStreamChannel>();
    public IStreamChatClient Client => _client;
    
    public event Action OnConnectionEstablished;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        _apiService = GetComponent<ChatAPIService>();
        if (_apiService == null)
        {
            _apiService = gameObject.AddComponent<ChatAPIService>();
        }

        await ConnectWithTokenAsync();
        
        if (IsConnected)
        {
            await LoadChannelsAsync();
            OnConnectionEstablished?.Invoke();
        }
    }

    public async Task ConnectWithTokenAsync()
    {
        if (IsConnected)
        {
            return;
        }
        var (userToken, userId) = await _apiService.GetStreamTokenFromServerAsync();

        if (string.IsNullOrEmpty(userToken) || string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Could not retrieve a valid token or user ID from the server. Aborting connection.");
            return;
        }

        try
        {
            _client = StreamChatClient.CreateDefaultClient();
            var user = await _client.ConnectUserAsync(apiKey, userId, userToken);
            
            IsConnected = true;
            Debug.Log($"Connection successful! UserId: {user.User.Id}, Name: {user.User.Name}");
        }
        catch (StreamApiException e)
        {
            IsConnected = false;
            Debug.LogError($"Stream API Error: {e.Message} | Status Code: {e.StatusCode}");
        }
        catch (System.Exception e)
        {
            IsConnected = false;
            Debug.LogError($"Could not connect user. Exception: {e.Message}");
        }
    }
    
    private async Task LoadChannelsAsync()
    {
        _channels = await GetUserChannelsAsync();
        await DisplayUserChannels();
    }
    
    public List<IStreamChannel> GetAllChannels()
    {
        return _channels;
    }
    
    public async Task RefreshChannelsAsync()
    {
        await LoadChannelsAsync();
    }
    
    public async Task<List<IStreamChannel>> GetUserChannelsAsync()
    {
        if (!IsConnected || _client == null)
        {
            return new List<IStreamChannel>();
        }

        try
        {
            var filters = new Dictionary<string, object>
            {
                { "members", new Dictionary<string, object> { { "$in", new[] { _client.LocalUserData.User.Id } } } }
            };

            var sort = ChannelSort.OrderByDescending(ChannelSortFieldName.LastMessageAt);

            var channels = await _client.QueryChannelsAsync(filters, sort, limit: 30, offset: 0);
            
            Debug.Log($"Found {channels.Count()} channels for user");
            
            return channels.ToList();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error fetching channels: {e.Message}");
            return new List<IStreamChannel>();
        }
    }

    private async Task DisplayUserChannels()
    {
        var channels = _channels;
        
        if (channels.Count == 0)
        {
            Debug.Log("No channels found for this user.");
            return;
        }
        
        // Debug.Log(channels);
        
        foreach (var channel in channels)
        {
            // Debug.Log($"Channel ID: {channel}");
        //     Debug.Log($"  Type: {channel.Type}");
        //     Debug.Log($"  Members: {channel.Members.Count}");
        //     Debug.Log($"  Created At: {channel.CreatedAt}");
        //     Debug.Log($"  Last Message At: {channel.LastMessageAt}");
        //     
        //     if (channel.Messages.Count > 0)
        //     {
        //         var lastMsg = channel.Messages.Last();
        //         Debug.Log($"  Last Message: '{lastMsg.Text}' by {lastMsg.User.Name}");
        //     }
        //     else
        //     {
        //         Debug.Log($"  Last Message: No messages yet");
        //     }
        //     
        //     Debug.Log($"  --- Members ---");
        //     foreach (var member in channel.Members)
        //     {
        //         Debug.Log($"    - {member.User.Name}");
        //     }
        }
    }

    public async Task<IStreamChannel> CreateOrGetDirectMessageChannelAsync(string otherUserId)
    {
        if (!IsConnected || _client == null)
        {
            Debug.LogError("StreamChatManager not connected");
            return null;
        }

        try
        {
            Debug.Log($"Creating or getting DM channel with user: {otherUserId}");

            // Query for the other user
            var filters = new IFieldFilterRule[]
            {
                UserFilter.Id.EqualsTo(otherUserId)
            };

            var users = await _client.QueryUsersAsync(filters);
            var otherUser = users.FirstOrDefault();

            if (otherUser == null)
            {
                Debug.LogError($"User {otherUserId} not found");
                return null;
            }

            var myUser = _client.LocalUserData.User;

            // Let StreamChat auto-generate the channel ID
            var channel = await _client.GetOrCreateChannelWithMembersAsync(
                ChannelType.Messaging,
                new[] { myUser, otherUser }
            );

            // Add to our local channels list if not already present
            if (!_channels.Any(c => c.Id == channel.Id))
            {
                _channels.Add(channel);
            }

            Debug.Log($"Channel created/retrieved: {channel.Id}");
            return channel;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating/getting channel: {e.Message}");
            return null;
        }
    }

    private async void OnDestroy()
    {
        if (_client != null && _client.IsConnected)
        {
            await _client.DisconnectUserAsync();
        }
    }
}