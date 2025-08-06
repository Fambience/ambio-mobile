using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using MiniJSON;
using Services;
using System.IO;

[Serializable]
public class UserProfileDataFetcher : MonoBehaviour
{
    public static UserProfileDataFetcher Instance { get; private set; }

    private Dictionary<string, ProfileCache> userProfileCache = new();
    private Dictionary<string, List<ProfileUserLite>> userFollowers = new();
    private Dictionary<string, List<ProfileUserLite>> userFollowing = new();

    // Reference the static baseScript class
    private string baseURL => baseScript.baseURL;

    void Awake()
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

    public ProfileCache GetCachedProfile(string userId)
    {
        return userProfileCache.ContainsKey(userId) ? userProfileCache[userId] : null;
    }

    public List<ProfileUserLite> GetCachedFollowers(string userId)
    {
        return userFollowers.ContainsKey(userId) ? userFollowers[userId] : new List<ProfileUserLite>();
    }

    public List<ProfileUserLite> GetCachedFollowing(string userId)
    {
        return userFollowing.ContainsKey(userId) ? userFollowing[userId] : new List<ProfileUserLite>();
    }

    public IEnumerator FetchUserProfile(string userId, Action<ProfileCache> onComplete)
    {
        // Validate inputs first
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[UserProfileDataFetcher] UserId is null or empty");
            onComplete?.Invoke(null);
            yield break;
        }

        string token = AuthTokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[UserProfileDataFetcher] Auth token is null or empty");
            onComplete?.Invoke(null);
            yield break;
        }

        if (string.IsNullOrEmpty(baseURL))
        {
            Debug.LogError("[UserProfileDataFetcher] BaseURL is not configured");
            onComplete?.Invoke(null);
            yield break;
        }

        string url = $"{baseURL}/api/v1/profile/me/{userId}";
        Debug.Log("[ProfileScreen] URL to fetch profile: " + url);
        Debug.Log("[ProfileScreen] Auth token present: " + !string.IsNullOrEmpty(token));

        UnityWebRequest req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", token);
        req.SetRequestHeader("Content-Type", "application/json");
        
        // Add timeout
        req.timeout = 30;

        yield return req.SendWebRequest();

        // More detailed error logging
        Debug.Log($"[UserProfileDataFetcher] Request completed with result: {req.result}");
        Debug.Log($"[UserProfileDataFetcher] Response code: {req.responseCode}");
        Debug.Log($"[UserProfileDataFetcher] Response headers: {req.GetResponseHeaders()?.Count ?? 0} headers");

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            Debug.Log($"[UserProfileDataFetcher] Response JSON length: {json?.Length ?? 0}");
            
            var profile = ParseAndBuildProfileCache(json);
            if (profile != null)
            {
                userProfileCache[userId] = profile;
                Debug.Log("[UserProfileDataFetcher] User profile stored: " + userId);

                // ✅ Show correct screen based on role
                if (profile.role == "USER")
                {
                    UIManager.Instance.OpenScreen(UIScreenType.UserProfileScreen);
                }
                else if (profile.role == "CREATOR")
                {
                    UIManager.Instance.OpenScreen(UIScreenType.CreatorProfileScreen);
                }

                onComplete?.Invoke(profile);
                yield break;
            }
        }

        // Enhanced error reporting
        string errorDetails = "";
        switch (req.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                errorDetails = "Connection Error - Check internet connection";
                break;
            case UnityWebRequest.Result.DataProcessingError:
                errorDetails = "Data Processing Error - Invalid response format";
                break;
            case UnityWebRequest.Result.ProtocolError:
                errorDetails = $"Protocol Error - HTTP {req.responseCode}";
                break;
            default:
                errorDetails = req.error ?? "Unknown error occurred";
                break;
        }

        Debug.LogError($"[UserProfileDataFetcher] Failed to fetch user profile: {errorDetails}");
        Debug.LogError($"[UserProfileDataFetcher] URL: {url}");
        Debug.LogError($"[UserProfileDataFetcher] Response: {req.downloadHandler?.text ?? "No response"}");
        
        onComplete?.Invoke(null);
    }

    public IEnumerator FetchFollowers(string userId, Action<List<ProfileUserLite>> onComplete)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(baseURL))
        {
            Debug.LogError("[UserProfileDataFetcher] Invalid userId or baseURL for followers fetch");
            onComplete?.Invoke(null);
            yield break;
        }

        string url = $"{baseURL}/api/v1/profile/{userId}/followers";
        string token = AuthTokenManager.GetToken();

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[UserProfileDataFetcher] Auth token missing for followers fetch");
            onComplete?.Invoke(null);
            yield break;
        }

        UnityWebRequest req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", token);
        req.timeout = 30;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            var followers = ParseFollowers(json);
            if (followers != null)
            {
                userFollowers[userId] = followers;
                onComplete?.Invoke(followers);
                yield break;
            }
        }
        
        Debug.LogError($"[UserProfileDataFetcher] Failed to fetch followers for: {userId} - {req.result} - {req.error ?? "No error details"}");
        Debug.LogError($"[UserProfileDataFetcher] Response: {req.downloadHandler?.text ?? "No response"}");
        onComplete?.Invoke(null);
    }

    public IEnumerator FetchFollowing(string userId, Action<List<ProfileUserLite>> onComplete)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(baseURL))
        {
            Debug.LogError("[UserProfileDataFetcher] Invalid userId or baseURL for following fetch");
            onComplete?.Invoke(null);
            yield break;
        }

        string url = $"{baseURL}/api/v1/profile/{userId}/following";
        string token = AuthTokenManager.GetToken();

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[UserProfileDataFetcher] Auth token missing for following fetch");
            onComplete?.Invoke(null);
            yield break;
        }

        UnityWebRequest req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", token);
        req.timeout = 30;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            var following = ParseFollowers(json); // Same structure
            if (following != null)
            {
                userFollowing[userId] = following;
                onComplete?.Invoke(following);
                yield break;
            }
        }
        
        Debug.LogError($"[UserProfileDataFetcher] Failed to fetch following for: {userId} - {req.result} - {req.error ?? "No error details"}");
        Debug.LogError($"[UserProfileDataFetcher] Response: {req.downloadHandler?.text ?? "No response"}");
        onComplete?.Invoke(null);
    }

    private ProfileCache ParseAndBuildProfileCache(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[UserProfileDataFetcher] JSON response is null or empty");
            return null;
        }

        try
        {
            Debug.Log($"[UserProfileDataFetcher] Raw JSON response: {json}");
            
            // First try JsonUtility parsing like in ProfileDataHandlers
            try
            {
                var apiResponse = JsonUtility.FromJson<ApiResponse>(json);
                if (apiResponse != null && apiResponse.success && apiResponse.data != null)
                {
                    Debug.Log("[UserProfileDataFetcher] JsonUtility parsing successful");
                    return ConvertToProfileCache(apiResponse.data);
                }
                else
                {
                    Debug.LogWarning("[UserProfileDataFetcher] JsonUtility parsing failed or success = false");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UserProfileDataFetcher] JsonUtility failed: {e.Message}");
            }

            // Fallback to MiniJSON parsing like in ProfileDataHandlers
            Debug.Log("[UserProfileDataFetcher] Trying MiniJSON as fallback...");
            var result = JSON.Deserialize(json) as Dictionary<string, object>;
            
            if (result == null)
            {
                Debug.LogError("[UserProfileDataFetcher] JSON.Deserialize returned null");
                return null;
            }
            
            Debug.Log($"[UserProfileDataFetcher] JSON parsed successfully. Keys: {string.Join(", ", result.Keys)}");
            
            // Check if success exists and is true
            if (!result.ContainsKey("success"))
            {
                Debug.LogError("[UserProfileDataFetcher] No 'success' key found in response");
                return null;
            }
            
            bool success = false;
            if (result["success"] is bool successBool)
            {
                success = successBool;
            }
            else if (bool.TryParse(result["success"].ToString(), out bool parsedSuccess))
            {
                success = parsedSuccess;
            }
            
            Debug.Log($"[UserProfileDataFetcher] Success value: {success}");
            
            if (!success)
            {
                Debug.LogError("[UserProfileDataFetcher] API returned success=false");
                return null;
            }
            
            // Check for data
            if (!result.ContainsKey("data"))
            {
                Debug.LogError("[UserProfileDataFetcher] No 'data' key found in response");
                return null;
            }
            
            if (result["data"] is Dictionary<string, object> dataDict)
            {
                Debug.Log($"[UserProfileDataFetcher] Data object found with {dataDict.Keys.Count} keys");
                return ParseProfileData(dataDict);
            }
            else
            {
                Debug.LogError($"[UserProfileDataFetcher] Data is not a Dictionary. Type: {result["data"]?.GetType().Name ?? "null"}");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[UserProfileDataFetcher] Error parsing profile JSON: " + e);
            Debug.LogError($"[UserProfileDataFetcher] Exception details: {e.Message}");
            Debug.LogError($"[UserProfileDataFetcher] Stack trace: {e.StackTrace}");
        }
        return null;
    }

    // Use the same conversion method as ProfileDataHandlers
    private ProfileCache ConvertToProfileCache(ProfileDataRaw rawData)
    {
        var cache = new ProfileCache();
        
        cache.userName = rawData.userName ?? "";
        cache.firstName = rawData.firstName ?? "";
        cache.lastName = rawData.lastName ?? "";
        cache.email = rawData.email ?? "";
        cache.role = rawData.role ?? "";
        cache.provider = rawData.provider ?? "";
        cache.bio = rawData.bio ?? "";
        cache.avatar = rawData.avatar ?? "";
        cache.followerCount = rawData.followerCount;
        cache.followingCount = rawData.followingCount;
        cache.postCount = rawData.postCount;

        // USER fields
        cache.homeLocation = rawData.homeLocation ?? "";
        cache.minBudget = rawData.minBudget;
        cache.maxBudget = rawData.maxBudget;
        cache.colorScheme = rawData.colorScheme != null ? new List<string>(rawData.colorScheme) : new List<string>();
        cache.homeSharingWith = rawData.homeSharingWith != null ? new List<string>(rawData.homeSharingWith) : new List<string>();
        
        // Convert designInspirations
        cache.designInspirations = new Dictionary<string, List<string>>();
        if (rawData.designInspirations != null)
        {
            if (rawData.designInspirations.MODERN_AND_MINIMAL != null)
                cache.designInspirations["MODERN_AND_MINIMAL"] = new List<string>(rawData.designInspirations.MODERN_AND_MINIMAL);
            if (rawData.designInspirations.CREATIVE_AND_CHARACTERFUL != null)
                cache.designInspirations["CREATIVE_AND_CHARACTERFUL"] = new List<string>(rawData.designInspirations.CREATIVE_AND_CHARACTERFUL);
        }

        cache.createdAt = rawData.createdAt ?? "";
        cache.updatedAt = rawData.updatedAt ?? "";
        cache.userId = rawData.userId ?? "";
        cache.userProfileId = rawData.userProfileId ?? "";
        cache.optLock = rawData.optLock;

        // CREATOR fields
        cache.website = rawData.website ?? "";
        cache.creatorType = rawData.creatorType ?? "";
        cache.region = rawData.region != null ? new List<string>(rawData.region) : new List<string>();
        cache.socials = rawData.socials != null ? new List<string>(rawData.socials) : new List<string>();
        cache.tagline = rawData.tagline ?? "";
        cache.yearsOfExperience = rawData.yearsOfExperience;

        Debug.Log($"[UserProfileDataFetcher] Profile converted successfully for user: {cache.userName}");
        return cache;
    }

    // Use the same parsing method as ProfileDataHandlers
    private ProfileCache ParseProfileData(Dictionary<string, object> dict)
    {
        Debug.Log("[UserProfileDataFetcher] Starting to parse profile data with MiniJSON...");
        var cache = new ProfileCache();
        
        try
        {
            cache.userName = GetString(dict, "userName");
            cache.firstName = GetString(dict, "firstName");
            cache.lastName = GetString(dict, "lastName");
            cache.email = GetString(dict, "email");
            cache.role = GetString(dict, "role");
            cache.provider = GetString(dict, "provider");
            cache.bio = GetString(dict, "bio");
            cache.avatar = GetString(dict, "avatar");
            cache.followerCount = GetInt(dict, "followerCount");
            cache.followingCount = GetInt(dict, "followingCount");
            cache.postCount = GetInt(dict, "postCount");

            // USER fields
            cache.homeLocation = GetString(dict, "homeLocation");
            cache.minBudget = GetInt(dict, "minBudget");
            cache.maxBudget = GetInt(dict, "maxBudget");
            cache.colorScheme = GetStringList(dict, "colorScheme");
            cache.homeSharingWith = GetStringList(dict, "homeSharingWith");
            cache.designInspirations = GetNestedDictionary(dict, "designInspirations");

            cache.createdAt = GetString(dict, "createdAt");
            cache.updatedAt = GetString(dict, "updatedAt");
            cache.userId = GetString(dict, "userId");
            cache.userProfileId = GetString(dict, "userProfileId");
            cache.optLock = GetInt(dict, "optLock");

            // CREATOR fields
            cache.website = GetString(dict, "website");
            cache.creatorType = GetString(dict, "creatorType");
            cache.region = GetStringList(dict, "region");
            cache.socials = GetStringList(dict, "socials");
            cache.tagline = GetString(dict, "tagline");
            cache.yearsOfExperience = GetInt(dict, "yearsOfExperience");

            Debug.Log($"[UserProfileDataFetcher] Profile parsed successfully for user: {cache.userName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserProfileDataFetcher] Error parsing profile data: {e}");
            throw;
        }

        return cache;
    }

    private string GetString(Dictionary<string, object> dict, string key)
    {
        if (dict.ContainsKey(key) && dict[key] != null)
        {
            return dict[key].ToString();
        }
        return "";
    }

    private int GetInt(Dictionary<string, object> dict, string key)
    {
        if (dict.ContainsKey(key) && dict[key] != null)
        {
            if (int.TryParse(dict[key].ToString(), out int val))
                return val;
        }
        return 0;
    }

    private List<string> GetStringList(Dictionary<string, object> dict, string key)
    {
        var result = new List<string>();
        if (dict.ContainsKey(key) && dict[key] != null)
        {
            if (dict[key] is List<object> rawList)
            {
                foreach (var item in rawList)
                {
                    if (item != null)
                        result.Add(item.ToString());
                }
            }
        }
        return result;
    }

    private Dictionary<string, List<string>> GetNestedDictionary(Dictionary<string, object> dict, string key)
    {
        var output = new Dictionary<string, List<string>>();
        if (!dict.ContainsKey(key) || dict[key] == null) return output;

        if (dict[key] is Dictionary<string, object> rawOuter)
        {
            foreach (var pair in rawOuter)
            {
                var innerList = new List<string>();
                if (pair.Value is List<object> rawInner)
                {
                    foreach (var item in rawInner)
                    {
                        if (item != null)
                            innerList.Add(item.ToString());
                    }
                }
                output[pair.Key] = innerList;
            }
        }
        return output;
    }

    private List<ProfileUserLite> ParseFollowers(string json)
    {
        var list = new List<ProfileUserLite>();

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[UserProfileDataFetcher] Followers JSON is null or empty");
            return null;
        }

        try
        {
            // Try JsonUtility first
            try
            {
                var followersResponse = JsonUtility.FromJson<FollowersApiResponse>(json);
                if (followersResponse != null && followersResponse.success && followersResponse.data != null)
                {
                    Debug.Log($"[UserProfileDataFetcher] JsonUtility parsing successful. Found {followersResponse.data.Length} users");
                    
                    foreach (var user in followersResponse.data)
                    {
                        var u = new ProfileUserLite
                        {
                            userId = user.userId ?? "",
                            firstName = user.firstName ?? "",
                            lastName = user.lastName ?? "",
                            userName = user.userName ?? "",
                            avatar = user.avatar ?? ""
                        };
                        list.Add(u);
                    }
                    return list;
                }
                else
                {
                    Debug.LogWarning("[UserProfileDataFetcher] JsonUtility parsing failed, trying MiniJSON fallback...");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UserProfileDataFetcher] JsonUtility failed: {e.Message}");
            }

            // Fallback to MiniJSON
            var parsed = JSON.Deserialize(json) as Dictionary<string, object>;
            if (parsed != null && parsed.TryGetValue("data", out var dataObj) && dataObj is List<object> users)
            {
                foreach (var item in users)
                {
                    if (item is Dictionary<string, object> dict)
                    {
                        var user = new ProfileUserLite
                        {
                            userId = dict.ContainsKey("userId") ? dict["userId"].ToString() : "",
                            firstName = dict.ContainsKey("firstName") ? dict["firstName"].ToString() : "",
                            lastName = dict.ContainsKey("lastName") ? dict["lastName"].ToString() : "",
                            userName = dict.ContainsKey("userName") ? dict["userName"].ToString() : "",
                            avatar = dict.ContainsKey("avatar") ? dict["avatar"].ToString() : ""
                        };
                        list.Add(user);
                    }
                }
                return list;
            }
            else
            {
                Debug.LogError("[UserProfileDataFetcher] Invalid followers JSON structure");
                Debug.LogError($"[UserProfileDataFetcher] JSON: {json}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[UserProfileDataFetcher] Error parsing followers/following JSON: " + e);
            Debug.LogError($"[UserProfileDataFetcher] JSON that failed: {json}");
        }

        return null;
    }
}