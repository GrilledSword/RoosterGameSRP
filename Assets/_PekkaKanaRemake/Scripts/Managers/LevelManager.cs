using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the state and rules for a specific level, including the timer and the level completion sequence.
/// It's initialized by the GameFlowManager after the level scene is loaded.
/// </summary>
public class LevelManager : NetworkBehaviour
{
    [Header("Level Configuration")]
    private LevelNodeDefinition currentLevel;

    [Header("UI References")]
    [SerializeField] private GameObject goalTextObject;
    private const string goalTextObjectName = "InGoalContainer";
    private TextMeshProUGUI timerText;
    private const string TimerObjectName = "InGameClock";
    private LevelSummaryUI levelSummaryUI;

    [Header("Settings")]
    [SerializeField] private float goalTextDisplayTime = 2.0f; // Mennyi ideig látszódjon a "CÉL!" felirat

    private float _remainingTime;
    private bool _isTimerRunning = false;
    private bool _levelCompleted = false;

    public void InitializeLevel(LevelNodeDefinition levelDef)
    {
        currentLevel = levelDef;
        _levelCompleted = false;

        if (currentLevel == null)
        {
            this.enabled = false;
            return;
        }

        this.enabled = true;

        FindMissingTexts();
        levelSummaryUI = FindFirstObjectByType<LevelSummaryUI>();
        if (goalTextObject != null) goalTextObject.SetActive(false); // Kezdetben a CÉL szöveg rejtve van

        if (currentLevel.hasTimeLimit)
        {
            _remainingTime = currentLevel.timeLimitInSeconds;
            _isTimerRunning = true;
            if (timerText != null) timerText.gameObject.SetActive(true);
        }
        else
        {
            _isTimerRunning = false;
            if (timerText != null) timerText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!_isTimerRunning || _levelCompleted) return;

        if (_remainingTime > 0)
        {
            _remainingTime -= Time.deltaTime;
            UpdateTimerUI(_remainingTime);
        }
        else
        {
            _remainingTime = 0;
            _isTimerRunning = false;
            UpdateTimerUI(0);
            KillPlayer();
        }
    }

    /// <summary>
    /// This method starts the multi-stage level completion sequence.
    /// Called by the PlayerController on the server.
    /// </summary>
    public void StartLevelEndSequence()
    {
        if (_levelCompleted || !IsServer) return;
        _levelCompleted = true;
        _isTimerRunning = false;

        GameFlowManager.Instance.CompleteLevelServerRpc(currentLevel.levelId);

        // Elõször a "CÉL!" feliratot mutatjuk, majd egy kis késleltetés után az összesítõt.
        ShowGoalTextClientRpc();
        StartCoroutine(ShowSummaryAfterDelay());
    }

    [ClientRpc]
    private void ShowGoalTextClientRpc()
    {
        if (goalTextObject != null)
        {
            goalTextObject.SetActive(true);
        }

        // Letiltjuk a játékos irányítását, hogy ne mozoghasson a befejezés alatt
        PekkaPlayerController localPlayer = FindFirstObjectByType<PekkaPlayerController>();
        if (localPlayer != null && localPlayer.IsOwner)
        {
            localPlayer.SetPlayerControlActive(false);
        }
    }

    private IEnumerator ShowSummaryAfterDelay()
    {
        yield return new WaitForSeconds(goalTextDisplayTime);
        ShowSummaryClientRpc();
    }

    [ClientRpc]
    private void ShowSummaryClientRpc()
    {
        // Elrejtjük a "CÉL!" feliratot
        if (goalTextObject != null)
        {
            goalTextObject.SetActive(false);
        }

        // Megjelenítjük a pontszám összesítõt
        PekkaPlayerController localPlayer = FindFirstObjectByType<PekkaPlayerController>();
        if (localPlayer != null && levelSummaryUI != null)
        {
            int baseScore = localPlayer.score.Value;
            int health = (int)localPlayer.currentHealth.Value;
            levelSummaryUI.ShowSummary(baseScore, _remainingTime, health);
        }
        else
        {
            // Fallback, ha nincs UI
            if (GameFlowManager.Instance != null && NetworkManager.Singleton.IsServer)
            {
                GameFlowManager.Instance.ReturnToWorldMap();
            }
        }
    }

    private void FindMissingTexts()
    {
        if (timerText != null) return;

        GameObject clockObject = GameObject.Find(TimerObjectName);
        if (clockObject != null)
        {
            timerText = clockObject.GetComponent<TextMeshProUGUI>();
        }

        if(goalTextObject != null) return;
        GameObject goalObject = GameObject.Find(goalTextObjectName);
        if (goalObject != null)
        {
            goalTextObject = goalObject;
            goalTextObject.SetActive(false);
        }
    }

    void UpdateTimerUI(float timeToDisplay)
    {
        if (timerText == null) return;

        if (timeToDisplay < 0) timeToDisplay = 0;

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void KillPlayer()
    {
        if (!IsServer) return;

        PekkaPlayerController player = FindFirstObjectByType<PekkaPlayerController>();
        if (player != null)
        {
            player.TakeDamage(9999, Faction.Player);
        }
    }
}

