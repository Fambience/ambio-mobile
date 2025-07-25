using UnityEngine;
using System.Collections.Generic;

public static class PostDataGetter
{
    public static List<PostData> GenerateDummyPosts(int totalPosts)
    {
        List<PostData> posts = new List<PostData>();
        
        // Load the minimalist image from Resources
        Texture2D minimalistImage = Resources.Load<Texture2D>("minimalist");
        
        if (minimalistImage == null)
        {
            Debug.LogWarning("Minimalist image not found in Resources folder. Creating posts without images for testing.");
        }
        else
        {
            Debug.Log("Minimalist image loaded successfully");
        }
        
        // Create dummy posts using the minimalist image
        for (int i = 0; i < totalPosts; i++)
        {
            PostData post = new PostData(
                minimalistImage,
                $"designer_{i + 1}",
                $"Minimalist Design {i + 1}"
            );
            
            posts.Add(post);
        }
        
        Debug.Log($"Generated {posts.Count} dummy posts");
        return posts;
    }
}