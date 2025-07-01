/**
* Universidad de La Laguna
* Proyecto: Roblockly-Android
* Autor: Thomas Edward Bradley
* Email: alu0101408248@ull.edu.es
* Fecha: 15/06/2025
* Descripcion: Clase encargada de generar el string JSON con nuestro nivel personalizado generado
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class JsonGenerator : MonoBehaviour {
  
  #region Atributos
  
  [Header("Campos de Texto Usuario")]
  [SerializeField] [Tooltip("Input de usuario con el nombre del nivel")]
  public TMP_InputField levelText;
  [SerializeField] [Tooltip("Input de usuario con el nombre del usuario")]
  public TMP_InputField userText;
  [SerializeField] [Tooltip("Input de usuario con el nombre del skybox")]
  public TextMeshProUGUI skyboxText;
  [SerializeField] [Tooltip("Input de usuario con el tamaño del nivel")]
  public TMP_InputField sizeText;

  [Header("Referencias a Objetos")]
  [SerializeField] [Tooltip("Lista de objetos que pueden ser invocados por Vuforia")]
  public List<GameObject> objects;
  //  [0] Goal
  //  [1] Car
  //  [2] Maze
  //  [3] Interactable Wall
  //  [4] Pressure Plate
  //  [5] Path
  //  [6] Color Bomb
  //  [7] Black & White

  /// <summary>
  /// Tamaño del nivel a generar, NxN
  /// </summary>
  private int levelSize = 7;
  /// <summary>
  /// Tamaño mínimo del nivel a generar
  /// </summary>
  private const int minGridSize = 7;

  /// <summary>
  /// Lista de elementos deseados dentro del nivel, en las mismas posiciones que la lista "objetos"
  /// True si se desea añadir el elemento
  /// </summary>
  private List<bool> activeObject = new List<bool>();
  /// <summary>
  /// Posiciones en nuestro nivel que ya han sido ocupados por algun otro elemento importante
  /// </summary>
  private List<Vector2Int> usedPositions = new List<Vector2Int>();

  /// <summary>
  /// Coordinada minimo de la bandera
  /// </summary>
  private Vector2Int minFlag;
  /// <summary>
  /// Coordinada maximo de la bandera
  /// </summary>
  private Vector2Int maxFlag;
  /// <summary>
  /// Coordinada minimo del punto de partida
  /// </summary>
  private Vector2Int minStart;
  /// <summary>
  /// Coordinada maximo del punto de partida
  /// </summary>
  private Vector2Int maxStart;

  /// <summary>
  /// Listado de todos los posibles colores
  /// </summary>
  private List<string> colorList = new List<string> {
    "White",
    "Light Red",
    "Red",
    "Orange",
    "Light Orange",
    "Yellow",
    "Green",
    "Light Green",
    "Turquoise",
    "Light Blue",
    "Blue",
    "Purple",
    "Pink",
    "Brown",
    "Gray",
    "Black"
  };

  #endregion

  #region JSON

  /// <summary>
  /// Crea un nivel personalizado en base a las opciones que estan guardados en la clase
  /// </summary>
  /// <returns>JSON string con el nivel generado</returns>
  public string buildExport() {
    JObject result = buildEnv();  // Construimos en entorno del JSON (la cabecera)
    usedPositions.Clear();        // Dejar vacio cada vez que empezamos una nueva export
    setupCoordValues();           // Para dejar meta y jugador en posiciones adecuadas

    int count = 0;          // Contador para asignar nombres unicos a objetos
    int doorCounter = 0;    // Contador para asignar a puertas interactivas y placas
    string color;           // Facilita la opcion de "Color Explosion"
    const string wallColor = "Black", floorColor = "Light Orange";
    JArray flags = new JArray(), spawnpoints = new JArray(), export = new JArray();
    Vector2Int doorCoord01 = new Vector2Int(), doorCoord02 = new Vector2Int(), plateCoord01 = new Vector2Int(), plateCoord02 = new Vector2Int(); 
    Vector2Int flagCoord = new Vector2Int(), startCoord = new Vector2Int();
    MazeGenerator storedMaze = new MazeGenerator(minGridSize);

    if (activeObject[2]) {  // Laberinto
      MazeGenerator generator = new MazeGenerator(levelSize);
      bool[,] maze = generator.GenerateMaze(new Vector2Int(1, 1));  // Empezamos la generación en (1, 1)
      storedMaze = generator;

      for (int x = 1; x < levelSize - 1; x++) {
        for (int y = 1; y < levelSize - 1; y++) {
          if (!maze[x, y]) {
            usedPositions.Add(new Vector2Int(x, y));
            color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "Black" : wallColor);
            export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(x, 1, y), new Quaternion(0 ,0 ,0 ,1), color));
            count++;
          } 
        }
      }
    } else {  // REGLAS PARA PAREDES INTERACTIVAS CUANDO NO HAY LABERINTO (despues de coche y laberinto)
      if (activeObject[3] && activeObject[4]) { // Si ambas paredes estan activas, hay que preparar las coordenadas conjuntamente
        doorCoord01 = generateRandomPos(2, levelSize - 3, 2, levelSize - 3);  // -3 Teniendo en cuenta que se puede generar una nueva pared tras ella
        doorCoord02 = generateRandomPos(doorCoord01.x + 1, levelSize - 2, doorCoord01.y + 1, levelSize - 2);
        plateCoord01 = generateRandomPos(1, doorCoord02.x, doorCoord01.y + 1, levelSize - 1);
        plateCoord02 = generateRandomPos(doorCoord02.x + 1, levelSize - 1, doorCoord01.y + 1, levelSize - 1);
      }

      if (activeObject[3]) {  // Pared Interactiva (horizontal)
        // Si activeObject[4] ya se han preparado las coordenadas
        if (!activeObject[4]) doorCoord01 = generateRandomPos(2, levelSize - 2, 2, levelSize - 2);  // -2 porque no puede ir en los laterales
        color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "White" : "Turquoise");
        export.Add(buildJSON(objects[3].name + " " + doorCounter.ToString(), new Vector3(doorCoord01.x, 1, doorCoord01.y), new Quaternion(0 ,0 ,0 ,1), color));

        if (!activeObject[4]) plateCoord01 = generateRandomPos(1, levelSize - 1, doorCoord01.y + 1, levelSize - 1);  // Placa para activar pared
        export.Add(buildJSON("Pressure Plate " + doorCounter.ToString(), new Vector3(plateCoord01.x, 1, plateCoord01.y), new Quaternion(0 ,0 ,0 ,1), "", doorCounter));
        doorCounter++;

        for (int i = 1; i < levelSize - 1; i++) { // Construimos paredes horizontales
          if (i == doorCoord01.x) continue;
          color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "Black" : wallColor); 
          export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(i, 1, doorCoord01.y), new Quaternion(0 ,0 ,0 ,1), color));
          usedPositions.Add(new Vector2Int(i, doorCoord01.y));
          count++;
        }

        maxFlag.y = doorCoord01.y;
        minStart.y = doorCoord01.y + 1;
      }

      if (activeObject[4]) {  // Place Presionable (pared interactiva vertical)
        // Si activeObject[3] ya se han preparado las coordenadas
        if (!activeObject[3]) doorCoord02 = generateRandomPos(2, levelSize - 2, 2, levelSize - 2);  // -2 porque no puede ir en los laterales
        color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "White" : "Red");
        export.Add(buildJSON(objects[3].name + " " + doorCounter.ToString(), new Vector3(doorCoord02.x, 1, doorCoord02.y), Quaternion.Euler(0f, 90f, 0f), color));

        if (!activeObject[3]) plateCoord02 = generateRandomPos(doorCoord02.x + 1, levelSize - 1, 1, levelSize - 1);  // Placa para activar pared 
        export.Add(buildJSON("Pressure Plate " + doorCounter.ToString(), new Vector3(plateCoord02.x, 1, plateCoord02.y), new Quaternion(0 ,0 ,0 ,1), "", doorCounter));
        doorCounter++;

        for (int i = 1; i < levelSize - 1; i++) { // Construimos paredes horizontales
          if (i == doorCoord02.y) continue;
          if (usedPositions.Contains(new Vector2Int(doorCoord02.x, i))) continue;
          color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "Black" : wallColor); 
          export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(doorCoord02.x, 1, i), new Quaternion(0 ,0 ,0 ,1), color));
          usedPositions.Add(new Vector2Int(doorCoord02.x, i));
          count++;
        }

        maxFlag.x = doorCoord02.x;
        minStart.x = doorCoord02.x + 1;
      }
    }

    if (activeObject[1]) {  // Punto de Partida / Coche
      // Posición aleatoria sin ocupar
      Vector2Int pos = startCoord = generateRandomPos(minStart.x, maxStart.x, minStart.y, maxStart.y);
      // Orientación aleatoria para el coche
      List<float> degrees = new List<float> { 0, 90, 180, 270 };
      System.Random rnd = new System.Random();
      int randomPos = rnd.Next(0, degrees.Count);
      // Lo añadimos al JSON
      spawnpoints.Add(buildJSON(objects[1].name + " 0", new Vector3(pos.x, 1, pos.y), Quaternion.Euler(0f, degrees[randomPos], 0f)));
    }

    if (activeObject[0]) {  // Objetivo / Bandera
      Vector2Int pos = new Vector2Int();
      if (activeObject[2]) {  // Si hay laberinto, colocamos el objetivo tan lejos como sea posible
        Vector2Int temp = storedMaze.FindFarthestReachablePoint(startCoord);
        pos = flagCoord = new Vector2Int(temp.x, temp.y);
        usedPositions.Add(pos);
      } else {
        pos = flagCoord = generateRandomPos(minFlag.x, maxFlag.x, minFlag.y, maxFlag.y);
      }
      flags.Add(buildJSON(objects[0].name + " 0", new Vector3(pos.x, 1, pos.y), new Quaternion(0 ,0 ,0 ,1)));
    }

    if (activeObject[2]) {  // REGLAS PARA PAREDES INTERACTIVAS CUANDO HAY LABERINTO (despuies de coche y objetivo)
      if (activeObject[3]) {  // Pared Interactiva (horizontal)
        List<Vector2Int> positions = new List<Vector2Int>();
        int counter = 0;
        while (positions.Count == 0 && counter < 10) {
          positions = getHorizWallMazeCoords(startCoord, flagCoord, true);
          counter++;
        }
        if (counter == 10) return "";
        doorCoord01 = positions[0];
        plateCoord01 = positions[1];

        color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "White" : "Turquoise");
        export.Add(buildJSON(objects[3].name + " " + doorCounter.ToString(), new Vector3(positions[0].x, 1, positions[0].y), new Quaternion(0 ,0 ,0 ,1), color));
        export.Add(buildJSON("Pressure Plate " + doorCounter.ToString(), new Vector3(positions[1].x, 1, positions[1].y), new Quaternion(0 ,0 ,0 ,1), "", doorCounter));
        doorCounter++;
      }
      if (activeObject[4]) {  // Pared Interactiva (vertical)
        List<Vector2Int> positions = new List<Vector2Int>();
        int counter = 0;
        while (positions.Count == 0 && counter < 10) {
          positions = getHorizWallMazeCoords(startCoord, flagCoord, false);
          counter++;
        }
        if (counter == 10) return "";
        doorCoord02 = positions[0];
        plateCoord02 = positions[1];

        color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "White" : "Red");
        export.Add(buildJSON(objects[3].name + " " + doorCounter.ToString(), new Vector3(positions[0].x, 1, positions[0].y), Quaternion.Euler(0f, 90f, 0f), color));
        export.Add(buildJSON("Pressure Plate " + doorCounter.ToString(), new Vector3(positions[1].x, 1, positions[1].y), new Quaternion(0 ,0 ,0 ,1), "", doorCounter));
        doorCounter++;
      }
    }

    if (activeObject[5]) {  // Camino
      List<Vector2Int> path = new List<Vector2Int>();
      List<Vector2Int> stops = new List<Vector2Int> { startCoord };

      if (activeObject[3] || activeObject[4]) { // Si hay una puerta, el camino debe pasar tanto por la puerta como la placa de presión
        if (activeObject[4]) {                  // Puerta vertical debe ir primero, para el caso de que las 2 esten puestas
          stops.Add(plateCoord02);
          stops.Add(doorCoord02);
        }
        if (activeObject[3]) {
          stops.Add(plateCoord01);
          stops.Add(doorCoord01);
        }
      } else if (!activeObject[2]) {            // Si es un laberinto no añadimos un punto de por medio
        stops.Add(generateRandomPos());         // Quitar esta linea para no tener punto por medio en generaciones normales
      }
      stops.Add(flagCoord);

      for (int i = 0; i < stops.Count - 1; i++) { // Combinando los caminos entre todos los elementos
        List<Vector2Int> tempPath = GeneratePathRecursive(stops[i], stops[i + 1]);
        if (tempPath.Count == 0) return "";       // Ha ocurrido un error
        if (i != 0) tempPath.RemoveAt(0);
        path.AddRange(tempPath);
      }
      
      for (int i = 0; i < path.Count; i++) { // Iteramos el camino para añadir el elemento correcto con la orientación correcta
        color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "White" : "Purple");
        Vector2Int pos = path[i];
        if (i == 0) { // Punto de Partida, pieza recta
          Vector2Int next = path[i + 1];
          Quaternion rotation = Quaternion.Euler(0f, 0f, 0f);
          if (next.x != pos.x) rotation = Quaternion.Euler(0f, 270f, 0f);
          export.Add(buildJSON("Straight Path " + count, new Vector3(pos.x, 1, pos.y), rotation, color));
        } else if (i != path.Count - 1) { // Secuencia principal entre punto de partida y final
          Vector2Int next = path[i + 1];
          Vector2Int prev = path[i - 1];
          if (prev.x != next.x && prev.y != next.y) {     // Pieza esquina           
            Quaternion rotation = getCornerRotation(prev, pos, next); // Obtener orientación de la esquina
            export.Add(buildJSON("Corner Path " + count, new Vector3(pos.x, 1, pos.y), rotation, color));
          } else {  // Pieza recta dentro de la secuencia
            Quaternion rotation = Quaternion.Euler(0f, 0f, 0f);
            if (next.x != pos.x) rotation = Quaternion.Euler(0f, 270f, 0f);
            export.Add(buildJSON("Straight Path " + count, new Vector3(pos.x, 1, pos.y), rotation, color));
          }
        } else {  // Objetivo, pieza recta
          Vector2Int prev = path[i - 1];
          Quaternion rotation = Quaternion.Euler(0f, 0f, 0f);
          if (prev.x != pos.x) rotation = Quaternion.Euler(0f, 270f, 0f);
          export.Add(buildJSON("Straight Path " + count, new Vector3(pos.x, 1, pos.y), rotation, color));
        }
        count++;  // Incrementar Contador
      }
    }

    for (int i = 0; i < levelSize; i++) { // Paredes externas y Suelo
      for (int j = 0; j < levelSize; j++) {
        if (i != 0 && i != levelSize - 1 && j != 0 && j != levelSize - 1) { // No hace falta meter el suelo en posiciones usadas
          color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "Black" : floorColor);
          export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(i, 0, j), new Quaternion(0 ,0 ,0 ,1), color));
          count++;
          continue;
        }
        color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "Black" : wallColor);
        export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(i, 1, j), new Quaternion(0 ,0 ,0 ,1), color));
        count++;
      }
    }

    result["spawnpoints"] = spawnpoints;
    result["flags"] = flags;
    result["level"] = export;

    return result.ToString(Formatting.Indented);
  }

  /// <summary>
  /// Construye la cabecera del fichero JSON del nivel a generar
  /// </summary>
  /// <returns></returns>
  private JObject buildEnv() {
    try {
      int temp = int.Parse(sizeText.text);
      levelSize = temp < minGridSize ? minGridSize : temp;
      if (activeObject[2] && levelSize % 2 == 0) levelSize += 1;  // Si tenemos laberinto, queremos que el tamaño sea impar
    } catch {
      Debug.LogWarning("Size Input was not a valid number");
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

  /// <summary>
  /// Construye un elemento del JSON
  /// </summary>
  /// <param name="name">Nombre del objeto</param>
  /// <param name="pos">Posicion dentro del nivel</param>
  /// <param name="rotation">Orientación dentro del nivel</param>
  /// <param name="colorSt">Color que debe tener</param>
  /// <param name="doorNumber">Numero de la puerta asociada (en el caso de placa)</param>
  /// <returns>El objeto en formato JSON con todas las opciones agregadas</returns>
  private JObject buildJSON(string name, Vector3 pos, Quaternion rotation, string colorSt = "White", int doorNumber = 0) {
    JObject json = new JObject();
    json["name"] = name;
    json["position"] = pos.ToString();
    json["rotation"] = rotation.ToString();

    JArray options = new JArray();
    if (name.Contains("Flag")) {                    // Opciones de Bandera
      JObject time = new JObject { { "Timed", "False" } };
      JObject seconds = new JObject { { "Seconds for win", "0" } };
      options.Add(time);
      options.Add(seconds);
    } else if (name.Contains("Pressure Plate")) {   // Opciones de Placa
      JObject lift = new JObject { { "Lifting Door " + doorNumber, "Toggle" } };
      options.Add(lift);
    } else if (!name.Contains("Spawnpoint")) {      // Opciones Regulares
      JObject color = new JObject { { "Color", colorSt } };
      options.Add(color);
    }
    json["options"] = options;

    return json;
  }

  #endregion

  #region Generacion Camino

  /// <summary>
  /// Prepara el entorno para llamar al metodo recursivo que encontrara un camino entre dos puntos
  /// </summary>
  /// <param name="start">Punto de partido</param>
  /// <param name="end">Posicion Objetiva</param>
  /// <returns>El camino encontrado entre los dos puntos, vacio si no se ha encontrado ninguno</returns>
  private List<Vector2Int> GeneratePathRecursive(Vector2Int start, Vector2Int end) {
    List<Vector2Int> result = new List<Vector2Int>();
    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

    bool found = TryStep(start, start, end, visited, result);
    if (!found) {
      Debug.LogWarning("No valid path found.");
      return new List<Vector2Int>();
    }
    return result;
  }

  /// <summary>
  /// Metodo recursivo para encontrar un camino valido entre dos puntos
  /// </summary>
  /// <param name="start">Posicion de partida</param>
  /// <param name="current">Posicion actual</param>
  /// <param name="end">Posicion objetiva</param>
  /// <param name="visited">Posiciones ya visitadas</param>
  /// <param name="path">El camino desarrollado hasta el momento</param>
  /// <returns>True si se ha conseguido encontrar el camino</returns>
  private bool TryStep(Vector2Int start, Vector2Int current, Vector2Int end, HashSet<Vector2Int> visited, List<Vector2Int> path) {
    // Falso si la posicion ya esta en uso y no es el punto de partida o el objetivo
    if (usedPositions.Contains(current) && !current.Equals(start) && !current.Equals(end) ) {
      return false;
    }
    // Falso si estamos en el borde de la rejilla
    if (current.x == 0 || current.x == levelSize - 1 || current.y == 0 || current.y == levelSize - 1) {
      return false;
    }
    // Falso si ya se ha explorado en esta busqueda
    if (visited.Contains(current)) return false;

    visited.Add(current);
    path.Add(current);

    if (current.Equals(end)) return true;   // Hemos llegado al objetivo

    // Vector con las distintas posiciones a las que nos podemos mover
    Vector2Int[] directions = new Vector2Int[] {
      Vector2Int.up,
      Vector2Int.down,
      Vector2Int.left,
      Vector2Int.right
    }.OrderBy(_ => UnityEngine.Random.value).ToArray();

    List<Vector2Int> posibilities = new List<Vector2Int>();

    // Probando cada dirección recursivamente
    foreach (var dir in directions) {
      Vector2Int next = new Vector2Int(current.x + dir.x, current.y + dir.y);

      int currentDist = Mathf.Abs(end.x - current.x) + Mathf.Abs(end.y - current.y);
      int nextDist = Mathf.Abs(end.x - next.x) + Mathf.Abs(end.y - next.y);

      if (nextDist <= currentDist) {
        if (TryStep(start, next, end, visited, path)) {
          if (!(activeObject[2] && (activeObject[3] || activeObject[4]))) usedPositions.Add(current);
          return true;
        }
      } else {
        posibilities.Add(next);
      }
    }

    // Backup si el camino "optimo" no existe
    foreach (Vector2Int test in posibilities) {
      if (TryStep(start, test, end, visited, path)) {
        if (!(activeObject[2] && (activeObject[3] || activeObject[4]))) usedPositions.Add(current);
        return true;
      }
    }

    // Backtrack
    path.RemoveAt(path.Count - 1);
    return false;
  }

  /// <summary>
  /// Metodo para averiguar la orientación que debe tener una esquina dependiendo de la pieza previa a ella y
  /// la que viene a continuación
  /// </summary>
  /// <param name="prev">Posición previa a la actual</param>
  /// <param name="pos">Posición con el que debemos tratar</param>
  /// <param name="next">Posición proxima a la actual</param>
  /// <returns>La rotacion correcta</returns>
  private Quaternion getCornerRotation(Vector2Int prev, Vector2Int pos, Vector2Int next) {
    Quaternion rotation = new Quaternion();

    if (prev.x + 1 == next.x && prev.y + 1 == next.y) {         // (+, +)
      if (pos.y + 1 == next.y) rotation = Quaternion.Euler(0f, 180f, 0f);
      else rotation = Quaternion.Euler(0f, 0f, 0f);;                         
    } else if (prev.x + 1 == next.x && prev.y - 1 == next.y) {  // (+ -)
      if (pos.y - 1 == next.y) rotation = Quaternion.Euler(0f, 90f, 0f);
      else rotation = Quaternion.Euler(0f, 270f, 0f);     
    } else if (prev.x - 1 == next.x && prev.y + 1 == next.y) {  // (-, +)
      if (pos.y + 1 == next.y) rotation = Quaternion.Euler(0f, 270f, 0f);
      else rotation = Quaternion.Euler(0f, 90f, 0f);                                 
    } else if (prev.x - 1 == next.x && prev.y - 1 == next.y) {  // (-, -)
      if (pos.y - 1 == next.y) rotation = Quaternion.Euler(0f, 0f, 0f);   
      else rotation = Quaternion.Euler(0f, 180f, 0f);;
    }

    return rotation;
  }

  /// <summary>
  /// Genera coordenadas para una pared interactiva y una placa dentro de un laberinto
  /// </summary>
  /// <param name="startPos">Posicion inicial</param>
  /// <param name="flagPos">Posicion Final</param>
  /// <param name="horizontal">True si es para la pared horizontal activeObject[3]</param>
  /// <returns>Coordinadas resultantes, [0] para pared interactiva y [1] para su placa</returns>
  private List<Vector2Int> getHorizWallMazeCoords(Vector2Int startPos, Vector2Int flagPos, bool horizontal, int otherX = 0, int otherY = 0) {
    List<Vector2Int> result = new List<Vector2Int>();
    List<Vector2Int> fullValidSpaces = getValidSpaces();
    List<Vector2Int> wallLimitedList = fullValidSpaces;
    System.Random rnd = new System.Random();
    bool found = false;

    while (!found && wallLimitedList.Count > 0) {      
      int wallPos = rnd.Next(0, wallLimitedList.Count);
      Vector2Int wallCandidate = wallLimitedList[wallPos];       
      int xCoord = wallCandidate.x, yCoord = wallCandidate.y;  
      wallLimitedList.RemoveAt(wallPos);   

      if (xCoord == 1 || xCoord == levelSize - 2 || yCoord == 1 || yCoord == levelSize - 2) continue; // No tener pared pegado al borde
      
      Vector2Int filled01 = horizontal ? new Vector2Int(xCoord + 1, yCoord) : new Vector2Int(xCoord, yCoord + 1);
      Vector2Int filled02 = horizontal ? new Vector2Int(xCoord - 1, yCoord) : new Vector2Int(xCoord, yCoord - 1);
      Vector2Int empty01 = horizontal ? new Vector2Int(xCoord, yCoord + 1) : new Vector2Int(xCoord + 1, yCoord);
      Vector2Int empty02 = horizontal ? new Vector2Int(xCoord, yCoord - 1) : new Vector2Int(xCoord - 1, yCoord);

      // La otra pared no debe ser adyacente
      if (!horizontal && (filled01.Equals(new Vector2Int(otherX, otherY)) || filled02.Equals(new Vector2Int(otherX, otherY)))) continue;

      if (!usedPositions.Contains(wallCandidate) 
          && usedPositions.Contains(filled01)
          && !filled01.Equals(startPos)
          && !filled01.Equals(flagPos)
          && usedPositions.Contains(filled02)
          && !filled02.Equals(startPos)
          && !filled02.Equals(flagPos)
          && !usedPositions.Contains(empty01)
          && !usedPositions.Contains(empty02)) {
        
        List<Vector2Int> plateLimitedList = fullValidSpaces;
        while (!found && plateLimitedList.Count > 0) {
          int platePos = rnd.Next(0, plateLimitedList.Count);
          Vector2Int plateCandidate = plateLimitedList[platePos];        
          plateLimitedList.RemoveAt(platePos);

          if (plateCandidate.Equals(wallCandidate)) continue;
          if (usedPositions.Contains(plateCandidate)) continue;

          if (pathExists(startPos, plateCandidate, wallCandidate)) {          
            result.Add(wallCandidate);
            usedPositions.Add(wallCandidate);
            result.Add(plateCandidate);
            usedPositions.Add(plateCandidate);
            found = true;
          } 
        }
      }
    }

    return result;
  }

  /// <summary>
  /// Calcula y retorna una lista con todos los espacios no ocupados
  /// </summary>
  /// <returns>Lista de espacios no ocupados</returns>
  private List<Vector2Int> getValidSpaces() {
    List<Vector2Int> result = new List<Vector2Int>();

    for (int i = 1; i < levelSize - 1; i++) {
      for (int j = 1; j < levelSize - 1; j++) {
        Vector2Int candidate = new Vector2Int(i, j);
        if (usedPositions.Contains(candidate)) continue;
        else result.Add(candidate);
      }
    }

    return result;
  }

  /// <summary>
  /// Averigua si existe un camino entre dos puntos
  /// </summary>
  /// <param name="start">Punto de partida</param>
  /// <param name="goal">Punto objetivo</param>
  /// <param name="blockedCell">Coordenada temporal que se concidera como bloqueado</param>
  /// <returns>True si el camino existe</returns>
  private bool pathExists(Vector2Int start, Vector2Int goal, Vector2Int blockedCell) {
    Queue<Vector2Int> frontier = new Queue<Vector2Int>();
    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    frontier.Enqueue(start);
    visited.Add(start);

    Vector2Int[] dirs = new Vector2Int[] {
      Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    while (frontier.Count > 0) {
      Vector2Int current = frontier.Dequeue();
      if (current.Equals(goal)) return true;

      foreach (var dir in dirs) {
        Vector2Int neighbor = new Vector2Int(current.x + dir.x, current.y + dir.y);

        if (neighbor.x == 0 || neighbor.x == levelSize - 1 || neighbor.y == 0 || neighbor.y == levelSize - 1) continue;
        if (neighbor.Equals(blockedCell)) continue;
        if (usedPositions.Contains(neighbor)) continue;
        if (visited.Contains(neighbor)) continue;
        
        visited.Add(neighbor);
        frontier.Enqueue(neighbor);
      }
    }

    return false;
  }

  #endregion

  #region Metodos Aux

  /// <summary>
  /// Genera una cordenada aleatoria en el nivel que no este en uso y que cumnpla con las restricciones de posición
  /// </summary>
  /// <param name="minX">Minimo valor que debe tener X, puede ser igual</param>
  /// <param name="maxX">Maximo valor que debe tener X</param>
  /// <param name="minY">Minimo valor que debe tener Y, puede ser igual</param>
  /// <param name="maxY">Maximo valor que debe tener Y</param>
  /// <returns>Coordenada generada</returns>
  private Vector2Int generateRandomPos(int minX = 1, int maxX = -1, int minY = 1, int maxY = -1) {
    if (maxX == -1) maxX = levelSize - 1;
    if (maxY == -1) maxY = levelSize - 1;
    System.Random rnd = new System.Random();
    bool found = false;

    while (!found) {      
      int xCoord, yCoord;
      xCoord = (minX == maxX) ? minX : rnd.Next(minX, maxX);  // En el caso de 4, n >= 1 & n < 4
      yCoord = (minY == maxY) ? minY : rnd.Next(minY, maxY);
      Vector2Int generated = new Vector2Int(xCoord, yCoord);
      
      if (!usedPositions.Contains(generated)) {
        usedPositions.Add(new Vector2Int(xCoord, yCoord));
        found = true;
      }
    }

    return usedPositions.Last();
  }

  /// <summary>
  /// Genera un color aleatorio en base a la lista de colores que tenemos guardado
  /// </summary>
  /// <returns>String del color elegido</returns>
  private string generateRandomColor() {
    System.Random rnd = new System.Random();
    int val = rnd.Next(0, colorList.Count);
    return colorList[val];
  }

  /// <summary>
  /// Elige entre el color blanco o negro aleatoriamente
  /// </summary>
  /// <returns>String del color elegido</returns>
  private string generateBlackOrWhite() {
    System.Random rnd = new System.Random();
    int val = rnd.Next(0, 2);   // Devolvera 0 o 1
    return (val == 0) ? "Black" : "White";
  }

  /// <summary>
  /// Asigna las posiciones minimas/maximas de nuestros puntos iniciales y finales a sus valores originales
  /// </summary>
  private void setupCoordValues() {
    minFlag = new Vector2Int(1, 1);
    maxFlag = new Vector2Int(levelSize - 1, levelSize - 1);
    minStart = new Vector2Int(1, 1);
    maxStart = new Vector2Int(levelSize - 1, levelSize - 1);
  }

  /// <summary>
  /// Setter: Copia una lista pasada con los objetos activados, que debemos incluir en nuestro nivel generado
  /// </summary>
  /// <param name="list">Lista bool de objetos activos, las posiciones son igual que el atributo "objects"</param>
  public void setActiveObjects (List<bool> list) {
    activeObject = list;
  }

  /// <summary>
  /// Getter: Deveulve la cantidad de objetos que tenemos a nuestra disposición
  /// </summary>
  /// <returns>Int de la cantidad de objetos</returns>
  public int objectAmount() {
    return objects.Count;
  }

  #endregion

}
