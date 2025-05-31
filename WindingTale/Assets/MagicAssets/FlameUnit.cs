using UnityEngine;

public class FlameUnit : MonoBehaviour
{
    public Sprite[] flameFrames;         // ����֡ͼƬ
    public float frameRate = 0.1f;       // ֡�л��ٶ�
    public float floatSpeed = 0.5f;      // �����ٶ�
    public float lifetime = 0.6f;        // �ܴ���ʱ��

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
        // ����֡����
        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            frameIndex = (frameIndex + 1) % flameFrames.Length;
            spriteRenderer.sprite = flameFrames[frameIndex];
        }

        // �����ƶ�
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // �Զ�����
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
