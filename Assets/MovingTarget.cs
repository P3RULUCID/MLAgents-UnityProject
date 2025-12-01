using UnityEngine;

public class MovingTarget : MonoBehaviour
{
    [Header("Movement Settings")]
    public float minSpeed = 1f;
    public float maxSpeed = 5f;
    public float moveRange = 8f;
    public bool moveInX = true;
    
    [Header("Speed Variation")]
    public float speedChangeInterval = 3f;
    
    private Vector3 startPosition;
    private float direction = 1f;
    private float currentDistance = 0f;
    private float currentSpeed;
    private float speedTimer = 0f;

    void Start()
    {
        startPosition = transform.position;
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }

    void Update()
    {
        UpdateSpeed();
        MoveTarget();
    }

    void UpdateSpeed()
    {
        speedTimer += Time.deltaTime;
        
        if (speedTimer >= speedChangeInterval)
        {
            currentSpeed = Random.Range(minSpeed, maxSpeed);
            speedTimer = 0f;
            
            Debug.Log($"Target speed changed to: {currentSpeed:F2}");
        }
    }

    void MoveTarget()
    {
        Vector3 moveVector = moveInX ? Vector3.right : Vector3.forward;
        transform.position += moveVector * direction * currentSpeed * Time.deltaTime;
        
        currentDistance = moveInX ? 
            Mathf.Abs(transform.position.x - startPosition.x) : 
            Mathf.Abs(transform.position.z - startPosition.z);
        
        if (currentDistance >= moveRange)
        {
            direction *= -1f;
        }
    }

    public void ResetTarget()
    {
        transform.position = startPosition;
        direction = Random.Range(0, 2) == 0 ? 1f : -1f;
        currentDistance = 0f;
        currentSpeed = Random.Range(minSpeed, maxSpeed);
        speedTimer = 0f;
    }
}

