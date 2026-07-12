using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MainCamera : MonoBehaviour
{
    public float moveSpeed = 20f; // �����ƶ��ٶ�
    public float acceleration = 5f; // ���ٶ�
    public float deceleration = 5f; // ���ٶ�
    public float minHeight = 8f; // ��͸߶ȣ�ƽ���ڵ��棩
    public float maxHeight = 48f; // ��߸߶ȣ�45�Ƚǣ�
    public float rotationAngle = 45f; // ��ʼ���ӽǶ�
    public float edgeScrollSpeed = 10f; // ��Ե�����ٶ�
    public float edgeScrollThreshold = 50f; // ��Ե����������ֵ
    public float zoomSpeed = 0.5f; // �����ٶ�
    public float zoomDeceleration = 8f; // ���ż���ٶȣ�Խ������ͣ��

    private Vector3 velocity = Vector3.zero;
    private float zoomVelocity = 0f;

    // Right-drag orbit: hold the right mouse button and move left/right to rotate the
    // whole camera around the world point currently under the cursor.
    public float rotateSpeed = 4.0f;   // degrees per unit of horizontal mouse movement
    private bool isRotating = false;
    private Vector3 rotatePivot = Vector3.zero;
    private Camera cam;

    // ---- Conversation camera follow ----
    // Default framing of the cursor while following it during a conversation slide.
    public const float FollowPitchAngle = 45f;      // look-down angle, degrees
    public const float FollowDistanceTiles = 8f;    // camera-to-cursor distance, tiles
    private const float WorldUnitsPerTile = 2f;     // see MapCoordinate (scales by 2)

    // When the dialog sits at the top of the screen, slide the whole camera this many
    // tiles horizontally across the map (toward the top of the view). The Camera->Cursor
    // framing is unchanged; the camera just translates on the ground plane, so the cursor
    // lands lower on screen, clear of the top dialog.
    public const float FollowTopCameraUpTiles = 3f;
    private float cameraUpTiles = 0f;

    private const float EnterDuration = 0.4f;       // min ease-in from gameplay, seconds
    private bool followActive = false;              // camera is framing the cursor
    private bool isSliding = false;                 // currently interpolating the framing
    private Vector3 slideStartPosition = Vector3.zero;
    private Quaternion slideStartRotation = Quaternion.identity;
    private Vector3 slideTargetPosition = Vector3.zero;
    private Quaternion slideTargetRotation = Quaternion.identity;
    private float slideDuration = 0f;
    private float slideElapsed = 0f;

    private const float ReturnDuration = 0.4f;      // ease back to gameplay framing, seconds
    private bool isReturning = false;
    private float returnElapsed = 0f;
    // Camera transform captured just before the follow took over, restored on return.
    private Vector3 savedPosition = Vector3.zero;
    private Quaternion savedRotation = Quaternion.identity;
    private Vector3 returnStartPosition = Vector3.zero;
    private Quaternion returnStartRotation = Quaternion.identity;

    /// <summary>
    /// Smoothly moves the camera to frame the given ground point with the fixed
    /// 45deg / 8-tile framing, easing (accelerate / decelerate) from wherever the
    /// camera currently is. The first call of a sequence eases in from the gameplay
    /// camera (mirroring the ease-out on ReturnToGameplay); later calls track the
    /// cursor. The next manual camera input returns control to the player.
    /// </summary>
    // True while the follow camera is easing toward a framing (not while merely holding).
    public bool IsFollowSliding => isSliding;

    public void SlideFocusTo(Vector3 groundTarget, float duration, bool keepCursorLow)
    {
        // Remember the gameplay camera transform the first time follow takes over,
        // so it can be restored exactly when the conversation ends.
        bool firstEntry = !followActive;
        if (firstEntry)
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
        }

        cameraUpTiles = keepCursorLow ? FollowTopCameraUpTiles : 0f;

        float yaw = transform.rotation.eulerAngles.y;
        slideTargetRotation = Quaternion.Euler(FollowPitchAngle, yaw, 0f);
        slideTargetPosition = FramePositionFor(groundTarget, slideTargetRotation);

        slideStartPosition = transform.position;
        slideStartRotation = transform.rotation;

        // First entry eases in from the gameplay camera over at least EnterDuration;
        // later follow slides track the cursor's own timing.
        slideDuration = firstEntry ? Mathf.Max(duration, EnterDuration) : Mathf.Max(0.0001f, duration);
        slideElapsed = 0f;
        isSliding = true;
        isReturning = false;
        followActive = true;
    }

    private Vector3 FramePositionFor(Vector3 groundFocus, Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;

        // Keep the fixed 45deg / 8-tile Camera->Cursor framing, then slide the whole
        // camera horizontally across the map toward the top of the view (the camera's
        // ground-forward direction) -- NOT world-up, NOT the camera's local axes -- so
        // the cursor lands lower on screen, clear of a top dialog.
        Vector3 position = groundFocus - forward * (FollowDistanceTiles * WorldUnitsPerTile);

        Vector3 mapForward = new Vector3(forward.x, 0f, forward.z).normalized;
        position += mapForward * (cameraUpTiles * WorldUnitsPerTile);

        return position;
    }

    private void UpdateFollow()
    {
        if (!isSliding)
        {
            return; // hold the current framing between slides
        }

        slideElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(slideElapsed / slideDuration);
        float eased = Mathf.SmoothStep(0f, 1f, t); // ease in / ease out
        transform.position = Vector3.Lerp(slideStartPosition, slideTargetPosition, eased);
        transform.rotation = Quaternion.Slerp(slideStartRotation, slideTargetRotation, eased);

        if (t >= 1f)
        {
            isSliding = false;
        }
    }

    /// <summary>
    /// Smoothly hands the camera back to gameplay when a conversation ends: eases the
    /// camera from its follow framing back to the exact transform it had before the
    /// follow took over, so control returns without a snap. No-op if not following.
    /// </summary>
    public void ReturnToGameplay()
    {
        if (!followActive || isReturning)
        {
            return;
        }

        returnStartPosition = transform.position;
        returnStartRotation = transform.rotation;
        returnElapsed = 0f;
        isSliding = false;
        isReturning = true;
    }

    private void UpdateReturn()
    {
        returnElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(returnElapsed / ReturnDuration);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        transform.position = Vector3.Lerp(returnStartPosition, savedPosition, eased);
        transform.rotation = Quaternion.Slerp(returnStartRotation, savedRotation, eased);

        if (t >= 1f)
        {
            isReturning = false;
            followActive = false; // restored to the pre-conversation gameplay transform
        }
    }

    // private float lowestHeight = 24f;

    void Start()
    {
        // ���ó�ʼ�Ƕ�
        transform.rotation = Quaternion.Euler(rotationAngle, 180, 0);

        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    void Update()
    {
        // While following the cursor during a conversation, the camera drives itself.
        // Any manual camera input hands control back to the player.
        if (followActive)
        {
            bool manualInput = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)
                || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S)
                || Input.GetMouseButton(1) || Input.GetAxis("Mouse ScrollWheel") != 0f;
            if (!manualInput)
            {
                if (isReturning)
                {
                    UpdateReturn();
                }
                else
                {
                    UpdateFollow();
                }
                return;
            }
            // Player took over: drop all follow state and resume normal control.
            followActive = false;
            isSliding = false;
            isReturning = false;
        }

        Vector3 targetVelocity = Vector3.zero;
        float heightFactor = Mathf.InverseLerp(minHeight, maxHeight, transform.position.y);

        // ��ȡ���λ��
        Vector3 mousePos = Input.mousePosition;

        // ����ƽ�ƣ�����ˮƽ��
        if (Input.GetKey(KeyCode.A)) // || mousePos.x <= edgeScrollThreshold)
        {
            targetVelocity -= transform.right * moveSpeed;
        }
        if (Input.GetKey(KeyCode.D)) // || mousePos.x >= Screen.width - edgeScrollThreshold)
        {
            targetVelocity += transform.right * moveSpeed;
        }

        // �����˾�
        /*
        if (Input.GetKey(KeyCode.W)) // || mousePos.y >= Screen.height - edgeScrollThreshold)
        {
            targetVelocity += transform.forward * moveSpeed;
        }
        if (Input.GetKey(KeyCode.S)) // || mousePos.y <= edgeScrollThreshold)
        {
            targetVelocity -= transform.forward * moveSpeed;
        }
        */

        // �����˾���ƽ���ڵ��棩
        if (Input.GetKey(KeyCode.W))
        {
            targetVelocity += new Vector3(transform.forward.x, 0, transform.forward.z) * moveSpeed * (float)1.5;
        }
        if (Input.GetKey(KeyCode.S))
        {
            targetVelocity -= new Vector3(transform.forward.x, 0, transform.forward.z) * moveSpeed * (float)1.5;
        }


        // ƽ���˶�����
        velocity = Vector3.Lerp(velocity, targetVelocity, Time.deltaTime * (targetVelocity.magnitude > 0 ? acceleration : deceleration));
        transform.position += velocity * Time.deltaTime;

        // Right-drag orbit: pick the pivot under the cursor on press, then rotate the
        // camera around it (about world up) as the mouse moves left/right.
        if (Input.GetMouseButtonDown(1))
        {
            if (TryGetGroundPoint(Input.mousePosition, out Vector3 pivot))
            {
                rotatePivot = pivot;
                isRotating = true;
            }
        }
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }
        if (isRotating)
        {
            float dx = Input.GetAxis("Mouse X");
            if (Mathf.Abs(dx) > Mathf.Epsilon)
            {
                transform.RotateAround(rotatePivot, Vector3.up, dx * rotateSpeed);
            }
        }

        // ���㾵ͷ����
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            zoomVelocity = scroll * zoomSpeed;
        }
        else
        {
            zoomVelocity = Mathf.Lerp(zoomVelocity, 0, Time.deltaTime * zoomDeceleration);
        }
        float newHeight = Mathf.Clamp(transform.position.y - zoomVelocity, minHeight, maxHeight);
        transform.position = new Vector3(transform.position.x, newHeight, transform.position.z);

        // �����½Ƕ�
        float newAngle = Mathf.Lerp(0, rotationAngle, Mathf.InverseLerp(minHeight, maxHeight, newHeight));
        transform.rotation = Quaternion.Euler(newAngle, transform.rotation.eulerAngles.y, 0);
    }

    // Intersects the cursor ray with the ground plane (y = 0) to find the world point
    // to orbit around. Returns false if the ray doesn't hit the plane (e.g. aimed at
    // the sky), in which case no rotation pivot is set.
    private bool TryGetGroundPoint(Vector3 screenPos, out Vector3 point)
    {
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }
}
