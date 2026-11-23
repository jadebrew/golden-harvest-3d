using UnityEngine;

public class AnimateOverTime : MonoBehaviour
{
    //values which are lerped over 24 hours
    public Vector3 RotationPer24hours;

    public Vector3 TranslationPer24hours;

    public int AwakeTime;

    public int AsleepTime;

    public bool invertSleepSchedule;

    private Vector3 startingPoint;
    private Quaternion startingRotation;

    void Start()
    {
        startingPoint = transform.position;
        startingRotation = transform.rotation;
    }

    void Update()
    {

    }
}
