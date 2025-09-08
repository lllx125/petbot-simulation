using UnityEngine;

public class KeyboardController : MonoBehaviour
{
    [Header("Motor References")]
    public Motors motors;
    public float speed1 = 10f;
    public float speed2 = 100f;

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

        // WASD key controls
        if (Input.GetKey(KeyCode.W))
        {
            motor1Speed = speed1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            motor1Speed = -speed1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            motor2Speed = -speed2;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            motor2Speed = speed2;
        }

        // Apply speeds to motors
        if (motors != null)
        {
            motors.SetMotor1Speed(motor1Speed);
            motors.SetMotor2Speed(motor2Speed);
        }
    }
}