using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Settings")]
    public float mouseSensitivity = 2f;
    public Difficulty difficulty = Difficulty.Standard;

    public enum Difficulty
    {
        Easy,
        Standard,
        Hard
    }

    // Difficulty-based stats
    public static class DifficultyStats
    {
        public static float GetMaxHealth(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return 300f;
                case Difficulty.Standard: return 200f;
                case Difficulty.Hard: return 100f;
                default: return 200f;
            }
        }

        public static float GetMaxStamina(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return 300f;
                case Difficulty.Standard: return 200f;
                case Difficulty.Hard: return 100f;
                default: return 200f;
            }
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        PlayerPrefs.SetInt("Difficulty", (int)difficulty);
        PlayerPrefs.Save();
        Debug.Log($"Settings saved - Sensitivity: {mouseSensitivity}, Difficulty: {difficulty}");
    }

    public void LoadSettings()
    {
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        difficulty = (Difficulty)PlayerPrefs.GetInt("Difficulty", 1); // Default to Standard
        Debug.Log($"Settings loaded - Sensitivity: {mouseSensitivity}, Difficulty: {difficulty}");
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
        SaveSettings();
    }

    public void SetDifficulty(Difficulty newDifficulty)
    {
        difficulty = newDifficulty;
        SaveSettings();
    }

    public string GetDifficultyString()
    {
        return difficulty.ToString();
    }

    public void CycleDifficulty()
    {
        int currentIndex = (int)difficulty;
        int nextIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(Difficulty)).Length;
        difficulty = (Difficulty)nextIndex;
        SaveSettings();
    }
}