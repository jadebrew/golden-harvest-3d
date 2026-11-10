using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "Scriptable Objects/Recipe")]
public class Recipe : ScriptableObject
{
    [Tooltip("Items consumed, with amounts.")]
    [SerializeField] public Dictionary<Item, int> input = new();
    [Tooltip("Items produced, with amounts.")]
    [SerializeField] public Dictionary<Item, int> output = new();

    public AuraType requiredAura;
}
