using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class UserProfileManager : MonoBehaviour
{
    [Header("User Info Components")]
    public TMP_Text userNameText;
    public TMP_Text userIdText;
    public Image userImage;

    [Header("Overview Components")]
    public TMP_Text followersText;
    public TMP_Text followingText;
    public TMP_Text postsText;

    [Header("Error Display (Optional)")]
    public TMP_Text errorText;

    [Header("Default Avatar")]
    public Sprite defaultAvatarSprite; // Assign in Inspector
    
    [Header("UI References")]
    public TMP_InputField nameInputField;
    public TMP_InputField emailInputField;

    private string profileApiEndpoint = "/api/v1/profile/me";
    private string token;
    private ProfileData cachedProfile; // Session cache

    // private void Start()
    // {
    //     string token = AuthTokenManager.GetToken();
    //     InitializeProfile(token);
    // }

    public void InitializeProfile(string bearerToken)
    {
        if (string.IsNullOrEmpty(bearerToken))
        {
            Debug.LogError("Token is null or empty. Cannot fetch profile data.");
            ShowError("Authentication token missing.");
            return;
        }

        token = bearerToken.StartsWith("Bearer ") ? bearerToken : "Bearer " + bearerToken;

        Debug.Log("<color=yellow>UserProfileManager initialized. Ready to fetch profile data.</color>");
        StartCoroutine(GetProfileData());
    }

    IEnumerator GetProfileData()
    {
        string fullUrl = baseScript.baseURL + profileApiEndpoint;
        Debug.Log("Sending GET request to: " + fullUrl);

        UnityWebRequest request = UnityWebRequest.Get(fullUrl);
        request.SetRequestHeader("Authorization", token);
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 10;

        yield return request.SendWebRequest();

        Debug.Log($"HTTP Status Code: {request.responseCode}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("<color=green>Profile API call successful.</color>\nRaw Response: " + request.downloadHandler.text);

            ProfileApiResponse response = JsonUtility.FromJson<ProfileApiResponse>(request.downloadHandler.text);

            if (response.success)
            {
                Debug.Log("Profile data retrieved successfully. Updating UI...");
                cachedProfile = response.data; // Cache it
                SetProfileData(response.data);
                SetInitialProfileValues(response.data);
            }
            else
            {
                Debug.LogError("Profile fetch failed: " + response.message);
                ShowError("Failed to load profile: " + response.message);

                if (cachedProfile != null)
                {
                    Debug.LogWarning("Using cached profile data due to API failure.");
                    SetProfileData(cachedProfile);
                    SetInitialProfileValues(cachedProfile);
                }
            }
        }
        else
        {
            Debug.LogError($"API Error: {request.error}\nResponse Code: {request.responseCode}");
            ShowError("Network error: " + request.error);

            if (cachedProfile != null)
            {
                Debug.LogWarning("Using cached profile data due to network error.");
                SetProfileData(cachedProfile);
                SetInitialProfileValues(cachedProfile);
            }
        }
    }
    
    public void SetInitialProfileValues(ProfileData profile)
    {
        if (nameInputField != null)
        {
            nameInputField.text = $"{profile.firstName} {profile.lastName}";
        }

        if (emailInputField != null)
        {
            emailInputField.text = profile.email;
        }
    }

    void SetProfileData(ProfileData data)
    {
        Debug.Log($"Updating UI with profile:\nName: {data.firstName} {data.lastName}\nUsername: {data.userName}\nFollowers: {data.followerCount}\nFollowing: {data.followingCount}\nPosts: {data.postCount}");

        userNameText.text = $"{data.firstName} {data.lastName}";
        userIdText.text = "@" + data.userName;

        followersText.text = data.followerCount.ToString();
        followingText.text = data.followingCount.ToString();
        postsText.text = data.postCount.ToString();

        if (!string.IsNullOrEmpty(data.avatar))
        {
            Debug.Log("Avatar URL found. Loading avatar...");
            StartCoroutine(LoadUserAvatar(data.avatar));
        }
        else
        {
            Debug.LogWarning("No avatar provided. Using default profile image.");
            SetDefaultAvatar();
        }
    }

    IEnumerator LoadUserAvatar(string avatarUrl)
    {
        Debug.Log("Starting avatar download from: " + avatarUrl);
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(avatarUrl);
        request.timeout = 10;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Avatar downloaded successfully. Setting sprite...");
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            userImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }
        else
        {
            Debug.LogError("Failed to download avatar: " + request.error);
            ShowError("Failed to load avatar image.");
            SetDefaultAvatar();
        }
    }

    void SetDefaultAvatar()
    {
        if (defaultAvatarSprite != null)
        {
            Debug.Log("Setting default avatar sprite.");
            userImage.sprite = defaultAvatarSprite;
        }
        else
        {
            Debug.LogWarning("Default avatar sprite is not assigned!");
        }
    }

    void ShowError(string message)
    {
        Debug.LogError("UI Error Message: " + message);
        if (errorText != null)
        {
            errorText.text = message;
        }
        else
        {
            Debug.LogWarning("No errorText assigned in UI. Error: " + message);
        }
    }
}

[System.Serializable]
public class ProfileApiResponse
{
    public bool success;
    public string message;
    public ProfileData data;
}

[System.Serializable]
public class ProfileData
{
    public int id;
    public string userName;
    public string firstName;
    public string lastName;
    public string email;
    public string role;
    public string provider;
    public int followerCount;
    public int followingCount;
    public int postCount;
    public string avatar;
}