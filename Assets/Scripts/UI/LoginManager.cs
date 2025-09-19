using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

namespace EaseDev.UI
{
    public class LoginManager : MonoBehaviour
    {
        [Header("UI References")]
        public InputField usernameInput;
        public InputField passwordInput;
        public Button loginButton;
        public Button registerButton;
        public Text messageText;

        [Header("Events")]
        public UnityEvent<string, string> OnLoginAttempt;
        public UnityEvent OnRegisterClicked;

        private void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // 绑定按钮事件
            loginButton.onClick.AddListener(OnLoginButtonClicked);
            registerButton.onClick.AddListener(OnRegisterButtonClicked);

            // 设置初始状态
            messageText.text = "";
            usernameInput.text = "";
            passwordInput.text = "";

            // 添加输入验证
            usernameInput.onValueChanged.AddListener(OnInputChanged);
            passwordInput.onValueChanged.AddListener(OnInputChanged);
        }

        private void OnLoginButtonClicked()
        {
            string username = usernameInput.text.Trim();
            string password = passwordInput.text.Trim();

            if (ValidateInput(username, password))
            {
                OnLoginAttempt?.Invoke(username, password);
                ShowMessage("正在登录...", Color.blue);
            }
        }

        private void OnRegisterButtonClicked()
        {
            OnRegisterClicked?.Invoke();
        }

        private bool ValidateInput(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                ShowMessage("用户名不能为空", Color.red);
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowMessage("密码不能为空", Color.red);
                return false;
            }

            if (password.Length < 6)
            {
                ShowMessage("密码长度至少6位", Color.red);
                return false;
            }

            return true;
        }

        private void OnInputChanged(string value)
        {
            // 清除错误信息
            if (messageText.color == Color.red)
            {
                messageText.text = "";
            }
        }

        public void ShowMessage(string message, Color color)
        {
            messageText.text = message;
            messageText.color = color;
        }

        public void OnLoginSuccess()
        {
            ShowMessage("登录成功！", Color.green);
        }

        public void OnLoginFailed(string error)
        {
            ShowMessage($"登录失败: {error}", Color.red);
        }
    }
}