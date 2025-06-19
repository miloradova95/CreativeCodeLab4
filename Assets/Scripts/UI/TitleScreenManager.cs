using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    [Header("UI References")]
    public Button startGameButton;
    public Button settingsButton;
    public Button exitButton;
    public GameObject settingsPanel;
    public Button closeSettingsButton;

    [Header("Settings UI")]
    public Slider sensitivitySlider;
    public Text sensitivityValueText;
    public Button difficultyButton;
    public Text difficultyText;

    [Header("Scene Management")]
    public string gameSceneName = "GameScene";

    private SettingsManager settingsManager;

    void Start()
    {
        // Ensure we have a settings manager
        if (SettingsManager.Instance == null)
        {
            GameObject settingsGO = new GameObject("SettingsManager");
            settingsManager = settingsGO.AddComponent<SettingsManager>();
        }
        else
        {
            settingsManager = SettingsManager.Instance;
        }

        // Setup button listeners
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        if (difficultyButton != null)
            difficultyButton.onClick.AddListener(CycleDifficulty);

        // Setup settings panel
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Setup sensitivity slider
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.5f;
            sensitivitySlider.maxValue = 5f;
            sensitivitySlider.value = settingsManager.mouseSensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        // Update UI elements
        UpdateSettingsUI();

        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        // Save settings before starting game
        settingsManager.SaveSettings();
        
        // Load game scene
        SceneManager.LoadScene(1);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            UpdateSettingsUI();
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ExitGame()
    {
        // Save settings before exiting
        settingsManager.SaveSettings();
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void OnSensitivityChanged(float value)
    {
        settingsManager.SetMouseSensitivity(value);
        UpdateSensitivityText();
    }

    public void CycleDifficulty()
    {
        settingsManager.CycleDifficulty();
        UpdateDifficultyText();
    }

    void UpdateSettingsUI()
    {
        UpdateSensitivityText();
        UpdateDifficultyText();
        
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = settingsManager.mouseSensitivity;
        }
    }

    void UpdateSensitivityText()
    {
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = settingsManager.mouseSensitivity.ToString("F1");
        }
    }

    void UpdateDifficultyText()
    {
        if (difficultyText != null)
        {
            difficultyText.text = settingsManager.GetDifficultyString();
        }
    }
}