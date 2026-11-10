using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Character creator: dropdowns that pick the character's face, shirt and hair color.
/// </summary>
public class CharacterCreatorUI : MonoBehaviour
{
    [Header("Character parts")]
    [SerializeField] private GameObject hair;
    [SerializeField] private GameObject face;
    [SerializeField] private GameObject body;

    [Header("Options")]
    [SerializeField] private GameObject[] hairstyles;
    [SerializeField] private Color[] hairColors;
    [SerializeField] private Dictionary<string, Texture> faces;
    [SerializeField] private Dictionary<string, Texture> shirts;

    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown facesDropdown;
    [SerializeField] private TMP_Dropdown shirtsDropdown;
    [SerializeField] private TMP_Dropdown hairDropdown;

    // Dropdown option index -> dictionary key.
    private readonly List<string> _faceNames = new();
    private readonly List<string> _shirtNames = new();

    private Renderer _hairRenderer;
    private Renderer _faceRenderer;
    private Renderer _bodyRenderer;

    private void Awake()
    {
        _hairRenderer = hair.GetComponent<MeshRenderer>();
        _faceRenderer = face.GetComponent<SkinnedMeshRenderer>();
        _bodyRenderer = body.GetComponent<SkinnedMeshRenderer>();
    }

    private void Start()
    {
        _faceNames.AddRange(faces.Keys);
        _shirtNames.AddRange(shirts.Keys);

        var hairColorNames = new List<string>();
        for (var i = 0; i < hairColors.Length; i++)
        {
            hairColorNames.Add($"Color {i + 1}");
        }

        SetUpDropdown(facesDropdown, _faceNames, OnFaceChanged);
        SetUpDropdown(shirtsDropdown, _shirtNames, OnShirtChanged);
        SetUpDropdown(hairDropdown, hairColorNames, OnHairChanged);
    }

    public void OnFaceChanged(int index)
    {
        _faceRenderer.material.mainTexture = faces[_faceNames[index]];
    }

    public void OnShirtChanged(int index)
    {
        _bodyRenderer.material.mainTexture = shirts[_shirtNames[index]];
    }

    public void OnHairChanged(int index)
    {
        _hairRenderer.material.color = hairColors[index];
    }

    private static void SetUpDropdown(TMP_Dropdown dropdown, List<string> options, UnityAction<int> onChanged)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(onChanged);
    }
}
