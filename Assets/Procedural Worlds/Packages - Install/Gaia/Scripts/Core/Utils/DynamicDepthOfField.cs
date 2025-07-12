using UnityEngine;
#if HDPipeline || UPPipeline
using UnityEngine.Rendering;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
#endif
#if UPPipeline
using UnityEngine.Rendering.Universal;
#endif
#endif
#if !HDPipeline && !UPPipeline
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
#endif
[System.Serializable]
public class DepthOfFieldParameters
{
#if HDPipeline
    [Header("HDRP")]
    public float minDistance = 1f;
    public float maxDistance = 15f;
    public float nearFocusSpeed = 10000f;
    public float farFocusSpeed = 10000f;
    public float minNearRangeStart = 0f;
    public float maxNearRangeStart = 0f;
    public float minNearRangeEnd = 1f;
    public float maxNearRangeEnd = 2f;
    public float minFarRangeStart = 25f;
    public float maxFarRangeStart = 1000f;
    public float minFarRangeEnd = 100f;
    public float maxFarRangeEnd = 2000f;
#endif
#if UPPipeline
    [Header("URP")]
    [Header("Bokeh Mode Settings")]
    public float minDistance = 1f;
    public float maxDistance = 1024f;
    public int FocalLength = 70;
    public float aperture = 8f;
    public int bladeCount = 5;
    public int bladeCurvature = 1;
    public int bladeRotation = 0;
#endif
#if !HDPipeline && !UPPipeline
    [Header("SRP Settings")]
    public float minDistance = 0f;
    public float maxDistance = 10f;
    public float aperture = 0.6f;
    public float focalLength = 15f;
#endif
}

public class DynamicDepthOfField : MonoBehaviour
{
    public LayerMask raycastLayerMask = -1;

    [Header("Depth of Field Parameters")]
    public DepthOfFieldParameters parameters;

    // Default values
    private DepthOfFieldParameters defaultParameters;

    private float currentNearRangeStart;
    private float currentNearRangeEnd;
    private float currentFarRangeStart;
    private float currentFarRangeEnd;
    private float targetNearRangeStart;
    private float targetNearRangeEnd;
    private float targetFarRangeStart;
    private float targetFarRangeEnd;

    public bool showGizmo = false;
    public Color gizmoColor = Color.red;

    public Camera mainCamera;
    private Vector3 hitPoint;

#if HDPipeline || UPPipeline
    private Volume volumeComponent;
    private DepthOfField depthOfField;
#endif
#if !HDPipeline && !UPPipeline
#if UNITY_POST_PROCESSING_STACK_V2
    private PostProcessVolume PostProcessVolumeComponent;
    private DepthOfField depthOfField;
#endif
#endif
    private void Start()
    {
        // Check if a camera is assigned
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
            // If still no camera, throw an error and skip the rest of the code
            if (mainCamera == null)
            {
                Debug.LogError("No camera assigned or found!, Please assign the Camera in the PostProcessing object in the Dynamic Depth of Field script");
                return;
            }
        }

#if HDPipeline || UPPipeline
#if GAIA_2023
        GameObject gaiaLighting = Gaia.GaiaUtils.GetLightingObject();
        Volume[] Volumes = gaiaLighting.GetComponentsInChildren<Volume>();
        foreach (Volume volume in Volumes)
        {
            if (volume.name.Contains("PostProcessing") || volume.name.Contains("Post Processing"))
            {
                volumeComponent = volume;
            }
        }

        if (volumeComponent == null)
        {
            Debug.LogWarning("No PostProcess Volumes");
        }

        if (!volumeComponent.profile.TryGet(out depthOfField))
        {
            depthOfField = volumeComponent.profile.Add<DepthOfField>();
        }
#endif
#endif
#if !HDPipeline && !UPPipeline
#if !UNITY_POST_PROCESSING_STACK_V2
        Debug.LogError("Please Install the PostProcessing Pack to use the DDOF");
#endif
#if UNITY_POST_PROCESSING_STACK_V2
        GameObject gaiaLighting = Gaia.GaiaUtils.GetLightingObject();
        PostProcessVolume[] Volumes = gaiaLighting.GetComponentsInChildren<PostProcessVolume>();
        foreach (PostProcessVolume volume in Volumes)
        {
            if (volume.name.Contains("PostProcessing") || volume.name.Contains("Post Processing"))
            {
                PostProcessVolumeComponent = volume;
            }
        }

        if (PostProcessVolumeComponent == null)
        {
            Debug.LogWarning("No PostProcess Volumes");
        }

        if (!PostProcessVolumeComponent.profile.TryGetSettings(out depthOfField))
        {
            depthOfField = PostProcessVolumeComponent.profile.AddSettings<DepthOfField>();
        }
#endif
#endif
        // Save default values
        defaultParameters = new DepthOfFieldParameters();
#if HDPipeline
        defaultParameters.minDistance = parameters.minDistance;
        defaultParameters.maxDistance = parameters.maxDistance;
        defaultParameters.nearFocusSpeed = parameters.nearFocusSpeed;
        defaultParameters.farFocusSpeed = parameters.farFocusSpeed;
        defaultParameters.minFarRangeStart = parameters.minFarRangeStart;
        defaultParameters.maxFarRangeStart = parameters.maxFarRangeStart;
        defaultParameters.minFarRangeEnd = parameters.minFarRangeEnd;
        defaultParameters.maxFarRangeEnd = parameters.maxFarRangeEnd;
        defaultParameters.minNearRangeStart = parameters.minNearRangeStart;
        defaultParameters.maxNearRangeStart = parameters.maxNearRangeStart;
        defaultParameters.minNearRangeEnd = parameters.minNearRangeEnd;
        defaultParameters.maxNearRangeEnd = parameters.maxNearRangeEnd;
#endif
#if UPPipeline
        defaultParameters.minDistance = parameters.minDistance;
        defaultParameters.maxDistance = parameters.maxDistance;
        defaultParameters.FocalLength = parameters.FocalLength;
        defaultParameters.aperture = parameters.aperture;
        defaultParameters.bladeCurvature = parameters.bladeCurvature;
        defaultParameters.bladeCount = parameters.bladeCount;
        defaultParameters.bladeRotation = parameters.bladeRotation;
#endif
#if !HDPipeline && !UPPipeline
        defaultParameters.minDistance = parameters.minDistance;
        defaultParameters.maxDistance = parameters.maxDistance;
        defaultParameters.aperture = parameters.aperture;
        defaultParameters.focalLength = parameters.focalLength;
#endif
        CheckAndSetupDepthOfField();
    }

    private void Update()
    {
        UpdateDepthOfFieldFocus();
    }

    private void CheckAndSetupDepthOfField()
    {
#if HDPipeline
        // Enable Depth of Field
        depthOfField.active = true;

        // Enable other necessary Depth of Field settings
        depthOfField.focusMode.overrideState = true;
        depthOfField.focusMode.value = DepthOfFieldMode.Manual;

        // Adjust the near and far manual ranges
        depthOfField.nearFocusStart.overrideState = true;
        depthOfField.farFocusStart.overrideState = true;
        depthOfField.nearFocusEnd.overrideState = true;
        depthOfField.farFocusEnd.overrideState = true;
#endif
#if UPPipeline
        depthOfField.active = true;
        depthOfField.mode.overrideState = true;
        depthOfField.mode.value = DepthOfFieldMode.Bokeh;
        depthOfField.focusDistance.overrideState = true;
        depthOfField.focalLength.overrideState = true;
        depthOfField.aperture.overrideState = true;
        depthOfField.bladeCount.overrideState = true;
        depthOfField.bladeCurvature.overrideState = true;
        depthOfField.bladeRotation.overrideState = true;
#endif
#if !HDPipeline && !UPPipeline
#if UNITY_POST_PROCESSING_STACK_V2
        // Enable Depth of Field
        depthOfField.active = true;

        // Enable other necessary Depth of Field settings
        depthOfField.focusDistance.overrideState = true;
        depthOfField.aperture.overrideState = true;
        depthOfField.focalLength.overrideState = true;
#endif
#endif
    }

    private void UpdateDepthOfFieldFocus()
    {
        if (mainCamera == null)
        {
            return;
        }
#if HDPipeline || UPPipeline || UNITY_POST_PROCESSING_STACK_V2
        float normalizedRangeDistance = 1f;
        Ray centerRay = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit centerHit;
#endif

        // Start the raycast
#if !HDPipeline && !UPPipeline
#if UNITY_POST_PROCESSING_STACK_V2
        if (Physics.Raycast(centerRay, out centerHit, parameters.maxDistance, raycastLayerMask))
        {
            hitPoint = centerHit.point;

            normalizedRangeDistance = Mathf.InverseLerp(parameters.minDistance, parameters.maxDistance, centerHit.distance);

            float focusDistance = Vector3.Distance(mainCamera.transform.position, hitPoint);
            depthOfField.focusDistance.value = focusDistance;
            depthOfField.aperture.value = parameters.aperture;
            depthOfField.focalLength.value = parameters.focalLength;


        }
        else
        {
            depthOfField.focusDistance.value = 100f;
            depthOfField.aperture.value = parameters.aperture;
            depthOfField.focalLength.value = parameters.focalLength;


        }
#endif
#endif
#if UPPipeline
            if (Physics.Raycast(centerRay, out centerHit, parameters.maxDistance, raycastLayerMask))
            {
                hitPoint = centerHit.point;

                normalizedRangeDistance = Mathf.InverseLerp(parameters.minDistance, parameters.maxDistance, centerHit.distance);

                float focusDistance = Vector3.Distance(mainCamera.transform.position, hitPoint);
                if (depthOfField.mode.value != DepthOfFieldMode.Bokeh)
                {
                    depthOfField.mode.value = DepthOfFieldMode.Bokeh;
                }

                if (focusDistance < parameters.minDistance)
                {
                  focusDistance = parameters.minDistance;
                }
                else if (focusDistance > parameters.maxDistance) 
                { 
                  focusDistance = parameters.maxDistance;
                }
                depthOfField.focusDistance.value = focusDistance;
                depthOfField.aperture.value = parameters.aperture;
                depthOfField.focalLength.value = parameters.FocalLength;
                depthOfField.bladeCount.value = parameters.bladeCount;
                depthOfField.bladeCurvature.value = parameters.bladeCurvature;
                depthOfField.bladeRotation.value = parameters.bladeRotation;
            }
            else
            {
                depthOfField.focusDistance.value = 100f;
                depthOfField.aperture.value = parameters.aperture;
                depthOfField.focalLength.value = parameters.FocalLength;
                depthOfField.bladeCount.value = parameters.bladeCount;
                depthOfField.bladeCurvature.value = parameters.bladeCurvature;
                depthOfField.bladeRotation.value = parameters.bladeRotation;
            }
#endif
#if HDPipeline

        if (Physics.Raycast(centerRay, out centerHit, parameters.maxDistance, raycastLayerMask))
        {
            hitPoint = centerHit.point;

            normalizedRangeDistance = Mathf.InverseLerp(parameters.minDistance, parameters.maxDistance, centerHit.distance);
        }

        targetNearRangeStart = Mathf.Lerp(parameters.minNearRangeStart, parameters.maxNearRangeStart, normalizedRangeDistance);
        targetNearRangeEnd = Mathf.Lerp(parameters.minNearRangeEnd, parameters.maxNearRangeEnd, normalizedRangeDistance);
        targetFarRangeStart = Mathf.Lerp(parameters.minFarRangeStart, parameters.maxFarRangeStart, normalizedRangeDistance);
        targetFarRangeEnd = Mathf.Lerp(parameters.minFarRangeEnd, parameters.maxFarRangeEnd, normalizedRangeDistance);

        currentNearRangeStart = Mathf.MoveTowards(currentNearRangeStart, targetNearRangeStart, parameters.nearFocusSpeed * Time.deltaTime);
        currentNearRangeEnd = Mathf.MoveTowards(currentNearRangeEnd, targetNearRangeEnd, parameters.nearFocusSpeed * Time.deltaTime);
        currentFarRangeStart = Mathf.MoveTowards(currentFarRangeStart, targetFarRangeStart, parameters.farFocusSpeed * Time.deltaTime);
        currentFarRangeEnd = Mathf.MoveTowards(currentFarRangeEnd, targetFarRangeEnd, parameters.farFocusSpeed * Time.deltaTime);

        // Apply the calculated focus distances
        depthOfField.nearFocusStart.value = currentNearRangeStart;
        depthOfField.nearFocusEnd.value = currentNearRangeEnd;
        depthOfField.farFocusStart.value = currentFarRangeStart;
        depthOfField.farFocusEnd.value = currentFarRangeEnd;

        //switch off depth of field when reaching the target Far range end - otherwise the sky will stay blurry
        if (currentFarRangeEnd == parameters.maxFarRangeEnd)
        {
            depthOfField.active = false;
        }
        else
        {
            depthOfField.active = true;
        }
#endif
    }




    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(hitPoint, 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (showGizmo)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(hitPoint, 0.2f);
        }
    }


    // Button function to restore default values
    [ContextMenu("Restore Default Values")]
    private void RestoreDefaultValues()
    {
        parameters = defaultParameters;
    }

}
