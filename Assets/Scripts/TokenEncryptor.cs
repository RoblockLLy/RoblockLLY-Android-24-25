using UnityEngine;

public class TokenEncryptor : MonoBehaviour {
  
  [SerializeField] [Tooltip("Encrypted Token")] [TextArea]
  public string encryptedToken = "fill-me";

  /// <summary>
  /// El numero que se suma / resta en el proceso de encripción
  /// </summary>
  private int encryptionInt = 3;
  
  /// <summary>
  /// Espacio para hacer tests y ver los resultados de encriptar / desencriptar
  /// </summary>
  private void Start() {
    // Debug.Log("Encrypted -> " + EncryptString());
    // Debug.Log("Decrypted -> " + DecryptString());
  }

  /// <summary>
  /// Encripta el token guardado como atributos
  /// </summary>
  /// <returns>El token encriptado</returns>
  public string EncryptString() {
    string input = encryptedToken;
    System.Text.StringBuilder builder = new System.Text.StringBuilder();

    foreach (char c in input) {
      char shifted = (char)(c + encryptionInt);
      builder.Append(shifted);
    }

    int randomDigit = Random.Range(0, 10); // Random digit from 0–9
    builder.Append(randomDigit.ToString());

    return builder.ToString();
  }

  /// <summary>
  /// Desencripta el token guardado como atributo
  /// </summary>
  /// <returns>El token desencriptado</returns>
  public string DecryptString() {
    string encrypted = encryptedToken;
    
    if (string.IsNullOrEmpty(encrypted) || encrypted.Length < 2) {
      Debug.LogWarning("Encrypted string too short to decrypt.");
      return "";
    }

    string withoutDigit = encrypted.Substring(0, encrypted.Length - 1);
    System.Text.StringBuilder builder = new System.Text.StringBuilder();

    foreach (char c in withoutDigit) {
      char shiftedBack = (char)(c - encryptionInt);
      builder.Append(shiftedBack);
    }

    return builder.ToString();
  }

}
