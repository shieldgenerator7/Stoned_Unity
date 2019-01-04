﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NPCManager : MonoBehaviour
{

    public GameObject npcTalkEffect;//the particle system for the visual part of NPC talking
    private static GameObject lastTalkingNPC;//the last NPC to talk
    public Text npcDialogueText;
    public Canvas canvas;
    public GameObject npcQuoteBox;
    public GameObject npcQuoteBoxTail;

    private static NPCManager instance;
    private static MusicManager musicManager;

    // Use this for initialization
    void Start()
    {
        //instance
        if (instance == null)
        {
            instance = this;

            npcDialogueText.fontSize = (int)(Camera.main.pixelHeight * 0.05f);
            musicManager = FindObjectOfType<MusicManager>();
            if (!instance.npcTalkEffect.GetComponent<ParticleSystem>().isPlaying)
            {
                instance.canvas.gameObject.SetActive(false);
                instance.enabled = false;
            }
        }
        else
        {
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Camera cam = Camera.main;
        RectTransform canTrans = ((RectTransform)canvas.transform);
        canTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Camera.main.pixelWidth);
        canTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Camera.main.pixelHeight);
        Vector2 size = cam.ScreenToWorldPoint(new Vector2(cam.pixelWidth, cam.pixelHeight)) - cam.ScreenToWorldPoint(Vector2.zero);
        float newDim = Mathf.Max(Mathf.Abs(size.x) / canTrans.rect.width, Mathf.Abs(size.y) / canTrans.rect.height);
        Vector3 newSize = new Vector3(newDim, newDim, 1);
        canvas.transform.localScale = newSize;
        ((RectTransform)npcDialogueText.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Camera.main.pixelWidth * 3 / 4);
        ((RectTransform)npcDialogueText.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Camera.main.pixelHeight * 3 / 4);
        canvas.transform.rotation = cam.transform.rotation;
    }

    /// <summary>
    /// Activates the visual effects for the given npc talking
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="talking">Whether to activate or deactivate the visual effects</param>
    public static void speakNPC(GameObject npc, bool talking, string message)
    {
        instance.canvas.gameObject.SetActive(talking);
        instance.npcDialogueText.text = message;
        instance.enabled = talking;
        if (talking)
        {
            instance.npcTalkEffect.transform.position = npc.transform.position;
            //Show text
            float textWidth = getTextWidth(instance.canvas, instance.npcDialogueText, message.Length);
            float textHeight = getTextHeight(instance.canvas, instance.npcDialogueText);
            float textBoxWidth = instance.npcDialogueText.rectTransform.rect.width * instance.canvas.transform.localScale.x;
            float textBoxHeight = textHeight;
            float buffer = textHeight / 2;
            float maxWidth = Mathf.Min(Screen.width / 2, textBoxWidth);
            if (textWidth > maxWidth)
            {
                float scalar = Mathf.Ceil(textWidth / maxWidth);
                string spacedString = message;
                int segmentLength = (int)(message.Length / scalar);
                int maxSegmentLength = 0;
                int spaceIndex = 0;
                for (int i = 1; i < scalar; i++)
                {
                    int searchStartIndex = i * segmentLength;
                    int prevSpaceIndex = spaceIndex;
                    spaceIndex = searchStartIndex;
                    for (int j = 0; j < 5; j++)
                    {
                        int lowerIndex = Mathf.Max(0, searchStartIndex - j);
                        if (spacedString.Substring(lowerIndex, 1) == " ")
                        {
                            spaceIndex = lowerIndex;
                            break;
                        }
                        int higherIndex = Mathf.Min(spacedString.Length - 1, searchStartIndex + j);
                        if (spacedString.Substring(higherIndex, 1) == " ")
                        {
                            spaceIndex = higherIndex;
                            break;
                        }
                    }
                    //
                    if (spaceIndex - prevSpaceIndex > maxSegmentLength)
                    {
                        maxSegmentLength = spaceIndex - prevSpaceIndex;
                    }
                    //Insert new line
                    spacedString = spacedString.Insert(spaceIndex, "\n");
                }
                instance.npcDialogueText.text = spacedString;
                textWidth = textBoxWidth = getTextWidth(instance.canvas, instance.npcDialogueText, maxSegmentLength);
                textBoxHeight *= scalar;
            }
            textBoxWidth = textWidth;
            textBoxWidth += buffer * 2;
            textBoxHeight += buffer * 2;
            instance.canvas.transform.position = npc.transform.position + Camera.main.transform.up.normalized * (textHeight * 3 + npc.GetComponent<SpriteRenderer>().bounds.extents.y);
            //Show quote box
            instance.npcQuoteBox.transform.position = instance.canvas.transform.position;
            SpriteRenderer quoteSR = instance.npcQuoteBox.GetComponent<SpriteRenderer>();
            quoteSR.size = new Vector2(textBoxWidth, textBoxHeight);
            instance.npcQuoteBoxTail.transform.position = instance.npcQuoteBox.transform.position - (Vector3.up * quoteSR.size.y / 2);
            //Show speaking particles
            if (!instance.npcTalkEffect.GetComponent<ParticleSystem>().isPlaying)
            {
                instance.npcTalkEffect.GetComponent<ParticleSystem>().Play();
            }
            if (lastTalkingNPC != npc)
            {
                lastTalkingNPC = npc;
                musicManager.setQuiet(true);
            }
        }
        else
        {
            if (npc == lastTalkingNPC)
            {
                musicManager.setQuiet(false);
                instance.npcTalkEffect.GetComponent<ParticleSystem>().Stop();
            }
        }
    }

    static float getTextWidth(Canvas canvas, Text text, int length)
    {
        return text.fontSize * 0.5f * length * canvas.transform.localScale.x;
    }
    static float getTextHeight(Canvas canvas, Text text)
    {
        return text.fontSize * canvas.transform.localScale.y;
    }
}
