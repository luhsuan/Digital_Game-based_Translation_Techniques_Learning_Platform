﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AccountViewer : MonoBehaviour {

    AccountManager am;
    Text Loadingrmsg;
    AudioSource ClickBtn;


    #region Login UI
    static InputField login_ac, login_pw;
    static string user_name;
    public Text errormsg;
    public Button btn_log, btn_reg;
    public string[] logininfo;
    #endregion

    #region Register UI
    static InputField reg_id, reg_pw, reg_name;
    private string reg_sex;
    private Text errormsg2;
    public Button btn_register, btn_back;
    public string[] registerinfo;
    #endregion

    private void Awake()
    {
        am = new AccountManager();
        //Screen.fullScreen = true;
    }

    void Start() {
        login_ac = GetComponentsInChildren<InputField>()[0];
        login_pw = GetComponentsInChildren<InputField>()[1];
        ClickBtn = GetComponents<AudioSource>()[1];
        btn_log.onClick.AddListener(confirmlogin);
        btn_reg.onClick.AddListener(showregister);
        UIManager.Instance.ShowPanel("UI_ShowMes");
        Loadingrmsg = GetComponentsInChildren<Text>()[6];
        UIManager.Instance.TogglePanel("UI_ShowMes", false);
    }

    public void confirmlogin()
    {
        ClickBtn.Play();

        if (login_ac.text != "")
        {
            if (login_pw.text != "")
            {
                logininfo = new string[] { login_ac.text, login_pw.text };
                StartCoroutine(login());
            }
            else
            {
                showerror("密碼不可為空");
            }
        }
        else
        {
            showerror("帳號不可為空");
        }
    }

    IEnumerator login()
    {
		StartCoroutine(am.CheckLogin("login.php", logininfo));
        StartCoroutine(waitload());
        yield return new WaitForSeconds(1f);
        if (am.state == 1)
        {
            //showerror("登入成功");
            SceneManager.LoadScene("Home");
        }
        else if (am.state == 0)
        {
            showerror("帳號或密碼不正確");
            //showerror(am.s_state);
        }
        else if (am.state == 2)
        {
            showerror("連線失敗");
        }
        else if (am.state == 3)
        {
            showerror("發生錯誤");
        }
    }


    public void showLogin() {
        ClickBtn.Play();
        UIManager.Instance.ClosePanel("L_RegisterUI");
    }

    public void showregister()
    {
        ClickBtn.Play();
        UIManager.Instance.ShowPanel("L_RegisterUI");
        reg_id = GetComponentsInChildren<InputField>()[2];
        reg_pw = GetComponentsInChildren<InputField>()[3];
        reg_name = GetComponentsInChildren<InputField>()[4];
        errormsg2 = GetComponentsInChildren<Text>()[17];
        btn_register = GetComponentsInChildren<Button>()[2];
        btn_back = GetComponentsInChildren<Button>()[3];

        btn_register.onClick.AddListener(confirmregister);
        btn_back.onClick.AddListener(showLogin);
    }

    public void confirmregister() {
        ClickBtn.Play();
        if (reg_id.text != "")
        {
            if (reg_id.text.Substring(0, 1) != "0")
            {
                if (reg_pw.text != "")
                {
                    if (reg_name.text != "")
                    {
                        if (GetComponentsInChildren<Toggle>()[0].isOn)
                        {
                            reg_sex = "0"; //"boy";
                        }
                        else
                        {
                            reg_sex = "1";// "girl";
                        }
                        registerinfo = new string[] { reg_id.text, reg_pw.text, reg_name.text, reg_sex };
                        StartCoroutine(register());
                    }
                    else
                    {
                        showerror("名稱不可為空");
                    }
                }
                else
                {
                    showerror("密碼不可為空");
                }
            }
            else { 
            showerror("帳號不可為0開頭");
             }
        }
        else
        {
            showerror("帳號不可為空");
        }
    }

    IEnumerator register()
    {
        StartCoroutine(am.CheckRegister("register.php", registerinfo));//此處用php新增帳戶
        StartCoroutine(waitload());
        yield return new WaitForSeconds(1f);

        if (am.state == 0) {
            SceneManager.LoadScene("Home");
        }
        else if (am.state == 1)
        {
            showerror("帳號已被使用");
        }

    }


    IEnumerator waitload()
    {
        UIManager.Instance.TogglePanel("UI_ShowMes", true);
        Loadingrmsg.text = "載入中......";
        yield return new WaitForSeconds(0.8f);
        UIManager.Instance.TogglePanel("UI_ShowMes", false);
    }

    void showerror(string err)
    {
        if (errormsg2 == null)
        {
            errormsg.text = err;
        }
        else
        {
            errormsg2.text = err;
        }
    }
}

