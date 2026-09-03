using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Item", fileName = "Item")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string title;
    [TextArea, SerializeField] private string description;
    [SerializeField, Min(0)] private int price;
    [SerializeField] private Sprite icon;

    public string Id => id;
    public string Title => title;
    public string Description => description;
    public int Price => price;
    public Sprite Icon => icon;
}
