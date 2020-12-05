﻿
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class stores variables that need to be saved from SavableMonoBehaviours
/// </summary>
public class SavableObject
{

    public Dictionary<string, System.Object> data = new Dictionary<string, System.Object>();
    /// <summary>
    /// True if it's an object that spawned during runtime
    /// </summary>
    public bool isSpawnedObject;//whether this SO's game object was spawned during run time
    public bool isSpawnedScript;//whether this SO's script was attached to its game object during run time
    public string scriptType;//the type of script that saved this SavableObject
    public string prefabName = "";//if isSpawnedObject, what the prefab name is. Prefab must be in the Resources folder
    public string spawnTag = "";//if isSpawnedObject, the unique tag applied to it to give it a unique name

    public SavableObject() { }

    /// <summary>
    /// Constructs a SavableObject with the given pieces of data
    /// Enter data in pairs: key,object,... 
    /// Example: "cracked",true,"name","CrackedGround"
    /// </summary>
    /// <param name="pairs"></param>
    public SavableObject(SavableMonoBehaviour smb, params System.Object[] pairs)
    {
        this.scriptType = smb.GetType().Name;

        more(pairs);

        if (smb.IsSpawnedObject)
        {
            isSpawnedObject = true;
            prefabName = smb.PrefabName;
            spawnTag = smb.SpawnTag;
        }
        if (smb.IsSpawnedScript)
        {
            isSpawnedScript = true;
        }
    }

    /// <summary>
    /// Used to add things to an existing SavableObject
    /// and return it in one line
    /// </summary>
    /// <param name="pairs"></param>
    /// <returns></returns>
    public SavableObject more(params System.Object[] pairs)
    {
        if (pairs.Length % 2 != 0)
        {
            throw new UnityException("Pairs has an odd amount of parameters! pairs.Length: " + pairs.Length);
        }
        for (int i = 0; i < pairs.Length; i += 2)
        {
            data.Add((string)pairs[i], pairs[i + 1]);
        }
        return this;
    }

    /// <summary>
    /// Spawn this saved object's game object
    /// This method is used during load
    /// precondition: the game object does not already exist (or at least has not been found)
    /// </summary>
    /// <returns></returns>
    public GameObject spawnObject()
    {
        GameObject prefab = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/" + prefabName));
        if (spawnTag != null && spawnTag != "")
        {
            prefab.name += spawnTag;
            foreach (Transform t in prefab.transform)
            {
                if (!t.gameObject.name.Contains(spawnTag))
                {
                    t.gameObject.name += spawnTag;
                }
            }
        }
        return (GameObject)prefab;
    }

    public System.Type getSavableMonobehaviourType()
        => getSavableMonobehaviourType(scriptType);

    /// <summary>
    /// The definitive list of known savable types
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public static System.Type getSavableMonobehaviourType(string typeName)
    {
        switch (typeName)
        {
            case "HardMaterial":
                return typeof(HardMaterial);
            case "BreakableWall":
                return typeof(BreakableWall);
            case "GravityAccepter":
                return typeof(GravityAccepter);
            case "GestureManager":
                return typeof(GestureManager);
            case "TimeManager":
                return typeof(TimeManager);
            case "GameEventManager":
                return typeof(GameEventManager);
            case "GameStatistics":
                return typeof(GameStatistics);
            case "SceneLoader":
                return typeof(SceneLoader);
            case "PlayerAbility":
                return typeof(PlayerAbility);
            case "TeleportAbility":
                return typeof(TeleportAbility);
            case "ForceLaunchAbility":
                return typeof(ForceLaunchAbility);
            case "SwapAbility":
                return typeof(SwapAbility);
            case "WallClimbAbility":
                return typeof(WallClimbAbility);
            case "AirSliceAbility":
                return typeof(AirSliceAbility);
            case "ElectricFieldAbility":
                return typeof(ElectricFieldAbility);
            case "LongTeleportAbility":
                return typeof(LongTeleportAbility);
            case "AfterWind":
                return typeof(AfterWind);
            case "StickyPadChecker":
                return typeof(StickyPadChecker);
            case "ElectricFieldController":
                return typeof(ElectricFieldController);
            case "PowerConduit":
                return typeof(PowerConduit);
            case "CrackedPiece":
                return typeof(CrackedPiece);
            case "BalloonController":
                return typeof(BalloonController);
            case "NPCController":
                return typeof(NPCController);
            case "NPCMetalController":
                return typeof(NPCMetalController);
            case "NPCVoiceLine":
                return typeof(NPCVoiceLine);
            case "SnailController":
                return typeof(SnailController);
            case "StaticHazard":
                return typeof(StaticHazard);
            case "CrabController":
                return typeof(CrabController);
            case "CactusController":
                return typeof(CactusController);
            default:
                throw new KeyNotFoundException(
                    "The type name \"" + typeName + "\" was not found. "
                    + "It might not be a SavableMonoBehaviour or might not exist. "
                    + "You might have to add it to this list."
                    );
        }
    }

    ///<summary>
    ///Adds this SavableObject's SavableMonobehaviour to the given GameObject
    ///</summary>
    ///<param name="go">The GameObject to add the script to</param>
    public virtual Component addScript(GameObject go)
    {
        return go.AddComponent(getSavableMonobehaviourType());
    }
}
