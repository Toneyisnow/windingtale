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
