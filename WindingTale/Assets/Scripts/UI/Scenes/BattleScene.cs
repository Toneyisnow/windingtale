using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;

public class BattleScene : MonoBehaviour
{
    public GameObject friendPlaceHolder;

    public GameObject enemyPlaceHolder;


    private FightInformation fightInformation = null;
    private FDCreature subject;
    private FDCreature target;


    // Start is called before the first frame update
    void Start()
    {
        DefinitionStore.Instance.LoadChapter(1);

        FDCreature subject = new FDCreature(1, CreatureFaction.Friend, DefinitionStore.Instance.GetCreatureDefinition(1), FDPosition.At(0, 0));
        FDCreature target = new FDCreature(11, CreatureFaction.Enemy, DefinitionStore.Instance.GetCreatureDefinition(50101), FDPosition.At(0, 0));

        this.Initialize(subject, target, null);
    }

    public void Initialize(FDCreature subject, FDCreature target, FightInformation fightInfo)
    {
        this.fightInformation = fightInfo;
        this.subject = subject;
        this.target = target;

        int subjectAniId = subject.Definition.AnimationId;
        FightAnimation subjectAni = DefinitionStore.Instance.GetFightAnimation(subjectAniId);

        int targetAniId = target.Definition.AnimationId;
        FightAnimation targetAni = DefinitionStore.Instance.GetFightAnimation(targetAniId);


        GameObject friendObject = new GameObject();
        friendObject.transform.parent = friendPlaceHolder.transform;
        friendObject.transform.localPosition = Vector3.zero;
        friendObject.transform.localScale = new Vector3(15, 15, 15);
        friendObject.transform.localEulerAngles = new Vector3(0, 90, 0);

        friendObject.AddComponent<SpriteRenderer>();


        Animator animator = friendObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Fights/001/a001");

        FightAniEvent friendAniEvent = friendObject.AddComponent<FightAniEvent>();

        //GameObject friendPrefab = Resources.Load<GameObject>("Fights/001/mObject");
        //GameObject friendObject = GameObject.Instantiate(friendPrefab);
        //friendObject.transform.parent = friendPlaceHolder.transform;
        /*
        friendObject.transform.localPosition = Vector3.zero;
        friendObject.transform.localScale = new Vector3(15, 15, 15);
        friendObject.transform.localEulerAngles = new Vector3(0, 90, 0);
        */

        friendAniEvent.OnAttackHittingCallback((value) => {
            this.OnFriendAttackHitting(value);
        });
        friendAniEvent.OnAttackCompleteCallback(() => {
            this.OnFriendAttackComplete();
        });

        //GameObject enemyPrefab = Resources.Load<GameObject>("Fights/701/Fight-701");
        //GameObject enemyObject = GameObject.Instantiate(enemyPrefab);
        //enemyObject.transform.parent = enemyPlaceHolder.transform;


    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnFriendAttackHitting(int value)
    {
        Debug.Log("OnFriendAttackHitting: " + value);
    }

    private void OnFriendAttackComplete()
    {
        Debug.Log("OnFriendAttackComplete.");
    }
}
