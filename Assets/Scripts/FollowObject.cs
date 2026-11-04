using UnityEngine;

public class FollowObject : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject goal;
    public GameObject target;
    public float cameraSpeed;
    private float distance;

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var distance = Vector3.Distance(this.transform.position, goal.transform.position);
        if (distance > Time.deltaTime*cameraSpeed)
        {
             this.transform.position = Vector3.MoveTowards(this.transform.position,goal.transform.position,Time.fixedDeltaTime*(cameraSpeed*distance));
        } else {
            this.transform.position = goal.transform.position;
        }

        this.transform.LookAt(target.transform.position);
    }
}
