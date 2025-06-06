using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using WindingTale.Core.Objects;
using WindingTale.Core.Definitions;
using WindingTale.Core.Algorithms;
using WindingTale.UI.Scenes.Game;

public class Creature : MonoBehaviour
{
    private int moveCount = 0;
    private bool isMoving = false;

    private FDMovePath path = null;

    public FDCreature creature
    {
        get; private set;
    }

    public void SetCreature(FDCreature creature)
    {
        this.creature = creature;
    }

    public void StartMove(FDMovePath path)
    {
        isMoving = true;
        this.path = path;

        gameObject.AddComponent<CreatureWalk>();
    }

    public void OnClicked()
    {
        Debug.Log("OnClicked");

        GameInterface.Instance.OnMapClicked(this.creature.Position);
    }

    public void SetGreyout(bool greyout)
    {
        GameObject obj1 = gameObject.transform.Find("Clip_01").GetChild(0).Find("default").gameObject;
        GameObject obj2 = gameObject.transform.Find("Clip_02").GetChild(0).Find("default").gameObject;
        GameObject obj3 = gameObject.transform.Find("Clip_03").GetChild(0).Find("default").gameObject;

        if (greyout)
        {
            GameInterface.Instance.ApplyDefaultGreyMaterial(obj1);
            GameInterface.Instance.ApplyDefaultGreyMaterial(obj2);
            GameInterface.Instance.ApplyDefaultGreyMaterial(obj3);
        }
        else
        {
            GameInterface.Instance.ApplyDefaultMaterial(obj1);
            GameInterface.Instance.ApplyDefaultMaterial(obj2);
            GameInterface.Instance.ApplyDefaultMaterial(obj3);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            moveCount++;

            if (moveCount > 90)
            {
                isMoving = false;
                moveCount = 0;

                // update position
                creature.Position = path.Desitination;

                // remove component
                Destroy(gameObject.GetComponent<CreatureWalk>());
            }
        }
        
    }
}
