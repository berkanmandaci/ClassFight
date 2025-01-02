using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterUI : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Canvas worldSpaceCanvas;

    [Header("Cooldown UI")]
    [SerializeField] private Image dashCooldownImage;
    [SerializeField] private Image attackCooldownImage;
    [SerializeField] private Image dodgeCooldownImage;
    [SerializeField] private TextMeshProUGUI dashStackText;

    private BaseCharacterController characterController;
    private ArcherController archerController;
    private Camera mainCamera;
    private float maxHealth = 100f;

    private void Start()
    {
        // Karakter kontrolcüsünü bul
        characterController = GetComponentInParent<BaseCharacterController>();
        archerController = characterController as ArcherController;
        mainCamera = Camera.main;

        if (characterController == null)
        {
            Debug.LogError("CharacterUI: BaseCharacterController bulunamadı!");
            enabled = false;
            return;
        }

        // Başlangıç can değerini kaydet
        maxHealth = characterController.Health;
    }

    private void LateUpdate()
    {
        if (characterController == null || mainCamera == null) return;

        // UI'ı kameraya döndür
        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.transform.forward = mainCamera.transform.forward;
        }

        UpdateHealth();
        UpdateCooldowns();
    }

    private void UpdateHealth()
    {
        if (healthBarFill != null)
        {
            float healthPercent = characterController.Health / maxHealth;
            healthBarFill.fillAmount = healthPercent;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(characterController.Health)}/{maxHealth}";
        }
    }

    private void UpdateCooldowns()
    {
        if (characterController == null) return;

        // Dash cooldown ve stack
        if (dashCooldownImage != null)
        {
            dashCooldownImage.fillAmount = 1f - characterController.GetDashCooldownProgress();
        }

        if (dashStackText != null)
        {
            dashStackText.text = $"{characterController.GetCurrentDashStacks()}/{characterController.GetMaxDashStacks()}";
        }

        // Attack cooldown (sadece Archer için)
        if (attackCooldownImage != null && archerController != null)
        {
            attackCooldownImage.fillAmount = 1f - archerController.GetAttackCooldownProgress();
        }

        // Dodge cooldown (eğer implement edilmişse)
        if (dodgeCooldownImage != null && archerController != null)
        {
            // Şimdilik dodge cooldown'u devre dışı
            dodgeCooldownImage.fillAmount = 0f;
        }
    }
}
