using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GitHubUploader : MonoBehaviour {
  [Header("GitHub Settings")]
  public string repoOwner = "your-username";
  public string repoName = "your-repo";
  public string folderInRepo = "uploads"; // Optional folder path in the repo
  public string commitMessage = "User submission";
  public string branch = "main";

  [Header("Authentication")] [TextArea]
  public string personalAccessToken;

  public void UploadTextAsFile(string textToUpload) {
    // Generate a random filename
    string randomFileName = $"submission_{Guid.NewGuid().ToString("N").Substring(0, 8)}.txt";
    string targetPathInRepo = string.IsNullOrEmpty(folderInRepo) ? randomFileName : $"{folderInRepo}/{randomFileName}";

    // Encode content in Base64
    string base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(textToUpload));

    string url = $"https://api.github.com/repos/{repoOwner}/{repoName}/contents/{targetPathInRepo}";
    StartCoroutine(UploadCoroutine(url, base64Content, targetPathInRepo));
  }

  private IEnumerator UploadCoroutine(string url, string base64Content, string pathInRepo) {
    // First check if file exists (usually it won’t for a randomized name)
    UnityWebRequest getRequest = UnityWebRequest.Get(url);
    getRequest.SetRequestHeader("Authorization", "Bearer " + personalAccessToken);
    getRequest.SetRequestHeader("User-Agent", "UnityUploader");
    yield return getRequest.SendWebRequest();

    string sha = null;
    if (getRequest.responseCode == 200) {
      var responseText = getRequest.downloadHandler.text;
      var json = JsonUtility.FromJson<GitHubFileResponse>(responseText);
      sha = json.sha;
    }

    var payload = new GitHubUploadRequest {
      message = commitMessage,
      content = base64Content,
      branch = branch,
      sha = sha
    };

    string jsonData = JsonUtility.ToJson(payload);
    var uploadRequest = new UnityWebRequest(url, "PUT");
    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
    uploadRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
    uploadRequest.downloadHandler = new DownloadHandlerBuffer();
    uploadRequest.SetRequestHeader("Content-Type", "application/json");
    uploadRequest.SetRequestHeader("Authorization", "Bearer " + personalAccessToken);
    uploadRequest.SetRequestHeader("User-Agent", "UnityUploader");

    yield return uploadRequest.SendWebRequest();

    if (uploadRequest.result != UnityWebRequest.Result.Success) {
      Debug.LogError("Upload failed: " + uploadRequest.error + "\n" + uploadRequest.downloadHandler.text);
    } else {
      Debug.Log($"File uploaded successfully to: {pathInRepo}");
    }
  }

  [Serializable]
  public class GitHubUploadRequest {
    public string message;
    public string content;
    public string branch;
    public string sha; // Optional
  }

  [Serializable]
  public class GitHubFileResponse {
    public string sha;
  }
}
