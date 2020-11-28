﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// GameManager in charge of Time and Space
/// </summary>
public class GameManager : MonoBehaviour
{
    //
    // Settings
    //
    [Header("Settings")]
    [SerializeField]
    private float baseRewindDelay = 0.05f;
    [SerializeField]
    private float rewindDelay = 0.05f;//the delay between rewind transitions
    [SerializeField]
    private float minRewindDuration = 1;//how many seconds a rewind should last for

    [Header("Objects")]
    public GameObject playerGhostPrefab;//this is to show Merky in the past (prefab)
    [SerializeField]
    private List<SceneLoader> sceneLoaders = new List<SceneLoader>();

    [Header("Demo Mode")]
    [SerializeField]
    private bool demoBuild = false;//true to not load on open or save with date/timestamp in filename
    [SerializeField]
    private bool saveWithTimeStamp = false;//true to save with date/timestamp in filename, even when not in demo build
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
    private int chosenId;//the id of the current game state
    private float lastRewindTime;//the last time the game rewound
    private float resetGameTimer;//the time that the game will reset at
    private float gamePlayTime;//how long the game can be played for, 0 for indefinitely

    public bool rewindInterruptableByPlayer = true;

    private string pauseForLoadingSceneName = null;//the name of the scene that needs the game to pause while it's loading
    public string PauseForLoadingSceneName
    {
        get => pauseForLoadingSceneName;
        set
        {
            pauseForLoadingSceneName = value;
            if (pauseForLoadingSceneName == null || pauseForLoadingSceneName == "")
            {
                //Resume if the scene is done loading
                Managers.Time.setPause(this, false);
            }
            else
            {
                //Pause if the scene is still loading
                Managers.Time.setPause(this, true);
            }
        }
    }

    //
    // Runtime Lists
    //
    //Game States
    private List<GameState> gameStates = new List<GameState>();//basically a timeline
    private Dictionary<string, GameObject> gameObjects = new Dictionary<string, GameObject>();//list of current objects that have state to save
    private List<GameObject> forgottenObjects = new List<GameObject>();//a list of objects that are inactive and thus unfindable, but still have state to save
    public List<GameObject> ForgottenObjects
    {
        get { return forgottenObjects; }
    }
    //Scene Loading
    private List<Scene> openScenes = new List<Scene>();//the list of the scenes that are open
    public bool playerSceneLoaded { get; private set; } = false;
    //Memories
    private Dictionary<string, MemoryObject> memories = new Dictionary<string, MemoryObject>();//memories that once turned on, don't get turned off

    // Use this for initialization
    void Start()
    {
        //Initialize the current game state id
        //There are possibly none, so the default "current" is -1
        chosenId = -1;
        //If a limit has been set on the demo playtime,
        if (GameDemoLength > 0)
        {
            //Auto-enable demo mode
            demoBuild = true;
            //Tell the gesture manager to start the timer when the player taps in game
            //Managers.Gesture.tapGesture += startDemoTimer;
            //Show the timer
            txtDemoTimer.transform.parent.gameObject.SetActive(true);
        }
        //If in demo mode,
        if (demoBuild)
        {
            //Save its future files with a time stamp
            saveWithTimeStamp = true;
        }
#if UNITY_EDITOR
        //Add list of already open scenes to open scene list (for editor)
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            openScenes.Add(SceneManager.GetSceneAt(i));
        }
#endif
        //Check to see which levels need loaded
        checkScenes();
        //Update the list of objects that have state to save
        refreshGameObjects();
        //If it's not in demo mode, and its save file exists,
        if (!demoBuild && ES3.FileExists("merky.txt"))
        {
            //Load the save file
            loadFromFile();
            //Update the game state id trackers
            chosenId = rewindId = gameStates.Count - 1;
            //Load the most recent game state
            Load(chosenId);
            //Load the memories
            LoadMemories();
        }
        //Register scene loading delegates
        SceneManager.sceneLoaded += sceneLoaded;
        SceneManager.sceneUnloaded += sceneUnloaded;
    }

    // Update is called once per frame
    void Update()
    {
        //Check all the scene loaders
        //to see if their scene needs loaded or unloaded
        //(done this way because standard trigger methods in Unity
        //don't always play nice with teleporting characters)
        if (!Rewinding)
        {
            checkScenes();
        }
        //If the time is rewinding,
        if (Rewinding)
        {
            //And it's time to rewind the next step,
            if (Time.unscaledTime > lastRewindTime + rewindDelay)
            {
                //Rewind to the next previous game state
                lastRewindTime = Time.unscaledTime;
                Load(chosenId - 1);
            }
        }
        //If in demo mode,
        if (GameDemoLength > 0)
        {
            float timeLeft = 0;
            //And the timer has started,
            if (resetGameTimer > 0)
            {
                //If the timer has stopped,
                if (Time.time >= resetGameTimer)
                {
                    //Show the end demo screen
                    showEndDemoScreen(true);
                    //If the ignore-input buffer period has ended,
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
                //Else if the timer is ticking,
                else
                {
                    //Show the time remaining
                    timeLeft = resetGameTimer - Time.time;
                }
            }
            //Else if the timer has not started,
            else
            {
                //Show the max play time of the demo
                timeLeft = GameDemoLength;
            }
            //Update the timer on screen
            txtDemoTimer.text = string.Format("{0:0.00}", timeLeft);
        }
    }

    #region GameObject List Management

    /// <summary>
    /// Adds an object to list of objects that have state to save
    /// </summary>
    /// <param name="go">The GameObject to add to the list</param>
    public void addObject(GameObject go)
    {
        //
        //Error checking
        //

        //If go is null
        if (go == null)
        {
            throw new System.ArgumentNullException("GameObject (" + go + ") cannot be null!");
        }

        //getKey() returns a string containing
        //the object's name and scene name
        string key = go.getKey();

        //If the game object's name is already in the dictionary...
        if (gameObjects.ContainsKey(key))
        {
            throw new System.ArgumentException(
                  "GameObject (" + key + ") is already inside the gameObjects dictionary! "
                  + "Check for 2 or more objects with the same name."
                  );
        }
        //If the game object doesn't have any state to save...
        if (!go.isSavable())
        {
            throw new System.ArgumentException(
                "GameObject (" + key + ") doesn't have any state to save! "
                + "Check to make sure it has a Rigidbody2D or a SavableMonoBehaviour."
                );
        }
        //Else if all good, add the object
        gameObjects.Add(key, go);
    }

    /// <summary>
    /// Retrieves the GameObject from the gameObjects list with the given scene and object names
    /// </summary>
    /// <param name="sceneName">The scene name of the object</param>
    /// <param name="objectName">The name of the object</param>
    /// <returns></returns>
    public GameObject getObject(string sceneName, string objectName)
    {
        string key = Utility.getKey(sceneName, objectName);
        //If the gameObjects list has the game object,
        if (gameObjects.ContainsKey(key))
        {
            //Return it
            return gameObjects[key];
        }
        //Otherwise, sorry, you're out of luck
        return null;
    }

    public List<GameObject> getObjectsWithName(string startsWith)
    {
        List<GameObject> matchingGOs = new List<GameObject>();
        //Search for GameObjects that start with the given string
        foreach (GameObject go in gameObjects.Values)
        {
            if (go.name.StartsWith(startsWith))
            {
                matchingGOs.Add(go);
            }
        }
        return matchingGOs;
    }
    /// <summary>
    /// Destroys the given GameObject and updates lists
    /// </summary>
    /// <param name="go">The GameObject to destroy</param>
    public void destroyObject(GameObject go)
    {
        removeObject(go);
        Destroy(go);
    }
    /// <summary>
    /// Removes the given GameObject from the gameObjects list
    /// </summary>
    /// <param name="go">The GameObject to remove from the list</param>
    private void removeObject(GameObject go)
    {
        gameObjects.Remove(go.getKey());
        forgottenObjects.Remove(go);
        //If go is not null and has children,
        if (go && go.transform.childCount > 0)
        {
            //For each of its children,
            foreach (Transform t in go.transform)
            {
                //Remove it from the gameObjects list
                gameObjects.Remove(t.gameObject.getKey());
                //And from the forgotten objects list
                forgottenObjects.Remove(t.gameObject);
            }
        }
    }
    /// <summary>
    /// Remove null objects from the gameObjects list
    /// </summary>
    private void cleanObjects()
    {
        string cleanedKeys = "";
        //Copy the game object keys
        List<string> keys = new List<string>(gameObjects.Keys);
        //Loop over copy list
        foreach (string key in keys)
        {
            //If the key's value is null,
            if (gameObjects[key] == null)
            {
                //Clean the key out
                cleanedKeys += key + ", ";
                gameObjects.Remove(key);
            }
        }
        //Write out to the console which keys were cleaned
        if (cleanedKeys != "")
        {
            Debug.LogError("Cleaned: " + cleanedKeys);
        }
    }

    /// <summary>
    /// Update the list of GameObjects with state to save
    /// </summary>
    public void refreshGameObjects()
    {
        //Make a new dictionary for the list
        gameObjects = new Dictionary<string, GameObject>();
        //Add objects that can move
        foreach (Rigidbody2D rb in FindObjectsOfType<Rigidbody2D>())
        {
            addObject(rb.gameObject);
        }
        //Add objects that have other variables that can get rewound
        foreach (SavableMonoBehaviour smb in FindObjectsOfType<SavableMonoBehaviour>())
        {
            if (!gameObjects.ContainsValue(smb.gameObject))
            {
                addObject(smb.gameObject);
            }
        }
        //Forgotten Objects
        foreach (GameObject fgo in forgottenObjects)
        {
            if (fgo != null)
            {
                addObject(fgo);
            }
        }
        //Memories
        foreach (MemoryMonoBehaviour mmb in FindObjectsOfType<MemoryMonoBehaviour>())
        {
            string key = mmb.gameObject.getKey();
            //If the memory has already been stored,
            if (memories.ContainsKey(key))
            {
                //Load the memory
                mmb.acceptMemoryObject(memories[key]);
            }
            //Else
            else
            {
                //Save the memory
                memories.Add(key, mmb.getMemoryObject());
            }
        }
    }

    /// <summary>
    /// Stores the given object before it gets set inactive
    /// </summary>
    /// <param name="obj"></param>
    public void saveForgottenObject(GameObject obj, bool forget = true)
    {
        //Error checking
        if (obj == null)
        {
            throw new System.ArgumentNullException("GameManager.saveForgottenObject() cannot accept null for obj! obj: " + obj);
        }
        //If it's about to be set inactive,
        if (forget)
        {
            //Add it to the list,
            forgottenObjects.Add(obj);
            //And set it inactive
            obj.SetActive(false);
        }
        //Else,
        else
        {
            //Remove it from the list,
            forgottenObjects.Remove(obj);
            //And set it active again
            obj.SetActive(true);
        }
    }
    #endregion

    #region Memory List Management
    /// <summary>
    /// Saves the memory to the memory list
    /// </summary>
    /// <param name="mmb"></param>
    public void saveMemory(MemoryMonoBehaviour mmb)
    {
        string key = mmb.gameObject.getKey();
        MemoryObject mo = mmb.getMemoryObject();
        //If the memory is already stored,
        if (memories.ContainsKey(key))
        {
            //Update it
            memories[key] = mo;
        }
        //Else
        else
        {
            //Add it
            memories.Add(key, mo);
        }
    }
    /// <summary>
    /// Restore all saved memories of game objects that have a memory saved
    /// </summary>
    void LoadMemories()
    {
        //Find all the game objects that can have memories
        foreach (MemoryMonoBehaviour mmb in FindObjectsOfType<MemoryMonoBehaviour>())
        {
            string key = mmb.gameObject.getKey();
            //If there's a memory saved for this object,
            if (memories.ContainsKey(key))
            {
                //Tell that object what it is
                mmb.acceptMemoryObject(memories[key]);
            }
        }
    }
    #endregion

    #region Time Management
    /// <summary>
    /// Saves the current game state
    /// </summary>
    public void Save()
    {
        //Remove any null objects from the list
        cleanObjects();
        //Create a new game state
        gameStates.Add(new GameState(gameObjects.Values));
        //Update game state id variables
        chosenId++;
        rewindId++;
        //Open Scenes
        foreach (SceneLoader sl in sceneLoaders)
        {
            //If the scene loader's scene is open,
            if (openScenes.Contains(sl.Scene))
            {
                //And it hasn't been open in any previous game state,
                if (sl.firstOpenGameStateId > chosenId)
                {
                    //It's first opened in this game state
                    sl.firstOpenGameStateId = chosenId;
                }
                //It's also last opened in this game state
                sl.lastOpenGameStateId = chosenId;
            }
        }
    }
    /// <summary>
    /// Load the game state with the given id
    /// </summary>
    /// <param name="gamestateId">The ID of the game state to load</param>
    public void Load(int gamestateId)
    {
        //Update chosenId to game-state-now
        chosenId = Utility.clamp(gamestateId, 0, gameStates.Count);
        //Remove null objects from the list
        cleanObjects();
        //Destroy objects not spawned yet in the new selected state
        List<GameObject> destroyObjectList = new List<GameObject>();
        foreach (GameObject go in gameObjects.Values)
        {
            foreach (SavableMonoBehaviour smb in go.GetComponents<SavableMonoBehaviour>())
            {
                //If the game object was spawned during run time
                //(versus pre-placed at edit time)
                if (smb.IsSpawnedObject)
                {
                    //And if the game object is not in the game state,
                    if (!gameStates[gamestateId].hasGameObject(go))
                    {
                        //remove it from game objects list
                        //by adding it to the list of game objects to be destroyed
                        destroyObjectList.Add(go);
                    }
                }
            }
        }
        //Actually destroy the objects that need destroyed
        for (int i = destroyObjectList.Count - 1; i >= 0; i--)
        {
            //Work around to avoid deleting objects from a list that's being iterated over
            destroyObject(destroyObjectList[i]);
        }
        //Actually load the game state
        gameStates[gamestateId].load();

        //Destroy game states in game-state-future
        for (int i = gameStates.Count - 1; i > gamestateId; i--)
        {
            Destroy(gameStates[i].Representation);
            gameStates.RemoveAt(i);
        }
        //Update the next game state id
        GameState.nextid = gamestateId + 1;

        //If the rewind is finished,
        if (chosenId == rewindId)
        {
            //Stop the rewind
            Rewinding = false;
        }
    }

    /// <summary>
    /// Rewinds back a number of states equal to count
    /// </summary>
    /// <param name="count">How many states to rewind. 0 doesn't rewind. 1 undoes 1 state</param>
    public void Rewind(int count)
    {
        RewindTo(chosenId - count, false);
    }

    /// <summary>
    /// Sets into motion the rewind state.
    /// Update carries out the motions of calling Load()
    /// </summary>
    /// <param name="gamestateId">The game state id to rewind to</param>
    void RewindTo(int gamestateId, bool playerInitiated = true)
    {
        //Set interruptable
        rewindInterruptableByPlayer = playerInitiated;
        //Set the game state tracker vars
        rewindId = Mathf.Max(0, gamestateId);
        //Start the rewind
        Rewinding = true;
    }
    /// <summary>
    /// Rewind the game all the way to the beginning
    /// </summary>
    public void RewindToStart(bool playerInitiated = false)
    {
        RewindTo(0, playerInitiated);
    }
    /// <summary>
    /// True if time is rewinding
    /// </summary>
    public bool Rewinding
    {
        get { return chosenId > rewindId; }
        private set
        {
            //Start rewinding
            if (value)
            {
                //Make sure rewind variable is set correctly
                if (rewindId == chosenId)
                {
                    //If it has not been,
                    //rewind to start
                    rewindId = 0;
                }
                //Set the music speed to rewind
                Managers.Music.SongSpeed = Managers.Music.rewindSongSpeed;
                //Show rewind visual effect
                Managers.Effect.showRewindEffect(true);
                //Set rewindDelay
                int count = chosenId - rewindId;
                rewindDelay = baseRewindDelay;
                if (count * rewindDelay < minRewindDuration)
                {
                    rewindDelay = minRewindDuration / count;
                }
                //Load levels that Merky will be passing through
                foreach (SceneLoader sl in sceneLoaders)
                {
                    for (int i = gameStates.Count - 1; i >= rewindId; i--)
                    {
                        if (sl.isPositionInScene(gameStates[i].Merky.position))
                        {
                            sl.loadLevelIfUnLoaded();
                            break;
                        }
                    }
                }
                //Recenter the camera on Merky
                Managers.Camera.recenter();
                //Disable physics while rewinding
                Managers.Physics2DSurrogate.enabled = true;
                //Pause time
                Managers.Time.setPause(this, true);
                //Update Stats
                GameStatistics.addOne("Rewind");
            }
            //Stop rewinding
            else
            {
                //Set rewindId to chosenId
                rewindId = chosenId;
                //Refresh the game object list
                refreshGameObjects();
                //Put the music back to normal
                Managers.Music.SongSpeed = Managers.Music.normalSongSpeed;
                //Stop rewind visual effect
                Managers.Effect.showRewindEffect(false);
                //Update Scene tracking variables
                foreach (SceneLoader sl in sceneLoaders)
                {
                    //If the scene was last opened after game-state-now,
                    if (sl.lastOpenGameStateId > chosenId)
                    {
                        //it is now last opened game-state-now
                        sl.lastOpenGameStateId = chosenId;
                    }
                    //if the scene was first opened after game-state-now,
                    if (sl.firstOpenGameStateId > chosenId)
                    {
                        //it is now never opened
                        sl.firstOpenGameStateId = int.MaxValue;
                        sl.lastOpenGameStateId = -1;
                    }
                }
                //Unpause time
                Managers.Time.setPause(this, false);
                //Re-enable physics because the rewind is over
                Managers.Physics2DSurrogate.enabled = false;
                //Rewind Finished Delegate
                onRewindFinished?.Invoke();
            }
        }
    }
    public delegate void OnRewindFinished();
    public OnRewindFinished onRewindFinished;
    /// <summary>
    /// Ends the rewind at the current game state
    /// </summary>
    public void cancelRewind()
    {
        //Stop the rewind
        Rewinding = false;
        //Load the current game state
        Load(chosenId);
    }
    #endregion

    #region Space Management
    void checkScenes()
    {
        foreach (SceneLoader sl in sceneLoaders)
        {
            sl.check();
        }
    }

    void sceneLoaded(Scene scene, LoadSceneMode m)
    {
        if (scene.name == "PlayerScene")
        {
            playerSceneLoaded = true;
        }
        //Update the list of objects with state to save
        Debug.Log("sceneLoaded: " + scene.name + ", old object count: " + gameObjects.Count);
        refreshGameObjects();
        Debug.Log("sceneLoaded: " + scene.name + ", new object count: " + gameObjects.Count);
        //Add the given scene to list of open scenes
        openScenes.Add(scene);
        //If time is moving forward,
        if (!Rewinding)
        {
            //Load the previous state of the objects in the scene
            LoadObjectsFromScene(scene);
        }
        //If the game has just begun,
        if (gameStates.Count == 0)
        {
            //Create the initial save state
            Save();
        }
        //If its a level scene,
        SceneLoader sceneLoader = getSceneLoaderByName(scene.name);
        if (sceneLoader)
        {
            if (scene.name == PauseForLoadingSceneName)
            {
                //Unpause the game
                PauseForLoadingSceneName = null;
            }
        }
    }
    void sceneUnloaded(Scene scene)
    {
        //Remove the given scene's objects from the forgotten objects list
        for (int i = forgottenObjects.Count - 1; i >= 0; i--)
        {
            GameObject fgo = forgottenObjects[i];
            if (fgo == null || fgo.scene == scene)
            {
                forgottenObjects.RemoveAt(i);
            }
        }
        //Update the list of game objects to save
        Debug.Log("sceneUnloaded: " + scene.name + ", old object count: " + gameObjects.Count);
        refreshGameObjects();
        Debug.Log("sceneUnloaded: " + scene.name + ", new object count: " + gameObjects.Count);
        //Remove the scene from the list of open scenes
        openScenes.Remove(scene);
    }

    public bool isSceneOpen(Scene scene)
    {
        foreach (Scene s in openScenes)
        {
            if (scene == s)
            {
                return true;
            }
        }
        return false;
    }

    public bool isSceneOpenByName(string sceneName)
    {
        foreach (Scene s in openScenes)
        {
            if (sceneName == s.name)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Restores the objects in the scene to their previous state before the scene was unloaded
    /// </summary>
    /// <param name="scene">The scene whose objects need their state stored</param>
    public void LoadObjectsFromScene(Scene scene)
    {
        //Find the last state that this scene was saved in
        int lastStateSeen = -1;
        foreach (SceneLoader sl in sceneLoaders)
        {
            if (scene == sl.Scene)
            {
                lastStateSeen = sl.lastOpenGameStateId;
                break;
            }
        }
        Debug.Log("LOFS: Scene " + scene.name + ": last state seen: " + lastStateSeen);
        //If the scene was never seen,
        if (lastStateSeen < 0)
        {
            //Don't restore its objects' states,
            //because there's nothing to restore
            return;
        }
        //If the scene was last seen after gamestate-now,
        if (lastStateSeen > chosenId)
        {
            //The scene is now last seen gamestate-now
            lastStateSeen = chosenId;
        }
        int newObjectsFound = 0;
        int objectsLoaded = 0;
        //Load Each Object
        foreach (GameObject go in gameObjects.Values)
        {
            //If the game object is in the given scene,
            if (go.scene == scene)
            {
                newObjectsFound++;
                //Search through the game states to see when it was last saved
                for (int stateid = lastStateSeen; stateid >= 0; stateid--)
                {
                    //If the game object was last saved in this game state,
                    if (gameStates[stateid].loadObject(go))
                    {
                        //Great! It's loaded,
                        //Let's move onto the next object
                        objectsLoaded++;
                        break;
                    }
                    //Else,
                    else
                    {
                        //Continue until you find the game state that has the most recent information about this object
                    }
                }
            }
        }
        Debug.Log("LOFS: Scene " + scene.name + ": objects found: " + newObjectsFound + ", objects loaded: " + objectsLoaded);
    }

    private SceneLoader getSceneLoaderByName(string sceneName)
    {
        foreach (SceneLoader sl in sceneLoaders)
        {
            if (sl.sceneName == sceneName)
            {
                return sl;
            }
        }
        return null;
    }
    #endregion

    #region File Management
    /// <summary>
    /// Saves the memories, game states, and scene cache to a save file
    /// </summary>
    public void saveToFile()
    {
        //Set the base filename
        string fileName = "merky";
        //If saving with time stamp,
        if (saveWithTimeStamp)
        {
            //Add the time stamp to the filename
            System.DateTime now = System.DateTime.Now;
            fileName += "-" + now.Ticks;
        }
        //Add an extension to the filename
        fileName += ".txt";
        //Save memories
        ES3.Save<Dictionary<string, MemoryObject>>("memories", memories, fileName);
        //Save game states
        ES3.Save<List<GameState>>("states", gameStates, fileName);
        //Save scene cache
        //ES3.Save<List<SceneLoader>>("scenes", sceneLoaders, fileName);
        //Save settings
        Managers.Settings.saveSettings();
        //Save file settings
        List<SettingObject> settings = new List<SettingObject>();
        foreach (Setting setting in FindObjectsOfType<MonoBehaviour>().OfType<Setting>())
        {
            if (setting.Scope == SettingScope.SAVE_FILE)
            {
                settings.Add(setting.Setting);
            }
        }
        ES3.Save<List<SettingObject>>("settings", settings, fileName);
    }
    /// <summary>
    /// Loads the game from the save file
    /// It assumes the file already exists
    /// </summary>
    public void loadFromFile()
    {
        try
        {
            //Set the base filename
            string fileName = "merky";
            //Add an extension to the filename
            fileName += ".txt";
            //Load memories
            memories = ES3.Load<Dictionary<string, MemoryObject>>("memories", fileName);
            //Load game states
            gameStates = ES3.Load<List<GameState>>("states", fileName);
            //Scenes
            //List<SceneLoader> rsls = ES3.Load<List<SceneLoader>>("scenes", fileName);
            //Load settings
            Managers.Settings.loadSettings();
            //Load file settings
            List<SettingObject> settings = ES3.Load<List<SettingObject>>("settings", fileName);
            foreach (Setting setting in FindObjectsOfType<MonoBehaviour>().OfType<Setting>())
            {
                if (setting.Scope == SettingScope.SAVE_FILE)
                {
                    string id = setting.ID;
                    foreach (SettingObject setObj in settings)
                    {
                        if (id == setObj.id)
                        {
                            setting.Setting = setObj;
                            break;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            if (ES3.FileExists("merky.txt"))
            {
                ES3.DeleteFile("merky.txt");
            }
            resetGame(false);
        }
    }
    //Sent to all GameObjects before the application is quit
    //Auto-save on exit
    void OnApplicationQuit()
    {
        //Save the game state and then
        Save();
        //Save the game to file
        saveToFile();
    }
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            OnApplicationQuit();
        }
    }
    #endregion

    #region Player Ghosts
    /// <summary>
    /// Shows the game state representations
    /// </summary>
    public void showPlayerGhosts(bool show)
    {
        //If the game state representations should be shown,
        if (show)
        {
            //Loop through all game states
            foreach (GameState gs in gameStates)
            {
                //Show a sprite to represent them on screen
                gs.showRepresentation(chosenId);
            }
        }
        //Else, they should be hidden
        else
        {
            //Loop through all game states
            foreach (GameState gs in gameStates)
            {
                //And hide their representations
                gs.hideRepresentation();
            }
        }
    }
    /// <summary>
    /// Returns the player ghost that is closest to the given position
    /// </summary>
    /// <param name="pos">The ideal position of the closest ghost</param>
    /// <returns>The player ghost that is closest to the given position</returns>
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
    /// <summary>
    /// Used specifically to highlight last saved Merky after the first death
    /// for tutorial purposes
    /// </summary>
    /// <returns></returns>
    public Vector2 getLatestSafeRewindGhostPosition()
    {
        return gameStates[chosenId - 1].Merky.position;
    }
    #endregion

    #region Input Processing
    /// <summary>
    /// Processes the tap gesture at the given position
    /// </summary>
    /// <param name="curMPWorld">The position of the tap in world coordinates</param>
    public void processTapGesture(Vector3 curMPWorld)
    {
        GameState final = null;
        GameState prevFinal = null;
        //We have to do 2 passes to allow for both precision clicking and fat-fingered tapping
        //Sprite detection pass
        foreach (GameState gs in gameStates)
        {
            //Check sprite overlap
            if (gs.checkRepresentation(curMPWorld))
            {
                //If this game state is more recent than the current picked one,
                if (final == null || gs.id > final.id)//assuming the later ones have higher id values
                {
                    //Set the current picked one to the previously picked one
                    prevFinal = final;//remember the second-to-latest one
                    //Set this game state to the current picked one
                    final = gs;//keep the latest one                    
                }
            }
        }
        //Collider detection pass
        if (final == null)
        {
            foreach (GameState gs in gameStates)
            {
                //Check collider overlap
                if (gs.checkRepresentation(curMPWorld, false))
                {
                    //If this game state is more recent than the current picked one,
                    if (final == null || gs.id > final.id)//assuming the later ones have higher id values
                    {
                        //Set the current picked one to the previously picked one
                        prevFinal = final;//remember the second-to-latest one
                        //Set this game state to the current picked one
                        final = gs;//keep the latest one
                    }
                }
            }
        }
        //Process tapped game state
        //If a past merky was indeed selected,
        if (final != null)
        {
            //If the tapped one is already the current one,
            if (final.id == chosenId)
            {
                //And if the current one overlaps a previous one,
                if (prevFinal != null)
                {
                    //Choose the previous one
                    RewindTo(prevFinal.id);
                }
                else
                {
                    //Else, Reload the current one
                    Load(final.id);
                }
            }
            //Else if a past one was tapped,
            else
            {
                //Rewind back to it
                RewindTo(final.id);
            }
            //Update Stats
            GameStatistics.addOne("RewindPlayer");
        }

        //Leave this zoom level even if no past merky was chosen
        float defaultZoomLevel = Managers.Camera.toZoomLevel(CameraController.CameraScalePoints.DEFAULT);
        Managers.Camera.ZoomLevel = defaultZoomLevel;
        Managers.Gesture.switchGestureProfile(GestureManager.GestureProfileType.MAIN);

        //Process tapProcessed delegates
        tapProcessed?.Invoke(curMPWorld);
    }
    public delegate void TapProcessed(Vector2 curMPWorld);
    public TapProcessed tapProcessed;
    #endregion

    #region Demo Mode Methods
    /// <summary>
    /// Resets the game back to the very beginning
    /// Basically starts a new game
    /// </summary>
    public void resetGame(bool savePrevGame = true)
    {
        //Save previous game
        if (savePrevGame)
        {
            Save();
            saveToFile();
        }
        //Empty object lists
        gameObjects.Clear();
        forgottenObjects.Clear();
        memories.Clear();
        //Reset game state nextid static variable
        GameState.nextid = 0;
        //Unset SceneLoader static variables
        SceneLoader.ExplorerObject = null;
        //Unload all scenes and reload PlayerScene
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// How long the demo lasts, in seconds
    /// 0 to have no time limit
    /// </summary>
    public float GameDemoLength
    {
        get { return gamePlayTime; }
        set { gamePlayTime = Mathf.Max(value, 0); }
    }

    /// <summary>
    /// Start the demo timer
    /// </summary>
    void startDemoTimer()
    {
        //If the menu is not open,
        if (Managers.Camera.ZoomLevel > Managers.Camera.toZoomLevel(CameraController.CameraScalePoints.PORTRAIT))
        {
            //Start the timer
            resetGameTimer = GameDemoLength + Time.time;
            //Unregister this delegate
            //Managers.Gesture.tapGesture -= startDemoTimer;
        }
    }

    /// <summary>
    /// Shows the "Thanks for Playing" screen when the demo timer stops
    /// </summary>
    /// <param name="show">True to show the screen, false to hide it</param>
    private void showEndDemoScreen(bool show)
    {
        //Update the screen's active state
        endDemoScreen.SetActive(show);
        //If it should be shown,
        if (show)
        {
            //Also update its position and rotation
            //to keep it in front of the camera
            endDemoScreen.transform.position = (Vector2)Camera.main.transform.position;
            endDemoScreen.transform.localRotation = Camera.main.transform.localRotation;
        }
    }
    #endregion

}


