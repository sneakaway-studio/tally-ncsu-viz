﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : Singleton<PlayerManager> {
    // singleton
    protected PlayerManager () { }
    //public static new PlayerManager Instance;

    //listeners
    void OnEnable ()
    {
        EventManager.StartListening ("DataDownloaded", ResetPlayers);
    }
    void OnDisable ()
    {
        EventManager.StopListening ("DataDownloaded", ResetPlayers);
    }


    // bounds, prefab, dict for instantiating players
    public Collider worldContainerCollider;
    public GameObject playerPrefab;
    public Dictionary<string, GameObject> playerDict;

    // player currently showing an event
    GameObject currentPlayer;
    // number of players
    public int playerCount;
    // player animation controller
    TallyAnimController playerCharacterAnim;

    // temp sprites for assigning avatars
    public Sprite [] avatars;


    private void Awake ()
    {
        //Instance = this;
        playerDict = new Dictionary<string, GameObject> ();
    }


    /**
     *  Remove all players from stage, reset dict
     */
    public void ResetPlayers ()
    {
        // clear the dictionary 
        playerDict.Clear ();

        // loop through the feed and add players
        foreach (var feed in DataManager.feeds) {
            CreateNewPlayer (feed.username, feed.avatarPath);
        }

        // update player count
        playerCount = playerDict.Count;


        // trigger data updated event
        EventManager.TriggerEvent ("PlayersUpdated");
    }

    /**
     *  Create a new player
     */
    public void CreateNewPlayer (string username, string avatarPath)
    {
        // make sure the player doesn't already exist
        if (playerDict.ContainsKey (username)) return;

        // get a position that doesn't contain any other colliders
        Vector3 spawnPosition = GetClearSpawnPosition ();

        // get the spawn rotation
        Quaternion spawnRotation = new Quaternion ();
        // random rotation - on local x only
        //spawnRotation.eulerAngles = new Vector3 (Random.Range (0.0f, 360.0f), 90f, 0f);
        // no random rotation
        spawnRotation.eulerAngles = new Vector3 (0f, 0f, 0f);

        // if clear spawn position
        if (spawnPosition != Vector3.zero) {
            // instantiate prefab @ spawn position
            GameObject obj = (GameObject)Instantiate (playerPrefab, spawnPosition, spawnRotation);
            // call Init() on Player
            obj.GetComponent<Player> ().Init (username, avatarPath);
            // set name in Unity Editor
            obj.name = username;
            // parent under PlayerManger
            obj.transform.parent = gameObject.transform;
            // finaly, add to dict
            playerDict.Add (username, obj);
        }
    }



    public void PlayEvent (FeedData feed)
    {
        //Debug.Log (DebugManager.GetSymbol ("smilingFace") + " PlayerManager.PlayEvent() [1] feed = " + feed.username.ToString ());

        // play matching sound
        AudioManager.Instance.Play (feed.eventType);

        // get the player from the dict
        playerDict.TryGetValue (feed.username, out currentPlayer);
        if (!currentPlayer) return;


        // MARKED FOR DELETION
        //// get player obj
        //GameObject playerCharacter;
        //TallyAnimController playerCharacterAnim;
        //// get the first child
        //playerCharacter = currentPlayer.transform.GetChild (0).gameObject;
        //Debug.Log (DebugManager.GetSymbol ("smilingFace") + " PlayerManager.PlayEvent() [2] - " + playerCharacter.name);
        //// get the animation controller
        //playerCharacterAnim = playerCharacter.GetComponent<TallyAnimController> ();



        // get the animation controller
        playerCharacterAnim = currentPlayer.GetComponentInChildren<TallyAnimController> ();

        // play the animation
        playerCharacterAnim.animEvent = feed.eventType;

    }





    /**
     *  Return random Vector3 position that doesn't already have a GameObject in it
     */
    Vector3 GetClearSpawnPosition ()
    {
        Vector3 spawnPosition = new Vector3 ();
        float startTime = Time.realtimeSinceStartup;
        bool positionClear = false;
        int layerMask = (1 << 8); // only Layer 8 "Players"
        while (positionClear == false) {
            // get random position
            Vector3 spawnPositionRaw = RandomPointInBounds (worldContainerCollider.bounds);
            // store for test
            spawnPosition = new Vector3 (spawnPositionRaw.x, spawnPositionRaw.y, spawnPositionRaw.z);
            // get all the overlaping colliders
            Collider [] hitColliders = Physics.OverlapSphere (spawnPosition, 0.75f, layerMask);
            // if collider isn't touching another player collider then set true to stop loop
            if (hitColliders.Length <= 0) positionClear = true;
            // else continue until time has run out
            if (Time.realtimeSinceStartup - startTime > 0.5f) {
                Debug.Log ("Time out placing GameObject!");
                return Vector3.zero;
            }
        }
        return spawnPosition;
    }

    /**
     *  Return random Vector3 position inside bounds
     */
    public static Vector3 RandomPointInBounds (Bounds bounds)
    {
        return new Vector3 (
            Random.Range (bounds.min.x, bounds.max.x),
            Random.Range (bounds.min.y, bounds.max.y),
            0 //Random.Range (bounds.min.z, bounds.max.z)
        );
    }

}