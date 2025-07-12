using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaia
{
    /// <summary>
    /// A visualization mesh and texture are calculated for each terrain in the scene. This makes it easy to break things down
    /// into smaller chunks and only update the visualization for the terrain that is currently being edited. This also provides
    /// a simple and nice way to feed these into Gaia as masks!
    /// </summary>
    [Serializable]
    public class PolyMaskVisualization
    {
        /// <summary>
        /// The mask we belong to
        /// </summary>
        public PolyMask Mask;

        /// <summary>
        /// The original mask name - based on terrain it was created on
        /// </summary>
        public string MaskName;

        /// <summary>
        /// The game object that holds the visualization
        /// </summary>
        public GameObject VisObject;

        /// <summary>
        /// The bounds of this mask
        /// </summary>
        public Bounds VisBounds;

        /// <summary>
        /// The texture resolution for this terrain
        /// </summary>
        public int VisMeshResolution = 10;

        /// <summary>
        /// The texture resolution for this terrain
        /// </summary>
        public int VisTextureResolution = 512;

        /// <summary>
        /// The visualization texture for this terrain
        /// </summary>
        public Texture2D VisTexture;

        /// <summary>
        /// The visualization material for this terrain
        /// </summary>
        public Material VisMaterial;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask">Mask that owns this visualization</param>
        /// <param name="terrain">The terrain this is based on</param>
        /// <param name="meshResolution">The generated mesh resolution</param>
        /// <param name="textureResolution">The generated texture resolution</param>
        public PolyMaskVisualization(PolyMask mask, Terrain terrain, int meshResolution = 10, int textureResolution = 512)
        {
            Mask = mask;
            MaskName = terrain.name;
            VisMeshResolution = meshResolution;
            VisTextureResolution = textureResolution;
            VisBounds = new Bounds(terrain.transform.position + terrain.terrainData.size * 0.5f, terrain.terrainData.size);
            Initialize();
            UpdateVisualizationMesh();
        }

        /// <summary>
        /// Create a visualization object from an existing visualization object
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="sourcePmv"></param>
        public PolyMaskVisualization(PolyMask mask, PolyMaskVisualization sourcePmv)
        {
            Mask = mask;
            MaskName = sourcePmv.MaskName;
            VisMeshResolution = sourcePmv.VisMeshResolution;
            VisTextureResolution = sourcePmv.VisTextureResolution;
            VisBounds = new Bounds(sourcePmv.VisBounds.center, sourcePmv.VisBounds.size);
            Initialize();
            UpdateVisualizationMesh();
        }

        /// <summary>
        /// Initialize the visualization object
        /// </summary>
        private void Initialize()
        {
            //Do the high level scaffolding
            GameObject gaiaTools = GameObject.Find("Gaia Tools");
            if (gaiaTools == null)
            {
                gaiaTools = new GameObject("Gaia Tools");
                gaiaTools.tag = "EditorOnly";
                gaiaTools.hideFlags = HideFlags.DontSaveInBuild;
            }
            GameObject polyMasks = FindChildByName(gaiaTools, "PolyMasks");
            if (polyMasks == null)
            {
                polyMasks = new GameObject("PolyMasks");
                polyMasks.tag = "EditorOnly";
                polyMasks.hideFlags = HideFlags.DontSaveInBuild;
                polyMasks.transform.parent = gaiaTools.transform;
            }
            GameObject vizData = FindChildByName(polyMasks, "Visualization Data");
            if (vizData == null)
            {
                vizData = new GameObject("Visualization Data");
                vizData.tag = "EditorOnly";
                vizData.hideFlags = HideFlags.DontSaveInBuild;
                vizData.transform.parent = polyMasks.transform;
            }
            VisObject = new GameObject("PMV-" + MaskName);
            VisObject.transform.position = new Vector3(VisBounds.center.x, 0f, VisBounds.center.z);
            VisObject.transform.parent = vizData.transform;
            VisObject.tag = "EditorOnly";
            VisObject.hideFlags = HideFlags.DontSaveInBuild;
            VisTexture = new Texture2D(VisTextureResolution, VisTextureResolution, TextureFormat.ARGB32, false);
            VisTexture.wrapMode = TextureWrapMode.Clamp;

            Mesh mesh = new Mesh();
            MeshFilter filter = VisObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = VisObject.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("PWS/MaskPreview");
            VisMaterial = new Material(shader);
            VisMaterial.SetTexture("_MainTex", VisTexture);
            VisMaterial.SetColor("_Color", Mask.MaskColor);
            VisMaterial.SetFloat("_Intensity", Mask.MaskIntensity);
            renderer.sharedMaterial = VisMaterial;
            renderer.receiveShadows = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            //Useful variables
            float size = VisBounds.size.x;
            float halfSize = size / 2f;
            float stepSize = size / VisMeshResolution;
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            // Create vertices and UVs
            for (int i = 0; i <= VisMeshResolution; i++)
            {
                float z = -halfSize + i * stepSize;
                for (int j = 0; j <= VisMeshResolution; j++)
                {
                    //vertices
                    float x = -halfSize + j * stepSize;
                    Vector3 vertex = new Vector3(x, 0, z); // Local space
                    vertices.Add(vertex);
                    //uvs
                    float u = (x + halfSize) / size;
                    float v = (z + halfSize) / size;
                    uvs.Add(new Vector2(u, v));
                }
            }

            // Create triangles
            for (int i = 0; i < VisMeshResolution; i++)
            {
                for (int j = 0; j < VisMeshResolution; j++)
                {
                    int index = i * (VisMeshResolution + 1) + j;
                    triangles.Add(index);
                    triangles.Add(index + VisMeshResolution + 1);
                    triangles.Add(index + 1);
                    triangles.Add(index + 1);
                    triangles.Add(index + VisMeshResolution + 1);
                    triangles.Add(index + VisMeshResolution + 2);
                }
            }

            // Assign vertices, triangles, and UVs to the mesh
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);

            // Recalculate normals and bounds
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            //Apply the mesh to the mesh filter
            filter.sharedMesh = mesh;
        }

        /// <summary>
        /// Set the name of the visualization object
        /// </summary>
        /// <param name="name"></param>
        public void SetName(string name)
        {
            if (VisObject == null)
            {
                Debug.LogError("Can't update visualization object name - visualization object is null!");
                return;
            }
            string newName = name + "-" + MaskName;
            if (VisObject.name != newName)
            {
                VisObject.name = newName;
            }
        }

        /// <summary>
        /// Check to see if the position is in bounds
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool IsPointInBounds(Vector3 position)
        {
            return VisBounds.Contains(position);
        }

        /// <summary>
        /// Do a quick sanity check to see if the visualization is valid
        /// </summary>
        /// <returns>True if we look good, false otherwise</returns>
        public bool IsVisualizationValid()
        {
            if (Mask == null)
            {
                return false;
            }

            if (VisObject == null)
            {
                return false;
            }

            if (VisTexture == null)
            {
                return false;
            }

            if (VisMaterial == null)
            {
                return false;
            }

            if (Mask.ControlPoints.Count == 0)
            {
                return false;
            }

            // Check if any control point is in bounds
            foreach (PolyMaskNode node in Mask.ControlPoints)
            {
                if (IsPointInBounds(node.Position))
                {
                    return true;
                }
            }

            // Even if no points are in bounds, check if the polygon intersects or encapsulates the bounds
            if (Mask.MaskType == PolyMask.PolyMaskType.Closed)
            {
                // Get the corners of our visualization bounds
                Vector3[] boundCorners = new Vector3[4];
                boundCorners[0] = new Vector3(VisBounds.min.x, VisBounds.center.y, VisBounds.min.z); // Bottom Left
                boundCorners[1] = new Vector3(VisBounds.max.x, VisBounds.center.y, VisBounds.min.z); // Bottom Right
                boundCorners[2] = new Vector3(VisBounds.max.x, VisBounds.center.y, VisBounds.max.z); // Top Right
                boundCorners[3] = new Vector3(VisBounds.min.x, VisBounds.center.y, VisBounds.max.z); // Top Left

                // Check if any corner is inside the polygon
                for (int i = 0; i < boundCorners.Length; i++)
                {
                    if (IsPointInPolygon(boundCorners[i]))
                    {
                        return true;
                    }
                }

                // Check if any polygon edge intersects with bounds edges
                for (int i = 0; i < Mask.ControlPoints.Count; i++)
                {
                    Vector3 p1 = Mask.ControlPoints[i].Position;
                    Vector3 p2 = Mask.ControlPoints[(i + 1) % Mask.ControlPoints.Count].Position;

                    for (int j = 0; j < boundCorners.Length; j++)
                    {
                        Vector3 b1 = boundCorners[j];
                        Vector3 b2 = boundCorners[(j + 1) % boundCorners.Length];

                        if (DoLinesIntersect(p1, p2, b1, b2))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsPointInPolygon(Vector3 point)
        {
            bool inside = false;
            int j = Mask.ControlPoints.Count - 1;

            for (int i = 0; i < Mask.ControlPoints.Count; i++)
            {
                Vector3 pi = Mask.ControlPoints[i].Position;
                Vector3 pj = Mask.ControlPoints[j].Position;

                if (((pi.z > point.z) != (pj.z > point.z)) &&
                    (point.x < (pj.x - pi.x) * (point.z - pi.z) / (pj.z - pi.z) + pi.x))
                {
                    inside = !inside;
                }
                j = i;
            }

            return inside;
        }

        private bool DoLinesIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            float denominator = ((p4.z - p3.z) * (p2.x - p1.x)) - ((p4.x - p3.x) * (p2.z - p1.z));

            if (denominator == 0)
            {
                return false;
            }

            float ua = (((p4.x - p3.x) * (p1.z - p3.z)) - ((p4.z - p3.z) * (p1.x - p3.x))) / denominator;
            float ub = (((p2.x - p1.x) * (p1.z - p3.z)) - ((p2.z - p1.z) * (p1.x - p3.x))) / denominator;

            return (ua >= 0 && ua <= 1) && (ub >= 0 && ub <= 1);
        }

        /// <summary>
        /// Update the mesh to match the terrain heights.
        /// </summary>
        public void UpdateVisualizationMesh()
        {
            //Sanity check
            if (VisObject == null)
            {
                Debug.LogError("Can't update visualization mesh - visualization object is null!");
                return;
            }

            //Update the mesh and the visualization
            Mesh mesh = VisObject.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 localVertex = vertices[i];
                Vector3 worldVertex = VisObject.transform.TransformPoint(localVertex); // Convert to world space

                // Cast a ray downward from above the vertex
                Vector3 rayOrigin = worldVertex + (Vector3.up * 500f);

                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, Vector3.down, out hit))
                {
                    // Get the local position of the hit position
                    Vector3 localHitPoint = VisObject.transform.InverseTransformPoint(hit.point);
                    vertices[i] = new Vector3(localVertex.x, localHitPoint.y, localVertex.z);
                }
            }
            mesh.SetVertices(vertices);
            mesh.RecalculateNormals();
        }

        /// <summary>
        /// Show the visualization mesh
        /// </summary>
        public void ShowVisualizationMesh()
        {
            if (VisObject != null)
            {
                VisObject.SetActive(true);
            }
        }

        /// <summary>
        /// Hide the visualization mesh
        /// </summary>
        public void HideVisualizationMesh()
        {
            if (VisObject != null)
            {
                VisObject.SetActive(false);
            }
        }

        /// <summary>
        /// Get the child object by name
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="childName"></param>
        /// <returns></returns>
        private GameObject FindChildByName(GameObject parent, string childName)
        {
            Transform childTransform = parent.transform.Find(childName);
            if (childTransform != null)
            {
                return childTransform.gameObject;
            }
            return null;
        }

        /// <summary>
        /// Export the texture to disk
        /// </summary>
        public void ExportMask()
        {
            string path = GaiaDirectories.GetMaskExportPathForSession();
            string fileName = VisObject.name + ".png";
            string fullPath = path + "/" + fileName;
            byte[] bytes = VisTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, bytes);
#if UNITY_EDITOR
            // Change import settings to RFloat
            TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.sRGBTexture = true;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    format = TextureImporterFormat.RGB16,
                    overridden = true,
                    name = UnityEditor.Build.NamedBuildTarget.Standalone.TargetName
                }); 

                importer.SaveAndReimport();
            }
#endif
            Debug.Log("Exported mask texture to " + fullPath);
        }

        /// <summary>
        /// Ping the exported mask in the editor
        /// </summary>
        public void PingExportedMask()
        {
            #if UNITY_EDITOR
            string path = GaiaDirectories.GetMaskExportPathForSession();
            string fileName = VisObject.name + ".png";
            string fullPath = path + "/" + fileName;
            UnityEngine.Object asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
            }
            #endif
        }
    }    
}