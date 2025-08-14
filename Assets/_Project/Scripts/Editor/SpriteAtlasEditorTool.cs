using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEngine.U2D;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Editor window for managing Sprite Atlases.
    /// </summary>
    public class SpriteAtlasEditorTool : EditorWindow
    {
        #region Fields

        private SpriteAtlas _spriteAtlas;
        private Object _spriteToAdd;

        #endregion

        #region Unity Override Methods

        [MenuItem("Tools/Sprite Atlas Editor Tool")]
        private static void OpenWindow()
        {
            GetWindow<SpriteAtlasEditorTool>("Sprite Atlas Editor Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("Sprite Atlas Editor Tool", EditorStyles.boldLabel);

            _spriteAtlas = (SpriteAtlas)EditorGUILayout.ObjectField("Sprite Atlas", _spriteAtlas, typeof(SpriteAtlas), false);
            _spriteToAdd = EditorGUILayout.ObjectField("Sprite to Add", _spriteToAdd, typeof(Sprite), false);

            if (GUILayout.Button("Add Sprite to Atlas"))
            {
                if (_spriteAtlas == null)
                {
                    Debug.LogError("Please assign a Sprite Atlas.");
                    return;
                }

                if (_spriteToAdd == null || !(_spriteToAdd is Sprite))
                {
                    Debug.LogError("Please assign a valid Sprite.");
                    return;
                }

                AddSpriteToAtlas(_spriteAtlas, _spriteToAdd as Sprite);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a sprite to the specified atlas if not already present.
        /// </summary>
        private void AddSpriteToAtlas(SpriteAtlas atlas, Sprite sprite)
        {
            string spritePath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(spritePath))
            {
                Debug.LogError("Could not find the asset path for the selected sprite.");
                return;
            }

            Object[] objectsInAtlas = atlas.GetPackables();

            foreach (Object obj in objectsInAtlas)
            {
                if (obj == sprite)
                {
                    Debug.LogWarning("Sprite is already in the atlas.");
                    return;
                }
            }

            atlas.Add(new Object[] { AssetDatabase.LoadMainAssetAtPath(spritePath) });
            AssetDatabase.SaveAssets();
            Debug.Log("Sprite added to the atlas successfully.");
        }

        #endregion
    }
}