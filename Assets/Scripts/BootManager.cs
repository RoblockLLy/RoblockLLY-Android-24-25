/**
* Universidad de La Laguna
* Proyecto: Roblockly-Android
* Autor: Thomas Edward Bradley
* Email: alu0101408248@ull.edu.es
* Fecha: 30/06/2025
* Descripcion: Clase encargado de las animaciones en la Boot Sequence
*/

using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BootManager : MonoBehaviour {
  public List<Animation> animations;

  /// <summary>
  /// Empezamos las animaciones al abrir la escena de boot
  /// </summary>
  void Start() {
    StartCoroutine(PlayAnimationsInSequence());
  }

  /// <summary>
  /// Corutina encargado de ejecutar todas las animaciones
  /// </summary>
  private IEnumerator PlayAnimationsInSequence() {
    foreach (Animation anim in animations) {
      anim.Play();
      yield return new WaitWhile(() => anim.isPlaying);
    }

    yield return new WaitForSeconds(0.2f);    // Buffer
    SceneManager.LoadScene(1);
  }
}
