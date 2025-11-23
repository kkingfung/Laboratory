#pragma warning disable 0162 // unreachable code detected.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GAP_ParticleSystemController;
using UnityEngine.Serialization;

namespace GAP_LaserSystem{

	public class PSList {

		public enum PSLIST_TYPE {start, middle, end};
		public GameObject _parent;
		public List<ParticleSystem> _listPS = new List<ParticleSystem>();
		public PSLIST_TYPE _psType;

		private bool _disable;

		//auxiliary function to add particle systems to a given list from a gameobject
		public void AddPSToList (GameObject obj, bool addFromObj) {
			if (addFromObj) {
				var psObj = obj.GetComponent<ParticleSystem> ();
				if (psObj != null)
					_listPS.Add (psObj);
			}

			if(obj.transform.childCount>0){
				for (int i = 0; i < obj.transform.childCount; i++) {
					var ps = obj.transform.GetChild (i).GetComponent<ParticleSystem> ();
					if(ps != null)
						_listPS.Add (ps);
				}
			}
			else
				Debug.Log ("The GameObject " + obj.name + " contains no childs.");
		}

		//auxiliary function to enable a list of particle systems
		public void EnablePS (){
			for (int i = 0; i < _listPS.Count; i++) {		
				if (_listPS [i].isEmitting == false){
					_disable = false;
					_listPS [i].Play ();
					var emission = _listPS [i].emission;
					emission.enabled = true;
					_listPS [i].gameObject.SetActive (true);
				}
			}
		}

		//auxiliary function to stop and disable emission of a list of particle systems
		public void DisablePS (bool disableImmediately){
			for (int i = 0; i < _listPS.Count; i++) {
				if(_listPS[i].isEmitting == true) {
					_disable = true;
					_listPS [i].Stop ();
					var emission = _listPS [i].emission;
					emission.enabled = false;
					if (disableImmediately == false)
						DelayedDisable (_listPS [i].gameObject, _listPS [i].main.duration + _listPS [i].main.startLifetime.constantMax);
					else
						_listPS [i].gameObject.SetActive (false);
				}
			}
		}

		public void LayerOrder (int amount) {
			for (int i = 0; i < _listPS.Count; i++) {
				_listPS [i].GetComponent<Renderer> ().sortingOrder += amount;
			}
		}

		public bool GetEnabled (){
			return _disable;
		}

		void DelayedDisable (GameObject obj, float delay){
			float timer = 0;

			while (timer <= delay && !_disable) {
				timer += Time.deltaTime;
				if (timer >= delay && _disable) {
					obj.SetActive (false);
				}
			}
		}
	}

	public class Trail {

		public ParticleSystem _ps;
		public Vector3 _trailPosition;
		public Vector3 _trailEulerAngles;
		public float _trailInterval;
		public bool _emittingTrail;

		private ParticleSystem.EmitParams emitParams;

		//emits the trail at a certain rate/interval
		public IEnumerator EmitTrail (){
			if (_emittingTrail == false) {
				_emittingTrail = true;
				emitParams.position = _trailPosition;
				emitParams.rotation3D = -_trailEulerAngles;
				_ps.Emit (emitParams, 1);
			}
			yield return new WaitForSeconds (_trailInterval);
			_emittingTrail = false;
		}
	}

	public class LaserScript : MonoBehaviour {
		
		[Tooltip("Layers where the Lasers will collide")]
		public LayerMask layersToCollide;
		[Tooltip("If this is On, it will apply the colors in New Max Color and New Min Color.")]
		public bool changeColor;
		[Tooltip("The new Max Color. Useful to create range between colors. Leave the same as New Min Color if you want the same color.")]
		public Color newMaxColor = new Color (0,0,0,1);
		[Tooltip("The new Min Color. Useful to create range between colors. Leave the same as New Max Color if you want the same color.")]
		public Color newMinColor = new Color (0,0,0,1);
		[Tooltip("The Line Renderers that are child of this gameobject, in other words the lasers.")]
		public List<LineRenderer> lineRenderers;
		[Tooltip("The starting position of the Laser.")]
		public GameObject firePoint;
		[Tooltip("The end position of the Laser. If using an input to control the endpoint, like the mouse for example, then assign the camera.")]
		[FormerlySerializedAs("cam")]
		public GameObject endPoint;
		[Tooltip("How many times the Laser bounces when it collides.")]
		public int bounces;
		[Tooltip("Multiplies the original size of the Laser (eg: 0.5 - half the size / 2 - double the size)")]
		public float size = 1;
		[Tooltip("The maximum length the Laser will have.")]
		public float maximumLength;
		[Tooltip("How much it overgrows when the Laser is shot.")]
		[FormerlySerializedAs("growWidth")]
		public float overgrow;
		[Tooltip("The speed which the overgrow grows.")]
		[FormerlySerializedAs("growSpeed")]
		public float overgrowSpeed;
		[Tooltip("How fast it shrinks when we stop shooting.")]
		public float shrinkSpeed; 
		[Tooltip("How many seconds before the Laser is disabled.")]
		public float disableDelay;
		[Tooltip("Parent of all the particles systems at the start of the Laser.")]
		public GameObject startVFX;
		[Tooltip("Enable/Disable Start VFX")]
		public bool useStart = true;
		[Tooltip("When using bounces, you can choose if you want to duplicate the Start VFX.")]
		public bool reflectStart;
		[Tooltip("Parent of all the particles systems at the middle of the Laser.")]
		[FormerlySerializedAs("psVFX")]
		public GameObject middleVFX;
		[Tooltip("Enable/Disable Middle VFX")]
		public bool useMiddle = true;
		[Tooltip("When using bounces, you can choose if you want to duplicate the Middle VFX.")]
		public bool reflectMiddle;
		[Tooltip("Parent of all the particle systems at the end of the Laser.")]
		public GameObject endVFX;
		[Tooltip("Enable/Disable End VFX")]
		public bool useEnd = true;
		[Tooltip("Enable/Disable End VFX when not colliding.")]
		public bool useEndAlways;
		[Tooltip("When using bounces, you can choose if you want to duplicate the End VFX.")]
		public bool reflectEnd;
		[Tooltip("The particle system that leaves a trail.")]
		public GameObject trailVFX;
		[Tooltip("Enable or disable trail.")]
		[FormerlySerializedAs("trail")]
		public bool useTrail;
		[Tooltip("When using bounces, you can choose if you want to duplicate the Trail.")]
		public bool reflectTrail;
		[Tooltip("The interval between each trail - 0 means a continuous trail.")]
		public float trailInterval;
		[Tooltip("Generates colliders at the end of every bounce.")]
		public bool generateColliders;
		[Tooltip("Are the colliders trigger?")]
		public bool isColliderTrigger;
		[Tooltip("The colliders radius.")]
		public float collidersRadius = 1;
		[Tooltip("Name of the Layer where the Laser is going to be.")]
		public string layerName = "Default";
		[Tooltip("Moves the Layer Order of the Line Renderers and Particle Systems to a respective value.")]
		public int layerOrderTo = 0;

		private Camera cam;
		private Ray rayMouse;
		private List<Trail> trails = new List<Trail> ();
		private List<PSList> psList = new List<PSList> ();
		private List<float> lrWidth = new List<float> ();
		private Vector3 mouseCurrentPosition;
		private ParticleSystemController psCtrl;
		private ParticleSystem psTrailVFX;
		private bool psListFilled;
		private bool isShooting;
		private bool isColliding;
		private WaitForSeconds growWait = new WaitForSeconds (0.025f);
		private WaitForSeconds updateWait = new WaitForSeconds (0.01f);
		private PSList startPSList = new PSList();
		private PSList middlePSList = new PSList();
		private PSList endPSList = new PSList();
		private List<BoxCollider> collidersList = new List<BoxCollider>();

		void Start () {			

			//Changes color
			psCtrl = GetComponent<ParticleSystemController>();
			if (psCtrl == null && size != 1 || changeColor) {
				gameObject.AddComponent <ParticleSystemController> ();
				psCtrl = GetComponent<ParticleSystemController>();
			}

			if (changeColor) {
				psCtrl.changeColor = changeColor;
				psCtrl.newMaxColor = newMaxColor;
				psCtrl.newMinColor = newMinColor;
				psCtrl.ChangeColorOnly ();
				for(int i = 0; i< lineRenderers.Count; i++){
					lineRenderers [i].colorGradient = psCtrl.ChangeGradientColor (lineRenderers [i].colorGradient, psCtrl.newMaxColor, psCtrl.newMinColor);
				}

			}
			if (size != 1) {
				Resize (false);
			}

			//if the end point is a camera than get the componenet
			if(endPoint != null)
				cam = endPoint.GetComponent<Camera> ();

			if (layerName != "Default") {
				var layerID = LayerMask.NameToLayer (layerName);
				if (layerID != -1) {
					gameObject.layer = layerID;
					ChangeLayerRecursively (gameObject);
				} else {
					Debug.Log ("That Layer Name doesn't exist. Watchout for a typo!");
				}
			}				

			FillLists ();
		}

		//use this function to shoot a laser that isn't in FPS or TPS.
		public void ShootLaser (float duration){
			StartCoroutine (ShootLaserCoroutine(duration));
		}

		IEnumerator ShootLaserCoroutine (float duration){
			yield return new WaitForSeconds (0.02f);//safe wait

			EnableLaser ();

			UpdateLaserContinuously ();

			yield return new WaitForSeconds (duration);

			DisableLaserCaller (disableDelay);
		}

		//enables line renderers and respective particle systems of a Laser. Also adds extra width (growWidth) to each line renderer.
		public void EnableLaser (){

			if (psList.Count > 0) {
				for (int i = 0; i < lineRenderers.Count; i++)
					lineRenderers [i].enabled = true;				

				for(int i = 0; i<psList.Count; i++){
					psList [i].EnablePS ();
				}

				GrowLaserCaller ();
				RotateToMouse (middleVFX, endVFX.transform.position);
			} else {
				FillLists ();
				EnableLaser ();
				return;
			}
		}

		//after 'EnableLaser', 'UpdateLaser' is used to keep the position, normals and trail updated, according to an input or to a gameobject position.
		public void UpdateLaser () {
			if (firePoint != null) {
				for (int i = 0; i < lineRenderers.Count; i++)
					lineRenderers [i].SetPosition (0, firePoint.transform.position);
				
				if (startVFX != null) {
					if (useStart)
						startVFX.transform.position = firePoint.transform.position;
					else
						startPSList.DisablePS (true);
				}
				
				if (middleVFX != null) {
					if (useMiddle)
						middleVFX.transform.position = firePoint.transform.position;
					else
						middlePSList.DisablePS (true);
				}

				if (endVFX != null) {
					if (!useEnd)
						endPSList.DisablePS (true);
				}
			} else {
				Debug.Log ("There is no fire point in the inspector.");
				return;
			}

			if (cam != null) {
				RaycastHit hit;
				var mousePos = Input.mousePosition;
				rayMouse = cam.ScreenPointToRay (mousePos);

				if (Physics.Raycast (rayMouse, out hit, maximumLength, layersToCollide)) {
					isColliding = true;

					for (int i = 0; i < lineRenderers.Count; i++)
						lineRenderers [i].SetPosition (1, hit.point);

					if (startVFX != null && useStart)
						RotateToMouse (startVFX, hit.point);
					
					if (middleVFX != null && useMiddle)
						RotateToMouse (middleVFX, hit.point);					

					if (endVFX != null) {
						endVFX.transform.position = hit.point;
						endVFX.transform.forward = -hit.normal;

						if (generateColliders) {
							if(collidersList.Count == 0)
								GenerateCollider (endVFX);
						}
					}

					if (trailVFX != null && useTrail) {
						if (trails [0]._emittingTrail == false) {
							trails [0]._trailPosition = hit.point;
							trails [0]._trailEulerAngles = Quaternion.LookRotation (-hit.normal).eulerAngles;
							StartCoroutine (trails [0].EmitTrail ());
						}
					}

					if (bounces > 0)
					{	
						for(int i = 0; i<lineRenderers.Count; i++)
							RecursiveBounces (lineRenderers[i], hit.point, Vector3.Reflect(rayMouse.direction, hit.normal), bounces, maximumLength - Vector3.Distance(lineRenderers[i].GetPosition(0), lineRenderers[i].GetPosition(1)));						
					}
					else
					{
						if (lineRenderers[0].positionCount > 2)	{
							for (int i = 0; i < lineRenderers.Count; i++)
								lineRenderers[i].positionCount = 2;
						}

						if (psList.Count > 3) {
							PSListToOriginalSize ();
						}
					}

				//when it's not colliding against anything
				} else 
				{	
					isColliding = false;
					var pos = rayMouse.GetPoint (maximumLength);

					if(startVFX!= null && useStart)
						RotateToMouse (startVFX, pos);
					
					if(middleVFX!= null && useMiddle)
						RotateToMouse (middleVFX, pos);

					if (lineRenderers[0].positionCount > 2)	{
						for (int i = 0; i < lineRenderers.Count; i++)
							lineRenderers[i].positionCount = 2;
					}

					for (int i = 0; i < lineRenderers.Count; i++)
						lineRenderers [i].SetPosition (1, pos);	

					if (psList.Count > 3) {
						PSListToOriginalSize ();
					}
					
					if (endVFX != null) {
						if (useEnd) {
							if (useEndAlways) {
								endVFX.transform.position = pos;
								RotateToMouse (endVFX, middleVFX.transform.position);	
							}else {
								endPSList.DisablePS (true);
							}
						}else {
							endPSList.DisablePS (true);
						}

						if (generateColliders) {
							if(collidersList.Count == 0)
								GenerateCollider (endVFX);
						}
					}

					if (trailVFX != null && useTrail) {
						for(int i = 0; i<trails.Count; i++){
							trails [i]._emittingTrail = false;
							trails [i]._ps.Stop ();
						}
					}
				}

			//Used when it's not a camera
			} else 
			{
				var endPointPos = endPoint.transform.position; 

				for (int i = 0; i < lineRenderers.Count; i++)
					lineRenderers [i].SetPosition (1, endPointPos);	

				if (startVFX != null && useStart)
					RotateToMouse (startVFX, endPointPos);

				if (middleVFX != null && useMiddle)
					RotateToMouse (middleVFX, endPointPos);		
				
				if (endVFX != null) {
					endVFX.transform.position = endPointPos;

					if (generateColliders) {
						if(collidersList.Count == 0)
							GenerateCollider (endVFX);
					}
				}

				if (trailVFX != null && useTrail) {
					if (trails [0]._emittingTrail == false) {
						trails [0]._trailPosition = endPointPos;
						trails [0]._trailEulerAngles = endVFX.transform.eulerAngles;
						StartCoroutine (trails [0].EmitTrail ());
					}
				}

				if (bounces > 0)
				{	
					RaycastHit hit;
					var direction = endPointPos - firePoint.transform.position;
					Ray ray = new Ray (firePoint.transform.position, direction);
					if(Physics.Raycast (ray, out hit)){
						for(int i = 0; i<lineRenderers.Count; i++)
							RecursiveBounces (lineRenderers[i], endPointPos, Vector3.Reflect(ray.direction, hit.normal), bounces, maximumLength - Vector3.Distance(lineRenderers[i].GetPosition(0), lineRenderers[i].GetPosition(1)));
					}
				}
				else
				{
					if (lineRenderers[0].positionCount > 2)	{
						for (int i = 0; i < lineRenderers.Count; i++)
							lineRenderers[i].positionCount = 2;
					}

					if (psList.Count > 3) {
						PSListToOriginalSize ();
					}
				}
			}
		}

		void RecursiveBounces (LineRenderer lr, Vector3 position, Vector3 direction, int bouncesRemaining, float lengthRemaining){			
			if (bouncesRemaining == 0)
				return;
			if (lengthRemaining <= 0.1f)
				return;

			int index = bounces - bouncesRemaining;

			if (lr.positionCount == index+2){
					lr.positionCount ++;
			}

			Ray ray = new Ray (position, direction);
			RaycastHit hit;

			if (Physics.Raycast (ray, out hit, lengthRemaining)) {				
				direction = Vector3.Reflect (direction, hit.normal);	
				lr.SetPosition(index+2, hit.point);

				if (startVFX != null && useStart && reflectStart) {
					if (GetByType (PSList.PSLIST_TYPE.start).Count == index + 1)
						CreateNewPSList (PSList.PSLIST_TYPE.start);

					List<PSList> startListPS = GetByType(PSList.PSLIST_TYPE.start);
					if (startListPS.Count > index) {
						startListPS[index + 1]._parent.transform.position = position;
						RotateToMouse (startListPS[index + 1]._parent, hit.point);
					}
				}

				if (middleVFX != null && useMiddle && reflectMiddle) {
					if (GetByType(PSList.PSLIST_TYPE.middle).Count == index + 1) 
						CreateNewPSList (PSList.PSLIST_TYPE.middle);

					List<PSList> middleListPS = GetByType(PSList.PSLIST_TYPE.middle);
					if (middleListPS.Count > index) {
						middleListPS[index + 1]._parent.transform.position = position;
						RotateToMouse (middleListPS[index + 1]._parent, hit.point);
					}
				}

				if (endVFX != null && useEnd) {
					if (reflectEnd) {
						if (GetByType (PSList.PSLIST_TYPE.end).Count == index + 1)
							CreateNewPSList (PSList.PSLIST_TYPE.end);					

						List<PSList> endListPS = GetByType (PSList.PSLIST_TYPE.end);
						if (endListPS.Count > index) {
							endListPS [index + 1]._parent.transform.position = hit.point;
							endListPS [index + 1]._parent.transform.forward = -hit.normal;
						}
					} else {
						endVFX.transform.position = hit.point;
						endVFX.transform.forward = -hit.normal;
					}
				}

				if (trailVFX != null && useTrail && reflectTrail) {
					if (trails.Count == index + 1) {
						Trail newTrail = new Trail (){_ps = psTrailVFX, _trailInterval = trailInterval};
						trails.Add (newTrail);
					}

					if (trails [index+1]._emittingTrail == false) {
						trails [index+1]._trailPosition = hit.point;
						trails [index+1]._trailEulerAngles = Quaternion.LookRotation (-hit.normal).eulerAngles;
						StartCoroutine (trails [index+1].EmitTrail ());
					}
				}

				if(!useEnd || !reflectEnd && generateColliders){
					if(collidersList.Count == index + 1){
						Debug.Log ("NewColliderAdded");
						GameObject gObj = new GameObject ("NewCollider");
						gObj.transform.SetParent (transform);
						gObj.transform.position = hit.point;
						gObj.transform.forward = -hit.normal;
						GenerateCollider (gObj);
					}

					if (collidersList.Count > index) {
						var coObj = collidersList [index + 1].gameObject;
						coObj.transform.position = hit.point;
						coObj.transform.forward = -hit.normal;
					}
				}
			}
			else {
				if (lr.positionCount > index+3)	{
						lr.positionCount--;
				}
				if (lengthRemaining > 0.1f) {
					var pos = ray.GetPoint (lengthRemaining);

					lr.SetPosition (index + 2, pos);

					if (startVFX != null && useStart && reflectStart) {
						if (GetByType (PSList.PSLIST_TYPE.start).Count == index + 1)
							CreateNewPSList (PSList.PSLIST_TYPE.start);

						List<PSList> startListPS = GetByType(PSList.PSLIST_TYPE.start);
						if (startListPS.Count > index) {
							startListPS[index + 1]._parent.transform.position = position;
							RotateToMouse (startListPS[index + 1]._parent, pos);
						}

						if (startListPS.Count > index + 2) {
							RemoveLastByType (PSList.PSLIST_TYPE.start);
						}
					}

					if (middleVFX != null && useMiddle && reflectMiddle) {
						if (GetByType(PSList.PSLIST_TYPE.middle).Count == index + 1) 
							CreateNewPSList (PSList.PSLIST_TYPE.middle);

						List<PSList> middleListPS = GetByType(PSList.PSLIST_TYPE.middle);
						if (middleListPS.Count > index) {
							middleListPS[index + 1]._parent.transform.position = position;
							RotateToMouse (middleListPS[index + 1]._parent, pos);
						}

						if (middleListPS.Count > index + 2) {
							RemoveLastByType (PSList.PSLIST_TYPE.middle);
						}
					}

					if (endVFX != null && useEnd) {
						if (reflectEnd){
							if (useEndAlways) {
								if (GetByType (PSList.PSLIST_TYPE.end).Count == index + 1)
									CreateNewPSList (PSList.PSLIST_TYPE.end);

								List<PSList> endListPS = GetByType (PSList.PSLIST_TYPE.end);
								if (endListPS.Count > index) {
									endListPS [index + 1]._parent.transform.position = pos;
									RotateToMouse (endListPS [index + 1]._parent, ray.origin);
								}

								if (endListPS.Count > index + 2) {
									RemoveLastByType (PSList.PSLIST_TYPE.end);
								}
							} else {
								List<PSList> endListPS = GetByType (PSList.PSLIST_TYPE.end);
								if (endListPS.Count > index + 1) {
									endListPS [index + 1].DisablePS (false);
								}
							}
						} else {
							endVFX.transform.position = pos;
							RotateToMouse (endVFX, ray.origin);
						}
					}

					if (trailVFX != null && useTrail) {
						for(int i = index+1; i<trails.Count; i++){
							trails [i]._emittingTrail = false;
							trails [i]._ps.Stop ();
						}
					}

					if(!useEnd || !reflectEnd && generateColliders){
						if(collidersList.Count == index + 1){
							Debug.Log ("NewColliderAdded");
							GameObject gObj = new GameObject ("NewCollider");
							gObj.transform.SetParent (transform);
							gObj.transform.position = pos;
							RotateToMouse (gObj, ray.origin);
							GenerateCollider (gObj);
						}

						if (collidersList.Count > index) {
							var coObj = collidersList [index + 1].gameObject;
							coObj.transform.position = pos;
							RotateToMouse (coObj, ray.origin);
						}

						if (collidersList.Count > index + 2) {
							Debug.Log ("Removing Collider from List. Left: " + collidersList.Count);
							RemoveLastCollider (true);
						}
					}
				}
			}

			bouncesRemaining--;
			lengthRemaining -= Vector3.Distance (lr.GetPosition(index+1), lr.GetPosition(index+2));
			RecursiveBounces (lr, hit.point, Vector3.Reflect(ray.direction, hit.normal), bouncesRemaining, lengthRemaining);
		}

		//useful for when not using any input to trigger the Laser (See AutomatedTargets_Scene)
		void UpdateLaserContinuously (){
			StartCoroutine (UpdateLaserCoroutine());
		}

		IEnumerator UpdateLaserCoroutine (){
			while (isShooting) {
				UpdateLaser ();
				yield return updateWait;
			}
		}

		//function to iterate trhough the line renderers and make them grow in the begging 
		void GrowLaserCaller () {
			isShooting = true;
			if (gameObject.activeSelf) {
				for (int i = 0; i < lineRenderers.Count; i++)
					StartCoroutine (GrowLaser (lineRenderers [i], lrWidth [i]));
			} else {
				gameObject.SetActive (true);
				for (int i = 0; i < lineRenderers.Count; i++)
					StartCoroutine (GrowLaser (lineRenderers [i], lrWidth [i]));
			}
		}

		//coroutine used to give some extra width in the beginning
		IEnumerator GrowLaser (LineRenderer lr, float originalWidth){
			while (isShooting && lr.widthMultiplier < (originalWidth + overgrow)-0.001f) {
				if (lr.widthMultiplier + overgrowSpeed > originalWidth + overgrow) {
					lr.widthMultiplier = originalWidth + overgrow;
					lr.widthMultiplier = (float)System.Math.Round (lr.widthMultiplier, 2);
				} else {
					lr.widthMultiplier += overgrowSpeed;
					lr.widthMultiplier = (float)System.Math.Round (lr.widthMultiplier, 2);
				}
				yield return growWait;
			}
			yield return growWait;
			while (isShooting && lr.widthMultiplier > originalWidth) {
				if (lr.widthMultiplier - overgrowSpeed < originalWidth)
					lr.widthMultiplier = originalWidth;
				else
					lr.widthMultiplier -= overgrowSpeed/2;
				yield return growWait;
			}
		}

		public void DisableLaserCaller (float timer){
			if(gameObject.activeSelf)
				StartCoroutine (DisableLaser(timer));
		}

		IEnumerator DisableLaser (float timer){
			isShooting = false;
			isColliding = false;
			ShrinkLaserCaller ();
			HoldLaserCaller ();

			for(int i = 0; i<psList.Count; i++){
				psList [i].DisablePS (false);
			}

			yield return new WaitForSeconds (timer);
			if (isShooting == false) {
				isShooting = true;
				for (int i = 0; i < lineRenderers.Count; i++) 
					lineRenderers[i].enabled = false;
			}
		} 

		//shrink each line renderer
		void ShrinkLaserCaller () {
			for(int i = 0; i< lineRenderers.Count; i++)
				StartCoroutine (ShrinkLaser(lineRenderers[i]));
		}

		IEnumerator ShrinkLaser (LineRenderer lr){
			while (!isShooting && lr.widthMultiplier > 0) {
				var amount = lr.widthMultiplier -= shrinkSpeed;
				if(amount < 0)
					lr.widthMultiplier = 0;
				else
					lr.widthMultiplier = amount;
				yield return growWait;
			}
		}

		//update the laser while shrinking
		void HoldLaserCaller (){
			StartCoroutine (HoldLaser ());
		}
			
		IEnumerator HoldLaser () {
			while (!isShooting){
				UpdateLaser ();
				yield return updateWait;
			}
		}

		//auxiliary function to rotate a gameobject to a destination
		void RotateToMouse (GameObject obj, Vector3 destination ) {
			var direction = destination - obj.transform.position;
			Quaternion rotation = Quaternion.LookRotation (direction);
			obj.transform.rotation = Quaternion.Lerp (obj.transform.rotation, rotation, 1);
		}

		//one time function that fills the line renderers width list; the start particle system list and adds to the psList; 
		//the middle particle system list and adds to the psList; the end particle system list and adds to the psList; creates 1st trail and adds to the trails list
		void FillLists () {
			if (!psListFilled) {
				psListFilled = true;

				//adds the width of the lasers to a list and sets the width to 0
				if(lineRenderers.Count > 0){
					if(lrWidth.Count == 0){
						for (int i = 0; i < lineRenderers.Count; i++) {
							lrWidth.Add(lineRenderers[i].widthMultiplier*size);
							lineRenderers [i].widthMultiplier = 0;
							lineRenderers [i].enabled = false;
							if(layerOrderTo != 0)
								lineRenderers [i].sortingOrder += layerOrderTo;
						}
					}
				}else
				{
					Debug.Log("Line Renderers list is empty.");
				}

				if (startVFX != null) {
					startPSList._parent = startVFX;
					startPSList._psType = PSList.PSLIST_TYPE.start;
					startPSList.AddPSToList (startVFX, true);
					startPSList.DisablePS (true);
					if(layerOrderTo != 0)
						startPSList.LayerOrder (layerOrderTo);
					psList.Add (startPSList);
				}

				if (middleVFX != null) {
					middlePSList._parent = middleVFX;
					middlePSList._psType = PSList.PSLIST_TYPE.middle;
					middlePSList.AddPSToList (middleVFX, true);
					middlePSList.DisablePS (true);
					if(layerOrderTo != 0)
						middlePSList.LayerOrder (layerOrderTo);
					psList.Add (middlePSList);
				}

				if (endVFX != null) {
					endPSList._parent = endVFX;
					endPSList._psType = PSList.PSLIST_TYPE.end;
					endPSList.AddPSToList (endVFX, true);
					endPSList.DisablePS (true);
					if(layerOrderTo != 0)
						endPSList.LayerOrder (layerOrderTo);
					psList.Add (endPSList);
				}

				if (trailVFX != null && useTrail) {
					psTrailVFX = trailVFX.GetComponent<ParticleSystem> ();
					if(layerName != "Default" || layerOrderTo != 0){
						var trailRenderer = psTrailVFX.GetComponent<Renderer>();
						trailRenderer.sortingOrder += layerOrderTo;
					}
					Trail newTrail = new Trail (){_ps = psTrailVFX, _trailInterval = trailInterval};
					trails.Add (newTrail);
				}
			}
		}

		//duplicate a list of particle systems. Useful for when bounces is higher then 0
		void CreateNewPSList (PSList.PSLIST_TYPE psListType){
			string[] psListTypeNames = System.Enum.GetNames (typeof(PSList.PSLIST_TYPE));
			for (int i = 0; i < psListTypeNames.Length; i++) {
				if (psListTypeNames[i] == psListType.ToString()) {
					for(int j = 0; j < psList.Count; j++){
						if(psList[j]._psType == psListType){
							GameObject newParent = Instantiate (psList[j]._parent) as GameObject;
							newParent.transform.SetParent (psList[j]._parent.transform.parent);					
							PSList newPSList = new PSList () {_parent = newParent };
							newPSList._psType = psListType;
							newPSList.AddPSToList (newPSList._parent, true);
							newPSList.EnablePS ();
							psList.Add (newPSList);
							if (newParent.transform.Find ("Collider") == true) {
								var boxCo = newParent.transform.Find ("Collider").GetComponent<BoxCollider> ();
								collidersList.Add (boxCo);
							}
							return;
						}
					}
				}
			}
		}

		List<PSList> GetByType (PSList.PSLIST_TYPE type){
			List<PSList> types = new List<PSList> ();
			for (int i = 0; i < psList.Count; i++) {
				if (psList [i]._psType == type) {
					types.Add (psList [i]);
				}
			}
			return types;
		}

		public void PSListToOriginalSize (){
			for (int i = 3; i < psList.Count; i++) {
				psList [i].DisablePS (false);
				GameObject newObj = psList[i]._parent;
				if (psList [i]._psType == PSList.PSLIST_TYPE.end) {
					if (newObj.transform.Find ("Collider") == true) 
						RemoveLastCollider (false);
				}
				psList.RemoveAt (i);
				Destroy (newObj);
			}
		}

		public void RemoveLastByType (PSList.PSLIST_TYPE type){
			for (int i = psList.Count-1; i > 0; i--) {
				if (psList [i]._psType == type) {
					psList [i].DisablePS (false);
					GameObject newObj = psList[i]._parent;
					psList.RemoveAt (i);
					if (type == PSList.PSLIST_TYPE.end) {
						if (newObj.transform.Find ("Collider") == true) 
							RemoveLastCollider (false);
					}
					Destroy (newObj);
					return;
				}
			}			
		}

		public void RemoveLastPositionLRs (){
			for (int i = 0; i < lineRenderers.Count; i++) {
				lineRenderers [i].positionCount--;
			}
		}

		public void StopLastTrail (){
			if (trailVFX != null && useTrail) {
				for(int i = trails.Count-1; i > 0; i--){
					trails [i]._emittingTrail = false;
					trails [i]._ps.Stop ();
					return;
				}
			}
		}

		public void Resize (bool live){

			if (psCtrl == null) {
				gameObject.AddComponent <ParticleSystemController> ();
				psCtrl = GetComponent<ParticleSystemController>();
			}

			if(live){
				if(lineRenderers.Count > 0){
					for (int i = 0; i < lineRenderers.Count; i++) {
						lrWidth[i] *= size;
						lrWidth [i] = (float)System.Math.Round (lrWidth [i], 2);
					}
				}
				overgrow *= size;
				overgrow = (float)System.Math.Round (overgrow, 2);
			}

			size = (float)System.Math.Round (size,2);
			psCtrl.size = size;
			psCtrl.ResizeOnly ();
		}

		public bool GetCollisionStatus (){
			return isColliding;
		}

		public List<BoxCollider> GetCollidersList (){
			return collidersList;
		}

		public List<Vector3> GetCollidersPosition (bool localPosition){
			List<Vector3> collidersPos = new List<Vector3> ();

			for(int i = 0; i<collidersList.Count; i++){
				if (localPosition)
					collidersPos.Add (collidersList [i].gameObject.transform.localPosition);
				else
					collidersPos.Add (collidersList [i].gameObject.transform.position);
			}

			return collidersPos;
		}

		void GenerateCollider (GameObject obj){
			if (obj.transform.Find ("Collider") == null) {
				var colliderObj = new GameObject ("Collider");
				colliderObj.transform.SetParent (obj.transform);
				colliderObj.transform.localPosition = Vector3.zero;
				colliderObj.layer = 2; //ignore raycast layer
				var boxCo = colliderObj.gameObject.AddComponent<BoxCollider> ();
				boxCo.isTrigger = isColliderTrigger;
				boxCo.size = new Vector3 (collidersRadius,collidersRadius,collidersRadius);
				collidersList.Add (boxCo);
			}
		}

		void RemoveLastCollider ( bool removeObject){
			for (int i = collidersList.Count-1; i > 0; i--) {
				if (removeObject) {
					GameObject newObj = collidersList [i].transform.parent.gameObject;
					Destroy (newObj);
				}
				collidersList.RemoveAt (i);
				return;
			}	
		}

		void ChangeLayerRecursively (GameObject obj) {
			foreach (Transform t in obj.transform){
				t.gameObject.layer = LayerMask.NameToLayer (layerName);
				if (t.childCount > 0)
					ChangeLayerRecursively (t.gameObject);
			}
		}
	}
}