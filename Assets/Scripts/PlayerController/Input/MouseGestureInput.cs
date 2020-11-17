﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseGestureInput : GestureInput
{
    public int mouseButton = 0;
    public int mouseButton2 = 1;
    public float dragThreshold = 50;
    public float holdThreshold = 0.2f;

    private DragType dragType = DragType.UNKNOWN;

    private Vector2 origPosScreen;
    private Vector2 OrigPosWorld
    {
        get => Utility.ScreenToWorldPoint(origPosScreen);
    }
    private float origTime;

    private enum MouseEvent
    {
        UNKNOWN,
        CLICK,
        DRAG,
        HOLD,
        SCROLL
    }
    private MouseEvent mouseEvent = MouseEvent.UNKNOWN;

    public override bool InputSupported
    {
        get => Input.mousePresent;
    }

    public override InputDeviceMethod InputType
    {
        get => InputDeviceMethod.MOUSE;
    }

    public override bool InputOngoing
    {
        get => Input.GetMouseButton(mouseButton)
            || Input.GetMouseButtonUp(mouseButton)
            || Input.GetMouseButton(mouseButton2)
            || Input.GetMouseButtonUp(mouseButton2)
            || Input.GetAxis("Mouse ScrollWheel") != 0;
    }

    public override bool processInput(GestureProfile profile)
    {
        //Debug.Log("mouse down: " + Input.GetMouseButtonDown(mouseButton) + " " + Time.time);
        //Debug.Log("mouse press: " + Input.GetMouseButton(mouseButton) + " " + Time.time);
        //Debug.Log("mouse up: " + Input.GetMouseButtonUp(mouseButton) + " " + Time.time);
        if (InputOngoing)
        {
            //
            //Check for click start
            //
            if (mouseEvent == MouseEvent.UNKNOWN)
            {
                //Click beginning
                if (Input.GetMouseButtonDown(mouseButton))
                {
                    origPosScreen = Input.mousePosition;
                    origTime = Time.time;
                    dragType = DragType.DRAG_PLAYER;
                }
                else if (Input.GetMouseButtonDown(mouseButton2))
                {
                    origPosScreen = Input.mousePosition;
                    origTime = Time.time;
                    dragType = DragType.DRAG_CAMERA;
                }
                else if (Input.GetAxis("Mouse ScrollWheel") != 0)
                {
                    mouseEvent = MouseEvent.SCROLL;
                }
                //Click middle
                else
                {
                    //Check Drag
                    float dragDistance = Vector2.Distance(origPosScreen, Input.mousePosition);
                    if (dragDistance >= dragThreshold)
                    {
                        mouseEvent = MouseEvent.DRAG;
                    }
                    //Check Hold
                    else if (Time.time - origTime >= holdThreshold)
                    {
                        //If trying to start a drag on Merky
                        if (dragDistance > 10 && Managers.Player.gestureOnPlayer(OrigPosWorld))
                        {
                            mouseEvent = MouseEvent.DRAG;
                        }
                        else
                        {
                            mouseEvent = MouseEvent.HOLD;
                        }
                    }
                }
            }

            //
            //Main Processing
            //

            switch (mouseEvent)
            {
                case MouseEvent.DRAG:
                    profile.processDragGesture(
                        OrigPosWorld,
                        Utility.ScreenToWorldPoint(Input.mousePosition),
                        dragType,
                        Input.GetMouseButtonUp(mouseButton)
                        );
                    break;
                case MouseEvent.HOLD:
                    profile.processHoldGesture(
                        Utility.ScreenToWorldPoint(Input.mousePosition),
                        Time.time - origTime,
                        Input.GetMouseButtonUp(mouseButton)
                        );
                    break;
                case MouseEvent.SCROLL:
                    if (Input.GetAxis("Mouse ScrollWheel") < 0)
                    {
                        Managers.Camera.ZoomLevel *= 1.2f;
                    }
                    else if (Input.GetAxis("Mouse ScrollWheel") > 0)
                    {
                        Managers.Camera.ZoomLevel /= 1.2f;
                    }
                    break;
            }

            //
            //Check for click end
            //
            if (Input.GetMouseButtonUp(mouseButton))
            {
                //If it's unknown,
                if (mouseEvent == MouseEvent.UNKNOWN)
                {
                    //Then it's a click.
                    mouseEvent = MouseEvent.CLICK;
                    profile.processTapGesture(Utility.ScreenToWorldPoint(Input.mousePosition));
                }
                dragType = DragType.UNKNOWN;
            }
            return true;
        }
        else
        {
            mouseEvent = MouseEvent.UNKNOWN;
            return false;
        }
    }
}
