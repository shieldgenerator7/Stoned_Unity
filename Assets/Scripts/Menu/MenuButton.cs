﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuButton : MonoBehaviour {

    public MenuFrame frame;

    private BoxCollider2D bc2d;

	// Use this for initialization
	protected virtual void Start () {
        bc2d = GetComponent<BoxCollider2D>();
	}
	
    public bool tapInArea(Vector3 pos)
    {
        return bc2d.OverlapPoint(pos);
    }

    public virtual void activate()
    {
        Debug.Log("MenuButton " + name + " pressed");
        frame.frameCamera();
    }
}