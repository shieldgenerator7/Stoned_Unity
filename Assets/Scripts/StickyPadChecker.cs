﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyPadChecker : SavableMonoBehaviour
{
    private HashSet<string> connectedObjs = new HashSet<string>();

    private Rigidbody2D rb2d;
    private Collider2D coll2d;
    private static RaycastHit2D[] rch2ds = new RaycastHit2D[100];

    // Use this for initialization
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        coll2d = GetComponent<Collider2D>();
    }

    public void init(Vector2 gravityDir)
    {
        transform.up = -gravityDir;
    }

    public override SavableObject getSavableObject()
    {//2018-02-21: copied from GameEventManager.getSavableObject()
        SavableObject so = new SavableObject(this);
        int counter = 0;
        foreach (string str in connectedObjs)
        {
            so.data.Add("conObj" + counter, str);
            counter++;
        }
        so.data.Add("conObjCount", counter);
        return so;
    }
    public override void acceptSavableObject(SavableObject savObj)
    {
        connectedObjs = new HashSet<string>();
        for (int i = 0; i < (int)savObj.data["conObjCount"]; i++)
        {
            connectedObjs.Add((string)savObj.data["conObj" + i]);
        }
        foreach (FixedJoint2D fj2d in GetComponents<FixedJoint2D>())
        {
            if (!connectedObjs.Contains(fj2d.connectedBody.gameObject.name))
            {
                Destroy(fj2d);
            }
        }
        //Get list of objects in area
        int colliderCount = coll2d.Cast(Vector2.zero, rch2ds, 0);
        //Find names that don't have a FixedJoint2D
        foreach (string objname in connectedObjs)
        {
            bool foundone = false;
            foreach (FixedJoint2D fj2d in GetComponents<FixedJoint2D>())
            {
                if (fj2d.connectedBody.gameObject.name == objname)
                {
                    //found one, break the inner loop
                    foundone = true;
                    break;
                }
            }
            if (!foundone)
            {
                //Find the name's object
                GameObject conObj = null;
                for (int i = 0; i < colliderCount; i++)
                {
                    RaycastHit2D rch2d = rch2ds[i];
                    if (rch2d.collider.gameObject.name == objname)
                    {
                        conObj = rch2d.collider.gameObject;
                        break;
                    }
                }
                //create new FixedJoint2D
                if (conObj != null)
                {
                    stickToObject(conObj);
                }
                else
                {
                    throw new UnityException("Connected object \"" + objname + "\" not found! StickyPadChecker: " + gameObject.name);
                }
            }
        }
    }
    public override bool isSpawnedObject()
    {
        return true;
    }
    public override string getPrefabName()
    {
        return "StickyPad";
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.isTrigger)
        {
            stickToObject(collision.gameObject);
        }
    }
    private void OnTriggerEnter2D(Collider2D coll)
    {
        if (!coll.isTrigger)
        {
            stickToObject(coll.gameObject);
        }
    }
    void stickToObject(GameObject go)
    {
        Rigidbody2D goRB2D = go.GetComponent<Rigidbody2D>();
        if (goRB2D)
        {
            bool foundObj = false;
            foreach (FixedJoint2D fj2d in GetComponents<FixedJoint2D>())
            {
                if (fj2d.connectedBody == goRB2D)
                {
                    foundObj = true;
                    break;
                }
            }
            if (!foundObj)
            {
                FixedJoint2D fj2d = gameObject.AddComponent<FixedJoint2D>();
                fj2d.connectedBody = goRB2D;
                fj2d.autoConfigureConnectedAnchor = false;
            }
        }
        else
        {
            if (!connectedObjs.Contains(go.name))
            {
                TargetJoint2D tj2d = gameObject.AddComponent<TargetJoint2D>();
                tj2d.autoConfigureTarget = false;
                rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
                connectedObjs.Add(go.name);
            }
        }
    }
}
