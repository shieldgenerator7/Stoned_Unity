﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LongTeleportAbility : PlayerAbility {

    protected override void init()
    {
        base.init();
        Managers.Camera.onOffsetChange += adjustRange;
    }
    public override void OnDisable()
    {
        base.OnDisable();
        Managers.Camera.onOffsetChange -= adjustRange;
    }

    /// <summary>
    /// Adjusts (increases) Merky's range the further out the camera is dragged.
    /// </summary>
    void adjustRange(Vector3 offset)
    {
        playerController.Range = playerController.baseRange + ((Vector2)offset).magnitude;
    }

    protected override void showTeleportEffect(Vector2 oldPos, Vector2 newPos)
    {
        if (playerController.Range > playerController.baseRange)
        {
            base.showTeleportEffect(oldPos, newPos);
        }
    }

}
