using UnityEngine;

public class FireSpellLauncher : MonoBehaviour
{
    public GameObject flamePrefab;         // 预制体
    public Sprite[] flameFrames;           // 帧图
    public Transform target;               // 敌人位置
    public int flames = 20;
    public float interval = 0.4f;

    void Start()
    {
        // Load flameFrames from local image
        flameFrames = new Sprite[5];
        flameFrames[0] = Resources.Load<Sprite>("Magics/101/019-10");
        flameFrames[1] = Resources.Load<Sprite>("Magics/101/019-11");
        flameFrames[2] = Resources.Load<Sprite>("Magics/101/019-12");
        flameFrames[3] = Resources.Load<Sprite>("Magics/101/019-13");
        flameFrames[4] = Resources.Load<Sprite>("Magics/101/019-14");


        StartCoroutine(SpawnFlames());
    }

    System.Collections.IEnumerator SpawnFlames()
    {
        for (int i = 0; i < flames; i++)
        {
            Vector3 pos = target.position + new Vector3(Random.Range(-0.3f, 0.3f), 0, 0);
            GameObject flame = Instantiate(flamePrefab, pos, Quaternion.identity);
            flame.GetComponent<FlameUnit>().flameFrames = flameFrames;
            yield return new WaitForSeconds(interval);
        }
    }
}
