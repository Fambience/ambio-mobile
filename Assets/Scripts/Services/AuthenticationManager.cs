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
    [SerializeField] private string backendBaseUrl = baseScript.stageBaseURL; // or leave empty to auto-use baseScript.baseURL
    [SerializeField] private bool mockBackend = false; // flip to false when your backend is ready

    private const string AuthSessionPath = "/api/v1/auth/google/firebase-login";

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

        // Keep if you ever add scene loads:
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

    // intent: "login" | "register" | "link"
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
                status = "ok",
                intent = intent,
                tokenType = "Bearer",
                token = "dev-session-" + System.Guid.NewGuid().ToString("N"),
                user = new ServerUser
                {
                    userId = fUser.UserId,
                    email = fUser.Email,
                    role = "user",
                    userName = fUser.DisplayName,
                    onboardingState = (intent == "register") ? "pending" : "complete"
                },
                remainingQuestions = (intent == "register") ? 3 : 0
            };
        }

        // 5) Real backend call
        var payload = new Dictionary<string, object>
        {
            { "idToken", firebaseIdToken },
            { "intent", intent }, // "login" | "register" | "link"
            { "googleSub", string.IsNullOrEmpty(gUser.UserId) ? TryDecodeSub(firebaseIdToken) : gUser.UserId }
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
                status = "ok",
                intent = "link",
                tokenType = "Bearer",
                token = "dev-session-" + System.Guid.NewGuid().ToString("N"),
                user = new ServerUser
                {
                    userId = auth.CurrentUser.UserId,
                    email = auth.CurrentUser.Email,
                    role = "user",
                    userName = auth.CurrentUser.DisplayName,
                    onboardingState = "complete"
                },
                remainingQuestions = 0
            };
        }

        var payload = new Dictionary<string, object>
        {
            { "idToken", firebaseIdToken },
            { "intent", "link" },
            { "googleSub", string.IsNullOrEmpty(gUser.UserId) ? TryDecodeSub(firebaseIdToken) : gUser.UserId }
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
        var r = new AuthResponse { user = new ServerUser() };
        if (data == null) return r;

        string S(object o) => o as string;

        if (data.TryGetValue("status", out var st)) r.status = S(st);
        if (data.TryGetValue("intent", out var it)) r.intent = S(it);
        if (data.TryGetValue("tokenType", out var tt)) r.tokenType = S(tt);
        if (data.TryGetValue("token", out var tk)) r.token = S(tk);
        if (data.TryGetValue("error", out var er)) r.error = S(er);
        if (data.TryGetValue("errorCode", out var ec)) r.errorCode = S(ec);

        if (data.TryGetValue("remainingQuestions", out var rq))
        {
            if (rq is long l) r.remainingQuestions = (int)l;
            else if (rq is int i) r.remainingQuestions = i;
            else int.TryParse(S(rq), out r.remainingQuestions);
        }

        if (data.TryGetValue("user", out var u) && u is Dictionary<string, object> ud)
        {
            if (ud.TryGetValue("userId", out var uid)) r.user.userId = S(uid);
            if (ud.TryGetValue("email", out var em)) r.user.email = S(em);
            if (ud.TryGetValue("role", out var rl)) r.user.role = S(rl);
            if (ud.TryGetValue("userName", out var un)) r.user.userName = S(un);
            if (ud.TryGetValue("onboardingState", out var ob)) r.user.onboardingState = S(ob);
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

    private AuthResponse Error(string code, string msg) =>
        new AuthResponse { status = "error", errorCode = code, error = msg };
}

// ===== Models aligned to your backend =====
[System.Serializable]
public class AuthResponse
{
    public string status;         // "ok" on success
    public string intent;         // echo from server
    public string tokenType;      // "Bearer"
    public string token;          // app JWT
    public ServerUser user;       // user payload
    public int remainingQuestions;
    public string error;          // failures
    public string errorCode;      // failures
}

[System.Serializable]
public class ServerUser
{
    public string userId;
    public string email;
    public string role;
    public string userName;
    public string onboardingState; // "pending" | "complete" | ...
}
