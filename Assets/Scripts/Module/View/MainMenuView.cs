/*
* ┌──────────────────────────────────┐
* │  描    述: 主界面视图                      
* │  类    名: MainMenuView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Common.Defines;
using GameProtocol;
using Module.Loading;
using MVC;
using MVC.Extensions;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class MainMenuView : BaseView
    {
        private Button startGameBtn;
        
        [Header("注册界面")]
        private TMP_InputField accountRegister;
        private TMP_InputField passwordRegister;
        private Button registerBtn;
        private Toggle acTogRegister;
        
        [Header("登录界面")]
        private TMP_InputField accountLogin;
        private TMP_InputField passwordLogin;
        private Button loginBtn;
        private Button forgotPasswordBtn;
        private Toggle acTogLogin;
        
        private GameObject loginView;
        private GameObject registerView;
        
        protected override void OnAwake()
        {
            startGameBtn = Find<Button>("Btn_start");
            accountLogin = Find<TMP_InputField>("loginView/Txt_account");
            passwordLogin = Find<TMP_InputField>("loginView/Txt_password");
            loginBtn = Find<Button>("loginView/btnsArea/Btn_login");
            forgotPasswordBtn = Find<Button>("loginView/btnsArea/Btn_forget");
            acTogLogin = Find<Toggle>("loginView/btnsArea/Tog_ac");
            
            accountRegister = Find<TMP_InputField>("registerView/Txt_account");
            passwordRegister = Find<TMP_InputField>("registerView/Txt_password");
            registerBtn = Find<Button>("registerView/btnsArea/Btn_register");
            acTogRegister = Find<Toggle>("registerView/btnsArea/Tog_ac");
            
            loginView = Find("loginView").gameObject;
            registerView = Find("registerView").gameObject;
        }

        protected override void OnStart()
        {
            loginBtn.onClick.AddListener(onLoginBtnClick);
            registerBtn.onClick.AddListener(onRegisterBtnClick);
            startGameBtn.onClick.AddListener(onStartGameBtnClick);
            
            //注册回调
            GameApp.NetworkManager.AddMessageHandler(ActionCode.Login, onLoginCallback);
            GameApp.NetworkManager.AddMessageHandler(ActionCode.Logon, onRegisterCallback);
        }

        protected override void OnDestroy()
        {
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.Login, onLoginCallback);
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.Logon, onRegisterCallback);
        }

        private void onLoginBtnClick()
        {
            //构造Protobuf大包
            MainPack pack = new MainPack();
            pack.RequestCode = RequestCode.User;
            pack.ActionCode = ActionCode.Login;
            pack.LoginPack = new LoginPack()
            {
                Username = accountLogin.text,
                Password = passwordLogin.text
            };
            
            GameApp.NetworkManager.Send(pack);
        }

        private void onLoginCallback(MainPack pack)
        {
            if (pack.ReturnCode == ReturnCode.Succeed)
            {
                Debug.Log($"登陆成功，欢迎 {pack.LoginPack?.Username??accountLogin.text}!");
                
                loginView.SetActive(false);
                GameApp.GameDataManager.SetPlayerName(accountLogin.text);
                GameApp.GameDataManager.isConnected = true;
            }
            else
            {
                Debug.LogError($"登陆失败，错误码: {pack.StrMsg}");
                //TODO：显示错误提示
            }
        }

        private void onRegisterBtnClick()
        {
            Debug.Log("点击了注册按钮,尝试注册中...");
            
            MainPack pack = new MainPack();
            pack.RequestCode = RequestCode.User;
            pack.ActionCode = ActionCode.Logon;
            pack.LoginPack = new LoginPack()
            {
                Username = accountRegister.text,
                Password = passwordRegister.text
            };
            
            GameApp.NetworkManager.Send(pack);
        }

        private void onRegisterCallback(MainPack pack)
        {
            if (pack.ReturnCode == ReturnCode.Succeed)
            {
                Debug.Log($"注册成功 {pack.LoginPack?.Username??accountLogin.text}!");
                
                //TODO：显示注册成功提示，并切换到登录界面
                registerView.SetActive(false);
                loginView.SetActive(true);
            }
            else
            {
                Debug.LogError($"注册失败，错误码: {pack.StrMsg}");
                //TODO：显示错误提示
            }
        }

        private void onStartGameBtnClick()
        {
            if (!GameApp.GameDataManager.isServerOnline)
            {
                GameApp.ViewManager.Open(ViewType.NoticeView, "服务器暂未开放", new Action(() =>
                    {
                        GameApp.ViewManager.CloseAll();
                        ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
                    }),
                    new Action(() =>
                    {
                        GameApp.ViewManager.CloseAll();
                        ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
                    }));
                return;
            }

            if (!GameApp.GameDataManager.isConnected)
            {
                GameApp.ViewManager.Open(ViewType.NoticeView, "失去连接,确认重新连接?", new Action(() =>
                    {
                        Debug.Log("正在重新连接...");
                        GameApp.NetworkManager.Connect();
                    }),
                    new Action(() =>
                    {
                        Debug.Log("取消重新连接");
                        GameApp.ViewManager.CloseAll();
                        ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
                    }));
                return;
            }
            
            
            GameApp.ViewManager.Close(ViewType.MainMenuView);
            ViewExtensions.LoadScene(this, SceneDefines.Game,(() =>
            {
                GameApp.ViewManager.Open(ViewType.GameView);
            }));
            
        }
    }
}