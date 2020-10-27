﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

/**
 *  Timeline class
 *   - new data from server or filesystem is placed in the buffer
 *   - on play, each event is taken from the buffer, visualized, then placed in the history
 *   - if buffer gets low then we attempt to fill it either with new data or by repeating events in history
 */
public class Timeline : Singleton<Timeline> {
    // singleton
    protected Timeline () { }
    //public static new Timeline Instance;



    // MAIN / CONTROLS

    [Space (10)]
    [Header ("MAIN")]


    public TMP_Dropdown dataSourceDropdown;
    //public TMP_Dropdown timelineStatusDropdown;
    public TMP_Text timelineStatusText;
    public Button startButton;
    public TMP_Text startButtonText;

    [Serializable]
    public enum TimelineStatus {
        init,           // start everything
        start,          // start everything, already know we have stopped
        stop,           // stop everything
        active,         // display / logic only - everything is running, loops managing their own data
        inactive,       // display / logic only - everything is off
        waitingForData, // display / logic only - holding pattern, waiting for data
        newDataReceived,// display / logic only - called after data received or updated
        bufferEmpty,    // display / logic only - reached end of buffer
        moveHistory,    // display / logic only - moving history to buffer
    }
    [Tooltip ("Timeline status")]
    public TimelineStatus status;

    [Tooltip ("Time since a data request made active")]
    public int waitingForDataProgress;




    // BUFFER

    [Space (10)]
    [Header ("BUFFER")]

    public List<FeedData> buffer = new List<FeedData> ();

    Coroutine bufferCoroutine;

    [Tooltip ("Current number of feed events in buffer")]
    public int bufferCount;

    [Tooltip ("Max items allowed in buffer")]
    [Range (1, 20)]
    public int bufferCountMin;
    [Tooltip ("Min items allowed in buffer")]
    [Range (10, 10000)]
    public int bufferCountMax;

    [Tooltip ("How often the buffer is checked (in seconds)")]
    public int bufferCheckFrequency = 2;


    public TMP_Text bufferText;
    public ScrollRect bufferScrollRect;
    public TMP_Text bufferTitleText;





    // HISTORY 

    [Space (10)]
    [Header ("HISTORY")]

    Coroutine historyCoroutine;

    public List<FeedData> history = new List<FeedData> ();

    [Tooltip ("Current number of feed events in history")]
    public int historyCount;

    public TMP_Text historyText;
    public ScrollRect historyScrollRect;
    public TMP_Text historyTitleText;




    // TIME

    [Space (10)]
    [Header ("TIME")]

    [Tooltip ("DateTime of previously displayed event")]
    [SerializeField]
    DateTime previousTime;

    [Tooltip ("Difference (seconds) between current time (feed.createdAt) and previousTime")]
    public int timeDiff;

    [Tooltip ("Difference (seconds) adjusted")]
    public float timeDiffScaled;

    [Tooltip ("How much faster time is replayed (timeDiff * scalar)")]
    [Range (0f, 1f)]
    public float timeDiffScalar = 0.01f;

    [Tooltip ("Min time allowed between events")]
    public float minTimeDiff = 1;
    [Tooltip ("Max time allowed between events")]
    public float maxTimeDiff = 10;




    private void Awake ()
    {
        //// populate options in the status dropdown 
        //Dropdown_PopulateStatus ();
    }

    private void Start ()
    {
        StartBufferLoop ();
    }



    /////////////////////////////////////////////////////////////
    /////////////////////////// UI //////////////////////////////
    /////////////////////////////////////////////////////////////

    //// populate dropdown options
    //void Dropdown_PopulateStatus ()
    //{
    //    // clear the options in the dropdown
    //    timelineStatusDropdown.ClearOptions ();
    //    // save the enum options as a string[]
    //    string [] enumOptions = Enum.GetNames (typeof (TimelineStatus));
    //    // create a new list for the available options
    //    List<string> options = new List<string> (enumOptions);
    //    // add options to list
    //    timelineStatusDropdown.AddOptions (options);
    //}
    ///**
    // *  Called from UI to update the game status
    // */
    //public void Dropdown_Listener (int _status)
    //{
    //    // call in-game logic, cast as Enum
    //    SetTimelineStatus ((TimelineStatus)_status, true);
    //}



    /**
     *  Called from game to update the status 
     */
    public void SetTimelineStatus (TimelineStatus _status, bool fromUI = false)
    {
        Debug.Log ("Timeline.SetTimelineStatus() status = " + status + ", _status = " + _status);

        // update status var
        status = _status;

        // if the call (note, original status) came from within the game then show in UI
        if (!fromUI) timelineStatusText.text = _status.ToString ();

        // run immediately
        //TimelineControl ();
    }


    /**
     *  Called from the UI button to start / stop
     */
    public void StartBtn ()
    {
        // if currently active
        if (status == TimelineStatus.active) {
            SetStartBtn (" --- ", false);
            // stop everything
            StopBufferLoop ();
            StopHistoryLoop ();
            // set status
            SetTimelineStatus (TimelineStatus.inactive);
            // update btn text and make interactable
            SetStartBtn ("start", true);
        }
        // if not active
        else {
            // set status
            SetTimelineStatus (TimelineStatus.start);
            // start loop
            StartBufferLoop ();
        }
        UpdateCounts ();
        UpdateTimelineLogs ();
    }

    void SetStartBtn (string txt, bool interact)
    {
        startButtonText.text = txt;
        startButton.interactable = interact;
    }




    /////////////////////////////////////////////////////////////
    //////////////////////// CONTROLS ///////////////////////////
    /////////////////////////////////////////////////////////////


    /**
     *  (stops and then) starts the buffer loop
     */
    public void StartBufferLoop ()
    {
        Debug.Log ("Timeline.StartBufferLoop()");

        // if coroutine running
        if (bufferCoroutine != null) StopCoroutine (bufferCoroutine);
        // start buffer, attempt to get new data
        bufferCoroutine = StartCoroutine (BufferLoop ());
    }
    /**
     *  Stop fetching new data
     */
    public void StopBufferLoop ()
    {
        Debug.Log ("Timeline.StopBufferLoop()");

        // if coroutine running
        if (bufferCoroutine != null) StopCoroutine (bufferCoroutine);

        // clear and reset capacity
        buffer.Clear ();
        buffer.TrimExcess ();
    }

    /**
     *  (stops and then) starts the history loop
     */
    public void StartHistoryLoop ()
    {
        Debug.Log ("Timeline.StartHistoryLoop()");

        // if coroutine running
        if (historyCoroutine != null) StopCoroutine (historyCoroutine);
        // start buffer, attempt to get new data
        historyCoroutine = StartCoroutine (HistoryLoop ());
    }
    /**
     *  Stop displaying events
     */
    public void StopHistoryLoop ()
    {
        Debug.Log ("Timeline.StopHistoryLoop()");

        // if coroutine running
        if (historyCoroutine != null) StopCoroutine (historyCoroutine);

        // reset previous time
        previousTime = DateTime.MinValue;

        // clear and reset capacity
        history.Clear ();
        history.TrimExcess ();
    }






    /////////////////////////////////////////////////////////////
    ///////////////////// BUFFER LOOP ///////////////////////////
    /////////////////////////////////////////////////////////////


    /**
     *  Check the buffer, add data
     */
    IEnumerator BufferLoop ()
    {
        while (true) {

            // update count
            UpdateCounts ();

            //Debug.Log ("Timeline.BufferLoop()");
            DebugManager.Instance.UpdateDisplay ("Timeline.BufferLoop() status = " + status + ", count = " + bufferCount.ToString ());



            // INIT

            if (status == TimelineStatus.init) {
                // set to start
                SetTimelineStatus (TimelineStatus.start);
            }

            // START

            if (status == TimelineStatus.start) {
                // reset progress
                waitingForDataProgress = 0;

                // disable button until data arrives
                SetStartBtn (" ... ", false);

                // set to waiting
                SetTimelineStatus (TimelineStatus.waitingForData);

                // attempt to get new data from server,
                EventManager.TriggerEvent ("GetNewData");
            }

           // STOP

           else if (status == TimelineStatus.stop) {

                // clear buffer
                buffer.Clear ();
                buffer.TrimExcess ();

                // set to waiting
                SetTimelineStatus (TimelineStatus.inactive);
            }

           // WAITING FOR DATA

           else if (status == TimelineStatus.waitingForData) {

                // time since request
                waitingForDataProgress++;

                // buffer WAS waiting, now has data
                if (bufferCount > 0) {

                    // set status
                    SetTimelineStatus (TimelineStatus.newDataReceived);

                } else if (waitingForDataProgress > 20) {
                    // handle lack of data here
                }
            }

           // NEW DATA RECEIVED

           else if (status == TimelineStatus.newDataReceived) {

                // after new data, sort ascending
                buffer.Sort ((x, y) => x.createdAt.CompareTo (y.createdAt));

                // remove duplicate users here?

                // start history loop again
                StartHistoryLoop ();

                // update btn text and make interactable
                SetStartBtn ("stop", true);

                // set status
                SetTimelineStatus (TimelineStatus.active);
            }


           // ACTIVE

           else if (status == TimelineStatus.active) {

                // if all of buffer has been moved to history
                if (bufferCount <= bufferCountMin) {

                    // set to handle end of buffer
                    SetTimelineStatus (TimelineStatus.bufferEmpty);

                }
                // is the bufferCount > max?
                else if (bufferCount > bufferCountMax) {
                    // need to handle this eventually
                    // maybe...
                }
            }

           // BUFFER EMPTY

           else if (status == TimelineStatus.bufferEmpty) {

                // local
                if (dataSourceDropdown.value == 0) {
                    // set to handle end of buffer
                    SetTimelineStatus (TimelineStatus.moveHistory);
                }
                // live
                else if (dataSourceDropdown.value == 1) {
                    // get new data
                }

            }

            // MOVE HISTORY (TO BUFFER)

            else if (status == TimelineStatus.moveHistory) {

                // move history back to buffer (assuming we are doing this with one big file)
                MoveListRange (bufferCountMax, history, buffer);
                // set to handle end of buffer
                SetTimelineStatus (TimelineStatus.active);

            }


            UpdateCounts ();

            // after checking condition
            if (bufferCount > 0) {
                // display
                UpdateTimelineLogs ();
                // if we have data in the buffer or history but no players then add them
                if (PlayerManager.Instance.playerCount < 1) {
                    EventManager.TriggerEvent ("ResetPlayers");
                }
            }

            yield return new WaitForSeconds (bufferCheckFrequency);
        }
    }



    /////////////////////////////////////////////////////////////
    /////////////////// HISTORY LOOP ////////////////////////////
    /////////////////////////////////////////////////////////////


    /**
     *  Play events from buffer and move to history
     */
    IEnumerator HistoryLoop ()
    {
        while (true) {

            // update count
            UpdateCounts ();

            //Debug.Log ("Timeline.HistoryLoop()");
            //DebugManager.Instance.UpdateDisplay ("Timeline.HistoryLoop() count = " + historyCount.ToString ());

            // if buffer has items
            if (bufferCount > 0) {

                // get first feed in buffer
                FeedData feed = buffer [0];


                // HANDLE TIME
                //Debug.Log ("previousTime = " + previousTime);
                // on first run only, use current time as previous time
                if (previousTime == null) previousTime = feed.createdAt;


                // find difference in seconds between createdAt and previousTime 
                timeDiff = (int)(feed.createdAt - previousTime).TotalSeconds;

                // scaled difference (speed it up)
                timeDiffScaled = Mathf.Clamp (timeDiff * timeDiffScalar, minTimeDiff, maxTimeDiff);

                // set previous time for next loop
                previousTime = feed.createdAt;



                // PLAY EVENT

                // let PlayerManager find player and visualize event
                PlayerManager.Instance.PlayEvent (feed);


                // MANAGE EVENT

                // move event to history
                MoveListRange (1, buffer, history);



                UpdateCounts ();

                // after 
                if (historyCount > 0) {
                    // after update, sort ascending
                    history.Sort ((x, y) => x.createdAt.CompareTo (y.createdAt));
                    // display
                    UpdateTimelineLogs ();
                }


                // log feed item
                var eventString = timeDiff + " (" + timeDiffScaled + ") " +
                    " (" + feed.createdAt + ") " +
                    //" = (" + previousTime + " - " + feed.createdAt + ") " +
                    feed.username + ", " + feed.eventType + "";

                DebugManager.Instance.UpdateDisplay ("Timeline.HistoryLoop() " + eventString);


            } else {
                // safety
                timeDiffScaled = 1;
            }

            // time difference to next event (or safety)
            yield return new WaitForSeconds (timeDiffScaled);
        }
    }



    /////////////////////////////////////////////////////////////
    ////////////////////// GENERAL //////////////////////////////
    /////////////////////////////////////////////////////////////


    /**
     *  Update display for both history and buffer
     */
    void UpdateTimelineLogs ()
    {
        //Debug.Log ("Timeline.UpdateTimelineLogs()");

        string bufferString = "";
        string historyString = "";

        int safety = 0;
        foreach (var feed in buffer) {
            bufferString += feed.eventType + ". " + feed.createdAt + " - " + feed.username + "<br>";
            if (++safety > bufferCountMax) {
                Debug.Log ("Safety first!");
                break;
            }
        }
        safety = 0;
        foreach (var feed in history) {
            historyString += feed.eventType + ". " + feed.createdAt + " - " + feed.username + "<br>";
            if (++safety > bufferCountMax) {
                Debug.Log ("Safety first!");
                break;
            }
        }

        bufferText.text = bufferString;
        historyText.text = historyString;

        UpdateCounts ();
        UpdateScroll ();

        bufferTitleText.text = "Buffer [ " + bufferCount + " ] ";
        historyTitleText.text = "History [ " + historyCount + " ] ";

        // trigger timeline updated event
        //EventManager.TriggerEvent ("TimelineUpdated");
    }

    /**
     *  Update counts of both history and buffer
     */
    void UpdateCounts ()
    {
        bufferCount = buffer.Count;
        historyCount = history.Count;
    }

    public void UpdateScroll ()
    {
        Canvas.ForceUpdateCanvases ();
        bufferScrollRect.verticalNormalizedPosition = 0f;
        historyScrollRect.verticalNormalizedPosition = 0f;
    }

    /**
     *  Moves n items from list1 to list2
     */
    void MoveListRange (int count, List<FeedData> list1, List<FeedData> list2)
    {
        //Debug.Log ("MoveListRange() [1] " + list1.ToString () + " > " + list2.ToString ());

        // return if no events in list1 to move
        if (list1.Count < 1) return;

        // update count if it exceeds number items in list1
        if (count > list1.Count) count = list1.Count;

        // copy range from list1 > list2
        list2.AddRange (list1.GetRange (0, count));

        // delete the range in list1
        list1.RemoveRange (0, count);
    }



}




