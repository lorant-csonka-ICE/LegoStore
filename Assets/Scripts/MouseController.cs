using System.Linq;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    public GameObject CurrentSphere;

    [Header("Mouse Movement")]
    public GameObject TeleportMarker;
    public Vector2 MoveUnit;
    public float MoveSpeed;
    public float ShortClickDuration = 0.02f;

    [Header("Scrolling Behaviour")]
    public float FOVSensitivity = 50;
    public float MinFOV = 20;
    public float MaxFOV = 100;

    [Header("Teleport Zoom Behaviour")]
    public float MaxFOVZoom = 30;
    public float ZoomSpeed = 200;

    private GameObject teleportZone;
    private Camera currentCamera;

    private bool shouldTeleport;
    private bool isTeleportZone;
    private bool clicking;

    private float totalMouseBtnDownTime;
    private float currentFoV = 60.0f;
    private float inputX;
    private float inputY;

    void Start()
    {
        currentCamera = gameObject.GetComponent<Camera>();
        currentFoV = currentCamera.fieldOfView;
    }

    void Update()
    {
        if (shouldTeleport)
        {
            currentCamera.fieldOfView = Mathf.MoveTowards(currentCamera.fieldOfView, MaxFOVZoom, ZoomSpeed * Time.deltaTime);

            if (Mathf.Abs(currentCamera.fieldOfView - MaxFOVZoom) <= float.Epsilon)
            {
                currentCamera.fieldOfView = 60;

                Teleport(teleportZone);

                shouldTeleport = false;
            }
        }
        else
        {
            var raycastHits = RayCastMouse();

            UpdateScrollFOV();

            teleportZone = GetTeleportZone(raycastHits);
            isTeleportZone = teleportZone != null;

            Aim(raycastHits, isTeleportZone);

            var isShortClicked = MouseClickHandling();

            if (isShortClicked)
            {
                if (isTeleportZone && teleportZone != null)
                {
                    shouldTeleport = true;
                }
            }
        }
    }

    /// <summary>
    /// Changes Camera FOV if mouse is scrolling (zoom effect)
    /// </summary>
    private void UpdateScrollFOV()
    {
        currentFoV -= Input.GetAxis("Mouse ScrollWheel") * FOVSensitivity;
        currentFoV = Mathf.Clamp(currentFoV, MinFOV, MaxFOV);
        currentCamera.fieldOfView = currentFoV;
    }

    /// <summary>
    /// Raycast (all colliders) by mouse postion
    /// </summary>
    /// <returns>RaycastHit array</returns>
    private RaycastHit[] RayCastMouse()
    {
        var mousePos = Input.mousePosition;
        mousePos.z = 100;
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);

        return Physics.RaycastAll(transform.position, mousePos - transform.position);
    }

    /// <summary>
    /// Teleports to the targetZone's TargetSphere
    /// </summary>
    /// <param name="targetZone">targetZone</param>
    private void Teleport(GameObject targetZone)
    {
        var targetSphere = targetZone.GetComponent<TeleportTarget>().TargetSphere;

        if (targetSphere == null)
        {
            Debug.LogWarning("Teleport TartgetSphete is null!");
        }

        currentCamera.transform.position = targetSphere.transform.position;

        CurrentSphere.SetActive(false);

        targetSphere.SetActive(true);

        CurrentSphere = targetSphere;
    }

    /// <summary>
    /// Get the TeleportZone GameObject
    /// </summary>
    /// <param name="raycastHits">raycastHits</param>
    /// <returns></returns>
    private GameObject GetTeleportZone(RaycastHit[] raycastHits)
    {
        foreach (var hitted in raycastHits)
        {
            if (hitted.transform.tag == "FanCylinderCollider")
            {
                return hitted.transform.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// This positions the TeleportMarker
    /// If a teleport area is not hit by the ray, scale will be 0.1f 
    /// </summary>
    /// <param name="raycastHits">raycastHits</param>
    /// <param name="isTeleportPosition">isTeleportPosition</param>
    private void Aim(RaycastHit[] raycastHits, bool isTeleportPosition = false)
    {
        if (raycastHits.Any(r => r.transform.tag == "NavigationSurface"))
        {
            var hit = raycastHits.FirstOrDefault(r => r.transform.tag == "NavigationSurface");

            TeleportMarker.transform.position = new Vector3(hit.point.x, TeleportMarker.transform.position.y, hit.point.z);

            if (isTeleportPosition)
            {
                TeleportMarker.transform.localScale = Vector3.one;
            }
            else
            {
                TeleportMarker.transform.localScale = Vector3.one * 0.1f;
            }
        }
    }

    /// <summary>
    /// Rotates the camera in the sphere
    /// </summary>
    private void MouseDrag()
    {
        inputX += Input.GetAxis("Mouse X") * MoveUnit.x;
        inputY += -Input.GetAxis("Mouse Y") * MoveUnit.y;

        currentCamera.transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(-inputY, -inputX, 0), Time.deltaTime * MoveSpeed);
    }

    /// <summary>
    /// Handle short and long clicks
    /// </summary>
    /// <returns>isShortClicked</returns>
    private bool MouseClickHandling()
    {
        if (Input.GetMouseButtonDown(0))
        {
            totalMouseBtnDownTime = 0;

            clicking = true;
        }

        if (Input.GetMouseButton(0))
        {
            MouseDrag();

            totalMouseBtnDownTime += Time.deltaTime;

            if (totalMouseBtnDownTime > 0.15f)
            {
                clicking = false;
            }
        }
        if (clicking && Input.GetMouseButtonUp(0))
        {
            clicking = false;

            return true;
        }

        return false;
    }
}