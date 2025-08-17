using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

public class UploadService : MonoBehaviour
{
    private static UploadService instance;
    
    public static UploadService Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("UploadService");
                instance = go.AddComponent<UploadService>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [System.Serializable]
    public class PostUploadResponse
    {
        public bool success;
        public string message;
        public string data;
        public string[] uploadResults;
    }

    public class UploadData
    {
        public string description;
        public string roomType;
        public string designStyle;
        public List<string> tags;
        public List<CreatePostMediaHandler.MediaItem> mediaItems;
        public string baseURL;
        public string authToken;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void StartUpload(UploadData uploadData, Action<float> onProgress, Action<bool, string> onComplete)
    {
        Debug.Log("UploadService: StartUpload called");
        StartCoroutine(UploadPostCoroutine(uploadData, onProgress, onComplete));
    }

    private IEnumerator UploadPostCoroutine(UploadData uploadData, Action<float> onProgress, Action<bool, string> onComplete)
    {
        Debug.Log("UploadService: Starting upload coroutine");
        
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("description", uploadData.description));
        formData.Add(new MultipartFormDataSection("roomType", uploadData.roomType));
        formData.Add(new MultipartFormDataSection("designStyle", uploadData.designStyle));
        
        string tagsJson = "[" + string.Join(",", uploadData.tags.Select(tag => $"\"{tag}\"")) + "]";
        formData.Add(new MultipartFormDataSection("hashtags", tagsJson));
        
        foreach (var mediaItem in uploadData.mediaItems)
        {
            if (File.Exists(mediaItem.filePath))
            {
                byte[] fileData = File.ReadAllBytes(mediaItem.filePath);
                formData.Add(new MultipartFormFileSection("media", fileData, mediaItem.fileName, 
                    GetMimeType(mediaItem.filePath)));
                Debug.Log($"UploadService: Added media file {mediaItem.fileName}");
            }
            else
            {
                Debug.LogError($"UploadService: File does not exist: {mediaItem.filePath}");
            }
        }
        
        string createPostUrl = $"{uploadData.baseURL}/api/v1/post/create-post";
        Debug.Log($"UploadService: Uploading to {createPostUrl}");
        
        using (UnityWebRequest www = UnityWebRequest.Post(createPostUrl, formData))
        {
            www.SetRequestHeader("Authorization", uploadData.authToken);
            
            // Start the request
            var asyncOperation = www.SendWebRequest();
            
            // Enhanced progress simulation with better progress reporting
            float simulatedProgress = 0f;
            float progressSpeed = UnityEngine.Random.Range(15f, 25f); // Random speed between 15-25% per second
            
            while (!asyncOperation.isDone)
            {
                // More realistic progress simulation
                float progressIncrement = progressSpeed * Time.deltaTime;
                simulatedProgress += progressIncrement;
                
                // Slow down as we approach completion, cap at 95% until actual completion
                if (simulatedProgress > 95f)
                {
                    simulatedProgress = 95f;
                    progressSpeed *= 0.5f; // Slow down near completion
                }
                
                // Call progress callback
                Debug.Log($"UploadService: Progress update - {simulatedProgress:F1}%");
                onProgress?.Invoke(simulatedProgress);
                
                yield return null;
            }
            
            // Report completion progress
            Debug.Log("UploadService: Upload request completed, processing response...");
            onProgress?.Invoke(98f); // Almost done, processing response
            
            yield return new WaitForSeconds(0.5f); // Brief pause for visual feedback
            
            // Now check the actual result
            bool uploadSuccess = false;
            string resultMessage = "";
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"UploadService: Upload successful! Response: {www.downloadHandler.text}");
                
                try
                {
                    var response = JsonUtility.FromJson<PostUploadResponse>(www.downloadHandler.text);
                    
                    if (response.success)
                    {
                        uploadSuccess = true;
                        resultMessage = "Post uploaded successfully!";
                        onProgress?.Invoke(100f); // Final progress update
                    }
                    else
                    {
                        uploadSuccess = false;
                        resultMessage = response.message;
                        Debug.LogError($"UploadService: API returned error: {response.message}");
                    }
                }
                catch (System.Exception e)
                {
                    uploadSuccess = false;
                    resultMessage = $"Parse error: {e.Message}";
                    Debug.LogError($"UploadService: Parse error: {e.Message}");
                }
            }
            else
            {
                uploadSuccess = false;
                resultMessage = $"Network error: {www.error}";
                Debug.LogError($"UploadService: Network error: {www.error}");
            }
            
            // Handle final completion outside of try/catch
            if (uploadSuccess)
            {
                yield return new WaitForSeconds(0.2f); // Brief pause to show 100%
            }
            
            onComplete?.Invoke(uploadSuccess, resultMessage);
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
}