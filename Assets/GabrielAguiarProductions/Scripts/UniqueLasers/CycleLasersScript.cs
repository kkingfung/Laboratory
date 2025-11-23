
#pragma warning disable 0414 // private field assigned but not used.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GAP_LaserSystem;

public class CycleLasersScript : MonoBehaviour {

	public GameObject playerFirePoint;
	public List<GameObject> targetPoints;
	public List<GameObject> Lasers = new List<GameObject> ();

	private int count = 0;
	private GameObject newLaser;
	private LaserScript laserScript;
	private WaitForSeconds shortWait = new WaitForSeconds (0.5f);
	private WaitForSeconds longWait = new WaitForSeconds (4);

	void Start () {
		StartCoroutine (CycleLasers());
	}

	IEnumerator CycleLasers (){
		for(int i = 0; i<Lasers.Count; i++){
			
			newLaser = Instantiate (Lasers [i]);
			laserScript = newLaser.GetComponent<LaserScript> ();
			//laserScript.bounces = 1;   add bounces to the Laser
			laserScript.useTrail = false;
			laserScript.firePoint = playerFirePoint;
			laserScript.endPoint = targetPoints [Random.Range (0, targetPoints.Count)].gameObject;
			newLaser.SetActive (true);
			laserScript.ShootLaser (3);

			yield return longWait;

			Destroy (newLaser);
		}

		StartCoroutine (CycleLasers());
	}
}
