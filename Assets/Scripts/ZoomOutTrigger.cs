﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomOutTrigger : MemoryMonoBehaviour {

    //Settings
    public int scalePoint = (int)CameraController.CameraScalePoints.DEFAULT;
    public bool triggersOnce = true;//true if it only triggers once
    //State
    public bool triggered = false;//whether or this has zoomed out the camera

    void OnTriggerEnter2D(Collider2D coll)
    {
        if (coll.gameObject.isPlayer() && !GameManager.Rewinding)
        {
            trigger();
        }
    }

    public virtual void trigger()
    {
        CameraController camCtr = Managers.Camera;
        camCtr.ZoomLevel = camCtr.scalePointToZoomLevel(scalePoint);//zoom out
        if (scalePoint == (int)CameraController.CameraScalePoints.TIMEREWIND)
        {
            Managers.Gesture.switchGestureProfile("Rewind");
        }
        if (triggersOnce)
        {
            triggered = true;
            GameManager.saveMemory(this);
            Destroy(gameObject);
        }
    }

    public override MemoryObject getMemoryObject()
    {
        return new MemoryObject(this, triggered);
    }
    public override void acceptMemoryObject(MemoryObject memObj)
    {
        if (memObj.found)
        {
            if (triggersOnce)
            {
                triggered = true;
                GameManager.saveMemory(this);
                Destroy(gameObject);
            }
        }
    }
}
