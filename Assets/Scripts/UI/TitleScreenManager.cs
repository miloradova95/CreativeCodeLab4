using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Add this for TextMeshPro

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
    public TextMeshProUGUI sensitivityValueText; // Changed from Text to TextMeshProUGUI
    public Button difficultyButton;
    public TextMeshProUGUI difficultyText; // Changed from Text to TextMeshProUGUI

    [Header("Scene Management")]
    public string gameSceneName = "GameScene";
    
    [Header("Fade Transition")]
    public Image fadePanel; // Drag a black UI Image here
    public float fadeDuration = 5f;

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

        // Setup fade panel
        if (fadePanel != null)
        {
            fadePanel.color = new Color(0, 0, 0, 0); // Start transparent
            fadePanel.gameObject.SetActive(false);
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
        
        // Start fade transition
        StartCoroutine(FadeToScene());
    }
    
    IEnumerator FadeToScene()
    {
        // Disable all buttons to prevent multiple clicks
        SetButtonsInteractable(false);
        
        // Enable fade panel
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            
            // Fade to black
            float elapsedTime = 0f;
            Color fadeColor = fadePanel.color;
            
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
                fadePanel.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }
            
            // Ensure we're fully black
            fadePanel.color = new Color(0, 0, 0, 1);
        }
        else
        {
            // If no fade panel, just wait the duration
            yield return new WaitForSeconds(fadeDuration);
        }
        
        // Load game scene
        SceneManager.LoadScene(1);
    }
    
    void SetButtonsInteractable(bool interactable)
    {
        if (startGameButton != null) startGameButton.interactable = interactable;
        if (settingsButton != null) settingsButton.interactable = interactable;
        if (exitButton != null) exitButton.interactable = interactable;
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