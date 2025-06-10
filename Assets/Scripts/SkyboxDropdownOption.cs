using TMPro;
using UnityEngine;

public class SkyboxDropdownOption : MonoBehaviour
{
  void Start()
  {
    Transform labelTF = transform.Find("Item Label"); 
    TextMeshProUGUI label = labelTF.GetComponent<TextMeshProUGUI>();
    label.text = this.name.Split(new[] { ": " }, System.StringSplitOptions.None)[1];
  }
}
