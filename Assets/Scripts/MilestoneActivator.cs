﻿using UnityEngine;

public abstract class MilestoneActivator : MemoryMonoBehaviour {

    public int incrementAmount = 1;
    public GameObject particle;
    public int starAmount = 25;
    public int starSpawnDuration = 25;
    
    public bool used = false;
    private float minX, maxX, minY, maxY;

    // Use this for initialization
    void Start()
    {
        if (transform.parent != null)
        {
            Bounds bounds = GetComponentInParent<SpriteRenderer>().bounds;
            float extra = 0.1f;
            minX = bounds.min.x - extra;
            maxX = bounds.max.x + extra;
            minY = bounds.min.y - extra;
            maxY = bounds.max.y + extra;
        }
    }

    void OnTriggerEnter2D(Collider2D coll)
    {
        if (!used && coll.gameObject.Equals(GameManager.getPlayerObject()))
        {
            if (transform.parent != null)
            {
                sparkle();
            }
            used = true;
            activateEffect();
            GameManager.saveMemory(this);
            Destroy(this);//makes sure it can only be used once
        }
    }

    public abstract void activateEffect();

    protected void sparkle()
    {//2016-03-17: copied from PlayerController.showTeleportStar(..)
        for (int i = 0; i < starAmount; i++)
        {
            GameObject newTS = (GameObject)Instantiate(particle);
            TeleportStarUpdater tsu = newTS.GetComponent<TeleportStarUpdater>();
            tsu.start = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY));
            tsu.waitTime = i*(starAmount/starSpawnDuration);
            tsu.position();
            tsu.turnOn(true);
        }
    }

    public override MemoryObject getMemoryObject()
    {
        return new MilestoneActivatorMemory(this);
    }
}

//
//Class that saves the important variables of this class
//
public class MilestoneActivatorMemory : MemoryObject
{
    public MilestoneActivatorMemory() { }//only called by the method that reads it from the file
    public MilestoneActivatorMemory(MilestoneActivator ha) : base(ha)
    {
        saveState(ha);
    }

    public override void loadState(GameObject go)
    {
        MilestoneActivator ma = go.GetComponent<MilestoneActivator>();
        if (ma != null)
        {
            if (this.found)
            {
                ma.used = true;
                ma.activateEffect();
            }
        }
    }
    public override void saveState(MemoryMonoBehaviour mmb)
    {
        MilestoneActivator ma = ((MilestoneActivator)mmb);
        this.found = ma.used;
    }
}
