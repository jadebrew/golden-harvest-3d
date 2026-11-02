using UnityEngine;

public class Node : MonoBehaviour
{
    public Item _spawnable;
    public GameObject target;
    public float magnetRange = 0.5f;

    void FixedUpdate()
    {
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance < magnetRange)
        {
            target.GetComponent<PlayerMovement>().Give(_spawnable);
            transform.gameObject.SetActive(false);
        }
    }

}
