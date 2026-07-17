using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MainCamera : MonoBehaviour
{
    public float moveSpeed = 20f; // �����ƶ��ٶ�
    public float acceleration = 12f; // ���ٶ� -- higher so the camera catches up with the cursor faster
    public float deceleration = 5f; // ���ٶ�
    public float minHeight = 8f; // ��͸߶ȣ�ƽ���ڵ��棩
    public float maxHeight = 48f; // ��߸߶ȣ�45�Ƚǣ�
    public float rotationAngle = 45f; // ��ʼ���ӽǶ�

    // Look-down angle at minHeight (fully zoomed in). Must stay clear of 0: a level camera
    // is aimed at the horizon, so the ground point it frames sits height / tan(pitch) away
    // -- which runs off to infinity as the pitch flattens. Zooming in then reads as the
    // camera retreating from the character instead of closing in on it.
    public float minRotationAngle = 25f;
    public float edgeScrollSpeed = 10f; // ��Ե�����ٶ�
    public float edgeScrollThreshold = 50f; // ��Ե����������ֵ
    public float zoomSpeed = 0.5f; // �����ٶ�
    public float zoomDeceleration = 8f; // ���ż���ٶȣ�Խ������ͣ��

    private Vector3 velocity = Vector3.zero;
    private float zoomVelocity = 0f;

    // Right-drag orbit: hold the right mouse button and move left/right to rotate the
    // whole camera around the world point currently under the cursor.
    public float rotateSpeed = 4.0f;   // degrees per unit of horizontal mouse movement

    // Keyboard camera controls: A/D orbit the view left/right (like the right-drag),
    // W/S zoom in/out (like the mouse wheel). J/I/K/L do the old A/W/S/D panning.
    public float keyboardRotateSpeed = 60f;   // degrees per second while A/D held
    public float keyboardZoomSpeed = 0.15f;   // wheel-equivalent zoom per frame while W/S held

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

    // ---- Field cursor edge follow (keyboard control) ----
    // While the player drives the field cursor by keyboard, the camera pans to keep it
    // on screen: it moves whenever the cursor sits inside the outer margin on any side,
    // and holds still otherwise.
    private const float CursorEdgeMargin = 0.40f;   // outer 40% of the screen on each side

    // The cursor outruns a moveSpeed pan: held arrow keys step it one tile every
    // PlayerInterface.RepeatInterval (2 world units per 0.08s = 25 units/s). The follow
    // pan therefore runs on its own, faster speed, and ramps up with how deep the cursor
    // has pushed into the margin so it eases instead of snapping on and off.
    public float cursorFollowSpeed = 60f;
    public float cursorFollowAcceleration = 30f;

    // Hard bound: the cursor is never allowed outside this margin. Whatever the pan
    // fails to keep up with (a slide, a zoom, a low frame) is corrected by translating
    // the camera the minimum amount that puts the cursor back on the bound.
    private const float CursorHardMargin = 0.12f;

    private Transform cursorFollowTarget = null;
    private bool cursorFollowActive = false;

    // ---- Menu framing ----
    // A menu covers part of the board, so the camera lifts to its highest, most top-down
    // framing while one is open. Only the height is driven: the pitch already follows it
    // (see the zoom section of Update), so maxHeight is by definition the steepest angle.
    private const float ZoomToTopDuration = 0.5f;
    private bool zoomToTopActive = false;
    private float zoomToTopStartHeight = 0f;
    private float zoomToTopElapsed = 0f;

    /// <summary>
    /// Eases the camera up to its highest, most top-down framing. Any manual zoom hands
    /// control straight back to the player.
    /// </summary>
    public void ZoomToTop()
    {
        zoomToTopStartHeight = transform.position.y;
        zoomToTopElapsed = 0f;
        zoomToTopActive = true;

        // The conversation follow drives the transform itself and returns out of Update
        // before the zoom runs, so it has to let go or the camera would never rise.
        followActive = false;
        isSliding = false;
        isReturning = false;
    }

    // Height the ease wants this frame, as a delta in the same sense as zoomVelocity
    // (positive zooms in, i.e. drops the camera).
    private float ZoomToTopVelocity()
    {
        zoomToTopElapsed += Time.deltaTime;

        float t = Mathf.Clamp01(zoomToTopElapsed / ZoomToTopDuration);
        float target = Mathf.Lerp(zoomToTopStartHeight, maxHeight, Mathf.SmoothStep(0f, 1f, t));

        if (t >= 1f)
        {
            zoomToTopActive = false;
        }

        return transform.position.y - target;
    }

    /// <summary>
    /// Starts (or refreshes) keyboard cursor follow around the given cursor transform.
    /// Any active conversation follow is released so gameplay follow can take over.
    /// </summary>
    public void BeginCursorFollow(Transform cursor)
    {
        cursorFollowTarget = cursor;
        cursorFollowActive = true;

        followActive = false;
        isSliding = false;
        isReturning = false;
    }

    // Returns the pan velocity that keeps the followed cursor out of the screen margins,
    // or zero when it is comfortably inside the safe zone. The speed scales with how far
    // into the margin the cursor sits: a gentle drift as it enters, full cursorFollowSpeed
    // once it reaches the screen edge -- which is faster than the cursor itself moves, so
    // the camera closes the gap instead of trailing further behind every step.
    private Vector3 ComputeCursorEdgeVelocity()
    {
        Vector3 screen = cam.WorldToScreenPoint(cursorFollowTarget.position);
        if (screen.z <= 0f)
        {
            return Vector3.zero; // cursor is behind the camera
        }

        float pushX = MarginPush(screen.x, Screen.width);
        float pushY = MarginPush(screen.y, Screen.height);

        Vector3 groundForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 direction = transform.right * pushX + groundForward * pushY;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        // Diagonals must not pan faster than a straight edge.
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        return direction * cursorFollowSpeed;
    }

    // How far the cursor has pushed into the margin on one screen axis: 0 inside the safe
    // zone, -1 / +1 at the low / high screen edge.
    private static float MarginPush(float screenPos, float screenSize)
    {
        float margin = screenSize * CursorEdgeMargin;

        if (screenPos < margin)
        {
            return -Mathf.Clamp01((margin - screenPos) / margin);
        }
        if (screenPos > screenSize - margin)
        {
            return Mathf.Clamp01((screenPos - (screenSize - margin)) / margin);
        }

        return 0f;
    }

    /// <summary>
    /// Translates the camera along the ground so the followed cursor stays inside the
    /// hard margin on every side. The pan alone can fall behind (the cursor steps a whole
    /// tile at a time and the camera accelerates into it), so this is the guarantee that
    /// it never leaves the screen -- most visibly on the left/right edges, where a held
    /// arrow key walks the cursor across the map faster than the camera builds up speed.
    /// </summary>
    private void ClampCursorOnScreen()
    {
        Vector3 cursorPosition = cursorFollowTarget.position;
        Vector3 screen = cam.WorldToScreenPoint(cursorPosition);
        if (screen.z <= 0f)
        {
            return; // cursor is behind the camera: nothing sensible to clamp against
        }

        float clampedX = Mathf.Clamp(screen.x, Screen.width * CursorHardMargin, Screen.width * (1f - CursorHardMargin));
        float clampedY = Mathf.Clamp(screen.y, Screen.height * CursorHardMargin, Screen.height * (1f - CursorHardMargin));
        if (Mathf.Approximately(clampedX, screen.x) && Mathf.Approximately(clampedY, screen.y))
        {
            return; // already in bounds
        }

        // Translating the camera by d shifts every world point's projection by d, so
        // landing the cursor on the clamped screen point means moving by the offset
        // between the cursor and whatever sits on that point now (taken at the cursor's
        // own height, so the offset stays on the ground plane).
        if (!TryGetPointAtHeight(new Vector3(clampedX, clampedY, 0f), cursorPosition.y, out Vector3 pointAtBound))
        {
            return;
        }

        Vector3 delta = cursorPosition - pointAtBound;
        transform.position += new Vector3(delta.x, 0f, delta.z);
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
                || Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.I)
                || Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.L)
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

        // Camera panning moved from A/W/S/D to J/I/K/L (same layout: I=forward,
        // K=back, J=left, L=right). Horizontal strafe on J/L (screen-horizontal).
        if (Input.GetKey(KeyCode.J))
        {
            targetVelocity -= transform.right * moveSpeed;
        }
        if (Input.GetKey(KeyCode.L))
        {
            targetVelocity += transform.right * moveSpeed;
        }

        // Forward / back on I/K, along the ground plane.
        if (Input.GetKey(KeyCode.I))
        {
            targetVelocity += new Vector3(transform.forward.x, 0, transform.forward.z) * moveSpeed * (float)1.5;
        }
        if (Input.GetKey(KeyCode.K))
        {
            targetVelocity -= new Vector3(transform.forward.x, 0, transform.forward.z) * moveSpeed * (float)1.5;
        }


        // No manual pan this frame: let the field cursor pull the camera along when it
        // reaches the screen margins (keyboard control).
        bool followingCursor = targetVelocity.sqrMagnitude < 0.0001f
            && cursorFollowActive && cursorFollowTarget != null && cam != null;
        if (followingCursor)
        {
            targetVelocity = ComputeCursorEdgeVelocity();
        }

        // ƽ���˶�����
        float accel = followingCursor ? cursorFollowAcceleration : acceleration;
        velocity = Vector3.Lerp(velocity, targetVelocity, Time.deltaTime * (targetVelocity.magnitude > 0 ? accel : deceleration));
        transform.position += velocity * Time.deltaTime;

        if (followingCursor)
        {
            ClampCursorOnScreen();
        }

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

        // A/D keyboard orbit: rotate the view left/right around the ground point at the
        // screen centre (same effect as the right-drag, but driven by keys).
        float keyRotate = 0f;
        if (Input.GetKey(KeyCode.A)) keyRotate += 1f;
        if (Input.GetKey(KeyCode.D)) keyRotate -= 1f;
        if (keyRotate != 0f)
        {
            Vector3 orbitPivot = GetKeyboardOrbitPivot();
            transform.RotateAround(orbitPivot, Vector3.up, keyRotate * keyboardRotateSpeed * Time.deltaTime);
        }

        // ���㾵ͷ���� -- mouse wheel, plus W/S as an equivalent zoom in / out.
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        float keyZoom = 0f;
        if (Input.GetKey(KeyCode.W)) keyZoom += 1f;
        if (Input.GetKey(KeyCode.S)) keyZoom -= 1f;

        if (scroll != 0)
        {
            zoomVelocity = scroll * zoomSpeed;
            zoomToTopActive = false; // player is zooming: abandon the menu framing
        }
        else if (keyZoom != 0f)
        {
            zoomVelocity = keyZoom * keyboardZoomSpeed;
            zoomToTopActive = false;
        }
        else if (zoomToTopActive)
        {
            zoomVelocity = ZoomToTopVelocity();
        }
        else
        {
            zoomVelocity = Mathf.Lerp(zoomVelocity, 0, Time.deltaTime * zoomDeceleration);
        }
        float oldHeight = transform.position.y;
        float newHeight = Mathf.Clamp(oldHeight - zoomVelocity, minHeight, maxHeight);

        // Zooming drops the camera and flattens its pitch, which on its own would drag the
        // framed ground point away from under the camera. Pin it instead: capture whatever
        // the camera is aimed at now, and put it back on the screen centre once the new
        // framing is applied. The camera then dollies straight in toward that point --
        // closer with every step, and standing still once the height clamps at minHeight.
        Vector3 zoomAnchor = Vector3.zero;
        bool hasZoomAnchor = false;
        if (Mathf.Abs(newHeight - oldHeight) > 0.0001f)
        {
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            hasZoomAnchor = TryGetGroundPoint(screenCenter, out zoomAnchor);
        }

        transform.position = new Vector3(transform.position.x, newHeight, transform.position.z);

        // �����½Ƕ�
        float newAngle = Mathf.Lerp(minRotationAngle, rotationAngle, Mathf.InverseLerp(minHeight, maxHeight, newHeight));
        transform.rotation = Quaternion.Euler(newAngle, transform.rotation.eulerAngles.y, 0);

        if (hasZoomAnchor)
        {
            PinGroundPointToScreenCenter(zoomAnchor);
        }
    }

    /// <summary>
    /// Slides the camera along the ground so the given ground point sits back under the
    /// screen centre at the current height and pitch. The centre ray runs along the camera
    /// forward, so its ground hit lies exactly height / tan(pitch) ahead on the flattened
    /// forward -- no raycast needed to place it.
    /// </summary>
    private void PinGroundPointToScreenCenter(Vector3 groundAnchor)
    {
        Vector3 forward = transform.forward;
        Vector3 groundForward = new Vector3(forward.x, 0f, forward.z);

        float horizontal = groundForward.magnitude;
        float down = -forward.y;
        if (horizontal < 1e-4f || down < 0.01f)
        {
            return; // looking straight down, or level / upward: no usable centre ground hit
        }
        groundForward /= horizontal;

        float distance = transform.position.y * horizontal / down; // height / tan(pitch)
        Vector3 position = groundAnchor - groundForward * distance;
        transform.position = new Vector3(position.x, transform.position.y, position.z);
    }

    // Farthest the A/D orbit pivot may sit in front of the camera, in world units.
    // At the lowest zoom the view is almost horizontal, so the screen-centre ray hits
    // the ground far away (or not at all); without this cap the pivot races toward
    // infinity and a single A/D press flings the camera clean off the map.
    private const float MaxKeyboardOrbitPivotDistance = 24f;

    // Ground point (on y = 0) the A/D orbit rotates around, clamped to a sane distance
    // in front of the camera. The screen-centre ray runs along the camera's forward, so
    // its ground hit lies exactly along the flattened forward direction -- meaning this
    // clamp leaves the zoomed-in behaviour untouched and only reins in the near-horizontal
    // edge case.
    private Vector3 GetKeyboardOrbitPivot()
    {
        Vector3 groundForward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (groundForward.sqrMagnitude < 1e-4f)
        {
            groundForward = Vector3.forward; // camera looking straight down/up: pick any heading
        }
        groundForward.Normalize();

        Vector3 flatCamPos = new Vector3(transform.position.x, 0f, transform.position.z);

        float distance = MaxKeyboardOrbitPivotDistance;
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        if (TryGetGroundPoint(screenCenter, out Vector3 groundPoint))
        {
            Vector3 flatPivot = new Vector3(groundPoint.x, 0f, groundPoint.z);
            distance = Mathf.Min(Vector3.Distance(flatCamPos, flatPivot), MaxKeyboardOrbitPivotDistance);
        }

        return flatCamPos + groundForward * distance;
    }

    // Intersects the cursor ray with the ground plane (y = 0) to find the world point
    // to orbit around. Returns false if the ray doesn't hit the plane (e.g. aimed at
    // the sky), in which case no rotation pivot is set.
    private bool TryGetGroundPoint(Vector3 screenPos, out Vector3 point)
    {
        return TryGetPointAtHeight(screenPos, 0f, out point);
    }

    // Same, against a horizontal plane at an arbitrary height rather than the ground.
    private bool TryGetPointAtHeight(Vector3 screenPos, float planeHeight, out Vector3 point)
    {
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }
}
