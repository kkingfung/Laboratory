using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GAP_LaserSystem;

public class SwitchLasersScript : MonoBehaviour {
	
	public Text effectName;
	public Text bouncesText;
	public Text sizeText;
	public GameObject fixedCamera;
	public GameObject fpsCamera;
	public List<GameObject> Lasers = new List<GameObject> ();

	private int count = 0;
	private float newSize = 1;
	private float originalSize;
	private GameObject activeLaser;
	private LaserScript laserScript;

	void Start () {
		activeLaser = Lasers [0];
		laserScript = activeLaser.GetComponent<LaserScript> ();
		
		if (effectName != null) effectName.text = activeLaser.name;
		if (bouncesText != null) bouncesText.text = "Bounces: " + laserScript.bounces;
		if (sizeText != null) sizeText.text = "Size: " + newSize;
		originalSize = laserScript.size;
	}

	void Update () {

		//enable the laser once we press mouse 1
		if (Input.GetMouseButtonDown (0)) {		
			laserScript.EnableLaser ();
			activeLaser.SetActive (true);

			//hides and locks the mouse to the center, if its a FPS Camera
			if (fpsCamera != null) {
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
		}

		//holding Mouse 1 to keep the laser firing
		if (Input.GetMouseButton (0)) {
			laserScript.UpdateLaser ();
		}

		//once we stop shooting we disable laser. With or without a delay
		if(Input.GetMouseButtonUp (0)){
			laserScript.DisableLaserCaller (laserScript.disableDelay);
		}

		if (Input.GetKeyDown (KeyCode.E))
			Next ();		

		if (Input.GetKeyDown (KeyCode.Q)) 
			Previous ();	

		if (Input.GetKeyDown (KeyCode.C) && fixedCamera != null && fpsCamera != null) {
			ChangeCamera ();
			RefreshLaser ();
		}

		if(Input.GetKeyDown (KeyCode.R)){
			laserScript.bounces++;
			if (bouncesText != null) bouncesText.text = "Bounces: " + laserScript.bounces;		
		}

		if(Input.GetKeyDown (KeyCode.F)){
			if (laserScript.bounces > 0) {
				laserScript.bounces--;
				laserScript.RemoveLastByType (PSList.PSLIST_TYPE.start);
				laserScript.RemoveLastByType (PSList.PSLIST_TYPE.middle);
				laserScript.RemoveLastByType (PSList.PSLIST_TYPE.end);
				laserScript.RemoveLastPositionLRs ();
				laserScript.StopLastTrail ();
			}
			if (bouncesText != null) bouncesText.text = "Bounces: " + laserScript.bounces;		
		}

		if(Input.GetKeyDown (KeyCode.X)){
			OriginalSize ();
			newSize += 0.1f;
			newSize = (float)System.Math.Round (newSize,2);
			laserScript.size = newSize;
			laserScript.Resize (true);
			if (sizeText != null) sizeText.text = "Size: " + newSize;	
		}

		if(Input.GetKeyDown (KeyCode.Z)){
			if (newSize > 0.1f) {
				OriginalSize ();
				newSize -= 0.1f;
				newSize = (float)System.Math.Round (newSize,2);
				laserScript.size = newSize;
				laserScript.Resize (true);
			}
			if (sizeText != null) sizeText.text = "Size: " + newSize;	
		}
	}

	void OriginalSize (){
		laserScript.size = 1 / newSize;
		laserScript.Resize (true);
	}

	public void Next () {
		count++;

		if (count > Lasers.Count)
			count = 0;

		for(int i = 0; i < Lasers.Count; i++){
			if (count == i) {
				laserScript.DisableLaserCaller (0);
				activeLaser = Lasers [i];
				activeLaser.SetActive (false);
				laserScript = activeLaser.GetComponent<LaserScript> ();
				newSize = 1;
			}
			if (effectName != null)	effectName.text = activeLaser.name;
			if (bouncesText != null) bouncesText.text = "Bounces: " + laserScript.bounces;	
			if (sizeText != null) sizeText.text = "Size: " + laserScript.size;	
		}
	}

	public void Previous () {
		count--;

		if (count < 0)
			count = Lasers.Count;

		for(int i = 0; i < Lasers.Count; i++){
			if (count == i) {
				laserScript.DisableLaserCaller (0);
				activeLaser = Lasers [i];
				activeLaser.SetActive (false);
				laserScript = activeLaser.GetComponent<LaserScript> ();
				newSize = 1;
			}
			if (effectName != null)	effectName.text = activeLaser.name;
			if (bouncesText != null) bouncesText.text = "Bounces: " + laserScript.bounces;	
			if (sizeText != null) sizeText.text = "Size: " + laserScript.size;
		}
	}

	public void ChangeCamera () {
		if (!fixedCamera.activeSelf) {
			fixedCamera.SetActive (true);
			fpsCamera.SetActive (false);
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		} else {
			fixedCamera.SetActive (false);
			fpsCamera.SetActive (true);
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	public void RefreshLaser (){
		for(int i = 0; i < Lasers.Count; i++){
			laserScript = Lasers[i].GetComponent<LaserScript> ();
			if (fixedCamera.gameObject.activeSelf) {
				laserScript.firePoint = Lasers [i];
				laserScript.endPoint = fixedCamera;
			}
			else{
				laserScript.firePoint = fpsCamera.transform.FindDeepChild("FirePoint").gameObject;
				laserScript.endPoint = fpsCamera;
			}
		}
	}
}
