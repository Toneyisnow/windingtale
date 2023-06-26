using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Objects;
using WindingTale.Core.Definitions;
using WindingTale.Core.Algorithms;

public class Creature : MonoBehaviour
{
    private int moveCount = 0;
    private bool isMoving = false;

    private FDMovePath path = null;

    public FDCreature creature
    {
        get; private set;
    }

    /*
    public CreatureIconComp(FDCreature creature)
    {
        this.creature = creature;
    }
    */

    public void SetCreature(FDCreature creature)
    {
        this.creature = creature;
    }

    public void StartMove(FDMovePath path)
    {
        Debug.Log("start move");
        isMoving = true;
        this.path = path;

        gameObject.AddComponent<CreatureWalk>();
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
                Debug.Log("Update position: " + path.Desitination);

                creature.Position = path.Desitination;
                Debug.Log("Moved to position: " + creature.Position.X + creature.Position.Y);

                // remove component
                Destroy(gameObject.GetComponent<CreatureWalk>());
            }
        }
        
    }
}
