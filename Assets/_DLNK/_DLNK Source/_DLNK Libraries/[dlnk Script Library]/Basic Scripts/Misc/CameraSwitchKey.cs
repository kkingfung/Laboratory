using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitchKey : MonoBehaviour
{
    public Camera Cam1;
    public Camera Cam2;
    public KeyCode CamSwitchKey = KeyCode.F5;

    public void Start()
    {
        if (Cam1.gameObject.activeInHierarchy && Cam2.gameObject.activeInHierarchy)
        {
            Cam1.gameObject.SetActive(true);
            Cam2.gameObject.SetActive(false);
        }

    }
    void Update()
    {
        if (Input.GetKeyDown(CamSwitchKey))
        {
            if (Cam1.gameObject.activeInHierarchy)
            {
                Cam1.gameObject.SetActive(false);
                Cam2.gameObject.SetActive(true);
            }
            else
            {
                Cam1.gameObject.SetActive(true);
                Cam2.gameObject.SetActive(false);
            }
        }
    }
}
