using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnemyHitEffect : MonoBehaviour
{
    [Header("���Ч������")]
    [Tooltip("�ܻ�ʱ���ŵ�����Ч��")]
    public ParticleSystem hitParticle;

    [Tooltip("�ܻ���˸����ɫ")]
    public Color hitColor = Color.red;

    [Tooltip("��˸����ʱ��")]
    public float flashDuration = 0.16f;

    [Header("����Ч������")]
    [Tooltip("���˵�����")]
    public float knockbackForce = 50f;

    [Tooltip("���˻ָ�ʱ��")]
    public float knockbackRecoveryTime = 1.0f;

    [Header("��Ļ��Ч")]
    [Tooltip("��Ļ�ܻ����ͼ��")]
    public Image hitEffectImage;
    [Tooltip("��Ļ������ʱ��")]
    public float screenFlashDuration = 0.2f;

    // �ڲ�����������ԭʼ��ɫ
    private Color originalColor;
    private Renderer enemyRenderer;
    private bool isFlashing = false;
    private Vector3 originalPosition;

    private void Start()
    {
        // ��ȡ Renderer ����¼ԭʼ��ɫ
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        originalPosition = transform.position;
    }

    /// <summary>
    /// �ⲿ���õķ������������ܵ�����ʱ���ô˷���
    /// </summary>
    /// <param name="hitDirection">�����ķ������ڻ���Ч����</param>
    public void OnHit(Vector3 hitDirection)
    {
        if (hitParticle != null)
        {
            hitParticle.transform.position = transform.position;
            hitParticle.Play();
        }

        Vector3 knockbackPosition = transform.position + hitDirection.normalized * knockbackForce;
        transform.position = knockbackPosition;

        StartCoroutine(MoveBackToOriginalPosition());

        if (!isFlashing)
        {
            StartCoroutine(FlashEffect());
        }

        if (hitEffectImage != null)
        {
            StartCoroutine(ScreenFlashEffect());
        }
    }

    /// <summary>
    /// 
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
    /// Э�̣��õ�����1���ڻص�ԭλ
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
    /// Э�̣���Ļ�ܱ��������Ч��
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
