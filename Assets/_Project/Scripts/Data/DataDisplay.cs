﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DataDisplay : MonoBehaviour {



    public TMP_Text eventCountText;
    public TMP_Text playerCountText;
    public TMP_Text eventNumberText;



    // listeners
    void OnEnable ()
    {
        EventManager.StartListening ("DataDownloaded", UpdateDisplay);
        EventManager.StartListening ("DataUpdated", UpdateDisplay);
        EventManager.StartListening ("PlayersUpdated", UpdateDisplay);
        EventManager.StartListening ("TimelineUpdated", UpdateDisplay);
    }
    void OnDisable ()
    {
        EventManager.StopListening ("DataDownloaded", UpdateDisplay);
        EventManager.StopListening ("DataUpdated", UpdateDisplay);
        EventManager.StopListening ("PlayersUpdated", UpdateDisplay);
        EventManager.StopListening ("TimelineUpdated", UpdateDisplay);
    }


    // update text
    public void UpdateDisplay ()
    {
        eventCountText.text = DataManager.dataCount.ToString () + " events";
        playerCountText.text = PlayerManager.Instance.playerCount.ToString () + " players";
        eventNumberText.text = TimelineManager.Instance.feedIndex.ToString () + " / " + DataManager.dataCount.ToString ();
    }






}