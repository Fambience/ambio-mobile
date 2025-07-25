using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

public static class ExplorePostUIBuilder
{
    public static VisualElement CreatePostsContainer(List<PostData> posts, int postsPerRow, Action<PostData, int> onPostClicked)
    {
        VisualElement postsContainer = new VisualElement();
        postsContainer.name = "postsContainer";
        postsContainer.style.marginTop = 20;
        postsContainer.style.flexDirection = FlexDirection.Column;
        postsContainer.style.alignItems = Align.Center;
        postsContainer.style.justifyContent = Justify.FlexStart;
        postsContainer.style.width = Length.Percent(100);
        
        int totalRows = Mathf.CeilToInt((float)posts.Count / postsPerRow);
        
        // Create rows
        for (int row = 0; row < totalRows; row++)
        {
            VisualElement postRow = CreatePostRow(posts, row, postsPerRow, onPostClicked);
            postsContainer.Add(postRow);
        }
        
        return postsContainer;
    }
    
    private static VisualElement CreatePostRow(List<PostData> posts, int rowIndex, int postsPerRow, Action<PostData, int> onPostClicked)
    {
        VisualElement postRow = new VisualElement();
        postRow.name = $"row{rowIndex + 1}";
        postRow.AddToClassList("post-row");
        
        // Ensure proper styling
        postRow.style.flexDirection = FlexDirection.Row;
        postRow.style.justifyContent = Justify.SpaceBetween;
        postRow.style.alignItems = Align.Center;
        postRow.style.width = Length.Percent(100);
        
        // Add margin between rows
        if (rowIndex > 0)
        {
            postRow.style.marginTop = 8;
        }
        
        // Create posts for this row
        for (int postInRow = 0; postInRow < postsPerRow; postInRow++)
        {
            int postIndex = rowIndex * postsPerRow + postInRow;
            
            if (postIndex < posts.Count)
            {
                VisualElement post = CreatePost(posts[postIndex], postIndex, onPostClicked);
                postRow.Add(post);
            }
        }
        
        return postRow;
    }
    
    private static VisualElement CreatePost(PostData postData, int index, Action<PostData, int> onPostClicked)
    {
        VisualElement post = new VisualElement();
        post.name = $"post_{index}";
        post.AddToClassList("post");
        
        // Apply post styling
        ApplyPostStyling(post);
        
        // Add content (image or placeholder)
        AddPostContent(post, postData, index);
        
        // Add click event
        post.RegisterCallback<ClickEvent>(evt => onPostClicked?.Invoke(postData, index));
        
        return post;
    }
    
    private static void ApplyPostStyling(VisualElement post)
    {
        post.style.width = 320;
        post.style.height = 320;
        post.style.borderBottomWidth = 2;
        post.style.borderTopWidth = 2;
        post.style.borderLeftWidth = 2;
        post.style.borderRightWidth = 2;
        
        Color borderColor = new Color(129f/255f, 129f/255f, 129f/255f, 0.84f);
        post.style.borderBottomColor = borderColor;
        post.style.borderTopColor = borderColor;
        post.style.borderLeftColor = borderColor;
        post.style.borderRightColor = borderColor;
        
        post.style.backgroundColor = Color.white;
        post.style.overflow = Overflow.Hidden;
    }
    
    private static void AddPostContent(VisualElement post, PostData postData, int index)
    {
        if (postData.image != null)
        {
            // Create image element
            Image postImage = new Image();
            postImage.image = postData.image;
            postImage.scaleMode = ScaleMode.ScaleAndCrop;
            postImage.style.width = Length.Percent(100);
            postImage.style.height = Length.Percent(100);
            post.Add(postImage);
        }
        else
        {
            // Create a placeholder for posts without images
            post.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            
            // Add placeholder text
            Label placeholderText = new Label($"Post {index + 1}");
            placeholderText.style.position = Position.Absolute;
            placeholderText.style.alignSelf = Align.Center;
            placeholderText.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.style.fontSize = 16;
            placeholderText.style.top = Length.Percent(50);
            placeholderText.style.left = Length.Percent(50);
            placeholderText.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            
            post.Add(placeholderText);
        }
    }
}