﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrackedPiece : SavableMonoBehaviour
{
    //2017-05-02: the script to make broken pieces work with time rewind system
    //Apply to parent object of broken object prefab

    public string prefabName;
    public string spawnTag;//the tag to make it unique among the other pieces

    public override bool IsSpawnedScript => true;

    public override bool IsSpawnedObject => true;

    public override string PrefabName => prefabName;

    public override string SpawnTag => spawnTag;

    public override SavableObject getSavableObject()
    {
        return new SavableObject(this, "prefabName", prefabName, "spawnTag", spawnTag);
    }

    public override void acceptSavableObject(SavableObject savObj)
    {
        prefabName = (string)savObj.data["prefabName"];
        spawnTag = (string)savObj.data["spawnTag"];
    }
}
