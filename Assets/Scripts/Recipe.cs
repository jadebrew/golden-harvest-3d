using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[CreateAssetMenu(fileName = "Recipe", menuName = "Scriptable Objects/Recipe")]
public class Recipe : ScriptableObject
{
    [SerializeField]
    public Dictionary<Item,int> input = new Dictionary<Item,int>();
    [SerializeField]
    public Dictionary<Item,int> output = new Dictionary<Item,int>();

    public AuraType requiredAura;
}
