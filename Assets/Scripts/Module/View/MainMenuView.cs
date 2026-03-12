/*
* ┌──────────────────────────────────┐
* │  描    述: 主界面视图                      
* │  类    名: MainMenuView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

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
            
        }
    }
}