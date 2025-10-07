using UnityEngine;
using UnityEngine.Networking;
using Firebase;
using Firebase.Auth;
using Google;
using System.Threading.Tasks;
using System.Collections.Generic;
using MiniJSON;

[DefaultExecutionOrder(-200)] // ensure Awake runs before most UI scripts
public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager Instance { get; private set; }

    private FirebaseAuth auth;
    private static TaskCompletionSource<bool> _readyTcs = new TaskCompletionSource<bool>();

    [Header("Config")]
    [SerializeField] private string webClientId =
        "506296903677-nju0s3rig8bklqvjetovua8u4v74v12d.apps.googleusercontent.com"; // Firebase → Web client ID
    [SerializeField] private string backendBaseUrl = baseScript.baseURL;   // leave empty to auto-use baseScript.baseURL
    [SerializeField] private bool mockBackend = true;      // flip to false when your backend is ready

    private const string AuthSessionPath = "/api/v1/auth/session";

    /// Wait until Firebase deps are ready. UI can await this.
    public static async Task<bool> WaitUntilReady()
    {
        try { return await _readyTcs.Task; }
        catch { return false; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // if you’ll ever load another scene, you can keep this:
        // DontDestroyOnLoad(gameObject);

        if (string.IsNullOrWhiteSpace(backendBaseUrl))
        {
            try { backendBaseUrl = baseScript.baseURL?.TrimEnd('/'); }
            catch { backendBaseUrl = ""; }
        }

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(t =>
        {
            if (t.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("Firebase ready");
                if (!_readyTcs.Task.IsCompleted) _readyTcs.SetResult(true);
            }
            else
            {
                Debug.LogError("Firebase deps missing: " + t.Result);
                if (!_readyTcs.Task.IsCompleted) _readyTcs.SetException(new System.Exception(t.Result.ToString()));
            }
        });
    }

    private GoogleSignInConfiguration BuildGsiConfig() => new GoogleSignInConfiguration
    {
        WebClientId = webClientId,
        RequestIdToken = true,
        RequestEmail = true
    };

    // Convenience overload (defaults to "register")
    public Task<AuthResponse> SignInWithGoogleAsync() => SignInWithGoogleAsync("register");

    // intent: "login" | "register"
    public async Task<AuthResponse> SignInWithGoogleAsync(string intent)
    {
        if (auth == null) return Error("FIREBASE_NOT_READY", "Firebase not initialized");

        if (string.IsNullOrWhiteSpace(webClientId))
            Debug.LogWarning("webClientId is empty. Set your Firebase Web Client ID in AuthenticationManager.");

        // 1) Google Sign-In
        GoogleSignIn.Configuration = BuildGsiConfig();
        GoogleSignIn.Configuration.UseGameSignIn = false;

        GoogleSignInUser gUser;
        try { gUser = await GoogleSignIn.DefaultInstance.SignIn(); }
        catch (System.Exception e) { return Error("GOOGLE_SIGNIN_FAILED", e.Message); }

        if (gUser == null || string.IsNullOrEmpty(gUser.IdToken))
            return Error("GOOGLE_NO_TOKEN", "No ID token from Google");

        // 2) Firebase sign-in
        var credential = GoogleAuthProvider.GetCredential(gUser.IdToken, null);

        FirebaseUser fUser;
        try { fUser = await auth.SignInWithCredentialAsync(credential); }
        catch (System.Exception e) { return Error("FIREBASE_SIGNIN_FAILED", e.Message); }

        // 3) Fresh Firebase ID token
        string firebaseIdToken = await fUser.TokenAsync(true);

        // 4) Mock server response (dev mode)
        if (mockBackend)
        {
            return new AuthResponse
            {
                sessionToken = "dev-session-" + System.Guid.NewGuid().ToString("N"),
                user = new GoogleUserData
                {
                    userId = fUser.UserId,
                    email  = fUser.Email,
                    name   = fUser.DisplayName,
                    isNewUser = (intent == "register")
                }
            };
        }

        // 5) Real backend call
        var payload = new Dictionary<string, object>
        {
            { "intent", intent },
            { "idToken", firebaseIdToken },
            { "googleSub", string.IsNullOrEmpty(gUser.UserId) ? TryDecodeSub(firebaseIdToken) : gUser.UserId },
            { "deviceInfo", new Dictionary<string, object> {
                { "platform", Application.platform.ToString().ToLower() },
                { "appIdentifier", Application.identifier },
                { "deviceId", SystemInfo.deviceUniqueIdentifier },
            } },
        };

        var baseUrl = string.IsNullOrWhiteSpace(backendBaseUrl) ? baseScript.baseURL?.TrimEnd('/') : backendBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl)) return Error("NO_BACKEND_URL", "Backend base URL missing");

        return await PostJson(baseUrl + AuthSessionPath, payload);
    }

    public async Task<AuthResponse> LinkGoogleToCurrentUserAsync()
    {
        if (auth == null || auth.CurrentUser == null)
            return Error("NO_ACTIVE_USER", "User must be signed in to link");

        GoogleSignIn.Configuration = BuildGsiConfig();
        GoogleSignIn.Configuration.UseGameSignIn = false;

        GoogleSignInUser gUser;
        try { gUser = await GoogleSignIn.DefaultInstance.SignIn(); }
        catch (System.Exception e) { return Error("GOOGLE_SIGNIN_FAILED", e.Message); }

        if (gUser == null || string.IsNullOrEmpty(gUser.IdToken))
            return Error("GOOGLE_NO_TOKEN", "No ID token from Google");

        var credential = GoogleAuthProvider.GetCredential(gUser.IdToken, null);

        try { await auth.CurrentUser.LinkWithCredentialAsync(credential); }
        catch (System.AggregateException ae) { return Error("FIREBASE_LINK_FAILED", ae.Flatten().Message); }
        catch (System.Exception e)         { return Error("FIREBASE_LINK_FAILED", e.Message); }

        string firebaseIdToken = await auth.CurrentUser.TokenAsync(true);

        if (mockBackend)
        {
            return new AuthResponse
            {
                sessionToken = "dev-session-" + System.Guid.NewGuid().ToString("N"),
                user = new GoogleUserData
                {
                    userId = auth.CurrentUser.UserId,
                    email  = auth.CurrentUser.Email,
                    name   = auth.CurrentUser.DisplayName,
                    isNewUser = false
                }
            };
        }

        var payload = new Dictionary<string, object>
        {
            { "intent", "link" },
            { "idToken", firebaseIdToken },
            { "googleSub", string.IsNullOrEmpty(gUser.UserId) ? TryDecodeSub(firebaseIdToken) : gUser.UserId },
        };

        var baseUrl = string.IsNullOrWhiteSpace(backendBaseUrl) ? baseScript.baseURL?.TrimEnd('/') : backendBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl)) return Error("NO_BACKEND_URL", "Backend base URL missing");

        return await PostJson(baseUrl + AuthSessionPath, payload);
    }

    private async Task<AuthResponse> PostJson(string url, Dictionary<string, object> body)
    {
        string json = JSON.Serialize(body);
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            var text = req.downloadHandler.text;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Backend error {req.responseCode}: {req.error} | {text}");
                var dictErr = !string.IsNullOrEmpty(text) ? (JSON.Deserialize(text) as Dictionary<string, object>) : null;
                var respErr = ParseAuthResponse(dictErr);
                if (string.IsNullOrEmpty(respErr.error))
                {
                    respErr.error = req.error ?? "NETWORK_ERROR";
                    respErr.errorCode = string.IsNullOrEmpty(respErr.errorCode) ? "NETWORK_ERROR" : respErr.errorCode;
                }
                return respErr;
            }

            var dict = JSON.Deserialize(text) as Dictionary<string, object>;
            return ParseAuthResponse(dict);
        }
    }

    private AuthResponse ParseAuthResponse(Dictionary<string, object> data)
    {
        var r = new AuthResponse { user = new GoogleUserData() };
        if (data == null) return r;

        if (data.TryGetValue("sessionToken", out var st)) r.sessionToken = st as string;
        if (data.TryGetValue("errorCode", out var ec)) r.errorCode = ec as string;
        if (data.TryGetValue("error", out var er)) r.error = er as string;

        if (data.TryGetValue("user", out var u) && u is Dictionary<string, object> ud)
        {
            if (ud.TryGetValue("userId", out var uid)) r.user.userId = uid as string;
            if (ud.TryGetValue("email", out var em)) r.user.email = em as string;
            if (ud.TryGetValue("name", out var nm)) r.user.name = nm as string;
            if (ud.TryGetValue("isNewUser", out var nu)) r.user.isNewUser = nu is bool b && b;
        }
        return r;
    }

    // Minimal JWT "sub" extractor
    private static string TryDecodeSub(string jwt)
    {
        try
        {
            var parts = jwt?.Split('.');
            if (parts == null || parts.Length < 2) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            var json = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(payload));
            var dict = JSON.Deserialize(json) as Dictionary<string, object>;
            return dict != null && dict.TryGetValue("sub", out var sub) ? sub.ToString() : null;
        }
        catch { return null; }
    }

    private AuthResponse Error(string code, string msg) => new AuthResponse { errorCode = code, error = msg };
}

[System.Serializable] public class AuthResponse { public string sessionToken; public GoogleUserData user; public string error; public string errorCode; }
[System.Serializable] public class GoogleUserData { public string userId; public string email; public string name; public bool isNewUser; }
