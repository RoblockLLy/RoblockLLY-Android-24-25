/**
* Universidad de La Laguna
* Proyecto: Roblockly-Android
* Autor: Thomas Edward Bradley
* Email: alu0101408248@ull.edu.es
* Fecha: 01/07/2025
* Descripcion: Clase encargada de visualizar una maqueta del nivel generado
*/

using Newtonsoft.Json.Linq;
using UnityEngine;

public class PreviewManager : MonoBehaviour {
  
  #region Atributos
  
  [Header("Camara")]
  [SerializeField] [Tooltip("")]
  public Camera renderCamera;
  
  [Header("Object Prefabs")]
  [SerializeField] [Tooltip("")]
  public GameObject startObj;
  [SerializeField] [Tooltip("")]
  public GameObject goalObj;
  [SerializeField] [Tooltip("")]
  public GameObject fullBlock;

  [Header("Materials")]
  [SerializeField] [Tooltip("")]
  public Material black;
  [SerializeField] [Tooltip("")]
  public Material orange;

  /// <summary>
  /// JSON con el nivel generado
  /// </summary>
  private JObject jsonLevel;

  /// <summary>
  /// Tamaño del nivel que se esta tratando, necesario para camara
  /// </summary>
  private int levelSize;

  /// <summary>
  /// Objeto padre en la cual se van a colocar todos los elementos del nivel
  /// </summary>
  private GameObject parentObject;

  #endregion

  #region Preview

  /// <summary>
  /// Funcion principal para la previsualización del nivel guardado
  /// </summary>
  public void preview() {
    parentObject = new GameObject("Level Preview");   // Instanciar objeto padre
    JArray spawns = (JArray)jsonLevel["spawnpoints"]; // Pillamos el spawn 
    JArray goals = (JArray)jsonLevel["flags"];        // Pillamos el objetivo
    JArray objects = (JArray)jsonLevel["level"];      // Pillamos los objetos 

    foreach (JObject spawn in spawns) {               // Colocamos el punto de partida en la escena
      GameObject current = Instantiate(startObj);
      current.transform.SetParent(parentObject.transform);
      current.transform.position = stringPosToVector(spawn["position"].ToString());
    }

    foreach (JObject goal in goals) {                 // Colocamos el punto objetivo en la escena
      GameObject current = Instantiate(goalObj);
      current.transform.SetParent(parentObject.transform);
      current.transform.position = stringPosToVector(goal["position"].ToString());
    }

    foreach (JObject obj in objects) {  // Revisamos todos los elementos de nuestro nivel
      string name = obj["name"].ToString();
      
      switch (name) {   // Switch para saber que objeto debemos colocar
        case string temp when temp.StartsWith("Full Block"):
          GameObject current = Instantiate(fullBlock);
          current.transform.SetParent(parentObject.transform);
          current.transform.position = stringPosToVector(obj["position"].ToString());
          if (current.transform.position.y == 0) current.GetComponent<Renderer>().material = orange;
          else current.GetComponent<Renderer>().material = black;
          break;
        case string temp when temp.StartsWith("Lifting Door"):
          break;
        case string temp when temp.StartsWith("Pressure Plate"):
          break;
        case string temp when temp.StartsWith("Straight Path"):
          break;
        case string temp when temp.StartsWith("Corner Path"):
          break;
        default:
          Debug.LogWarning("Error - Non supported JSON element: \n" + obj);
          break;
      }
    }

    allignCamera();
  }

  /// <summary>
  /// Alinea la camara con el nivel en el escenario
  /// </summary>
  public void allignCamera() {
    renderCamera.transform.position = new Vector3(renderCamera.transform.position.x, levelSize * 1.5f, renderCamera.transform.position.z);
    Vector3 middlePoint = new Vector3(levelSize / 2, 0f, levelSize / 2);
    renderCamera.transform.LookAt(middlePoint);
  }

  /// <summary>
  /// Borra un nivel ya creado de antemano
  /// </summary>
  public void removeLevel() {
    GameObject.Destroy(parentObject);
  }

  #endregion

  #region Metodos Aux

  /// <summary>
  /// Setter para el JObject que contiene el nivel generado
  /// </summary>
  /// <param name="level">String que se parseara como JSON</param>
  public void setInternalValues(string level, int size) {
    jsonLevel = JObject.Parse(level);
    levelSize = size;
  }

  /// <summary>
  /// Convierte un string con un Vector3 a un Vector3 propio
  /// </summary>
  /// <param name="str">String con Vector3 sin procesar</param>
  /// <returns>Vector3 resultante</returns>
  private Vector3 stringPosToVector(string str) {
    str = str.Trim('(', ')');
    string[] parts = str.Split(',');

    float.TryParse(parts[0].Trim(), out float x);
    float.TryParse(parts[1].Trim(), out float y);
    float.TryParse(parts[2].Trim(), out float z);

    return new Vector3(x, y, z);
  }

  #endregion

}
