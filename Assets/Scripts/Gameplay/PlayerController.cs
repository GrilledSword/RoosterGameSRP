using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PekkaPlayerController : NetworkBehaviour, IDamageable, ISaveable
{
    [Header("Mozgás Beállítások")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeedMultiplier = 1.5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private int maxDoubleJumps = 1;
    [SerializeField] private float groundedStickyForce = 10f;

    [Header("Komponens Referenciák")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private PlayerSoundController soundController;

    [Header("Kamera Beállítások")]
    [SerializeField] private GameObject cameraRoot;
    [SerializeField] private float cameraRootSmoothSpeed = 5f;
    [SerializeField] private float cameraRootZPosition;

    [Header("Karakter Adatok")]
    [SerializeField] private float maximumHealth = 100;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> activeSpeedMultiplier = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Coroutine activePowerUpCoroutine;
    private NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Sebzés Kezelés")]
    [SerializeField] private float invincibilityDurationAfterDamage = 1.5f;
    private Coroutine damageInvincibilityCoroutine;
    private NetworkVariable<bool> isDamagedState = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Frakció Beállítás")]
    private Faction faction = Faction.Player;
    public Faction Faction => faction;

    [Header("Karakter UI")]
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private float scoreCountingDuration = 0.5f;
    private float displayedScore = 0f;
    private Coroutine scoreCountingCoroutine;

    [Header("Karakter Inventory")]
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private Slots slots;
    [SerializeField] private bool isPlayerSpawned = false;

    private NetworkVariable<Vector2> networkMovementInput = new NetworkVariable<Vector2>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkAttackTrigger = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkRunInput = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsGrounded = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsJumping = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsFalling = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool isRunningInputPressedLocal = false;
    private int currentDoubleJumps;
    private bool isInventoryInputPressedLocal = false;
    private PlayerControls playerInputActions;

    void Awake()
    {
        playerInputActions = new PlayerControls();
        playerInputActions.Player.Move.performed += ctx => networkMovementInput.Value = ctx.ReadValue<Vector2>();
        playerInputActions.Player.Move.canceled += ctx => networkMovementInput.Value = Vector2.zero;
        playerInputActions.Player.Jump.performed += ctx => RequestJumpServerRpc();
        playerInputActions.Player.Sprint.performed += ctx => isRunningInputPressedLocal = true;
        playerInputActions.Player.Sprint.canceled += ctx => isRunningInputPressedLocal = false;
        playerInputActions.Player.Inventory.performed += ctx => isInventoryInputPressedLocal = true;
        playerInputActions.Player.Inventory.canceled += ctx => isInventoryInputPressedLocal = false;
        playerInputActions.Player.Slot1.performed += ctx => AttemptAttackServerRpc(0);
        playerInputActions.Player.Slot2.performed += ctx => AttemptAttackServerRpc(1);
        playerInputActions.Player.Slot3.performed += ctx => slots.UseItemServerRpc(2);
        playerInputActions.Player.Slot4.performed += ctx => slots.UseItemServerRpc(3);
        playerInputActions.Player.Slot5.performed += ctx => slots.UseItemServerRpc(4);

        if (soundController == null)
        {
            soundController = GetComponent<PlayerSoundController>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maximumHealth;
            isDead.Value = false;
            score.Value = 0;
        }

        if (IsOwner)
        {
            playerInputActions.Enable();

            AssignCameraToPlayer();
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
            }

            if (inventoryUI != null)
            {
                inventoryUI.SetActive(false);
            }
            if (healthBarSlider != null)
            {
                healthBarSlider.maxValue = maximumHealth;
            }
        }
        else
        {
            playerInputActions.Disable();
        }

        networkSpeed.OnValueChanged += OnSpeedChanged;
        networkIsGrounded.OnValueChanged += OnIsGroundedChanged;
        networkIsJumping.OnValueChanged += OnIsJumpingChanged;
        networkIsFalling.OnValueChanged += OnIsFallingChanged;
        networkAttackTrigger.OnValueChanged += OnAttackTriggered;
        currentHealth.OnValueChanged += OnHealthChanged;
        score.OnValueChanged += OnScoreChanged;
        isDead.OnValueChanged += OnIsDeadChanged;
        isDamagedState.OnValueChanged += OnIsDamagedStateChanged;

        if (IsOwner)
        {
            OnHealthChanged(0, currentHealth.Value);
            displayedScore = score.Value;
            UpdateScoreText(score.Value);
            isPlayerSpawned = true;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
        }

        networkSpeed.OnValueChanged -= OnSpeedChanged;
        networkIsGrounded.OnValueChanged -= OnIsGroundedChanged;
        networkIsJumping.OnValueChanged -= OnIsJumpingChanged;
        networkIsFalling.OnValueChanged -= OnIsFallingChanged;
        networkAttackTrigger.OnValueChanged -= OnAttackTriggered;
        currentHealth.OnValueChanged -= OnHealthChanged;
        score.OnValueChanged -= OnScoreChanged;
        isDead.OnValueChanged -= OnIsDeadChanged;
        isDamagedState.OnValueChanged -= OnIsDamagedStateChanged;
    }

    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (IsOwner)
        {
            AssignCameraToPlayer();
        }
    }

    private void AssignCameraToPlayer()
    {
        CinemachineCamera virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.Follow = cameraRoot != null ? cameraRoot.transform : transform;
            virtualCamera.LookAt = cameraRoot != null ? cameraRoot.transform : transform;
        }
    }

    void FixedUpdate()
    {
        if (IsServer)
        {
            CheckGroundStatus();
            HandleMovementServer();
            ResetAttackTriggerServer();
            UpdateNetworkAnimatorParametersServer();

            if (networkIsGrounded.Value)
            {
                rb.AddForce(Vector3.down * groundedStickyForce, ForceMode.Force);
            }
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            networkRunInput.Value = isRunningInputPressedLocal;
            UpdateLocalAnimatorParameters();
            UpdateCameraRootPosition();
            HandleInventoryUI();
        }
    }

    private void ResetAttackTriggerServer()
    {
        if (networkAttackTrigger.Value)
        {
            networkAttackTrigger.Value = false;
        }
    }

    private void CheckGroundStatus()
    {
        bool wasGrounded = networkIsGrounded.Value;
        networkIsGrounded.Value = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (networkIsGrounded.Value && !wasGrounded)
        {
            currentDoubleJumps = maxDoubleJumps;
        }
    }

    private void HandleMovementServer()
    {
        if (isDead.Value) return;
        Vector3 moveDirection = new Vector3(networkMovementInput.Value.x, 0f, 0f).normalized;
        float targetSpeed = moveSpeed * activeSpeedMultiplier.Value;

        if (networkRunInput.Value && !isDamagedState.Value)
        {
            targetSpeed *= runSpeedMultiplier;
        }

        rb.linearVelocity = new Vector3(moveDirection.x * targetSpeed, rb.linearVelocity.y, 0f);
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0, 0));
        }
    }

    private void UpdateNetworkAnimatorParametersServer()
    {
        float currentHorizontalSpeed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
        networkSpeed.Value = currentHorizontalSpeed / (moveSpeed * runSpeedMultiplier);
        if (!networkIsGrounded.Value)
        {
            networkIsJumping.Value = rb.linearVelocity.y > 0.1f;
            networkIsFalling.Value = rb.linearVelocity.y < -0.1f;
        }
        else
        {
            networkIsJumping.Value = false;
            networkIsFalling.Value = false;
        }
    }

    [ServerRpc]
    private void RequestJumpServerRpc()
    {
        if (isDead.Value) return;

        bool jumpExecuted = false;

        if (networkIsGrounded.Value)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpExecuted = true;
        }
        else if (currentDoubleJumps > 0 && !isDamagedState.Value)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            currentDoubleJumps--;
            jumpExecuted = true;
        }

        if (jumpExecuted && soundController != null)
        {
            // A szerver közvetlenül a SoundController-nek szól, hogy játssza le a hangot mindenkinél.
            soundController.PlayJumpSoundClientRpc();
        }
    }

    [ServerRpc]
    private void AttemptAttackServerRpc(int slotIndex)
    {
        if (slots != null)
        {
            slots.TriggerAttackFromSlot(slotIndex);
        }
    }

    public void TakeDamage(float damage, Faction sourceFaction)
    {
        if (isDead.Value || isInvincible.Value) return;

        currentHealth.Value -= damage;
        if (soundController != null) soundController.PlayDamageSoundClientRpc();

        if (damageInvincibilityCoroutine != null)
        {
            StopCoroutine(damageInvincibilityCoroutine);
        }
        damageInvincibilityCoroutine = StartCoroutine(DamageInvincibilityCoroutine());

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            DieServerRpc();
        }
    }

    private IEnumerator DamageInvincibilityCoroutine()
    {
        isInvincible.Value = true;
        isDamagedState.Value = true;
        yield return new WaitForSeconds(invincibilityDurationAfterDamage);

        if (activePowerUpCoroutine == null)
        {
            isInvincible.Value = false;
        }
        isDamagedState.Value = false;
        damageInvincibilityCoroutine = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(float amount)
    {
        if (isDead.Value) return;
        currentHealth.Value = Mathf.Min(currentHealth.Value + amount, maximumHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyPowerUpServerRpc(int itemID)
    {
        if (isDead.Value) return;
        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemID);
        if (itemDef is PowerUpItemDefinition powerUp)
        {
            if (activePowerUpCoroutine != null)
            {
                StopCoroutine(activePowerUpCoroutine);
            }
            activePowerUpCoroutine = StartCoroutine(PowerUpCoroutine(powerUp));
        }
    }

    private IEnumerator PowerUpCoroutine(PowerUpItemDefinition powerUp)
    {
        if (powerUp.grantsInvincibility)
        {
            if (damageInvincibilityCoroutine != null)
            {
                StopCoroutine(damageInvincibilityCoroutine);
                isDamagedState.Value = false;
                damageInvincibilityCoroutine = null;
            }
            isInvincible.Value = true;
        }
        activeSpeedMultiplier.Value = powerUp.speedMultiplier;

        yield return new WaitForSeconds(powerUp.duration);

        if (powerUp.grantsInvincibility)
        {
            isInvincible.Value = false;
        }
        activeSpeedMultiplier.Value = 1f;
        activePowerUpCoroutine = null;
    }

    public void ExecuteAttack()
    {
        if (!IsServer) return;
        networkAttackTrigger.Value = true;
    }

    [ServerRpc]
    private void DieServerRpc()
    {
        isDead.Value = true;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;
        if (soundController != null) soundController.PlayDeathSoundClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(int amount)
    {
        if (isDead.Value || amount == 0) return;
        score.Value += amount;
    }

    private void OnSpeedChanged(float oldSpeed, float newSpeed) => animator.SetFloat("Speed", newSpeed);
    private void OnIsGroundedChanged(bool oldIsGrounded, bool newIsGrounded)
    {
        animator.SetBool("IsGrounded", newIsGrounded);
        if (!oldIsGrounded && newIsGrounded && soundController != null)
        {
            soundController.PlayLandSoundClientRpc();
        }
    }
    private void OnIsJumpingChanged(bool oldIsJumping, bool newIsJumping) => animator.SetBool("IsJumping", newIsJumping);
    private void OnIsFallingChanged(bool oldIsFalling, bool newIsFalling) => animator.SetBool("IsFalling", newIsFalling);
    private void OnAttackTriggered(bool oldAttackTrigger, bool newAttackTrigger)
    {
        if (newAttackTrigger) animator.SetTrigger("Attack");
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        if (IsOwner && healthBarSlider != null)
        {
            healthBarSlider.value = newHealth;
        }
    }

    private void OnIsDamagedStateChanged(bool previousValue, bool newValue)
    {
        animator.SetBool("IsDamaged", newValue);
    }

    private void OnScoreChanged(int oldScore, int newScore)
    {
        if (IsOwner)
        {
            if (scoreCountingCoroutine != null)
            {
                StopCoroutine(scoreCountingCoroutine);
            }
            scoreCountingCoroutine = StartCoroutine(CountScoreCoroutine(newScore));
        }
    }

    private IEnumerator CountScoreCoroutine(int targetScore)
    {
        float startScore = displayedScore;
        float elapsedTime = 0f;

        while (elapsedTime < scoreCountingDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / scoreCountingDuration;
            displayedScore = Mathf.Lerp(startScore, targetScore, progress);
            UpdateScoreText(Mathf.RoundToInt(displayedScore));
            yield return null;
        }

        displayedScore = targetScore;
        UpdateScoreText(targetScore);
        scoreCountingCoroutine = null;
    }

    private void UpdateScoreText(int scoreValue)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Pontszám: {scoreValue}";
        }
    }

    private void OnIsDeadChanged(bool oldIsDead, bool newIsDead)
    {
        if (newIsDead && !oldIsDead)
        {
            animator.SetTrigger("Die");
        }
    }
    private void UpdateLocalAnimatorParameters()
    {
        float currentHorizontalSpeed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
        float animationSpeed = currentHorizontalSpeed / (moveSpeed * runSpeedMultiplier);
        animator.SetFloat("Speed", animationSpeed);
        bool currentLocalIsGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("IsGrounded", currentLocalIsGrounded);
        if (!currentLocalIsGrounded)
        {
            animator.SetBool("IsJumping", rb.linearVelocity.y > 0.1f);
            animator.SetBool("IsFalling", rb.linearVelocity.y < -0.1f);
        }
        else
        {
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsFalling", false);
        }
    }
    private void UpdateCameraRootPosition()
    {
        if (cameraRoot == null) return;
        float targetZ = (networkMovementInput.Value.x > 0.1f || networkMovementInput.Value.x < -0.1f) ? cameraRootZPosition : 0f;
        Vector3 currentLocalPos = cameraRoot.transform.localPosition;
        currentLocalPos.z = Mathf.Lerp(currentLocalPos.z, targetZ, Time.deltaTime * cameraRootSmoothSpeed);
        cameraRoot.transform.localPosition = currentLocalPos;
    }
    private void HandleInventoryUI()
    {
        if (!isPlayerSpawned || inventoryUI == null) return;
        if (isInventoryInputPressedLocal)
        {
            inventoryUI.SetActive(!inventoryUI.activeSelf);
            isInventoryInputPressedLocal = false;
        }
    }

    public void AnimEvent_PlayFootstepSound()
    {
        soundController?.AnimEvent_PlayFootstepSound();
    }

    public void SaveData(ref GameData data)
    {
        data.playerPosition = transform.position;
        data.currentHealth = this.currentHealth.Value;
        data.score = this.score.Value;

        if (slots != null)
        {
            data.inventoryItems = new List<ItemDataSerializable>();
            foreach (var item in slots.GetInventoryItems())
            {
                data.inventoryItems.Add(new ItemDataSerializable
                {
                    itemID = item.itemID,
                    quantity = item.quantity,
                    isEmpty = item.isEmpty
                });
            }
        }
    }

    public void LoadData(GameData data)
    {
        if (!IsServer) return;

        if (TryGetComponent<CharacterController>(out var cc))
        {
            cc.enabled = false;
            transform.position = data.playerPosition;
            cc.enabled = true;
        }
        else
        {
            transform.position = data.playerPosition;
        }

        this.currentHealth.Value = data.currentHealth;
        this.score.Value = data.score;

        if (slots != null)
        {
            slots.LoadInventoryData(data.inventoryItems);
        }
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = networkIsGrounded.Value ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
