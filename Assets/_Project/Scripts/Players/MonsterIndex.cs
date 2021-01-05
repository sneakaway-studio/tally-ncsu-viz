﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 *  Index of all the monster sprites, mids, etc.
 */
public class MonsterIndex : Singleton<MonsterIndex> {
    // singleton
    protected MonsterIndex () { }
    //public static new MonsterIndex Instance;


    [Space (10)]
    [Header ("MONSTER SPRITES")]

    [Tooltip ("Array of sprites, including the duplicate names (textures)")]
    public Sprite [] monstersFromSheets;

    [Tooltip ("Array of sprites with the duplicate names removed")]
    public Sprite [] monstersFromSheetsDistinct;

    // adding new monsters:
    // 1. this array is exported from tally-utilities/images-composite
    // 2. drag sprites into array
    [Tooltip ("Array of mids used in the game - index * 3 = sprite position")]
    public int [] gameMids = {
        6,15,48,63,82,86,87,89,91,92,96,100,102,110,111,118,121,122,132,135,137,151,154,155,158,161,165,169,170,171,172,174,176,177,178,179,181,184,202,204,205,207,209,210,211,212,215,216,217,218,220,221,222,224,229,
        231,241,242,243,244,269,271,272,274,277,278,279,281,282,283,300,301,310,315,316,324,325,326,327,329,330,331,332,333,334,335,336,343,346,347,348,350,357,359,360,362,363,370,371,376,381,383,384,385,386,410,418,
        420,425,443,450,456,466,469,470,472,477,482,492,499,521,545,563,582,584,589,594,596,599,600,601,607,614,618,620,625,627,628,630,632,633,635,637,638,640,653,655,673,677,681,683,684
    };
    // "first party trackers"
    public int [] gameMidsFirstParyTrackers = {
        995,996,997,998,999
    };

    [Tooltip ("Total sprites")]
    public int monstersFromSheetsDistinctLength;


    public List<GameObject> monsterMasterList = new List<GameObject> ();



    private void Awake ()
    {
        // remove duplicates from the spritesheet array
        monstersFromSheetsDistinct = monstersFromSheets.Distinct ().ToArray ();
        // set the length
        monstersFromSheetsDistinctLength = monstersFromSheetsDistinct.Length;
    }


    /**
     *  Return the index of the mid within the midsInGame array
     */
    public int GetGameMidIndex (int _mid)
    {
        //Debug.Log ("GetMonsterSpriteIndex() [1] _mid = " + _mid);
        int midSpriteIndex = -1;
        if (gameMids.Contains (_mid))
            midSpriteIndex = Array.IndexOf (gameMids, _mid);
        //Debug.Log ("GetMonsterSpriteIndex() [2] midSpriteIndex = " + midSpriteIndex);
        return midSpriteIndex;
    }


    public int GetRandomMid (int min = 0, int max = 200)
    {
        // keep max in range
        max = Math.Min (max, gameMids.Length);
        int index = (int)UnityEngine.Random.Range (0, max);
        return gameMids [index];
    }

    public int GetRandomFirstPartyMid (int min = 0, int max = 200)
    {
        // keep max in range
        max = Math.Min (max, gameMidsFirstParyTrackers.Length);
        int index = (int)UnityEngine.Random.Range (0, max);
        return gameMidsFirstParyTrackers [index];
    }

}
