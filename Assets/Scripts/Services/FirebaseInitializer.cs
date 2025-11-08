using System;
using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Extensions;

/// <summary>
/// Initializes Firebase and its dependencies
/// Must be present in the first scene that loads
/// </summary>
public class FirebaseInitializer : MonoBehaviour
{
    public static FirebaseInitializer Instance { get; private set; }

    [Header("Status")]
    public bool IsFirebaseReady { get; private set; } = false;
    public DependencyStatus FirebaseDependencyStatus { get; private set; }

    // Events
    public event Action OnFirebaseReady;
    public event Action<string> OnFirebaseError;

    private FirebaseApp firebaseApp;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Debug.Log("[Firebase] Starting Firebase initialization...");
        StartCoroutine(InitializeFirebase());
    }

    /// <summary>
    /// Initialize Firebase and check dependencies
    /// </summary>
    private IEnumerator InitializeFirebase()
    {
        // Check and fix dependencies
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();

        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        FirebaseDependencyStatus = dependencyTask.Result;

        if (FirebaseDependencyStatus == DependencyStatus.Available)
        {
            Debug.Log("[Firebase] Dependencies resolved successfully");

            // Initialize Firebase App
            InitializeFirebaseApp();

            // Mark as ready
            IsFirebaseReady = true;

            Debug.Log("[Firebase] Firebase initialized successfully");

            // Notify listeners
            OnFirebaseReady?.Invoke();
        }
        else
        {
            string errorMessage = $"Could not resolve all Firebase dependencies: {FirebaseDependencyStatus}";
            Debug.LogError($"[Firebase] {errorMessage}");

            IsFirebaseReady = false;

            // Notify listeners of error
            OnFirebaseError?.Invoke(errorMessage);
        }
    }

    /// <summary>
    /// Initialize the Firebase App instance
    /// </summary>
    private void InitializeFirebaseApp()
    {
        firebaseApp = FirebaseApp.DefaultInstance;

        if (firebaseApp != null)
        {
            Debug.Log($"[Firebase] Firebase App initialized: {firebaseApp.Name}");
            Debug.Log($"[Firebase] Firebase App Options: {firebaseApp.Options.AppId}");
        }
        else
        {
            Debug.LogError("[Firebase] Failed to initialize Firebase App");
        }
    }

    /// <summary>
    /// Get the Firebase App instance
    /// </summary>
    public FirebaseApp GetFirebaseApp()
    {
        return firebaseApp;
    }

    /// <summary>
    /// Wait for Firebase to be ready (useful for other scripts)
    /// </summary>
    public IEnumerator WaitForFirebaseReady()
    {
        yield return new WaitUntil(() => IsFirebaseReady);
    }

    private void OnDestroy()
    {
        // Clean up events
        OnFirebaseReady = null;
        OnFirebaseError = null;
    }
}
