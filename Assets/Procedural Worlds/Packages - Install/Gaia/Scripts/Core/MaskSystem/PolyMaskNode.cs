using System;
using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// A node for a polymask mask
    /// </summary>

    [Serializable]
    public class PolyMaskNode
    {
        public int ID; 
        public Vector3 Position;
        public float Radius = 1f;
        public float Feather = 1f;
        public float Strength = 1f;
    }
}