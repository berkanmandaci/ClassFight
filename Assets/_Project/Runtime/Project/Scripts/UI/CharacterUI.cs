using _Project.Runtime.Project.Service.Scripts.Model;
using _Project.Scripts.Vo;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseCharacterController character;
    [SerializeField] private Canvas worldSpaceCanvas;
    [SerializeField] private Camera mainCamera;

    [Header("Health Bar")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Cooldowns")]
    [SerializeField] private Image dashCooldownFill;
    [SerializeField] private TextMeshProUGUI dashStacksText;
    [SerializeField] private Image dodgeCooldownFill;
    [SerializeField] private Image attackCooldownFill;

    [Header("UserInfo")]
    [SerializeField] private TextMeshProUGUI nicknameText;
    private string userId;

    private PvpUserVo userVo => PvpArenaModel.Instance.PvpArenaVo.GetUser(userId);
    private float maxHealth = 100f;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (character == null)
            character = GetComponentInParent<BaseCharacterController>();

        // Initialize max health

    }
    public void Init(string userId)
    {
        this.userId = userId;
        nicknameText.text = userVo.DisplayName;
        maxHealth = character.Health;
    }

    private void LateUpdate()
    {
        if (character == null || mainCamera == null) return;

        // Update health bar
        float healthPercent = character.Health / maxHealth;
        healthBarFill.fillAmount = healthPercent;
        healthText.text = $"{Mathf.CeilToInt(character.Health)}/{maxHealth}";

        // Make UI face camera
        worldSpaceCanvas.transform.forward = mainCamera.transform.forward;

        // Update cooldowns if character is Archer
        if (character is ArcherController archer)
        {
            // Dash stacks
            dashStacksText.text = archer.CurrentDashStacks.ToString();

            // Cooldown fills
            float dashCooldownPercent = archer.GetDashCooldownPercent();
            dashCooldownFill.fillAmount = dashCooldownPercent;

            float dodgeCooldownPercent = archer.GetDodgeCooldownPercent();
            dodgeCooldownFill.fillAmount = dodgeCooldownPercent;

            float attackCooldownPercent = archer.GetAttackCooldownPercent();
            attackCooldownFill.fillAmount = attackCooldownPercent;
        }
    }
}
