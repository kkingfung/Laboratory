// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.UI;

public class CharacterCustomization : MonoBehaviour
{
    [Header("Customization Options")]
    public SkinnedMeshRenderer characterMesh;
    public Material[] skinMaterials;
    public Material[] hairMaterials;
    public GameObject[] hairstyles;

    [Header("UI Elements")]
    public Dropdown skinDropdown;
    public Dropdown hairDropdown;
    public Dropdown hairstyleDropdown;

    private void Start()
    {
        // Populate UI dropdowns
        PopulateDropdown(skinDropdown, skinMaterials, "Skin");
        PopulateDropdown(hairDropdown, hairMaterials, "Hair Color");
        PopulateDropdown(hairstyleDropdown, hairstyles, "Hairstyle");

        // Add listeners to dropdowns
        skinDropdown.onValueChanged.AddListener(ChangeSkin);
        hairDropdown.onValueChanged.AddListener(ChangeHairColor);
        hairstyleDropdown.onValueChanged.AddListener(ChangeHairstyle);
    }

    private void PopulateDropdown(Dropdown dropdown, Object[] options, string label)
    {
        dropdown.ClearOptions();
        var optionList = new System.Collections.Generic.List<string>();
        for (int i = 0; i < options.Length; i++)
        {
            optionList.Add(label + " " + (i + 1));
        }
        dropdown.AddOptions(optionList);
    }

    public void ChangeSkin(int index)
    {
        if (index >= 0 && index < skinMaterials.Length)
        {
            characterMesh.material = skinMaterials[index];
        }
    }

    public void ChangeHairColor(int index)
    {
        if (index >= 0 && index < hairMaterials.Length)
        {
            // Assuming the hair material is separate from the skin material
            Material[] materials = characterMesh.materials;
            materials[1] = hairMaterials[index]; // Assuming hair material is at index 1
            characterMesh.materials = materials;
        }
    }

    public void ChangeHairstyle(int index)
    {
        for (int i = 0; i < hairstyles.Length; i++)
        {
            hairstyles[i].SetActive(i == index);
        }
    }
}