using UnityEngine;
using System.Collections.Generic;

public class ObstacleManager : MonoBehaviour
{
    [Header("Obstacle Prefab")]
    public GameObject obstaclePrefab;
    
    [Header("Spawn Settings")]
    public float spawnAreaSize = 8f;
    
    [Header("Curriculum Learning")]
    public bool useCurriculum = true;
    public int startingObstacles = 3;
    public int maxObstacles = 7;
    public float startMinSpeed = 1f;
    public float startMaxSpeed = 2f;
    public float endMinSpeed = 2f;
    public float endMaxSpeed = 4f;
    
    [Header("Randomization")]
    public int randomizeEveryNEpisodes = 100;
    public bool randomizeAxis = true;
    
    [Header("Debug Info")]
    public int currentEpisodeCount = 0;
    public int currentObstacleCount = 3;
    public float currentMinSpeed = 1f;
    public float currentMaxSpeed = 2f;
    
    private List<GameObject> activeObstacles = new List<GameObject>();
    private int episodesSinceLastRandomization = 0;

    void Start()
    {
        // Initial spawn
        currentObstacleCount = startingObstacles;
        currentMinSpeed = startMinSpeed;
        currentMaxSpeed = startMaxSpeed;
        SpawnObstacles();
        
        Debug.Log($"<color=cyan>ObstacleManager: Started with {currentObstacleCount} obstacles (Speed: {currentMinSpeed}-{currentMaxSpeed})</color>");
    }

    // Call this from AgentController when episode begins
    public void OnAgentEpisodeBegin()
    {
        currentEpisodeCount++;
        episodesSinceLastRandomization++;
        
        // Check if it's time to randomize
        if (episodesSinceLastRandomization >= randomizeEveryNEpisodes)
        {
            RandomizeEnvironment();
            episodesSinceLastRandomization = 0;
        }
    }

    void RandomizeEnvironment()
    {
        // Update curriculum difficulty based on total episodes
        if (useCurriculum)
        {
            UpdateCurriculumDifficulty();
        }
        
        // Respawn obstacles with new configuration
        ClearObstacles();
        SpawnObstacles();
        
        Debug.Log($"<color=yellow>🔄 ENVIRONMENT RANDOMIZED (Episode {currentEpisodeCount})</color>");
        Debug.Log($"<color=yellow>   Obstacles: {currentObstacleCount} | Speed: {currentMinSpeed:F1}-{currentMaxSpeed:F1}</color>");
    }

    void UpdateCurriculumDifficulty()
    {

        if (currentEpisodeCount < 10)
        {
            // Phase 1: Easy
            currentObstacleCount = Random.Range(startingObstacles, startingObstacles + 1); // 3
            currentMinSpeed = startMinSpeed; // 1
            currentMaxSpeed = startMaxSpeed; // 2
        }
        else if (currentEpisodeCount < 20)
        {
            // Phase 2: Medium
            int midObstacles = (startingObstacles + maxObstacles) / 2;
            currentObstacleCount = Random.Range(midObstacles - 1, midObstacles + 1); // 4-5
            currentMinSpeed = (startMinSpeed + endMinSpeed) / 2; // 1.5
            currentMaxSpeed = (startMaxSpeed + endMaxSpeed) / 2; // 3
        }
        else
        {
            // Phase 3: Hard
            currentObstacleCount = Random.Range(maxObstacles - 1, maxObstacles + 1); // 6-7
            currentMinSpeed = endMinSpeed; // 2
            currentMaxSpeed = endMaxSpeed; // 4
        }
        
        // Clamp values
        currentObstacleCount = Mathf.Clamp(currentObstacleCount, startingObstacles, maxObstacles);
    }

    void SpawnObstacles()
    {
        for (int i = 0; i < currentObstacleCount; i++)
        {
            SpawnSingleObstacle();
        }
        
        Debug.Log($"ObstacleManager: Spawned {activeObstacles.Count} obstacles");
    }

    void SpawnSingleObstacle()
    {
        // Calculate safe spawn area
        float safeSpawnArea = Mathf.Min(spawnAreaSize, 8f);
        
        Vector3 spawnPos;
        int attempts = 0;
        bool validPosition = false;
        
        do
        {
            spawnPos = new Vector3(
                Random.Range(-safeSpawnArea, safeSpawnArea),
                0.5f,
                Random.Range(-safeSpawnArea, safeSpawnArea)
            );
            attempts++;
            
            // Check distance from center (agent spawn)
            if (Vector3.Distance(spawnPos, Vector3.zero) < 3f)
                continue;
            
            // Check distance from other obstacles
            validPosition = true;
            foreach (GameObject existingObstacle in activeObstacles)
            {
                if (existingObstacle != null && Vector3.Distance(spawnPos, existingObstacle.transform.position) < 2f)
                {
                    validPosition = false;
                    break;
                }
            }
            
        } while (!validPosition && attempts < 20);
        
        GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
        obstacle.transform.parent = transform;
        
        MovingObstacle obstacleScript = obstacle.GetComponent<MovingObstacle>();
        if (obstacleScript != null)
        {
            // Randomize speed within current curriculum range
            float speed = Random.Range(currentMinSpeed, currentMaxSpeed);
            
            // Randomize movement axis
            bool moveInX = randomizeAxis ? (Random.Range(0, 2) == 0) : true;
            
            obstacleScript.moveSpeed = speed;
            obstacleScript.moveInX = moveInX;
            obstacleScript.arenaBoundary = 9f;
        }
        
        activeObstacles.Add(obstacle);
    }

    void ClearObstacles()
    {
        foreach (GameObject obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }
        
        activeObstacles.Clear();
    }

    void OnDestroy()
    {
        ClearObstacles();
    }

    public void ManualRandomize()
    {
        RandomizeEnvironment();
    }
}

