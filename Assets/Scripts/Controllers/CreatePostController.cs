using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class CreatePostController : MonoBehaviour
{
    private VisualElement root;
    
    // Components
    private CreatePostMediaHandler mediaHandler;
    private CreatePostDropdownHandler dropdownHandler;
    
    // Tag elements
    private TextField tagInput;
    private Button addTagButton;
    private VisualElement tagsContainer;
    
    // Other elements
    private Button completeButton;
    
    // Data storage
    private List<string> addedTags = new List<string>();
    
    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        
        // Initialize components
        mediaHandler = GetComponent<CreatePostMediaHandler>();
        if (mediaHandler == null)
            mediaHandler = gameObject.AddComponent<CreatePostMediaHandler>();
            
        dropdownHandler = GetComponent<CreatePostDropdownHandler>();
        if (dropdownHandler == null)
            dropdownHandler = gameObject.AddComponent<CreatePostDropdownHandler>();
        
        // Initialize components
        mediaHandler.Initialize(root);
        dropdownHandler.Initialize(root);
        
        BindUIElements();
        SetupTags();
        SetupButtons();
    }
    
    private void BindUIElements()
    {
        // Tag elements
        tagInput = root.Q<TextField>("tag-input");
        addTagButton = root.Q<Button>("add-tag-button");
        tagsContainer = root.Q<VisualElement>("tags-container");
        
        // Other elements
        completeButton = root.Q<Button>("completeButton");
        
        Debug.Log($"Tag Input Found: {tagInput != null}");
        Debug.Log($"Add Tag Button Found: {addTagButton != null}");
        Debug.Log($"Tags Container Found: {tagsContainer != null}");
        Debug.Log($"Complete Button Found: {completeButton != null}");
    }
    
    #region Tag System
    
    private void SetupTags()
    {
        if (addTagButton != null)
        {
            addTagButton.clicked += AddTag;
        }
        
        if (tagInput != null)
        {
            tagInput.RegisterCallback<KeyDownEvent>(evt => 
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    AddTag();
                }
            });
        }
        
        // Set tags container to horizontal flow
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
        
        if (string.IsNullOrEmpty(tagText))
        {
            Debug.Log("Tag text is empty");
            return;
        }
        
        if (addedTags.Contains(tagText))
        {
            Debug.Log("Tag already exists");
            return;
        }
        
        addedTags.Add(tagText);
        CreateTagElement(tagText);
        tagInput.value = "";
        
        Debug.Log($"Added tag: {tagText}");
    }
    
    private void CreateTagElement(string tagText)
    {
        if (tagsContainer == null) return;
        
        // Create main tag container
        var tagElement = new VisualElement();
        tagElement.style.flexDirection = FlexDirection.Row;
        tagElement.style.alignItems = Align.Center;
        tagElement.style.backgroundColor = new StyleColor(new Color(0.545f, 0.298f, 0.216f)); // #8B4C39
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
        
        // Create tag text label
        var tagLabel = new Label(tagText);
        tagLabel.style.color = Color.white;
        tagLabel.style.fontSize = 25;
        tagLabel.style.flexGrow = 1;
        tagLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        
        // Create remove button (X)
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
        
        // Add hover effect for remove button
        removeButton.RegisterCallback<MouseEnterEvent>(evt => 
        {
            removeButton.style.backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.2f));
        });
        
        removeButton.RegisterCallback<MouseLeaveEvent>(evt => 
        {
            removeButton.style.backgroundColor = StyleKeyword.None;
        });
        
        // Add remove functionality
        removeButton.clicked += () => RemoveTag(tagText, tagElement);
        
        // Assemble the tag element
        tagElement.Add(tagLabel);
        tagElement.Add(removeButton);
        
        // Add to tags container
        tagsContainer.Add(tagElement);
    }
    
    private void RemoveTag(string tagText, VisualElement tagElement)
    {
        if (tagsContainer == null) return;
        
        addedTags.Remove(tagText);
        tagsContainer.Remove(tagElement);
        
        Debug.Log($"Removed tag: {tagText}");
    }
    
    #endregion
    
    #region Button Setup
    
    private void SetupButtons()
    {
        if (completeButton != null)
        {
            completeButton.clicked += OnCompleteButtonClicked;
        }
    }
    
    private void OnCompleteButtonClicked()
    {
        // Collect all form data
        var descriptionBox = root.Q<VisualElement>("descriptionBox");
        string description = ""; // You'll need to get this from your custom DescriptionTextBox component
        
        Debug.Log("=== Post Data ===");
        Debug.Log($"Description: {description}");
        Debug.Log($"Room Type: {dropdownHandler.GetSelectedRoomType()}");
        Debug.Log($"Design Style: {dropdownHandler.GetSelectedDesignStyle()}");
        Debug.Log($"Tags: {string.Join(", ", addedTags)}");
        Debug.Log($"Media Items: {mediaHandler.GetSelectedMedia().Count}");
        
        // Log media information
        var selectedMedia = mediaHandler.GetSelectedMedia();
        for (int i = 0; i < selectedMedia.Count; i++)
        {
            var media = selectedMedia[i];
            Debug.Log($"  Media {i + 1}: {media.fileName} (Video: {media.isVideo})");
        }
        
        // Here you can add your upload logic
        // For example: UploadPost(description, selectedRoomType, selectedDesignStyle, addedTags, selectedMedia);
    }
    
    #endregion
    
    #region Public API Methods
    
    // Public method to get added tags
    public List<string> GetAddedTags()
    {
        return new List<string>(addedTags);
    }
    
    // Public method to clear all tags
    public void ClearAllTags()
    {
        addedTags.Clear();
        if (tagsContainer != null)
        {
            tagsContainer.Clear();
        }
        Debug.Log("All tags cleared");
    }
    
    // Delegate methods to access other components
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
    
    #endregion
    
    #region Cleanup
    
    private void OnDisable()
    {
        // Clean up resources when component is disabled
        ClearAllTags();
        mediaHandler?.ClearAllMedia();
    }
    
    private void OnDestroy()
    {
        // Clean up resources when component is destroyed
        ClearAllTags();
        mediaHandler?.ClearAllMedia();
    }
    
    #endregion
}