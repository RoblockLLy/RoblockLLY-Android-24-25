/**
* Universidad de La Laguna
* Proyecto: Roblockly-Android
* Autor: Thomas Edward Bradley
* Email: alu0101408248@ull.edu.es
* Fecha: 15/06/2025
* Descripcion: Manager de los elementos UI y rasgos generales del proyecto
*/

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
  [SerializeField] [Tooltip("Form UI para cuando se ha hecho el scan")]
  public GameObject panelInfo;
  [SerializeField] [Tooltip("Form UI para cuando se ha introducido toda la información adicional")]
  public GameObject panelExport;
  [SerializeField] [Tooltip("Form UI para cuando ha ocurrido un error con la generación del nivel")]
  public GameObject panelError;
  [SerializeField] [Tooltip("Campo donde mostrar el código completado del nivel")]
  public TMP_InputField exportText;

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
      panelScan.SetActive(false); // Necesario ocultar porque interactua mal con el Dropdown de Skybox
      panelInfo.SetActive(true);
    }
  }

  /// <summary>
  /// Activado cuando queremos abrir o cerrar el panel de Codigo (con el nivel generado)
  /// Si no esta abierto, se encargara de generar nuestro nivel para poder mostrar esta
  /// </summary>
  public void CodePanel() {
    if (panelExport.activeSelf) {
      panelExport.SetActive(false);
    } else {
      jsonBuilder.setActiveObjects(activeObjects);
      string result = jsonBuilder.buildExport();

      if (!string.IsNullOrEmpty(result)) {
        exportText.text = finishedLevelText = result;
        panelExport.SetActive(true);
      } else {  // Se ha producido un error con la generación
        panelError.SetActive(true);
      } 
    }
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

  #region Extra Buttons

  /// <summary>
  /// Deseamos subir nuestro nivel creado al repositorio de GitHub
  /// </summary>
  public void submitGeneratedLevel() {
    uploader.UploadTextAsFile(finishedLevelText);
  }

  #endregion

}
