using UnityEngine;

public enum ItemType
{
    Resource,
    Weapon,
    Tool
}

public enum DamageType
{
    Normal,
    Bug
}

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    [Header("Item Properties")]
    public string itemName;
    public Sprite sprite;
    public ItemType itemType;
    public DamageType damageType;
    public float damage;
}
