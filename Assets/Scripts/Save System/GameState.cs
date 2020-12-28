﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GameState
{
    public int id;
    public List<ObjectState> states = new List<ObjectState>();
    private ObjectState merky;//the object state in the list specifically for Merky
    public ObjectState Merky
    {
        get
        {
            if (merky == null)
            {
                merky = states.Find(os => os.objectName == "merky");
            }
            return merky;
        }
        private set => merky = value;
    }

    public static int nextid = 0;

    //Instantiation
    public GameState()
    {
        id = nextid;
        nextid++;
    }
    public GameState(List<GameObject> list) : this()
    {
        //Object States
        foreach (GameObject go in list)
        {
            try
            {
                ObjectState os = new ObjectState(go);
                states.Add(os);
                if (go.name == "merky")
                {
                    Merky = os;
                }
            }
            catch (NullReferenceException nre)
            {
                Debug.LogError(
                    "Object " + go.name + " does not have an ObjectInfo. NRE: " + nre,
                    go
                    );
            }
        }
    }
    //Loading
    public void load()
    {
        states.ForEach(os =>
        {
            GameObject go = Managers.Object.getObject(os.objectName);
            if (go != null && !ReferenceEquals(go, null))
            {
                os.loadState(go);
            }
            else
            {
                Scene scene = SceneManager.GetSceneByName(os.sceneName);
                if (scene.IsValid() && scene.isLoaded)
                {
                    //If scene is valid, Create the GameObject
                    AsyncOperationHandle<GameObject> op = Managers.Object.createObject(
                        os.objectName,
                        os.prefabGUID
                        );
                    //2020-12-28: Potential error: 
                    //if multiple game states try to create the gameobject,
                    //then they all will hook into the op's Completed handle,
                    //and if the handles get called in the wrong order,
                    //then the object might stop rewinding and be in the wrong state.
                    //This heavily relies on delegates being called in the order they're added.
                    op.Completed -= loadState;
                    op.Completed += loadState;
                }
            }
        });
    }
    private void loadState(AsyncOperationHandle<GameObject> op)
    {
        GameObject go = op.Result;
        ObjectState os = states.Find(os1 => os1.objectName == go.name);
        os.loadState(op.Result);
        SceneLoader.moveToScene(op.Result, os.sceneName);
    }
    public bool loadObject(GameObject go)
    {
        ObjectState state = states.Find(
            os => os.sceneName == go.scene.name && os.objectName == go.name
            );
        if (state != null)
        {
            state.loadState(go);
            return true;
        }
        return false;
    }

    //Returns true IFF the given GameObject has an ObjectState in this GameState
    public bool hasGameObject(GameObject go)
    {
        if (go == null)
        {
            throw new System.ArgumentNullException("GameState.hasGameObject() cannot accept null for go! go: " + go);
        }
        return states.Any(
            os => os.objectName == go.name && os.sceneName == go.scene.name
            );
    }
}
