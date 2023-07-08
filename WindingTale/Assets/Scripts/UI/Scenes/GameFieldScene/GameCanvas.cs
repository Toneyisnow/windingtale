using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCanvas : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool HasDialog()
    {
        return transform.Find("MessageDialog") != null || transform.Find("CreatureDialog") != null;
    }

    public void ShowMessageDialog()
    {
        GameObject dialogPrefab = Resources.Load<GameObject>("Dialogs/MessageDialog");
        GameObject dialog = Instantiate(dialogPrefab, transform);
        dialog.name = "MessageDialog";
    }

    public void ShowCreatureDialog()
    {
        GameObject dialogPrefab = Resources.Load<GameObject>("Dialogs/CreatureDialog");
        GameObject dialog = Instantiate(dialogPrefab, transform);
        dialog.name = "CreatureDialog";
    }
}
