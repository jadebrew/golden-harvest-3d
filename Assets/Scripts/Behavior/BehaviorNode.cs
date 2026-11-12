using UnityEngine;

public enum NodeType
{
    Condition,
    Sequence,
    Random,
    Leaf,
}

[CreateAssetMenu(fileName = "BehaviorNode", menuName = "BehaviorNodes/Node")]
public class BehaviorNode : ScriptableObject
{
    public BehaviorNode[] _nodes;
    public string _nodeName;
    public NodeType _nodeType;
    private bool _condition;

    public bool Compute(BehaviorNode _parent)
    {
        switch (_nodeType)
        {
            case NodeType.Sequence:
                //follow all paths, ezpz.
                foreach (BehaviorNode node in _nodes)
                {
                    node.Compute(this);
                }
                break;
            case NodeType.Condition:
                //how to check for a condition?
                //follow leaf -> return true or false
                //break at first true!
                foreach (BehaviorNode node in _nodes)
                {
                    if (node.Compute(this))
                    {
                        break;
                    }
                }
                break;
            case NodeType.Random:
                //follow a random path. ezpz.
                _nodes[Random.Range(0,_nodes.Length)].Compute(this);
                break;
            case NodeType.Leaf:
                Debug.Log("Hi my name is " + _nodeName);
                _condition = true;
                break;
            default:
                _condition = false;
                break;

        }
        return _condition;
    }

    public void SetCondition(bool value)
    {
        _condition = value;
    }
}
