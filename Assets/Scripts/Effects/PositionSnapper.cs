﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionSnapper : MonoBehaviour
{

    public bool snapToPlayerGhosts = true;
    public float range = -1;
    public Vector2 offset = Vector2.zero;

    private Vector2 startPosition;

    // Use this for initialization
    void Awake()
    {
        startPosition = transform.position;
    }

    private void OnEnable()
    {
        if (snapToPlayerGhosts)
        {
            Vector2 position = GameManager.getClosestPlayerGhost(startPosition)
                .transform.position;
            if (range < 0 || Vector2.Distance(transform.position, position) < range)
            {
                transform.position = position + offset;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}