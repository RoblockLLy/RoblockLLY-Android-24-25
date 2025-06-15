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

  #region Metodos

  /// <summary>
  /// Constructor de la clase
  /// </summary>
  /// <param name="size">Tamaño NxN que va tener el laberinto</param>
  public MazeGenerator(int size) {
    this.size = size;
    maze = new bool[size, size]; // false = wall, true = path
  }

  /// <summary>
  /// Genera el laberinto entre dos puntos, asegurando que se puede llegar del uno al otro
  /// </summary>
  /// <param name="start">Punto de partida</param>
  /// <param name="end">Punto final</param>
  /// <returns>Laberinto generado</returns>
  public bool[,] GenerateMaze(Vector2Int start, Vector2Int end) {
    // Paso 1: Llenamos todo el laberinto con muros (false)
    for (int x = 0; x < size; x++) {
      for (int y = 0; y < size; y++) {
        maze[x, y] = false;
      }          
    }

    // Paso 2: Generamos un camino entre el punto de partida y el final
    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    CarvePathDFS(start, end, visited);

    // Paso 3: Devolvemos el resultado
    return maze;
  }

  /// <summary>
  /// Algoritmo DFS que trata de encontrar un camino válido entre los dos puntos
  /// </summary>
  /// <param name="current">Posición actual</param>
  /// <param name="target">Posición destino</param>
  /// <param name="visited">Posiciones Visitadas</param>
  /// <returns>True si se ha encontrado un camino valido</returns>
  private bool CarvePathDFS(Vector2Int current, Vector2Int target, HashSet<Vector2Int> visited) {
    visited.Add(current);
    maze[current.x, current.y] = true;

    if (current == target) return true;
        
    Shuffle(directions);

    foreach (var dir in directions) {
      Vector2Int next = current + dir;

      if (IsInInnerBounds(next) && !visited.Contains(next)) {
        if (CarvePathDFS(next, target, visited)) {
          return true;
        }
      }
    }

    // No hay camino por esta avenida
    return false;
  }

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
