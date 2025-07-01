/**
* Universidad de La Laguna
* Proyecto: Roblockly-Android
* Autor: Thomas Edward Bradley
* Email: alu0101408248@ull.edu.es
* Fecha: 15/06/2025
* Descripcion: Manager de los elementos UI y rasgos generales del proyecto
*/

using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour {
  
  #region Atributos

  [Header("Referencias a Managers")]
  [SerializeField] [Tooltip("Código para generar nuestro fichero JSON")]
  public JsonGenerator jsonBuilder;
  [SerializeField] [Tooltip("Código para subir nuestros resultados a GitHub")]
  public GitHubUploader uploader;

  [Header("Elementos UI")]
  [SerializeField] [Tooltip("Form UI para cuando se esta haciendo el scan")]
  public GameObject panelScan;
  [SerializeField] [Tooltip("Form UI para cuando se ha intentado subir un scan sin tener la bandera y el coche")]
  public GameObject panelMissingScans;
  [SerializeField] [Tooltip("Form UI para cuando se ha hecho el scan")]
  public GameObject panelInfo;
  [SerializeField] [Tooltip("Form UI para cuando se ha introducido toda la información adicional")]
  public GameObject panelExport;
  [SerializeField] [Tooltip("Panel UI donde se muestra el código generado para el nivel")]
  public GameObject contentObject;
  [SerializeField] [Tooltip("Form UI para cuando se ha subido un nivel al repositorio con éxito")]
  public GameObject panelSubmited;
  [SerializeField] [Tooltip("Form UI para cuando ha ocurrido un error con la generación del nivel")]
  public GameObject panelError;
  [SerializeField] [Tooltip("Campo donde mostrar el código completado del nivel")]
  public TMP_InputField exportText;

  [Header("Preview Elements")]
  [SerializeField] [Tooltip("Manager que se encaraga de visualizar el nivel por pantalla")]
  public PreviewManager previewManager;

  /// <summary>
  /// Lista de objetos activados por Vuforia, true si estan puestos
  /// </summary>
  private List<bool> activeObjects = new List<bool>();
  /// <summary>
  /// Texto completado del nivel generado
  /// </summary>
  private string finishedLevelText = "";

  #endregion

  /// <summary>
  /// Reseteamos la lista de Objetos al iniciar
  /// </summary>
  private void Start() {
    resetObjects();
  }
   
  #region UI

  /// <summary>
  /// Activado cuando queremos abrir o cerrar el panel de Scan (el principal con la cámara)
  /// Prepara la UI necesario conforme a lo previo
  /// </summary>
  public void ScanPanel() {
    if (panelInfo.activeSelf) {   // Volver a panel de Scan
      resetObjects();             // Borrar objetos guardados como encontrados al volver
      panelScan.SetActive(true);
      panelInfo.SetActive(false);
      if (panelExport.activeSelf) panelExport.SetActive(false);
      if (panelError.activeSelf) panelError.SetActive(false);
    } else {                      // Avanzar al Panel de Información Adicional
      if (!checkValidScans()) {   // Comprobamos de que estan la bandera y el coche
        panelMissingScans.SetActive(true);
        return;
      }
      
      panelScan.SetActive(false); // Necesario ocultar porque interactua mal con el Dropdown de Skybox
      if (panelMissingScans.activeSelf) panelMissingScans.SetActive(false);
      panelInfo.SetActive(true);
    }
  }

  /// <summary>
  /// Activado cuando queremos abrir o cerrar el panel de Codigo (con el nivel generado)
  /// Si no esta abierto, se encargara de generar nuestro nivel para poder mostrar esta
  /// </summary>
  public void CodePanel() {
    if (panelExport.activeSelf) {
      previewManager.removeLevel();
      panelExport.SetActive(false);
    } else {      
      int counter = 0, maxCounter = 1000;
      bool succesful = false;
      jsonBuilder.setActiveObjects(activeObjects);

      while (counter < maxCounter && !succesful) {     // Se realizan hasta 5 generacione si hace falta, protección contra generación camino
        string result = jsonBuilder.buildExport();

        if (!string.IsNullOrEmpty(result)) {  // Generación éxitosa
          succesful = true;
          exportText.text = finishedLevelText = result;

          // CAMBIAR TAMAÑO DEL ESPACIO DE SCROLL DINAMICAMENTE          
          GameObject inputField = contentObject.transform.Find("InputField (TMP)").gameObject;
          int lineBreaks = finishedLevelText.Count(c => c == '\n');
          float heightPerLine = 13.85f;  // Aproximadamente
          contentObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, heightPerLine * lineBreaks);
          inputField.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, heightPerLine * lineBreaks);
          
          previewManager.setInternalValues(finishedLevelText, jsonBuilder.getLevelSize());
          previewManager.preview();

          panelExport.SetActive(true);
        } else {                              // Se ha producido un error con la generación
          counter++;
        } 
        // Debug.Log("Counter: " + counter);
      }

      if (!succesful) {  // Hubo errores con las generaciones
        panelError.SetActive(true);
      } 
    }
  }

  /// <summary>
  /// Se encarga de cerrar el panel de aviso sobre scans ausentes
  /// </summary>
  public void CloseMissingScansPanel() {
    if (panelMissingScans.activeSelf) panelMissingScans.SetActive(false);
  }

  /// <summary>
  /// Se encarga de cerrar el panel de subida de nivel éxitosa
  /// </summary>
  public void CloseSubmitedPanel() {
    if (panelSubmited.activeSelf) panelSubmited.SetActive(false);
  }

  /// <summary>
  /// Se encarga de cerrar el panel de Error
  /// </summary>
  public void CloseErrorPanel() {
    if (panelError.activeSelf) panelError.SetActive(false);
  }

  #endregion

  #region Vuforia

  /// <summary>
  /// Activado cuando Vuforia pilla un objeto, value corresponde a la posición del objeto pillado
  /// Solo funcionara cuando estamos en el Form de Scan
  /// </summary>
  /// <param name="value"></param>
  public void activateObject(int value) {
    if (!panelInfo.activeSelf) activeObjects[value] = true;
  }

  /// <summary>
  /// Activado cuando Vuforia pierde un objeto, value corresponde a la posición del objeto perdido
  /// Solo funcionara cuando estamos en el Form de Scan
  /// </summary>
  public void deactivateObject(int value) {
    if (!panelInfo.activeSelf) activeObjects[value] = false;
  }

  /// <summary>
  /// Borramos la lista actual de objetos activos y lo volvemos a inicializar con todos a false
  /// </summary>
  private void resetObjects() {
    activeObjects = new List<bool>();
    for (int i = 0; i < jsonBuilder.objectAmount(); i++) {
      activeObjects.Add(false);
    }
  }

  #endregion

  #region Metodos Aux

  /// <summary>
  /// Averigua si estan activos tanto la bandera como el coche
  /// </summary>
  /// <returns>True si ambos estan activos</returns>
  public bool checkValidScans() {
    return activeObjects[0] && activeObjects[1];
  }

  /// <summary>
  /// Deseamos subir nuestro nivel creado al repositorio de GitHub
  /// </summary>
  public void submitGeneratedLevel() {
    uploader.UploadTextAsFile(finishedLevelText);
  }

  /// <summary>
  /// Método usado por la clase GithubUpload para comunicar si se ha tenido éxito con la subida del nivel
  /// </summary>
  /// <param name="status">True si la subida del fichero fue éxitoso</param>
  public void submissionResult(bool status) {
    if (status) {
      panelExport.SetActive(false);
      panelSubmited.SetActive(true);
    } else {
      panelError.SetActive(true);
    }
  }

  /// <summary>
  /// Activado cuando se pulsa sobre el boton de Copiar, llama al metodo de copiar con el codigo final generado
  /// </summary>
  public void copyCode() {
    copyToClipboard(finishedLevelText);
  }

  /// <summary>
  /// Copia el texto pasado para que el ussuario pueda pegarlo (ya sea en android o editor)
  /// </summary>
  /// <param name="text">Texto a copiar</param>
  public void copyToClipboard(string text){
    #if UNITY_ANDROID && !UNITY_EDITOR
      AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
      AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

      AndroidJavaObject clipboardManager = activity.Call<AndroidJavaObject>("getSystemService", "clipboard");
      AndroidJavaClass clipDataClass = new AndroidJavaClass("android.content.ClipData");
      AndroidJavaObject clip = clipDataClass.CallStatic<AndroidJavaObject>("newPlainText", "label", text);
      clipboardManager.Call("setPrimaryClip", clip);
    #else   // Para cuando se este usando Editor
      GUIUtility.systemCopyBuffer = text; 
    #endif
  }

  #endregion

}
