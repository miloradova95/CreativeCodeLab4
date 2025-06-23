using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUIManager : MonoBehaviour
{
    [Header("Health Overlay")]
    public Image damageOverlay;
    public float damageFlashDuration = 0.5f;
    public float damageFlashIntensity = 0.8f;
    public float lowHealthAlpha = 0.3f;
    public float criticalHealthAlpha = 0.6f;

    [Header("Colors")]
    public Color damageColor = new Color(1f, 0f, 0f, 0.8f);
    public Color lowHealthColor = new Color(1f, 0f, 0f, 0.3f);
    public Color criticalHealthColor = new Color(1f, 0f, 0f, 0.6f);

    private FirstPersonController playerController;
    private Coroutine damageFlashCoroutine;
    private bool isFlashing = false;

    void Start()
    {
        // Find the player controller
        playerController = FindObjectOfType<FirstPersonController>();
        
        if (damageOverlay != null)
        {
            // Start with overlay invisible
            damageOverlay.color = new Color(damageColor.r, damageColor.g, damageColor.b, 0f);
        }
    }

    void Update()
    {
        if (playerController != null && damageOverlay != null && !isFlashing)
        {
            UpdateHealthOverlay();
        }
    }

    void UpdateHealthOverlay()
    {
        float healthPercentage = playerController.GetHealthPercentage();
        
        if (healthPercentage <= 0.6f) // Below 60% health
        {
            float alpha;
            Color targetColor;
            
            if (playerController.IsPlayerDebuffed()) // Critical health (below threshold)
            {
                alpha = criticalHealthAlpha;
                targetColor = criticalHealthColor;
            }
            else // Low health (below 60% but above threshold)
            {
                alpha = lowHealthAlpha;
                targetColor = lowHealthColor;
            }
            
            // Smoothly transition to the target overlay
            Color currentColor = damageOverlay.color;
            Color newColor = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            damageOverlay.color = Color.Lerp(currentColor, newColor, Time.deltaTime * 2f);
        }
        else
        {
            // Fade out the overlay when health is above 60%
            Color currentColor = damageOverlay.color;
            Color targetColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
            damageOverlay.color = Color.Lerp(currentColor, targetColor, Time.deltaTime * 3f);
        }
    }

    public void ShowDamageFlash()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }
        
        damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    private IEnumerator DamageFlashCoroutine()
    {
        isFlashing = true;
        
        // Flash to full intensity
        damageOverlay.color = new Color(damageColor.r, damageColor.g, damageColor.b, damageFlashIntensity);
        
        // Wait for flash duration
        yield return new WaitForSeconds(damageFlashDuration);
        
        isFlashing = false;
        
        // Let the Update method handle the fade out based on current health
    }
}