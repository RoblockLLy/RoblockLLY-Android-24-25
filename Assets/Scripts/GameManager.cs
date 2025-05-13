using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
  public void CargaEscena() {
    Scene escena = SceneManager.GetActiveScene();
    
    switch (escena.buildIndex) {
      case 0:
        SceneManager.LoadScene(1);
        break;
      case 1:
        SceneManager.LoadScene(0);
        break;
    }
  }
}
