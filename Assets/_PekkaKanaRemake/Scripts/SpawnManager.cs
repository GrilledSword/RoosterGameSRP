using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    private List<Transform> _spawnPoints = new List<Transform>();
    private int _nextSpawnIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSpawnPoints();
    }
    private void RefreshSpawnPoints()
    {
        _spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None)
            .Select(sp => sp.transform)
            .ToList();

        _nextSpawnIndex = 0;
        if (_spawnPoints.Count == 0 && SceneManager.GetActiveScene().name != "MainMenuScene")
        {
            Debug.LogWarning($"No spawn points found in scene '{SceneManager.GetActiveScene().name}'. Players will spawn at this manager's position (0,0,0).");
        }
    }
    public Transform GetNextSpawnPoint()
    {
        if (_spawnPoints == null || _spawnPoints.Count == 0)
        {
            return transform;
        }

        Transform spawnPoint = _spawnPoints[_nextSpawnIndex];
        _nextSpawnIndex = (_nextSpawnIndex + 1) % _spawnPoints.Count;

        return spawnPoint;
    }
}

