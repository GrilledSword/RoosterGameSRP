using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PekkaPlayerController : NetworkBehaviour, IDamageable, ISaveable
{
    #region Változók és Referenciák
    [Header("Mozgás Beállítások")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeedMultiplier = 1.5f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private int maxDoubleJumps = 1;
    [SerializeField] private float groundedStickyForce = 10f;

    [Header("Ugrás Beállítások")] // MÓDOSÍTVA: Külön szekció az ugrásnak
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpStaminaCost = 10f; // ÚJ: Az ugrás stamina költsége
    [SerializeField] private float hopForce = 3f; // ÚJ: A "szökkenés" ereje, ha nincs elég stamina

    [Header("Stamina Beállítások")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDepletionRate = 20f;
    [SerializeField] private float staminaRegenerationRate = 15f;
    [SerializeField] private float staminaRegenerationDelay = 2f;

    [Header("Glide Beállítások")]
    [SerializeField] private float glideFallSpeed = 2f;
    [SerializeField] private float glideStaminaCost = 15f; // ÚJ: A siklás stamina költsége másodpercenként

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
    [SerializeField] private float maximumMana = 100; // ÚJ

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> currentMana = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> currentStamina = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isExhausted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Coroutine staminaRegenCoroutine;
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> activeSpeedMultiplier = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Coroutine activePowerUpCoroutine;
    private NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Sebzés Kezelés")]
    [SerializeField] private float invincibilityDurationAfterDamage = 1.5f;
    private Coroutine damageInvincibilityCoroutine;
    private NetworkVariable<bool> isDamagedState = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Támadás Beállítások")]
    [SerializeField] private Transform projectileSpawnPoint;
    private NetworkVariable<bool> isAttackOnCooldown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Frakció Beállítás")]
    private Faction faction = Faction.Player;
    public Faction Faction => faction;

    [Header("Karakter UI")]
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private Slider manaBarSlider;
    [SerializeField] private Slider staminaBarSlider;
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
    private NetworkVariable<bool> networkIsGliding = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // ÚJ
    private NetworkVariable<bool> networkJumpInputHeld = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); // ÚJ

    private bool isRunningInputPressedLocal = false;
    private bool isJumpInputHeldLocal = false; // ÚJ
    private int currentDoubleJumps;
    private bool isInventoryInputPressedLocal = false;
    private PlayerControls playerInputActions;
    private Dictionary<char, int> spriteIndexMap;
    #endregion
    #region Unity Életciklus és Hálózati Metódusok
    void Awake()
    {
        playerInputActions = new PlayerControls();

        if (soundController == null)
        {
            soundController = GetComponent<PlayerSoundController>();
        }
        InitializeSpriteMap();
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maximumHealth;
            currentMana.Value = maximumMana; // ÚJ
            currentStamina.Value = maxStamina; // ÚJ
            isDead.Value = false;
            score.Value = 0;
        }

        // Eseménykezelők feliratkozása
        networkSpeed.OnValueChanged += OnSpeedChanged;
        networkIsGrounded.OnValueChanged += OnIsGroundedChanged;
        networkIsJumping.OnValueChanged += OnIsJumpingChanged;
        networkIsGliding.OnValueChanged += OnIsGlidingChanged; // ÚJ
        networkIsFalling.OnValueChanged += OnIsFallingChanged;
        networkAttackTrigger.OnValueChanged += OnAttackTriggered; // FONTOS: Feliratkozás az eseményre
        currentHealth.OnValueChanged += OnHealthChanged;
        currentMana.OnValueChanged += OnManaChanged; // ÚJ
        currentStamina.OnValueChanged += OnStaminaChanged; // ÚJ
        isExhausted.OnValueChanged += OnExhaustedChanged; // ÚJ
        score.OnValueChanged += OnScoreChanged;
        isDead.OnValueChanged += OnIsDeadChanged;
        isDamagedState.OnValueChanged += OnIsDamagedStateChanged;

        if (IsOwner)
        {
            // Input események
            playerInputActions.Player.Move.performed += OnMovePerformed;
            playerInputActions.Player.Move.canceled += OnMoveCanceled;
            playerInputActions.Player.Jump.performed += OnJumpPerformed; // MÓDOSÍTVA
            playerInputActions.Player.Jump.canceled += OnJumpCanceled; // ÚJ
            playerInputActions.Player.Sprint.performed += OnSprintPerformed;
            playerInputActions.Player.Sprint.canceled += OnSprintCanceled;
            playerInputActions.Player.Inventory.performed += OnInventoryPerformed;
            playerInputActions.Player.Inventory.canceled += OnInventoryCanceled;
            playerInputActions.Player.Slot1.performed += ctx => OnAttackInput(0); // JAVÍTVA
            playerInputActions.Player.Slot2.performed += ctx => OnAttackInput(1); // JAVÍTVA
            playerInputActions.Player.Slot3.performed += ctx => OnUseItemInput(2); // JAVÍTVA
            playerInputActions.Player.Slot4.performed += ctx => OnUseItemInput(3); // JAVÍTVA
            playerInputActions.Player.Slot5.performed += ctx => OnUseItemInput(4); // JAVÍTVA
            playerInputActions.Player.Menu.performed += OnMenuPerformed;
            playerInputActions.Enable();

            // Betöltés és UI beállítás
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
            if (manaBarSlider != null) manaBarSlider.maxValue = maximumMana; // ÚJ
            if (staminaBarSlider != null) staminaBarSlider.maxValue = maxStamina; // ÚJ

            OnHealthChanged(0, currentHealth.Value);
            OnManaChanged(0, currentMana.Value); // ÚJ
            OnStaminaChanged(0, currentStamina.Value); // ÚJ

            displayedScore = score.Value;
            UpdateScoreText(score.Value);
            isPlayerSpawned = true;
        }
    }
    public override void OnNetworkDespawn()
    {
        // Leiratkozás az eseményekről
        networkSpeed.OnValueChanged -= OnSpeedChanged;
        networkIsGrounded.OnValueChanged -= OnIsGroundedChanged;
        networkIsJumping.OnValueChanged -= OnIsJumpingChanged;
        networkIsGliding.OnValueChanged -= OnIsGlidingChanged; // ÚJ
        networkIsFalling.OnValueChanged -= OnIsFallingChanged;
        networkAttackTrigger.OnValueChanged -= OnAttackTriggered;
        currentHealth.OnValueChanged -= OnHealthChanged;
        currentMana.OnValueChanged -= OnManaChanged; // ÚJ
        currentStamina.OnValueChanged -= OnStaminaChanged; // ÚJ
        isExhausted.OnValueChanged -= OnExhaustedChanged; // ÚJ
        score.OnValueChanged -= OnScoreChanged;
        isDead.OnValueChanged -= OnIsDeadChanged;
        isDamagedState.OnValueChanged -= OnIsDamagedStateChanged;

        if (IsOwner)
        {
            playerInputActions.Player.Move.performed -= OnMovePerformed;
            playerInputActions.Player.Move.canceled -= OnMoveCanceled;
            playerInputActions.Player.Jump.performed -= OnJumpPerformed;
            playerInputActions.Player.Jump.canceled -= OnJumpCanceled; // ÚJ
            playerInputActions.Player.Sprint.performed -= OnSprintPerformed;
            playerInputActions.Player.Sprint.canceled -= OnSprintCanceled;
            playerInputActions.Player.Inventory.performed -= OnInventoryPerformed;
            playerInputActions.Player.Inventory.canceled -= OnInventoryCanceled;
            playerInputActions.Player.Slot1.performed -= ctx => OnAttackInput(0);
            playerInputActions.Player.Slot2.performed -= ctx => OnAttackInput(1);
            playerInputActions.Player.Slot3.performed -= ctx => OnUseItemInput(2);
            playerInputActions.Player.Slot4.performed -= ctx => OnUseItemInput(3);
            playerInputActions.Player.Slot5.performed -= ctx => OnUseItemInput(4);
            playerInputActions.Player.Menu.performed -= OnMenuPerformed;
            playerInputActions.Disable();

            if (NetworkManager.Singleton != null) NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
        }
    }
    void FixedUpdate()
    {
        if (IsServer)
        {
            CheckGroundStatus();
            HandleMovementAndStaminaServer(); // MÓDOSÍTVA
            HandleGlidingServer(); // ÚJ
            UpdateNetworkAnimatorParametersServer();

            if (networkIsGrounded.Value)
            {
                rb.AddForce(Vector3.down * groundedStickyForce, ForceMode.Force);
            }

            // JAVÍTVA: A triggert itt reseteljük, miután a kliensek megkapták az eseményt
            if (networkAttackTrigger.Value)
            {
                networkAttackTrigger.Value = false;
            }
        }
    }
    void Update()
    {
        if (IsOwner)
        {
            networkRunInput.Value = isRunningInputPressedLocal;
            networkJumpInputHeld.Value = isJumpInputHeldLocal; // ÚJ: Szinkronizáljuk a gomb lenyomva tartását
            if (!IsServer) UpdateLocalAnimatorParameters(); // Csak a nem-szerver tulajdonosoknak kell ezt futtatni
            UpdateCameraRootPosition();
            HandleInventoryUI();
        }
    }
    #endregion
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
    private void OnMovePerformed(InputAction.CallbackContext ctx) => networkMovementInput.Value = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => networkMovementInput.Value = Vector2.zero;
    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        isJumpInputHeldLocal = true; // ÚJ
        RequestJumpServerRpc();
    }
    private void OnJumpCanceled(InputAction.CallbackContext ctx) // ÚJ
    {
        isJumpInputHeldLocal = false;
    }
    private void OnSprintPerformed(InputAction.CallbackContext ctx) => isRunningInputPressedLocal = true;
    private void OnSprintCanceled(InputAction.CallbackContext ctx) => isRunningInputPressedLocal = false;
    private void OnInventoryPerformed(InputAction.CallbackContext ctx) => isInventoryInputPressedLocal = true;
    private void OnInventoryCanceled(InputAction.CallbackContext ctx) => isInventoryInputPressedLocal = false;
    private void OnMenuPerformed(InputAction.CallbackContext ctx) => ToggleInGameMenu();
    private void OnAttackInput(int slotIndex)
    {
        if (IsOwner)
        {
            RequestAttackServerRpc(slotIndex);
        }
    }
    private void OnUseItemInput(int slotIndex)
    {
        if (IsOwner && slots != null)
        {
            slots.UseItemServerRpc(slotIndex);
        }
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
        var virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.Follow = cameraRoot != null ? cameraRoot.transform : transform;
            virtualCamera.LookAt = cameraRoot != null ? cameraRoot.transform : transform;
        }
    }
    private void ToggleInGameMenu()
    {
        if (IsOwner && inGameMenuManager != null)
        {
            inGameMenuManager.ToggleMenu();
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
    private void HandleGlidingServer()
    {
        // Csak akkor siklunk, ha a levegőben vagyunk, esünk, és a játékos nyomva tartja az ugrás gombot
        if (!networkIsGrounded.Value && networkJumpInputHeld.Value && rb.linearVelocity.y < 0)
        {
            networkIsGliding.Value = true;
            Vector3 velocity = rb.linearVelocity;
            velocity.y = -glideFallSpeed; // Beállítjuk a lassabb esési sebességet
            rb.linearVelocity = velocity;
        }
        else
        {
            networkIsGliding.Value = false;
        }
    }
    private void HandleMovementAndStaminaServer()
    {
        if (isDead.Value) return;
        Vector3 moveDirection = new Vector3(networkMovementInput.Value.x, 0f, 0f).normalized;
        float targetSpeed = moveSpeed * activeSpeedMultiplier.Value;

        // Sprintelés logikája
        bool isTryingToSprint = networkRunInput.Value && !isDamagedState.Value && moveDirection.magnitude > 0.1f;

        if (isTryingToSprint && !isExhausted.Value)
        {
            targetSpeed *= runSpeedMultiplier;
            currentStamina.Value -= staminaDepletionRate * Time.fixedDeltaTime;

            if (currentStamina.Value <= 0)
            {
                currentStamina.Value = 0;
                isExhausted.Value = true;
            }

            if (staminaRegenCoroutine != null)
            {
                StopCoroutine(staminaRegenCoroutine);
                staminaRegenCoroutine = null;
            }
        }
        else
        {
            if (currentStamina.Value < maxStamina && staminaRegenCoroutine == null)
            {
                staminaRegenCoroutine = StartCoroutine(RegenerateStamina());
            }
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

        bool isIdleAndTired = isExhausted.Value && currentHorizontalSpeed < 0.1f && networkIsGrounded.Value;
        animator.SetBool("IsExhausted", isIdleAndTired);

        if (!networkIsGrounded.Value)
        {
            // A siklás felülírja az esés és ugrás animációt
            if (!networkIsGliding.Value)
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
    private void RequestAttackServerRpc(int slotIndex)
    {
        if (isAttackOnCooldown.Value || isDead.Value || slots == null) return;

        ItemData weaponData = slots.GetItemAt(slotIndex);
        if (weaponData.isEmpty) return;

        ItemDefinition weaponDef = ItemManager.Instance.GetItemDefinition(weaponData.itemID);
        if (weaponDef == null || weaponDef.category != ItemCategory.Weapon || weaponDef.projectilePrefab == null)
        {
            return;
        }

        // KÉSŐBB ITT KELL MAJD ELLENŐRIZNI A MANA KÖLTSÉGET
        // float manaCost = (weaponDef is WeaponItemDefinition) ? (weaponDef as WeaponItemDefinition).manaCost : 0;
        // if (currentMana.Value < manaCost) return;

        // ConsumeManaServerRpc(manaCost);

        StartCoroutine(AttackCooldownCoroutine(weaponDef.shootDuration));

        GameObject projectileInstance = Instantiate(weaponDef.projectilePrefab, projectileSpawnPoint.position, transform.rotation);
        NetworkObject netObj = projectileInstance.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        if (projectileInstance.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.Initialize(weaponDef.shootDistance);
        }

        networkAttackTrigger.Value = true;
        PlayAttackSoundClientRpc(weaponData.itemID);
    }
    [ServerRpc]
    public void ConsumeManaServerRpc(float amount)
    {
        if (isDead.Value) return;
        currentMana.Value = Mathf.Max(currentMana.Value - amount, 0);
    }
    [ClientRpc]
    private void PlayAttackSoundClientRpc(int itemID)
    {
        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemID);
        if (itemDef != null && itemDef.useSound != null && soundController != null)
        {
            soundController.audioSource.PlayOneShot(itemDef.useSound);
        }
    }
    private void OnManaChanged(float oldMana, float newMana)
    {
        if (IsOwner && manaBarSlider != null)
        {
            manaBarSlider.value = newMana;
        }
    }
    private void OnStaminaChanged(float oldStamina, float newStamina)
    {
        if (IsOwner && staminaBarSlider != null)
        {
            staminaBarSlider.value = newStamina;
        }
    }
    private void OnExhaustedChanged(bool oldVal, bool newVal)
    {
        // Ez a kliensen is lefut, hogy az animáció azonnal frissüljön
        if (animator != null)
        {
            animator.SetBool("IsExhausted", newVal);
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
        if (newAttackTrigger)
        {
            animator.SetTrigger("Attack");
        }
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

        bool isIdleAndTired = isExhausted.Value && currentHorizontalSpeed < 0.1f && currentLocalIsGrounded;
        animator.SetBool("IsExhausted", isIdleAndTired);

        animator.SetBool("IsGliding", networkIsGliding.Value); // ÚJ

        if (!Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer))
        {
            if (!networkIsGliding.Value)
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
        else
        {
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsFalling", false);
        }
    }
    private void OnIsGlidingChanged(bool oldVal, bool newVal)
    {
        if (animator != null)
        {
            animator.SetBool("IsGliding", newVal);
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
        data.currentMana = this.currentMana.Value; // ÚJ
        data.currentStamina = this.currentStamina.Value; // ÚJ
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
            this.currentMana.Value = data.currentMana; // ÚJ
            this.currentStamina.Value = data.currentStamina; // ÚJ
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
    private IEnumerator AttackCooldownCoroutine(float duration)
    {
        isAttackOnCooldown.Value = true;
        yield return new WaitForSeconds(duration);
        isAttackOnCooldown.Value = false;
    }
    private IEnumerator RegenerateStamina()
    {
        yield return new WaitForSeconds(staminaRegenerationDelay);

        while (currentStamina.Value < maxStamina)
        {
            currentStamina.Value += staminaRegenerationRate * Time.deltaTime;
            currentStamina.Value = Mathf.Min(currentStamina.Value, maxStamina);

            // Ha a stamina eléri a felét, a játékos újra tud sprintelni
            if (isExhausted.Value && currentStamina.Value >= maxStamina / 2)
            {
                isExhausted.Value = false;
            }

            yield return null;
        }
        staminaRegenCoroutine = null;
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

//### Teendőid a kóddal kapcsolatban

//Most, hogy a kód készen áll, van néhány beállítás, amit el kell végezned a Unity Editorban, hogy minden működjön.

//---

//### 1. UI Beállítása
//A Player prefabodon (vagy ahol a `PekkaPlayerController` szkript van) megjelentek új mezők:
//-**Mana Bar Slider**: Ide húzd be a Mana csíkot reprezentáló `Slider` komponenst a Hierarchy-ből.
//-**Stamina Bar Slider**: Ide pedig a Stamina csíkot reprezentáló `Slider`-t.
//Győződj meg róla, hogy mindkét Slider `MaxValue` értéke `100` (vagy amennyit beállítottál a szkriptben).

//---

//### 2. Animator Beállítása
//A "lihegő" animációhoz az Animator Controllerben kell módosításokat tenned:
//1.  * *Új Paraméter * *: Nyisd meg az Animator ablakot, és a "Parameters" fülön adj hozzá egy új **Bool** típusú paramétert. Nevezd el `IsExhausted`-nek.
//2.  **Új Állapot**: Húzd be a lihegő animációdat az Animator ablakba, hogy létrejöjjön egy új állapot (state).
//3.  **Átmenetek (Transitions)**:
//*Készíts egy átmenetet az **Idle** állapotból a **lihegő** állapotba. Ennek a feltétele (`Condition`) legyen: `IsExhausted` -> `true`.
//*Készíts egy átmenetet a **lihegő** állapotból vissza az **Idle** állapotba. Ennek a feltétele legyen: `IsExhausted` -> `false`.
//*Valószínűleg a **Walk/Run** állapotokból is kell majd egy átmenet az **Idle**-be, hogy onnan tudjon a lihegő állapotba váltani, ha a játékos megáll.

//---

//### 3. Mana Költség (Jövőbeli feladat)
//Ahogy a kódban is jeleztem, a mana rendszer még csak elő van készítve. Amikor olyan fegyvereket készítesz, amik manát használnak, a következőket kell majd tenned:
//1.Nyisd meg az `ItemDefinition.cs` (vagy ha van, a `WeaponItemDefinition.cs`) szkriptet.
//2.  Adj hozzá egy új változót: `public float manaCost = 0;`.
//3.Ezután a `RequestAttackServerRpc` metódusban a kommentbe tett részeket élesítheted, hogy a fegyverek ténylegesen fogyasszák a manát.

//Remélem, ez segít! Jó munkát a Pekka Kana 2 folytatásához, nagyon izgalmasan hangzik! 

//### Összefoglalás és teendőid

//1.  * *Glide Mechanika * *:
//    *Hozzáadtam egy `glideFallSpeed` változót, amit az Inspectorban állíthatsz. Ez határozza meg, milyen lassan essen a karakter siklás közben.
//    * A rendszer figyeli, hogy a játékos a levegőben van-e, esik-e (`velocity.y < 0`), és lenyomva tartja-e az ugrás gombot.
//    * Ha ezek a feltételek teljesülnek, a szerver aktiválja a `networkIsGliding` állapotot, és a `Rigidbody` esési sebességét a `glideFallSpeed`-re korlátozza.
//    * A `networkIsGliding` változó szinkronizálja az animációt minden kliens számára.

//2.  **Input Kezelés**:
//    *Az `PlayerControls` `Jump` akciójának most már a `performed` (lenyomás) és a `canceled` (felengedés) eseményét is figyeljük, hogy tudjuk, a játékos lenyomva tartja-e a gombot.

//3.  **Animáció**:
//    *A siklás animációja felülírja az ugrás és esés animációkat, amikor aktív.

//---

//### Amit még neked kell megtenned

//1.  **Unity Editor**:
//    *A `PekkaPlayerController` szkripten az Inspectorban állítsd be a `Glide Fall Speed` értékét egy neked tetsző alacsony számra (pl. `2` vagy `3`).

//2.  **Animator Controller**:
//    *Nyisd meg a karaktered Animator Controllerét.
//    * A "Parameters" fülön adj hozzá egy új **Bool** paramétert, és nevezd el `IsGliding`-nek.
//    * Húzd be a siklás animációdat az Animator ablakba.
//    * Készíts egy átmenetet (transition) az **Falling** (vagy a fő levegőben lévő) állapotból a **Glide** állapotba. A feltétele (`Condition`) legyen: `IsGliding` -> `true`.
//    * Készíts egy átmenetet a **Glide** állapotból vissza a **Falling** állapotba. A feltétele legyen: `IsGliding` -> `false`.
//    * Győződj meg róla, hogy a `Has Exit Time` ki van kapcsolva ezeknél az átmeneteknél, hogy azonnal reagáljanak a gombnyomásra.

//Ezekkel a kiegészítésekkel a karaktered képes lesz a levegőben siklani. Jó munkát a továbbiakh