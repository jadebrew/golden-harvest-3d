using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CACUi : MonoBehaviour
{
    public GameObject hair;
    public GameObject face;
    public GameObject body;

    public GameObject[] hairstyles;

    public Color[] colors;

    [SerializeField]
    public Dictionary<string,Texture> faces;
    [SerializeField]
    public Dictionary<string,Texture>  shirts;

    //Create a List of new Dropdown options
    List<string> faceOptions = new List<string>();
    List<string> shirtOptions = new List<string>();
    List<string> haircolorOptions = new List<string>();
    //This is the Dropdown
    public TMP_Dropdown facesDropdown;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TMP_Dropdown shirtsDropdown;

    public TMP_Dropdown hairDropdown;
    void Start()
    {
        //Clear the old options of the Dropdown menu
        facesDropdown.ClearOptions();
        shirtsDropdown.ClearOptions();
        hairDropdown.ClearOptions();
        foreach (KeyValuePair<string,Texture> face in faces)
        {
            //Add the options created in the List above
           faceOptions.Add(face.Key);

        }
        int colorIndex = 1;
        foreach (Color _col in colors)
        {
            //Add the options created in the List above
            haircolorOptions.Add("Color " + colorIndex);
            colorIndex++;
        }

        foreach (KeyValuePair<string,Texture> shirt in shirts)
        {
            //Add the options created in the List above
            shirtOptions.Add(shirt.Key);
        }
        facesDropdown.AddOptions(faceOptions);
        shirtsDropdown.AddOptions(shirtOptions);
        hairDropdown.AddOptions(haircolorOptions);

        hairDropdown.onValueChanged.AddListener(OnHairChanged);
        facesDropdown.onValueChanged.AddListener(OnFaceChanged);
        shirtsDropdown.onValueChanged.AddListener(OnShirtChanged);
    }

    public void OnFaceChanged(int value)
    {

        SkinnedMeshRenderer meshRenderer = face.GetComponent<SkinnedMeshRenderer>();
        meshRenderer.material.mainTexture = faces[faceOptions[value]];
    }

    public void OnHairChanged(int value)
    {

        MeshRenderer meshRenderer = hair.GetComponent<MeshRenderer>();
        meshRenderer.material.color = colors[value];
    }

    public void OnShirtChanged(int value)
    {

        SkinnedMeshRenderer meshRenderer = body.GetComponent<SkinnedMeshRenderer>();
        meshRenderer.material.mainTexture = shirts[shirtOptions[value]];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
