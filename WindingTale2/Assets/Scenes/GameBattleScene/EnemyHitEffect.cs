using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnemyHitEffect : MonoBehaviour
{
    [Header("打击效果设置")]
    [Tooltip("受击时播放的粒子效果")]
    public ParticleSystem hitParticle;

    [Tooltip("受击闪烁的颜色")]
    public Color hitColor = Color.red;

    [Tooltip("闪烁持续时间")]
    public float flashDuration = 0.16f;

    [Header("击退效果设置")]
    [Tooltip("击退的力度")]
    public float knockbackForce = 50f;

    [Tooltip("击退恢复时间")]
    public float knockbackRecoveryTime = 1.0f;

    [Header("屏幕特效")]
    [Tooltip("屏幕受击红光图像")]
    public Image hitEffectImage;
    [Tooltip("屏幕红光持续时间")]
    public float screenFlashDuration = 0.2f;

    // 内部变量：保存原始颜色
    private Color originalColor;
    private Renderer enemyRenderer;
    private bool isFlashing = false;
    private Vector3 originalPosition;

    private void Start()
    {
        // 获取 Renderer 并记录原始颜色
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        originalPosition = transform.position;
    }

    /// <summary>
    /// 外部调用的方法：当敌人受到攻击时调用此方法
    /// </summary>
    /// <param name="hitDirection">攻击的方向（用于击退效果）</param>
    public void OnHit(Vector3 hitDirection)
    {
        // 播放受击粒子效果
        if (hitParticle != null)
        {
            // 可根据需要调整粒子位置
            hitParticle.transform.position = transform.position;
            hitParticle.Play();
        }

        // 立即施加击退效果
        Vector3 knockbackPosition = transform.position + hitDirection.normalized * knockbackForce;
        transform.position = knockbackPosition;

        // 触发击退恢复
        StartCoroutine(MoveBackToOriginalPosition());

        // 触发受击闪烁效果
        if (!isFlashing)
        {
            StartCoroutine(FlashEffect());
        }

        // 触发屏幕红光效果
        if (hitEffectImage != null)
        {
            StartCoroutine(ScreenFlashEffect());
        }
    }

    /// <summary>
    /// 协程：短暂改变材质颜色，营造打击感
    /// </summary>
    private IEnumerator FlashEffect()
    {
        isFlashing = true;
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = hitColor;
        }
        yield return new WaitForSeconds(flashDuration);
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = originalColor;
        }
        isFlashing = false;
    }

    /// <summary>
    /// 协程：让敌人在1秒内回到原位
    /// </summary>
    private IEnumerator MoveBackToOriginalPosition()
    {
        float elapsedTime = 0;
        Vector3 startPosition = transform.position;
        while (elapsedTime < knockbackRecoveryTime)
        {
            transform.position = Vector3.Lerp(startPosition, originalPosition, elapsedTime / knockbackRecoveryTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
    }

    /// <summary>
    /// 协程：屏幕周边闪动红光效果
    /// </summary>
    private IEnumerator ScreenFlashEffect()
    {
        Color tempColor = hitEffectImage.color;
        tempColor.a = 1f;
        hitEffectImage.color = tempColor;
        yield return new WaitForSeconds(screenFlashDuration);
        tempColor.a = 0f;
        hitEffectImage.color = tempColor;
    }
}
