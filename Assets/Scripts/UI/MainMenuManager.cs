using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EaseDev.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        public Button startGameButton;
        public Button settingsButton;
        public Button creditsButton;
        public Button quitButton;

        [Header("Panels")]
        public GameObject settingsPanel;
        public GameObject creditsPanel;

        [Header("Settings")]
        public string gameSceneName = "GameScene";

        private void Start()
        {
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            // 绑定按钮事件
            startGameButton.onClick.AddListener(StartGame);
            settingsButton.onClick.AddListener(OpenSettings);
            creditsButton.onClick.AddListener(OpenCredits);
            quitButton.onClick.AddListener(QuitGame);

            // 初始化面板状态
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        public void StartGame()
        {
            Debug.Log("Starting game...");
            SceneManager.LoadScene(gameSceneName);
        }

        public void OpenSettings()
        {
            Debug.Log("Opening settings...");
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        public void OpenCredits()
        {
            Debug.Log("Opening credits...");
            if (creditsPanel != null)
                creditsPanel.SetActive(true);
        }

        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public void ClosePanel(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}