using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathScreenManager : MonoBehaviour
{
    [Header("Death UI")]
    public GameObject deathPanel;
    public Text deathText;
    public Image fadeToBlackImage;
    public Button titleScreenButton;
    public Button restartButton;

    [Header("Death Animation Settings")]
    public float deathTextFadeInDuration = 2f;
    public float fadeToBlackDuration = 3f;
    public float buttonFadeInDelay = 1f;
    public float buttonFadeInDuration = 1.5f;

    private bool isDeathSequenceActive = false;

    void Start()
    {
        // Setup UI elements
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        if (titleScreenButton != null)
        {
            titleScreenButton.onClick.AddListener(GoToTitleScreen);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        // Make sure fade overlay starts transparent
        if (fadeToBlackImage != null)
        {
            Color fadeColor = fadeToBlackImage.color;
            fadeColor.a = 0f;
            fadeToBlackImage.color = fadeColor;
        }
    }

    public void StartDeathSequence()
    {
        if (isDeathSequenceActive) return;
        
        isDeathSequenceActive = true;
        StartCoroutine(DeathSequenceCoroutine());
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        // Wait a moment before starting the death sequence
        yield return new WaitForSeconds(0.5f);

        // Activate death panel
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        // Fade in death text
        if (deathText != null)
        {
            deathText.text = "You Died";
            yield return StartCoroutine(FadeInText(deathText, deathTextFadeInDuration));
        }

        // Wait a moment before starting fade to black
        yield return new WaitForSeconds(1f);

        // Fade to black
        if (fadeToBlackImage != null)
        {
            yield return StartCoroutine(FadeToBlack(fadeToBlackDuration));
        }

        // Wait before showing buttons
        yield return new WaitForSeconds(buttonFadeInDelay);

        // Fade in buttons
        if (titleScreenButton != null && restartButton != null)
        {
            StartCoroutine(FadeInButton(titleScreenButton, buttonFadeInDuration));
            StartCoroutine(FadeInButton(restartButton, buttonFadeInDuration));
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

    private IEnumerator FadeToBlack(float duration)
    {
        Color fadeColor = fadeToBlackImage.color;
        fadeColor.a = 0f;
        fadeToBlackImage.color = fadeColor;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / duration);
            fadeColor.a = alpha;
            fadeToBlackImage.color = fadeColor;
            yield return null;
        }

        fadeColor.a = 1f;
        fadeToBlackImage.color = fadeColor;
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

    public void RestartGame()
    {
        SceneManager.LoadScene(1);
    }

    public bool IsDeathSequenceActive()
    {
        return isDeathSequenceActive;
    }
}