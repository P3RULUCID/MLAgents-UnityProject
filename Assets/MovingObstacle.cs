using UnityEngine;
using Unity.MLAgents;

public class MovingObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float moveRange = 6f;
    public bool moveInX = true;
    
    [Header("Collision Settings")]
    public float collisionPenalty = -0.5f;
    public bool avoidOtherObstacles = true;
    
    [Header("Boundary Settings")]
    public float arenaBoundary = 9f;
    public bool stayInBounds = true;
    
    private Vector3 startPosition;
    private float direction = 1f;
    private float currentDistance = 0f;
    private bool isReversing = false;

    void Start()
    {
        startPosition = transform.position;
        direction = Random.Range(0, 2) == 0 ? 1f : -1f;
        ClampToBounds();
    }

    void Update()
    {
        MoveObstacle();
        
        if (stayInBounds)
        {
            ClampToBounds();
        }
    }

    void MoveObstacle()
    {
        Vector3 moveVector = moveInX ? Vector3.right : Vector3.forward;
        Vector3 nextPosition = transform.position + moveVector * direction * moveSpeed * Time.deltaTime;
        
        // Check for obstacles ahead BEFORE moving (but NOT walls!)
        if (avoidOtherObstacles && CheckForObstacleAhead(moveVector * direction))
        {
            if (!isReversing)
            {
                direction *= -1f;
                isReversing = true;
                return;
            }
        }
        else
        {
            isReversing = false;
        }
        
        // Boundary checking
        if (stayInBounds)
        {
            if (moveInX)
            {
                if (Mathf.Abs(nextPosition.x) >= arenaBoundary)
                {
                    direction *= -1f;
                    return;
                }
            }
            else
            {
                if (Mathf.Abs(nextPosition.z) >= arenaBoundary)
                {
                    direction *= -1f;
                    return;
                }
            }
        }
        
        transform.position = nextPosition;
        
        currentDistance = moveInX ? 
            Mathf.Abs(transform.position.x - startPosition.x) : 
            Mathf.Abs(transform.position.z - startPosition.z);
        
        if (currentDistance >= moveRange)
        {
            direction *= -1f;
        }
    }

    bool CheckForObstacleAhead(Vector3 direction)
    {
        // Raycast to detect obstacles ahead
        RaycastHit hit;
        float checkDistance = 1.5f;
        
        // Cast ray from center of obstacle
        if (Physics.Raycast(transform.position, direction, out hit, checkDistance))
        {
            // ONLY reverse if we hit another OBSTACLE (not walls, not agents)
            if (hit.collider.gameObject.CompareTag("Obstacle") && 
                hit.collider.gameObject != gameObject) // Don't detect self!
            {
                return true;
            }
        }
        
        return false;
    }

    void ClampToBounds()
    {
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -arenaBoundary, arenaBoundary);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, -arenaBoundary, arenaBoundary);
        clampedPosition.y = 0.5f;
        transform.position = clampedPosition;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Agent"))
        {
            Agent agent = collision.gameObject.GetComponent<Agent>();
            if (agent != null)
            {
                agent.AddReward(collisionPenalty);
            }
        }
        
        // Reverse when hitting arena wall
        if (collision.gameObject.CompareTag("Wall"))
        {
            direction *= -1f;
        }
        
        // Reverse when hitting another obstacle
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            direction *= -1f;
        }
    }

    public void ResetObstacle()
    {
        transform.position = startPosition;
        direction = Random.Range(0, 2) == 0 ? 1f : -1f;
        currentDistance = 0f;
        isReversing = false;
        ClampToBounds();
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying && avoidOtherObstacles)
        {
            // Draw movement direction
            Gizmos.color = Color.yellow;
            Vector3 moveVector = moveInX ? Vector3.right : Vector3.forward;
            Gizmos.DrawRay(transform.position, moveVector * direction * 2f);
            
            // Draw detection ray (only shows when detecting obstacles)
            RaycastHit hit;
            if (Physics.Raycast(transform.position, moveVector * direction, out hit, 1.5f))
            {
                if (hit.collider.CompareTag("Obstacle"))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(transform.position, moveVector * direction * hit.distance);
                }
            }
        }
    }
}
