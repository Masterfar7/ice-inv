using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;

    [SerializeField] private Color activeBackgroundColor = new Color32(0x8B, 0x5C, 0xF6, 0xFF);
    [SerializeField] private Color idleBackgroundColor = new Color32(0x2A, 0x2A, 0x3A, 0xFF);
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color idleTextColor = new Color32(0xB0, 0xB0, 0xC0, 0xFF);

    public Button Button => button;

    public void SetVisualState(bool isActive)
    {
        background.color = isActive ? activeBackgroundColor : idleBackgroundColor;
        label.color = isActive ? activeTextColor : idleTextColor;
    }
}
