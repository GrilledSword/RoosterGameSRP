using System.Collections;
using System.Collections.Generic;
using System.Text; // Fontos a StringBuilder-hez
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private InGameMenuManager inGameMenuManager;

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
    private Dictionary<char, int> spriteIndexMap;
    void Awake()
    {
        playerInputActions = new PlayerControls();

        if (soundController == null)
        {
            soundController = GetComponent<PlayerSoundController>();
        }
        InitializeSpriteMap();
    }
    public void SetPlayerControlActive(bool isActive)
    {
        if (!IsOwner) return;

        if (isActive)
        {
            playerInputActions.Enable();
        }
        else
        {
            playerInputActions.Disable();
        }
    }
    private void InitializeSpriteMap()
    {
        spriteIndexMap = new Dictionary<char, int>
        {
            { '0', 29 }, { '1', 30 }, { '2', 31 }, { '3', 32 }, { '4', 33 },
            { '5', 34 }, { '6', 35 }, { '7', 36 }, { '8', 37 }, { '9', 38 },
            { 'p', 15 }, { 'o', 14 }, { 'n', 13 }, { 't', 19 }, { 's', 18 },
            { 'z', 25 }, { 'á', 26 }, { 'm', 12 },
            { '.', 39 }, { '!', 40 }, { '?', 41 }
        };
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maximumHealth;
            isDead.Value = false;
            score.Value = 0;
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
            playerInputActions.Player.Move.performed += OnMovePerformed;
            playerInputActions.Player.Move.canceled += OnMoveCanceled;
            playerInputActions.Player.Jump.performed += OnJumpPerformed;
            playerInputActions.Player.Sprint.performed += OnSprintPerformed;
            playerInputActions.Player.Sprint.canceled += OnSprintCanceled;
            playerInputActions.Player.Inventory.performed += OnInventoryPerformed;
            playerInputActions.Player.Inventory.canceled += OnInventoryCanceled;
            playerInputActions.Player.Slot1.performed += OnSlot1Performed;
            playerInputActions.Player.Slot2.performed += OnSlot2Performed;
            playerInputActions.Player.Slot3.performed += OnSlot3Performed;
            playerInputActions.Player.Slot4.performed += OnSlot4Performed;
            playerInputActions.Player.Slot5.performed += OnSlot5Performed;
            playerInputActions.Player.Menu.performed += OnMenuPerformed;
            playerInputActions.Enable();

            if (SaveManager.Instance != null && SaveManager.Instance.IsLoading)
            {
                LoadData(SaveManager.Instance.CurrentlyLoadedData);
                SaveManager.Instance.IsLoading = false;
            }
            if (inGameMenuManager == null) inGameMenuManager = FindFirstObjectByType<InGameMenuManager>();
            AssignCameraToPlayer();
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
            if (inventoryUI != null) inventoryUI.SetActive(false);
            if (healthBarSlider != null) healthBarSlider.maxValue = maximumHealth;

            OnHealthChanged(0, currentHealth.Value);
            displayedScore = score.Value;
            UpdateScoreText(score.Value);
            isPlayerSpawned = true;
        }
    }
    public override void OnNetworkDespawn()
    {
        // Leiratkozunk az eseményekről, hogy elkerüljük a memóriaszivárgást.
        networkSpeed.OnValueChanged -= OnSpeedChanged;
        networkIsGrounded.OnValueChanged -= OnIsGroundedChanged;
        networkIsJumping.OnValueChanged -= OnIsJumpingChanged;
        networkIsFalling.OnValueChanged -= OnIsFallingChanged;
        networkAttackTrigger.OnValueChanged -= OnAttackTriggered;
        currentHealth.OnValueChanged -= OnHealthChanged;
        score.OnValueChanged -= OnScoreChanged;
        isDead.OnValueChanged -= OnIsDeadChanged;
        isDamagedState.OnValueChanged -= OnIsDamagedStateChanged;

        if (IsOwner)
        {
            playerInputActions.Player.Move.performed -= OnMovePerformed;
            playerInputActions.Player.Move.canceled -= OnMoveCanceled;
            playerInputActions.Player.Jump.performed -= OnJumpPerformed;
            playerInputActions.Player.Sprint.performed -= OnSprintPerformed;
            playerInputActions.Player.Sprint.canceled -= OnSprintCanceled;
            playerInputActions.Player.Inventory.performed -= OnInventoryPerformed;
            playerInputActions.Player.Inventory.canceled -= OnInventoryCanceled;
            playerInputActions.Player.Slot1.performed -= OnSlot1Performed;
            playerInputActions.Player.Slot2.performed -= OnSlot2Performed;
            playerInputActions.Player.Slot3.performed -= OnSlot3Performed;
            playerInputActions.Player.Slot4.performed -= OnSlot4Performed;
            playerInputActions.Player.Slot5.performed -= OnSlot5Performed;
            playerInputActions.Player.Menu.performed -= OnMenuPerformed;
            playerInputActions.Disable();

            if (NetworkManager.Singleton != null) NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
        }
    }
    private void OnMovePerformed(InputAction.CallbackContext ctx) => networkMovementInput.Value = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => networkMovementInput.Value = Vector2.zero;
    private void OnJumpPerformed(InputAction.CallbackContext ctx) => RequestJumpServerRpc();
    private void OnSprintPerformed(InputAction.CallbackContext ctx) => isRunningInputPressedLocal = true;
    private void OnSprintCanceled(InputAction.CallbackContext ctx) => isRunningInputPressedLocal = false;
    private void OnInventoryPerformed(InputAction.CallbackContext ctx) => isInventoryInputPressedLocal = true;
    private void OnInventoryCanceled(InputAction.CallbackContext ctx) => isInventoryInputPressedLocal = false;
    private void OnSlot1Performed(InputAction.CallbackContext ctx) => AttemptAttackServerRpc(0);
    private void OnSlot2Performed(InputAction.CallbackContext ctx) => AttemptAttackServerRpc(1);
    private void OnSlot3Performed(InputAction.CallbackContext ctx) => slots.UseItemServerRpc(2);
    private void OnSlot4Performed(InputAction.CallbackContext ctx) => slots.UseItemServerRpc(3);
    private void OnSlot5Performed(InputAction.CallbackContext ctx) => slots.UseItemServerRpc(4);
    private void OnMenuPerformed(InputAction.CallbackContext ctx) => ToggleInGameMenu();
    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (IsOwner)
        {
            AssignCameraToPlayer();
        }
    }
    private void AssignCameraToPlayer()
    {
        var virtualCamera = FindAnyObjectByType<CinemachineCamera>();
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
    private void ToggleInGameMenu()
    {
        // Csak a helyi játékos nyithatja meg a menüt.
        if (IsOwner && inGameMenuManager != null)
        {
            inGameMenuManager.ToggleMenu();
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
        if (scoreText == null) return;

        string textToDisplay = $"pontszám {scoreValue}";
        var builder = new StringBuilder();

        foreach (char c in textToDisplay)
        {
            if (c == ' ')
            {
                builder.Append("<space=20>");
            }
            else
            {
                char lookupChar = char.ToLower(c);
                if (spriteIndexMap.ContainsKey(lookupChar))
                {
                    builder.Append($"<sprite index={spriteIndexMap[lookupChar]}>");
                }
            }
        }

        scoreText.text = builder.ToString();
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
        if (IsOwner)
        {
            var virtualCamera = FindAnyObjectByType<CinemachineCamera>();
            if (virtualCamera != null)
            {
                data.cameraPosition = virtualCamera.transform.position;
            }
        }
    }
    public void LoadData(GameData data)
    {
        if (IsServer)
        {
            transform.position = data.playerPosition;

            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            this.currentHealth.Value = data.currentHealth;
            this.score.Value = data.score;

            if (slots != null)
            {
                slots.LoadInventoryData(data.inventoryItems);
            }
        }
        if (IsOwner)
        {
            var virtualCamera = FindAnyObjectByType<CinemachineCamera>();
            if (virtualCamera != null)
            {
                virtualCamera.transform.position = data.cameraPosition;
            }
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