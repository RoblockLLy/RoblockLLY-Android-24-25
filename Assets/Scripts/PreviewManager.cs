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
using UnityEngine.UI;

public class PreviewManager : MonoBehaviour {
  
  #region Atributos
  
  [Header("Rendering")]
  [SerializeField] [Tooltip("Camara usado para capturar imagen del preview")]
  public Camera renderCamera;
  [SerializeField] [Tooltip("Textura vinculado a la camara encargado de renderizar la imagen")]
  public RenderTexture renderTexture;
  [SerializeField] [Tooltip("Imagen donde se mostrara la textura preview")]
  public RawImage previewImage;
  
  [Header("Object Prefabs")]
  [SerializeField] [Tooltip("Prefab para representar el punto de partida")]
  public GameObject startObj;
  [SerializeField] [Tooltip("Prefab para representar el punto objetivo")]
  public GameObject goalObj;
  [SerializeField] [Tooltip("Prefab para representar una pared")]
  public GameObject fullBlock;
  [SerializeField] [Tooltip("Prefab para representar una puerta interactiva")]
  public GameObject interactableDoor;
  [SerializeField] [Tooltip("Prefab para representar una placa presionable")]
  public GameObject pressurePlate;
  [SerializeField] [Tooltip("Prefab para representar un camino recto")]
  public GameObject straightPath;
  [SerializeField] [Tooltip("Prefab para representar una esquina de camino")]
  public GameObject cornerPath;

  [Header("Materials")]
  [SerializeField] [Tooltip("Material matte usado para el color 'white'")]
  public Material white;
  [SerializeField] [Tooltip("Material matte usado para el color 'light red'")]
  public Material lightRed;
  [SerializeField] [Tooltip("Material matte usado para el color 'red'")]
  public Material red;
  [SerializeField] [Tooltip("Material matte usado para el color 'orange'")]
  public Material orange;
  [SerializeField] [Tooltip("Material matte usado para el color 'light orange'")]
  public Material lightOrange;
  [SerializeField] [Tooltip("Material matte usado para el color 'yellow'")]
  public Material yellow;
  [SerializeField] [Tooltip("Material matte usado para el color 'green'")]
  public Material green;
  [SerializeField] [Tooltip("Material matte usado para el color 'lightGreen'")]
  public Material lightGreen;
  [SerializeField] [Tooltip("Material matte usado para el color 'turquoise'")]
  public Material turquoise;
  [SerializeField] [Tooltip("Material matte usado para el color 'light blue'")]
  public Material lightBlue;
  [SerializeField] [Tooltip("Material matte usado para el color 'blue'")]
  public Material blue;
  [SerializeField] [Tooltip("Material matte usado para el color 'purple'")]
  public Material purple;
  [SerializeField] [Tooltip("Material matte usado para el color 'pink'")]
  public Material pink;
  [SerializeField] [Tooltip("Material matte usado para el color 'brown'")]
  public Material brown;
  [SerializeField] [Tooltip("Material matte usado para el color 'gray'")]
  public Material gray;
  [SerializeField] [Tooltip("Material matte usado para el color 'black'")]
  public Material black;

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
  /// Tenemos que incializar los elementos del preview en Start() en lugar de editor para que funcione en Android
  /// </summary>
  private void Start() {
    renderTexture = new RenderTexture(1024, 1024, 16, RenderTextureFormat.Default);
    renderTexture.Create();
    renderCamera.targetTexture = renderTexture;
    previewImage.texture = renderTexture;
  }

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
          GameObject currentBlock = Instantiate(fullBlock);
          currentBlock.transform.SetParent(parentObject.transform);
          currentBlock.transform.position = stringPosToVector(obj["position"].ToString());
          currentBlock.GetComponent<Renderer>().material = fetchMaterial(obj["options"][0]["Color"].ToString());
          break;
        case string temp when temp.StartsWith("Lifting Door"):
          GameObject currentInteractableDoor = Instantiate(interactableDoor);
          currentInteractableDoor.transform.SetParent(parentObject.transform);
          currentInteractableDoor.transform.position = stringPosToVector(obj["position"].ToString());
          currentInteractableDoor.transform.rotation = StringToQuaternion(obj["rotation"].ToString());
          currentInteractableDoor.GetComponent<Renderer>().material = fetchMaterial(obj["options"][0]["Color"].ToString());
          break;
        case string temp when temp.StartsWith("Pressure Plate"):
          GameObject currentPlate = Instantiate(pressurePlate);
          currentPlate.transform.SetParent(parentObject.transform);
          currentPlate.transform.position = stringPosToVector(obj["position"].ToString());
          break;
        case string temp when temp.StartsWith("Straight Path"):
          GameObject currentStraightPath = Instantiate(straightPath);
          currentStraightPath.transform.SetParent(parentObject.transform);
          currentStraightPath.transform.position = stringPosToVector(obj["position"].ToString());
          currentStraightPath.transform.rotation = StringToQuaternion(obj["rotation"].ToString());
          foreach (Transform child in currentStraightPath.transform) {
            child.gameObject.GetComponent<Renderer>().material = fetchMaterial(obj["options"][0]["Color"].ToString());
          }
          break;
        case string temp when temp.StartsWith("Corner Path"):
          GameObject currentCornerPath = Instantiate(cornerPath);
          currentCornerPath.transform.SetParent(parentObject.transform);
          currentCornerPath.transform.position = stringPosToVector(obj["position"].ToString());
          currentCornerPath.transform.rotation = StringToQuaternion(obj["rotation"].ToString());
          foreach (Transform child in currentCornerPath.transform) {
            child.gameObject.GetComponent<Renderer>().material = fetchMaterial(obj["options"][0]["Color"].ToString());
          }
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

  /// <summary>
  /// Convierte un string con un Quaternion a un Quaternion propio
  /// </summary>
  /// <param name="str">String con Quaternion sin procesar</param>
  /// <returns>Quaternion resultante</returns>
  private Quaternion StringToQuaternion(string str) {
    str = str.Trim('(', ')');
    string[] parts = str.Split(',');

    float.TryParse(parts[0].Trim(), out float x);
    float.TryParse(parts[1].Trim(), out float y);
    float.TryParse(parts[2].Trim(), out float z);
    float.TryParse(parts[3].Trim(), out float w);

    return new Quaternion(x, y, z, w);
  }

  /// <summary>
  /// Devuelve un material en base al texto pasado
  /// </summary>
  /// <param name="text">Nombre del color en string</param>
  /// <returns>Material correspondiente, blanco si no se reconoce</returns>
  private Material fetchMaterial(string text) {
    switch (text) {
      case "White":
        return white;
      case "Light Red":
        return lightRed;
      case "Red":
        return red;
      case "Orange":
        return orange;
      case "Light Orange":
        return lightOrange;
      case "Yellow":
        return yellow;
      case "Green":
        return green;
      case "Light Green":
        return lightGreen;
      case "Turquoise":
        return turquoise;
      case "Light Blue":
        return lightBlue;
      case "Blue":
        return blue;
      case "Purple":
        return purple;
      case "Pink":
        return pink;
      case "Brown":
        return brown;
      case "Black":
        return black;
      default:
        Debug.LogWarning("Unrecognized Color");
        return white;
    }
  }

  #endregion

}
