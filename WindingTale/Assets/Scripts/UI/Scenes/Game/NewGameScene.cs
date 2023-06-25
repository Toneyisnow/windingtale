using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

public class NewGameScene : MonoBehaviour, IGameInterface
{
    public GameObject creatureIconPrefab;

    private Material defaultMaterial = null;

    // Start is called before the first frame update
    void Start()
    {
        defaultMaterial = Resources.Load<Material>("Materials/material-fd2-palette");

        GameMain gameMain = GameMain.StartNewGame(this);



    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddCreatureUI(FDCreature creature, FDPosition pos)
    {
        GameObject creatureIcon = Instantiate(creatureIconPrefab, new Vector3(0, 0, 0), Quaternion.identity);

        creatureIcon.name = string.Format("creature_{0}", creature.Id);
        creatureIcon.transform.SetParent(transform);

        AttachIcon(string.Format("Icons/{0}/Icon_{0}_01", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_01"));
        AttachIcon(string.Format("Icons/{0}/Icon_{0}_02", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_02"));
        AttachIcon(string.Format("Icons/{0}/Icon_{0}_03", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_03"));

        CreatureIconComp comp = creatureIcon.GetComponent<CreatureIconComp>();
        comp.SetCreature(creature);

        creature.Position = pos;
    }


    public void MoveCreatureUI(FDCreature creature, FDMovePath path)
    {
        GameObject creatureObject = transform.Find(string.Format("creature_{0}", creature.Id)).gameObject;

        CreatureIconComp creatureComp = creatureObject.GetComponent<CreatureIconComp>();
        creatureComp.SetCreature(creature);
        creatureComp.StartMove(path);

    }


    #region Private Methods

    private void AttachIcon(string iconFilePath, Transform parent)
    {
        Debug.Log("iconFilePath: " + iconFilePath);

        GameObject prefab = Resources.Load<GameObject>(iconFilePath);
        GameObject d = prefab.transform.Find("default").gameObject;
        MeshRenderer renderer = d.GetComponent<MeshRenderer>();
        renderer.materials = new Material[1] { defaultMaterial };

        GameObject icon = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
        icon.transform.SetParent(parent);
    }

    #endregion
}
