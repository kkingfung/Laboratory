using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserGunScript : MonoBehaviour {

	private Animator anim;

	void Start(){
		anim = GetComponent<Animator> ();
	}

	void Update () {
		if (Input.GetMouseButtonDown (0)) {			
			anim.SetTrigger ("isFiring");
		}

		if(Input.GetMouseButtonUp (0)){		
			anim.SetTrigger ("isIdle");
		}
	}
}
