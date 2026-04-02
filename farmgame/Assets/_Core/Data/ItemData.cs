using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    public string ItemId;
    public string ItemName;
    public Sprite Icon;
    public int MaxStack = 99;
}