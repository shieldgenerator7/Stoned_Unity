﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AfterWind : SavableMonoBehaviour
{//2018-01-25: copied from GravityZone

    public Vector2 windVector;//direction and magnitude
    public float fadeOutDuration = 1.0f;//how long (sec) it will take for this to fade away

    private BoxCollider2D coll;
    private SpriteRenderer sr;
    private RaycastHit2D[] rch2dStartup = new RaycastHit2D[Utility.MAX_HIT_COUNT];
    private float fadeStartTime = 0f;//when the fade out started
    private float fadeEndTime = 0f;//when the fade out will end and this GameObject will be deleted

    // Use this for initialization
    void Start()
    {
        coll = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        init();
    }
    private void init()
    {
        if (Mathf.Approximately(fadeStartTime, 0))
        {
            fadeStartTime = Time.time;
        }
        fadeEndTime = fadeStartTime + fadeOutDuration;
    }
    public override SavableObject CurrentState
    {
        get => new SavableObject(this,
            "windVector", windVector,
            "fadeOutDuration", fadeOutDuration,
            "fadeTime", (Time.time - fadeStartTime)
            );
        set
        {
            windVector = value.Vector2("windVector");
            fadeOutDuration = value.Float("fadeOutDuration");
            fadeStartTime = Time.time - value.Float("fadeTime");
            init();
        }
    }
    public override bool IsSpawnedObject => true;

    public override string PrefabName => "ForceChargeAfterWind";


    void FixedUpdate()
    {
        //Decrease push force as the zone fades
        float fadeFactor = (fadeEndTime - Time.time) / fadeOutDuration;
        Vector2 pushVector = windVector * fadeFactor;

        //Push objects in zone
        int count = Utility.Cast(coll, Vector2.zero, rch2dStartup);
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D rb2d = rch2dStartup[i].collider.gameObject.GetComponent<Rigidbody2D>();
            if (rb2d)
            {
                GravityAccepter ga = rb2d.gameObject.GetComponent<GravityAccepter>();
                if (ga)
                {
                    if (ga.AcceptsGravity)
                    {
                        rb2d.AddForce(pushVector);
                    }
                }
                else
                {
                    rb2d.AddForce(pushVector);
                }
            }
        }
        //Fade the sprite
        sr.color = sr.color.adjustAlpha(Mathf.SmoothStep(0, 1, fadeFactor));
        if (fadeFactor <= 0 || Mathf.Approximately(fadeFactor, 0))
        {
            Managers.Object.destroyObject(gameObject);
        }
    }
}
