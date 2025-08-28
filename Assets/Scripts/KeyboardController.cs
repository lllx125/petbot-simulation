using UnityEngine;

public class KeyboardController : MonoBehaviour
{
    [Header("Motor References")]
    public Motors motors;
    public float speed = 100f;

    void Start()
    {
        // Auto-find motors if not assigned
        if (motors == null)
        {
            motors = GameObject.FindFirstObjectByType<Motors>();
        }
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
        if (motors != null)
        {
            motors.SetMotor1Speed(motor1Speed);
            motors.SetMotor2Speed(motor2Speed);
        }
    }
}