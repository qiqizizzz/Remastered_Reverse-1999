/*
* ┌──────────────────────────────────┐
* │  描    述: 主界面视图                      
* │  类    名: MainMenuView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using GameProtocol;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class MainMenuView : BaseView
    {
        private Button startGameBtn;
        
        [Header("登陆界面UI")]
        private TMP_InputField accountInput;
        private TMP_InputField passwordInput;
        private Button loginBtn;
        private Button registerBtn;
        private Button forgotPasswordBtn;
        private Toggle acTog;
        
        protected override void OnAwake()
        {
            startGameBtn = Find<Button>("Btn_start");
            accountInput = Find<TMP_InputField>("loginView/Txt_account");
            passwordInput = Find<TMP_InputField>("loginView/Txt_password");
            loginBtn = Find<Button>("loginView/btnsArea/Btn_login");
            registerBtn = Find<Button>("loginView/btnsArea/Btn_register");
            forgotPasswordBtn = Find<Button>("loginView/btnsArea/Btn_forget");
            acTog = Find<Toggle>("loginView/btnsArea/Tog_ac");
        }

        protected override void OnStart()
        {
            loginBtn.onClick.AddListener(onLoginBtnClick);
            
            //注册回调
            GameApp.NetworkManager.AddMessageHandler(ActionCode.Login, OnLoginCallback);
        }

        protected override void OnDestroy()
        {
            GameApp.NetworkManager.RemoveMessageHandler(ActionCode.Login, OnLoginCallback);
        }

        private void onLoginBtnClick()
        {
            //测试
            Debug.Log("点击了登陆按钮,尝试连接服务器...");
            
            //构造Protobuf大包
            MainPack pack = new MainPack();
            pack.RequestCode = RequestCode.User;
            pack.ActionCode = ActionCode.Login;
            pack.LoginPack = new LoginPack()
            {
                Username = accountInput.text,
                Password = passwordInput.text
            };
            
            GameApp.NetworkManager.Send(pack);
        }

        private void OnLoginCallback(MainPack pack)
        {
            if (pack.ReturnCode == ReturnCode.Succeed)
            {
                Debug.Log($"登陆成功，欢迎 {pack.LoginPack?.Username??accountInput.text}!");
                
                //TODO：隐藏登陆面板
            }
            else
            {
                Debug.LogError($"登陆失败，错误码: {pack.StrMsg}");
                //TODO：显示错误提示
            }
        }
    }
}