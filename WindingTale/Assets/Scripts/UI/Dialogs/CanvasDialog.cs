using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CanvasDialog : MonoBehaviour
{
    protected GameObject rootOnCanvas = null;

    public virtual void Initialize(GameObject root)
    {
        this.rootOnCanvas = root;
    }

    protected void AddControl(string resourceName, Vector3 position, Vector3 scale)
    {
        GameObject detail = Resources.Load<GameObject>(resourceName);
        GameObject detailIns = GameObject.Instantiate(detail);
        detailIns.transform.parent = rootOnCanvas.transform;
        detailIns.layer = 5; //UI
        detailIns.transform.Find(@"default").gameObject.layer = 5;
        detailIns.transform.localPosition = position;
        detailIns.transform.localScale = scale;
        detailIns.transform.localRotation = Quaternion.Euler(90, 0, 180);

    }
}
