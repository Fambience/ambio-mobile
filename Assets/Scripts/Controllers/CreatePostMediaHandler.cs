using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System;

public class CreatePostMediaHandler : MonoBehaviour
{
    private VisualElement root;
    
    // Media upload elements
    private VisualElement uploadImageIcon;
    private VisualElement mediaPreviewContainer;
    private Label warningImageText;
    
    // Media storage
    private List<MediaItem> selectedMedia = new List<MediaItem>();
    private int maxMediaItems = 10; // Set your limit

    // Events for validation
    public event Action OnMediaChanged;

    // Media item class to store media data
    [System.Serializable]
    public class MediaItem
    {
        public string filePath;
        public Texture2D texture;
        public bool isVideo;
        public string fileName;
        
        public MediaItem(string path, Texture2D tex, bool video, string name)
        {
            filePath = path;
            texture = tex;
            isVideo = video;
            fileName = name;
        }
    }

    public void Initialize(VisualElement rootElement)
    {
        root = rootElement;
        BindUIElements();
        SetupMediaUpload();
        HideWarningText(); // Initially hide warning
    }
    
    private void BindUIElements()
    {
        // Media upload elements
        uploadImageIcon = root.Q<VisualElement>("UploadImageIcon");
        mediaPreviewContainer = root.Q<VisualElement>("media-preview-container");
        warningImageText = root.Q<Label>("warningImageText");
        
        // Debug: Check if elements are found
        Debug.Log($"Upload Icon Found: {uploadImageIcon != null}");
        Debug.Log($"Media Preview Container Found: {mediaPreviewContainer != null}");
        Debug.Log($"Warning Text Found: {warningImageText != null}");
    }
    
    #region Warning Text Management
    
    private void ShowWarningText(string message)
    {
        if (warningImageText != null)
        {
            warningImageText.text = message;
            warningImageText.style.display = DisplayStyle.Flex;
            warningImageText.style.color = new StyleColor(Color.red);
            
            // Auto-hide after 5 seconds
            StartCoroutine(HideWarningAfterDelay(5f));
        }
    }
    
    private void HideWarningText()
    {
        if (warningImageText != null)
        {
            warningImageText.style.display = DisplayStyle.None;
        }
    }
    
    private IEnumerator HideWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideWarningText();
    }
    
    #endregion
    
    #region Media Upload Setup
    
    private void SetupMediaUpload()
    {
        if (uploadImageIcon != null)
        {
            // Fix uploadImageIcon size to prevent shrinking
            uploadImageIcon.style.width = 350; // Set fixed width
            uploadImageIcon.style.height = 300; // Set fixed height
            uploadImageIcon.style.flexShrink = 0; // Prevent shrinking
            uploadImageIcon.style.flexGrow = 0; // Prevent growing
            
            uploadImageIcon.RegisterCallback<ClickEvent>(_ => ShowMediaSelectionOptions());
        }
        
        // Set up media preview container for horizontal scrolling
        if (mediaPreviewContainer != null)
        {
            // Create a ScrollView for horizontal scrolling only
            var horizontalScrollView = new ScrollView();
            
            // Configure ScrollView for horizontal only scrolling
            horizontalScrollView.mode = ScrollViewMode.Horizontal;
            horizontalScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden; // Hide horizontal scrollbar
            horizontalScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden; // Hide vertical scrollbar
            
            horizontalScrollView.style.flexDirection = FlexDirection.Row;
            horizontalScrollView.style.height = 400; // Fixed height for the scroll area
            horizontalScrollView.style.width = Length.Percent(100);
            horizontalScrollView.style.overflow = Overflow.Hidden;
            
            // Disable vertical scrolling completely
            horizontalScrollView.verticalPageSize = 0;
            horizontalScrollView.scrollOffset = new Vector2(horizontalScrollView.scrollOffset.x, 0);
            
            // Create the content container inside ScrollView
            var contentContainer = new VisualElement();
            contentContainer.style.flexDirection = FlexDirection.Row;
            contentContainer.style.alignItems = Align.FlexStart;
            contentContainer.style.height = Length.Percent(100);
            contentContainer.style.flexShrink = 0;
            
            // Clear existing content and add the new structure
            mediaPreviewContainer.Clear();
            horizontalScrollView.Add(contentContainer);
            mediaPreviewContainer.Add(horizontalScrollView);
            
            // Store reference to the content container for adding previews
            mediaPreviewContainer.userData = contentContainer;
            
            // Set the container styling
            mediaPreviewContainer.style.width = Length.Percent(100);
            mediaPreviewContainer.style.overflow = Overflow.Hidden;
            mediaPreviewContainer.style.flexDirection = FlexDirection.Column; // Keep main container vertical
        }
    }
    
    private void ShowMediaSelectionOptions()
    {
        if (selectedMedia.Count >= maxMediaItems)
        {
            ShowWarningText($"Maximum media items ({maxMediaItems}) reached!");
            Debug.Log($"Maximum media items ({maxMediaItems}) reached!");
            return;
        }
        
        // Hide any existing warning before opening picker
        HideWarningText();
        
        // Show options: Camera or Gallery
        ShowMediaPickerDialog();
    }
    
    private void ShowMediaPickerDialog()
    {
        // Create a simple dialog to choose between Camera and Gallery
        // For now, we'll directly open gallery. You can enhance this with a custom dialog
        
#if UNITY_ANDROID || UNITY_IOS
        // Request permission first
        if (!NativeGallery.IsMediaPickerBusy())
        {
            RequestPermissionAndPickMedia();
        }
        else
        {
            ShowWarningText("Media picker is busy! Please try again.");
            Debug.Log("Media picker is busy!");
        }
#else
        ShowWarningText("Media picker only works on Android/iOS devices.");
        Debug.Log("Media picker only works on Android/iOS");
#endif
    }
    
    private void RequestPermissionAndPickMedia()
    {
#if UNITY_ANDROID || UNITY_IOS
        // Check permission first
        bool hasPermission = NativeGallery.CheckPermission(NativeGallery.PermissionType.Read, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video);
        
        Debug.Log($"Gallery permission status: {hasPermission}");
        
        if (hasPermission)
        {
            // Permission already granted, pick media
            PickMediaFromGallery();
        }
        else
        {
            // Request permission asynchronously
            Debug.Log("Requesting gallery permission...");
            NativeGallery.RequestPermissionAsync((permission) =>
            {
                Debug.Log($"Permission result: {permission}");
                if (permission == NativeGallery.Permission.Granted)
                {
                    PickMediaFromGallery();
                }
                else
                {
                    ShowWarningText("Gallery permission denied! Please enable it in device settings.");
                    Debug.Log("Gallery permission denied!");
                    if (permission == NativeGallery.Permission.Denied)
                    {
                        // User can manually grant permission from settings
                        Debug.Log("User needs to grant permission from Settings");
                    }
                }
            }, NativeGallery.PermissionType.Read, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video);
        }
#endif
    }
    
    private void PickMediaFromGallery()
    {
#if UNITY_ANDROID || UNITY_IOS
        // For more precise control, you could use GetImageFromGallery or GetVideoFromGallery separately
        // But GetMixedMediaFromGallery is more convenient for user experience
        
        NativeGallery.GetMixedMediaFromGallery(
            callback: OnMediaSelected,
            mediaTypes: NativeGallery.MediaType.Image | NativeGallery.MediaType.Video,
            title: "Select Image or Video"
        );
        
        // Note: NativeGallery doesn't provide fine-grained format filtering at system level
        // We handle format validation after selection in OnMediaSelected method
        Debug.Log("Opening gallery for mixed media selection...");
        Debug.Log("Supported formats - Images: .jpg, .jpeg, .png, .webp, .heic | Videos: .mp4, .mov");
#endif
    }
    
    private void OnMediaSelected(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Media selection cancelled or failed");
            return;
        }
        
        Debug.Log($"Media selected: {path}");
        
        // Verify file exists
        if (!File.Exists(path))
        {
            ShowWarningText("Selected file does not exist!");
            Debug.LogError($"Selected file does not exist: {path}");
            return;
        }
        
        // Get file info for debugging
        var fileInfo = new FileInfo(path);
        Debug.Log($"File size: {fileInfo.Length} bytes");
        Debug.Log($"File extension: {fileInfo.Extension}");
        
        // Enhanced security validation - checks both extension and file content
        if (!IsSecureValidMediaFile(path))
        {
            string extension = Path.GetExtension(path).ToLower();
            string fileName = Path.GetFileName(path);
            
            Debug.LogWarning($"Invalid or potentially malicious file: {fileName}");
            Debug.LogWarning($"Extension: {extension}");
            Debug.LogWarning("File content doesn't match the extension or format is unsupported");
            
            ShowSecurityWarningMessage(fileName, extension);
            return;
        }
        
        // Check if it's a video or image (after security validation)
        bool isVideo = IsVideoFile(path);
        Debug.Log($"Is video: {isVideo}");
        Debug.Log("File passed security validation ✅");
        
        // Hide warning on successful validation
        HideWarningText();
        
        if (isVideo)
        {
            ProcessVideoFile(path);
        }
        else
        {
            ProcessImageFile(path);
        }
    }
    
    private void ShowSecurityWarningMessage(string fileName, string extension)
    {
        Debug.LogError($"SECURITY WARNING: File rejected - {fileName}");
        Debug.LogError($"Reason: File content doesn't match extension '{extension}' or contains invalid data");
        Debug.Log("This could be:");
        Debug.Log("1. A file with spoofed extension (e.g., malware.exe.png)");
        Debug.Log("2. A corrupted file");
        Debug.Log("3. An unsupported format");
        Debug.Log("");
        Debug.Log("Supported formats:");
        Debug.Log("Images: .jpg, .jpeg, .png, .webp, .heic (with valid content)");
        Debug.Log("Videos: .mp4, .mov (with valid content)");
        
        // Show warning to user
        ShowWarningText($"Invalid file format! Supported: JPG, PNG, WEBP, HEIC, MP4, MOV");
    }
    
    private void ShowUnsupportedFormatMessage(string extension)
    {
        Debug.LogError($"Unsupported file format: {extension}");
        Debug.Log("Supported formats:");
        Debug.Log("Images: .jpg, .jpeg, .png, .webp, .heic");
        Debug.Log("Videos: .mp4, .mov");
        
        // Show warning to user
        ShowWarningText($"Unsupported format '{extension}'! Use JPG, PNG, WEBP, HEIC, MP4, MOV");
    }
    
    private bool IsImageFile(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || 
               extension == ".webp" || extension == ".heic";
    }
    
    private bool IsVideoFile(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        return extension == ".mp4" || extension == ".mov";
    }
    
    private bool IsValidMediaFile(string path)
    {
        return IsImageFile(path) || IsVideoFile(path);
    }
    
    // Enhanced validation that checks both extension and file content
    private bool IsValidImageContent(string path)
    {
        try
        {
            // Read first few bytes to check file signature (magic numbers)
            byte[] headerBytes = new byte[12];
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                int bytesRead = fs.Read(headerBytes, 0, headerBytes.Length);
                if (bytesRead < 4) return false;
            }
            
            // Check magic numbers for different image formats
            // JPEG: FF D8 FF
            if (headerBytes[0] == 0xFF && headerBytes[1] == 0xD8 && headerBytes[2] == 0xFF)
            {
                return IsImageFile(path) && (path.ToLower().EndsWith(".jpg") || path.ToLower().EndsWith(".jpeg"));
            }
            
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (headerBytes[0] == 0x89 && headerBytes[1] == 0x50 && headerBytes[2] == 0x4E && headerBytes[3] == 0x47 &&
                headerBytes[4] == 0x0D && headerBytes[5] == 0x0A && headerBytes[6] == 0x1A && headerBytes[7] == 0x0A)
            {
                return IsImageFile(path) && path.ToLower().EndsWith(".png");
            }
            
            // WebP: "RIFF" followed by file size, then "WEBP"
            if (headerBytes[0] == 0x52 && headerBytes[1] == 0x49 && headerBytes[2] == 0x46 && headerBytes[3] == 0x46 &&
                headerBytes[8] == 0x57 && headerBytes[9] == 0x45 && headerBytes[10] == 0x42 && headerBytes[11] == 0x50)
            {
                return IsImageFile(path) && path.ToLower().EndsWith(".webp");
            }
            
            // HEIC: More complex, check for "ftyp" at offset 4 and HEIC brand
            if (headerBytes[4] == 0x66 && headerBytes[5] == 0x74 && headerBytes[6] == 0x79 && headerBytes[7] == 0x70)
            {
                return IsImageFile(path) && path.ToLower().EndsWith(".heic");
            }
            
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error validating image content: {e.Message}");
            return false;
        }
    }
    
    private bool IsValidVideoContent(string path)
    {
        try
        {
            // Read first few bytes to check file signature
            byte[] headerBytes = new byte[12];
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                int bytesRead = fs.Read(headerBytes, 0, headerBytes.Length);
                if (bytesRead < 8) return false;
            }
            
            // MP4: Check for "ftyp" signature at offset 4
            if (headerBytes[4] == 0x66 && headerBytes[5] == 0x74 && headerBytes[6] == 0x79 && headerBytes[7] == 0x70)
            {
                return IsVideoFile(path) && path.ToLower().EndsWith(".mp4");
            }
            
            // MOV: Also uses "ftyp" but different brand, or check for "moov" atom
            // MOV files can have various signatures, this is a basic check
            if ((headerBytes[4] == 0x66 && headerBytes[5] == 0x74 && headerBytes[6] == 0x79 && headerBytes[7] == 0x70) ||
                (headerBytes[4] == 0x6D && headerBytes[5] == 0x6F && headerBytes[6] == 0x6F && headerBytes[7] == 0x76))
            {
                return IsVideoFile(path) && path.ToLower().EndsWith(".mov");
            }
            
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error validating video content: {e.Message}");
            return false;
        }
    }
    
    private bool IsSecureValidMediaFile(string path)
    {
        // First check extension (fast check)
        if (!IsValidMediaFile(path))
        {
            return false;
        }
        
        // Then check file content (security check)
        if (IsImageFile(path))
        {
            return IsValidImageContent(path);
        }
        else if (IsVideoFile(path))
        {
            return IsValidVideoContent(path);
        }
        
        return false;
    }
    
    private void ProcessImageFile(string path)
    {
        StartCoroutine(LoadImageCoroutine(path));
    }
    
    private void ProcessVideoFile(string path)
    {
        // For video, we'll create a thumbnail
        StartCoroutine(LoadVideoThumbnailCoroutine(path));
    }
    
    private IEnumerator LoadImageCoroutine(string path)
    {
        byte[] imageData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        
        if (texture.LoadImage(imageData))
        {
            string fileName = Path.GetFileName(path);
            MediaItem mediaItem = new MediaItem(path, texture, false, fileName);
            selectedMedia.Add(mediaItem);
            
            // Update UI on main thread
            CreateMediaPreview(mediaItem);
            
            // Trigger validation update
            OnMediaChanged?.Invoke();
            
            Debug.Log($"Image loaded: {fileName}");
        }
        else
        {
            ShowWarningText("Failed to load image. File may be corrupted.");
            Debug.LogError("Failed to load image");
            DestroyImmediate(texture);
        }
        
        yield return null;
    }
    
    private IEnumerator LoadVideoThumbnailCoroutine(string path)
    {
        string fileName = Path.GetFileName(path);
        
#if UNITY_ANDROID || UNITY_IOS
        // Try to get video thumbnail using NativeGallery
        Texture2D thumbnailTexture = null;
        
        try
        {
            // First try to get thumbnail with larger size for better quality
            thumbnailTexture = NativeGallery.GetVideoThumbnail(path, maxSize: 512);
            
            if (thumbnailTexture == null)
            {
                // If that fails, try with smaller size
                thumbnailTexture = NativeGallery.GetVideoThumbnail(path, maxSize: 256);
            }
            
            if (thumbnailTexture == null)
            {
                // Try once more with default size
                thumbnailTexture = NativeGallery.GetVideoThumbnail(path);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error getting video thumbnail: {e.Message}");
            thumbnailTexture = null;
        }
        
        if (thumbnailTexture != null && thumbnailTexture.width > 1 && thumbnailTexture.height > 1)
        {
            MediaItem mediaItem = new MediaItem(path, thumbnailTexture, true, fileName);
            selectedMedia.Add(mediaItem);
            CreateMediaPreview(mediaItem);
            
            // Trigger validation update
            OnMediaChanged?.Invoke();
            
            Debug.Log($"Video thumbnail loaded successfully: {fileName} ({thumbnailTexture.width}x{thumbnailTexture.height})");
        }
        else
        {
            Debug.LogWarning($"Failed to generate thumbnail for video: {fileName}. Creating enhanced placeholder.");
            CreateEnhancedVideoPlaceholder(path, fileName);
        }
#else
        // In editor, create enhanced placeholder
        Debug.Log($"Video thumbnail generation not available in editor. Creating placeholder for: {fileName}");
        CreateEnhancedVideoPlaceholder(path, fileName);
#endif
        
        yield return null;
    }
    
    private void CreateEnhancedVideoPlaceholder(string path, string fileName)
    {
        // Create a more informative placeholder for video with video icon overlay
        Texture2D placeholderTexture = new Texture2D(256, 256);
        Color[] colors = new Color[256 * 256];
        
        // Create a gradient background instead of solid gray
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                int index = y * 256 + x;
                float gradient = (float)y / 256f;
                // Dark blue to lighter blue gradient
                colors[index] = new Color(0.2f + gradient * 0.3f, 0.3f + gradient * 0.2f, 0.5f + gradient * 0.3f, 1f);
            }
        }
        
        // Add a film strip pattern at the top and bottom
        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                int topIndex = y * 256 + x;
                int bottomIndex = (255 - y) * 256 + x;
                
                // Create film strip holes pattern
                if (x % 40 < 20)
                {
                    colors[topIndex] = Color.black;
                    colors[bottomIndex] = Color.black;
                }
                else
                {
                    colors[topIndex] = new Color(0.1f, 0.1f, 0.1f, 1f);
                    colors[bottomIndex] = new Color(0.1f, 0.1f, 0.1f, 1f);
                }
            }
        }
        
        placeholderTexture.SetPixels(colors);
        placeholderTexture.Apply();
        
        MediaItem mediaItem = new MediaItem(path, placeholderTexture, true, fileName);
        selectedMedia.Add(mediaItem);
        CreateMediaPreview(mediaItem);
        
        // Trigger validation update
        OnMediaChanged?.Invoke();
        
        Debug.Log($"Enhanced video placeholder created: {fileName}");
    }
    
    private void CreateMediaPreview(MediaItem mediaItem)
    {
        if (mediaPreviewContainer == null) return;
        
        // Get the content container from userData
        var contentContainer = mediaPreviewContainer.userData as VisualElement;
        if (contentContainer == null)
        {
            // Fallback: use mediaPreviewContainer directly if userData is not set
            contentContainer = mediaPreviewContainer;
        }
        
        // Create preview container
        var previewElement = new VisualElement();
        previewElement.AddToClassList("media-preview");
        previewElement.style.position = Position.Relative;
        previewElement.style.width = 350;
        previewElement.style.height = 300;
        previewElement.style.marginLeft = 30;
        previewElement.style.marginTop = 50;
        previewElement.style.marginLeft = 5; // Add some left margin for first item
        previewElement.style.flexShrink = 0; // Prevent shrinking in horizontal layout
        previewElement.style.flexGrow = 0; // Prevent growing
        previewElement.style.borderTopLeftRadius = 8;
        previewElement.style.borderTopRightRadius = 8;
        previewElement.style.borderBottomLeftRadius = 8;
        previewElement.style.borderBottomRightRadius = 8;
        previewElement.style.overflow = Overflow.Hidden;
        previewElement.style.backgroundColor = new StyleColor(Color.gray);
        
        // Create image element
        var imageElement = new Image();
        imageElement.image = mediaItem.texture;
        imageElement.scaleMode = ScaleMode.ScaleAndCrop;
        imageElement.style.width = Length.Percent(100);
        imageElement.style.height = Length.Percent(100);
        imageElement.style.flexShrink = 0; // Prevent image from shrinking
        
        // Create remove button
        var removeButton = new Button();
        removeButton.text = "×";
        removeButton.AddToClassList("media-remove-button");
        removeButton.style.position = Position.Absolute;
        removeButton.style.top = 5;
        removeButton.style.right = 5;
        removeButton.style.width = 40;
        removeButton.style.height = 40;
        removeButton.style.marginTop = 0;
        removeButton.style.marginLeft = 0;
        removeButton.style.alignItems = Align.Center;
        removeButton.style.justifyContent = Justify.Center;
        removeButton.style.borderTopLeftRadius = 20;
        removeButton.style.borderTopRightRadius = 20;
        removeButton.style.borderBottomLeftRadius = 20;
        removeButton.style.borderBottomRightRadius = 20;
        removeButton.style.backgroundColor = new StyleColor(new Color(1f, 0f, 0f, 0.8f));
        removeButton.style.color = Color.white;
        removeButton.style.fontSize = 40;
        removeButton.style.borderTopWidth = 0;
        removeButton.style.borderBottomWidth = 0;
        removeButton.style.borderLeftWidth = 0;
        removeButton.style.borderRightWidth = 0;
        removeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        removeButton.style.paddingTop = 0;
        removeButton.style.paddingBottom = 0;
        removeButton.style.paddingLeft = 0;
        removeButton.style.paddingRight = 0;
        removeButton.style.flexShrink = 0; // Prevent button from shrinking
        
        // Add hover effect
        removeButton.RegisterCallback<MouseEnterEvent>(_ => 
        {
            removeButton.style.backgroundColor = new StyleColor(new Color(1f, 0f, 0f, 1f));
        });
        
        removeButton.RegisterCallback<MouseLeaveEvent>(_ => 
        {
            removeButton.style.backgroundColor = new StyleColor(new Color(1f, 0f, 0f, 0.8f));
        });
        
        // Add remove functionality
        removeButton.clicked += () => RemoveMediaItem(mediaItem, previewElement);
        
        // Add video indicator if it's a video
        if (mediaItem.isVideo)
        {
            // Create a more prominent video indicator container
            var videoIndicatorContainer = new VisualElement();
            videoIndicatorContainer.style.position = Position.Absolute;
            videoIndicatorContainer.style.width = Length.Percent(100);
            videoIndicatorContainer.style.height = Length.Percent(100);
            videoIndicatorContainer.style.alignItems = Align.Center;
            videoIndicatorContainer.style.justifyContent = Justify.Center;
            videoIndicatorContainer.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.3f));
            
            // Large play button in center
            var playButton = new VisualElement();
            playButton.style.width = 60;
            playButton.style.height = 60;
            playButton.style.backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.9f));
            playButton.style.borderTopLeftRadius = 30;
            playButton.style.borderTopRightRadius = 30;
            playButton.style.borderBottomLeftRadius = 30;
            playButton.style.borderBottomRightRadius = 30;
            playButton.style.alignItems = Align.Center;
            playButton.style.justifyContent = Justify.Center;
            
            // Play triangle icon
            var playIcon = new Label("▶");
            playIcon.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            playIcon.style.fontSize = 24;
            playIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
            playIcon.style.marginLeft = 3; // Slight offset to center the triangle
            
            playButton.Add(playIcon);
            videoIndicatorContainer.Add(playButton);
            
            // Small video label in bottom left
            var videoLabel = new Label("VIDEO");
            videoLabel.style.position = Position.Absolute;
            videoLabel.style.bottom = 8;
            videoLabel.style.left = 8;
            videoLabel.style.color = Color.white;
            videoLabel.style.fontSize = 12;
            videoLabel.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.7f));
            videoLabel.style.paddingLeft = 6;
            videoLabel.style.paddingRight = 6;
            videoLabel.style.paddingTop = 2;
            videoLabel.style.paddingBottom = 2;
            videoLabel.style.borderTopLeftRadius = 3;
            videoLabel.style.borderTopRightRadius = 3;
            videoLabel.style.borderBottomLeftRadius = 3;
            videoLabel.style.borderBottomRightRadius = 3;
            
            videoIndicatorContainer.Add(videoLabel);
            previewElement.Add(videoIndicatorContainer);
        }
        
        // Assemble preview element
        previewElement.Add(imageElement);
        previewElement.Add(removeButton);
        
        // Add to horizontal content container
        contentContainer.Add(previewElement);
        
        Debug.Log($"Media preview created for: {mediaItem.fileName}");
    }

    
    private void RemoveMediaItem(MediaItem mediaItem, VisualElement previewElement)
    {
        // Remove from data
        selectedMedia.Remove(mediaItem);
    
        // Clean up texture
        if (mediaItem.texture != null)
        {
            DestroyImmediate(mediaItem.texture);
        }
    
        // Remove from UI
        if (mediaPreviewContainer != null && previewElement != null)
        {
            var contentContainer = mediaPreviewContainer.userData as VisualElement;
            if (contentContainer != null)
            {
                contentContainer.Remove(previewElement);
            }
            else
            {
                // Fallback
                mediaPreviewContainer.Remove(previewElement);
            }
        }

        // Trigger validation update
        OnMediaChanged?.Invoke();
    
        Debug.Log($"Removed media item: {mediaItem.fileName}");
    }
    
    #endregion
    
    #region Public API Methods
    
    // Public method to get selected media
    public List<MediaItem> GetSelectedMedia()
    {
        return new List<MediaItem>(selectedMedia);
    }
    
    // Public method to check if media is valid/present
    public bool HasValidMedia()
    {
        return selectedMedia.Count > 0;
    }
    
    // Public method to get supported image formats
    public static string[] GetSupportedImageFormats()
    {
        return new string[] { ".jpg", ".jpeg", ".png", ".webp", ".heic" };
    }
    
    // Public method to get supported video formats
    public static string[] GetSupportedVideoFormats()
    {
        return new string[] { ".mp4", ".mov" };
    }
    
    // Public method to get all supported formats
    public static string[] GetAllSupportedFormats()
    {
        var imageFormats = GetSupportedImageFormats();
        var videoFormats = GetSupportedVideoFormats();
        var allFormats = new string[imageFormats.Length + videoFormats.Length];
        imageFormats.CopyTo(allFormats, 0);
        videoFormats.CopyTo(allFormats, imageFormats.Length);
        return allFormats;
    }
    
    // Public method to check if a file format is supported
    public static bool IsFormatSupported(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        var supportedFormats = GetAllSupportedFormats();
        return System.Array.Exists(supportedFormats, format => format == extension);
    }
    
    // Public method to clear all media
    public void ClearAllMedia()
    {
        // Clean up textures
        foreach (var media in selectedMedia)
        {
            if (media.texture != null)
            {
                DestroyImmediate(media.texture);
            }
        }
    
        selectedMedia.Clear();
    
        // Clear UI
        if (mediaPreviewContainer != null)
        {
            var contentContainer = mediaPreviewContainer.userData as VisualElement;
            if (contentContainer != null)
            {
                contentContainer.Clear();
            }
            else
            {
                // Fallback
                mediaPreviewContainer.Clear();
                // Re-setup the horizontal structure
                SetupMediaUpload();
            }
        }

        // Hide warning text when clearing
        HideWarningText();

        // Trigger validation update
        OnMediaChanged?.Invoke();
    
        Debug.Log("All media cleared");
    }
    
    // Public method to set maximum media items
    public void SetMaxMediaItems(int max)
    {
        maxMediaItems = max;
        Debug.Log($"Max media items set to: {max}");
    }
    
    // Public method to get current media count
    public int GetMediaCount()
    {
        return selectedMedia.Count;
    }
    
    // Public method to check if max limit reached
    public bool IsMaxLimitReached()
    {
        return selectedMedia.Count >= maxMediaItems;
    }
    
    #endregion
}