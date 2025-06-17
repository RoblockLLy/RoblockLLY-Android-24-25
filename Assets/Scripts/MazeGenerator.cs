/**
* Universidad de La Laguna
* Proyecto: Roblockly-Android
* Autor: Thomas Edward Bradley
* Email: alu0101408248@ull.edu.es
* Fecha: 15/06/2025
* Descripcion: Clase encargada de generar un laberinto cuadrado de un tamaño previsto
*              con un punto inicial y final predeterminados
*/

using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator {
  
  #region Atributos
  
  /// <summary>
  /// Tamaño NxN del laberinto
  /// </summary>
  public int size;
  /// <summary>
  /// Tamaño excluyendo los bloques separadores, tiene la formula size * 2 + 1
  /// Size siempre debe ser un numero impar y las posiciones inicial / final deben ser
  /// par, esto lo aseguramos en el código de JSON generator
  /// </summary>
  public int logicalSize;   // Si = 5, size = 11 (grid 11x11)
  /// <summary>
  /// Array que almacena si una posicion dentro del laberinto esta ocupada o no
  /// </summary>
  public bool[,] maze;
  /// <summary>
  /// Objeto para generar valores aleatorios
  /// </summary>
  private System.Random rng = new System.Random();

  /// <summary>
  /// Array de Vectores con las dirrecciones que uno puede tomar al moverse por el laberinto
  /// </summary>
  private Vector2Int[] directions = new Vector2Int[] {
    new Vector2Int(0, 1),   // up
    new Vector2Int(0, -1),  // down
    new Vector2Int(-1, 0),  // left
    new Vector2Int(1, 0)    // right
  };

  #endregion

  #region Metodos Principales

  /// <summary>
  /// Constructor de la clase
  /// </summary>
  /// <param name="size">Tamaño del grid</param>
  public MazeGenerator(int size) {
    this.size = size;
    this.logicalSize = (size - 1) / 2;
    maze = new bool[size, size];
  }

  /// <summary>
  /// çPrepara el entorno del laberinto, llama su creación, inserta loops aleatorios y
  /// se asegura de que las posiciones importantes se mantengan abiertos
  /// </summary>
  /// <param name="startCell">Posicion Inicial del laberinto (1, 1)</param>
  /// <param name="loopChance">Porcentaje de que hayan conexiones entre caminos</param>
  /// <returns></returns>
  public bool[,] GenerateMaze(Vector2Int startCell, float loopChance = 0.05f) {
    int size = logicalSize * 2 + 1;

    // Llenamos todo el grid con paredes, asignando el valor false
    for (int x = 0; x < size; x++) {
      for (int y = 0; y < size; y++) {
        maze[x, y] = false;
      }        
    }

    var visited = new HashSet<Vector2Int>();
    CarveMaze(startCell, visited);
    AddRandomLoops(loopChance);

    return maze;
  }

  /// <summary>
  /// Genera el laberinto
  /// </summary>
  /// <param name="current">La posición actual</param>
  /// <param name="visited">Lista de posiciones ya visitadas</param>
  private void CarveMaze(Vector2Int current, HashSet<Vector2Int> visited) {
    visited.Add(current);
    maze[current.x, current.y] = true;

    Shuffle(directions);

    foreach (var dir in directions) {
      Vector2Int next = current + dir * 2;
      Vector2Int between = current + dir;

      if (IsInInnerBounds(next) && !visited.Contains(next)) {
        maze[between.x, between.y] = true;  // carve wall between
        CarveMaze(next, visited);
      }
    }
  }

  /// <summary>
  /// Genera loops aleatorios dentro del laberinto generado
  /// </summary>
  /// <param name="chance">Probabilidad (en decimales) de que haya un loop</param>
  private void AddRandomLoops(float chance = 0.05f) {
    int size = logicalSize * 2 + 1;

    for (int x = 1; x < size - 1; x++) {
      for (int y = 1; y < size - 1; y++) {
        if (x % 2 == 1 || y % 2 == 1) continue; // Only look at wall positions

        if (!maze[x, y] && rng.NextDouble() < chance) {
          // Only open wall if it connects two different paths
          int connections = 0;
          if (maze[x + 1, y]) connections++;
          if (maze[x - 1, y]) connections++;
          if (maze[x, y + 1]) connections++;
          if (maze[x, y - 1]) connections++;
          if (connections == 2) maze[x, y] = true;
        }
      }
    }
  }

  #endregion

  #region Metodos Aux

  /// <summary>
  /// Asegura que seguimos dentro del espacio permitido por las restricciones del laberinto
  /// </summary>
  /// <param name="pos">Posicion que queremos analizar</param>
  /// <returns>True si la posiciones esta dentro de los limites</returns>
  private bool IsInInnerBounds(Vector2Int pos) {
    // La capa exterior se va llenar con paredes, las comparaciones no son con igual
    int downLimit = 0;
    int upLimit = size - 1;

    return pos.x > downLimit && pos.x < upLimit && pos.y > downLimit && pos.y < upLimit;
  }

  /// <summary>
  /// Mezcla un array de Vectores 2D
  /// </summary>
  /// <param name="array">Array a mezclar aleatoriamente</param>
  private void Shuffle(Vector2Int[] array) {
    for (int i = array.Length - 1; i > 0; i--) {
      int j = rng.Next(i + 1);
      var tmp = array[i];
      array[i] = array[j];
      array[j] = tmp;
    }
  }

  #endregion

}
