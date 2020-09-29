using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Linq;

public class DataManager : Singleton<DataManager> {
    // singleton
    protected DataManager () { }
    public static new DataManager Instance;

    // listeners 
    void OnEnable ()
    {
        EventManager.StartListening ("GetNewData", GetNewData);
    }
    void OnDisable ()
    {
        EventManager.StopListening ("GetNewData", GetNewData);
    }


    string symbol = "\u2665".ToString ();



    // HOSTS

    [Serializable]
    public enum HostType {
        local,
        remote
    }
    public HostType chosenHost;
    string [] hosts = {
        "https://127.0.0.1:5000/api/",
        "https://tallysavestheinternet.com/api/"
    };


    // ENDPOINT / PATH

    [Serializable]
    public enum EndpointType {
        recent20,
        rangeOneWeek,
        rangePlusStreamOneDay,
        rangePlusStreamOneHour
    }
    public EndpointType chosenEndpoint;

    string [] endpoints = {
        "feed/recent",      // 20 recent
        "feed/range/1/week", // 1 week
        "feed/range/plusStream/1/day/",  // 1 day
        "feed/range/plusStream/1/hour/" // 1 hour
    };

    // chosen host and endpoint for API
    public string host;
    public string endpoint;



    // the current data as string
    public static string current;
    // as Dict
    public static IList<FeedData> feeds = new List<FeedData> ();




    private void Start ()
    {
        host = hosts [(int)chosenHost];
        endpoint = endpoints [(int)chosenEndpoint];

        // start everything
        GetNewData ();
    }


    public void GetNewData ()
    {
        Debug.Log (symbol + " DataManager.GetNewData() path = " + host + endpoint);

        StartCoroutine (GetRequest (host + endpoint));

        // error test
        // StartCoroutine(GetRequest("https://error.html"));
    }

    // do a get request for JSON data at url
    public static IEnumerator GetRequest (string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get (uri)) {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest ();

            string [] pages = uri.Split ('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError) {
                Debug.Log (pages [page] + ": Error: " + webRequest.error);
            } else {
                Debug.Log (pages [page] + ":\nReceived: " + webRequest.downloadHandler.text);

                // parse JSON array 
                JArray a = JArray.Parse (webRequest.downloadHandler.text);

                // loop through array and add each 
                foreach (JObject item in a) {
                    // base class properties
                    string _username = item.GetValue ("username").ToString ();
                    string _avatarPath = item.GetValue ("avatarPath").ToString ();
                    string _eventType = item.GetValue ("eventType").ToString ();
                    string _createdAtStr = item.GetValue ("createdAt").ToString ();
                    string _monsters = item.GetValue ("monsters").ToString ();
                    string _trackers = item.GetValue ("trackers").ToString ();

                    // parse string to ISO 8601 format
                    DateTime _createdAt = DateTime.Parse (_createdAtStr, null, System.Globalization.DateTimeStyles.RoundtripKind);

                    // parse eventData 
                    JObject d = JObject.Parse (item.GetValue ("eventData").ToString ());

                    if (_eventType == "attack") {
                        feeds.Add (new AttackData {
                            username = _username,
                            avatarPath = _avatarPath,
                            eventType = _eventType,
                            createdAt = _createdAt,
                            monsters = _monsters,
                            trackers = _trackers,

                            name = (string)d ["name"],
                            type = (string)d ["level"],
                            selected = (bool)d ["selected"]
                        });
                    } else if (_eventType == "badge") {
                        feeds.Add (new BadgeData {
                            username = _username,
                            avatarPath = _avatarPath,
                            eventType = _eventType,
                            createdAt = _createdAt,
                            monsters = _monsters,
                            trackers = _trackers,

                            name = (string)d ["name"],
                            level = (int)d ["level"]
                        });
                    } else if (_eventType == "consumable") {
                        feeds.Add (new ConsumableData {
                            username = _username,
                            avatarPath = _avatarPath,
                            eventType = _eventType,
                            createdAt = _createdAt,
                            monsters = _monsters,
                            trackers = _trackers,

                            name = (string)d ["name"],
                            slug = (string)d ["slug"],
                            stat = (string)d ["stat"],
                            type = (string)d ["type"],
                            value = (int)d ["value"]
                        });
                    } else if (_eventType == "disguise") {
                        feeds.Add (new DisguiseData {
                            username = _username,
                            avatarPath = _avatarPath,
                            eventType = _eventType,
                            createdAt = _createdAt,
                            monsters = _monsters,
                            trackers = _trackers,

                            name = (string)d ["name"],
                            type = (string)d ["type"]
                        });
                    } else if (_eventType == "monster") {
                        feeds.Add (new MonsterData {
                            username = _username,
                            avatarPath = _avatarPath,
                            eventType = _eventType,
                            createdAt = _createdAt,
                            monsters = _monsters,
                            trackers = _trackers,

                            mid = (int)d ["mid"],
                            level = (int)d ["level"],
                            captured = (int)d ["captured"],
                        });
                    } else if (_eventType == "tracker") {
                        feeds.Add (new TrackerData {
                            username = _username,
                            avatarPath = _avatarPath,
                            eventType = _eventType,
                            createdAt = _createdAt,
                            monsters = _monsters,
                            trackers = _trackers,

                            tracker = (string)d ["tracker"],
                            captured = (int)d ["captured"],
                        });
                    } else if (_eventType == "stream") {
                        feeds.Add (new StreamData {
                            username = _username,
                            avatarPath = _avatarPath,
                            eventType = _eventType,
                            createdAt = _createdAt,
                            monsters = _monsters,
                            trackers = _trackers,

                            score = (int)d ["score"],
                            clicks = (int)d ["clicks"],
                            likes = (int)d ["likes"],
                        });
                    }




                    //Debug.Log(_eventType);
                }



                //feeds = a.Select(p => new FeedData
                //{
                //    username = (string)p["username"],
                //    avatarPath = (string)p["avatarPath"],
                //    eventType = (string)p["eventType"],
                //    createdAt = (DateTime)p["createdAt"],

                //    //type = (string)p["type"],
                //    //name = (string)p["name"],
                //    //level = (int)p["level"],
                //    //stat = (string)p["stat"],
                //    //captured = (int)p["captured"],

                //}).ToList();


                ////Debug.Log(a[0]);
                //Debug.Log(feeds.ToString());


                // reset current
                current = "";

                foreach (var feed in feeds) {

                    var line =

                        feed.createdAt + "\t" +
                        feed.username + ", " +
                        feed.eventType + ", " +

                        //feed.type + ", " + feed.name + ", " + feed.level +
                        //", " + feed.type

                        ""
                        ;

                    current += line + "<br>";

                    //Debug.Log(line);
                }

                // trigger event
                EventManager.TriggerEvent ("DataUpdated");

            }
        }
    }
}