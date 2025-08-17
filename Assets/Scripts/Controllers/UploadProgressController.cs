using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class UploadProgressController : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement root;
    private VisualElement progressBarFill;
    private TextElement percentageText;
    private TextElement uploadTitle;
    private Image uploadIcon;
    
    [Header("Progress Settings")]
    [Range(0f, 100f)]
    public float currentProgress = 0f;
    public float animationSpeed = 50f; // Increased for smoother animation
    public bool autoSimulateProgress = false; // Changed to false by default
    public float simulationSpeed = 1f;
    
    [Header("Test Settings")]
    public bool simulateError = false;
    public float errorAtPercentage = 75f;
    
    private float targetProgress = 0f;
    private bool isUploading = false;
    private UploadState currentState = UploadState.Ready;
    
    public enum UploadState
    {
        Ready,
        Uploading,
        Completed,
        Error
    }

    void Start()
    {
        InitializeUI();
    }

    void InitializeUI()
    {
        // Get the UI Document component
        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            // Try to get from HomeScreenController if not on this component
            var homeController = GetComponent<HomeScreenController>();
            if (homeController != null)
            {
                uiDocument = homeController.GetComponent<UIDocument>();
            }
            
            if (uiDocument == null)
            {
                Debug.LogError("UIDocument component not found!");
                return;
            }
        }

        root = uiDocument.rootVisualElement;
    
        // Get references to UI elements from the home screen UXML
        progressBarFill = root.Q<VisualElement>("progressBarFill");
        percentageText = root.Q<TextElement>("percentage");
        uploadTitle = root.Q<TextElement>("uploadTitle");
        uploadIcon = root.Q<Image>("uploadIcon");

        if (progressBarFill == null)
        {
            Debug.LogError("progressBarFill element not found! Check UXML structure.");
        }
        if (percentageText == null)
        {
            Debug.LogError("percentage element not found! Check UXML structure.");
        }
        if (uploadTitle == null)
        {
            Debug.LogError("uploadTitle element not found! Check UXML structure.");
        }
        if (uploadIcon == null)
        {
            Debug.LogError("uploadIcon element not found! Check UXML structure.");
        }

        // Initialize to ready state
        SetUploadState(UploadState.Ready);
    }

    void Update()
    {
        // Smoothly animate progress bar (except in error state)
        if (currentState != UploadState.Error && Mathf.Abs(currentProgress - targetProgress) > 0.1f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, animationSpeed * Time.deltaTime);
            UpdateProgressUI();
        }
    }

    // UploadProgressController.cs
    public void SetProgress(float progress)
    {
        if (currentState == UploadState.Error) return;

        targetProgress = Mathf.Clamp(progress, 0f, 100f);

        // NEW: ensure UI refs exist even if Start() hasn’t run yet
        if (progressBarFill == null) InitializeUI();

        // NEW: make the bar reflect the latest number immediately
        currentProgress = targetProgress;
        UpdateProgressUI();
    }

    public void StartUpload()
    {
        Debug.Log("UploadProgressController: StartUpload called");
        isUploading = true;
        SetUploadState(UploadState.Uploading);
        SetProgress(0f);
    }

    public void CompleteUpload()
    {
        Debug.Log("UploadProgressController: CompleteUpload called");
        isUploading = false;
        autoSimulateProgress = false;
        SetUploadState(UploadState.Completed);
        SetProgress(100f);
        
        // Optional: Hide UI after completion
        StartCoroutine(HideUIAfterDelay(3f));
    }

    public void TriggerError()
    {
        Debug.Log("UploadProgressController: TriggerError called");
        isUploading = false;
        autoSimulateProgress = false;
        SetUploadState(UploadState.Error);
        // Don't change progress - keep it where it failed
    }

    public void CancelUpload()
    {
        Debug.Log("UploadProgressController: CancelUpload called");
        isUploading = false;
        autoSimulateProgress = false;
        SetUploadState(UploadState.Ready);
        SetProgress(0f);
    }

    private void SetUploadState(UploadState newState)
    {
        Debug.Log($"UploadProgressController: State changed from {currentState} to {newState}");
        currentState = newState;
    
        if (progressBarFill == null)
        {
            Debug.LogError("progressBarFill is null, cannot update state");
            return;
        }
    
        // Remove existing CSS classes
        progressBarFill.RemoveFromClassList("completed");
        progressBarFill.RemoveFromClassList("error");
    
        switch (newState)
        {
            case UploadState.Ready:
                if (uploadTitle != null) uploadTitle.text = "Ready to Upload";
                if (uploadIcon != null) uploadIcon.image = Resources.Load<Texture2D>("uploading");
                if (percentageText != null) percentageText.style.display = DisplayStyle.Flex;
                break;
            
            case UploadState.Uploading:
                if (uploadTitle != null) uploadTitle.text = "Uploading...";
                if (uploadIcon != null) uploadIcon.image = Resources.Load<Texture2D>("uploading");
                if (percentageText != null) percentageText.style.display = DisplayStyle.Flex;
                break;
            
            case UploadState.Completed:
                if (uploadTitle != null) uploadTitle.text = "Uploaded Successfully";
                if (uploadIcon != null) uploadIcon.image = Resources.Load<Texture2D>("uploaded");
                if (percentageText != null) percentageText.style.display = DisplayStyle.Flex;
                progressBarFill.AddToClassList("completed");
                break;
            
            case UploadState.Error:
                if (uploadTitle != null) uploadTitle.text = "Upload Failed";
                if (uploadIcon != null) uploadIcon.image = Resources.Load<Texture2D>("uploadingError");
                if (percentageText != null) percentageText.style.display = DisplayStyle.None;
                progressBarFill.AddToClassList("error");
                break;
        }
    
        UpdateProgressUI();
    }

    private void UpdateProgressUI()
    {
        if (progressBarFill != null)
        {
            // Update progress bar width
            progressBarFill.style.width = Length.Percent(currentProgress);
            Debug.Log($"UploadProgressController: Progress bar updated to {currentProgress}%");
        }
        else
        {
            Debug.LogError("progressBarFill is null, cannot update progress UI");
        }

        if (percentageText != null && currentState != UploadState.Error)
        {
            // Update percentage text (only if not in error state)
            percentageText.text = $"{currentProgress:F0}%";
        }
    }

    private IEnumerator HideUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (root != null)
        {
            var uploadSection = root.Q<VisualElement>("uploadProgressSection");
            if (uploadSection != null)
            {
                uploadSection.style.display = DisplayStyle.None;
            }
            else
            {
                root.style.display = DisplayStyle.None;
            }
        }
    }

    // Public methods for external control
    public void ResetProgress()
    {
        SetProgress(0f);
        isUploading = false;
        SetUploadState(UploadState.Ready);
    }

    public void SetUploadSpeed(float speed)
    {
        simulationSpeed = Mathf.Max(0.1f, speed);
    }

    public bool IsUploading()
    {
        return isUploading;
    }

    public float GetCurrentProgress()
    {
        return currentProgress;
    }

    public UploadState GetCurrentState()
    {
        return currentState;
    }

    // Method to manually trigger error for testing
    public void SimulateError()
    {
        TriggerError();
    }

    // Example of how to use this script with actual file upload
    public void StartRealUpload(System.Action<float> onProgressUpdate, System.Action onComplete, System.Action onError = null)
    {
        StartCoroutine(SimulateRealUpload(onProgressUpdate, onComplete, onError));
    }

    private IEnumerator SimulateRealUpload(System.Action<float> onProgressUpdate, System.Action onComplete, System.Action onError)
    {
        StartUpload();
        
        float progress = 0f;
        while (progress < 100f && currentState == UploadState.Uploading)
        {
            // Simulate upload progress (replace with actual upload logic)
            progress += Random.Range(1f, 5f);
            progress = Mathf.Min(progress, 100f);
            
            // Simulate random error chance (5% chance per update)
            if (Random.Range(0f, 100f) < 5f && progress > 20f)
            {
                TriggerError();
                onError?.Invoke();
                yield break;
            }
            
            SetProgress(progress);
            onProgressUpdate?.Invoke(progress);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        if (currentState == UploadState.Uploading)
        {
            CompleteUpload();
            onComplete?.Invoke();
        }
    }

    // Method to retry upload after error
    public void RetryUpload()
    {
        if (currentState == UploadState.Error)
        {
            ResetProgress();
            StartUpload();
        }
    }
}