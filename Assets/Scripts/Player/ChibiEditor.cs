
using UnityEngine;

public class ChibiEditor : MonoBehaviour
{
    [Header("Equipment")]
    public GameObject handItemPrefab;
    public GameObject hairPrefab;

    [Header("Hair")]
    public Material hairMaterial;
    public Color hairColor = Color.white;

    [Header("Body Materials")]
    public Material armsMaterial;
    public Color armsColor = Color.white;

    public Material faceMaterial;
    public Color faceColor = Color.white;

    public Material feetMaterial;
    public Color feetColor = Color.white;

    public Material handsMaterial;
    public Color handsColor = Color.white;

    public Material headMaterial;
    public Color headColor = Color.white;

    public Material legsMaterial;
    public Color legsColor = Color.white;

    public Material torsoMaterial;
    public Color torsoColor = Color.white;

    // Automatically discovered references
    private Transform handSlot;
    private Transform hairSlot;

    private SkinnedMeshRenderer arms;
    private SkinnedMeshRenderer face;
    private SkinnedMeshRenderer feet;
    private SkinnedMeshRenderer hands;
    private SkinnedMeshRenderer head;
    private SkinnedMeshRenderer legs;
    private SkinnedMeshRenderer torso;

    private void Awake()
    {
        // Find equipment slots
        handSlot = FindChildRecursive(transform, "handslot");
        hairSlot = FindChildRecursive(transform, "hairslot");

        // Find body parts
        arms = FindRenderer("ARMS");
        face = FindRenderer("FACE");
        feet = FindRenderer("FEET");
        hands = FindRenderer("HANDS");
        head = FindRenderer("HEAD");
        legs = FindRenderer("LEGS");
        torso = FindRenderer("TORSO");

        // Setup hand item
        if (handSlot != null && handItemPrefab != null)
        {
            ClearSlot(handSlot);
            Instantiate(handItemPrefab, handSlot);
        }

        // Setup hair
        if (hairSlot != null && hairPrefab != null)
        {
            ClearSlot(hairSlot);

            GameObject newHair = Instantiate(hairPrefab, hairSlot);

            ApplyMaterial(
                newHair.GetComponentsInChildren<Renderer>(true),
                          hairMaterial,
                          hairColor
            );
        }

        // Apply body materials
        ApplyMaterial(arms, armsMaterial, armsColor);
        ApplyMaterial(face, faceMaterial, faceColor);
        ApplyMaterial(feet, feetMaterial, feetColor);
        ApplyMaterial(hands, handsMaterial, handsColor);
        ApplyMaterial(head, headMaterial, headColor);
        ApplyMaterial(legs, legsMaterial, legsColor);
        ApplyMaterial(torso, torsoMaterial, torsoColor);
    }

    private SkinnedMeshRenderer FindRenderer(string objectName)
    {
        Transform found = FindChildRecursive(transform, objectName);

        if (found == null)
        {
            Debug.LogWarning($"Could not find {objectName}!", this);
            return null;
        }

        SkinnedMeshRenderer renderer =
        found.GetComponent<SkinnedMeshRenderer>();

        if (renderer == null)
        {
            Debug.LogWarning(
                $"{objectName} does not have a SkinnedMeshRenderer!",
                found
            );
        }

        return renderer;
    }

    private Transform FindChildRecursive(
        Transform parent,
        string targetName)
    {
        foreach (Transform child in parent)
        {
            if (string.Equals(
                child.name,
                targetName,
                System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            Transform result = FindChildRecursive(child, targetName);

            if (result != null)
                return result;
        }

        return null;
    }

    private void ClearSlot(Transform slot)
    {
        foreach (Transform child in slot)
        {
            Destroy(child.gameObject);
        }
    }

    private void ApplyMaterial(
        SkinnedMeshRenderer renderer,
        Material source,
        Color color)
    {
        if (renderer == null || source == null)
            return;

        Material instance = new Material(source);
        instance.color = color;

        renderer.material = instance;
    }

    private void ApplyMaterial(
        Renderer[] renderers,
        Material source,
        Color color)
    {
        if (source == null)
            return;

        Material instance = new Material(source);
        instance.color = color;

        foreach (Renderer renderer in renderers)
        {
            renderer.material = instance;
        }
    }

    private void OnDestroy()
    {
        // Clean up instantiated body material instances
        foreach (SkinnedMeshRenderer renderer in new[]
        {
            arms, face, feet, hands, head, legs, torso
        })
        {
            if (renderer == null)
                continue;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null &&
                    material.name.EndsWith("(Instance)"))
                {
                    Destroy(material);
                }
            }
        }
    }
}
