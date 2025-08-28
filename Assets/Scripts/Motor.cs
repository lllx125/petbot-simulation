using UnityEngine;

public class Motors : MonoBehaviour
{

    float motor1Speed = 0f;
    float motor2Speed = 0f;
    public float radius = 0.15f;
    Rigidbody rb;

    void Start()
    {
        motor1Speed = 0f;
        motor2Speed = 0f;
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        rb.angularVelocity = rb.transform.right * motor1Speed + rb.transform.forward * motor2Speed;
    }

    public void SetMotor1Speed(float newSpeed)
    {
        motor1Speed = newSpeed;
    }

    public void SetMotor2Speed(float newSpeed)
    {
        motor2Speed = newSpeed;
    }
}
