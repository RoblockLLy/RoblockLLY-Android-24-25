/**
* Universidad de La Laguna
* Proyecto: Roblockly-Android
* Autor: Thomas Edward Bradley
* Email: alu0101408248@ull.edu.es
* Fecha: 15/06/2025
* Descripcion: Clase encargada de subir nuestros ficheros (con los niveles completados)
*              a github con un nombre aleatorio, commiteado por la cuenta RoblockLLy
*/

using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GitHubUploader : MonoBehaviour {
  
  #region Atributos

  [Header("GitHub Settings")]
  [SerializeField] [Tooltip("Nombre del usuario propietario que creo el repositorio a buscar")]
  public string repoOwner = "your-username";
  [SerializeField] [Tooltip("Nombre del repositorio al que deseamos subir el nivel")]
  public string repoName = "your-repo";
  [SerializeField] [Tooltip("Dirección dentro del directorio en el que queremos dejar el nivel")]
  public string folderInRepo = "uploads";
  [SerializeField] [Tooltip("Mensaje del commit que se dejara al subir el nivel")]
  public string commitMessage = "User submission";
  [SerializeField] [Tooltip("Rama dentro del repositorio al que se subira el nivel")]
  public string branch = "main";

  [Header("Authentication")] [TextArea]
  [SerializeField] [Tooltip("Token con permisos sobre el repositorio, necesario para poder subir")]
  public string personalAccessToken;

  #endregion

  #region Funciones GitHub

  /// <summary>
  /// Prepara la URL a la que queremos subir el texto y crea el nombre del fichero,
  /// también lanza la corrutina encargada del proceso de subida
  /// </summary>
  /// <param name="textToUpload">Texto JSON con el nivel generado</param>
  public void UploadTextAsFile(string textToUpload) {
    // Generate a random filename
    string randomFileName = $"submission_{Guid.NewGuid().ToString("N").Substring(0, 8)}.txt";
    string targetPathInRepo = string.IsNullOrEmpty(folderInRepo) ? randomFileName : $"{folderInRepo}/{randomFileName}";

    // Encode content in Base64
    string base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(textToUpload));

    string url = $"https://api.github.com/repos/{repoOwner}/{repoName}/contents/{targetPathInRepo}";
    StartCoroutine(UploadCoroutine(url, base64Content, targetPathInRepo));
  }

  /// <summary>
  /// Corutina encargado de subir nuestro contenido al repositorio GitHub
  /// </summary>
  /// <param name="url">Dirección web a la que queremos subir el fichero</param>
  /// <param name="base64Content">Texto a subir dentro del fichero, contiene el nivel generado</param>
  /// <param name="pathInRepo">Directorio dentro del repositorio en el que alojaremos el fichero, incluyendo el nombre del mismo</param>
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

  #endregion

  #region Clases Adicionales

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

  #endregion

}
