using Gaia.Internal;
using PWCommon5;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// Editor for the polymask system
    /// </summary>
    [CustomEditor(typeof(PolyMask))]
    public class PolyMaskEditor : PWEditor, IPWEditor
    {
        #region Variables
        //Initialization state 
        private bool m_isInitialized = false;

        //Editor utils
        private EditorUtils m_editorUtils;

        //The polymask we are using
        private PolyMask m_polyMask;

        //The texture used as a button for editing nodes
        private Texture2D m_nodeButtonTexture;

        //The name of the texture used as a button for editing nodes
        private const string m_nodeButtonTextureName = "UI/Skin/Knob.psd";

        //Node button size constants
        private Vector2 m_nodeBrushSize = new Vector2(25f, 25f);
        private Vector2 m_halfNodeBrushSize = new Vector2(12.5f, 12.5f);

        //Current brush location
        private Vector3 m_brushPosition = Vector3.zero;

        //Current brush normal
        private Vector3 m_brushNormal = Vector3.up;

        //Last terrain we hit
        private Terrain m_lastBrushTerrain = null;

        //Brush states
        private bool m_isIntentToPaint = false;
        private bool m_isIntentToErase = false;
        private bool m_isPainting = false;
        private bool m_isErasing = false;

        #endregion

        #region Add Polymask Editor Menu

        [MenuItem("Window/Procedural Worlds/Gaia/Create Polymask")]
        public static void CreatePolymask()
        {
            GameObject gameObject = new GameObject($"Polymask-{DateTime.Now:ffffff}");
            gameObject.AddComponent<PolyMask>();
            gameObject.tag = "EditorOnly";
            gameObject.hideFlags = HideFlags.DontSaveInBuild;
            Selection.activeGameObject = gameObject;
        }

        #endregion

        #region Initialization - setup and teardown

        /// <summary>
        /// Initialize the system for use with the polymask
        /// </summary>
        private void InitializePolymask()
        {
            //Create and initialize editor utils
            if (m_editorUtils == null)
            {
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            //Check for poly mask
            if (m_polyMask == null)
            {
                m_polyMask = target as PolyMask;
                if (m_polyMask == null)
                {
                    return;
                }
            }

            //Make sure we have our button texture loaded
            if (m_nodeButtonTexture == null)
            {
                m_nodeButtonTexture = AssetDatabase.GetBuiltinExtraResource<Texture2D>(m_nodeButtonTextureName);
            }

            //Register for editor events and 
            if (!m_isInitialized)
            {
                SceneView.duringSceneGui -= OnSceneGUIUpdate;
                SceneView.duringSceneGui += OnSceneGUIUpdate;
                m_isInitialized = true;
            }
        }

        /// <summary>
        /// Tidy up after finising with the polymask
        /// </summary>
        private void DeInitializePolymask()
        {
            if (m_editorUtils != null)
            {
                m_editorUtils.Dispose();
                m_editorUtils = null;
            }
            if (m_isInitialized)
            {
                SceneView.duringSceneGui -= OnSceneGUIUpdate;
                m_isInitialized = false;
            }
        }

        #endregion

        #region Main event processing handlers

        /// <summary>
        /// On enable handler
        /// </summary>
        private void OnEnable()
        {
            InitializePolymask();
            if (!m_polyMask.IsNameChanged())
            {
                m_polyMask.ShowVisualizationMeshes();
            }
            m_polyMask.UpdateVisualization();
        }

        /// <summary>
        /// On disable handler
        /// </summary>
        private void OnDisable()
        {
            if (!m_polyMask.AlwaysShowVisualization)
            {
                m_polyMask.HideVisualizationMeshes();
            }
            DeInitializePolymask();
        }

        /// <summary>
        /// On destroy handler
        /// </summary>
        private void OnDestroy()
        {
            DeInitializePolymask();
        }

        /// <summary>
        /// On inspector GUI - Main editing UX
        /// </summary>
        public override void OnInspectorGUI()
        {
            //Create and initialize 
            InitializePolymask();

            //Do not remove this - required for the editor utils to work
            m_editorUtils.Initialize();

            //Display title etc
            m_editorUtils.Title("PolyMaskMainPanelTitle");
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            m_editorUtils.Text("PolyMaskMainPanelHelp");
            GUILayout.EndHorizontal();

            //Dislay the main panel 
            m_editorUtils.Panel("MainPanel", MainPanel, true);
        }

        /// <summary>
        /// UX for the main panel for the mask
        /// </summary>
        /// <param name="helpEnabled"></param>
        private void MainPanel(bool helpEnabled)
        {
            //Check for name changes - gets called a lot, but relatively cheap
            if (m_polyMask.IsNameChanged())
            {
                m_polyMask.UpdateVisualization();
            }

            //Otherwise handle the mask editing as normal
            EditorGUI.BeginChangeCheck();
            PolyMask.PolyMaskType oldMaskType = (PolyMask.PolyMaskType)m_editorUtils.EnumPopup("MaskType", m_polyMask.MaskType, helpEnabled);
            Color oldColor = m_editorUtils.ColorField("MaskColor", m_polyMask.MaskColor, helpEnabled);
            float oldIntensity = m_editorUtils.Slider("MaskIntensity", m_polyMask.MaskIntensity, 0f, 2f, helpEnabled);
            Texture2D oldBrushTexture = (Texture2D)m_editorUtils.ObjectField("BrushTexture", m_polyMask.BrushTexture, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
            float oldBrushRadius = m_editorUtils.Slider("BrushSize", m_polyMask.BrushRadius * 2f, m_polyMask.MinBrushRadius * 2f, m_polyMask.MaxBrushRadius * 2f, helpEnabled) / 2f;
            bool oldAlwaysShowVisualization = m_editorUtils.Toggle("AlwaysShowVisualization", m_polyMask.AlwaysShowVisualization, helpEnabled);
            if (m_editorUtils.Button("ExportMask"))
            {
                m_polyMask.ExportMasks();
                AssetDatabase.Refresh();
                m_polyMask.PingFirstExportedMask();
            }

            if (EditorGUI.EndChangeCheck())
            {
                //Update the mask type
                if (oldMaskType != m_polyMask.MaskType)
                {
                    m_polyMask.MaskType = oldMaskType;
                    m_polyMask.UpdateVisualization();
                }

                //Update the mask color
                if (oldColor != m_polyMask.MaskColor)
                {
                    m_polyMask.MaskColor = oldColor;
                    m_polyMask.UpdateVisualization();
                }

                //Update the mask intensity
                if (oldIntensity != m_polyMask.MaskIntensity)
                {
                    m_polyMask.MaskIntensity = oldIntensity;
                    m_polyMask.UpdateVisualization();
                }

                //Update the brush texture and visualization
                if (oldBrushTexture != m_polyMask.BrushTexture)
                {
                    m_polyMask.BrushTexture = oldBrushTexture;
                    m_polyMask.UpdateVisualization();
                }

                //Update the brush radius - its being lost!
                if (oldBrushRadius != m_polyMask.BrushRadius)
                {
                    m_polyMask.BrushRadius = oldBrushRadius;
                }

                //Update the always show visualization
                if (oldAlwaysShowVisualization != m_polyMask.AlwaysShowVisualization)
                {
                    m_polyMask.AlwaysShowVisualization = oldAlwaysShowVisualization;
                    m_polyMask.UpdateVisualization();
                }

                //Mark the mask as dirty
                EditorUtility.SetDirty(m_polyMask);
            }
        }

        /// <summary>
        /// Event handling - needs to be handled here as we are doing brush events, and mouse events
        /// are not sent to OnSceneGUI. Registration is done in the InitializePolymask method.
        /// </summary>
        /// <param name="sceneView"></param>
        private void OnSceneGUIUpdate(SceneView sceneView)
        {
            //Check for valid event
            Event e = Event.current;
            if (e == null)
            {
                return;
            }

            //Check for valid scene view
            if (sceneView == null)
            {
                return;
            }

            //Process changes to the transform
            if (m_polyMask.transform.hasChanged)
            {
                m_polyMask.ProcessTransformUpdate();
                return;
            }

            //Exit on unity center or right mouse button based nav based events
            if (e.button == 1 || e.button == 2)
            {
                return;
            }

            //If shift is pressed, then we are in view and edit nodes mode
            if (e.shift)
            {
                DrawNodesAndProcessMovement();
                return;
            }

            //Exit if not a control or alt key related event
            if (!(e.control || e.alt))
            {
                return;
            }

            //Exit if we are not in the scene view window
            Vector2 mousePosition = e.mousePosition;
            if (mousePosition.x < 0 || mousePosition.y < 0 ||
                mousePosition.x > sceneView.position.width ||
                mousePosition.y > sceneView.position.height)
            {
                m_isIntentToPaint = false;
                m_isPainting = false;
                m_isIntentToErase = false;
                m_isErasing = false;
                return;
            }

            // Mouse wheel scroll adjusts brush size or scroll in scene view
            if (e.type == EventType.ScrollWheel)
            {
                m_polyMask.BrushRadius = Mathf.Clamp(m_polyMask.BrushRadius - e.delta.y * 0.1f, m_polyMask.MinBrushRadius, m_polyMask.MaxBrushRadius);
                e.Use();
                Repaint();
                return;
            }

            //Show where painting or deleting is happening
            if (e.control)
            {
                DrawActiveAndNextNode();
            }
            else
            {
                DrawAllNodes();
            }

            //Draw the brush if its a layout or repaint event
            if (e.type == EventType.Repaint)
            {
                DrawBrush(e);
                return;
            }

            if (!m_isErasing && !m_isPainting)
            {
                if (e.type != EventType.MouseMove && e.type != EventType.MouseDown && 
                    e.type != EventType.MouseDrag && e.type != EventType.MouseUp)
                {
                    if (e.control)
                    {
                        GetWorldPositionAtMouse(sceneView, e.mousePosition, out m_brushPosition, out m_brushNormal);
                        m_isIntentToPaint = true;
                        m_isPainting = false;
                        m_isIntentToErase = false;
                        m_isErasing = false;
                        sceneView.Repaint();
                        return;
                    }
                    else if (e.alt)
                    {
                        GetWorldPositionAtMouse(sceneView, e.mousePosition, out m_brushPosition, out m_brushNormal);
                        m_isIntentToPaint = false;
                        m_isPainting = false;
                        m_isIntentToErase = true;
                        m_isErasing = false;
                        sceneView.Repaint();
                        return;
                    }
                    else
                    {
                        m_isIntentToPaint = false;
                        m_isPainting = false;
                        m_isIntentToErase = false;
                        m_isErasing = false;
                        return;
                    }
                }
            }

            if (e.type == EventType.MouseMove)
            {
                //Process the event
                GetWorldPositionAtMouse(sceneView, e.mousePosition, out m_brushPosition, out m_brushNormal);
                if (e.control)
                {
                    m_isIntentToPaint = true;
                    m_isPainting = false;
                    m_isIntentToErase = false;
                    m_isErasing = false;
                }
                else if (e.alt)
                {
                    m_isIntentToPaint = false;
                    m_isPainting = false;
                    m_isIntentToErase = true;
                    m_isErasing = false;
                }
                sceneView.Repaint();
            }

            if (e.type == EventType.MouseDown)
            {
                //Process the event
                GetWorldPositionAtMouse(sceneView, e.mousePosition, out m_brushPosition, out m_brushNormal);
                if (e.control)
                {
                    m_isIntentToPaint = false;
                    m_isPainting = true;
                    m_isIntentToErase = false;
                    m_isErasing = false;
                    m_polyMask.AddPaint(m_brushPosition, m_lastBrushTerrain);
                }
                else if (e.alt)
                {
                    m_isIntentToPaint = false;
                    m_isPainting = false;
                    m_isIntentToErase = false;
                    m_isErasing = true;
                    m_polyMask.RemovePaint(m_brushPosition);
                }
                else
                {
                    m_isIntentToPaint = false;
                    m_isPainting = false;
                    m_isIntentToErase = false;
                    m_isErasing = false;
                }
                e.Use();
                sceneView.Repaint();
                return;
            }

            if (e.type == EventType.MouseDrag)
            {
                //Process the event
                GetWorldPositionAtMouse(sceneView, e.mousePosition, out m_brushPosition, out m_brushNormal);
                if (m_isPainting)
                {
                    m_polyMask.AddPaint(m_brushPosition, m_lastBrushTerrain, true);
                }
                else if (m_isErasing)
                {
                    m_polyMask.RemovePaint(m_brushPosition);
                }
                e.Use();
                sceneView.Repaint();
                return;
            }

            if (e.type == EventType.MouseUp)
            {
                //Process the event
                GetWorldPositionAtMouse(sceneView, e.mousePosition, out m_brushPosition, out m_brushNormal);
                if (e.control)
                {
                    m_isIntentToPaint = true;
                    m_isPainting = false;
                    m_isIntentToErase = false;
                    m_isErasing = false;
                }
                else if (e.alt)
                {
                    m_isIntentToPaint = false;
                    m_isPainting = false;
                    m_isIntentToErase = true;
                    m_isErasing = false;
                }
                else
                {
                    m_isIntentToPaint = false;
                    m_isPainting = false;
                    m_isIntentToErase = false;
                    m_isErasing = false;
                }
                e.Use();
                sceneView.Repaint();
                return;
            }
        }

        #endregion

        #region General command handlers and utilities

        /// <summary>
        /// Draw the brush
        /// </summary>
        private void DrawBrush(Event e)
        {
            Color oldColor = Handles.color;
            if (m_isIntentToPaint)
            {
                Handles.color = Color.blue;
            }
            else if (m_isIntentToErase)
            {
                Handles.color = Color.yellow;
            }
            else if (m_isPainting)
            {
                Handles.color = Color.green;
            }
            else if (m_isErasing)
            {
                Handles.color = Color.red;
            }
            else
            {
                Handles.color = Color.white;
            }
            Handles.DrawLine(m_brushPosition, m_brushPosition + (Vector3.up * 2f), 2f);
            Handles.DrawWireDisc(m_brushPosition, m_brushNormal, m_polyMask.BrushRadius, 2f);
            Handles.color = oldColor;
        }

        /// <summary>
        /// Draw the nodes as a visualization
        /// </summary>
        private void DrawActiveAndNextNode()
        {
            //Just want to show the selected node and the next node
            if (m_polyMask.ControlPoints.Count == 0)
            {
                return;
            }
            Color color = Handles.color;
            Handles.BeginGUI();
            //Draw the selected node button
            DrawNode(HandleUtility.WorldToGUIPoint(m_polyMask.ControlPoints[m_polyMask.SelectedNodeIndex].Position), Color.red);
            if (m_polyMask.SelectedNodeIndex < m_polyMask.ControlPoints.Count - 2)
            {
                DrawNode(HandleUtility.WorldToGUIPoint(m_polyMask.ControlPoints[m_polyMask.SelectedNodeIndex + 1].Position), Color.green);
            }
            else
            {
                DrawNode(HandleUtility.WorldToGUIPoint(m_polyMask.ControlPoints[0].Position), Color.green);
            }
            Handles.EndGUI();
        }

        /// <summary>
        /// Draw all the nodes as a visualization
        /// </summary>
        private void DrawAllNodes()
        {
            //Just want to show the selected node and the next node
            if (m_polyMask.ControlPoints.Count == 0)
            {
                return;
            }
            Color oldColor = Handles.color;
            Handles.BeginGUI();
            int nextNodeIndex = m_polyMask.SelectedNodeIndex + 1;
            if (nextNodeIndex >= m_polyMask.ControlPoints.Count)
            {
                nextNodeIndex = 0;
            }
            for (int i = 0;i < m_polyMask.ControlPoints.Count; i++)
            {
                PolyMaskNode node = m_polyMask.ControlPoints[i];
                if (m_polyMask.IsOnScreen(node.Position))
                {
                    if (i == m_polyMask.SelectedNodeIndex)
                    {
                        //Draw the selected node button
                        DrawNode(HandleUtility.WorldToGUIPoint(node.Position), Color.red);
                    }
                    else if (i == nextNodeIndex)
                    {
                        //Draw the direction button in a different color signifying the direction
                        DrawNode(HandleUtility.WorldToGUIPoint(node.Position), Color.green);
                    }
                    else
                    {
                        DrawNode(HandleUtility.WorldToGUIPoint(node.Position), Color.blue);
                    }
                }
            }
            Handles.EndGUI();
            Handles.color = oldColor;
        }

        /// <summary>
        /// Draw a node at the position with the color provided
        /// </summary>
        /// <param name="position"></param>
        /// <param name="color"></param>
        public void DrawNode(Vector2 position, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            Rect buttonRect = new Rect(position - m_halfNodeBrushSize, m_nodeBrushSize);
            GUI.DrawTexture(buttonRect, m_nodeButtonTexture, ScaleMode.ScaleToFit);
            GUI.color = oldColor;
        }

        /// <summary>
        /// Draw the nodes as buttons and process node editing and movement
        /// </summary>
        private void DrawNodesAndProcessMovement()
        {
            //Handle node editing
            if (m_polyMask.SelectedNodeIndex < m_polyMask.ControlPoints.Count)
            {
                PolyMaskNode activeNode = m_polyMask.ControlPoints[m_polyMask.SelectedNodeIndex];
                if (Tools.current == Tool.Move)
                {
                    Vector3 pos = Handles.PositionHandle(activeNode.Position, Quaternion.identity);
                    if (pos != activeNode.Position)
                    {
                        pos.y += 1000f;

                        //Pin it to the terrain
                        RaycastHit hit;
                        Ray ray = new Ray(pos, Vector3.down);
                        if (Physics.Raycast(ray, out hit))
                        {
                            activeNode.Position = hit.point;
                        }

                        m_polyMask.UpdateVisualization();
                        return;
                    }
                }
                else if (Tools.current == Tool.Rotate)
                {
                    Quaternion rot = Handles.RotationHandle(Quaternion.identity, activeNode.Position);
                    if (rot != Quaternion.identity)
                    {
                        //Debug.Log("Ignoring node rotation");
                        return;
                    }
                }
                else if (Tools.current == Tool.Scale)
                {
                    Vector3 scale = Handles.ScaleHandle(Vector3.one, activeNode.Position, Quaternion.identity, 1f);
                    if (scale != Vector3.one)
                    {
                        //Debug.Log("Ignoring node scale");
                        return;
                    }
                }
            }

            //Draw the nodes buttons and select nodes. Show where current node is and next node would be added.
            Color color = Handles.color;
            Handles.BeginGUI();
            int nextNodeIndex = m_polyMask.SelectedNodeIndex + 1;
            if (nextNodeIndex >= m_polyMask.ControlPoints.Count)
            {
                nextNodeIndex = 0;
            }
            for (int i = 0; i < m_polyMask.ControlPoints.Count; i++)
            {
                PolyMaskNode node = m_polyMask.ControlPoints[i];
                if (m_polyMask.IsOnScreen(node.Position))
                {
                    if (i == m_polyMask.SelectedNodeIndex)
                    {
                        //Draw the selected node button
                        if (DrawNodeButton(HandleUtility.WorldToGUIPoint(node.Position), Color.red))
                        {
                            //Debug.Log("Clicked on node " + i);
                        }
                    }
                    else if (i == nextNodeIndex)
                    {
                        //Draw the direction button in a different color signifying the direction
                        if (DrawNodeButton(HandleUtility.WorldToGUIPoint(node.Position), Color.green))
                        {
                            //Debug.Log("Selecting node " + i);
                            m_polyMask.SelectNodeByIndex(i);
                        }
                    }
                    else
                    {
                        //Draw the normal node button
                        if (DrawNodeButton(HandleUtility.WorldToGUIPoint(node.Position), Color.blue))
                        {
                            //Debug.Log("Selecting node " + i);
                            m_polyMask.SelectNodeByIndex(i);
                        }
                    }
                }
            }
            Handles.color = color;
            Handles.EndGUI();
        }

        /// <summary>
        /// Draw a node button at the position with the texture and color provided
        /// </summary>
        /// <param name="position"></param>
        /// <param name="texture2D"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public bool DrawNodeButton(Vector2 position, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            Rect buttonRect = new Rect(position - m_halfNodeBrushSize, m_nodeBrushSize);
            bool result = GUI.Button(buttonRect, m_nodeButtonTexture, GUIStyle.none);
            GUI.color = oldColor;
            return result;
        }

        /// <summary>
        /// Get the world position from the mouse position
        /// </summary>
        /// <param name="sceneView">Scene we are in</param>
        /// <param name="mousePosition">Mouse position</param>
        /// <param name="hitPoint">Hitpoint returned</param>
        /// <param name="hitNormal">Hitnormal returned</param>
        /// <returns>True if we hit something</returns>
        private bool GetWorldPositionAtMouse(SceneView sceneView, Vector2 mousePosition, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            m_lastBrushTerrain = null;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                m_lastBrushTerrain = hit.transform.GetComponent<Terrain>();
                hitPoint = hit.point;
                hitNormal = hit.normal;
                return true;
            }

            // If no hit, project to a plane at y=0
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                hitPoint = ray.GetPoint(distance);
                hitNormal = Vector3.up;
                return true;
            }

            //General failure
            hitPoint = Vector3.zero;
            hitNormal = Vector3.up;
            return false;
        }

        #endregion
   }
}