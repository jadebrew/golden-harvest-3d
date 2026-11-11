using UnityEngine;

public enum ItemType
{
    Resource,
    Weapon,
    Tool,
    Place,
    Consumable
}

public enum DamageType
{
    Normal,
    Axe,
    Sword,
    Pickaxe,
    Sickle,
    Bug
}

public enum AnimationType
{
    Pull,
    Sweep,
    Consume
}

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    [Header("Item Properties")]
    public string itemName;
    public Sprite sprite;
    public GameObject model;
    public GameObject worldModel;
    public ItemType itemType;
    public DamageType damageType;
    public AnimationType animationType;
    public float damage;
}
