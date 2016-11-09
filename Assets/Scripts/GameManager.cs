﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public bool save = false;
    public bool load = false;
    public int chosenId = 0;
    public int amount = 0;
    public GameObject playerGhost;//this is to show Merky in the past
    private int rewindId = 0;//the id to eventually load back to
    private List<GameState> gameStates = new List<GameState>();
    private List<GameObject> gameObjects = new List<GameObject>();

    private static GameManager instance;
    private CameraController camCtr;
    private float actionTime = Time.time;//used to determine how often to rewind
    private const float rewindDelay = 0.05f;//how much to delay each rewind transition by

    // Use this for initialization
    void Start()
    {
        if (ES2.Exists("merky.txt"))
        {
            loadFromFile();
            chosenId = rewindId = gameStates.Count - 1;
            Load(chosenId);
            CameraController cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraController>();
            cam.pinPoint();
            cam.recenter();
            cam.refocus();
        }

        refreshGameObjects();

        

        camCtr = FindObjectOfType<CameraController>();
    }
    public void addAll(List<GameObject> list)
    {
        foreach (GameObject go in list)
        {
            gameObjects.Add(go);
        }
    }

     // Update is called once per frame
    void Update()
    {
        if (save == true)
        {
            save = false;
            Save();
        }
        if (load == true)
        {
            load = false;
            Load(chosenId);
        }
        if (chosenId > rewindId)
        {
            if (Time.time > actionTime)
            {
                actionTime = Time.time + rewindDelay;
                Load(chosenId - 1);
            }
        }
    }

    public void refreshGameObjects()
    {
        gameObjects = new List<GameObject>();
        foreach (Rigidbody2D rb in FindObjectsOfType<Rigidbody2D>())
        {
            gameObjects.Add(rb.gameObject);
        }
    }

    public void Save()
    {
        gameStates.Add(new GameState(gameObjects));
        amount++;
        chosenId++;
        rewindId++;
    }
    public void Load(int gamestateId)
    {
        chosenId = gamestateId;
        gameStates[gamestateId].load();
        for (int i = gameStates.Count - 1; i > gamestateId ; i--)
        {
            Destroy(gameStates[i].representation);
            gameStates.RemoveAt(i);
        }
        GameState.nextid = gamestateId + 1;
    }
    void Rewind(int gamestateId)//rewinds one state at a time
    {
        rewindId = gamestateId;
    }
    public void saveToFile()
    {
        ES2.Save(gameStates, "merky.txt?tag=states");
    }
    public void loadFromFile()
    {
        gameStates = ES2.LoadList<GameState>("merky.txt?tag=states");
    }
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            refreshGameObjects();
            Destroy(gameObject);
        }
        SceneManager.LoadScene(1, LoadSceneMode.Additive);//load the SceneLoaderTrigger scene
    }
    void OnApplicationQuit()
    {
        Save();
        saveToFile();
    }

    public void showPlayerGhosts()
    {
        foreach (GameState gs in gameStates)
        {
            gs.showRepresentation(playerGhost);
        }
    }
    public void hidePlayerGhosts()
    {
        foreach (GameState gs in gameStates)
        {
            gs.hideRepresentation();
        }
    }
    public void processTapGesture(Vector3 curMPWorld)
    {
        Debug.Log("GameManager.pTG: curMPWorld: " + curMPWorld);
        GameState final = null;
        foreach (GameState gs in gameStates)
        {
            if (gs.checkRepresentation(curMPWorld))
            {
                if (final == null || gs.id > final.id)//assuming the later ones have higher id values
                {
                    final = gs;//keep the latest one
                }
            }
        }
        if (final != null)
        {
            hidePlayerGhosts();
            if (final.id == chosenId)
            {
                Load(final.id);
            }
            else {
                Rewind(final.id);
            }
            camCtr.adjustScalePoint(-1);
            camCtr.recenter();
            camCtr.refocus();
        }
    }
}


