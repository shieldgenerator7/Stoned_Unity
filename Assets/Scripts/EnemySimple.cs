﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySimple : MonoBehaviour
{

    public float speed = 1.0f;//units per second
    public float healsPerSecond = 5.0f;
    [Range(0, 1)]
    public float onDamageHealPercent = 0.2f;//what percent of damage it does to others it heals itself
    public bool activeMove = false;//controls whether it can move or not
    public float allowedLeftAndRightVariance = 25.0f;//used to determine if a colliding object is left or right of this enemy
    public float directionSwitchCooldown = 0.5f;//how many seconds after switching direction this enemy can switch it again
    public float sightRange = 10.0f;//how far away from itself it can see

    public ParticleSystem fearParticles;//the particle system that activates when the enemy is frightened

    private Vector2 direction = Vector2.left;
    private bool mustSwitchDirection = false;//whether or not switch direction should be called
    private float lastDirectionSwitchTime = 0.0f;//the last time that the direction was switched
    private bool quickTurn = false;//switch direction but quickly turn again
    private bool goingRight = true;//whether the bug is going right relative to its orientation
    private bool healing = false;
    private bool losToPlayer = false;//"Line Of Sight to Player": whether this bug can see the player
    [Range(0, 1)]
    public float senseVerticalOffset = 0.25f;//how higher up then center point wall detection ray should start
    private static RaycastHit2D[] rch2ds = new RaycastHit2D[Utility.MAX_HIT_COUNT];//for processing collider casts

    private Rigidbody2D rb2d;
    private HardMaterial hm;
    private BoxCollider2D groundCollider;//collider used to see if the enemy is touching the ground
    private GravityAccepter gravity;

    // Use this for initialization
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        hm = GetComponent<HardMaterial>();
        hm.hardCollision += eatDamage;
        groundCollider = GetComponent<BoxCollider2D>();
        gravity = GetComponent<GravityAccepter>();
        direction = Utility.PerpendicularLeft(transform.up).normalized;
        direction = transform.right;
    }

    private void Update()
    {
        losToPlayer = false;
        if (Managers.Player.transform.position.inRange(transform.position, sightRange))
        {
            losToPlayer = gameObject.lineOfSight(Managers.Player.gameObject);
        }
        if (losToPlayer)
        {
            if (!fearParticles.isPlaying)
            {
                fearParticles.Play();
            }
        }
        else
        {
            if (fearParticles.isPlaying)
            {
                fearParticles.Stop();
            }
        }
        if (!rb2d.isMoving())
        {
            hm.addIntegrity(healsPerSecond * Time.deltaTime);
            if (hm.getIntegrity() == hm.maxIntegrity)
            {
                healing = false;
            }
        }
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (activeMove && isGrounded())
        {
            float tempSpeed = speed;
            if (rb2d.velocity.magnitude < speed / 4)
            {
                tempSpeed *= speed * 2;
            }
            if (hm.getIntegrity() < hm.maxIntegrity / 4 || healing)
            {
                healing = true;
                tempSpeed = 0;
            }
            if (tempSpeed > 0)
            {
                rb2d.AddForce(rb2d.mass * direction * tempSpeed);
            }
            rb2d.AddForce(rb2d.mass * -transform.up * 0.1f);
            //Cliff detection
            if (!losToPlayer) //nothing between it and the player
            {
                if (senseFloorInFront() == null) //there's a cliff up ahead
                {
                    //Logger.log(this.gameObject, "Switchdir cliff ahead");
                    mustSwitchDirection = true;
                }
            }
            GameObject wall = senseWallInFront();
            if (wall != null)
            {
                if (!wall.GetComponent<Rigidbody>() && !wall.GetComponent<HardMaterial>())
                {
                    //Logger.log(this.gameObject, "Switchdir hitting wall: " + wall.name);
                    mustSwitchDirection = true;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (mustSwitchDirection || quickTurn)
        {
            if (Time.time - lastDirectionSwitchTime >= directionSwitchCooldown)
            {
                lastDirectionSwitchTime = Time.time;
                switchDirection();
                if (isGrounded())
                {
                    rb2d.AddForce(rb2d.mass * rb2d.velocity.magnitude * direction);
                }
                //if it switched because of quickTurn, turn quickTurn off
                if (!mustSwitchDirection)
                {
                    quickTurn = false;
                }
            }
        }
        mustSwitchDirection = false;
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        ContactPoint2D[] cp2ds = new ContactPoint2D[1];
        coll.GetContacts(cp2ds);
        //If the object is left or ride of this enemy
        float angle = Vector2.Angle(transform.up, cp2ds[0].point - (Vector2)transform.position);
        if (angle > 90 - allowedLeftAndRightVariance && angle < 90 + allowedLeftAndRightVariance)
        {
            //Logger.log(this.gameObject, "Switchdir after collision: " + coll.gameObject.name);
            mustSwitchDirection = true;
            if (coll.gameObject.GetComponent<HardMaterial>() != null || coll.gameObject.GetComponent<Rigidbody2D>() != null)
            {
                quickTurn = true;
            }
        }
    }

    /// <summary>
    /// Heals the diamond shell when it does damage
    /// Called from HardMaterial.hardCollision
    /// </summary>
    /// <param name="damageToSelf"></param>
    /// <param name="damageToOther"></param>
    void eatDamage(float damageToSelf, float damageToOther, Vector2 contactPoint)
    {
        hm.addIntegrity(damageToOther * onDamageHealPercent);
    }

    void switchDirection()
    {
        goingRight = !goingRight;
        if (goingRight)
        {
            direction = transform.right;
        }
        else
        {
            direction = -transform.right;
        }

        float facingDir = -1 * Mathf.Sign(direction.x);//times -1 bc the sprite was drawn facing left
        if (facingDir != transform.localScale.x)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(facingDir) * Mathf.Abs(transform.localScale.x);
            transform.localScale = scale;
        }
    }

    GameObject senseFloorInFront()
    {
        Vector2 ahead = direction * 2;
        Vector2 senseDir = ahead - (Vector2)transform.up.normalized;
        Debug.DrawLine((Vector2)transform.position + ahead, senseDir + (Vector2)transform.position, Color.blue);
        Utility.RaycastAnswer answer = Utility.RaycastAll((Vector2)transform.position + ahead, -transform.up, 1);
        for (int i = 0; i < answer.count; i++)
        {
            RaycastHit2D rch2d = answer.rch2ds[i];
            if (!rch2d.collider.isTrigger)
            {
                return rch2d.collider.gameObject;
            }
        }
        return null;
    }
    GameObject senseWallInFront()
    {
        Vector2 ahead = direction * Mathf.Abs(transform.localScale.x);
        float distance = 0.1f;
        Vector2 length = direction * distance * Mathf.Abs(transform.localScale.x);
        Vector2 senseDir = ahead + length;
        Vector2 offset = transform.up.normalized * senseVerticalOffset;
        Debug.DrawLine((Vector2)transform.position + offset + ahead, (Vector2)transform.position + offset + senseDir, Color.green);
        Utility.RaycastAnswer answer = Utility.RaycastAll((Vector2)transform.position + offset + ahead, length, distance);
        for (int i = 0; i < answer.count; i++)
        {
            RaycastHit2D rch2d = answer.rch2ds[i];
            if (!rch2d.collider.isTrigger)
            {
                return rch2d.collider.gameObject;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns true IFF the bottom is touching the ground
    /// </summary>
    /// <returns></returns>
    bool isGrounded()
    {
        int amount = Utility.Cast(groundCollider, -transform.up, rch2ds, 0, true);
        for (int i = 0; i < amount; i++)
        {
            RaycastHit2D rch2d = rch2ds[i];
            if (!rch2d.collider.isTrigger
                && rch2d.collider.gameObject != gameObject)
            {
                return true;
            }
        }
        return false;
    }
}
