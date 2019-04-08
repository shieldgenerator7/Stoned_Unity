﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerInput
{
    //2019-04-08: moved here from GestureManager

    public enum ClickState { Began, InProgress, Ended, None };
    public ClickState clickState = ClickState.None;

    //Original Positions
    public Vector3 origMP;//"original mouse position": the mouse position at the last mouse down (or tap down) event
    public Vector3 origMP2;//second orginal "mouse position" for second touch
    public Vector3 origMPWorld;//"original mouse position world" - the original mouse coordinate in the world
    public float origTime = 0f;//"original time": the clock time at the last mouse down (or tap down) event
    public float origOrthoSize = 1f;//"original orthographic size"
    //Current Positions
    public Vector3 curMP;//"current mouse position"
    public Vector3 curMP2;//"current mouse position" for second touch
    public Vector3 curMPWorld;//"current mouse position world" - the mouse coordinates in the world
    public float curTime = 0f;
    //Stats
    public int touchCount = 0;//how many touches to process, usually only 0 or 1, only 2 if zoom
    public float maxMouseMovement = 0f;//how far the mouse has moved since the last mouse down (or tap down) event
    public float holdTime = 0f;//how long the gesture has been held for    
    //Flags
    public bool cameraDragInProgress = false;
    public bool isDrag = false;
    public bool isTapGesture = true;
    public bool isHoldGesture = false;
    public bool isPinchGesture = false;
    public bool isCameraMovementOnly = false;//true to make only the camera move until the gesture is over

    public abstract PlayerInput getInput();

    /// <summary>
    /// Used in Update() to convey that the Input
    /// indicates the beginning of a new single-tap gesture,
    /// used often to transition between gestures with continuous input
    /// 2019-04-08: moved here from GestureManager
    /// </summary>
    /// <param name="tapIndex">The index of the tap in Input.GetTouch()</param>
    protected void beginSingleTapGesture(int tapIndex = 0)
    {
        touchCount = 1;
        clickState = ClickState.Began;
        origMP = Input.GetTouch(tapIndex).position;
        if (isPinchGesture)
        {
            isDrag = true;
        }
        else
        {
            isCameraMovementOnly = false;
        }
    }
}
