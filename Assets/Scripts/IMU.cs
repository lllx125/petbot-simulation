using UnityEngine;

[System.Serializable]
public struct IMUData
{
    public Vector3 acceleration;
    public Vector3 angularVelocity;
}

public class IMU : MonoBehaviour
{
    [Header("Error Simulation")]
    public float accelNoise = 0.1f;
    public float gyroNoise = 0.01f;
    public Vector3 accelBias = Vector3.zero;
    public Vector3 gyroBias = Vector3.zero;
    
    private Rigidbody rb;
    private System.Random noiseGenerator;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        noiseGenerator = new System.Random();
    }
    
    public IMUData GetIMUData()
    {
        Vector3 acceleration = Vector3.zero;
        Vector3 angularVelocity = Vector3.zero;
        
        if (rb != null)
        {
            acceleration = rb.velocity / Time.fixedDeltaTime + Physics.gravity;
            angularVelocity = rb.angularVelocity;
        }
        
        // Add simulated errors
        acceleration += accelBias + GetNoiseVector(accelNoise);
        angularVelocity += gyroBias + GetNoiseVector(gyroNoise);
        
        return new IMUData
        {
            acceleration = acceleration,
            angularVelocity = angularVelocity
        };
    }
    
    public Vector3 GetAcceleration()
    {
        return GetIMUData().acceleration;
    }
    
    public Vector3 GetAngularVelocity()
    {
        return GetIMUData().angularVelocity;
    }
    
    private Vector3 GetNoiseVector(float noise)
    {
        return new Vector3(
            GaussianNoise(0f, noise),
            GaussianNoise(0f, noise),
            GaussianNoise(0f, noise)
        );
    }
    
    private float GaussianNoise(float mean, float stdDev)
    {
        float u1 = 1f - (float)noiseGenerator.NextDouble();
        float u2 = 1f - (float)noiseGenerator.NextDouble();
        float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
        return mean + stdDev * randStdNormal;
    }
}