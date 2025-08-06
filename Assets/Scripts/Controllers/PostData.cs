using UnityEngine;

[System.Serializable]
public class PostData
{
    public Texture2D image;
    public string designerId;
    public string postTitle;
    
    public PostData(Texture2D img, string designer, string title)
    {
        image = img;
        designerId = designer;
        postTitle = title;
    }
}