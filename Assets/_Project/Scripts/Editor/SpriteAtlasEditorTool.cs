// 2025/7/21 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEngine.U2D;

public class SpriteAtlasEditorTool : EditorWindow
{
    private SpriteAtlas spriteAtlas;
    private Object spriteToAdd;

    [MenuItem("Tools/Sprite Atlas Editor Tool")]
    private static void OpenWindow()
    {
        GetWindow<SpriteAtlasEditorTool>("Sprite Atlas Editor Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite Atlas Editor Tool", EditorStyles.boldLabel);

        spriteAtlas = (SpriteAtlas)EditorGUILayout.ObjectField("Sprite Atlas", spriteAtlas, typeof(SpriteAtlas), false);
        spriteToAdd = EditorGUILayout.ObjectField("Sprite to Add", spriteToAdd, typeof(Sprite), false);

        if (GUILayout.Button("Add Sprite to Atlas"))
        {
            if (spriteAtlas == null)
            {
                Debug.LogError("Please assign a Sprite Atlas.");
                return;
            }

            if (spriteToAdd == null || !(spriteToAdd is Sprite))
            {
                Debug.LogError("Please assign a valid Sprite.");
                return;
            }

            AddSpriteToAtlas(spriteAtlas, spriteToAdd as Sprite);
        }
    }

    private void AddSpriteToAtlas(SpriteAtlas atlas, Sprite sprite)
    {
        string spritePath = AssetDatabase.GetAssetPath(sprite);
        if (string.IsNullOrEmpty(spritePath))
        {
            Debug.LogError("Could not find the asset path for the selected sprite.");
            return;
        }

        Object[] objectsInAtlas = atlas.GetPackables();

        // Check if the sprite is already in the atlas
        foreach (Object obj in objectsInAtlas)
        {
            if (obj == sprite)
            {
                Debug.LogWarning("Sprite is already in the atlas.");
                return;
            }
        }

        // Add the sprite to the atlas
        atlas.Add(new Object[] { AssetDatabase.LoadMainAssetAtPath(spritePath) });
        AssetDatabase.SaveAssets();
        Debug.Log("Sprite added to the atlas successfully.");
    }
}