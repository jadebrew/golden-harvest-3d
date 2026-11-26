
using UnityEngine;

public class ChibiEditor : MonoBehaviour
{
    public GameObject handItemPrefab;

    public GameObject hairPrefab;
    public Material hairMaterial;
    public Color hairColor;

    private Transform handSlot;
    private Transform hairSlot;

    private void Awake()
    {
        // Find slots inside children (including nested children)
        handSlot = FindChildRecursive(transform, "handslot");
        hairSlot = FindChildRecursive(transform, "hairslot");

        // Validate slots
        if (handSlot == null || hairSlot == null)
        {
            Debug.LogError("Could not find HandSlot or HairSlot!", this);
            return;
        }

        // Step 1: Clear hand slot and instantiate hand item
        ClearSlot(handSlot);
        Instantiate(handItemPrefab, handSlot);

        // Step 2: Clear hair slot and instantiate hair
        ClearSlot(hairSlot);
        GameObject newHair = Instantiate(hairPrefab, hairSlot);

        // Step 3: Create a material instance
        if (hairMaterial != null)
        {
            Material instance = new Material(hairMaterial);

            // Step 4: Set hair color on the instance
            instance.color = hairColor;

            // Apply material instance to all child renderers
            Renderer[] renderers = newHair.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                renderer.material = instance;
            }
        }
        else
        {
            Debug.LogWarning("No hair material assigned!", this);
        }
    }

    private void ClearSlot(Transform slot)
    {
        foreach (Transform child in slot)
        {
            Destroy(child.gameObject);
        }
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower() == targetName.ToLower())
                return child;

            Transform result = FindChildRecursive(child, targetName);

            if (result != null)
                return result;
        }

        return null;
    }
}
