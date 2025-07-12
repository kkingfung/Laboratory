using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// A polymask mask - a mask that can be painted on a terrain using a brush
    /// </summary>
    [ExecuteInEditMode]
    public class PolyMask : MonoBehaviour
    {
        #region Enums
        public enum PolyMaskType { Open, Closed };
        public enum PolyMaskEditMode { Points, Brush };

        #endregion

        #region Public variables - much of this should be private - meh!

        public PolyMaskType MaskType = PolyMaskType.Open;
        public List<PolyMaskNode> ControlPoints = new List<PolyMaskNode>();
        public Color MaskColor = Color.red;
        public float MaskIntensity = 0.6f;
        public Texture2D BrushTexture;
        public float BrushRadius = 2.5f; 
        public float BrushStrength = 1f;
        public float MinBrushRadius = 0.25f;
        public float MaxBrushRadius = 150f;
        public bool AlwaysShowVisualization = false;
        public int VisualizationTextureResolution = 512;
        public int VisualizationMeshResolution = 10;
        public int SelectedNodeIndex = 0;
        public int MaxNodeID = -1;  
        public float MaxDrawDistance = 700f;
        private bool m_wasMoved = false;
        public List<PolyMaskVisualization> m_polyMaskVisualizations = new List<PolyMaskVisualization>(); //Matched to unity terrains
        public float m_maxRaycastDistance = 100f;
        public LayerMask m_raycastLayerMask = Physics.DefaultRaycastLayers;
        public Material m_brushMaterial;
        public Material m_floodFillMaterial;
        public RenderTexture m_visualizationBrushRT;
        public RenderTexture m_visualizationDestRT;
        public Vector3 m_visPosition;
        public Quaternion m_visRotation;
        public Vector3 m_visScale = Vector3.one;
        public int m_objectID = int.MinValue;

        #endregion

        #region Events

        public delegate void VisualizationUpdated(PolyMask polyMask);
        public event VisualizationUpdated OnVisualizationUpdated;
        public delegate void Destroyed(PolyMask polyMask);
        public event Destroyed OnDestroyed;

        #endregion

        #region Unity methods

        /// <summary>
        /// Essentially only gets called once, and in the editor
        /// </summary>
        private void Awake()
        {
            if (!Application.isPlaying)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Nuke all the child objects when we get destroyed
        /// </summary>
        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                foreach (PolyMaskVisualization pmv in m_polyMaskVisualizations)
                {
                    GameObject.DestroyImmediate(pmv.VisObject);
                }
            }
            if (OnDestroyed != null)
            {
                OnDestroyed(this);
            }
        }

        #endregion

        #region Main operations

        /// <summary>
        /// Handle transform level updates
        /// </summary>
        public void ProcessTransformUpdate()
        {
            //Ignore if we moved the transform ourselves
            if (m_wasMoved)
            {
                transform.hasChanged = false;
                m_visPosition = transform.position;
                m_visRotation = transform.rotation;
                m_visScale = transform.localScale;
                m_wasMoved = false;
                return;
            }

            //Check for changes
            //Position change
            Vector3 deltaPosition = transform.position - m_visPosition;
            if (deltaPosition != Vector3.zero)
            {
                //Put the transform back on the terrain
                Ray ray = new Ray(transform.position + Vector3.up * 500f, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    transform.position = hit.point;
                }

                //Now move the control points
                deltaPosition = transform.position - m_visPosition;
                foreach (PolyMaskNode node in ControlPoints)
                {
                    //Stick them to the ground
                    node.Position += deltaPosition;
                    ray = new Ray(node.Position + Vector3.up * 500f, Vector3.down);
                    if (Physics.Raycast(ray, out hit))
                    {
                        node.Position = hit.point;
                    }
                }
                AddMissingVisualizations();
                UpdateVisualization();
                transform.hasChanged = false;
                m_visPosition = transform.position;
                return;
            }

            //Rotation change
            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(m_visRotation);
            if (deltaRotation != Quaternion.identity)
            {
                // Rotate all the control points around the transform position by the delta rotation
                foreach (PolyMaskNode node in ControlPoints)
                {
                    Vector3 delta = node.Position - transform.position;
                    node.Position = transform.position + deltaRotation * delta;
                    
                    //Stick it to the ground
                    Ray ray = new Ray(node.Position + Vector3.up * 500f, Vector3.down);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        node.Position = hit.point;
                    }
                }
                AddMissingVisualizations();
                UpdateVisualization();
                transform.hasChanged = false;
                m_visRotation = transform.rotation;
                return;
            }

            //Scale change
            Vector3 deltaScale = transform.localScale - m_visScale;
            if (deltaScale != Vector3.zero)
            {
                // Scale all control points relative to the transform position
                foreach (PolyMaskNode node in ControlPoints)
                {
                    // Get the vector from transform to node
                    Vector3 delta = node.Position - transform.position;

                    // Scale the delta vector by the relative scale change
                    Vector3 scaledDelta = new Vector3(
                        delta.x * (transform.localScale.x / m_visScale.x),
                        delta.y * (transform.localScale.y / m_visScale.y),
                        delta.z * (transform.localScale.z / m_visScale.z)
                    );

                    // Set the new position
                    node.Position = transform.position + scaledDelta;

                    //Stick it to the ground
                    Ray ray = new Ray(node.Position + Vector3.up * 500f, Vector3.down);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        node.Position = hit.point;
                    }
                }
                AddMissingVisualizations();
                UpdateVisualization();
                transform.hasChanged = false;
                m_visScale = transform.localScale;
                return;
            }   

            transform.hasChanged = false;
        }

        /// <summary>
        /// Add any missing visualizations
        /// </summary>
        private void AddMissingVisualizations()
        {
            foreach (PolyMaskNode node in ControlPoints)
            {
                // Create the visualization if it does not exist yet
                PolyMaskVisualization pmv = GetVisualizationAtPosition(node.Position);
                if (pmv == null)
                {
                    //Locate the terrain at this location
                    Terrain terrain = Gaia.TerrainHelper.GetTerrainStrict(node.Position);
                    if (terrain != null)
                    {
                        AddVisualizationAtPosition(node.Position, terrain);
                    }
                }
            }
        }

        /// <summary>
        /// Add a new paint node to the mask
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        public void AddPaint(Vector3 position, Terrain terrain, bool isPainting = false)
        {
            //Create the visualization if it does not exist yet
            PolyMaskVisualization polyTerrain = GetVisualizationAtPosition(position);
            if (polyTerrain == null && terrain != null)
            {
                AddVisualizationAtPosition(position, terrain);
            }

            //Feels a bit clunky
            if (ControlPoints.Count == 0)
            {
                SetupVisualization(position);
            }

            //Check that it is not too close to the nearest nodes when in paint mode
            if (isPainting && TooClose(position))
            {
                return;
            }   

            //Create and add the new node
            PolyMaskNode node = new PolyMaskNode();
            node.ID = ++MaxNodeID;
            node.Position = position;
            node.Radius = BrushRadius;
            node.Feather = 0f;
            node.Strength = 1f;

            if (SelectedNodeIndex >= ControlPoints.Count - 1)
            {
                ControlPoints.Add(node);
                SelectedNodeIndex = ControlPoints.Count-1;
            }
            else
            {
                SelectedNodeIndex++;
                ControlPoints.Insert(SelectedNodeIndex, node);
            }

            //Paint it
            UpdateVisualization();
        }

        /// <summary>
        /// Remove paint nodes within the brush radius of the point
        /// </summary>
        /// <param name="point"></param>
        public void RemovePaint(Vector3 point)
        {
            int minIndexRemoved = int.MaxValue;
            for (int i = 0; i < ControlPoints.Count;)
            {
                PolyMaskNode node = ControlPoints[i];
                if (Vector3.Distance(node.Position, point) < BrushRadius)
                {
                    ControlPoints.RemoveAt(i);
                    if (i < minIndexRemoved)
                    {
                        minIndexRemoved = i;
                    }
                }
                else
                {
                    i++;
                }
            }

            //Exit if nothing was removed
            if (minIndexRemoved == int.MaxValue)
            {
                return;
            }

            //Reset the selected node to be one before the minimum index removed
            SelectedNodeIndex = minIndexRemoved - 1;
            if (SelectedNodeIndex < 0)
            {
                SelectedNodeIndex = 0;
            }

            //Paint it
            UpdateVisualization();
        }

        /// <summary>
        /// Select a node by its index
        /// </summary>
        /// <param name="index"></param>
        public void SelectNodeByIndex(int index)
        {
            if (index < 0 || index >= ControlPoints.Count)
            {
                //Reset the selected node index 
                SelectedNodeIndex = 0;
                //Signal error
                Debug.LogWarning("Node index out of bounds:" + index);
                return;
            }
            //Select the new node index
            SelectedNodeIndex = index;
        }

        /// <summary>
        /// Select a node by its ID
        /// </summary>
        /// <param name="nodeID">Node ID to select</param>
        public void SelectNodeByID(int nodeID)
        {
            //Fallback position to the first node
            SelectedNodeIndex = 0;

            //If there are no nodes then return
            if (ControlPoints.Count == 0)
            {
                return;
            }

            //Find and select the new node by its ID
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                if (ControlPoints[i].ID == nodeID)
                {
                    SelectedNodeIndex = i;
                    return;
                }
            }
        }

        /// <summary>
        /// Select a node by the node itself
        /// </summary>
        /// <param name="node">Node to select</param>
        public void SelectNode(PolyMaskNode node)
        {
            SelectNodeByID(node.ID);
        }

        /// <summary>
        /// Check that the point is not too close to any other point
        /// </summary>
        /// <param name="position">position to check</param>
        /// <param name="radius">radius to check</param>
        /// <returns></returns>
        private bool TooClose(Vector3 position, bool checkAll = false)
        {
            //If no nodes, then we are good
            if (ControlPoints.Count == 0)
            {
                return false;
            }

            //Set the minimum distance to half the brush radius
            float minDistance = BrushRadius / 2f;

            //Check against the selected node for a quick check
            //Debug.Log("Check all:" + checkAll + " Selected node index:" + SelectedNodeIndex + " Control points count:" + ControlPoints.Count);
            if (Vector3.Distance(ControlPoints[SelectedNodeIndex].Position, position) < minDistance)
            {
                //Debug.LogWarning("Too close to selected node");
                return true;
            }

            //Now check all other nodes if required
            if (checkAll)
            {
                for (int i = 0; i < ControlPoints.Count; i++)
                {
                    if (Vector3.Distance(ControlPoints[i].Position, position) < minDistance)
                    {
                        return true;
                    }
                }
            }

            //We are good
            return false;
        }

        #endregion

        #region Gizmos handling for simple editor visualization

        /// <summary>
        /// Visualize the brush hits in the editor
        /// </summary>
        void OnDrawGizmos()
        {
            VisualizeMask();
        }

        /// <summary>
        /// Visualize the brush hits in the editor
        /// </summary>
        void OnDrawGizmosSelected()
        {
            VisualizeMask();
        }

        /// <summary>
        /// Visualize the mask in the editor
        /// </summary>
        private void VisualizeMask()
        {
            if (ControlPoints.Count < 2)
            {
                return;
            }
            Color oldColor = Gizmos.color;
            Gizmos.color = MaskColor;
            for (int i = 0; i < ControlPoints.Count - 1; i++)
            {
                if (IsOnScreen(ControlPoints[i].Position) || IsOnScreen(ControlPoints[i + 1].Position))
                {
                    Gizmos.DrawLine(ControlPoints[i].Position, ControlPoints[i + 1].Position);
                }
            }

            if (MaskType == PolyMaskType.Closed && ControlPoints.Count > 2)
            {
                if (IsOnScreen(ControlPoints[ControlPoints.Count - 1].Position) || IsOnScreen(ControlPoints[0].Position))
                {
                    Gizmos.DrawLine(ControlPoints[ControlPoints.Count - 1].Position, ControlPoints[0].Position);
                }
            }
            Gizmos.color = oldColor;
        }

        /// <summary>
        /// Check that the position is on screen and within a certain distance
        /// </summary>
        /// <param name="position"></param>
        /// <returns>True if on screen and within range</returns>
        public bool IsOnScreen(Vector3 position)
        {
            Vector3 onScreen = Camera.current.WorldToViewportPoint(position);
            return onScreen.z > 0f && onScreen.x > 0f &&
                   onScreen.y > 0f && onScreen.x < 1f &&
                   onScreen.y < 1f && onScreen.z < MaxDrawDistance;
        }

        /// <summary>
        /// Check that the position is on screen and within a certain distance
        /// </summary>
        /// <param name="position"></param>
        /// <param name="maxDistance"></param>
        /// <returns>True if on screen and within range</returns>
        public bool IsOnScreen(Vector3 position, float maxDistance = float.MaxValue)
        {
            Vector3 onScreen = Camera.current.WorldToViewportPoint(position);
            return onScreen.z > 0f && onScreen.x > 0f &&
                   onScreen.y > 0f && onScreen.x < 1f &&
                   onScreen.y < 1f && onScreen.z < maxDistance;
        }

        #endregion

        #region Visualization methods

        /// <summary>
        /// Setup the visualization
        /// </summary>
        /// <param name="position"></param>
        private void SetupVisualization(Vector3 position)
        {
            Vector3 newPosition = CalculateVisualizationCentroid(position);
            if (newPosition != transform.position)
            {
                transform.position = newPosition;
                m_wasMoved = true;
            }
            Initialize();
        }

        /// <summary>
        /// Initialize the visualization system
        /// </summary>
        public void Initialize()
        {
            //Debug.Log("Initializing visualization");

            // Initialize render textures if they don't exist
            if (m_visualizationBrushRT == null)
            {
                m_visualizationBrushRT = new RenderTexture(VisualizationTextureResolution, VisualizationTextureResolution, 0, RenderTextureFormat.R8);
                m_visualizationBrushRT.name = "PolyMask_BrushRT";
                m_visualizationBrushRT.enableRandomWrite = true;
                m_visualizationBrushRT.Create();
            }

            if (m_visualizationDestRT == null)
            {
                m_visualizationDestRT = new RenderTexture(VisualizationTextureResolution, VisualizationTextureResolution, 0, RenderTextureFormat.R8);
                m_visualizationDestRT.name = "PolyMask_DestRT";
                m_visualizationDestRT.enableRandomWrite = true;
                m_visualizationDestRT.Create();
            }

            // Initialize materials with correct shaders
            if (m_floodFillMaterial == null)
            {
                Shader shader = Shader.Find("PWS/PolygonFill");
                if (shader == null)
                {
                    Debug.LogError("[PolyMask] Could not find shader 'PWS/PolygonFill'");
                    return;
                }
                m_floodFillMaterial = new Material(shader);
                m_floodFillMaterial.name = "PolyMask_FloodFill";
            }

            if (m_brushMaterial == null)
            {
                Shader shader = Shader.Find("PWS/AdditiveBrush");
                if (shader == null)
                {
                    Debug.LogError("[PolyMask] Could not find shader 'PWS/AdditiveBrush'");
                    return;
                }
                m_brushMaterial = new Material(shader);
                m_brushMaterial.name = "PolyMask_Brush";
            }

            // Initialize collections
            if (ControlPoints == null)
            {
                ControlPoints = new List<PolyMaskNode>();
            }

            if (m_polyMaskVisualizations == null)
            {
                m_polyMaskVisualizations = new List<PolyMaskVisualization>();
            }

            // Store initial transform state
            m_visPosition = transform.position;
            m_visRotation = transform.rotation;
            m_visScale = transform.localScale;

            // Assign unique object ID if not already assigned
            if (m_objectID == int.MinValue)
            {
                m_objectID = GetInstanceID();
            }
        }


        /// <summary>
        /// Generates the centroid of all of the points in the list,
        /// and positions the poly mask at that centroid, and ensures that it
        /// is on the pmv.
        Vector3 CalculateVisualizationCentroid(Vector3 position)
        {
            Vector3 centroid = position;
            if (ControlPoints.Count == 0)
            {
                return centroid;
            }

            foreach (PolyMaskNode node in ControlPoints)
            {
                centroid += node.Position;
            }
            centroid /= ControlPoints.Count;

            //Pin it to the a collider (eg terrain)
            RaycastHit hit;
            Ray ray = new Ray(centroid + Vector3.up * 500f, Vector3.down);
            if (Physics.Raycast(ray, out hit))
            {
                centroid = hit.point;
            }
            return centroid;
        }

        /// <summary>
        /// Clear the render textures
        /// </summary>
        private void ClearRenderTextures()
        {
            Graphics.Blit(Texture2D.blackTexture, m_visualizationBrushRT);
            Graphics.Blit(Texture2D.blackTexture, m_visualizationDestRT);
        }

        /// <summary>
        /// Update the visualization on each terrain
        /// </summary>
        public void UpdateVisualization()
        {
            if (m_visualizationBrushRT == null || m_visualizationDestRT == null || m_floodFillMaterial == null || m_brushMaterial == null)
            {
                Initialize();
            }

            //Remove any invalid visualizations
            RemoveInvalidVisualizations();

            //Check for name changes and duplicates, expensive so we want to avoid this, and the name change is the easiest to check
            if (IsNameChanged())
            {
                ValidateAndFixVisualizations();
            }

            //Add any missing visualizations
            AddMissingVisualizations();

            //Update the visualization on each terrain
            foreach (PolyMaskVisualization pmv in m_polyMaskVisualizations)
            {
                RenderPMV(pmv);
            }

            //Call the updated event
            if (OnVisualizationUpdated != null)
            {
                OnVisualizationUpdated(this);
            }
        }

        /// <summary>
        /// Renders a single PMV into the destination render textures
        /// </summary>
        /// <param name="pmv"></param>
        public void RenderPMV(PolyMaskVisualization pmv)
        {
            //Update the visualization textures etc
            ClearRenderTextures();
            if (MaskType == PolyMaskType.Closed && ControlPoints.Count > 2)
            {
                ApplyFloodFill(pmv);
            }
            foreach (PolyMaskNode node in ControlPoints)
            {
                ApplyBrushStroke(pmv, BrushTexture, node.Position, node.Strength, node.Radius);
            }
            ApplyRenderToTexture(pmv);
        }

        /// <summary>
        /// Remove invalid visualizations - invalid is define as either a null game object or
        /// a visualition with the same name. 
        /// </summary>
        private void RemoveInvalidVisualizations()
        {
            //Handle invalid visualizations by removing them
            List<PolyMaskVisualization> pmvSource = m_polyMaskVisualizations.ToList();
            Dictionary<string, PolyMaskVisualization> pmvDict = new Dictionary<string, PolyMaskVisualization>();
            foreach (PolyMaskVisualization pmv in pmvSource)
            {
                if (!pmv.IsVisualizationValid())
                {
                    m_polyMaskVisualizations.Remove(pmv);
                    if (pmv.VisObject != null)
                    {
                        GameObject.DestroyImmediate(pmv.VisObject);
                    }
                }
                else
                {
                    //Handle duplicates visualization objects (should never happen) by removing them and deleting their objects
                    if (!pmvDict.ContainsKey(pmv.VisObject.name))
                    {
                        pmvDict.Add(pmv.VisObject.name, pmv);   
                    }
                    else
                    {
                        m_polyMaskVisualizations.Remove(pmv);
                        if (pmv.VisObject != null)
                        {
                            GameObject.DestroyImmediate(pmv.VisObject);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validate and fix the visualizations. Makes sure that the right visualizations are in place
        /// for this mask. Handles name changes, duplications, addition and deletion.
        /// </summary>
        private void ValidateAndFixVisualizations()
        {
            //Handle invalid visualizations first by removing them
            RemoveInvalidVisualizations();

            //Handle duplications caused by name changes by creating new visualizations and removing the references to the old ones
            if (IsDuplicate())
            {
                //Increment the mask color to make it different
                IncrementColor();

                //Duplicate the visualizations
                List<PolyMaskVisualization> pmvSource = m_polyMaskVisualizations.ToList();
                foreach (PolyMaskVisualization pmv in pmvSource)
                {
                    m_polyMaskVisualizations.Remove(pmv);
                    PolyMaskVisualization newPmv = new PolyMaskVisualization(this, pmv);
                    newPmv.SetName(gameObject.name);
                    m_polyMaskVisualizations.Add(newPmv);
                }
            }

            //Handle name changes
            if (IsNameChanged())
            {
                foreach (PolyMaskVisualization pmv in m_polyMaskVisualizations)
                {
                    pmv.SetName(gameObject.name);
                }
            }

            //Now add any missing visualizations
            foreach (PolyMaskNode node in ControlPoints)
            {
                //Check to see if we have a visualization at this point
                PolyMaskVisualization pmv = GetVisualizationAtPosition(node.Position);

                //If we don't have a visualization then create one
                if (pmv == null)
                {
                    //Create a new visualization
                    Terrain terrain = Gaia.TerrainHelper.GetTerrainStrict(node.Position);
                    if (terrain != null)
                    {
                        AddVisualizationAtPosition(node.Position, terrain);
                    }
                }
            }

            //And make them active
            foreach (PolyMaskVisualization pmv in m_polyMaskVisualizations)
            {
                pmv.ShowVisualizationMesh();
            }
        }


        /// <summary>
        /// Increment the color of the mask
        /// </summary>
        /// <param name="percentage">Amount to increment it by</param>
        private void IncrementColor(float percentage = 0.10f)
        {
            float h, s, v;
            Color.RGBToHSV(MaskColor, out h, out s, out v);
            h += percentage;
            if (h > 1.0f)
            {
                h = 1f - h;
            }
            MaskColor = Color.HSVToRGB(h, 1f, 1f);
        }

        /// <summary>
        /// Determine if this this mask is a duplicate of another mask based on shared visualizations. A mask should
        /// never share its visualization with another mask as each mask is different and independant.
        /// This is an expensive operation as it forces a scane of all game objects in the scene 
        /// so it should be used sparingly.
        /// </summary>
        /// <returns>True if this mask is a duplicate</returns>
        public bool IsDuplicate()
        {
            if (m_polyMaskVisualizations.Count > 0)
            {
                //Get the first visualization we can find
                GameObject targetPmvGo = m_polyMaskVisualizations[0].VisObject;

                //Now see if we have any other masks with the same visualization
                PolyMask[] polyMasks = FindObjectsByType<PolyMask>(FindObjectsSortMode.None);
                foreach (PolyMask polyMask in polyMasks)
                {
                    if (polyMask == this)
                    {
                        continue;
                    }
                    foreach (PolyMaskVisualization pmv in polyMask.m_polyMaskVisualizations)
                    {
                        if (pmv.VisObject == targetPmvGo)
                        {
                            Debug.Log("Duplicate mask detected");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determine if the name has been changed 
        /// </summary>
        /// <returns>True if the name has been changed</returns>
        public bool IsNameChanged()
        {
            if (m_polyMaskVisualizations.Count > 0)
            {
                if (m_polyMaskVisualizations[0].VisObject != null)
                {
                    if (!m_polyMaskVisualizations[0].VisObject.name.StartsWith(gameObject.name))
                    {
                        return true;
                    }
                }
            }
            return false;
        }   

        /// <summary>
        /// Apply the flood fill to each terrain
        /// </summary>
        private void ApplyFloodFill(PolyMaskVisualization pmv)
        {
            //Generate the polygon UVs
            List<Vector2> polygonUVs = new List<Vector2>();
            float meshSize = pmv.VisBounds.size.x;
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                Vector3 localVertex = pmv.VisBounds.max - ControlPoints[i].Position;
                Vector2 uv = new Vector2(1f - (localVertex.x / meshSize), 1f - (localVertex.z / meshSize));
                polygonUVs.Add(uv);
            }
            ComputeBuffer uvBuffer = new ComputeBuffer(polygonUVs.Count, sizeof(float) * 2);
            uvBuffer.SetData(polygonUVs);
            m_floodFillMaterial.SetColor("_FillColor", new Color(1,1,1,1));
            m_floodFillMaterial.SetBuffer("_UVCoordinates", uvBuffer);
            m_floodFillMaterial.SetInt("_UVCount", polygonUVs.Count);
            Graphics.Blit(m_visualizationDestRT, m_floodFillMaterial);
            uvBuffer.Release(); 
        }

        /// <summary>
        /// Apply a brush stroke to the visualization texture
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="position"></param>
        /// <param name="strength"></param>
        /// <param name="radius"></param>
        private void ApplyBrushStroke(PolyMaskVisualization pmv, Texture2D brush, Vector3 position, float strength, float radius)
        {
            if (brush == null)
            {  
                return; 
            }
            float brushScale = pmv.VisBounds.size.x / (radius * 2f);
            Vector2 imageScale = Vector2.one * brushScale;
            float offsetX = ((position.x - radius - pmv.VisBounds.min.x) / pmv.VisBounds.size.x) * brushScale * -1f;
            float offsetZ = ((position.z - radius - pmv.VisBounds.min.z) / pmv.VisBounds.size.z) * brushScale * -1f;
            Vector2 imageOffset = new Vector2(offsetX, offsetZ);
            Graphics.Blit(brush, m_visualizationBrushRT, imageScale, imageOffset);
            Graphics.Blit(m_visualizationBrushRT, m_visualizationDestRT, m_brushMaterial);
        }

        /// <summary>
        /// Render the mask to the visualization texture
        /// </summary>
        private void ApplyRenderToTexture(PolyMaskVisualization pmv)
        {
            var oldRt = RenderTexture.active;
            RenderTexture.active = m_visualizationDestRT;
            pmv.VisTexture.ReadPixels(new Rect(0, 0, m_visualizationDestRT.width, m_visualizationDestRT.height), 0, 0);
            pmv.VisTexture.Apply();
            RenderTexture.active = oldRt;
            pmv.VisMaterial.SetColor("_Color", MaskColor);
            pmv.VisMaterial.SetFloat("_Intensity", MaskIntensity);
        }

        /// <summary>
        /// Show the visualization meshes
        /// </summary>
        public void ShowVisualizationMeshes()
        {
            foreach (PolyMaskVisualization pmv in m_polyMaskVisualizations)
            {
                pmv.ShowVisualizationMesh();
            }
        }

        /// <summary>
        /// Hide the visualization meshes
        /// </summary>
        public void HideVisualizationMeshes()
        {
            foreach (PolyMaskVisualization pmv in m_polyMaskVisualizations)
            {
                pmv.HideVisualizationMesh();
            }
        }

        #endregion

        #region Terrain / region methods

        /// <summary>
        /// Add the specified pmv to the brush terrains at the position given.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="terrain"></param>
        private void AddVisualizationAtPosition(Vector3 position, Terrain terrain)
        {
            //Sanity check the pmv object
            if (terrain == null)
            {
                Debug.LogWarning("Attempting to add a null terrain to the mask system.");
                return;
            }   

            //Check to see if we already have this pmv
            if (GetVisualizationAtPosition(position) != null)
            {
                return;
            }
            //If we don't have a pmv then exit
            if (terrain != null)
            {
                PolyMaskVisualization pmv = new PolyMaskVisualization(this, terrain, VisualizationMeshResolution, VisualizationTextureResolution);
                pmv.SetName(gameObject.name);
                m_polyMaskVisualizations.Add(pmv);
            }
        }

        /// <summary>
        /// Get the pmv at the position
        /// </summary>
        /// <param name="position"></param>
        /// <returns>m_terrain if we have one registered there</returns>
        PolyMaskVisualization GetVisualizationAtPosition(Vector3 position)
        {
            foreach (PolyMaskVisualization pmv in m_polyMaskVisualizations)
            {
                if (pmv.IsPointInBounds(position))
                {
                    return pmv;
                }
            }
            return null;
        }

        #endregion

        #region Mask Export methods

        /// <summary>
        /// Export all the masks to disk
        /// </summary>
        public void ExportMasks()
        {
            foreach (PolyMaskVisualization pmv in m_polyMaskVisualizations)
            {
                if (pmv != null)
                {
                    pmv.ExportMask();
                }
            }
        }

        /// <summary>
        /// Ping all the first mask to show where it is on disk
        /// </summary>
        public void PingFirstExportedMask()
        {
            foreach (PolyMaskVisualization pmv in m_polyMaskVisualizations)
            {
                if (pmv != null)
                {
                    pmv.PingExportedMask();
                    return;
                }
            }
        }


        /// <summary>
        /// Ping all the masks to show where they are on disk
        /// </summary>
        public void PingAllExportedMasks()
        {
            foreach (PolyMaskVisualization pmv in m_polyMaskVisualizations)
            {
                if (pmv != null)
                {
                    pmv.PingExportedMask();
                }
            }
        }

        #endregion
    }
}