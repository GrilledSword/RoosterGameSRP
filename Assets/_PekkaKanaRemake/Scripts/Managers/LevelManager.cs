using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LevelManager : NetworkBehaviour
{
    [Header("Level Configuration")]
    private LevelNodeDefinition currentLevel;

    [Header("UI References")]
    [SerializeField] private GameObject goalTextObject;
    private const string goalTextObjectName = "EndLevelUI";
    private TextMeshProUGUI timerText;
    private const string TimerObjectName = "InGameClock";
    private LevelSummaryUI levelSummaryUI;
    private const string levelSummaryUIName = "LevelSummaryUI";

    [Header("Settings")]
    [SerializeField] private float goalTextDisplayTime = 2.0f;

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
        if (goalTextObject != null) goalTextObject.SetActive(false);

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
    public void StartLevelEndSequence()
    {
        if (_levelCompleted || !IsServer) return;
        _levelCompleted = true;
        _isTimerRunning = false;

        GameFlowManager.Instance.CompleteLevelServerRpc(currentLevel.levelId);

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
        if (goalTextObject != null)
        {
            goalTextObject.SetActive(false);
        }
        PekkaPlayerController localPlayer = FindFirstObjectByType<PekkaPlayerController>();
        if (localPlayer != null && levelSummaryUI != null)
        {
            int baseScore = localPlayer.score.Value;
            int health = (int)localPlayer.currentHealth.Value;
            levelSummaryUI.ShowSummary(baseScore, _remainingTime, health);
        }
        else
        {
            if (GameFlowManager.Instance != null && NetworkManager.Singleton.IsServer)
            {
                GameFlowManager.Instance.ReturnToWorldMap();
            }
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
    private void FindMissingTexts()
    {
        GameObject clockObject = GameObject.Find(TimerObjectName);
        if (clockObject != null)
        {
            timerText = clockObject.GetComponent<TextMeshProUGUI>();
        }
        GameObject goalObject = GameObject.Find(goalTextObjectName);
        if (goalObject != null)
        {
            goalTextObject = goalObject;
            goalTextObject.SetActive(false);
        }
        GameObject summaryObject = GameObject.Find(levelSummaryUIName);
        if (summaryObject != null)
        {
            levelSummaryUI = summaryObject.GetComponent<LevelSummaryUI>();
        }
    }
}

