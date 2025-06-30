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
using UnityEngine;

/// <summary>
/// Struct que guarda la información pertinente a una Coordenada en un espacio 2D
/// </summary>
struct Coord {
  public int xVal;
  public int yVal;
};

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
  //  [2] Wall
  //  [3] Interactable Wall
  //  [4] Pressure Plate
  //  [5] Path
  //  [6] Color (Prioridad sobre [7])
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
  private List<Coord> usedPositions = new List<Coord>();

  /// <summary>
  /// Coordinada minimo de la bandera
  /// </summary>
  private Coord minFlag;
  /// <summary>
  /// Coordinada maximo de la bandera
  /// </summary>
  private Coord maxFlag;
  /// <summary>
  /// Coordinada minimo del punto de partida
  /// </summary>
  private Coord minStart;
  /// <summary>
  /// Coordinada maximo del punto de partida
  /// </summary>
  private Coord maxStart;

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
    string color;           // Facilita la opcion de "Color Explosion"
    const string wallColor = "Black", floorColor = "Light Orange";
    JArray flags = new JArray(), spawnpoints = new JArray(), export = new JArray();
    Coord doorCoord01 = new Coord(), doorCoord02 = new Coord(), plateCoord01 = new Coord(), plateCoord02 = new Coord(); 
    Coord flagCoord = new Coord(), startCoord = new Coord();

    if (activeObject[2]) {  // Laberinto
      MazeGenerator generator = new MazeGenerator(levelSize);
      bool[,] maze = generator.GenerateMaze(new Vector2Int(1, 1));  // Empezamos la generación en (1, 1)

      for (int x = 1; x < levelSize - 1; x++) {
        for (int y = 1; y < levelSize - 1; y++) {
          if (!maze[x, y]) {
            usedPositions.Add(new Coord { xVal = x, yVal = y } );
            color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "Black" : wallColor);
            export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(x, 1, y), new Quaternion(0 ,0 ,0 ,1), color));
            count++;
          } 
        }
      }
    }

    if (activeObject[3] && activeObject[4]) { // Si ambas paredes estan activas, hay que preparar las coordenadas conjuntamente
      doorCoord01 = generateRandomPos(2, levelSize - 3, 2, levelSize - 3);  // -3 Teniendo en cuenta que se puede generar una nueva pared tras ella
      doorCoord02 = generateRandomPos(doorCoord01.xVal + 1, levelSize - 2, doorCoord01.yVal + 1, levelSize - 2);
      plateCoord01 = generateRandomPos(1, doorCoord02.xVal, doorCoord01.yVal + 1, levelSize - 1);
      plateCoord02 = generateRandomPos(doorCoord02.xVal + 1, levelSize - 1, doorCoord01.yVal + 1, levelSize - 1);
    }

    if (activeObject[3]) {  // Pared Interactiva (horizontal)
      // Si activeObject[4] ya se han preparado las coordenadas
      if (!activeObject[4]) doorCoord01 = generateRandomPos(2, levelSize - 2, 2, levelSize - 2);  // -2 porque no puede ir en los laterales
      color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "White" : "Turquoise");
      export.Add(buildJSON(objects[3].name + " " + count.ToString(), new Vector3(doorCoord01.xVal, 1, doorCoord01.yVal), new Quaternion(0 ,0 ,0 ,1), color));

      if (!activeObject[4]) plateCoord01 = generateRandomPos(1, levelSize - 1, doorCoord01.yVal + 1, levelSize - 1);  // Placa para activar pared
      export.Add(buildJSON("Pressure Plate " + count.ToString(), new Vector3(plateCoord01.xVal, 1, plateCoord01.yVal), new Quaternion(0 ,0 ,0 ,1), "", count));

      for (int i = 1; i < levelSize - 1; i++) { // Construimos paredes horizontales
        if (i == doorCoord01.xVal) continue;
        color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "Black" : wallColor); 
        export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(i, 1, doorCoord01.yVal), new Quaternion(0 ,0 ,0 ,1), color));
        usedPositions.Add(new Coord { xVal = i, yVal = doorCoord01.yVal });
        count++;
      }

      maxFlag.yVal = doorCoord01.yVal;
      minStart.yVal = doorCoord01.yVal + 1;
    }

    if (activeObject[4]) {  // Place Presionable (pared interactiva vertical)
      // Si activeObject[3] ya se han preparado las coordenadas
      if (!activeObject[3]) doorCoord02 = generateRandomPos(2, levelSize - 2, 2, levelSize - 2);  // -2 porque no puede ir en los laterales
      color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "White" : "Red");
      export.Add(buildJSON(objects[3].name + " " + count.ToString(), new Vector3(doorCoord02.xVal, 1, doorCoord02.yVal), Quaternion.Euler(0f, 90f, 0f), color));

      if (!activeObject[3]) plateCoord02 = generateRandomPos(doorCoord02.xVal + 1, levelSize - 1, 1, levelSize - 1);  // Placa para activar pared 
      export.Add(buildJSON("Pressure Plate " + count.ToString(), new Vector3(plateCoord02.xVal, 1, plateCoord02.yVal), new Quaternion(0 ,0 ,0 ,1), "", count));

      for (int i = 1; i < levelSize - 1; i++) { // Construimos paredes horizontales
        if (i == doorCoord02.yVal) continue;
        if (usedPositions.Contains(new Coord() { xVal = doorCoord02.xVal, yVal = i })) continue;
        color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "Black" : wallColor); 
        export.Add(buildJSON("Full Block" + " " + count.ToString(), new Vector3(doorCoord02.xVal, 1, i), new Quaternion(0 ,0 ,0 ,1), color));
        usedPositions.Add(new Coord { xVal = doorCoord02.xVal, yVal = i });
        count++;
      }

      maxFlag.xVal = doorCoord02.xVal;
      minStart.xVal = doorCoord02.xVal + 1;
    }

    if (activeObject[0]) {  // Objetivo / Bandera
      Coord pos = flagCoord = generateRandomPos(minFlag.xVal, maxFlag.xVal, minFlag.yVal, maxFlag.yVal);
      flags.Add(buildJSON(objects[0].name + " 0", new Vector3(pos.xVal, 1, pos.yVal), new Quaternion(0 ,0 ,0 ,1)));
    }

    if (activeObject[1]) {  // Punto de Partida / Coche
      // Posición aleatoria sin ocupar
      Coord pos = startCoord = generateRandomPos(minStart.xVal, maxStart.xVal, minStart.yVal, maxStart.yVal);
      // Orientación aleatoria para el coche
      List<float> degrees = new List<float> { 0, 90, 180, 270 };
      System.Random rnd = new System.Random();
      int randomPos = rnd.Next(0, degrees.Count);
      // Lo añadimos al JSON
      spawnpoints.Add(buildJSON(objects[1].name + " 0", new Vector3(pos.xVal, 1, pos.yVal), Quaternion.Euler(0f, degrees[randomPos], 0f)));
    }

    if (activeObject[5]) {  // Camino
      List<Coord> path = new List<Coord>();
      List<Coord> stops = new List<Coord> { startCoord };

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
        List<Coord> tempPath = GeneratePathRecursive(stops[i], stops[i + 1]);
        if (tempPath.Count == 0) return "";       // Ha ocurrido un error
        if (i != 0) tempPath.RemoveAt(0);
        path.AddRange(tempPath);
      }
      
      for (int i = 0; i < path.Count; i++) { // Iteramos el camino para añadir el elemento correcto con la orientación correcta
        color = activeObject[6] ? (activeObject[7] ? generateBlackOrWhite() : generateRandomColor()) : (activeObject[7] ? "White" : "Purple");
        Coord pos = path[i];
        if (i == 0) { // Punto de Partida, pieza recta
          Coord next = path[i + 1];
          Quaternion rotation = Quaternion.Euler(0f, 0f, 0f);
          if (next.xVal != pos.xVal) rotation = Quaternion.Euler(0f, 270f, 0f);
          export.Add(buildJSON("Straight Path " + count, new Vector3(pos.xVal, 1, pos.yVal), rotation, color));
        } else if (i != path.Count - 1) { // Secuencia principal entre punto de partida y final
          Coord next = path[i + 1];
          Coord prev = path[i - 1];
          if (prev.xVal != next.xVal && prev.yVal != next.yVal) {     // Pieza esquina           
            Quaternion rotation = getCornerRotation(prev, pos, next); // Obtener orientación de la esquina
            export.Add(buildJSON("Corner Path " + count, new Vector3(pos.xVal, 1, pos.yVal), rotation, color));
          } else {  // Pieza recta dentro de la secuencia
            Quaternion rotation = Quaternion.Euler(0f, 0f, 0f);
            if (next.xVal != pos.xVal) rotation = Quaternion.Euler(0f, 270f, 0f);
            export.Add(buildJSON("Straight Path " + count, new Vector3(pos.xVal, 1, pos.yVal), rotation, color));
          }
        } else {  // Objetivo, pieza recta
          Coord prev = path[i - 1];
          Quaternion rotation = Quaternion.Euler(0f, 0f, 0f);
          if (prev.xVal != pos.xVal) rotation = Quaternion.Euler(0f, 270f, 0f);
          export.Add(buildJSON("Straight Path " + count, new Vector3(pos.xVal, 1, pos.yVal), rotation, color));
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
  private List<Coord> GeneratePathRecursive(Coord start, Coord end) {
    List<Coord> result = new List<Coord>();
    HashSet<Coord> visited = new HashSet<Coord>();

    bool found = TryStep(start, start, end, visited, result);
    if (!found) {
      Debug.LogWarning("No valid path found.");
      return new List<Coord>();
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
  private bool TryStep(Coord start, Coord current, Coord end, HashSet<Coord> visited, List<Coord> path) {
    // Falso si la posicion ya esta en uso y no es el punto de partida o el objetivo
    if (usedPositions.Contains(current) && !current.Equals(start) && !current.Equals(end) ) {
      return false;
    }
    // Falso si estamos en el borde de la rejilla
    if (current.xVal == 0 || current.xVal == levelSize - 1 || current.yVal == 0 || current.yVal == levelSize - 1) {
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

    List<Coord> posibilities = new List<Coord>();

    // Probando cada dirección recursivamente
    foreach (var dir in directions) {
      Coord next = new Coord { xVal = current.xVal + dir.x, yVal = current.yVal + dir.y };

      int currentDist = Mathf.Abs(end.xVal - current.xVal) + Mathf.Abs(end.yVal - current.yVal);
      int nextDist = Mathf.Abs(end.xVal - next.xVal) + Mathf.Abs(end.yVal - next.yVal);

      if (nextDist <= currentDist) {
        if (TryStep(start, next, end, visited, path)) {
          usedPositions.Add(current);
          return true;
        }
      } else {
        posibilities.Add(next);
      }
    }

    // Backup si el camino "optimo" no existe
    foreach (Coord test in posibilities) {
      if (TryStep(start, test, end, visited, path)) {
        usedPositions.Add(current);
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
  private Quaternion getCornerRotation(Coord prev, Coord pos, Coord next) {
    Quaternion rotation = new Quaternion();

    if (prev.xVal + 1 == next.xVal && prev.yVal + 1 == next.yVal) {         // (+, +)
      if (pos.yVal + 1 == next.yVal) rotation = Quaternion.Euler(0f, 180f, 0f);
      else rotation = Quaternion.Euler(0f, 0f, 0f);;                         
    } else if (prev.xVal + 1 == next.xVal && prev.yVal - 1 == next.yVal) {  // (+ -)
      if (pos.yVal - 1 == next.yVal) rotation = Quaternion.Euler(0f, 90f, 0f);
      else rotation = Quaternion.Euler(0f, 270f, 0f);     
    } else if (prev.xVal - 1 == next.xVal && prev.yVal + 1 == next.yVal) {  // (-, +)
      if (pos.yVal + 1 == next.yVal) rotation = Quaternion.Euler(0f, 270f, 0f);
      else rotation = Quaternion.Euler(0f, 90f, 0f);                                 
    } else if (prev.xVal - 1 == next.xVal && prev.yVal - 1 == next.yVal) {  // (-, -)
      if (pos.yVal - 1 == next.yVal) rotation = Quaternion.Euler(0f, 0f, 0f);   
      else rotation = Quaternion.Euler(0f, 180f, 0f);;
    }

    return rotation;
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
  private Coord generateRandomPos(int minX = 1, int maxX = -1, int minY = 1, int maxY = -1) {
    if (maxX == -1) maxX = levelSize - 1;
    if (maxY == -1) maxY = levelSize - 1;
    System.Random rnd = new System.Random();
    bool found = false;

    while (!found) {      
      int xCoord, yCoord;
      xCoord = (minX == maxX) ? minX : rnd.Next(minX, maxX);  // En el caso de 4, n >= 1 & n < 4
      yCoord = (minY == maxY) ? minY : rnd.Next(minY, maxY);
      Coord generated = new Coord { xVal = xCoord, yVal = yCoord };
      
      if (!usedPositions.Contains(generated)) {
        usedPositions.Add(new Coord { xVal = xCoord, yVal = yCoord });
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
    minFlag = new Coord { xVal = 1, yVal = 1 };
    maxFlag = new Coord { xVal = levelSize - 1, yVal = levelSize - 1 };
    minStart = new Coord { xVal = 1, yVal = 1 };
    maxStart = new Coord { xVal = levelSize - 1, yVal = levelSize - 1 };
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
