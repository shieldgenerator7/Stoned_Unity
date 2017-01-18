﻿using UnityEngine;
using System.Collections;

public class ShieldBubbleAbility : PlayerAbility
{//2017-01-17: copied from ForceTeleportAbility
    public GameObject shieldRangeIndicator;//prefab
    private TeleportRangeIndicatorUpdater sriu;//"shield range indicator updater"
    private GameObject srii;//"shield range indicator instance"
    public GameObject shieldBubblePrefab;//prefab used to spawn shield bubbles
    //public GameObject explosionEffect;

    //public float maxForceAmount = 5000;
    //public float forceAmount = 10;//how much force to apply = forceAmount * 2^(holdTime*10)
    public float maxRange = 3;
    public float maxHoldTime = 1;//how long until the max range is reached

    public AudioClip shieldBubbleSound;

    public new bool takesGesture()
    {
        return true;
    }

    public new bool takesHoldGesture()
    {
        return true;
    }

    public new void processHoldGesture(Vector2 pos, float holdTime, bool finished)
    {
        float range = maxRange * holdTime * GestureManager.holdTimeScaleRecip / maxHoldTime;
        if (range > maxRange)
        {
            range = maxRange;
        }
        if (finished)
        {
            //Spawn Shield Bubble
            spawnShieldBubble(pos, range);
            AudioSource.PlayClipAtPoint(shieldBubbleSound, pos);
            //Destroy(srii);
            //srii = null;
        }
        else {
            if (srii == null)
            {
                srii = Instantiate(shieldRangeIndicator);
                sriu = srii.GetComponent<TeleportRangeIndicatorUpdater>();
                srii.GetComponent<SpriteRenderer>().enabled = false;
            }
            srii.transform.position = (Vector2)pos;
            sriu.setSize(range * 2);
        }
    }

    void spawnShieldBubble(Vector2 pos, float range)
    {
        GameObject newSB = (GameObject)Instantiate(shieldBubblePrefab);
        newSB.transform.position = pos;
        //newSB.GetComponent<ShieldBubble>().range = range;
    }
}
