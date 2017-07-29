﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : SavableMonoBehaviour
{
    AudioSource source;

    public List<NPCVoiceLine> voiceLines;

    private GameObject playerObject;

    //State
    public int currentVoiceLineIndex = -1;//the index of the voiceline that is currently playing, -1 if none

    // Use this for initialization
    protected virtual void Start()
    {
        source = GetComponent<AudioSource>();
        playerObject = GameManager.getPlayerObject();
    }

    public override SavableObject getSavableObject()
    {
        return new SavableObject(this,
            "currentVoiceLineIndex", currentVoiceLineIndex,
            "playBackTime", source.time);
    }
    public override void acceptSavableObject(SavableObject savObj)
    {
        currentVoiceLineIndex = (int)savObj.data["currentVoiceLineIndex"];
        float playBackTime = (float)savObj.data["playBackTime"];
        setVoiceLine(currentVoiceLineIndex, playBackTime);
    }

    // Update is called once per frame
    void Update()
    {
        source.transform.position = transform.position;
        //Debug.Log("Number things found: " + thingsFound);
        if (canGreet())
        {
            if (!source.isPlaying)
            {
                int mrvli = getMostRelevantVoiceLineIndex();
                if (mrvli >= 0)
                {
                    setVoiceLine(mrvli);
                    NPCVoiceLine npcvl = voiceLines[mrvli];
                    npcvl.played = true;
                    if (npcvl.triggerEvent != null)
                    {
                        GameEventManager.addEvent(npcvl.triggerEvent);
                    }
                }
            }
        }
        else
        {
            if (shouldStop())
            {
                source.Stop();
            }
        }
        if (source.isPlaying)
        {
            GameManager.speakNPC(gameObject, true);
        }
        else
        {
            currentVoiceLineIndex = -1;
            GameManager.speakNPC(gameObject, false);
        }
    }

    /// <summary>
    /// Whether or not this NPC should only greet once
    /// </summary>
    /// <returns></returns>
    protected virtual bool greetOnlyOnce()
    {
        return true;
    }

    /// <summary>
    /// Returns whether or not this NPC can play its greeting voiceline
    /// </summary>
    /// <returns></returns>
    protected virtual bool canGreet()
    {
        float distance = Vector3.Distance(playerObject.transform.position, transform.position);
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, playerObject.transform.position - transform.position, distance);
        int thingsFound = hits.Length;
        return distance < 5 && thingsFound == 2;
    }

    protected virtual bool shouldStop()
    {
        return Vector3.Distance(playerObject.transform.position, transform.position) > 10;
    }

    public NPCVoiceLine getMostRelevantVoiceLine()
    {
        int mrvli = getMostRelevantVoiceLineIndex();
        if (mrvli < 0)
        {
            return null;
        }
        return voiceLines[mrvli];
    }
    public int getMostRelevantVoiceLineIndex()
    {
        for (int i = voiceLines.Count - 1; i >= 0; i--)
        {
            NPCVoiceLine npcvl = voiceLines[i];
            if (!npcvl.played && GameEventManager.eventHappened(npcvl.eventReq))
            {
                return i;
            }
            else if (npcvl.played && npcvl.checkPointLine)
            {
                return -1;
            }
        }
        return -1;
    }

    /// <summary>
    /// Sets the current voiceline and the playback time
    /// </summary>
    /// <param name="index">The index in the voiceLines array of the voiceline to play</param>
    /// <param name="timePos">The playback time</param>
    public void setVoiceLine(int index, float timePos = 0)
    {
        if (index >= 0)
        {
            currentVoiceLineIndex = index;
            source.clip = voiceLines[index].voiceLine;
            source.time = timePos;
            if (!source.isPlaying)
            {
                source.Play();
            }
        }
        else
        {
            if (source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}
