using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using MiniJSON;
using Services;

// Define ProfileUserLite at the top level, outside of any class
[System.Serializable]
public class ProfileUserLite
{
    public string userId;
    public string firstName;
    public string lastName;
    public string userName;
    public string avatar;
}

// Add these classes at the top level for followers/following API responses
[System.Serializable]
public class FollowersApiResponse
{
    public bool success;
    public string message;
    public FollowerData[] data;
    public PaginationData pagination;
}

[System.Serializable]
public class FollowerData
{
    public string userId;
    public string firstName;
    public string lastName;
    public string userName;
    public string avatar;
}

[System.Serializable]
public class PaginationData
{
    public int total;
    public int page;
    public int limit;
    public int totalPages;
}

public partial class ProfileDataHandlers : MonoBehaviour
{
    public static ProfileDataHandlers Instance { get; private set; }
    public ProfileCache ProfileData { get; private set; }

    // Static lists for followers and following
    public static List<ProfileUserLite> FollowersList = new List<ProfileUserLite>();
    public static List<ProfileUserLite> FollowingList = new List<ProfileUserLite>();

    private string token;
    private string cachePath => Path.Combine(Application.persistentDataPath, "profile_cache.json");
    public string baseURL = baseScript.baseURL;

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

        token = AuthTokenManager.GetToken();

        StartCoroutine(FetchProfileData(token, success =>
        {
            if (success)
            {
                Debug.Log("[ProfileDataHandlers] Profile data fetched successfully.");
                // Fetch followers and following after profile data is loaded
                FetchFollowersAndFollowing();
            }
            else
            {
                Debug.LogWarning("[ProfileDataHandlers] Failed to fetch profile data.");
            }
        }));
    }

    public void FetchFollowersAndFollowing()
    {
        StartCoroutine(FetchMyFollowersList(() => {
            Debug.Log("[ProfileDataHandlers] Followers fetch completed");
        }));
        StartCoroutine(FetchMyFollowingList(() => {
            Debug.Log("[ProfileDataHandlers] Following fetch completed");
        }));
    }

    public IEnumerator FetchProfileData(string authToken, Action<bool> onComplete)
    {
        authToken = AuthTokenManager.GetToken();
        string url = baseScript.baseURL + baseScript.profileEndpoint;
        Debug.Log("[ProfileDataHandlers] API url: " + url);
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"{authToken}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ProfileDataHandlers] Error fetching profile: {request.error}");
            onComplete?.Invoke(false);
            yield break;
        }
        
        string jsonText = request.downloadHandler.text;
        Debug.Log("[ProfileDataHandlers] Profile JSON data fetched successfully.");
        Debug.Log("[ProfileDataHandlers] Raw Json Data: " + jsonText);

        // Try Unity's JsonUtility first, then fallback to MiniJSON
        bool parseSuccess = false;
        
        try
        {
            // Parse with Unity JsonUtility
            var apiResponse = JsonUtility.FromJson<ApiResponse>(jsonText);
            if (apiResponse != null && apiResponse.success)
            {
                Debug.Log("[ProfileDataHandlers] JsonUtility parsing successful");
                ProfileData = ConvertToProfileCache(apiResponse.data);
                SaveProfileCache(ProfileData);
                Debug.Log("[ProfileDataHandlers] Profile data processing completed successfully.");
                parseSuccess = true;
            }
            else
            {
                Debug.LogWarning("[ProfileDataHandlers] JsonUtility parsing failed or success = false");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ProfileDataHandlers] JsonUtility failed: {e.Message}");
        }

        // Fallback to MiniJSON if JsonUtility failed
        if (!parseSuccess)
        {
            Debug.Log("[ProfileDataHandlers] Trying MiniJSON as fallback...");
            try
            {
                var result = JSON.Deserialize(jsonText) as Dictionary<string, object>;
                if (result != null && result.ContainsKey("success") && (bool)result["success"])
                {
                    Debug.Log("[ProfileDataHandlers] MiniJSON parsing successful");
                    if (result.ContainsKey("data"))
                    {
                        var rawData = result["data"];
                        if (rawData is Dictionary<string, object> dataDict)
                        {
                            ProfileData = ParseProfileData(dataDict);
                            SaveProfileCache(ProfileData);
                            Debug.Log("[ProfileDataHandlers] Profile data processing completed successfully via MiniJSON.");
                            parseSuccess = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProfileDataHandlers] MiniJSON also failed: {e}");
            }
        }

        onComplete?.Invoke(parseSuccess);
    }

    public IEnumerator FetchMyFollowersList(Action onComplete = null)
    {
        Debug.Log("[Followers] Starting FetchMyFollowersList...");
        
        string token = AuthTokenManager.GetToken();
        Debug.Log($"[Followers] Token retrieved: {(!string.IsNullOrEmpty(token) ? "Valid" : "NULL/EMPTY")}");
        
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[Followers] Token missing.");
            onComplete?.Invoke();
            yield break;
        }

        // Check if UserData.userName is available
        string userName = "";
        if (ProfileData != null && !string.IsNullOrEmpty(ProfileData.userName))
        {
            userName = ProfileData.userName;
        }
        else if (UserData.userName != null)
        {
            userName = UserData.userName;
        }
        
        Debug.Log($"[Followers] Using userName: {userName ?? "NULL"}");
        if (string.IsNullOrEmpty(userName))
        {
            Debug.LogError("[Followers] Username missing. Cannot fetch followers.");
            onComplete?.Invoke();
            yield break;
        }

        string followersURL = $"{baseURL}/api/v1/profile/{userName}/followers";
        Debug.Log($"[Followers] Request URL: {followersURL}");
        
        UnityWebRequest req = UnityWebRequest.Get(followersURL);
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        
        Debug.Log("[Followers] Sending web request...");
        yield return req.SendWebRequest();

        Debug.Log($"[Followers] Request completed with result: {req.result}");
        Debug.Log($"[Followers] Response code: {req.responseCode}");

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[Followers] Request successful, processing response...");
            FollowersList.Clear();

            var json = req.downloadHandler.text;
            Debug.Log($"[Followers] Raw JSON response: {json}");
            Debug.Log($"[Followers] JSON length: {json?.Length ?? 0}");
            Debug.Log($"[Followers] JSON is null or empty: {string.IsNullOrEmpty(json)}");
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[Followers] Response JSON is null or empty");
                onComplete?.Invoke();
                yield break;
            }
            
            try
            {
                // Try Unity's JsonUtility first (more reliable)
                var followersResponse = JsonUtility.FromJson<FollowersApiResponse>(json);
                if (followersResponse != null && followersResponse.success && followersResponse.data != null)
                {
                    Debug.Log($"[Followers] JsonUtility parsing successful. Found {followersResponse.data.Length} followers");
                    
                    foreach (var follower in followersResponse.data)
                    {
                        var u = new ProfileUserLite
                        {
                            userId = follower.userId ?? "",
                            firstName = follower.firstName ?? "",
                            lastName = follower.lastName ?? "",
                            userName = follower.userName ?? "",
                            avatar = follower.avatar ?? ""
                        };

                        Debug.Log($"[Followers] Added user: {u.userName} ({u.firstName} {u.lastName})");
                        FollowersList.Add(u);
                    }
                }
                else
                {
                    Debug.LogWarning("[Followers] JsonUtility parsing failed, trying MiniJSON fallback...");
                    
                    // Fallback to MiniJSON
                    var parsed = JSON.Deserialize(json) as Dictionary<string, object>;
                    Debug.Log($"[Followers] MiniJSON parsed object is null: {parsed == null}");
                    if (parsed != null)
                    {
                        Debug.Log($"[Followers] JSON parsed successfully. Keys: {string.Join(", ", parsed.Keys)}");
                        
                        if (parsed.TryGetValue("data", out var dataObj))
                        {
                            Debug.Log($"[Followers] Data object type: {dataObj?.GetType().Name ?? "NULL"}");
                            
                            if (dataObj is List<object> list)
                            {
                                Debug.Log($"[Followers] Found {list.Count} followers in response");
                                
                                foreach (var item in list)
                                {
                                    if (item is Dictionary<string, object> dict)
                                    {
                                        Debug.Log($"[Followers] Processing follower with keys: {string.Join(", ", dict.Keys)}");
                                        
                                        var u = new ProfileUserLite
                                        {
                                            userId = dict.ContainsKey("userId") ? dict["userId"].ToString() : "",
                                            firstName = dict.ContainsKey("firstName") ? dict["firstName"].ToString() : "",
                                            lastName = dict.ContainsKey("lastName") ? dict["lastName"].ToString() : "",
                                            userName = dict.ContainsKey("userName") ? dict["userName"].ToString() : "",
                                            avatar = dict.ContainsKey("avatar") && dict["avatar"] != null ? dict["avatar"].ToString() : ""
                                        };

                                        Debug.Log($"[Followers] Added user: {u.userName} ({u.firstName} {u.lastName})");
                                        FollowersList.Add(u);
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"[Followers] Unexpected item type in list: {item?.GetType().Name ?? "NULL"}");
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"[Followers] Data is not a List<object>. Type: {dataObj?.GetType().Name ?? "NULL"}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[Followers] No 'data' key found in response");
                        }
                    }
                    else
                    {
                        Debug.LogError("[Followers] Failed to parse JSON response - both JsonUtility and MiniJSON failed");
                        Debug.LogError($"[Followers] Raw JSON for debugging: '{json}'");
                        Debug.LogError($"[Followers] First 100 chars: '{json.Substring(0, Math.Min(100, json.Length))}'");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Followers] Exception during JSON parsing: {e.Message}\nStackTrace: {e.StackTrace}");
                Debug.LogError($"[Followers] Raw JSON that failed: '{json}'");
            }
            
            Debug.Log($"[Followers] Final count: {FollowersList.Count} users cached.");
        }
        else
        {
            Debug.LogError($"[Followers] Request failed - Result: {req.result}, Error: {req.error}");
            Debug.LogError($"[Followers] Response text: {req.downloadHandler?.text ?? "No response text"}");
        }

        onComplete?.Invoke();
    }

    public IEnumerator FetchMyFollowingList(Action onComplete = null)
    {
        Debug.Log("[Following] Starting FetchMyFollowingList...");
        
        string token = AuthTokenManager.GetToken();
        Debug.Log($"[Following] Token retrieved: {(!string.IsNullOrEmpty(token) ? "Valid" : "NULL/EMPTY")}");
        
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[Following] Token missing.");
            onComplete?.Invoke();
            yield break;
        }

        // Check if UserData.userName is available
        string userName = "";
        if (ProfileData != null && !string.IsNullOrEmpty(ProfileData.userName))
        {
            userName = ProfileData.userName;
        }
        else if (UserData.userName != null)
        {
            userName = UserData.userName;
        }
        
        Debug.Log($"[Following] Using userName: {userName ?? "NULL"}");
        if (string.IsNullOrEmpty(userName))
        {
            Debug.LogError("[Following] Username missing. Cannot fetch following.");
            onComplete?.Invoke();
            yield break;
        }

        string followingURL = $"{baseURL}/api/v1/profile/{userName}/following";
        Debug.Log($"[Following] Request URL: {followingURL}");
        
        UnityWebRequest req = UnityWebRequest.Get(followingURL);
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        
        Debug.Log("[Following] Sending web request...");
        yield return req.SendWebRequest();

        Debug.Log($"[Following] Request completed with result: {req.result}");
        Debug.Log($"[Following] Response code: {req.responseCode}");

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[Following] Request successful, processing response...");
            FollowingList.Clear();

            var json = req.downloadHandler.text;
            Debug.Log($"[Following] Raw JSON response: {json}");
            Debug.Log($"[Following] JSON length: {json?.Length ?? 0}");
            Debug.Log($"[Following] JSON is null or empty: {string.IsNullOrEmpty(json)}");
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[Following] Response JSON is null or empty");
                onComplete?.Invoke();
                yield break;
            }
            
            try
            {
                // Try Unity's JsonUtility first (more reliable)
                var followingResponse = JsonUtility.FromJson<FollowersApiResponse>(json);
                if (followingResponse != null && followingResponse.success && followingResponse.data != null)
                {
                    Debug.Log($"[Following] JsonUtility parsing successful. Found {followingResponse.data.Length} following users");
                    
                    foreach (var followingUser in followingResponse.data)
                    {
                        var u = new ProfileUserLite
                        {
                            userId = followingUser.userId ?? "",
                            firstName = followingUser.firstName ?? "",
                            lastName = followingUser.lastName ?? "",
                            userName = followingUser.userName ?? "",
                            avatar = followingUser.avatar ?? ""
                        };

                        Debug.Log($"[Following] Added user: {u.userName} ({u.firstName} {u.lastName})");
                        FollowingList.Add(u);
                    }
                }
                else
                {
                    Debug.LogWarning("[Following] JsonUtility parsing failed, trying MiniJSON fallback...");
                    
                    // Fallback to MiniJSON
                    var parsed = JSON.Deserialize(json) as Dictionary<string, object>;
                    Debug.Log($"[Following] MiniJSON parsed object is null: {parsed == null}");
                    if (parsed != null)
                    {
                        Debug.Log($"[Following] JSON parsed successfully. Keys: {string.Join(", ", parsed.Keys)}");
                        
                        if (parsed.TryGetValue("data", out var dataObj))
                        {
                            Debug.Log($"[Following] Data object type: {dataObj?.GetType().Name ?? "NULL"}");
                            
                            if (dataObj is List<object> list)
                            {
                                Debug.Log($"[Following] Found {list.Count} following users in response");
                                
                                foreach (var item in list)
                                {
                                    if (item is Dictionary<string, object> dict)
                                    {
                                        Debug.Log($"[Following] Processing user with keys: {string.Join(", ", dict.Keys)}");
                                        
                                        var u = new ProfileUserLite
                                        {
                                            userId = dict.ContainsKey("userId") ? dict["userId"].ToString() : "",
                                            firstName = dict.ContainsKey("firstName") ? dict["firstName"].ToString() : "",
                                            lastName = dict.ContainsKey("lastName") ? dict["lastName"].ToString() : "",
                                            userName = dict.ContainsKey("userName") ? dict["userName"].ToString() : "",
                                            avatar = dict.ContainsKey("avatar") && dict["avatar"] != null ? dict["avatar"].ToString() : ""
                                        };

                                        Debug.Log($"[Following] Added user: {u.userName} ({u.firstName} {u.lastName})");
                                        FollowingList.Add(u);
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"[Following] Unexpected item type in list: {item?.GetType().Name ?? "NULL"}");
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"[Following] Data is not a List<object>. Type: {dataObj?.GetType().Name ?? "NULL"}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[Following] No 'data' key found in response");
                        }
                    }
                    else
                    {
                        Debug.LogError("[Following] Failed to parse JSON response - both JsonUtility and MiniJSON failed");
                        Debug.LogError($"[Following] Raw JSON for debugging: '{json}'");
                        Debug.LogError($"[Following] First 100 chars: '{json.Substring(0, Math.Min(100, json.Length))}'");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Following] Exception during JSON parsing: {e.Message}\nStackTrace: {e.StackTrace}");
                Debug.LogError($"[Following] Raw JSON that failed: '{json}'");
            }
            
            Debug.Log($"[Following] Final count: {FollowingList.Count} users cached.");
        }
        else
        {
            Debug.LogError($"[Following] Request failed - Result: {req.result}, Error: {req.error}");
            Debug.LogError($"[Following] Response text: {req.downloadHandler?.text ?? "No response text"}");
        }

        onComplete?.Invoke();
    }

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

        Debug.Log($"[ProfileDataHandlers] Profile converted successfully for user: {cache.userName}");
        return cache;
    }

    public ProfileCache LoadProfileCache()
    {
        if (!File.Exists(cachePath)) return null;

        try
        {
            string json = File.ReadAllText(cachePath);
            var data = JSON.Deserialize(json) as Dictionary<string, object>;
            ProfileData = ParseProfileData(data);
            return ProfileData;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ProfileDataHandlers] Failed to load cache: {e}");
            return null;
        }
    }

    public void SaveProfileCache(ProfileCache data)
    {
        try
        {
            var dict = data.ToDictionary();
            string json = JSON.Serialize(dict);
            File.WriteAllText(cachePath, json);
            Debug.Log("[ProfileDataHandlers] Profile cache saved successfully.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ProfileDataHandlers] Failed to save cache: {e}");
        }
    }

    private ProfileCache ParseProfileData(Dictionary<string, object> dict)
    {
        Debug.Log("[ProfileDataHandlers] Starting to parse profile data with MiniJSON...");
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

            Debug.Log($"[ProfileDataHandlers] Profile parsed successfully for user: {cache.userName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProfileDataHandlers] Error parsing profile data: {e}");
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
}

// Data classes for JsonUtility
[Serializable]
public class ApiResponse
{
    public bool success;
    public string message;
    public ProfileDataRaw data;
}

[Serializable]
public class ProfileDataRaw
{
    public int id;
    public string userName;
    public string firstName;
    public string lastName;
    public string email;
    public string role;
    public string provider;
    public string bio;
    public string avatar;
    public string createdAt;
    public int optLock;
    public string updatedAt;
    public string userId;
    public string userProfileId;
    public string homeLocation;
    public string[] colorScheme;
    public string[] homeSharingWith;
    public int minBudget;
    public int maxBudget;
    public DesignInspirationsRaw designInspirations;
    public int followerCount;
    public int followingCount;
    public int postCount;
    public string website;
    public string creatorType;
    public string[] region;
    public string[] socials;
    public string tagline;
    public int yearsOfExperience;
}

[Serializable]
public class DesignInspirationsRaw
{
    public string[] MODERN_AND_MINIMAL;
    public string[] CREATIVE_AND_CHARACTERFUL;
}

[Serializable]
public class ProfileCache
{
    public string userName;
    public string firstName;
    public string lastName;
    public string email;
    public string role;
    public string provider;
    public string bio;
    public string avatar;

    // USER-specific
    public string homeLocation;
    public int minBudget;
    public int maxBudget;
    public List<string> colorScheme;
    public List<string> homeSharingWith;
    public Dictionary<string, List<string>> designInspirations;
    public string createdAt;
    public string updatedAt;
    public string userId;
    public string userProfileId;
    public int optLock;

    // CREATOR-specific
    public string website;
    public string creatorType;
    public List<string> region;
    public List<string> socials;
    public string tagline;
    public int yearsOfExperience;

    // Common
    public int followerCount;
    public int followingCount;
    public int postCount;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            {"userName", userName},
            {"firstName", firstName},
            {"lastName", lastName},
            {"email", email},
            {"role", role},
            {"provider", provider},
            {"bio", bio},
            {"avatar", avatar},
            {"homeLocation", homeLocation},
            {"minBudget", minBudget},
            {"maxBudget", maxBudget},
            {"colorScheme", colorScheme},
            {"homeSharingWith", homeSharingWith},
            {"designInspirations", designInspirations},
            {"createdAt", createdAt},
            {"updatedAt", updatedAt},
            {"userId", userId},
            {"userProfileId", userProfileId},
            {"optLock", optLock},
            {"website", website},
            {"creatorType", creatorType},
            {"region", region},
            {"socials", socials},
            {"tagline", tagline},
            {"yearsOfExperience", yearsOfExperience},
            {"followerCount", followerCount},
            {"followingCount", followingCount},
            {"postCount", postCount}
        };
    }
}