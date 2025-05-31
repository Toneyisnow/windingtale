using UnityEngine;

public class FlameUnit : MonoBehaviour
{
    public Sprite[] flameFrames;         // 传入帧图片
    public float frameRate = 0.1f;       // 帧切换速度
    public float floatSpeed = 0.5f;      // 上升速度
    public float lifetime = 0.6f;        // 总存在时间

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int frameIndex;
    private float lifeTimer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        frameIndex = 0;
        timer = 0f;
        lifeTimer = 0f;
    }

    void Update()
    {
        // 播放帧动画
        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            frameIndex = (frameIndex + 1) % flameFrames.Length;
            spriteRenderer.sprite = flameFrames[frameIndex];
        }

        // 向上移动
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // 自动销毁
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
