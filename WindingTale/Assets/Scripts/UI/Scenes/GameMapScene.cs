using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using static UnityEditor.Progress;

public class GameMapScene : MonoBehaviour
{
    public GameObject mapObjects;

    public GameObject creatureIconPrefab;

    private Material defaultMaterial = null; 


    // Start is called before the first frame update
    void Start()
    {
        defaultMaterial = Resources.Load<Material>("Materials/material-fd2-palette");

        FDCreature creature = new FDCreature(102, WindingTale.Core.Definitions.CreatureFaction.Friend);
        AddCreatureUI(creature, FDPosition.At(0, 0));



    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void AddCreatureUI(FDCreature creature, FDPosition pos)
    {
        GameObject creatureIcon = Instantiate(creatureIconPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        creatureIcon.transform.SetParent(transform);

        AttachIcon(string.Format("Icons/{0}/Icon_{0}_01", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_01"));
        AttachIcon(string.Format("Icons/{0}/Icon_{0}_02", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_02"));
        AttachIcon(string.Format("Icons/{0}/Icon_{0}_03", StringUtils.Digit3(creature.Definition.AnimationId)), creatureIcon.transform.Find("Clip_03"));

        Creature comp = creatureIcon.AddComponent<Creature>();
        comp.SetCreature(creature);
    }

    private void AttachIcon(string iconFilePath, Transform parent)
    {
        GameObject prefab = Resources.Load<GameObject>(iconFilePath);
        GameObject d = prefab.transform.Find("default").gameObject;
        MeshRenderer renderer = d.GetComponent<MeshRenderer>();
        renderer.materials = new Material[1] { defaultMaterial };

        GameObject icon = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
        icon.transform.SetParent(parent);
    }

}
