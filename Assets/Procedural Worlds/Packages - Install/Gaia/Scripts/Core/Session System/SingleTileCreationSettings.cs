using Gaia;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleTileCreationSettings : ScriptableObject
{
    public string m_terrainName;
    public Vector3 m_position;
    public int m_terrainSize;
    public GaiaDefaults m_gaiaDefaults;
    public float m_terrainHeight;
    public bool m_createInTerrainScene;
    public TerrainLayer[] m_terrainLayers;
    public DetailPrototype[] m_terrainDetails;
    public TreePrototype[] m_terrainTrees;
}
