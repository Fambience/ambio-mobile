using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using MiniJSON;
using Services;

public partial class ProfileDataHandlers : MonoBehaviour
{
    public static ProfileDataHandlers Instance { get; private set; }

    public ProfileCache ProfileData { get; private set; }

    private string token;
    private string cachePath => Path.Combine(Application.persistentDataPath, "profile_cache.json");

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
            }
            else
            {
                Debug.LogWarning("[ProfileDataHandlers] Failed to fetch profile data.");
            }
        }));
        StartCoroutine(FetchMyFollowersList());
        StartCoroutine(FetchMyFollowingList());
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
public class ProfileUserLite
{
    public string userId;
    public string firstName;
    public string lastName;
    public string userName;
    public string avatar;
}

public partial class ProfileDataHandlers : MonoBehaviour
{
    public static List<ProfileUserLite> FollowersList = new();
    public static List<ProfileUserLite> FollowingList = new();

    public string baseURL = baseScript.baseURL;

    public IEnumerator FetchMyFollowersList(Action onComplete = null)
    {
        string token = AuthTokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[Followers] Token missing.");
            yield break;
        }

        string followersURL = $"{baseURL}/api/v1/profile/{UserData.userName}/followers";
        UnityWebRequest req = UnityWebRequest.Get(followersURL);
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            FollowersList.Clear();

            var json = req.downloadHandler.text;
            var parsed = JSON.Deserialize(json) as Dictionary<string, object>;
            if (parsed != null && parsed.TryGetValue("data", out var dataObj))
            {
                var list = dataObj as List<object>;
                foreach (var item in list)
                {
                    var dict = item as Dictionary<string, object>;
                    var u = new ProfileUserLite
                    {
                        userId = dict.ContainsKey("userId") ? dict["userId"].ToString() : "",
                        firstName = dict.ContainsKey("firstName") ? dict["firstName"].ToString() : "",
                        lastName = dict.ContainsKey("lastName") ? dict["lastName"].ToString() : "",
                        userName = dict.ContainsKey("userName") ? dict["userName"].ToString() : "",
                        avatar = dict.ContainsKey("avatar") && dict["avatar"] != null ? dict["avatar"].ToString() : ""
                    };

                    FollowersList.Add(u);
                }
                Debug.Log($"[Followers] Cached {FollowersList.Count} users.");
            }
        }
        else
        {
            Debug.LogError("[Followers] Error: " + req.error);
        }

        onComplete?.Invoke();
    }

    public IEnumerator FetchMyFollowingList(Action onComplete = null)
    {
        string token = AuthTokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[Following] Token missing.");
            yield break;
        }

        string followingURL = $"{baseURL}/api/v1/profile/{UserData.userName}/following";
        UnityWebRequest req = UnityWebRequest.Get(followingURL);
        req.SetRequestHeader("Authorization", $"Bearer {token}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            FollowingList.Clear();

            var json = req.downloadHandler.text;
            var parsed = JSON.Deserialize(json) as Dictionary<string, object>;
            if (parsed != null && parsed.TryGetValue("data", out var dataObj))
            {
                var list = dataObj as List<object>;
                foreach (var item in list)
                {
                    var dict = item as Dictionary<string, object>;
                    var u = new ProfileUserLite
                    {
                        userId = dict.ContainsKey("userId") ? dict["userId"].ToString() : "",
                        firstName = dict.ContainsKey("firstName") ? dict["firstName"].ToString() : "",
                        lastName = dict.ContainsKey("lastName") ? dict["lastName"].ToString() : "",
                        userName = dict.ContainsKey("userName") ? dict["userName"].ToString() : "",
                        avatar = dict.ContainsKey("avatar") && dict["avatar"] != null ? dict["avatar"].ToString() : ""
                    };

                    FollowingList.Add(u);
                }
                Debug.Log($"[Following] Cached {FollowingList.Count} users.");
            }
        }
        else
        {
            Debug.LogError("[Following] Error: " + req.error);
        }

        onComplete?.Invoke();
    }
}