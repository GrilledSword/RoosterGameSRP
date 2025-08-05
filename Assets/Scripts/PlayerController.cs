using System.Collections;
using TMPro;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PekkaPlayerController : NetworkBehaviour, IDamageable
{
    [Header("Mozgás Beállítások")]
    [Tooltip("Pekka mozgás sebessége")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Pekka futási sebessége (gyorsabb mint a normál mozgása).")]
    [SerializeField] private float runSpeedMultiplier = 1.5f;
    [Tooltip("Pekka ugrási ereje")]
    [SerializeField] private float jumpForce = 10f;
    [Tooltip("Föld ellenörző gömb mérete")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    [Tooltip("A föld réteg (LayerMask) amivel Pekka kapcsolatba lép")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("A dupla ugrások száma amit Pekka végre tud hajtani.")]
    [SerializeField] private int maxDoubleJumps = 1;

    [Header("Komponens Referenciák")]
    [Tooltip("A Pekka Rigidbody komponense, ami a fizikai mozgást kezeli.")]
    [SerializeField] private Rigidbody rb;
    [Tooltip("A Pekka Animator komponense, ami az animációkat kezeli.")]
    [SerializeField] private Animator animator;
    [Tooltip("A Pekka groundCheckje, ami az földérzékelés állapotgépét kezeli.")]
    [SerializeField] private Transform groundCheck;

    [Header("Kamera Beállítások")]
    [Tooltip("A játékos Cinemachine kamerája prefab, amit a játékoshoz rendeljünk.")]
    [SerializeField] private GameObject playerCinemachineCameraPrefab;
    private GameObject instantiatedCinemachineCamera;
    [Tooltip("A kamera gyökér GameObject, ami a játékoshoz van rendelve. Ez mozog a játékos mozgásával.")]
    [SerializeField] private GameObject cameraRoot;
    [Tooltip("A kamera gyökér GameObject mozgási sebessége.")]
    [SerializeField] private float cameraRootSmoothSpeed = 5f;
    [Tooltip("A kamera gyökér GameObject Z pozíciója, ami a játékos mozgásával változik.")]
    [SerializeField] private float cameraRootZPosition;

    [Header("Hang Beállítások")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip[] footsteps;
    private float lastFootstepTime;
    [SerializeField] private float footstepInterval = 0.3f;

    [Header("Karakter Adatok")]
    [Tooltip("A játékos maximális életereje.")]
    [SerializeField] private float maximumHealth = 100;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> activeSpeedMultiplier = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
     private Coroutine activePowerUpCoroutine;
    private NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Sebzés Kezelés")]
    [Tooltip("Mennyi ideig sebezhetetlen a játékos, miután sebzést kapott (másodpercben).")]
    [SerializeField] private float invincibilityDurationAfterDamage = 1.5f;
    private Coroutine damageInvincibilityCoroutine;
    private NetworkVariable<bool> isDamagedState = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Frakció Beállítás")]
    [Tooltip("A játékos frakciója.")]
    private Faction faction = Faction.Player;

    public Faction Faction => faction;

    [Header("Karakter UI")]
    [Tooltip("A játékos élet csíkja.")]
    [SerializeField] private Slider healthBarSlider;
    [Tooltip("A játékos pontszám szövege.")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [Tooltip("Mennyi idő alatt számoljon fel a pontszám a cél értékre (másodpercben).")]
    [SerializeField] private float scoreCountingDuration = 0.5f;
    private float displayedScore = 0f;
    private Coroutine scoreCountingCoroutine;

    [Header("Karakter Inventory")]
    [Tooltip("A játékos inventory UI-ja.")]
    [SerializeField] private GameObject inventoryUI;
    [Tooltip("A játékos inventory slotjai.")]
    [SerializeField] private Slots slots;
    [SerializeField] private bool isPlayerSpawned = false;

    private NetworkVariable<Vector2> networkMovementInput = new NetworkVariable<Vector2>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkJumpInput = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkAttackTrigger = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkRunInput = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsGrounded = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsJumping = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkIsFalling = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool isJumpingInputPressedLocal = false;
    private bool isRunningInputPressedLocal = false;
    private int currentDoubleJumps;
    private bool isInventoryInputPressedLocal = false;
    private PlayerControls playerInputActions;

    void Awake()
    {
        playerInputActions = new PlayerControls();
        playerInputActions.Player.Move.performed += ctx => networkMovementInput.Value = ctx.ReadValue<Vector2>();
        playerInputActions.Player.Move.canceled += ctx => networkMovementInput.Value = Vector2.zero;
        playerInputActions.Player.Jump.performed += ctx => isJumpingInputPressedLocal = true;
        playerInputActions.Player.Jump.canceled += ctx => isJumpingInputPressedLocal = false;
        playerInputActions.Player.Sprint.performed += ctx => isRunningInputPressedLocal = true;
        playerInputActions.Player.Sprint.canceled += ctx => isRunningInputPressedLocal = false;
        playerInputActions.Player.Inventory.performed += ctx => isInventoryInputPressedLocal = true;
        playerInputActions.Player.Inventory.canceled += ctx => isInventoryInputPressedLocal = false;
        playerInputActions.Player.Slot1.performed += ctx => AttemptAttackServerRpc(0);
        playerInputActions.Player.Slot2.performed += ctx => AttemptAttackServerRpc(1);
        playerInputActions.Player.Slot3.performed += ctx => slots.UseItemServerRpc(2);
        playerInputActions.Player.Slot4.performed += ctx => slots.UseItemServerRpc(3);
        playerInputActions.Player.Slot5.performed += ctx => slots.UseItemServerRpc(4);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
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
            if (playerCinemachineCameraPrefab != null)
            {
                instantiatedCinemachineCamera = Instantiate(playerCinemachineCameraPrefab);
                CinemachineCamera virtualCamera = instantiatedCinemachineCamera.GetComponent<CinemachineCamera>();
                if (virtualCamera != null)
                {
                    virtualCamera.Follow = cameraRoot != null ? cameraRoot.transform : transform;
                    virtualCamera.LookAt = cameraRoot != null ? cameraRoot.transform : transform;
                }
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
        if (IsOwner && instantiatedCinemachineCamera != null)
        {
            Destroy(instantiatedCinemachineCamera);
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

    void FixedUpdate()
    {
        if (IsServer)
        {
            CheckGroundStatus();
            HandleMovementServer();
            HandleJumpServer();
            ResetAttackTriggerServer();
            UpdateNetworkAnimatorParametersServer();
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            SendInputToServer();
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

        // A sprintelés csak akkor lehetséges, ha a játékos NINCS sebzett állapotban.
        if (networkRunInput.Value && !isDamagedState.Value)
        {
            targetSpeed *= runSpeedMultiplier;
        }

        rb.linearVelocity = new Vector3(moveDirection.x * targetSpeed, rb.linearVelocity.y, 0f);
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0, 0));
        }
        float currentHorizontalSpeed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
        if (networkIsGrounded.Value && currentHorizontalSpeed > 0.1f && Time.time > lastFootstepTime + footstepInterval)
        {
            PlayFootstepSoundClientRpc();
            lastFootstepTime = Time.time;
        }
    }

    private void HandleJumpServer()
    {
        if (isDead.Value) return;
        bool jumpInputReceived = networkJumpInput.Value;
        networkJumpInput.Value = false;
        if (jumpInputReceived)
        {
            if (networkIsGrounded.Value)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                PlayJumpSoundClientRpc();
            }
            // A dupla ugrás csak akkor lehetséges, ha a játékos NINCS sebzett állapotban.
            else if (currentDoubleJumps > 0 && !isDamagedState.Value)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                currentDoubleJumps--;
                PlayJumpSoundClientRpc();
            }
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

    private void SendInputToServer()
    {
        if (isJumpingInputPressedLocal)
        {
            UpdateJumpInputServerRpc(true);
            isJumpingInputPressedLocal = false;
        }
        UpdateRunInputServerRpc(isRunningInputPressedLocal);
    }

    [ServerRpc] private void UpdateJumpInputServerRpc(bool jumpInput) => networkJumpInput.Value = jumpInput;
    [ServerRpc]
    private void AttemptAttackServerRpc(int slotIndex)
    {
        if (slots != null)
        {
            slots.TriggerAttackFromSlot(slotIndex);
        }
    }
    [ServerRpc] private void UpdateRunInputServerRpc(bool runInput) => networkRunInput.Value = runInput;

    public void TakeDamage(float damage, Faction sourceFaction)
    {
        if (isDead.Value || isInvincible.Value) return;

        currentHealth.Value -= damage;
        PlayDamageSoundClientRpc();

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
        isDamagedState.Value = true; // Animáció indítása
        yield return new WaitForSeconds(invincibilityDurationAfterDamage);

        if (activePowerUpCoroutine == null)
        {
            isInvincible.Value = false;
        }
        isDamagedState.Value = false; // Animáció leállítása
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
                isDamagedState.Value = false; // A sebzés animációt is leállítjuk
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
        cameraRootSmoothSpeed = 0f;
        GetComponent<Collider>().enabled = false;
        PlayDeathSoundClientRpc();
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
        if (!oldIsGrounded && newIsGrounded) PlayLandSoundClientRpc();
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

    [ClientRpc] private void PlayJumpSoundClientRpc() => audioSource.PlayOneShot(jumpSound);
    [ClientRpc] private void PlayLandSoundClientRpc() => audioSource.PlayOneShot(landSound);
    [ClientRpc] private void PlayDamageSoundClientRpc() => audioSource.PlayOneShot(damageSound);
    [ClientRpc] private void PlayDeathSoundClientRpc() => audioSource.PlayOneShot(deathSound);
    [ClientRpc]
    public void PlayPickupSoundClientRpc(int itemID)
    {
        if (audioSource == null) return;
        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemID);
        if (itemDef != null && itemDef.pickupSound != null)
        {
            audioSource.PlayOneShot(itemDef.pickupSound);
        }
    }
    [ClientRpc]
    public void PlayUseSoundClientRpc(int itemID)
    {
        if (audioSource == null) return;
        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemID);
        if (itemDef != null && itemDef.useSound != null)
        {
            audioSource.PlayOneShot(itemDef.useSound);
        }
    }
    [ClientRpc]
    private void PlayFootstepSoundClientRpc()
    {
        if (audioSource != null && footsteps != null && footsteps.Length > 0)
        {
            audioSource.PlayOneShot(footsteps[Random.Range(0, footsteps.Length)]);
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
