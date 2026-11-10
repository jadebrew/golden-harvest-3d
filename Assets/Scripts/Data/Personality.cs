using UnityEngine;

[CreateAssetMenu(fileName = "Personality", menuName = "Scriptable Objects/Personality")]
public class Personality : ScriptableObject
{
    public string shortName;
    public string longName;
    public string chatter;
    public Sprite portrait;
}
