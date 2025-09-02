using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#region Inspector Beállítási Osztályok
[System.Serializable]
public class PlayerStats
{
    [Tooltip("A karakter maximális életereje.")]
    public float maximumHealth = 100;
    [Tooltip("A karakter maximális manája.")]
    public float maximumMana = 100;
}

[System.Serializable]
public class MovementSettings
{
    [Tooltip("Az alap mozgási sebesség.")]
    public float moveSpeed = 5f;
    [Tooltip("A sprintelés sebességének szorzója.")]
    public float runSpeedMultiplier = 1.5f;
    [Tooltip("A sugár, amiben a karakter a földet keresi.")]
    public float groundCheckRadius = 0.2f;
    [Tooltip("A réteg, amit a karakter földnek érzékel.")]
    public LayerMask groundLayer;
    [Tooltip("Mennyire tapadjon a karakter a földhöz, hogy ne csússzon le a lejtőkön.")]
    public float groundedStickyForce = 10f;
}

[System.Serializable]
public class JumpSettings
{
    [Tooltip("Az ugrás ereje.")]
    public float jumpForce = 10f;
    // JAVÍTVA: A dupla ugrás változó eltávolítva
    [Tooltip("A 'szökkenés' ereje, ha nincs elég stamina.")]
    public float hopForce = 3f;
    [Tooltip("Mennyire csökkentse a sebességet a gomb elengedésekor (0 és 1 között).")]
    [Range(0f, 1f)]
    public float jumpCutoffMultiplier = 0.5f;
}

[System.Serializable]
public class StaminaSettings
{
    [Tooltip("A karakter maximális staminája.")]
    public float maxStamina = 100f;
    [Tooltip("Stamina fogyás másodpercenként sprinteléskor.")]
    public float sprintDepletionRate = 20f;
    [Tooltip("Stamina visszatöltődés másodpercenként.")]
    public float regenerationRate = 15f;
    [Tooltip("Késleltetés másodpercben, mielőtt a stamina elkezd visszatöltődni.")]
    public float regenerationDelay = 2f;
    [Tooltip("A teljes (magas) ugrás stamina költsége.")] // JAVÍTVA
    public float fullJumpCost = 15f;
    [Tooltip("A rövid ugrás stamina költsége.")] // JAVÍTVA
    public float shortJumpCost = 5f;
    [Tooltip("Az ugrás stamina költsége.")]
    public float jumpCost = 10f;
    [Tooltip("A siklás stamina költsége másodpercenként.")]
    public float glideCost = 15f;
}

[System.Serializable]
public class GlideSettings
{
    [Tooltip("A siklás közbeni esési sebesség.")]
    public float fallSpeed = 2f;
}

[System.Serializable]
public class CombatSettings
{
    [Tooltip("A pont, ahonnan a lövedékek indulnak.")]
    public Transform projectileSpawnPoint;
    [Tooltip("Mennyi ideig sebezhetetlen a karakter sebződés után.")]
    public float invincibilityDurationAfterDamage = 1.5f;
}

[System.Serializable]
public class ComponentReferences
{
    [Tooltip("A karakter Rigidbody komponense.")]
    public Rigidbody rb;
    [Tooltip("A karakter Animator komponense.")]
    public Animator animator;
    [Tooltip("A transzform, ami a földet ellenőrzi.")]
    public Transform groundCheck;
    [Tooltip("A karakter hangjait kezelő szkript.")]
    public PlayerSoundController soundController;
    [Tooltip("A kamera gyökérobjektuma a finomabb mozgásért.")]
    public GameObject cameraRoot;
    [Tooltip("A karakter inventory-ját kezelő szkript.")]
    public Slots slots;
}

[System.Serializable]
public class UIReferences
{
    [Tooltip("Az életerő csíkot megjelenítő Slider.")]
    public Slider healthBarSlider;
    [Tooltip("A mana csíkot megjelenítő Slider.")]
    public Slider manaBarSlider;
    [Tooltip("A stamina csíkot megjelenítő Slider.")]
    public Slider staminaBarSlider;
    [Tooltip("A pontszámot megjelenítő szöveg.")]
    public TextMeshProUGUI scoreText;
    [Tooltip("A játék közbeni menüt kezelő szkript.")]
    public InGameMenuManager inGameMenuManager;
    [Tooltip("Az inventory UI panelje.")]
    public GameObject inventoryUI;
}
[System.Serializable]
public class UISettings
{
    [Tooltip("Milyen gyorsan kövessék a UI csíkok (élet, mana, stb.) a tényleges értéket. Minél nagyobb, annál gyorsabb.")]
    public float barSmoothSpeed = 10f;
    [Tooltip("A pontszám számláló animációjának hossza másodpercben.")]
    public float scoreCountingDuration = 0.5f;
}
#endregion
public class PekkaPlayerController : NetworkBehaviour, IDamageable, ISaveable
{
    #region Változók és Referenciák

    [Header("Karakter Beállítások")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private MovementSettings movement;
    [SerializeField] private JumpSettings jump;
    [SerializeField] private StaminaSettings stamina;
    [SerializeField] private GlideSettings glide;
    [SerializeField] private CombatSettings combat;

    [Header("Komponens és UI Referenciák")]
    [SerializeField] private ComponentReferences components;
    [SerializeField] private UIReferences ui;
    [SerializeField] private UISettings uiSettings;

    [Header("Kamera Beállítások")]
    [SerializeField] private float cameraRootSmoothSpeed = 5f;
    [SerializeField] private float cameraRootZPosition;

    [Header("Frakció Beállítás")]
    private Faction faction = Faction.Player;
    public Faction Faction => faction;

    // Network Változók (Karakter Állapot)
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> currentMana = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> currentStamina = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isExhausted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> activeSpeedMultiplier = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isDamagedState = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isAttackOnCooldown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Network Változók (Mozgás Állapot)
    private NetworkVariable<Vector2> networkMovementInput = new NetworkVariable<Vector2>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkAttackTrigger = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkRunInput = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsGrounded = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsJumping = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsFalling = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsGliding = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkJumpInputHeld = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Lokális Változók
    private Coroutine staminaRegenCoroutine;
    private Coroutine activePowerUpCoroutine;
    private Coroutine damageInvincibilityCoroutine;
    private Coroutine scoreCountingCoroutine;
    private bool isRunningInputPressedLocal = false;
    private bool isJumpInputHeldLocal = false;
    private bool isInventoryInputPressedLocal = false;
    private PlayerControls playerInputActions;
    private Dictionary<char, int> spriteIndexMap;
    private bool canRequestContinuousJump = true;
    private float displayedScore = 0f;
    private bool isPlayerSpawned = false;
    private bool lastJumpWasEligibleForRefund = false;

    #endregion

    #region Unity Életciklus és Hálózati Metódusok
    void Awake()
    {
        playerInputActions = new PlayerControls();
        if (components.soundController == null) components.soundController = GetComponent<PlayerSoundController>();
        InitializeSpriteMap();
    }
    private CinemachineCamera _vcam;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = stats.maximumHealth;
            currentMana.Value = stats.maximumMana;
            currentStamina.Value = stamina.maxStamina;
            isDead.Value = false;
            score.Value = 0;
        }

        SubscribeToEvents();

        if (IsOwner)
        {
            _vcam = FindFirstObjectByType<CinemachineCamera>();
            if (_vcam != null)
            {
                _vcam.Follow = transform;
            }
            SubscribeToOwnerEvents();
            InitializeOwner();
        }
    }
    void FixedUpdate()
    {
        if (IsServer)
        {
            ServerTick();
        }
    }
    void Update()
    {
        if (InGameMenuManager.GameIsPaused)
        {
            return;
        }
        if (IsOwner)
        {
            OwnerTick();
        }
    }
    public override void OnNetworkDespawn()
    {
        UnsubscribeFromEvents();
        if (IsOwner)
        {
            UnsubscribeFromOwnerEvents();
        }
    }
    #endregion

    #region Inicializálás
    private void InitializeOwner()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoading)
        {
            LoadData(SaveManager.Instance.CurrentlyLoadedData);
            SaveManager.Instance.IsLoading = false;
        }
        if (ui.inGameMenuManager == null) ui.inGameMenuManager = FindFirstObjectByType<InGameMenuManager>();
        AssignCameraToPlayer();
        if (ui.inventoryUI != null) ui.inventoryUI.SetActive(false);

        if (ui.healthBarSlider != null) ui.healthBarSlider.maxValue = stats.maximumHealth;
        if (ui.manaBarSlider != null) ui.manaBarSlider.maxValue = stats.maximumMana;
        if (ui.staminaBarSlider != null) ui.staminaBarSlider.maxValue = stamina.maxStamina;

        if (IsOwner)
        {
            if (ui.healthBarSlider != null) ui.healthBarSlider.value = currentHealth.Value;
            if (ui.manaBarSlider != null) ui.manaBarSlider.value = currentMana.Value;
            if (ui.staminaBarSlider != null) ui.staminaBarSlider.value = currentStamina.Value;
        }

        OnHealthChanged(0, currentHealth.Value);
        OnManaChanged(0, currentMana.Value);
        OnStaminaChanged(0, currentStamina.Value);

        displayedScore = score.Value;
        UpdateScoreText(score.Value);
        isPlayerSpawned = true;
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
    #endregion

    #region Eseménykezelés (Fel- és Leiratkozás)
    private void SubscribeToEvents()
    {
        networkSpeed.OnValueChanged += OnSpeedChanged;
        networkIsGrounded.OnValueChanged += OnIsGroundedChanged;
        networkIsJumping.OnValueChanged += OnIsJumpingChanged;
        networkIsGliding.OnValueChanged += OnIsGlidingChanged;
        networkIsFalling.OnValueChanged += OnIsFallingChanged;
        networkAttackTrigger.OnValueChanged += OnAttackTriggered;
        //currentHealth.OnValueChanged += OnHealthChanged;
        //currentMana.OnValueChanged += OnManaChanged;
        //currentStamina.OnValueChanged += OnStaminaChanged;
        isExhausted.OnValueChanged += OnExhaustedChanged;
        score.OnValueChanged += OnScoreChanged;
        isDead.OnValueChanged += OnIsDeadChanged;
        isDamagedState.OnValueChanged += OnIsDamagedStateChanged;
    }
    private void UnsubscribeFromEvents()
    {
        networkSpeed.OnValueChanged -= OnSpeedChanged;
        networkIsGrounded.OnValueChanged -= OnIsGroundedChanged;
        networkIsJumping.OnValueChanged -= OnIsJumpingChanged;
        networkIsGliding.OnValueChanged -= OnIsGlidingChanged;
        networkIsFalling.OnValueChanged -= OnIsFallingChanged;
        networkAttackTrigger.OnValueChanged -= OnAttackTriggered;
        //currentHealth.OnValueChanged -= OnHealthChanged;
        //currentMana.OnValueChanged -= OnManaChanged;
        //currentStamina.OnValueChanged -= OnStaminaChanged;
        isExhausted.OnValueChanged -= OnExhaustedChanged;
        score.OnValueChanged -= OnScoreChanged;
        isDead.OnValueChanged -= OnIsDeadChanged;
        isDamagedState.OnValueChanged -= OnIsDamagedStateChanged;
    }
    private void SubscribeToOwnerEvents()
    {
        playerInputActions.Player.Move.performed += OnMovePerformed;
        playerInputActions.Player.Move.canceled += OnMoveCanceled;
        playerInputActions.Player.Jump.performed += OnJumpPerformed;
        playerInputActions.Player.Jump.canceled += OnJumpCanceled;
        playerInputActions.Player.Sprint.performed += OnSprintPerformed;
        playerInputActions.Player.Sprint.canceled += OnSprintCanceled;
        playerInputActions.Player.Inventory.performed += OnInventoryPerformed;
        playerInputActions.Player.Inventory.canceled += OnInventoryCanceled;
        playerInputActions.Player.Slot1.performed += ctx => OnAttackInput(0);
        playerInputActions.Player.Slot2.performed += ctx => OnAttackInput(1);
        playerInputActions.Player.Slot3.performed += ctx => OnUseItemInput(2);
        playerInputActions.Player.Slot4.performed += ctx => OnUseItemInput(3);
        playerInputActions.Player.Slot5.performed += ctx => OnUseItemInput(4);
        playerInputActions.Player.Menu.performed += OnMenuPerformed;
        playerInputActions.Enable();
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
    }
    private void UnsubscribeFromOwnerEvents()
    {
        playerInputActions.Player.Move.performed -= OnMovePerformed;
        playerInputActions.Player.Move.canceled -= OnMoveCanceled;
        playerInputActions.Player.Jump.performed -= OnJumpPerformed;
        playerInputActions.Player.Jump.canceled -= OnJumpCanceled;
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
    #endregion

    #region Input Kezelés
    private void OnMovePerformed(InputAction.CallbackContext ctx) => networkMovementInput.Value = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => networkMovementInput.Value = Vector2.zero;
    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        isJumpInputHeldLocal = true;
        RequestJumpServerRpc();
    }
    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        isJumpInputHeldLocal = false;
        RequestJumpCutoffServerRpc();
    }
    private void OnSprintPerformed(InputAction.CallbackContext ctx) => isRunningInputPressedLocal = true;
    private void OnSprintCanceled(InputAction.CallbackContext ctx) => isRunningInputPressedLocal = false;
    private void OnInventoryPerformed(InputAction.CallbackContext ctx) => isInventoryInputPressedLocal = true;
    private void OnInventoryCanceled(InputAction.CallbackContext ctx) => isInventoryInputPressedLocal = false;
    private void OnMenuPerformed(InputAction.CallbackContext ctx) => ToggleInGameMenu();
    private void OnAttackInput(int slotIndex)
    {

        if (IsOwner) RequestAttackServerRpc(slotIndex);
    }
    private void OnUseItemInput(int slotIndex)
    {
        if (IsOwner && components.slots != null) components.slots.UseItemServerRpc(slotIndex);
    }
    #endregion

    #region Tick Metódusok (Szerver és Kliens)
    private void ServerTick()
    {
        CheckGroundStatus();
        HandleMovementAndStaminaServer();
        HandleGlidingServer();
        UpdateNetworkAnimatorParametersServer();

        if (networkIsGrounded.Value)
        {
            components.rb.AddForce(Vector3.down * movement.groundedStickyForce, ForceMode.Force);
        }

        if (networkAttackTrigger.Value)
        {
            networkAttackTrigger.Value = false;
        }
    }
    private void OwnerTick()
    {
        networkRunInput.Value = isRunningInputPressedLocal;
        networkJumpInputHeld.Value = isJumpInputHeldLocal;

        if (isJumpInputHeldLocal && networkIsGrounded.Value && canRequestContinuousJump && currentStamina.Value >= stamina.fullJumpCost)
        {
            RequestJumpServerRpc();
            StartCoroutine(JumpRequestCooldown());
        }

        if (!IsServer) UpdateLocalAnimatorParameters();
        UpdateCameraRootPosition();
        HandleInventoryUI();
        HandleStatBarsSmoothly();
    }
    #endregion

    #region Mozgás és Képességek (Szerver Oldal)
    private void CheckGroundStatus()
    {
        networkIsGrounded.Value = Physics.CheckSphere(components.groundCheck.position, movement.groundCheckRadius, movement.groundLayer);
    }
    private void HandleMovementAndStaminaServer()
    {
        if (isDead.Value) return;
        Vector3 moveDirection = new Vector3(networkMovementInput.Value.x, 0f, 0f).normalized;
        float targetSpeed = movement.moveSpeed * activeSpeedMultiplier.Value;

        bool isTryingToSprint = networkRunInput.Value && !isDamagedState.Value && moveDirection.magnitude > 0.1f;

        if (isTryingToSprint && !isExhausted.Value)
        {
            targetSpeed *= movement.runSpeedMultiplier;
            currentStamina.Value -= stamina.sprintDepletionRate * Time.fixedDeltaTime;

            if (currentStamina.Value <= 0)
            {
                currentStamina.Value = 0;
                isExhausted.Value = true;
            }
            StopStaminaRegen();
        }
        else
        {
            if (currentStamina.Value < stamina.maxStamina && staminaRegenCoroutine == null && !networkIsGliding.Value)
            {
                staminaRegenCoroutine = StartCoroutine(RegenerateStamina());
            }
        }

        components.rb.linearVelocity = new Vector3(moveDirection.x * targetSpeed, components.rb.linearVelocity.y, 0f);
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0, 0));
        }
    }
    private void HandleGlidingServer()
    {
        if (!networkIsGrounded.Value && networkJumpInputHeld.Value && components.rb.linearVelocity.y < 0 && currentStamina.Value > 0)
        {
            networkIsGliding.Value = true;
            Vector3 velocity = components.rb.linearVelocity;
            velocity.y = -glide.fallSpeed;
            components.rb.linearVelocity = velocity;

            currentStamina.Value -= stamina.glideCost * Time.fixedDeltaTime;
            StopStaminaRegen();
        }
        else
        {
            networkIsGliding.Value = false;
        }
    }
    [ServerRpc]
    private void RequestJumpServerRpc()
    {
        if (isDead.Value || !networkIsGrounded.Value) return;

        bool jumpExecuted = false;
        lastJumpWasEligibleForRefund = false;

        if (currentStamina.Value >= stamina.shortJumpCost)
        {
            currentStamina.Value -= stamina.fullJumpCost;
            components.rb.AddForce(Vector3.up * jump.jumpForce, ForceMode.Impulse);
            jumpExecuted = true;
            lastJumpWasEligibleForRefund = true;
            StopStaminaRegen();
        }
        else
        {
            components.rb.AddForce(Vector3.up * jump.hopForce, ForceMode.Impulse);
            jumpExecuted = true;
        }

        if (jumpExecuted && components.soundController != null)
        {
            components.soundController.PlayJumpSoundClientRpc();
        }
    }
    [ServerRpc]
    private void RequestJumpCutoffServerRpc()
    {
        if (components.rb.linearVelocity.y > 0)
        {
            components.rb.linearVelocity = new Vector3(components.rb.linearVelocity.x, components.rb.linearVelocity.y * jump.jumpCutoffMultiplier, components.rb.linearVelocity.z);
        }

        if (lastJumpWasEligibleForRefund)
        {
            float refundAmount = stamina.fullJumpCost - stamina.shortJumpCost;
            currentStamina.Value = Mathf.Min(stamina.maxStamina, currentStamina.Value + refundAmount);
            lastJumpWasEligibleForRefund = false;
        }
    }
    [ServerRpc]
    private void RequestAttackServerRpc(int slotIndex)
    {
        if (isAttackOnCooldown.Value || isDead.Value || components.slots == null) return;
        if(InGameMenuManager.GameIsPaused) return;

        ItemData weaponData = components.slots.GetItemAt(slotIndex);
        if (weaponData.isEmpty) return;

        if (ItemManager.Instance.GetItemDefinition(weaponData.itemID) is not WeaponItemDefinition weaponDef || weaponDef.projectilePrefab == null)
        {
            return;
        }
       
        if (weaponDef.manaCost > 0)
        {
            if (currentMana.Value < weaponDef.manaCost) return; // Nincs elég mana
            ConsumeManaServerRpc(weaponDef.manaCost);
        }

        if (weaponDef.staminaCost > 0)
        {
            if (currentStamina.Value < weaponDef.staminaCost) return; // Nincs elég stamina
            ConsumeStaminaServerRpc(weaponDef.staminaCost);
        }

        StartCoroutine(AttackCooldownCoroutine(weaponDef.shootDuration));

        GameObject projectileInstance = Instantiate(weaponDef.projectilePrefab, combat.projectileSpawnPoint.position, transform.rotation);
        NetworkObject netObj = projectileInstance.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        if (projectileInstance.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.Initialize(weaponDef.shootDistance, faction); // JAVÍTVA: Átadjuk a frakciót
        }

        networkAttackTrigger.Value = true;
        PlayAttackSoundClientRpc(weaponData.itemID);
    }
    #endregion

    #region Karakter Állapotok (Élet, Mana, Sebzés)
    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(float amount)
    {
        if (isDead.Value) return;
        currentHealth.Value = Mathf.Min(currentHealth.Value + amount, stats.maximumHealth);
    }
    [ServerRpc(RequireOwnership = false)]
    public void ConsumeManaServerRpc(float amount)
    {
        if (isDead.Value) return;
        currentMana.Value = Mathf.Max(0, currentMana.Value - amount);
    }
    [ServerRpc(RequireOwnership = false)]
    public void ConsumeStaminaServerRpc(float amount)
    {
        if (isDead.Value) return;
        currentStamina.Value = Mathf.Max(0, currentStamina.Value - amount);
        StopStaminaRegen();
    }
    [ServerRpc(RequireOwnership = false)]
    public void ApplyPowerUpServerRpc(int itemID)
    {
        if (isDead.Value) return;
        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemID);
        if (itemDef is PowerUpItemDefinition powerUp)
        {
            if (activePowerUpCoroutine != null) StopCoroutine(activePowerUpCoroutine);
            activePowerUpCoroutine = StartCoroutine(PowerUpCoroutine(powerUp));
        }
    }
    [ServerRpc]
    private void DieServerRpc()
    {
        isDead.Value = true;
        components.rb.linearVelocity = Vector3.zero;
        components.rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;
        if (components.soundController != null) components.soundController.PlayDeathSoundClientRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(int amount)
    {
        if (isDead.Value || amount == 0) return;
        score.Value += amount;
    }
    [ClientRpc]
    public void TeleportPlayerClientRpc(Vector3 position)
    {
        // A Rigidbody teleportálásához a pozícióját és a sebességét is állítani kell.
        if (components.rb != null)
        {
            components.rb.position = position;
            components.rb.linearVelocity = Vector3.zero;
        }
        else
        {
            // Fallback, ha valamiért nincs Rigidbody
            transform.position = position;
        }
    }
    public void Respawn()
    {
        if (!IsServer) return;

        // Get spawn position
        Vector3 spawnPosition = Vector3.zero;
        SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();
        if (spawnManager != null)
        {
            spawnPosition = spawnManager.GetNextSpawnPoint().position;
        }
        else
        {
            Debug.LogWarning("No SpawnManager found in scene, respawning at (0,0,0).");
        }

        // Reset health and call the RPC with the new position
        currentHealth.Value = stats.maximumHealth;
        isDead.Value = false;
        TeleportPlayerClientRpc(spawnPosition); // Újrahasznosítjuk a teleport RPC-t
    }
    public void TakeDamage(float damage, Faction sourceFaction)
    {
        if (IsServer)
        {
            if (isDead.Value || isInvincible.Value) return;

            currentHealth.Value -= damage;
            if (components.soundController != null) components.soundController.PlayDamageSoundClientRpc();

            if (damageInvincibilityCoroutine != null) StopCoroutine(damageInvincibilityCoroutine);
            damageInvincibilityCoroutine = StartCoroutine(DamageInvincibilityCoroutine());

            if (currentHealth.Value <= 0)
            {
                currentHealth.Value = 0;
                DieServerRpc();
            }
        }
    }
    #endregion

    #region Coroutine-ok
    private IEnumerator RegenerateStamina()
    {
        yield return new WaitForSeconds(stamina.regenerationDelay);

        while (currentStamina.Value < stamina.maxStamina)
        {
            float newValue = currentStamina.Value + stamina.regenerationRate * Time.deltaTime;

            if (newValue >= stamina.maxStamina)
            {
                currentStamina.Value = stamina.maxStamina;
                break;
            }

            currentStamina.Value = newValue;

            if (isExhausted.Value && currentStamina.Value >= stamina.maxStamina / 2)
            {
                isExhausted.Value = false;
            }
            yield return null;
        }

        currentStamina.Value = stamina.maxStamina;
        staminaRegenCoroutine = null;
    }
    private IEnumerator AttackCooldownCoroutine(float duration)
    {
        isAttackOnCooldown.Value = true;
        yield return new WaitForSeconds(duration);
        isAttackOnCooldown.Value = false;
    }
    private IEnumerator DamageInvincibilityCoroutine()
    {
        isInvincible.Value = true;
        isDamagedState.Value = true;
        yield return new WaitForSeconds(combat.invincibilityDurationAfterDamage);
        if (activePowerUpCoroutine == null)
        {
            isInvincible.Value = false;
        }
        isDamagedState.Value = false;
        damageInvincibilityCoroutine = null;
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

        if (powerUp.grantsInvincibility) isInvincible.Value = false;
        activeSpeedMultiplier.Value = 1f;
        activePowerUpCoroutine = null;
    }
    private IEnumerator CountScoreCoroutine(int targetScore)
    {
        float startScore = displayedScore;
        float elapsedTime = 0f;
        while (elapsedTime < uiSettings.scoreCountingDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / uiSettings.scoreCountingDuration;
            displayedScore = Mathf.Lerp(startScore, targetScore, progress);
            UpdateScoreText(Mathf.RoundToInt(displayedScore));
            yield return null;
        }
        displayedScore = targetScore;
        UpdateScoreText(targetScore);
        scoreCountingCoroutine = null;
    }
    private IEnumerator JumpRequestCooldown()
    {
        canRequestContinuousJump = false;
        yield return new WaitForSeconds(0.1f);
        canRequestContinuousJump = true;
    }
    #endregion

    #region Network Variable Változás Kezelők
    private void OnHealthChanged(float oldHealth, float newHealth) { }
    private void OnManaChanged(float oldMana, float newMana) { }
    private void OnStaminaChanged(float oldStamina, float newStamina) { }
    private void OnSpeedChanged(float oldSpeed, float newSpeed) => components.animator.SetFloat("Speed", newSpeed);
    private void OnIsGroundedChanged(bool oldIsGrounded, bool newIsGrounded)
    {
        components.animator.SetBool("IsGrounded", newIsGrounded);
        if (!oldIsGrounded && newIsGrounded && components.soundController != null)
        {
            components.soundController.PlayLandSoundClientRpc();
        }
    }
    private void OnIsJumpingChanged(bool oldIsJumping, bool newIsJumping) => components.animator.SetBool("IsJumping", newIsJumping);
    private void OnIsFallingChanged(bool oldIsFalling, bool newIsFalling) => components.animator.SetBool("IsFalling", newIsFalling);
    private void OnIsGlidingChanged(bool oldVal, bool newVal) => components.animator.SetBool("IsGliding", newVal);
    private void OnAttackTriggered(bool oldAttackTrigger, bool newAttackTrigger)
    {
        if (newAttackTrigger) components.animator.SetTrigger("Attack");
    }
    private void OnExhaustedChanged(bool oldVal, bool newVal)
    {
        if (components.animator != null) components.animator.SetBool("IsExhausted", newVal);
    }
    private void OnIsDamagedStateChanged(bool previousValue, bool newValue) => components.animator.SetBool("IsDamaged", newValue);
    private void OnScoreChanged(int oldScore, int newScore)
    {
        if (IsOwner)
        {
            if (scoreCountingCoroutine != null) StopCoroutine(scoreCountingCoroutine);
            scoreCountingCoroutine = StartCoroutine(CountScoreCoroutine(newScore));
        }
    }
    private void OnIsDeadChanged(bool oldIsDead, bool newIsDead)
    {
        if (newIsDead && !oldIsDead) components.animator.SetTrigger("Die");
    }
    #endregion

    #region Animáció és Vizuális Elemek
    private void UpdateNetworkAnimatorParametersServer()
    {
        float currentHorizontalSpeed = new Vector3(components.rb.linearVelocity.x, 0, components.rb.linearVelocity.z).magnitude;
        networkSpeed.Value = currentHorizontalSpeed / (movement.moveSpeed * movement.runSpeedMultiplier);

        bool isIdleAndTired = isExhausted.Value && currentHorizontalSpeed < 0.1f && networkIsGrounded.Value;
        components.animator.SetBool("IsExhausted", isIdleAndTired);

        if (!networkIsGrounded.Value)
        {
            if (!networkIsGliding.Value)
            {
                networkIsJumping.Value = components.rb.linearVelocity.y > 0.1f;
                networkIsFalling.Value = components.rb.linearVelocity.y < -0.1f;
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
    private void UpdateLocalAnimatorParameters()
    {
        float currentHorizontalSpeed = new Vector3(components.rb.linearVelocity.x, 0, components.rb.linearVelocity.z).magnitude;
        float animationSpeed = currentHorizontalSpeed / (movement.moveSpeed * movement.runSpeedMultiplier);
        components.animator.SetFloat("Speed", animationSpeed);
        bool currentLocalIsGrounded = Physics.CheckSphere(components.groundCheck.position, movement.groundCheckRadius, movement.groundLayer);
        components.animator.SetBool("IsGrounded", currentLocalIsGrounded);

        bool isIdleAndTired = isExhausted.Value && currentHorizontalSpeed < 0.1f && currentLocalIsGrounded;
        components.animator.SetBool("IsExhausted", isIdleAndTired);
        components.animator.SetBool("IsGliding", networkIsGliding.Value);

        if (!currentLocalIsGrounded)
        {
            if (!networkIsGliding.Value)
            {
                components.animator.SetBool("IsJumping", components.rb.linearVelocity.y > 0.1f);
                components.animator.SetBool("IsFalling", components.rb.linearVelocity.y < -0.1f);
            }
            else
            {
                components.animator.SetBool("IsJumping", false);
                components.animator.SetBool("IsFalling", false);
            }
        }
        else
        {
            components.animator.SetBool("IsJumping", false);
            components.animator.SetBool("IsFalling", false);
        }
    }
    [ClientRpc]
    private void PlayAttackSoundClientRpc(int itemID)
    {
        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemID);
        if (itemDef != null && itemDef.useSound != null && components.soundController != null)
        {
            components.soundController.audioSource.PlayOneShot(itemDef.useSound);
        }
    }
    public void AnimEvent_PlayFootstepSound()
    {
        components.soundController?.AnimEvent_PlayFootstepSound();
    }
    #endregion

    #region UI és Kamera
    private void HandleStatBarsSmoothly()
    {
        float smoothSpeed = uiSettings.barSmoothSpeed * 10; // A sebességet megszorozzuk, hogy a MoveTowards hasonlóan reszponzív legyen, mint a Lerp volt

        if (ui.healthBarSlider != null)
        {
            ui.healthBarSlider.value = Mathf.MoveTowards(ui.healthBarSlider.value, currentHealth.Value, Time.deltaTime * smoothSpeed);
        }
        if (ui.manaBarSlider != null)
        {
            ui.manaBarSlider.value = Mathf.MoveTowards(ui.manaBarSlider.value, currentMana.Value, Time.deltaTime * smoothSpeed);
        }
        if (ui.staminaBarSlider != null)
        {
            ui.staminaBarSlider.value = Mathf.MoveTowards(ui.staminaBarSlider.value, currentStamina.Value, Time.deltaTime * smoothSpeed);
        }
    }
    private void UpdateCameraRootPosition()
    {
        if (components.cameraRoot == null) return;
        float targetZ = (networkMovementInput.Value.x > 0.1f || networkMovementInput.Value.x < -0.1f) ? cameraRootZPosition : 0f;
        Vector3 currentLocalPos = components.cameraRoot.transform.localPosition;
        currentLocalPos.z = Mathf.Lerp(currentLocalPos.z, targetZ, Time.deltaTime * cameraRootSmoothSpeed);
        components.cameraRoot.transform.localPosition = currentLocalPos;
    }
    private void HandleInventoryUI()
    {
        if (!isPlayerSpawned || ui.inventoryUI == null) return;
        if (isInventoryInputPressedLocal)
        {
            ui.inventoryUI.SetActive(!ui.inventoryUI.activeSelf);
            isInventoryInputPressedLocal = false;
        }
    }
    private void ToggleInGameMenu()
    {
        if (IsOwner && ui.inGameMenuManager != null)
        {
            ui.inGameMenuManager.ToggleMenu();
        }
    }
    private void UpdateScoreText(int scoreValue)
    {
        if (ui.scoreText == null) return;
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
        ui.scoreText.text = builder.ToString();
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
        var virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.Follow = components.cameraRoot != null ? components.cameraRoot.transform : transform;
            virtualCamera.LookAt = components.cameraRoot != null ? components.cameraRoot.transform : transform;
        }
    }
    #endregion

    #region Mentés és Betöltés
    public void SaveData(ref GameData data)
    {
        if (data.playersData.ContainsKey(OwnerClientId.ToString()))
        {
            PlayerData playerData = data.playersData[OwnerClientId.ToString()];
            playerData.position = transform.position;
        }
        else
        {
            data.playersData.Add(OwnerClientId.ToString(), new PlayerData
            {
                position = transform.position
            });
        }
    }
    public void LoadData(GameData data)
    {
        if (IsServer)
        {
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            this.currentHealth.Value = data.currentHealth;
            this.currentMana.Value = data.currentMana;
            this.currentStamina.Value = data.currentStamina;
            this.score.Value = data.score;
            if (components.slots != null)
            {
                components.slots.LoadInventoryData(data.inventoryItems);
            }
        }
    }
    #endregion

    #region Segédfüggvények és Gizmos
    [ClientRpc]
    public void TeleportPlayerClientRpc(Vector3 position, ClientRpcParams clientRpcParams = default)
    {
        transform.position = position;
        if (IsOwner && _vcam != null)
        {
            _vcam.PreviousStateIsValid = false;
        }
    }
    private void StopStaminaRegen()
    {
        if (staminaRegenCoroutine != null)
        {
            StopCoroutine(staminaRegenCoroutine);
            staminaRegenCoroutine = null;
        }
    }
    void OnDrawGizmos()
    {
        if (components.groundCheck != null)
        {
            Gizmos.color = networkIsGrounded.Value ? Color.green : Color.red;
            Gizmos.DrawWireSphere(components.groundCheck.position, movement.groundCheckRadius);
        }
    }
    #endregion
}