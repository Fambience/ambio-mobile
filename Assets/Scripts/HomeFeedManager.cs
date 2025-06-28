// using UnityEngine;
// using UnityEngine.Networking;
// using System.Collections;
//
// [System.Serializable]
// public class HomeApiResponse
// {
//     public bool success;
//     public string message;
//     public HomeData data;
// }
//
// [System.Serializable]
// public class HomeData
// {
//     public string type; // "USERS" or "POSTS"
//     public UserData[] creators;
//     public PostData[] posts;
//     public Pagination pagination;
// }
//
// [System.Serializable]
// public class UserData
// {
//     public string userId;
//     public string userName;
//     public string firstName;
//     public string lastName;
//     public string role;
//     public string email;
//     public string gender;
// }
//
// [System.Serializable]
// public class PostData
// {
//     public Post post;
//     public PostUser user;
// }
//
// [System.Serializable]
// public class Post
// {
//     public string postId;
//     public string caption;
//     public string description;
//     public string createdAt;
//     public string userId;
//     public PostMedia[] postMedia;
// }
//
// [System.Serializable]
// public class PostUser
// {
//     public string userId;
//     public string userName;
//     public string role;
// }
//
// [System.Serializable]
// public class PostMedia
// {
//     public string postMediaId;
//     public string filePath;
// }
//
// [System.Serializable]
// public class Pagination
// {
//     public int page;
//     public int limit;
// }
//
// public class HomeFeedManager : MonoBehaviour
// {
//     [Header("Scroll Sections")]
//     public GameObject userScroll;         // Scroll view for user cards
//     public GameObject postScroll;         // Scroll view for post cards
//
//     [Header("Pagination Components")]
//     public UserScrollPagination userPagination;   // Script for user pagination
//     public PostScrollPagination postPagination;   // Script for post pagination
//
//     [Header("Config")]
//     public int testLimit = 15;            // Number of items per page
//     private int followedCount = 0;
//     
//     public void LoadHomeFeed()
//     {
//         Debug.Log("Loading Home Feed");
//         StartCoroutine(CallHomeFeedAPI());
//     }
//
//     IEnumerator CallHomeFeedAPI()
//     {
//         string baseURL = baseScript.baseURL;
//         string token = AuthTokenManager.GetToken();
//
//         string url = $"{baseURL}/api/v1/post/home?page=1&limit={testLimit}";
//         UnityWebRequest request = UnityWebRequest.Get(url);
//         request.SetRequestHeader("Authorization", token);
//         request.SetRequestHeader("Content-Type", "application/json");
//
//         yield return request.SendWebRequest();
//
//         if (request.result == UnityWebRequest.Result.Success)
//         {
//             var response = JsonUtility.FromJson<HomeApiResponse>(request.downloadHandler.text);
//
//             if (response.success)
//             {
//                 if (response.data.type == "USERS")
//                 {
//                     ShowUserScroll();
//                     userPagination.Initialize(token, testLimit);
//                 }
//                 else if (response.data.type == "POSTS")
//                 {
//                     ShowPostScroll();
//                     postPagination.Initialize(token, testLimit);
//                 }
//             }
//             else
//             {
//                 Debug.LogWarning("Home feed API success false: " + response.message);
//             }
//         }
//         else
//         {
//             Debug.LogError("Home feed API error: " + request.error);
//         }
//     }
//
//     void ShowUserScroll()
//     {
//         userScroll.SetActive(true);
//         postScroll.SetActive(false);
//     }
//
//     void ShowPostScroll()
//     {
//         postScroll.SetActive(true);
//         userScroll.SetActive(false);
//     }
// }