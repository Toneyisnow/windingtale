using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageDialog : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnConfirm()
    {
        Debug.Log("Confirmed ");
        Destroy(this.gameObject);
    }
}
