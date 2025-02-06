using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MainCamera : MonoBehaviour
{
    public float moveSpeed = 10f; // 基础移动速度
    public float acceleration = 5f; // 加速度
    public float deceleration = 5f; // 减速度
    public float minHeight = 2f; // 最低高度（平行于地面）
    public float maxHeight = 20f; // 最高高度（45度角）
    public float rotationAngle = 45f; // 初始俯视角度
    public float edgeScrollSpeed = 10f; // 边缘滚动速度
    public float edgeScrollThreshold = 50f; // 边缘滚动触发阈值
    public float zoomSpeed = 1.4f; // 缩放速度
    public float zoomAcceleration = 2f; // 缩放加速度

    private Vector3 velocity = Vector3.zero;
    private float zoomVelocity = 0f;

    void Start()
    {
        // 设置初始角度
        transform.rotation = Quaternion.Euler(rotationAngle, 180, 0);
    }

    void Update()
    {
        Vector3 targetVelocity = Vector3.zero;
        float heightFactor = Mathf.InverseLerp(minHeight, maxHeight, transform.position.y);

        // 获取鼠标位置
        Vector3 mousePos = Input.mousePosition;

        // 左右平移（保持水平）
        if (Input.GetKey(KeyCode.A)) // || mousePos.x <= edgeScrollThreshold)
        {
            targetVelocity -= transform.right * moveSpeed;
        }
        if (Input.GetKey(KeyCode.D)) // || mousePos.x >= Screen.width - edgeScrollThreshold)
        {
            targetVelocity += transform.right * moveSpeed;
        }

        // 上下运镜
        if (Input.GetKey(KeyCode.W)) // || mousePos.y >= Screen.height - edgeScrollThreshold)
        {
            targetVelocity += transform.forward * moveSpeed;
        }
        if (Input.GetKey(KeyCode.S)) // || mousePos.y <= edgeScrollThreshold)
        {
            targetVelocity -= transform.forward * moveSpeed;
        }

        // 平滑运动计算
        velocity = Vector3.Lerp(velocity, targetVelocity, Time.deltaTime * (targetVelocity.magnitude > 0 ? acceleration : deceleration));
        transform.position += velocity * Time.deltaTime;

        // 计算镜头缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            zoomVelocity += scroll * zoomSpeed;
        }
        zoomVelocity = Mathf.Lerp(zoomVelocity, 0, Time.deltaTime * zoomAcceleration);
        float newHeight = Mathf.Clamp(transform.position.y - zoomVelocity, minHeight, maxHeight);
        transform.position = new Vector3(transform.position.x, newHeight, transform.position.z);

        // 计算新角度
        float newAngle = Mathf.Lerp(0, rotationAngle, Mathf.InverseLerp(minHeight, maxHeight, newHeight));
        transform.rotation = Quaternion.Euler(newAngle, transform.rotation.eulerAngles.y, 0);
    }
}
