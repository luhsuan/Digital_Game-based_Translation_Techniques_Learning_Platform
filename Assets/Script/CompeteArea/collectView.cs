﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PhotonView))]
public class collectView : PunBehaviour, IPunTurnManagerCallbacks
{
    public GameObject ConnectUiView, WaitingUI, GameStartUI,ShowResultMes, ResultUIView,ShowMesUI, cardgroup, card;
    public Text question, question_ch, RemotePlayerText, LocalPlayerText, TurnText, TimeText;
    public AudioSource vol_pronun;
    PhotonPlayer[] player;
    AudioSource ClickBtn,ChooseCard;
    Button btn_gamestart, btn_Wexit,btn_hintSA,btn_hintEO;
    bool timerflag = false;
    bool timestop = false;

    int currentTime;
    int max_correctNum, C_correctNum, correctNum, wrongNum;
    int cardCount;//卡牌數量
    static int c_hintSA_count, c_hintEO_count;//當前使用提示的次數
    static int hintSA_count, hintEO_count;//使用提示的最大次數
    string[] s_option;//該回合的選項
    string[] quesInfo, optionInfo;
    DateTime TurnStartTime;
    string compete_theme;

    private PunTurnManager turnManager;
    private string localSelection, remoteSelection;
    private DateTime localTime;
    private DateTime remoteTime;
    private ResultType result;
    private bool IsShowingResults;

    #region 記錄資料
    Xmlprocess xmlprocess;
    string[] achievementState;//獎章狀態
    #endregion

    public enum ResultType
    {
        None = 0,
        CorrectAns,
        WrongAns
    }
    void Awake() {
        achievementState = new string[7];//對戰獎章數量
    }

    void Start() {
        ClickBtn = GetComponentsInChildren<AudioSource>()[0];
        ChooseCard = GetComponentsInChildren<AudioSource>()[1];
        xmlprocess = new Xmlprocess();
        this.turnManager = this.gameObject.AddComponent<PunTurnManager>();
        this.turnManager.TurnManagerListener = this;
        this.turnManager.TurnDuration = 15f;
        cardCount = 4;
        C_correctNum = -1;//當前連續答對題數
        max_correctNum = -1;//最大連續答對數
        correctNum = 0;//累計正確題數
        hintSA_count = 1; hintEO_count = 3;
        c_hintSA_count = 0; c_hintEO_count = 0;
        wrongNum = 0;
        IsShowingResults = false;
        compete_theme = ManageLevel_C.level;
        RefreshConnectUI();

    }


    void Update() {
        if (PhotonNetwork.connected)
        {
            this.ConnectUiView.SetActive(false);
            /*
            if (PhotonNetwork.masterClient == null) {
                OnMasterClientSwitched(PhotonNetwork.player);
            }
            */
        }

        if (!PhotonNetwork.connected && !PhotonNetwork.connecting && !this.ConnectUiView.GetActive())
        {
            this.ConnectUiView.SetActive(true);
        }
        if (timerflag )
        {
            currentTime = (int)turnManager.RemainingSecondsInTurn;
            this.TimeText.text = currentTime.ToString();//顯示倒數秒數
        }
       
    }


    #region Implement IPunTurnManagerCallbacks
    /// <summary>Called when a turn begins (Master Client set a new Turn number).</summary>
    /// 
    public void OnTurnBegins(int turn)
    {

        //回合初始化
        this.StartCoroutine("initialTurn");
    }

    public void OnTurnCompleted(int obj)
    {
        Debug.Log("OnTurnCompleted: " + obj);
        timerflag = false;
        this.CalculateWinAndLoss();
        this.UpdateScores();
    }

    public void OnTurnTimeEnds(int obj)
    {
        if (!IsShowingResults)
        {
            Debug.Log("Time's up!");
            OnTurnCompleted(-1);
        }
    }

    // when a player moved (but did not finish the turn)
    public void OnPlayerMove(PhotonPlayer photonPlayer, int turn, object move)
    {
        Debug.Log("OnPlayerMove: " + photonPlayer + " turn: " + turn + " action: " + move.ToString());
        throw new NotImplementedException();
    }


    //when a player made the last/final move in a turn
    public void OnPlayerFinished(PhotonPlayer photonPlayer, int turn, object move)
    {
        Debug.Log("OnTurnFinished: " + photonPlayer + " turn: " + turn + " action: " + move.ToString());

        if (photonPlayer.IsLocal)
        {
            this.localTime = DateTime.Now;
            this.localSelection = move.ToString();
        }
        else
        {
            this.remoteTime = DateTime.Now;
            this.remoteSelection = move.ToString();
        }
    }

    IEnumerator OnEndTurn()
    //public void OnEndTurn()
    {
        if (this.turnManager.Turn < 10)
        {
            this.StartCoroutine("ShowResultsBeginNextTurnCoroutine");
        }
        else //競賽結束，顯示本次雙方分數
        {
            this.StartCoroutine("ShowResultsBeginNextTurnCoroutine");
            yield return new WaitForSeconds(2.5f);
            this.StartCoroutine("showResult");
        }
    }
    IEnumerator showResult() {//總排名
        GameObject[] PlayerLists = GameObject.FindGameObjectsWithTag("PlayerLists");//抓取玩家名單的物件，方便銷毀
        GameStartUI.SetActive(false);
        ResultUIView.SetActive(true);
        PhotonPlayer local = PhotonNetwork.player;
        int localRank = 0;

        for (int i = 0; i < PhotonNetwork.room.PlayerCount; i++)
        {
            if (player[i].NickName == local.NickName) localRank = i + 1;
            ResultUIView.GetComponentsInChildren<Text>()[0].text += player[i].NickName + "　分數:" + player[i].GetScore().ToString("D2") + "\n\n";
        }
        ResultUIView.GetComponentsInChildren<Text>()[1].text = c_hintSA_count.ToString();
        ResultUIView.GetComponentsInChildren<Text>()[2].text = c_hintEO_count.ToString();
        Button btn_learn = ResultUIView.GetComponentsInChildren<Button>()[0];
        Button btn_practice = ResultUIView.GetComponentsInChildren<Button>()[1];
        Button btn_compete = ResultUIView.GetComponentsInChildren<Button>()[2];
        Button btn_exit = ResultUIView.GetComponentsInChildren<Button>()[3];
        btn_learn.onClick.AddListener(delegate () { gameover(0, PlayerLists); });
        btn_practice.onClick.AddListener(delegate () { gameover(1, PlayerLists); });
        btn_compete.onClick.AddListener(delegate () { gameover(2, PlayerLists); });
        btn_exit.onClick.AddListener(delegate () { gameover(3, PlayerLists); });
        yield return new WaitForSeconds(0.1f);
        achievementState[1] = xmlprocess.setCompeteCountandTheme(ManageLevel_C.level);//對戰次數
        if (xmlprocess.setCompeteCorrectRecord(correctNum, wrongNum,ManageLevel_C.level) != null) achievementState[3] = xmlprocess.setCompeteCorrectRecord(correctNum, wrongNum,ManageLevel_C.level);//累積答對
        if (xmlprocess.setCompeteMaxCorrectRecord(max_correctNum) != null) achievementState[3] = xmlprocess.setCompeteMaxCorrectRecord(max_correctNum);//連續答對
        string[] s_state = xmlprocess.setCompeteScoreRecord(ManageLevel_C.level,c_hintSA_count, c_hintEO_count, local.GetScore(), localRank);//提示與分數排名
        if (s_state[0] != null) achievementState[4] = s_state[0];//有進步
        if (s_state[1] != null) achievementState[5] = s_state[1];//有刷新分數
        if (s_state[2] != null) achievementState[6] = s_state[2];//有進榜
    }
    #endregion

    public void StartTurn()
    {
        Debug.Log("start");
        if (this.turnManager.Turn == 0)
        {
            InitialGameUI();
        }
        //房主抓取題目、選項、當前回合數
        if (PhotonNetwork.isMasterClient)
        {
            this.turnManager.BeginTurn();
            this.turnManager.selectQues(collectConn.ques);
            if(ManageLevel_C.level != "integrate")
            {
                this.turnManager.randomOptions(collectConn.option);
            }
            
        }
        //Debug.Log("turn"+this.turnManager.Turn);
        this.question.text = "";
        this.question_ch.text = "";
        this.localSelection = "";
        this.remoteSelection = "";

    }
    public IEnumerator initialTurn()//每回合初始化
    {
        yield return new WaitForSeconds(0.5f);
        //存取新題目、選項、當前回合數
        quesInfo = this.turnManager.TurnQues;
        s_option = this.turnManager.TurnOption;
        TurnText.text = this.turnManager.Turn.ToString();
        xmlprocess.createRoundRecord(this.turnManager.Turn,quesInfo[0]);//創建新的回合紀錄

        //銷毀上一回合的卡片
        GameObject[] tmp_cards = GameObject.FindGameObjectsWithTag("card");
        if (tmp_cards.Length > 0)
        {
            for (int i = 0; i < tmp_cards.Length; i++)
            {
                Destroy(tmp_cards[i]);
            }
        }
        //產生卡牌
        if( ManageLevel_C.level == "amplification" || ManageLevel_C.level == "omission" )
        {
            createCard();
        }
        if( ManageLevel_C.level == "means" || ManageLevel_C.level == "conversion" || ManageLevel_C.level == "integrate")
        {
            createCardbyId();
        }
        
        cardgroup.SetActive(true);
        //播放聲音
        vol_pronun.clip = Resources.Load("Sound/" + quesInfo[1], typeof(AudioClip)) as AudioClip;
        vol_pronun.Play();

        timerflag = true;
        Debug.Log("關卡:"+ManageLevel_C.level);
        if( ManageLevel_C.level == "amplification")
        {
            this.turnManager.TurnDuration=20f; 
            currentTime = (int)this.turnManager.TurnDuration;
        }
        if( ManageLevel_C.level == "omission" )
        {
            this.turnManager.TurnDuration=30f;
            currentTime = (int)this.turnManager.TurnDuration;
        }
        if( ManageLevel_C.level == "means" || ManageLevel_C.level == "conversion" || ManageLevel_C.level == "integrate")
        {
            this.turnManager.TurnDuration=15f;
            currentTime = (int)this.turnManager.TurnDuration;
        }
        Debug.Log("倒計時:"+currentTime);
        this.TimeText.text = currentTime.ToString();
        TurnStartTime = DateTime.Now;
        IsShowingResults = false;
    }

    //建立卡牌
    void createCard()
    {
   
        ShowQuestion();
     
        int ans_pos = UnityEngine.Random.Range(0, cardCount);
        if (quesInfo != null && quesInfo.Length > 0)
        {

            for (int i = 0, j = -1; i < cardCount; i++)
            {
                GameObject cardObj = Instantiate(card);
                cardObj.gameObject.SetActive(true);

                // do {//如果選項與答案相同,則跳過抓下一個選項
                    j++;
                    optionInfo = s_option[j].Split(',');
                // } while (optionInfo[1] == quesInfo[1]);

                if (i == ans_pos)//如果當前位置為答案位置 
                {
                    cardObj.GetComponentInChildren<Text>().text = quesInfo[2];
                    cardObj.name = quesInfo[2];
                }
                else //不是答案的其餘選項
                {
                    if( quesInfo[2] == optionInfo[0] )//當答案與選項相同時 顯示最後一個選項
                    {
                        optionInfo = s_option[6].Split(',');
                        cardObj.GetComponentInChildren<Text>().text = optionInfo[0];
                        cardObj.name = optionInfo[0];
                    }
                    else
                    {
                        cardObj.GetComponentInChildren<Text>().text = optionInfo[0];
                        cardObj.name = optionInfo[0];
                    }
                }
                    cardObj.GetComponent<Button>().onClick.AddListener(delegate () { MakeTurn(cardObj.name); });
                    cardObj.transform.SetParent(cardgroup.transform);
                    cardObj.transform.localPosition = new Vector3(-80 + (i % 2) * 220, (i / 2) * -160+160, 0);
                    cardObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            }
        }
    }

//藉由id編號建立卡牌
    void createCardbyId()
    {
   
        ShowQuestion();
     
        int ans_pos = UnityEngine.Random.Range(0, cardCount-1);
        if (quesInfo != null && quesInfo.Length > 0)
        {
            int j=3;
            if(ManageLevel_C.level == "conversion" || ManageLevel_C.level == "integrate")
            {
                j=4;
            }
            for (int i = 0 ; i < cardCount; i++)
            {
                GameObject cardObj = Instantiate(card);
                cardObj.gameObject.SetActive(true);

                if (i == ans_pos)//如果當前位置為答案位置 
                {
                    cardObj.GetComponentInChildren<Text>().text = quesInfo[2];
                    cardObj.name = quesInfo[2];
                }
                else //不是答案的其餘選項
                {
                    cardObj.GetComponentInChildren<Text>().text = quesInfo[j];
                    cardObj.name = quesInfo[j];
                    j++;

                  
                }
                    cardObj.GetComponent<Button>().onClick.AddListener(delegate () { MakeTurn(cardObj.name); });
                    cardObj.transform.SetParent(cardgroup.transform);
                    cardObj.transform.localPosition = new Vector3(-80 + (i % 2) * 220, (i / 2) * -160+160, 0);
                    cardObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            }
        }
    }

    //set plaer's selection
    public void MakeTurn(string cardName)
    {
        ChooseCard.Play();
        this.turnManager.SendMove(cardName, true);
        //禁止修改答案
        GameObject[] tmp_cards = GameObject.FindGameObjectsWithTag("card");
        if (tmp_cards.Length > 0)
        {
            for (int i = 0; i < tmp_cards.Length; i++)
            {
                if (tmp_cards[i].name != cardName)
                {
                    //Debug.Log(tmp_cards[i]);
                    tmp_cards[i].GetComponent<Button>().interactable = false;
                    //tmp_cards[i].GetComponentsInChildren<Image>()[1].color = Color.gray;
                }
            }
        }

    }

    //作答耗費的時間
    private int DateDiff(DateTime DateTime1, DateTime DateTime2)
    {
        int dateDiff = 0;
        TimeSpan ts1 = new TimeSpan(DateTime1.Ticks);
        TimeSpan ts2 = new TimeSpan(DateTime2.Ticks);
        TimeSpan ts = ts1.Subtract(ts2).Duration();
        dateDiff = ts.Seconds;
        return dateDiff;
    }


    //判斷作答結果
    private void CalculateWinAndLoss()
    {
        if (this.localSelection == "")
        {
            this.result = ResultType.None;
            Debug.Log("You hadn't select");
            if (C_correctNum >= max_correctNum)
            {
                max_correctNum = C_correctNum;
                C_correctNum = -1;
            }
            wrongNum++;
            return;
        }

        if (this.remoteSelection == "")
        {
            this.result = ResultType.None;
        }

        if (this.localSelection == quesInfo[2])
        {
           this.result = ResultType.CorrectAns;
            Debug.Log("Correct answer!");
            C_correctNum++;
            correctNum ++;
            max_correctNum = C_correctNum;
        }
        else {
            this.result = ResultType.WrongAns;
            Debug.Log("Wrong answer");
            if (C_correctNum >= max_correctNum)
            {
                max_correctNum = C_correctNum;
                C_correctNum = -1;
            }
            wrongNum++;

        }
    }

    //計算得分
    private void UpdateScores()
    {
        PhotonPlayer local = PhotonNetwork.player;
        int spendTime = DateDiff(this.localTime, TurnStartTime);
        int restTime = (int)this.turnManager.TurnDuration - spendTime;//剩餘時間
        int _hintSA = xmlprocess.getRoundHintcount("hint_SA");//當回合使用顯示答案
        int _hintEO = xmlprocess.getRoundHintcount("hint_EO");//當回合使用提示排除一半選項
        string resultState = "";
        switch (this.result)
        {
            case ResultType.CorrectAns:
                PhotonNetwork.player.AddScore((int)(restTime * 0.8 + local.GetScore() * 0.15 - (_hintSA * 30) - (_hintEO * 1.5) + (PhotonNetwork.room.PlayerCount * 0.25)));//剩餘時間*0.8+原本分數*0.2-使用提示+房間人數*0.5
                resultState = "correct";
                break;
            case ResultType.None:
                PhotonNetwork.player.AddScore(-4);
                resultState = "none";
                break;
            case ResultType.WrongAns:
                PhotonNetwork.player.AddScore(-1);
                resultState = "wrong";
                break;
        }
        //Debug.Log("花費時間: "+ spendTime);
        achievementState[0] = xmlprocess.setRoundAns(resultState, spendTime);//答題迅速獎章
        StartCoroutine(UpdatePlayerTexts());
    }

    //更新即時排名
    IEnumerator UpdatePlayerTexts()
    {
        Debug.Log("Refresh the leadboard!");
        yield return new WaitForSeconds(0.5f);
        PhotonPlayer local = PhotonNetwork.player;
        player = PhotonNetwork.playerList;
        int localRank = 0;
        //依分數排序玩家清單
        for (int i = 0; i < PhotonNetwork.room.PlayerCount - 1; i++)
        {
            for (int j = i + 1; j < PhotonNetwork.room.PlayerCount; j++)
            {
                if (player[i].GetScore() < player[j].GetScore())
                {
                    PhotonPlayer tmp = player[j];
                    player[j] = player[i];
                    player[i] = tmp;
                }
            }
        }
        //更新排行榜UI與自己畫面的分數
        for (int i = 0; i < PhotonNetwork.room.PlayerCount; i++)
        {
            GameObject GameRank = GameObject.FindGameObjectWithTag("GameRank");
            GameRank.GetComponentsInChildren<Text>()[i].text = player[i].NickName + "　" + player[i].GetScore().ToString("D2") + "分";
            if (local.NickName == player[i].NickName) localRank = i + 1;
        }
        if (local != null)
        {
            this.LocalPlayerText.text = local.GetScore().ToString("D2");
        }
        xmlprocess.setRoundScore(local.GetScore(), localRank);
        //回合結束
        StartCoroutine(this.OnEndTurn());
    }

    //顯示當回合的答題UI
    public IEnumerator ShowResultsBeginNextTurnCoroutine()
    {
        //ButtonCanvasGroup.interactable = false;
        IsShowingResults = true;
        GameObject ResultMes = Instantiate(ShowResultMes);
        ResultMes.transform.SetParent(GameStartUI.transform);
        ResultMes.transform.localPosition = Vector3.zero;
        ResultMes.transform.localScale = Vector3.one;
        Image imgResult = ResultMes.GetComponentsInChildren<Image>()[1];
        Text textResult = ResultMes.GetComponentInChildren<Text>();


        switch (this.result)
        {
            case ResultType.None:
                imgResult.sprite = Resources.Load("Image/none", typeof(Sprite)) as Sprite;
                textResult.text = "你沒有選擇卡牌";
                break;
            case ResultType.CorrectAns:
                imgResult.sprite = Resources.Load("Image/correct", typeof(Sprite)) as Sprite;
                textResult.text = "答對囉！";

                break;
            case ResultType.WrongAns:
                imgResult.sprite = Resources.Load("Image/wrong", typeof(Sprite)) as Sprite;
                textResult.text = "正確答案:"+ quesInfo[2];

                break;
        }
        yield return new WaitForSeconds(1.0f);
        Destroy(ResultMes);
        if (this.turnManager.Turn < 10)
        {
            this.StartTurn();
        }
    }

    #region Recheck connect and Initialize UI
    void RefreshConnectUI()
    {
        this.ConnectUiView.SetActive(!PhotonNetwork.inRoom);//如果還沒進房間則顯示連線畫面

        this.WaitingUI.SetActive(PhotonNetwork.inRoom);
        if (GameStartUI.GetActive())
        {
            this.GameStartUI.SetActive(false);
        }
    }

    void RefreshWaitUI() {
        if (PhotonNetwork.room.PlayerCount <= 4)
        {
            if (PhotonNetwork.isMasterClient)
            {
                //房主才有遊戲開始的按鈕
                btn_gamestart.gameObject.SetActive(true);
                btn_gamestart.onClick.AddListener(ClickGameStart);
            }
            else
            {
                btn_gamestart.gameObject.SetActive(false);
            }
            Debug.Log("Waiting for another player");
        }


        PhotonPlayer hostPlayer = PhotonNetwork.masterClient;
        GameObject HostInfo = GameObject.FindGameObjectWithTag("Host");
        HostInfo.GetComponentsInChildren<Text>()[0].text = hostPlayer.NickName;

        //Initialize players'name
        for (int i = 1; i < 4; i++)
        {
            GameObject PlayerInfo = GameObject.FindGameObjectWithTag("Player" + i);
            PlayerInfo.GetComponentsInChildren<Text>()[0].text = "";
        }

        if (PhotonNetwork.room.PlayerCount > 1)
        {
            for (int i = 0,j=1; i < PhotonNetwork.room.PlayerCount; i++)
            {
                PhotonPlayer[] waitroom_player = PhotonNetwork.playerList;
                GameObject PlayerInfo = GameObject.FindGameObjectWithTag("Player" + j);
                if (waitroom_player[i].NickName!= hostPlayer.NickName) {
                    Debug.Log(waitroom_player[i].NickName);
                    PlayerInfo.GetComponentsInChildren<Text>()[0].text = waitroom_player[i].NickName;
                    j++;
                }
            }
        }
    }

    void InitialGameUI() {
        //初次進入進行遊戲畫面初始化

        btn_hintSA = this.GameStartUI.GetComponentsInChildren<Button>()[0];
        btn_hintEO = this.GameStartUI.GetComponentsInChildren<Button>()[1];
        //提示按鈕監聽事件
        btn_hintSA.onClick.AddListener(ShowAnswer);
        btn_hintEO.onClick.AddListener(ExcludeOption);

        for (int i = 0; i < PhotonNetwork.room.PlayerCount; i++)
        {
            PhotonPlayer local = PhotonNetwork.player;
            PhotonNetwork.playerList[i].SetScore(0);//重置玩家分數
            LocalPlayerText.text = local.GetScore().ToString("D2");
            player = PhotonNetwork.playerList;
            //player[i].SetScore(0);

            Text remote = Instantiate(RemotePlayerText);
            GameObject GameRank = GameObject.FindGameObjectWithTag("GameRank");
            remote.transform.SetParent(GameRank.transform);
            remote.transform.localPosition = new Vector3(28, - i * 80+140, 0);
            remote.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            remote.name = (i+1)+"";
            remote.text = player[i].NickName + "　" + player[i].GetScore().ToString("D2") + "分";

        }
        xmlprocess.createCompeteRecord(ManageLevel_C.level);
        xmlprocess.ScceneHistoryRecord("StartCompete", DateTime.Now.ToString("HH:mm:ss"));
    }

    void gameover(int sceneNum,GameObject [] PlayerLists) {
        ClickBtn.Play();
        ExitGame(sceneNum,PlayerLists);
    }

    #endregion

    #region Button Event

    public void ClickGameStart()
    {
        ClickBtn.Play();
        //if (PhotonNetwork.room.PlayerCount== 4)
        // if (PhotonNetwork.room.PlayerCount>=2 && PhotonNetwork.room.PlayerCount<=4)
        if (PhotonNetwork.room.PlayerCount>=1 && PhotonNetwork.room.PlayerCount<=4)
        {
            PhotonNetwork.room.IsOpen = false;
            PhotonNetwork.room.IsVisible = false;
            this.photonView.RPC("GameStart", PhotonTargets.All);

        }
        else
        {
            Debug.Log("Player isn't enough.");
        }
    }

   void ShowAnswer(){
    ClickBtn.Play();

     for(int i=2 ; i < 6 ; i++)
        {
            if( GetComponentsInChildren<Button>()[i].name != quesInfo[2] )
            {
                GetComponentsInChildren<Button>()[i].interactable = false;
                Debug.Log("選項:"+GetComponentsInChildren<Button>()[i].name);
            }
        
        }

        c_hintSA_count = c_hintSA_count+1;
        btn_hintSA.GetComponentsInChildren<Text>()[0].text = c_hintSA_count + "次";
        xmlprocess.setRoundHintcount("hint_SA", c_hintSA_count);
        if (c_hintSA_count == hintSA_count)
        {
            btn_hintSA.interactable = false;
        }

   }

    void ExcludeOption() {
        ClickBtn.Play();

        for(int i=0 ; i < 2 ; i++)
        {
            int rand = UnityEngine.Random.Range(2, 6);
            while( GetComponentsInChildren<Button>()[rand].name == quesInfo[2] || GetComponentsInChildren<Button>()[rand].interactable == false )
            {
                rand = UnityEngine.Random.Range(2, 6);
                 // Debug.Log("迴圈選項名字: "+rand);
            }
            GetComponentsInChildren<Button>()[rand].interactable = false;
            // Debug.Log("選項名字: "+rand);
        }

        c_hintEO_count = c_hintEO_count+1;
        btn_hintEO.GetComponentsInChildren<Text>()[0].text = c_hintEO_count + "次";
        xmlprocess.setRoundHintcount("hint_EO", c_hintEO_count);
        if (c_hintEO_count == hintEO_count)
        {
            btn_hintEO.interactable = false;
        }
    }

    void ShowQuestion() {
        // ClickBtn.Play();
        if( ManageLevel_C.level == "means" )
        {
            
            quesInfo[1] = quesInfo[1].Replace('/', ',');//英文題目
            this.question.text = quesInfo[1];

        }
        else
        {
            
            quesInfo[3] = quesInfo[3].Replace('/', ',');//英文題目
            this.question.text = quesInfo[3];
            quesInfo[1] = quesInfo[1].Replace('/', ',');//中文題目
            this.question_ch.text = quesInfo[1];
        }
        
        // Debug.Log("ShowTranslation: "+quesInfo[0]+","+quesInfo[1]+","+quesInfo[2]);
       
    }

    void ExitGame()//等待時離開
    {
        ClickBtn.Play();
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("CompeteArea");
    }


    void ExitGame(int sceneNum ,GameObject [] PlayerLists)//遊戲結束時離開
    {
        /*--------------------------*/
        //銷毀排行榜的玩家名單物件
        if (PlayerLists.Length > 0)
        {
            for (int i = 0; i < PlayerLists.Length; i++)
            {
                Destroy(PlayerLists[i]);
            }
        }
        switch (sceneNum) {
            case 0://learning
                PhotonNetwork.Disconnect();
                xmlprocess.ScceneHistoryRecord("Learning", DateTime.Now.ToString("HH:mm:ss"));
                SceneManager.LoadScene("Learning_Level");
                break;
            case 1://practice
                PhotonNetwork.Disconnect();
                xmlprocess.ScceneHistoryRecord("Practice", DateTime.Now.ToString("HH:mm:ss"));
                SceneManager.LoadScene("Practice_Level");
                break;
            case 2://compete
                PhotonNetwork.Disconnect();
                xmlprocess.ScceneHistoryRecord("Compete", DateTime.Now.ToString("HH:mm:ss"));
                SceneManager.LoadScene("Compete_Level");
                break;
            case 3://exit
                PhotonNetwork.Disconnect();
                SceneManager.LoadScene("Home");
                break;
        }
        //PhotonNetwork.LeaveRoom(false);
        //PhotonNetwork.Disconnect();
    }

    [PunRPC]
    void GameStart()
    {
        if (this.turnManager.Turn == 0)
        {
            // when the room has two players, start the first turn (later on, joining players won't trigger a turn)
            this.WaitingUI.SetActive(false);
            this.GameStartUI.SetActive(true);
            this.StartTurn();

        }
    }

    #endregion

    public override void OnJoinedRoom()
    {
        RefreshConnectUI();
        if (this.WaitingUI.GetActive())
        {
            btn_gamestart = this.WaitingUI.GetComponentsInChildren<Button>()[0];
            btn_Wexit = this.WaitingUI.GetComponentsInChildren<Button>()[1];
            btn_Wexit.onClick.AddListener(ExitGame);
        }
        RefreshWaitUI();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom (local)");
        RefreshConnectUI();

    }

    public override void OnMasterClientSwitched(PhotonPlayer player)
    {
        Debug.Log("OnMasterClientSwitchedto: " + PhotonNetwork.masterClient.NickName);
        string message;
        InRoomChat chatComponent = GetComponent<InRoomChat>();  // if we find a InRoomChat component, we print out a short message
        if (chatComponent != null)
        {
            // to check if this client is the new master...
            if (player.IsLocal)
            {
                message = "You are Master Client now.";
            }
            else
            {
                message = player.NickName + " is Master Client now.";
            }


            chatComponent.AddLine(message); // the Chat method is a RPC. as we don't want to send an RPC and neither create a PhotonMessageInfo, lets call AddLine()
        }
        RefreshWaitUI();
    }



    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log("Other player arrived");

        GameObject PlayerInfo = GameObject.FindGameObjectWithTag("Player"+(PhotonNetwork.room.PlayerCount-1));
        PlayerInfo.GetComponentsInChildren<Text>()[0].text = newPlayer.NickName;
    }


    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        RefreshConnectUI();
        RefreshWaitUI();
        Debug.Log("Other player disconnected! " + otherPlayer.ToStringFull());
    }

    public override void OnDisconnectedFromPhoton()
    {
        RefreshConnectUI();
        Debug.Log("OnFailedToConnectToPhoton");
    }


}
