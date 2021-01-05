﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class ObjectState
{
    //Transform
    public Vector3 position;//2017-10-10: actually stores the localPosition
    public Vector3 localScale;
    public Quaternion rotation;//2017-10-10: actually stores the localRotation
    //RigidBody2D
    public Vector2 velocity;
    public float angularVelocity;
    //Saveable Object
    public List<SavableObject> soList = new List<SavableObject>();
    //Name
    public int objectId = -1;
    public int priority = 0;//higher priority gets loaded first

    public ObjectState() { }
    public ObjectState(GameObject go)
    {
        SavableObjectInfo info = go.GetComponent<SavableObjectInfo>();
        objectId = info.Id;
        saveState(go);
    }

    private void saveState(GameObject go)
    {
        //Transform
        position = go.transform.position;
        localScale = go.transform.localScale;
        rotation = go.transform.rotation;
        //Rigidbody2D
        Rigidbody2D rb2d = go.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            velocity = rb2d.velocity;
            angularVelocity = rb2d.angularVelocity;
        }
        //SavableMonoBehaviours
        List<SavableMonoBehaviour> smbs = go.GetComponents<SavableMonoBehaviour>().ToList();
        smbs.ForEach(smb => soList.Add(smb.CurrentState));
        if (smbs.Count > 0)
        {
            priority = smbs.Max(smb => smb.Priority);
        }
    }
    public void loadState(GameObject go)
    {
        go.transform.position = position;
        go.transform.localScale = localScale;
        go.transform.rotation = rotation;
        Rigidbody2D rb2d = go.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.velocity = velocity;
            rb2d.angularVelocity = angularVelocity;
        }
        foreach (SavableObject so in this.soList)
        {
            SavableMonoBehaviour smb =
                (SavableMonoBehaviour)go.GetComponent(so.ScriptType);
            if (smb == null)
            {
                if (so.isSpawnedScript)
                {
                    smb = (SavableMonoBehaviour)so.addScript(go);
                }
                else
                {
                    throw new UnityException("Object " + go + " is missing non-spawnable script " + so.scriptType);
                }
            }
            smb.CurrentState = so;
        }
    }
}
