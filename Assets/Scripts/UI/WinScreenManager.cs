using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WinScreenManager : MonoBehaviour
{
    [Header("Win UI")]
    public GameObject winPanel;
    public Text winText;
    public Image fadeToWhiteImage; // Changed to white for victory feel
    public Button titleScreenButton;
    public Button playAgainButton;

    [Header("Win Animation Settings")]
    public float winTextFadeInDuration = 2f;
    public float fadeToWhiteDuration = 3f;
    public float buttonFadeInDelay = 1f;
    public float buttonFadeInDuration = 1.5f;

    [Header("Door Tracking")]
    public int totalDoorsToUnlock = 2; // Set this to match your total doors
    private int unlockedDoors = 0;

    private bool isWinSequenceActive = false;
    private CallEvent callEvent;

    // Static instance for easy access from doors
    public static WinScreenManager Instance;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        callEvent = GetComponent<CallEvent>();
        if (callEvent == null)
        {
            callEvent = FindObjectOfType<CallEvent>();
        }

        // Setup UI elements
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (titleScreenButton != null)
        {
            titleScreenButton.onClick.AddListener(GoToTitleScreen);
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        // Make sure fade overlay starts transparent
        if (fadeToWhiteImage != null)
        {
            Color fadeColor = fadeToWhiteImage.color;
            fadeColor.a = 0f;
            fadeToWhiteImage.color = fadeColor;
        }

        Debug.Log($"WinScreenManager initialized. Need to unlock {totalDoorsToUnlock} doors to win.");
    }

    public void RegisterDoorUnlock()
    {
        unlockedDoors++;
        Debug.Log($"Door unlocked! Progress: {unlockedDoors}/{totalDoorsToUnlock}");

        if (unlockedDoors >= totalDoorsToUnlock)
        {
            StartWinSequence();
        }
    }

    public void StartWinSequence()
    {
        if (isWinSequenceActive) return;

        isWinSequenceActive = true;
        
        // Play victory sound
        if (callEvent != null)
        {
            callEvent.Callevent("Victory"); // Make sure you have this event in Wwise
        }

        StartCoroutine(WinSequenceCoroutine());
    }

    private IEnumerator WinSequenceCoroutine()
    {
        // Wait a moment before starting the win sequence
        yield return new WaitForSeconds(1f);

        // Activate win panel
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        // Fade in win text
        if (winText != null)
        {
            winText.text = "You Won!";
            yield return StartCoroutine(FadeInText(winText, winTextFadeInDuration));
        }

        // Wait a moment before starting fade to white
        yield return new WaitForSeconds(1f);

        // Fade to white (victory feel)
        if (fadeToWhiteImage != null)
        {
            yield return StartCoroutine(FadeToWhite(fadeToWhiteDuration));
        }

        // Wait before showing buttons
        yield return new WaitForSeconds(buttonFadeInDelay);

        // Fade in buttons
        if (titleScreenButton != null && playAgainButton != null)
        {
            StartCoroutine(FadeInButton(titleScreenButton, buttonFadeInDuration));
            StartCoroutine(FadeInButton(playAgainButton, buttonFadeInDuration));
        }

        // Show cursor for button interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator FadeInText(Text text, float duration)
    {
        Color textColor = text.color;
        textColor.a = 0f;
        text.color = textColor;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / duration);
            textColor.a = alpha;
            text.color = textColor;
            yield return null;
        }

        textColor.a = 1f;
        text.color = textColor;
    }

    private IEnumerator FadeToWhite(float duration)
    {
        Color fadeColor = fadeToWhiteImage.color;
        fadeColor.a = 0f;
        fadeToWhiteImage.color = fadeColor;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / duration);
            fadeColor.a = alpha;
            fadeToWhiteImage.color = fadeColor;
            yield return null;
        }

        fadeColor.a = 1f;
        fadeToWhiteImage.color = fadeColor;
    }

    private IEnumerator FadeInButton(Button button, float duration)
    {
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
    }

    public void GoToTitleScreen()
    {
        SceneManager.LoadScene(0);
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(1);
    }

    public bool IsWinSequenceActive()
    {
        return isWinSequenceActive;
    }

    // Public method to manually trigger win (for testing)
    public void TriggerWin()
    {
        unlockedDoors = totalDoorsToUnlock;
        StartWinSequence();
    }
}