using UnityEngine;

public class KeyboardController : MonoBehaviour
{
    [Header("Motor References")]
    public Motor1 motor1;
    public Motor2 motor2;
    public float speed = 100f;

    void Start()
    {
        // Auto-find motors if not assigned
        if (motor1 == null)
            motor1 = GameObject.FindFirstObjectByType<Motor1>();
        
        if (motor2 == null)
            motor2 = GameObject.FindFirstObjectByType<Motor2>();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    void HandleInput()
    {
        float motor1Speed = 0f;
        float motor2Speed = 0f;
        
        // Arrow key controls
        if (Input.GetKey(KeyCode.UpArrow))
        {
            motor1Speed = speed;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            motor1Speed = -speed;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            motor2Speed = speed;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            motor2Speed = -speed;
        }
        
        // Apply speeds to motors
        if (motor1 != null)
            motor1.SetSpeed(motor1Speed);
        
        if (motor2 != null)
            motor2.SetSpeed(motor2Speed);
    }
}