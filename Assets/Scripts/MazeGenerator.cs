using System;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator {
  public int width, height;
  public bool[,] maze;
  private System.Random rng = new System.Random();

  private Vector2Int[] directions = new Vector2Int[] {
    new Vector2Int(0, 1),   // up
    new Vector2Int(0, -1),  // down
    new Vector2Int(-1, 0),  // left
    new Vector2Int(1, 0)    // right
  };

  public MazeGenerator(int width, int height) {
    this.width = width;
    this.height = height;
    maze = new bool[width, height]; // false = wall, true = path
  }

  public bool[,] GenerateMaze(Vector2Int start, Vector2Int end) {
    // Step 1: Fill everything with walls (false)
    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        maze[x, y] = false;
      }          
    }

    // Step 2: Create a guaranteed path from start to end
    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    CarvePathDFS(start, end, visited);

    return maze;
  }

  private bool CarvePathDFS(Vector2Int current, Vector2Int target, HashSet<Vector2Int> visited) {
    visited.Add(current);
    maze[current.x, current.y] = true;

    if (current == target) {
      return true;
    }
        
    Shuffle(directions);

    foreach (var dir in directions) {
      Vector2Int next = current + dir;

      if (IsInInnerBounds(next) && !visited.Contains(next)) {
        if (CarvePathDFS(next, target, visited)) {
          return true;
        }
      }
    }

    // Dead end, backtrack
    return false;
  }

  private bool IsInInnerBounds(Vector2Int pos) {
      return pos.x > 0 && pos.x < width - 1 && pos.y > 0 && pos.y < height - 1;
  }

  private void Shuffle(Vector2Int[] array) {
    for (int i = array.Length - 1; i > 0; i--) {
      int j = rng.Next(i + 1);
      var tmp = array[i];
      array[i] = array[j];
      array[j] = tmp;
    }
  }
}
