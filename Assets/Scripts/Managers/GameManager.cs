﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    //
    // Settings
    //
    [Header("Settings")]
    [SerializeField]
    private float respawnDelay = 1.0f;//how long Merky must wait before rewinding after shattering
    [SerializeField]
    private float rewindDelay = 0.05f;//how much to delay each rewind transition by

    [Header("Objects")]
    public GameObject playerGhostPrefab;//this is to show Merky in the past (prefab)
    [SerializeField]
    private List<SceneLoader> sceneLoaders = new List<SceneLoader>();

    [Header("Demo Mode")]
    [SerializeField]
    private bool demoBuild = false;//true to not load on open or save with date/timestamp in filename
    [SerializeField]
    private float restartDemoDelay = 10;//how many seconds before the game can reset after the demo ends
    [SerializeField]
    private Text txtDemoTimer;//the text that shows much time is left in the demo
    [SerializeField]
    private GameObject endDemoScreen;//the picture to show the player after the game resets

    //
    // Runtime variables
    //
    private int rewindId;//the id to eventually load back to
    private int chosenId;
    private float lastRewindTime;//used to determine how often to rewind
    private float respawnTime;//the earliest time Merky can rewind after shattering
    private int loadedSceneCount;
    private string unloadedScene = null;
    private float resetGameTimer;//the time that the game will reset at
    private static float gamePlayTime;//how long the game can be played for, 0 for indefinitely

    //
    // Runtime Lists
    //
    //Game States
    private List<GameState> gameStates = new List<GameState>();
    private List<GameObject> gameObjects = new List<GameObject>();
    private List<GameObject> forgottenObjects = new List<GameObject>();//a list of objects that are inactive and thus unfindable
    //Scene Loading
    private List<string> openScenes = new List<string>();//the list of names of the scenes that are open
    private List<string> newlyLoadedScenes = new List<string>();
    //Memories
    private List<MemoryObject> memories = new List<MemoryObject>();
    //Checkpoints
    private List<CheckPointChecker> activeCheckPoints = new List<CheckPointChecker>();

    // Use this for initialization
    void Start()
    {
        CameraController camCtr = Managers.Camera;
        camCtr.pinPoint();
        camCtr.recenter();
        camCtr.refocus();
        chosenId = -1;
        //If a limit has been set on the demo playtime
        if (GameDemoLength > 0)
        {
            demoBuild = true;//auto enable demo build mode
            Managers.Gesture.tapGesture += startDemoTimer;
            txtDemoTimer.transform.parent.gameObject.SetActive(true);
        }
        if (!demoBuild && ES2.Exists("merky.txt"))
        {
            loadFromFile();
            chosenId = rewindId = gameStates.Count - 1;
            Load(chosenId);
            LoadMemories();
        }

        refreshGameObjects();
        SceneManager.sceneLoaded += sceneLoaded;
        SceneManager.sceneUnloaded += sceneUnloaded;
    }

    /// <summary>
    /// Resets the game back to the very beginning
    /// </summary>
    public void resetGame(bool savePrevGame = true)
    {
        //Save previous game
        if (savePrevGame)
        {
            Save();
            saveToFile();
        }
        //Unload all scenes and reload PlayerScene
        GameState.nextid = 0;
        SceneManager.LoadScene(0);
    }
    public static float GameDemoLength
    {
        get
        {
            return gamePlayTime;
        }
        set
        {
            gamePlayTime = Mathf.Max(value, 0);
        }
    }
    void startDemoTimer()
    {
        if (Managers.Camera.ZoomLevel > Managers.Camera.scalePointToZoomLevel((int)CameraController.CameraScalePoints.PORTRAIT))
        {
            resetGameTimer = GameDemoLength + Time.time;
            Managers.Gesture.tapGesture -= startDemoTimer;
        }
    }

    private void showEndDemoScreen(bool show)
    {
        endDemoScreen.SetActive(show);
        if (show)
        {
            endDemoScreen.transform.position = (Vector2)Camera.main.transform.position;
            endDemoScreen.transform.localRotation = Camera.main.transform.localRotation;
        }
    }

    public static void addObject(GameObject go)
    {
        Managers.Game.gameObjects.Add(go);
    }
    public void addAll(List<GameObject> list)
    {
        foreach (GameObject go in list)
        {
            gameObjects.Add(go);
        }
    }
    /// <summary>
    /// Destroys the given GameObject and updates lists
    /// </summary>
    /// <param name="go"></param>
    public static void destroyObject(GameObject go)
    {
        Managers.Game.removeObject(go);
        Destroy(go);
    }
    /// <summary>
    /// Removes the given GameObject from the gameObjects list
    /// </summary>
    /// <param name="go"></param>
    private void removeObject(GameObject go)
    {
        gameObjects.Remove(go);
        forgottenObjects.Remove(go);
        if (go && go.transform.childCount > 0)
        {
            foreach (Transform t in go.transform)
            {
                gameObjects.Remove(t.gameObject);
                forgottenObjects.Remove(t.gameObject);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (SceneLoader sl in sceneLoaders)
        {
            sl.check();
        }
        if (newlyLoadedScenes.Count > 0)
        {
            refreshGameObjects();
            foreach (string s in newlyLoadedScenes)
            {
                LoadObjectsFromScene(SceneManager.GetSceneByName(s));
                loadedSceneCount++;
            }
            newlyLoadedScenes.Clear();
        }
        if (unloadedScene != null)
        {
            refreshGameObjects();
            unloadedScene = null;
        }
        if (gameStates.Count == 0 && loadedSceneCount > 0)
        {
            Save();
        }
        if (GameDemoLength > 0)
        {
            float timeLeft = 0;
            if (resetGameTimer > 0)
            {
                if (Time.time >= resetGameTimer)
                {
                    showEndDemoScreen(true);
                    //If the buffer period has ended,
                    if (Time.time >= resetGameTimer + restartDemoDelay)
                    {
                        //And user has given input,
                        if (Input.GetMouseButton(0)
                            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
                            )
                        {
                            //Reset game
                            showEndDemoScreen(false);
                            resetGame();
                        }
                    }
                }
                else
                {
                    timeLeft = resetGameTimer - Time.time;
                }
            }
            else
            {
                timeLeft = GameDemoLength;
            }
            txtDemoTimer.text = string.Format("{0:0.00}", timeLeft);
        }
    }

    private void FixedUpdate()
    {
        if (Rewinding)
        {
            if (Time.time > lastRewindTime)
            {
                lastRewindTime = Time.time + rewindDelay;
                Load(chosenId - 1);
            }
        }
    }

    void sceneLoaded(Scene s, LoadSceneMode m)
    {
        refreshGameObjects();
        newlyLoadedScenes.Add(s.name);
        openScenes.Add(s.name);
    }
    void sceneUnloaded(Scene s)
    {
        foreach (GameObject fgo in forgottenObjects)
        {
            if (fgo != null && fgo.scene == s)
            {
                forgottenObjects.Remove(fgo);
            }
        }
        refreshGameObjects();
        unloadedScene = s.name;
        openScenes.Remove(s.name);
        loadedSceneCount--;
    }
    public static void refresh() { Managers.Game.refreshGameObjects(); }
    public void refreshGameObjects()
    {
        gameObjects = new List<GameObject>();
        foreach (Rigidbody2D rb in FindObjectsOfType<Rigidbody2D>())
        {
            gameObjects.Add(rb.gameObject);
        }
        foreach (SavableMonoBehaviour smb in FindObjectsOfType<SavableMonoBehaviour>())
        {
            if (!gameObjects.Contains(smb.gameObject))
            {
                gameObjects.Add(smb.gameObject);
            }
        }
        //Forgotten Objects
        foreach (GameObject dgo in forgottenObjects)
        {
            if (dgo != null)
            {
                gameObjects.Add(dgo);
            }
        }
        foreach (MemoryMonoBehaviour mmb in FindObjectsOfType<MemoryMonoBehaviour>())
        {
            //load state if found, save state if not foud
            bool foundMO = false;
            foreach (MemoryObject mo in memories)
            {
                if (mo.isFor(mmb))
                {
                    foundMO = true;
                    mmb.acceptMemoryObject(mo);
                    break;
                }
            }
            if (!foundMO)
            {
                memories.Add(mmb.getMemoryObject());
            }
        }
    }
    public void Save()
    {
        gameStates.Add(new GameState(gameObjects));
        chosenId++;
        rewindId++;
        //Open Scenes
        foreach (SceneLoader sl in sceneLoaders)
        {
            if (openScenes.Contains(sl.sceneName))
            {
                if (sl.firstOpenGameStateId > chosenId)
                {
                    sl.firstOpenGameStateId = chosenId;
                }
                sl.lastOpenGameStateId = chosenId;
            }
        }
    }
    public void saveMemory(MemoryMonoBehaviour mmb)
    {
        bool foundMO = false;
        foreach (MemoryObject mo in memories)
        {
            if (mo.isFor(mmb))
            {
                foundMO = true;
                mo.found = mmb.getMemoryObject().found;
                break;
            }
        }
        if (!foundMO)
        {
            memories.Add(mmb.getMemoryObject());
        }
    }
    public void saveCheckPoint(CheckPointChecker cpc)//checkpoints have to work across levels, so they need to be saved separately
    {
        if (!activeCheckPoints.Contains(cpc))
        {
            activeCheckPoints.Add(cpc);
        }
    }
    /// <summary>
    /// Stores the given object before it gets set inactive
    /// </summary>
    /// <param name="obj"></param>
    public void saveForgottenObject(GameObject obj, bool forget = true)
    {
        if (forget)
        {
            forgottenObjects.Add(obj);
            obj.SetActive(false);
        }
        else
        {
            forgottenObjects.Remove(obj);
            obj.SetActive(true);
        }
    }
    public List<GameObject> ForgottenObjects
    {
        get
        {
            return forgottenObjects;
        }
    }
    public void Load(int gamestateId)
    {
        //Destroy objects not spawned yet in the new selected state
        //chosenId is the previous current gamestate, which is in the future compared to gamestateId
        for (int i = gameObjects.Count - 1; i > 0; i--)
        {
            GameObject go = gameObjects[i];
            if (!gameStates[gamestateId].hasGameObject(go))
            {
                if (go == null)
                {
                    destroyObject(go);
                    continue;
                }
                foreach (SavableMonoBehaviour smb in go.GetComponents<SavableMonoBehaviour>())
                {
                    if (smb.isSpawnedObject())
                    {
                        destroyObject(go);//remove it from game objects list
                    }
                }
            }
        }
        //
        chosenId = gamestateId;
        if (chosenId == rewindId)
        {
            //After rewind is finished, refresh the game object list
            refreshGameObjects();
            Managers.Music.SongSpeed = Managers.Music.normalSongSpeed;
            //Open Scenes
            foreach (SceneLoader sl in sceneLoaders)
            {
                if (sl.lastOpenGameStateId > chosenId)
                {
                    sl.lastOpenGameStateId = chosenId;
                }
                if (sl.firstOpenGameStateId > chosenId)
                {
                    sl.firstOpenGameStateId = int.MaxValue;
                    sl.lastOpenGameStateId = -1;
                }
            }
        }
        gameStates[gamestateId].load();
        if (chosenId <= rewindId)
        {
            refreshGameObjects();//a second time, just to be sure
        }
        for (int i = gameStates.Count - 1; i > gamestateId; i--)
        {
            Destroy(gameStates[i].Representation);
            gameStates.RemoveAt(i);
        }
        GameState.nextid = gamestateId + 1;
        //Recenter the camera
        Managers.Camera.recenter();
        Managers.Camera.refocus();
    }
    public void LoadObjectsFromScene(Scene s)
    {
        //Find the last state that this scene was saved in
        int lastStateSeen = -1;
        foreach (SceneLoader sl in sceneLoaders)
        {
            if (s.name == sl.sceneName)
            {
                lastStateSeen = sl.lastOpenGameStateId;
                break;
            }
        }
        if (lastStateSeen < 0)
        {
            return;
        }
        if (lastStateSeen > chosenId)
        {
            lastStateSeen = chosenId;
        }
        //Load Each Object
        foreach (GameObject go in gameObjects)
        {
            if (go.scene.Equals(s))
            {
                for (int stateid = lastStateSeen; stateid >= 0; stateid--)
                {
                    if (gameStates[stateid].loadObject(go))
                    {
                        break;
                    }
                    else
                    {
                        //continue until you find the game state that has the most recent information about this object
                    }
                }
            }
        }
    }
    public bool Rewinding
    {
        get
        {
            return chosenId > rewindId;
        }
    }
    public void cancelRewind()
    {
        rewindId = chosenId;
        Load(chosenId);
        Managers.Music.SongSpeed = Managers.Music.normalSongSpeed;
    }
    public void RewindToStart()
    {
        Rewind(0);
    }
    /// <summary>
    /// Sets into motion the rewind state.
    /// FixedUpdate carries out the motions of calling Load()
    /// </summary>
    /// <param name="gamestateId"></param>
    void Rewind(int gamestateId)
    {
        Managers.Music.SongSpeed = Managers.Music.rewindSongSpeed;
        rewindId = gamestateId;
        Managers.Camera.recenter();
    }
    void LoadMemories()
    {
        foreach (MemoryObject mo in memories)
        {
            GameObject go = mo.findGameObject();
            if (go != null)
            {
                MemoryMonoBehaviour mmb = go.GetComponent<MemoryMonoBehaviour>();
                if (mo.isFor(mmb))
                {
                    mmb.acceptMemoryObject(mo);
                }
            }
        }
    }
    public void saveToFile()
    {
        string fileName = "merky";
        if (demoBuild)
        {
            System.DateTime now = System.DateTime.Now;
            fileName += "-" + now.Ticks;
        }
        fileName += ".txt";
        ES2.Save(memories, fileName + "?tag=memories");
        ES2.Save(gameStates, fileName + "?tag=states");
        ES2.Save(sceneLoaders, fileName + "?tag=scenes");
    }
    public void loadFromFile()
    {
        memories = ES2.LoadList<MemoryObject>("merky.txt?tag=memories");
        gameStates = ES2.LoadList<GameState>("merky.txt?tag=states");
        //Scenes
        List<SceneLoader> rsls = ES2.LoadList<SceneLoader>("merky.txt?tag=scenes");
        foreach (SceneLoader sl in sceneLoaders)//all scene loaders
        {
            foreach (SceneLoader rsl in rsls)//read in scene loaders
            {
                if (rsl != null && sl.sceneName == rsl.sceneName && rsl != sl)
                {
                    sl.lastOpenGameStateId = rsl.lastOpenGameStateId;
                    Destroy(rsl);
                    break;
                }
            }
        }
    }
    void OnApplicationQuit()
    {
        Save();
        saveToFile();
    }

    public List<CheckPointChecker> ActiveCheckPoints
    {
        get
        {
            return activeCheckPoints;
        }
    }

    public int CurrentStateId
    {
        get
        {
            return chosenId;
        }
    }

    /// <summary>
    /// Called when Merky gets shattered
    /// </summary>
    public void playerShattered()
    {
        respawnTime = Time.time + respawnDelay;
    }

    public void showPlayerGhosts()
    {
        bool intact = Managers.Player.HardMaterial.isIntact();
        foreach (GameState gs in gameStates)
        {
            if (intact || gs.id != chosenId)
            {//don't include last game state if merky is shattered
                gs.showRepresentation(chosenId);
            }
        }
    }
    public void hidePlayerGhosts()
    {
        bool intact = Managers.Player.HardMaterial.isIntact();
        foreach (GameState gs in gameStates)
        {
            if (intact || gs.id != chosenId)
            {//don't include last game state if merky is shattered
                gs.hideRepresentation();
            }
        }
    }
    /// <summary>
    /// Returns the player ghost that is closest to the given position
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public GameObject getClosestPlayerGhost(Vector2 pos)
    {
        float closestDistance = float.MaxValue;
        GameObject closestObject = null;
        foreach (GameState gs in gameStates)
        {
            Vector2 gsPos = gs.Representation.transform.position;
            float gsDistance = Vector2.Distance(gsPos, pos);
            if (gsDistance < closestDistance)
            {
                closestDistance = gsDistance;
                closestObject = gs.Representation;
            }
        }
        return closestObject;
    }

    public void processTapGesture(Vector3 curMPWorld)
    {
        Debug.Log("GameManager.pTG: curMPWorld: " + curMPWorld);
        if (respawnTime > Time.time)
        {
            //If respawn timer is not over, don't do anything
            return;
        }
        GameState final = null;
        GameState prevFinal = null;
        bool intact = Managers.Player.HardMaterial.isIntact();
        //Sprite detection pass
        foreach (GameState gs in gameStates)
        {
            //don't include last game state if merky is shattered
            if (intact || gs.id != chosenId)
            {
                //Check sprite overlap
                if (gs.checkRepresentation(curMPWorld))
                {
                    if (final == null || gs.id > final.id)//assuming the later ones have higher id values
                    {
                        prevFinal = final;//keep the second-to-latest one
                        final = gs;//keep the latest one
                    }
                }
            }
        }
        //Collider detection pass
        if (final == null)
        {
            foreach (GameState gs in gameStates)
            {
                //don't include last game state if merky is shattered
                if (intact || gs.id != chosenId)
                {
                    //Check collider overlap
                    if (gs.checkRepresentation(curMPWorld, false))
                    {
                        if (final == null || gs.id > final.id)//assuming the later ones have higher id values
                        {
                            prevFinal = final;//keep the second-to-latest one
                            final = gs;//keep the latest one
                        }
                    }
                }
            }
        }
        //Process tapped game state
        if (final != null)
        {
            if (final.id == chosenId)
            {
                if (prevFinal != null)
                {//if the current one overlaps a previous one, choose the previous one
                    Rewind(prevFinal.id);
                }
                else
                {
                    Load(final.id);
                }
            }
            else
            {
                Rewind(final.id);
            }
        }
        else if (!intact)
        {
            Rewind(chosenId - 1);//go back to the latest safe past merky
        }
        if (GameStatistics.counter("deathCount") == 1)
        {
            Managers.Effect.highlightTapArea(Vector2.zero, false);
        }

        //leave this zoom level even if no past merky was chosen
        float defaultZoomLevel = Managers.Camera.scalePointToZoomLevel((int)CameraController.CameraScalePoints.DEFAULT);
        Managers.Camera.ZoomLevel = defaultZoomLevel;
        Managers.Gesture.switchGestureProfile("Main");

        if (tapProcessed != null)
        {
            tapProcessed(curMPWorld);
        }
    }
    public delegate void TapProcessed(Vector2 curMPWorld);
    public TapProcessed tapProcessed;

    /// <summary>
    /// Used specifically to highlight last saved Merky after the first death
    /// for tutorial purposes
    /// </summary>
    /// <returns></returns>
    public Vector2 getLatestSafeRewindGhostPosition()
    {
        return gameStates[chosenId - 1].merky.position;
    }
}


