﻿using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{

    public GameObject player;
    public int viewMultiplier = 2;

    private Vector3 offset;
    private Camera cam;
    private Rigidbody2D playerRB2D;
    private float moveTime = 0f;//used to delay the camera refocusing on the player
    private bool wasDelayed = false;
    private GestureManager gm;

    // Use this for initialization
    void Start()
    {
        pinPoint();
        cam = GetComponent<Camera>();
        playerRB2D = player.GetComponent<Rigidbody2D>();
        gm = GameObject.FindGameObjectWithTag("GestureManager").GetComponent<GestureManager>();
    }

    // Update is called once per frame, after all other objects have moved that frame
    void LateUpdate()
    {
        //transform.position = player.transform.position + offset;
        if (moveTime <= Time.time && !gm.cameraDragInProgress)
        {
            if (wasDelayed)
            {
                wasDelayed = false;
                recenter();
            }
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.transform.position + offset,
                (Vector3.Distance(
                    transform.position,
                    player.transform.position) * 2 + playerRB2D.velocity.magnitude)
                    * Time.deltaTime);
        }
    }

    /**
    * Makes sure that the current camera movement delay is at least the given delay amount in seconds
    * @param delayAmount How much to delay camera movement by in seconds
    */
    public void delayMovement(float delayAmount)
    {
        if (moveTime < Time.time + delayAmount)
        {
            moveTime = Time.time + delayAmount;
        }
        wasDelayed = true;
    }

    public void discardMovementDelay()
    {
        moveTime = Time.time;
        wasDelayed = false;
    }

    //Sets the camera's offset so it stays at this position relative to the player
    public void pinPoint()
    {
        offset = transform.position - player.transform.position;
    }

    //Recenters on Merky
    public void recenter()
    {
        offset = new Vector3(0, 0, offset.z);
    }

    public void setZoomLevel(float level){
        if (level < 0)
        {
            level = 0;
        }
        //Set the size
        cam.orthographicSize = level;
        //Make sure player is still in view
        Vector3 size = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight)) - cam.ScreenToWorldPoint(new Vector3(0, 0)) + new Vector3(0, 0, 20);
        Bounds b = new Bounds(cam.transform.position, size);
        if ( ! b.Contains(player.transform.position))
        {
            float newX = offset.x;
            float newY = offset.y;
            Vector3 ppos = player.transform.position;
            if (ppos.x < b.min.x)
            {
                newX = b.extents.x;
            }
            else if (ppos.x > b.max.x)
            {
                newX = -b.extents.x;
            }
            if (ppos.y < b.min.y)
            {
                newY = b.extents.y;
            }
            else if (ppos.y > b.max.y)
            {
                newY = -b.extents.y;
            }
            offset = new Vector3(newX, newY, offset.z);
        }
    }
    public void adjustZoomLevel(float addend)
    {
        setZoomLevel(cam.orthographicSize + addend);
    }
}
