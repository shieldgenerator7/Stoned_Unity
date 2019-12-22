﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : SavableMonoBehaviour
{
    [SerializeField]
    private float time = -1;//the time since the game started, accounting for rewind
    public float Time
    {
        get
        {
            float diff = UnityEngine.Time.time - lastCheckedTime;
            time += diff;
            lastCheckedTime = UnityEngine.Time.time;
            return time;
        }
        set
        {
            time = value;
            lastCheckedTime = UnityEngine.Time.time;
            endGameTimer.Active = true;
            endGameTimer.overrideStartTime(0);
        }
    }
    private float lastCheckedTime;//the Time.time point that it last checked for program time

    public Timer endGameTimer;

    private void Start()
    {
        if (time <= 0)
        {
            Time = 0;
        }
        endGameTimer.onTimeFinished += cycleEnded;
    }

    void cycleEnded()
    {
        Managers.Game.RewindToStart();
    }

    public override SavableObject getSavableObject()
    {
        return new SavableObject(this,
            "time", Time
            );
    }

    public override void acceptSavableObject(SavableObject savObj)
    {
        Time = (float)savObj.data["time"];
    }
}