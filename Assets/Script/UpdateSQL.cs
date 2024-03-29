﻿using UnityEngine;
using System.Collections;
using System.Xml.Linq;
using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Xml;
using System.IO;
using System.Text;
using edu.ncu.list.util;
using UnityEngine.UI;

public class UpdateSQL : MonoBehaviour {
    public Button test;

    protected Xmlprocess xmlprocess;
    MySQLAccess mySQLAccess;
    public XmlDocument xmlDoc;

    //private string serverlink = "http://140.115.126.167/translate/uploadData.php";


    public int stateBG;
    static string host = "140.115.126.167";
    static string id = "leelu";
    static string pwd = "lu293533";
    static string database = "translation";
    public string user_highscore = "";

    string userID = "";

    void Start () {
        mySQLAccess = new MySQLAccess(host, id, pwd, database);
        xmlprocess = new Xmlprocess();
        StartCoroutine("ReloadXMLtoDB", 0.5F);

    }

    IEnumerator ReloadXMLtoDB(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
       // xmlDoc = new XmlDocument();
        //xmlDoc.Load(xmlprocess.getPath());
        XmlNode node = xmlprocess.xmlDoc.SelectSingleNode("Loadfile/User");
        XmlElement element = (XmlElement)node;
        XmlAttribute attribute = element.GetAttributeNode("ID");
        userID = attribute.Value;

        // /*查詢玩家練習排名*/
        // DataSet ds = new DataSet();
        // ds = mySQLAccess.Select("translation.practice_task", "MAX(highscore)", "user_id", "=", userID.ToString());
        // user_highscore = ds.Tables[0].Rows[0][0].ToString();

        /*等級*/
        string userlevel = element.GetAttributeNode("level").Value;
        mySQLAccess.UpdateInto("member", "level", userlevel, "user_id", userID.ToString());

        /*學習狀態 learning_task*/
        string[] learning_task_col = new string[6];
        learning_task_col[0] = "user_id";
        learning_task_col[1] = "review_means_count";
        learning_task_col[2] = "learning_means_count";
        learning_task_col[3] = "review_conversion_count";
        learning_task_col[4] = "learning_conversion_count";
        learning_task_col[5] = "uploadTime";

        node = xmlprocess.xmlDoc.SelectSingleNode("Loadfile/User/learning");
        element = (XmlElement)node;
        string[] learning_task = new string[6];
        learning_task[0] = userID;
        learning_task[1] = element.GetAttributeNode("review_means_count").Value;
        learning_task[2] = element.GetAttributeNode("learning_means_count").Value;
        learning_task[3] = element.GetAttributeNode("review_conversion_count").Value;
        learning_task[4] = element.GetAttributeNode("learning_conversion_count").Value;
        learning_task[5] = DateTime.Now.ToString();
        mySQLAccess.InsertInto("learning_task", learning_task_col,learning_task);

        /*練習狀態 practice_task*/
        // string[] practice_task_col = new string[9];
        // practice_task_col[0] = "user_id";
        // practice_task_col[1] = "practice_theme";
        // practice_task_col[2] = "practice_level";
        // practice_task_col[3] = "practice_count";
        // practice_task_col[4] = "practice_correct";
        // practice_task_col[5] = "practice_wrong";
        // practice_task_col[6] = "practice_improve";
        // practice_task_col[7] = "highscore";
        // practice_task_col[8] = "uploadTime";

        // node = xmlprocess.xmlDoc.SelectSingleNode("Loadfile/User/practice");
        // element = (XmlElement)node;
        // string[] practice_task = new string[9];
        // practice_task[0] = userID;
        // practice_task[1] = element.GetAttributeNode("practice_theme").Value;
        // practice_task[2] = element.GetAttributeNode("practice_level").Value;
        // practice_task[3] = element.GetAttributeNode("practice_count").Value;
        // practice_task[4] = element.GetAttributeNode("practice_correct").Value;
        // practice_task[5] = element.GetAttributeNode("practice_wrong").Value;
        // practice_task[6] = element.GetAttributeNode("practice_improve").Value;
        // practice_task[7] = element.GetAttributeNode("highscore").Value;
        // practice_task[8] = DateTime.Now.ToString();
        // mySQLAccess.InsertInto("practice_task", practice_task_col,practice_task);

        /*對戰狀態*/
        string[] compete_task_col = new string[8];
        compete_task_col[0] = "user_id";
        compete_task_col[1] = "compete_theme";
        compete_task_col[2] = "compete_count";
        compete_task_col[3] = "compete_correct";
        compete_task_col[4] = "compete_wrong";
        compete_task_col[5] = "compete_improve";
        compete_task_col[6] = "highscore";
        compete_task_col[7] = "uploadTime";

        string[] compete_theme = new string[] {"integrate","means","conversion"};
        for( int i=0; i<3; i++ )
        {
            node = xmlprocess.xmlDoc.SelectSingleNode("Loadfile/User/compete_"+compete_theme[i]);
            element = (XmlElement)node;
            string[] compete_task = new string[8];
            compete_task[0] = userID;
            compete_task[1] = element.GetAttributeNode("compete_theme").Value;
            compete_task[2] = element.GetAttributeNode("compete_count").Value;
            compete_task[3] = element.GetAttributeNode("compete_correct").Value;
            compete_task[4] = element.GetAttributeNode("compete_wrong").Value;
            compete_task[5] = element.GetAttributeNode("compete_improve").Value;
            compete_task[6] = element.GetAttributeNode("highscore").Value;
            compete_task[7] = DateTime.Now.ToString();
            mySQLAccess.InsertInto("compete_task", compete_task_col, compete_task);
        }

        /*學習類獎章紀錄*/
        string[] badge_learning_col = new string[4];
        badge_learning_col[0] = "user_id";
        badge_learning_col[1] = "badge_id";
        badge_learning_col[2] = "badge_level";
        badge_learning_col[3] = "uploadTime";
        for (int i = 1; i <= 5; i++)//學習類有5種獎章
        {
            string[] badge_learning = new string[4];
            badge_learning[0] = userID;
            badge_learning[1] = i.ToString();
            node = xmlprocess.xmlDoc.SelectSingleNode("Loadfile/badge_record/badge_learning/badge"+i);
            element = (XmlElement)node;
            badge_learning[2] = element.GetAttributeNode("level").Value;
            badge_learning[3] = DateTime.Now.ToString();
            mySQLAccess.InsertInto("badge_record", badge_learning_col, badge_learning);
        }
        /*對戰類獎章紀錄*/
        // for (int i = 6; i <= 12; i++)//對戰類有7種獎章
        // {
        //     string[] badge_compete = new string[4];
        //     badge_compete[0] = userID;
        //     badge_compete[1] = i.ToString();
        //     node = xmlprocess.xmlDoc.SelectSingleNode("Loadfile/badge_record/badge_compete/badge" + i);
        //     element = (XmlElement)node;
        //     badge_compete[2] = element.GetAttributeNode("level").Value;
        //     badge_compete[3] = DateTime.Now.ToString();
        //     mySQLAccess.InsertInto("badge_record", badge_learning_col, badge_compete);
        // }

        /*點擊成就頁面紀錄*/
        // string[] touch_record_col = new string[4];
        // touch_record_col[0] = "user_id";
        // touch_record_col[1] = "clickcount";
        // touch_record_col[2] = "showcount";
        // touch_record_col[3] = "uploadTime";

        // node = xmlprocess.xmlDoc.SelectSingleNode("Loadfile/touch_history/touch_achieve");
        // element = (XmlElement)node;
        // string[] touch_record = new string[4];
        // touch_record[0] = userID;
        // touch_record[1] = element.GetAttributeNode("clickcount").Value;
        // touch_record[2] = element.GetAttributeNode("showcount").Value;
        // touch_record[3] = DateTime.Now.ToString();
        // mySQLAccess.InsertInto("touch_record", touch_record_col, touch_record);

        /*練習紀錄 practice_record*/
        string[] practice_record_col = new string[9];
        practice_record_col[0] = "user_id";
        practice_record_col[1] = "theme";
        practice_record_col[2] = "level";
        practice_record_col[3] = "startTime";
        practice_record_col[4] = "endTime";
        practice_record_col[5] = "score";
        practice_record_col[6] = "correct";
        practice_record_col[7] = "maxcorrect";
        practice_record_col[8] = "uploadTime";

        XmlNodeList nodelist = xmlprocess.xmlDoc.SelectNodes("//practice_record");
        foreach (XmlNode itemsNode in nodelist)
        {
            element = (XmlElement)itemsNode;
            string[] practice_record = new string[9];
            practice_record[0] = userID;
            practice_record[1] = element.GetAttributeNode("theme").Value;
            practice_record[2] = element.GetAttributeNode("level").Value; 
            practice_record[3] = element.GetAttributeNode("startTime").Value; 
            practice_record[4] = element.GetAttributeNode("endTime").Value; 
            practice_record[5] = element.GetAttributeNode("score").Value; 
            practice_record[6] = element.GetAttributeNode("correct").Value; 
            practice_record[7] = element.GetAttributeNode("maxcorrect").Value; 
            practice_record[8] = DateTime.Now.ToString();
            mySQLAccess.InsertInto("practice_record", practice_record_col, practice_record);

            
        }

        /*對戰紀錄*/
        string[] compete_record_col = new string[12];
        compete_record_col[0] = "user_id";
        compete_record_col[1] = "theme";
        compete_record_col[2] = "compete_id";
        compete_record_col[3] = "startTime";
        compete_record_col[4] = "endTime";
        compete_record_col[5] = "hint_SA";
        compete_record_col[6] = "hint_EO";
        compete_record_col[7] = "correct";
        compete_record_col[8] = "maxcorrect";
        compete_record_col[9] = "score";
        compete_record_col[10] = "rank";
        compete_record_col[11] = "uploadTime";

        nodelist = xmlprocess.xmlDoc.SelectNodes("//compete_record ");
        foreach (XmlNode itemsNode in nodelist)
        {
            element = (XmlElement)itemsNode;
            string[] compete_record = new string[12];
            compete_record[0] = userID;
            compete_record[1] = element.GetAttributeNode("theme").Value;
            compete_record[2] = element.GetAttributeNode("compete_id").Value;
            compete_record[3] = element.GetAttributeNode("startTime").Value; 
            compete_record[4] = element.GetAttributeNode("endTime").Value; 
            compete_record[5] = element.GetAttributeNode("hint_SA").Value;
            compete_record[6] = element.GetAttributeNode("hint_EO").Value; 
            compete_record[7] = element.GetAttributeNode("correct").Value; 
            compete_record[8] = element.GetAttributeNode("maxcorrect").Value; 
            compete_record[9] = element.GetAttributeNode("score").Value; 
            compete_record[10] = element.GetAttributeNode("rank").Value; 
            compete_record[11] = DateTime.Now.ToString();
            mySQLAccess.InsertInto("compete_record", compete_record_col, compete_record);
        }

        /*回合紀錄*/
        string[] round_record_col = new string[11];
        round_record_col[0] = "user_id";
        round_record_col[1] = "compete_id";
        round_record_col[2] = "round_id";
        round_record_col[3] = "ques_id";
        round_record_col[4] = "ans_state";
        round_record_col[5] = "duration";
        round_record_col[6] = "hint_SA";
        round_record_col[7] = "hint_EO";
        round_record_col[8] = "score";
        round_record_col[9] = "rank";
        round_record_col[10] = "uploadTime";

        nodelist = xmlprocess.xmlDoc.SelectNodes("//round_record ");
        foreach (XmlNode itemsNode in nodelist)
        {
            element = (XmlElement)itemsNode;
            string[] round_record = new string[11];
            round_record[0] = userID;
            round_record[1] = element.GetAttributeNode("compete_id").Value;
            round_record[2] = element.GetAttributeNode("round_id").Value;
            round_record[3] = element.GetAttributeNode("ques_id").Value; 
            round_record[4] = element.GetAttributeNode("ans_state").Value; 
            round_record[5] = element.GetAttributeNode("duration").Value;
            round_record[6] = element.GetAttributeNode("hint_SA").Value; 
            round_record[7] = element.GetAttributeNode("hint_EO").Value; 
            round_record[8] = element.GetAttributeNode("score").Value;
            round_record[9] = element.GetAttributeNode("rank").Value;
            round_record[10] = DateTime.Now.ToString();
            mySQLAccess.InsertInto("round_record", round_record_col, round_record);
        }

        /*場景紀錄*/
        string[] scene_record_col = new string[4];
        scene_record_col[0] = "user_id";
        scene_record_col[1] = "scene";
        scene_record_col[2] = "startTime";
        scene_record_col[3] = "uploadTime";

        nodelist = xmlprocess.xmlDoc.SelectNodes("//scene_record ");
        foreach (XmlNode itemsNode in nodelist)
        {
            element = (XmlElement)itemsNode;
            string[] scene_record = new string[4];
            scene_record[0] = userID;
            scene_record[1] = element.GetAttributeNode("scene").Value;
            scene_record[2] = element.GetAttributeNode("startTime").Value;
            scene_record[3] = DateTime.Now.ToString();
            mySQLAccess.InsertInto("scene_record", scene_record_col, scene_record);

            
        }
        Application.Quit();
    }

     public IEnumerator UpdatePractice_task(string theme,string level)
    {
        mySQLAccess = new MySQLAccess(host, id, pwd, database);
        xmlprocess = new Xmlprocess();
        yield return new WaitForSeconds(0.5f);
       // xmlDoc = new XmlDocument();
        //xmlDoc.Load(xmlprocess.getPath());
        XmlNode node = xmlprocess.xmlDoc.SelectSingleNode("Loadfile/User");
        XmlElement element = (XmlElement)node;
        XmlAttribute attribute = element.GetAttributeNode("ID");
        userID = attribute.Value;


        string[] practice_task_col = new string[9];
        practice_task_col[0] = "user_id";
        practice_task_col[1] = "practice_theme";
        practice_task_col[2] = "practice_level";
        practice_task_col[3] = "practice_count";
        practice_task_col[4] = "practice_correct";
        practice_task_col[5] = "practice_wrong";
        practice_task_col[6] = "practice_improve";
        practice_task_col[7] = "highscore";
        practice_task_col[8] = "uploadTime";

        node = xmlprocess.xmlDoc.SelectSingleNode("Loadfile/User/practice_"+theme+"_"+level);
        element = (XmlElement)node;
        string[] practice_task = new string[9];
        practice_task[0] = userID;
        practice_task[1] = element.GetAttributeNode("practice_theme").Value;
        practice_task[2] = element.GetAttributeNode("practice_level").Value;
        practice_task[3] = element.GetAttributeNode("practice_count").Value;
        practice_task[4] = element.GetAttributeNode("practice_correct").Value;
        practice_task[5] = element.GetAttributeNode("practice_wrong").Value;
        practice_task[6] = element.GetAttributeNode("practice_improve").Value;
        practice_task[7] = element.GetAttributeNode("highscore").Value;
        practice_task[8] = DateTime.Now.ToString();
        mySQLAccess.InsertInto("practice_task", practice_task_col,practice_task);

        /*查詢玩家練習排名*/
        DataSet ds = new DataSet();
        ds = mySQLAccess.Select("translation.practice_task", "MAX(highscore)", "user_id", "=", userID.ToString(),
            "practice_theme","=",theme,"practice_level","=",level);
        user_highscore = ds.Tables[0].Rows[0][0].ToString();

    }

    public static void OnApplicationQuit()
    {
        MySQLAccess.Close();
        
    }



}
