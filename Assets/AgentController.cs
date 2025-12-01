using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class AgentController : Agent
{
    public Transform target;
    public Transform otherAgent;
    public float moveSpeed = 100f;
    
    [Header("Obstacle Detection")]
    public int numberOfRays = 8;
    public float rayDistance = 5f;
    public LayerMask obstacleLayer;
    
    private Rigidbody rb;
    private float platformSize = 10f;
    private int stepCount = 0;
    private const int MAX_STEPS = 1000;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    public override void OnEpisodeBegin()
    {
        stepCount = 0;
        
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // RANDOMIZE target position
        float randomX = Random.Range(-8f, 8f);
        float randomZ = Random.Range(-8f, 8f);
        target.localPosition = new Vector3(randomX, 0.5f, randomZ);
        
        ObstacleManager obstacleManager = FindObjectOfType<ObstacleManager>();
        if (obstacleManager != null)
        {
            obstacleManager.OnAgentEpisodeBegin();
        }
        
        Debug.Log($"=== EPISODE START === Agent: {transform.localPosition} | Target: {target.localPosition}");
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Direction to target - 3 values
        Vector3 directionToTarget = target.localPosition - transform.localPosition;
        sensor.AddObservation(directionToTarget.normalized);
        
        // 2. Distance to target - 1 value
        float distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        sensor.AddObservation(distanceToTarget / 20f);
        
        // 3. Agent velocity - 3 values
        sensor.AddObservation(rb.velocity / 10f);
        
        // 4. Distance to walls - 4 values
        float distanceToNorth = platformSize - transform.localPosition.z;
        float distanceToSouth = platformSize + transform.localPosition.z;
        float distanceToEast = platformSize - transform.localPosition.x;
        float distanceToWest = platformSize + transform.localPosition.x;
        
        sensor.AddObservation(distanceToNorth / platformSize);
        sensor.AddObservation(distanceToSouth / platformSize);
        sensor.AddObservation(distanceToEast / platformSize);
        sensor.AddObservation(distanceToWest / platformSize);
        
        // 5. Agent's normalized position - 2 values
        sensor.AddObservation(transform.localPosition.x / platformSize);
        sensor.AddObservation(transform.localPosition.z / platformSize);
        
        // 6. OTHER AGENT OBSERVATIONS
        if (otherAgent != null)
        {
            Vector3 directionToOther = otherAgent.localPosition - transform.localPosition;
            sensor.AddObservation(directionToOther.normalized);
            
            float distanceToOther = Vector3.Distance(transform.localPosition, otherAgent.localPosition);
            sensor.AddObservation(distanceToOther / 20f);
            
            Rigidbody otherRb = otherAgent.GetComponent<Rigidbody>();
            if (otherRb != null)
            {
                sensor.AddObservation(otherRb.velocity / 10f);
            }
            else
            {
                sensor.AddObservation(Vector3.zero);
            }
            
            float collisionRisk = CalculateCollisionRisk(otherAgent);
            sensor.AddObservation(collisionRisk);
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                sensor.AddObservation(0f);
            }
        }
        
        // 7. Raycast observations for obstacles - numberOfRays values
        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = (360f / numberOfRays) * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            RaycastHit hit;
            float rayValue = 0f;
            
            if (Physics.Raycast(transform.position, direction, out hit, rayDistance, obstacleLayer))
            {
                // Normalize distance (closer = higher value)
                rayValue = 1f - (hit.distance / rayDistance);
                
                // Debug visualization
                Debug.DrawRay(transform.position, direction * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction * rayDistance, Color.green);
            }
            
            sensor.AddObservation(rayValue);
        }
        
        // 8. Nearest obstacle data - 4 values
        GameObject nearestObstacle = FindNearestObstacle();
        if (nearestObstacle != null)
        {
            Vector3 directionToObstacle = nearestObstacle.transform.position - transform.localPosition;
            sensor.AddObservation(directionToObstacle.normalized.x);
            sensor.AddObservation(directionToObstacle.normalized.z);
            
            float obstacleDistance = directionToObstacle.magnitude;
            sensor.AddObservation(obstacleDistance / 20f);
            
            // Obstacle velocity (if it's moving)
            MovingObstacle obstacleScript = nearestObstacle.GetComponent<MovingObstacle>();
            if (obstacleScript != null)
            {
                // Estimate velocity based on movement direction
                float velocityEstimate = obstacleScript.moveInX ? obstacleScript.moveSpeed : -obstacleScript.moveSpeed;
                sensor.AddObservation(velocityEstimate / 5f);
            }
            else
            {
                sensor.AddObservation(0f);
            }
        }
        else
        {
            // No obstacles nearby
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(1f);
            sensor.AddObservation(0f);
        }
    }

    private GameObject FindNearestObstacle()
    {
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (GameObject obstacle in obstacles)
        {
            float distance = Vector3.Distance(transform.position, obstacle.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = obstacle;
            }
        }
        
        return nearest;
    }


    private float CalculateCollisionRisk(Transform other)
    {
        Vector3 toOther = other.localPosition - transform.localPosition;
        float distance = toOther.magnitude;
        
        if (distance > 5f) return 0f;
        
        Vector3 relativeVelocity = rb.velocity - other.GetComponent<Rigidbody>().velocity;
        float dotProduct = Vector3.Dot(relativeVelocity.normalized, toOther.normalized);
        
        if (dotProduct < -0.5f && distance < 3f)
        {
            return 1f - (distance / 3f);
        }
        
        return 0f;
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        stepCount++;
        
        if (stepCount >= MAX_STEPS)
        {
            Debug.Log($"⏱️ [{gameObject.name}] TIMEOUT! Episode ended.");
            AddReward(-0.5f);
            EndEpisode();
            return;
        }
        
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        
        float previousDistance = Vector3.Distance(transform.localPosition, target.localPosition);
        
        Vector3 movement = new Vector3(moveX, 0, moveZ) * moveSpeed;
        rb.AddForce(movement, ForceMode.VelocityChange);
        
        if (rb.velocity.magnitude > 5f)
        {
            rb.velocity = rb.velocity.normalized * 5f;
        }
        
        float currentDistance = Vector3.Distance(transform.localPosition, target.localPosition);
        
        if (stepCount % 5 == 0)
        {
            float approachReward = (previousDistance - currentDistance) * 0.1f;
            AddReward(approachReward);
        }
        
        // Wall proximity penalty
        float distanceToNearestWall = Mathf.Min(
            platformSize - Mathf.Abs(transform.localPosition.x),
            platformSize - Mathf.Abs(transform.localPosition.z)
        );
        
        if (distanceToNearestWall < 1f)
        {
            AddReward(-0.02f);
        }
        
        // Other agent collision avoidance
        if (otherAgent != null)
        {
            float distanceToOther = Vector3.Distance(transform.localPosition, otherAgent.localPosition);
            
            if (distanceToOther < 1.5f)
            {
                AddReward(-0.02f);
            }
            else if (distanceToOther > 2f && distanceToOther < 4f)
            {
                AddReward(0.001f);
            }
            
            float collisionRisk = CalculateCollisionRisk(otherAgent);
            if (collisionRisk > 0.5f)
            {
                AddReward(-0.1f * collisionRisk);
            }
        }
        
        // === NEW: OBSTACLE AVOIDANCE REWARDS ===
        GameObject nearestObstacle = FindNearestObstacle();
        if (nearestObstacle != null)
        {
            float obstacleDistance = Vector3.Distance(transform.position, nearestObstacle.transform.position);
            
            // Danger zone - very close to obstacle
            if (obstacleDistance < 1.5f)
            {
                AddReward(-0.05f); // Penalty for being too close
            }
            // Safe navigation zone
            else if (obstacleDistance > 2f && obstacleDistance < 4f)
            {
                // Small reward for maintaining safe distance while moving toward target
                if (currentDistance < previousDistance)
                {
                    AddReward(0.004f);
                }
            }
        }
        
        // Smooth movement reward
        float velocityChange = (rb.velocity - movement).magnitude;
        if (velocityChange > 2f)
        {
            AddReward(-0.002f);
        }
        
        AddReward(-0.0002f);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            Debug.Log($"✅ [{gameObject.name}] REACHED TARGET! +3.0");
            AddReward(3.0f);
            EndEpisode();
        }
        else if (other.CompareTag("Wall"))
        {
            Debug.Log($"❌ [{gameObject.name}] HIT WALL! -0.5");
            AddReward(-0.5f);
            EndEpisode();
        }
        else if (other.CompareTag("Agent"))
        {
            Debug.Log($"💥 [{gameObject.name}] COLLIDED WITH OTHER AGENT! -0.25");
            AddReward(-0.25f);
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log($"🚧 [{gameObject.name}] HIT OBSTACLE! -0.25");
            AddReward(-0.25f);
            EndEpisode();
        }
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
    }
}