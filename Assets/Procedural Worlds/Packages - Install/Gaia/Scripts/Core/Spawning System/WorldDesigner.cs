using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gaia
{
    /// <summary>
    /// This class only exists to change the displayed name from "Spawner" to "World Generator" on the component.
    /// </summary>
    public class WorldDesigner : Spawner
    {
        // Static event triggered when the world designer is created
        public static event Action<GameObject> OnWorldDesignerCreated;

        private void Awake()
        {
            OnWorldDesignerCreated?.Invoke(this.gameObject);
        }

    }
}
