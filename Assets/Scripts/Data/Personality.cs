using UnityEngine;

[CreateAssetMenu(fileName = "Personality", menuName = "Scriptable Objects/Personality")]
public class Personality : ScriptableObject
{
    public string shortname;
    public string longname;
    public string chatter;

    public Sprite portrait;
}
