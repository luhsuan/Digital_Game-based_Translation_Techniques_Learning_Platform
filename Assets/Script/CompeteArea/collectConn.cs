﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using System;

public class collectConn : PunBehaviour
{

    public GameObject obj_gamestart, UI_guide;
    Button btn_start,btn_guide,btn_backguide;
    //Text id;
    string username;
    AudioSource ClickBtn;

    public static string[] ques, option;
    private string serverlink = "http://140.115.126.167/translate/";
    string UserID;

    string previousRoomPlayerPrefKey = "PUN:Demo:RPS:PreviousRoom";
    ///
    public string previousRoom;
    private const string MainSceneName = "Home";
    const string NickNamePlayerPrefsKey = "NickName";
    Xmlprocess xmlprocess;

    void Start () {
        ClickBtn = GetComponents<AudioSource>()[1];
        //id = obj_gamestart.GetComponentsInChildren<Text>()[0];
        // username = obj_gamestart.GetComponentsInChildren<InputField>()[0];
        btn_start = obj_gamestart.GetComponentsInChildren<Button>()[0];
        btn_guide = obj_gamestart.GetComponentsInChildren<Button>()[2];
        btn_backguide = obj_gamestart.GetComponentsInChildren<Button>()[3];

        btn_backguide.onClick.AddListener(back);
        btn_start.onClick.AddListener(gamestart);
        btn_guide.onClick.AddListener(showGuide);

        xmlprocess = new Xmlprocess();
        //id.text = xmlprocess.getUserInfo()[0];
        username = xmlprocess.getUserInfo()[1];
        UIManager.Instance.CloseAllPanel();
    }


    void gamestart() {

        //-----------暫時不使用創建方式------------------
        // createUser();
        //obj_gamestart.gameObject.SetActive(false);
        //-----------------------------------------------------------
        ClickBtn.Play();
        xmlprocess.ScceneHistoryRecord("WaitingCompete", DateTime.Now.ToString("HH:mm:ss"));
        if (PhotonNetwork.AuthValues == null)
        {
            PhotonNetwork.AuthValues = new AuthenticationValues();
        }
        PhotonNetwork.AuthValues.UserId = xmlprocess.getUserInfo()[0];//學號
        Debug.Log("playerName: " + username + "AuthValues userID: " + PhotonNetwork.AuthValues.UserId);
        PhotonNetwork.playerName = username;

        PlayerPrefs.SetString(NickNamePlayerPrefsKey, username);
        PhotonNetwork.ConnectUsingSettings("0.5");
        PhotonHandler.StopFallbackSendAckThread();
        StartCoroutine(getQuestion());
        StartCoroutine(getOption());
    }

    void showGuide()
    {
        ClickBtn.Play();
        UI_guide.SetActive(true);
        Debug.Log("showGuide");

    }

    void back()
    {
        ClickBtn.Play();
        UI_guide.SetActive(false);
        Debug.Log("back");
    }
    /*
    void createUser() {
        WWWForm phpform = new WWWForm();
        phpform.AddField("user_id", id.GetComponentsInChildren<Text>()[1].text);
        phpform.AddField("user_name", username.GetComponentsInChildren<Text>()[1].text);
        new WWW(serverlink + "collectData", phpform);
    }
    */

    IEnumerator getQuestion()
    {
        string php_name = "";
        Debug.Log("關卡:"+ManageLevel_C.level);
        if( ManageLevel_C.level == "amplification" )
        {
            php_name = "AmplificationQuest.php";
        }
        if( ManageLevel_C.level == "omission" )
        {
            php_name = "OmissionQuest.php";
        }
        if( ManageLevel_C.level == "means" )
        {
            php_name = "MeansQuest.php";
        }
        if( ManageLevel_C.level == "conversion" )
        {
            php_name = "ConversionQuest.php";
        }
        if( ManageLevel_C.level == "integrate" )
        {
            php_name = "IntegrateQuest.php";
        }
        if( ManageLevel_C.level == "positive" )
        {
            php_name = "PosQuest.php";
        }
        if( ManageLevel_C.level == "domestic" )
        {
            php_name = "DomesticateQuest_compete.php";
        }

        WWWForm phpform = new WWWForm();
        phpform.AddField("action", "getQuestion");

        WWW reg = new WWW(serverlink + php_name, phpform);
        yield return reg;
        if (reg.error == null)
        {
            ques = reg.text.Split('|');//最後一個是空的
        }
        else
        {
            Debug.Log("error msg" + reg.error);
        }

    }

    IEnumerator getOption()
    {
        string php_name = "";
        // Debug.Log("關卡:"+ManageLevel_C.level);
        if( ManageLevel_C.level == "amplification" )
        {
            php_name = "getAmplificationOption.php";
        }
        if( ManageLevel_C.level == "omission" )
        {
            php_name = "getOmissionOption.php";
        }
        if( ManageLevel_C.level == "means" )
        {
            php_name = "getMeansOption.php";
        }
        if( ManageLevel_C.level == "conversion" )
        {
            php_name = "getConversionOption.php";
        }
        if( ManageLevel_C.level == "domestic" )
        {
            php_name = "getDomesticateOption.php";
        }
   

        WWWForm phpform = new WWWForm();
        phpform.AddField("action", "getOption");
        WWW reg = new WWW(serverlink + php_name, phpform);
        yield return reg;
        if (reg.error == null)
        {
            option = reg.text.Split('|');//最後一個是空的
        }
        else
        {
            Debug.Log("error msg" + reg.error);
        }

    }
   



    public override void OnConnectedToMaster()
    {
        this.UserID = PhotonNetwork.player.UserId;
        //Debug.Log("UserID " + this.UserID);

        if (PlayerPrefs.HasKey(previousRoomPlayerPrefKey))//有先前的房間
        {
            this.previousRoom = PlayerPrefs.GetString(previousRoomPlayerPrefKey);
            PlayerPrefs.DeleteKey(previousRoomPlayerPrefKey);
            // 重新連回原本的房間
            if (!string.IsNullOrEmpty(this.previousRoom))
            {
                Debug.Log("ReJoining previous room: " + this.previousRoom);
                PhotonNetwork.ReJoinRoom(this.previousRoom);
                this.previousRoom = null; 
            }
        }
        else
        {
            PhotonNetwork.JoinRandomRoom();//隨機加入房間
        }
    }

    public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)//如果先前房間不存在，則刪除key
    {
        Debug.Log("previousRoom isn't exist");
        this.previousRoom = null;
        PlayerPrefs.DeleteKey(previousRoomPlayerPrefKey);

    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined other room: " + PhotonNetwork.room.Name);

        //UIManager.Instance.ClosePanel("UI_ShowMes");
        this.previousRoom = PhotonNetwork.room.Name;
        PlayerPrefs.SetString(previousRoomPlayerPrefKey, this.previousRoom);
    }

    public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)//如果沒有其他房間可以加入，則創建房間
    {
        Debug.Log("CreateRoom");
        PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = 4}, null);
    }


}
