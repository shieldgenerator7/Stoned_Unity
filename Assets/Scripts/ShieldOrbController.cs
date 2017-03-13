﻿using UnityEngine;
using System.Collections;

public class ShieldOrbController : MonoBehaviour
{
    //2017-03-13: copied from ExplosionOrbController
    private CircleCollider2D cc2D;
    private ShieldBubbleAbility sba;
    private RaycastHit2D[] rh2ds = new RaycastHit2D[1];//used in Update() for a method call. Not actually updated

    private float chargeTime = 0.0f;//amount of time spent charging

    //Tutorial toggles
    public bool generatesUponContact = true;//false = requires mouseup (tap up) to explode
    public bool chargesAutomatically = true;//false = requires hold to charge
    public bool generatesAtAll = true;//false = doesn't do anything

    // Use this for initialization
    void Start()
    {
        cc2D = GetComponent<CircleCollider2D>();
        sba = GetComponent<ShieldBubbleAbility>();
    }

    // Update is called once per frame
    void Update()
    {
        if (generatesAtAll)
        {
            if (sba.canSpawnShieldBubble(transform.position, sba.maxRange))
            {
                if (generatesUponContact && isBeingTriggered())
                {
                    Debug.Log("Collision!");
                    if (chargeTime >= sba.maxHoldTime)
                    {
                        trigger();
                    }
                }
                if (chargesAutomatically)
                {
                    charge(Time.deltaTime);
                }
            }
            else
            {
                sba.dropHoldGesture();

            }
        }
    }

    public bool isBeingTriggered()
    {
        return cc2D.Cast(Vector2.zero, rh2ds, 0, true) > 0 && !rh2ds[0].collider.isTrigger;
    }

    public void charge(float deltaChargeTime)
    {
        chargeTime += deltaChargeTime;
        sba.processHoldGesture(transform.position, chargeTime, false);
    }

    public void trigger()
    {
        //Shield Orbs require to be at full capacity to deploy a shield
        if (sba.maxHoldTime <= chargeTime)
        {
            sba.processHoldGesture(transform.position, chargeTime, true);
            chargeTime = 0;
        }
        else
        {
            sba.dropHoldGesture();
        }
    }

}
