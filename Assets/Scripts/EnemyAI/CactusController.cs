﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CactusController : Hazard
{
    [SerializeField]
    private bool rooted = true;
    public bool Rooted
    {
        get => rooted;
        set
        {
            rooted = value;
            GetComponent<Rigidbody2D>().isKinematic = rooted;
        }
    }

    private void Start()
    {
        //Initialize state
        Rooted = rooted;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Rigidbody2D>())
        {
            Rooted = false;
        }
    }

    public override SavableObject getSavableObject()
    {
        SavableObject so = base.getSavableObject();
        so.data.Add("rooted", rooted);
        return so;
    }
    public override void acceptSavableObject(SavableObject savObj)
    {
        base.acceptSavableObject(savObj);
        Rooted = (bool)savObj.data["rooted"];
    }
}
