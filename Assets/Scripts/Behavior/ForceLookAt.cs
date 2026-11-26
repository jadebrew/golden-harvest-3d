using UnityEngine;

public class ForceLookAt : MonoBehaviour
{
    public GameObject target;
    private Quaternion _rotation;
    void Awake()
    {
        _rotation = transform.rotation;
    }
    void Update()
    {
        transform.LookAt(target.transform);
        transform.Rotate(_rotation.eulerAngles);
    }
}
