using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ChatAPIService : MonoBehaviour
{
    private string TokenEndpointURL = $"{baseScript.baseURL}/api/v1/chat/stream-token";

    [System.Serializable]
    private class TokenResponseWrapper
    {
        public TokenData data;
    }

    [System.Serializable]
    private class TokenData
    {
        public string streamToken;
        public string userId;
        public string userName;
    }

    [System.Serializable]
    public class FollowingApiResponse
    {
        public bool success;
        public string message;
        public FollowingUserData[] data;
        public PaginationInfo pagination;
    }

    [System.Serializable]
    public class FollowingUserData
    {
        public string userId;
        public string firstName;
        public string lastName;
        public string userName;
        public string avatar;
    }

    [System.Serializable]
    public class PaginationInfo
    {
        public int total;
        public int page;
        public int limit;
        public int totalPages;
    }

    public async Task<(string token, string userId)> GetStreamTokenFromServerAsync()
    {
        var fullUrl = TokenEndpointURL;
        Debug.Log($"Attempting to fetch token from: {fullUrl}");

        using (var request = UnityWebRequest.Get(TokenEndpointURL))
        {
            request.SetRequestHeader("Authorization", AuthTokenManager.GetToken());

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching Stream token: {request.error}");
                return (null, null);
            }

            var jsonResponse = request.downloadHandler.text;
            Debug.Log($"Server response: {jsonResponse}");

            var tokenData = JsonUtility.FromJson<TokenResponseWrapper>(jsonResponse).data;

            return (tokenData.streamToken, tokenData.userId);
        }
    }

    public async Task<List<FollowingUserData>> GetFollowingListAsync(string username, int page = 1, int limit = 20)
    {
        var url = $"{baseScript.baseURL}/api/v1/profile/{username}/following?page={page}&limit={limit}";
        Debug.Log($"Fetching following list from: {url}");

        using (var request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", AuthTokenManager.GetToken());
            request.SetRequestHeader("accept", "application/json, text/plain, */*");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching following list: {request.error}");
                return new List<FollowingUserData>();
            }

            var jsonResponse = request.downloadHandler.text;
            Debug.Log($"Following list response: {jsonResponse}");

            var response = JsonUtility.FromJson<FollowingApiResponse>(jsonResponse);

            if (response.success && response.data != null)
            {
                return new List<FollowingUserData>(response.data);
            }

            return new List<FollowingUserData>();
        }
    }
}