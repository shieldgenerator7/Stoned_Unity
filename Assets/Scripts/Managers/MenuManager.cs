﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{

    public MenuFrame startFrame;

    public List<MenuFrame> frames = new List<MenuFrame>();
    
    private void Start()
    {
        foreach (MenuFrame mf in FindObjectsOfType<MenuFrame>())
        {
            if (mf.canDelegateTaps())
            {
                frames.Add(mf);
            }
        }
        GameObject player = Managers.Player.gameObject;
        transform.position = player.transform.position;
        transform.rotation = player.transform.rotation;
        startFrame.frameCamera();
    }

    private void Update()
    {
        Managers.Camera.setRotation(Managers.Player.transform.up);
    }

    public void processTapGesture(Vector3 pos)
    {
        foreach (MenuFrame mf in frames)
        {
            if (mf.tapInArea(pos))
            {
                mf.delegateTap(pos);
                return;
            }
        }
    }
    public bool processDragGesture(Vector3 origMPWorld, Vector3 newMPWorld)
    {
        foreach (MenuFrame mf in frames)
        {
            if (mf.tapInArea(origMPWorld))
            {
                if (mf.delegateDrag(origMPWorld, newMPWorld))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool isMenuOpen()
    {
        return Managers.Menu != null;
    }
}
