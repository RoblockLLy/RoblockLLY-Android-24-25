/**
* Universidad de La Laguna
* Proyecto: Roblockly-Android
* Autor: Thomas Edward Bradley
* Email: alu0101408248@ull.edu.es
* Fecha: 30/06/2025
* Descripcion: 
*/

using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BootManager : MonoBehaviour {
  public Animation roblockllyLogo;
  public Animation ullLogo;

  void Start() {
    StartCoroutine(PlayAnimationsInSequence());
  }

  /// <summary>
  /// 
  /// </summary>
  private IEnumerator PlayAnimationsInSequence() {
    roblockllyLogo.Play();
    yield return new WaitWhile(() => roblockllyLogo.isPlaying);

    ullLogo.Play();
    yield return new WaitWhile(() => ullLogo.isPlaying);

    yield return new WaitForSeconds(0.5f);    // Buffer
    SceneManager.LoadScene(1);
  }
}
