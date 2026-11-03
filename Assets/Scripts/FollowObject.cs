using UnityEngine;

public class FollowObject : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject goal;
    public GameObject target;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = Vector3.Lerp(this.transform.position, goal.transform.position, 0.1f);
        this.transform.LookAt(target.transform);
    }
}
