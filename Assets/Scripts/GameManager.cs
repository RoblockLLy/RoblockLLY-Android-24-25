using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

struct Coord {
  public int xVal;
  public int yVal;
};

public class GameManager : MonoBehaviour
{
  
  #region Atributos
  
  [Header("Campos Modificables")]
  [SerializeField] [Tooltip("Tamaño NxN del grid, incluyendo paredes externas")]
  public int levelSize = 6;
  
  [Header("Campos de Texto Usuario")]
  [SerializeField] [Tooltip("Input de usuario con el nombre del nivel")]
  public TMP_InputField levelText;
  [SerializeField] [Tooltip("Input de usuario con el nombre del usuario")]
  public TMP_InputField userText;
  [SerializeField] [Tooltip("Input de usuario con el nombre del skybox")]
  public TextMeshProUGUI skyboxText;
  [SerializeField] [Tooltip("Input de usuario con el tamaño del nivel")]
  public TMP_InputField sizeText;

  [SerializeField] [Tooltip("Form UI para cuando se esta haciendo el scan")]
  public GameObject panelScan;
  [SerializeField] [Tooltip("Form UI para cuando se ha hecho el scan")]
  public GameObject panelInfo;
  [SerializeField] [Tooltip("Form UI para cuando se ha introducido toda la información adicional")]
  public GameObject panelExport;
  [SerializeField] [Tooltip("Campo donde mostrar el código completado del nivel")]
  public TMP_InputField exportText;
  [SerializeField] [Tooltip("Lista de objetos que pueden ser invocados por Vuforia")]
  public List<GameObject> objects;
  //  [0] Goal
  //  [1] Car
  //  [2] Wall
  //  [3] Slide Wall
  //  [4] Plate
  //  [5] Path

  private const int minGridSize = 5;
  private const float quarterTurn = 0.70711f;
  private List<bool> activeObject = new List<bool>();
  private List<Coord> usedPositions = new List<Coord>();

  private Coord minFlag;
  private Coord maxFlag;
  private Coord minStart;
  private Coord maxStart;

  #endregion

  private void Start() {
    resetObjects();
  }
   
  #region UI

  public void OpenScan() {
    if (panelInfo.activeSelf) {
      resetObjects(); // Objetos detectados a false
      panelScan.SetActive(true);  // Necesario ocultar para que no se reorganice con la activación del Dropdown
      panelInfo.SetActive(false);
      if (panelExport.activeSelf) panelExport.SetActive(false);
    } else {
      panelScan.SetActive(false);
      panelInfo.SetActive(true);
    }
  }

  public void CodePanel() {
    if (panelExport.activeSelf) {
      panelExport.SetActive(false);
    } else {
      buildExport();
      panelExport.SetActive(true);
    }
  }

  public void CargaEscena() {
    Scene escena = SceneManager.GetActiveScene();
    
    switch (escena.buildIndex) {
      case 0:
        buildExport();
        SceneManager.LoadScene(1);
        break;
      case 1:
        SceneManager.LoadScene(0);
        break;
    }
  }

  #endregion

  #region Build JSON

  public void buildExport() {
    JObject result = buildEnv();
    
    usedPositions.Clear();  // Dejar vacio cada vez que empezamos una nueva export
    setupCoordValues();     // Para dejar meta y jugador en posiciones adecuadas

    JArray flags = new JArray();
    JArray spawnpoints = new JArray();
    JArray export = new JArray();

    int count = 0;
    if (activeObject[3]) {  // Slide Wall, tiene que ser antes de bandera y jugador
      Coord pos = generateRandomPos(2, levelSize - 2, 2, levelSize - 2);  // No puede ir en los laterales
      export.Add(buildJSON(objects[3].name + " " + count.ToString(), new Vector3(pos.xVal, 1, pos.yVal), new Quaternion(0 ,0 ,0 ,1), "Turquoise"));

      Coord platePos = generateRandomPos(1, levelSize - 1, pos.yVal + 1, levelSize - 1);  // Placa para activar pared
      export.Add(buildJSON("Pressure Plate " + count.ToString(), new Vector3(platePos.xVal, 1, platePos.yVal), new Quaternion(0 ,0 ,0 ,1), "White", count));

      for (int i = 1; i < levelSize - 1; i++) { // Build Horizontal Wall
        if (i == pos.xVal) continue;
        export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(i, 1, pos.yVal), new Quaternion(0 ,0 ,0 ,1)));
        usedPositions.Add(new Coord { xVal = i, yVal = pos.yVal });
        count++;
      }

      maxFlag.yVal = pos.yVal;
      minStart.yVal = pos.yVal + 1;
    }

    Coord flagCoord = new Coord(), startCoord = new Coord();

    if (activeObject[0]) {  // Flag Active
      Coord pos = flagCoord = generateRandomPos(minFlag.xVal, maxFlag.xVal, minFlag.yVal, maxFlag.yVal);
      flags.Add(buildJSON(objects[0].name + " 0", new Vector3(pos.xVal, 1, pos.yVal), new Quaternion(0 ,0 ,0 ,1)));
    }
    

    if (activeObject[1]) {  // Spawn Active
      Coord pos = startCoord = generateRandomPos(minStart.xVal, maxStart.xVal, minStart.yVal, maxStart.yVal);
      spawnpoints.Add(buildJSON(objects[1].name + " 0", new Vector3(pos.xVal, 1, pos.yVal), new Quaternion(0 ,0 ,0 ,1)));
    }

    if (activeObject[5]) {  // Path
      List<Coord> path = generatePath(startCoord, flagCoord);
      for (int i = 0; i < path.Count; i++) {
        Coord pos = path[i];
        if (i == 0) { // First Straight Piece
          Coord next = path[i + 1];
          Quaternion rotation = new Quaternion(0, 0, 0 ,1);
          if (next.xVal != pos.xVal) rotation = new Quaternion(0, quarterTurn, 0, -quarterTurn);
          export.Add(buildJSON("Straight Path " + count, new Vector3(pos.xVal, 1, pos.yVal), rotation));
          count++;
        } else if (i != path.Count - 1) { // Middle Path
          Coord next = path[i + 1];
          Coord prev = path[i - 1];
          if (prev.xVal != next.xVal && prev.yVal != next.yVal) { // Corner Piece
            Quaternion rotation = new Quaternion();
            if (prev.xVal + 1 == next.xVal && prev.yVal + 1 == next.yVal) { // (+, +)
              if (pos.yVal + 1 == next.yVal) rotation = new Quaternion(0, 1, 0, 0);
              else rotation = new Quaternion(0, 0, 0, 1);                         
            } else if (prev.xVal + 1 == next.xVal && prev.yVal - 1 == next.yVal) {  // (+ -)
              if (pos.yVal - 1 == next.yVal) rotation = new Quaternion(0, quarterTurn, 0 , quarterTurn);
              else rotation = new Quaternion(0, quarterTurn, 0 , -quarterTurn);     
            } else if (prev.xVal - 1 == next.xVal && prev.yVal + 1 == next.yVal) {  // (-, +)
              if (pos.yVal + 1 == next.yVal) rotation = new Quaternion(0, quarterTurn, 0 , -quarterTurn);
              else rotation = new Quaternion(0, quarterTurn, 0 , quarterTurn);                                 
            } else if (prev.xVal - 1 == next.xVal && prev.yVal - 1 == next.yVal) {  // (-, -)
              if (pos.yVal - 1 == next.yVal) rotation = new Quaternion(0, 0, 0, 1);   
              else rotation = new Quaternion(0, 1, 0, 0);
            }
            export.Add(buildJSON("Corner Path " + count, new Vector3(pos.xVal, 1, pos.yVal), rotation));
            count++;
          } else {  // Straight Piece in Sequence
            Quaternion rotation = new Quaternion(0, 0, 0 ,1);
            if (next.xVal != pos.xVal) rotation = new Quaternion(0, quarterTurn, 0, -quarterTurn);
            export.Add(buildJSON("Straight Path " + count, new Vector3(pos.xVal, 1, pos.yVal), rotation));
            count++;
          }
        } else {  // Final Straight Piece
          Coord prev = path[i - 1];
          Quaternion rotation = new Quaternion(0, 0, 0 ,1);
          if (prev.xVal != pos.xVal) rotation = new Quaternion(0, quarterTurn, 0, -quarterTurn);
          export.Add(buildJSON("Straight Path " + count, new Vector3(pos.xVal, 1, pos.yVal), rotation));
          count++;
        }
      }
    }

    for (int i = 0; i < levelSize; i++) { // Building Outer Walls & Floor
      for (int j = 0; j < levelSize; j++) {
        if (i != 0 && i != levelSize - 1 && j != 0 && j != levelSize - 1) {
          export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(i, 0, j), new Quaternion(0 ,0 ,0 ,1), "Light Orange"));
          count++;
          continue;
        } ;
        export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(i, 1, j), new Quaternion(0 ,0 ,0 ,1)));
        count++;
      }
    }

    for (int i = 2; i < objects.Count; i++) {  // Build Additional Pieces
      if (i == 3) continue; // To remove later
      if (i == 5) continue; // To remove later
      if (activeObject[i]) {
        Coord pos = generateRandomPos();
        export.Add(buildJSON(objects[i].name + " " + count.ToString(), new Vector3(pos.xVal, 1, pos.yVal), new Quaternion(0 ,0 ,0 ,0)));
        count++;
      }
    }

    result["spawnpoints"] = spawnpoints;
    result["flags"] = flags;
    result["level"] = export;
    // result["dev_blocks"] = count;

    string finalText = result.ToString(Formatting.Indented);
    exportText.text = finalText;
    Debug.Log(finalText);
  }

  private JObject buildEnv() {
    try {
      int temp = int.Parse(sizeText.text);
      levelSize = temp < minGridSize ? minGridSize : temp;
    } catch {
      Debug.LogError("Size Input was not a valid number");
      levelSize = minGridSize;
    }
    
    JObject environment = new JObject {
      { "skybox", skyboxText.text },
      { "level_name", levelText.text },
      { "user_name", userText.text },
      { "dev_minutes", 0 },
      { "dev_seconds", 0 },
      { "dev_blocks", 0 }
    };

    return new JObject() { { "environment", environment } };
  }

  private JObject buildJSON(string name, Vector3 pos, Quaternion rotation, string colorSt = "White", int doorNumber = 0) {
    JObject json = new JObject();
    json["name"] = name;
    json["position"] = pos.ToString();
    json["rotation"] = rotation.ToString();

    JArray options = new JArray();
    if (name.Contains("Flag")) {
      JObject time = new JObject { { "Timed", "False" } };
      JObject seconds = new JObject { { "Seconds for win", "0" } };
      options.Add(time);
      options.Add(seconds);
    } else if (name.Contains("Pressure Plate")) {
      JObject lift = new JObject { { "Lifting Door " + doorNumber, "Toggle" } };
      options.Add(lift);
    } else if (!name.Contains("Spawnpoint")) {
      JObject color = new JObject { { "Color", colorSt } };
      options.Add(color);
    }
    json["options"] = options;

    return json;
  }

  private Coord generateRandomPos(int minX = 1, int maxX = -1, int minY = 1, int maxY = -1) {
    if (maxX == -1) maxX = levelSize - 1;
    if (maxY == -1) maxY = levelSize - 1;
    bool found = false;

    while (!found) {
      System.Random rnd = new System.Random();
      int xCoord, yCoord;
      if (minX == maxX) {
        xCoord = minX;
      } else {
        xCoord = rnd.Next(minX, maxX);  // En el caso de 4, n >= 1 & n < 4
      }
      if (minY == maxY) {
        yCoord = minY;
      } else {
        yCoord = rnd.Next(minY, maxY);
      }
      Coord generated = new Coord { xVal = xCoord, yVal = yCoord };
      
      if (!usedPositions.Contains(generated)) {
        usedPositions.Add(new Coord { xVal = xCoord, yVal = yCoord });
        found = true;
      }
    }

    return usedPositions.Last();
  }

  private void setupCoordValues() {
    minFlag = new Coord { xVal = 1, yVal = 1 };
    maxFlag = new Coord { xVal = levelSize - 1, yVal = levelSize - 1 };
    minStart = new Coord { xVal = 1, yVal = 1 };
    maxStart = new Coord { xVal = levelSize - 1, yVal = levelSize - 1 };
  }

  private List<Coord> generatePath(Coord start, Coord end) {
    Debug.Log("In Function!");
    List<Coord> result = new List<Coord>();
    HashSet<Coord> visited = new HashSet<Coord>();
    Coord current = start;

    result.Add(current);
    visited.Add(current);

    System.Random rng = new System.Random();

    // Define movement directions
    Vector2Int[] directions = new Vector2Int[]
    {
      Vector2Int.up,
      Vector2Int.down,
      Vector2Int.left,
      Vector2Int.right
    };

    while (current.xVal != end.xVal || current.yVal != end.yVal)
    {
      // Create a list of valid, not-yet-visited neighbors
      List<Coord> neighbors = new List<Coord>();

      foreach (var dir in directions)
      {
        Coord next = new Coord { xVal = current.xVal + dir.x, yVal = current.yVal + dir.y };

        // Optional: check bounds here if you want to limit to a grid
        if (visited.Contains(next)) continue;

        // Heuristic: only add moves that bring us closer or sideways
        int remainingDistCurrent = Mathf.Abs(end.xVal - current.xVal) + Mathf.Abs(end.yVal - current.yVal);
        int remainingDistNext = Mathf.Abs(end.xVal - next.xVal) + Mathf.Abs(end.yVal - next.yVal);

        if (remainingDistNext <= remainingDistCurrent)
        {
          neighbors.Add(next);
        }
      }

      if (neighbors.Count == 0)
      {
        // No valid moves, backtrack or fail
        Debug.LogWarning("No valid path found. Path is blocked or trapped.");
        return result;
      }

      // Pick a random neighbor
      current = neighbors[rng.Next(neighbors.Count)];
      result.Add(current);
      visited.Add(current);
    }
    return result;
  }

  #endregion

  #region Vuforia

  public void activateObject(int value) {
    // Solo actualizar si estamos en el form de scan
    if (!panelInfo.activeSelf) activeObject[value] = true;
  }

  public void deactivateObject(int value) {
    // Solo actualizar si estamos en el form de scan
    if (!panelInfo.activeSelf) activeObject[value] = false;
  }

  private void resetObjects() {
    activeObject = new List<bool>();
    foreach (bool val in objects) {
      activeObject.Add(false);
    }
  }

  #endregion

}
