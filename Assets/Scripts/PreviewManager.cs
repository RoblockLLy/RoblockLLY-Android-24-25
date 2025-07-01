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
  
  [Header("UI Elements")]
  [SerializeField] [Tooltip("")]
  public GameObject renderSpace;
  
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
  }

  #endregion

  #region Metodos Aux

  /// <summary>
  /// Setter para el JObject que contiene el nivel generado
  /// </summary>
  /// <param name="level">String que se parseara como JSON</param>
  public void setLevel(string level) {
    jsonLevel = JObject.Parse(level);
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
