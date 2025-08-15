using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine.Networking;
using System.IO;

public class CreatePostController : MonoBehaviour
{
    private VisualElement root;
    private CreatePostMediaHandler mediaHandler;
    private CreatePostDropdownHandler dropdownHandler;
    private TextField tagInput;
    private Button addTagButton;
    private VisualElement tagsContainer;
    private Button completeButton;
    private DescriptionTextBox descriptionBox;
    private List<string> addedTags = new List<string>();
    private string baseURL;
    private string authToken;
    private bool isValidationEnabled = true;

    [System.Serializable]
    public class PostUploadResponse
    {
        public bool success;
        public string message;
        public string data;
        public string[] uploadResults;
    }
    
    private void OnEnable()
    {
        baseURL = baseScript.baseURL;
        authToken = AuthTokenManager.GetToken();
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        StartCoroutine(ShowNavigationAfterDelay());   
        
        mediaHandler = GetComponent<CreatePostMediaHandler>() ?? gameObject.AddComponent<CreatePostMediaHandler>();
        dropdownHandler = GetComponent<CreatePostDropdownHandler>() ?? gameObject.AddComponent<CreatePostDropdownHandler>();
        
        mediaHandler.Initialize(root);
        dropdownHandler.Initialize(root);
        
        BindUIElements();
        SetupTags();
        SetupButtons();
        SetupValidation();
    }
    
    private IEnumerator ShowNavigationAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        NavigationManager.ToggleNavigationBar(true);
        NavigationManager.UpdateSelectedIcon(NavScreen.Create);
        yield return new WaitForSeconds(0.1f);
    }
    
    private void BindUIElements()
    {
        tagInput = root.Q<TextField>("tag-input");
        addTagButton = root.Q<Button>("add-tag-button");
        tagsContainer = root.Q<VisualElement>("tags-container");
        completeButton = root.Q<Button>("completeButton");
        descriptionBox = root.Q<DescriptionTextBox>("descriptionBox");
    }

    private void SetupValidation()
    {
        if (mediaHandler != null)
            mediaHandler.OnMediaChanged += ValidateForm;

        if (descriptionBox != null)
            descriptionBox.onValueChanged += (value) => ValidateForm();
        
        if (dropdownHandler != null)
            dropdownHandler.OnSelectionChanged += ValidateForm;
        
        ValidateForm();
    }

    private void ValidateForm()
    {
        if (!isValidationEnabled || completeButton == null) return;

        bool isValid = IsFormValid();
        completeButton.SetEnabled(isValid);
        
        if (isValid)
        {
            completeButton.style.opacity = 1f;
            completeButton.style.backgroundColor = new StyleColor(new Color(90f / 255f, 42f / 255f, 31f / 255f, 1f));
        }
        else
        {
            completeButton.style.opacity = 0.6f;
            completeButton.style.backgroundColor = new StyleColor(Color.gray);
        }
    }

    private bool IsFormValid()
    {
        
        string roomType = dropdownHandler?.GetSelectedRoomType() ?? "";
        Debug.Log($"Room Type: '{roomType}' | Valid: {!string.IsNullOrWhiteSpace(roomType)}");
    
        string designStyle = dropdownHandler?.GetSelectedDesignStyle() ?? "";
        Debug.Log($"Design Style: '{designStyle}' | Valid: {!string.IsNullOrWhiteSpace(designStyle)}");
        
        string description = descriptionBox?.text ?? "";
        if (string.IsNullOrWhiteSpace(description)) return false;
        if (string.IsNullOrWhiteSpace(dropdownHandler?.GetSelectedRoomType())) return false;
        if (string.IsNullOrWhiteSpace(dropdownHandler?.GetSelectedDesignStyle())) return false;
        if (!mediaHandler?.HasValidMedia() ?? true) return false;
        if (addedTags == null || addedTags.Count == 0) return false;
    
        return true;
    }

    private void ShowValidationError()
    {
        string errorMessage = GetValidationErrorMessage();
        ShowErrorMessage(errorMessage);
    }

    private string GetValidationErrorMessage()
    {
        string description = descriptionBox?.text ?? "";
        if (string.IsNullOrWhiteSpace(description)) return "Please add a description";
        if (string.IsNullOrWhiteSpace(dropdownHandler?.GetSelectedRoomType())) return "Please select a room type";
        if (string.IsNullOrWhiteSpace(dropdownHandler?.GetSelectedDesignStyle())) return "Please select a design style";
        if (!mediaHandler?.HasValidMedia() ?? true) return "Please add at least one image or video";
        return "Please fill in all required fields";
    }
    
    private void SetupTags()
    {
        if (addTagButton != null)
            addTagButton.clicked += AddTag;
        
        if (tagInput != null)
        {
            tagInput.RegisterCallback<KeyDownEvent>(evt => 
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    AddTag();
            });
        }
        
        if (tagsContainer != null)
        {
            tagsContainer.style.flexDirection = FlexDirection.Row;
            tagsContainer.style.flexWrap = Wrap.Wrap;
            tagsContainer.style.marginTop = 15;
        }
    }
    
    private void AddTag()
    {
        if (tagInput == null) return;
        
        string tagText = tagInput.value.Trim();
        if (string.IsNullOrEmpty(tagText)) return;
        if (addedTags.Contains(tagText))
        {
            ShowErrorMessage("Tag already exists");
            return;
        }
        
        addedTags.Add(tagText);
        CreateTagElement(tagText);
        tagInput.value = "";
        ValidateForm();
    }
    
    private void CreateTagElement(string tagText)
    {
        if (tagsContainer == null) return;
        
        var tagElement = new VisualElement();
        tagElement.style.flexDirection = FlexDirection.Row;
        tagElement.style.alignItems = Align.Center;
        tagElement.style.backgroundColor = new StyleColor(new Color(0.545f, 0.298f, 0.216f));
        tagElement.style.borderTopLeftRadius = 15;
        tagElement.style.borderTopRightRadius = 15;
        tagElement.style.borderBottomLeftRadius = 15;
        tagElement.style.borderBottomRightRadius = 15;
        tagElement.style.paddingLeft = 15;
        tagElement.style.paddingRight = 10;
        tagElement.style.paddingTop = 8;
        tagElement.style.paddingBottom = 8;
        tagElement.style.marginRight = 10;
        tagElement.style.marginBottom = 10;
        tagElement.style.height = 50;
        
        var tagLabel = new Label(tagText);
        tagLabel.style.color = Color.white;
        tagLabel.style.fontSize = 25;
        tagLabel.style.flexGrow = 1;
        tagLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        
        var removeButton = new Button();
        removeButton.text = "×";
        removeButton.style.backgroundColor = StyleKeyword.None;
        removeButton.style.borderTopWidth = 0;
        removeButton.style.borderBottomWidth = 0;
        removeButton.style.borderLeftWidth = 0;
        removeButton.style.borderRightWidth = 0;
        removeButton.style.color = Color.white;
        removeButton.style.fontSize = 30;
        removeButton.style.width = 30;
        removeButton.style.height = 30;
        removeButton.style.marginLeft = 10;
        removeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        removeButton.style.paddingTop = 0;
        removeButton.style.paddingBottom = 0;
        removeButton.style.paddingLeft = 0;
        removeButton.style.paddingRight = 0;
        
        removeButton.RegisterCallback<MouseEnterEvent>(evt => 
        {
            removeButton.style.backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.2f));
        });
        
        removeButton.RegisterCallback<MouseLeaveEvent>(evt => 
        {
            removeButton.style.backgroundColor = StyleKeyword.None;
        });
        
        removeButton.clicked += () => RemoveTag(tagText, tagElement);
        
        tagElement.Add(tagLabel);
        tagElement.Add(removeButton);
        tagsContainer.Add(tagElement);
    }
    
    private void RemoveTag(string tagText, VisualElement tagElement)
    {
        if (tagsContainer == null) return;
        addedTags.Remove(tagText);
        tagsContainer.Remove(tagElement);
        ValidateForm();
    }
    
    private void SetupButtons()
    {
        if (completeButton != null)
            completeButton.clicked += OnCompleteButtonClicked;
    }
    
    private void OnCompleteButtonClicked()
    {
        if (!IsFormValid())
        {
            ShowValidationError();
            return;
        }

        string description = descriptionBox?.text ?? "";
        if (string.IsNullOrWhiteSpace(description))
        {
            ShowErrorMessage("Please add a description");
            return;
        }
    
        if (string.IsNullOrWhiteSpace(dropdownHandler.GetSelectedRoomType()))
        {
            ShowErrorMessage("Please select a room type");
            return;
        }
    
        if (string.IsNullOrWhiteSpace(dropdownHandler.GetSelectedDesignStyle()))
        {
            ShowErrorMessage("Please select a design style");
            return;
        }
    
        var selectedMedia = mediaHandler.GetSelectedMedia();
        if (selectedMedia.Count == 0)
        {
            ShowErrorMessage("Please add at least one image or video");
            return;
        }
    
        isValidationEnabled = false;
        completeButton.SetEnabled(false);
    
        StartCoroutine(UploadPostCoroutine(description, dropdownHandler.GetSelectedRoomType(), 
            dropdownHandler.GetSelectedDesignStyle(), addedTags, selectedMedia));
    }
    
    private void ShowErrorMessage(string message)
    {
        Debug.LogError(message);
    }
    
    private void ShowSuccessMessage(string message)
    {
        Debug.Log(message);
    }
    
    private IEnumerator UploadPostCoroutine(string description, string roomType, string designStyle, 
        List<string> tags, List<CreatePostMediaHandler.MediaItem> mediaItems)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("description", description));
        formData.Add(new MultipartFormDataSection("roomType", roomType));
        formData.Add(new MultipartFormDataSection("designStyle", designStyle));
        
        string tagsJson = "[" + string.Join(",", tags.Select(tag => $"\"{tag}\"")) + "]";
        formData.Add(new MultipartFormDataSection("hashtags", tagsJson));
        
        foreach (var mediaItem in mediaItems)
        {
            if (File.Exists(mediaItem.filePath))
            {
                byte[] fileData = File.ReadAllBytes(mediaItem.filePath);
                formData.Add(new MultipartFormFileSection("media", fileData, mediaItem.fileName, 
                    GetMimeType(mediaItem.filePath)));
            }
        }
        
        string createPostUrl = $"{baseURL}/api/v1/post/create-post";
        using (UnityWebRequest www = UnityWebRequest.Post(createPostUrl, formData))
        {
            Debug.Log("Himanshu kumar mahto");
            www.SetRequestHeader("Authorization", authToken);
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<PostUploadResponse>(www.downloadHandler.text);
                    OnPostUploadSuccess(response);
                }
                catch
                {
                    OnPostUploadSuccess(null);
                }
            }
            else
            {
                OnPostUploadError(www.error);
            }
        }
    }
    
    private string GetMimeType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        switch (extension)
        {
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".png": return "image/png";
            case ".webp": return "image/webp";
            case ".heic": return "image/heic";
            case ".mp4": return "video/mp4";
            case ".mov": return "video/quicktime";
            default: return "application/octet-stream";
        }
    }
    
    private void OnPostUploadSuccess(PostUploadResponse response)
    {
        isValidationEnabled = true;
        completeButton.SetEnabled(true);
        ClearAllTags();
        ClearAllMedia();
        dropdownHandler?.ResetSelections();
        descriptionBox?.ClearText();
        ShowSuccessMessage("Post uploaded successfully!");
        ValidateForm();
    }

    private void OnPostUploadError(string error)
    {
        isValidationEnabled = true;
        completeButton.SetEnabled(true);
        ShowErrorMessage($"Upload failed: {error}");
        ValidateForm();
    }
    
    public List<string> GetAddedTags()
    {
        return new List<string>(addedTags);
    }
    
    public void ClearAllTags()
    {
        addedTags.Clear();
        tagsContainer?.Clear();
    }

    public string GetSelectedRoomType()
    {
        return dropdownHandler?.GetSelectedRoomType() ?? "";
    }
    
    public string GetSelectedDesignStyle()
    {
        return dropdownHandler?.GetSelectedDesignStyle() ?? "";
    }
    
    public List<CreatePostMediaHandler.MediaItem> GetSelectedMedia()
    {
        return mediaHandler?.GetSelectedMedia() ?? new List<CreatePostMediaHandler.MediaItem>();
    }
    
    public void UpdateRoomTypes(List<string> newRoomTypes)
    {
        dropdownHandler?.UpdateRoomTypes(newRoomTypes);
    }
    
    public void UpdateDesignStyles(List<string> newDesignStyles)
    {
        dropdownHandler?.UpdateDesignStyles(newDesignStyles);
    }
    
    public void ClearAllMedia()
    {
        mediaHandler?.ClearAllMedia();
    }
    
    public void SetMaxMediaItems(int max)
    {
        mediaHandler?.SetMaxMediaItems(max);
    }
    
    public void TriggerValidation()
    {
        ValidateForm();
    }
    
    public bool IsCurrentlyValid()
    {
        return IsFormValid();
    }
    
    private void OnDisable()
    {
        ClearAllTags();
        mediaHandler?.ClearAllMedia();
        CancelInvoke(nameof(ValidateForm));
        
        if (mediaHandler != null)
            mediaHandler.OnMediaChanged -= ValidateForm;
        
        if (dropdownHandler != null)
            dropdownHandler.OnSelectionChanged -= ValidateForm;
    }
    
    private void OnDestroy()
    {
        ClearAllTags();
        mediaHandler?.ClearAllMedia();
        CancelInvoke(nameof(ValidateForm));
        if (mediaHandler != null)
            mediaHandler.OnMediaChanged -= ValidateForm;
        
        if (dropdownHandler != null)
            dropdownHandler.OnSelectionChanged -= ValidateForm;
    }
}