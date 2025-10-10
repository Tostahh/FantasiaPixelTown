using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceUI : MonoBehaviour
{
    [Header("UI Text Elements")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI materialsText;
    public TextMeshProUGUI manaText;

    private void OnGUI()
    {
        goldText.text = ResourceManager.Instance.gold.ToString();
        materialsText.text = ResourceManager.Instance.materials.ToString();
        manaText.text = ResourceManager.Instance.mana.ToString();
    }
}
