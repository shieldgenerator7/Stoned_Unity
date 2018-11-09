﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleScaling : MonoBehaviour
{//2018-08-11: copied from SimpleMovement

    //Settings
    public float scale = 2f;//the scale to transition to
    public float duration = 0.4f;//in seconds
    public float endDelay = 0.2f;//delay after reaching the end before resetting to the beginning

    //Runtime constants
    private float speed;
    private Vector3 startScale;
    private Vector3 endScale;
    private float scaleDiff;
    //Runtime vars
    private bool forwards = true;//true to move towards endScale
    private float lastKeyFrame = 0;//the last time it switched states
    private bool pausing = false;

    // Use this for initialization
    void Start()
    {
        startScale = transform.localScale;
        endScale = startScale * scale;
        scaleDiff = (endScale - startScale).magnitude;
        speed = scaleDiff / duration;
    }

    // Update is called once per frame
    void Update()
    {
        if (pausing)
        {
            if (Time.time > lastKeyFrame + endDelay)
            {
                lastKeyFrame = lastKeyFrame + endDelay;
                forwards = !forwards;
                pausing = false;
            }
        }
        else
        {
            if (forwards)
            {
                transform.localScale = Vector3.MoveTowards(
                    startScale,
                    endScale,
                    scaleDiff * (Time.time - lastKeyFrame) / duration
                    );
                if (transform.localScale == endScale)
                {
                    lastKeyFrame = lastKeyFrame + duration;
                    pausing = true;
                }
            }
            else
            {
                transform.localScale = Vector3.MoveTowards(
                    endScale,
                    startScale,
                    scaleDiff * (Time.time - lastKeyFrame) / duration
                    );
                if (transform.localScale == startScale)
                {
                    lastKeyFrame = lastKeyFrame + duration;
                    pausing = true;
                }
            }
        }
    }
}
