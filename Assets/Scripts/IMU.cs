using UnityEngine;
using System;

[System.Serializable]
public struct IMUData
{
    public Vector3 acceleration;      // Linear acceleration (m/s²)
    public Vector3 angularVelocity;   // Angular velocity (rad/s)
    public Vector3 gravity;           // Gravity vector (m/s²)
    public float temperature;         // Sensor temperature (°C)
    public double timestamp;          // Timestamp
}

public class IMU : MonoBehaviour
{
    [Header("IMU Configuration")]
    [Tooltip("Update rate in Hz (typical IMU rates: 100Hz, 200Hz, 1000Hz)")]
    [Range(10f, 1000f)]
    public float updateRate = 100f;
    
    [Tooltip("Coordinate system convention")]
    public CoordinateSystem coordinateSystem = CoordinateSystem.NED;
    
    [Header("Accelerometer Settings")]
    [Tooltip("Accelerometer range in g (±2g, ±4g, ±8g, ±16g)")]
    public float accelRange = 16f;
    
    [Tooltip("Accelerometer noise standard deviation (m/s²)")]
    public float accelNoise = 0.1f;
    
    [Tooltip("Accelerometer bias (m/s²)")]
    public Vector3 accelBias = Vector3.zero;
    
    [Header("Gyroscope Settings")]
    [Tooltip("Gyroscope range in degrees/second (±250, ±500, ±1000, ±2000)")]
    public float gyroRange = 2000f;
    
    [Tooltip("Gyroscope noise standard deviation (rad/s)")]
    public float gyroNoise = 0.01f;
    
    [Tooltip("Gyroscope bias (rad/s)")]
    public Vector3 gyroBias = Vector3.zero;
    
    [Header("Environmental")]
    [Tooltip("Ambient temperature (°C)")]
    public float temperature = 25f;
    
    [Tooltip("Temperature drift coefficient")]
    public float tempDriftCoeff = 0.001f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool logData = false;
    
    // Private variables
    private Vector3 lastVelocity;
    private Vector3 lastAngularVelocity;
    private Vector3 currentAcceleration;
    private Vector3 currentAngularVelocity;
    private float lastUpdateTime;
    private IMUData currentData;
    
    // Noise generators
    private System.Random noiseGenerator;
    
    public enum CoordinateSystem
    {
        NED,    // North-East-Down (common in aerospace)
        ENU,    // East-North-Up (common in robotics)
        Unity   // Unity's coordinate system (left-handed)
    }
    
    void Start()
    {
        noiseGenerator = new System.Random();
        lastUpdateTime = Time.time;
        lastVelocity = Vector3.zero;
        lastAngularVelocity = Vector3.zero;
        
        // Initialize IMU
        CalibrateIMU();
    }
    
    void Update()
    {
        // Update at specified rate
        if (Time.time - lastUpdateTime >= 1f / updateRate)
        {
            UpdateIMUReadings();
            lastUpdateTime = Time.time;
            
            if (logData)
            {
                LogIMUData();
            }
        }
    }
    
    void UpdateIMUReadings()
    {
        // Calculate time delta
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0) deltaTime = 1f / updateRate;
        
        // Get current transform state
        Vector3 currentPosition = transform.position;
        Vector3 currentVelocity = (currentPosition - transform.position) / deltaTime;
        Vector3 currentAngVel = GetAngularVelocity(deltaTime);
        
        // Calculate accelerations
        currentAcceleration = CalculateAcceleration(currentVelocity, deltaTime);
        currentAngularVelocity = currentAngVel;
        
        // Apply coordinate system transformation
        Vector3 transformedAccel = TransformToIMUCoordinates(currentAcceleration);
        Vector3 transformedAngVel = TransformToIMUCoordinates(currentAngularVelocity);
        
        // Add gravity to accelerometer reading (IMUs measure specific force, not acceleration)
        Vector3 gravityVector = GetGravityInIMUFrame();
        transformedAccel += gravityVector;
        
        // Apply sensor characteristics (noise, bias, saturation)
        transformedAccel = ApplyAccelerometerCharacteristics(transformedAccel);
        transformedAngVel = ApplyGyroscopeCharacteristics(transformedAngVel);
        
        // Update IMU data structure
        currentData = new IMUData
        {
            acceleration = transformedAccel,
            angularVelocity = transformedAngVel,
            gravity = gravityVector,
            temperature = temperature + GetTemperatureDrift(),
            timestamp = Time.timeAsDouble
        };
        
        // Update last values
        lastVelocity = currentVelocity;
        lastAngularVelocity = currentAngularVelocity;
    }
    
    Vector3 CalculateAcceleration(Vector3 currentVel, float deltaTime)
    {
        // Calculate linear acceleration from velocity change
        Vector3 accel = (currentVel - lastVelocity) / deltaTime;
        
        // For more realistic simulation, we can also use Rigidbody if available
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Use physics-based acceleration
            Vector3 totalForce = rb.linearVelocity - lastVelocity;
            accel = totalForce / (rb.mass * deltaTime);
        }
        
        return accel;
    }
    
    Vector3 GetAngularVelocity(float deltaTime)
    {
        // Calculate angular velocity from rotation change
        Quaternion currentRotation = transform.rotation;
        
        // Get angular velocity from Rigidbody if available (more accurate)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            return rb.angularVelocity;
        }
        
        // Fallback: calculate from rotation change
        // This is less accurate but works without Rigidbody
        Vector3 eulerDiff = (transform.eulerAngles - transform.eulerAngles) / deltaTime;
        return eulerDiff * Mathf.Deg2Rad;
    }
    
    Vector3 GetGravityInIMUFrame()
    {
        // World gravity vector (typically pointing down in Unity)
        Vector3 worldGravity = Physics.gravity;
        
        // Transform gravity to IMU's local coordinate frame
        Vector3 localGravity = transform.InverseTransformDirection(worldGravity);
        
        return localGravity;
    }
    
    Vector3 TransformToIMUCoordinates(Vector3 vector)
    {
        Vector3 result = vector;
        
        switch (coordinateSystem)
        {
            case CoordinateSystem.NED:
                // Convert Unity (left-handed) to NED (right-handed)
                // Unity: +X=right, +Y=up, +Z=forward
                // NED: +X=north, +Y=east, +Z=down
                result = new Vector3(vector.z, vector.x, -vector.y);
                break;
                
            case CoordinateSystem.ENU:
                // Convert Unity to ENU (right-handed)
                // ENU: +X=east, +Y=north, +Z=up
                result = new Vector3(vector.x, vector.z, vector.y);
                break;
                
            case CoordinateSystem.Unity:
                // Keep Unity coordinates
                result = vector;
                break;
        }
        
        return result;
    }
    
    Vector3 ApplyAccelerometerCharacteristics(Vector3 rawAccel)
    {
        Vector3 result = rawAccel;
        
        // Apply bias
        result += accelBias;
        
        // Apply temperature drift
        float tempDrift = GetTemperatureDrift();
        result += Vector3.one * tempDrift * tempDriftCoeff;
        
        // Add noise
        result += GetNoiseVector(accelNoise);
        
        // Apply saturation (clamp to sensor range)
        float maxAccel = accelRange * 9.81f; // Convert g to m/s²
        result.x = Mathf.Clamp(result.x, -maxAccel, maxAccel);
        result.y = Mathf.Clamp(result.y, -maxAccel, maxAccel);
        result.z = Mathf.Clamp(result.z, -maxAccel, maxAccel);
        
        return result;
    }
    
    Vector3 ApplyGyroscopeCharacteristics(Vector3 rawGyro)
    {
        Vector3 result = rawGyro;
        
        // Apply bias
        result += gyroBias;
        
        // Apply temperature drift
        float tempDrift = GetTemperatureDrift();
        result += Vector3.one * tempDrift * tempDriftCoeff * 0.1f;
        
        // Add noise
        result += GetNoiseVector(gyroNoise);
        
        // Apply saturation (clamp to sensor range)
        float maxGyro = gyroRange * Mathf.Deg2Rad; // Convert deg/s to rad/s
        result.x = Mathf.Clamp(result.x, -maxGyro, maxGyro);
        result.y = Mathf.Clamp(result.y, -maxGyro, maxGyro);
        result.z = Mathf.Clamp(result.z, -maxGyro, maxGyro);
        
        return result;
    }
    
    Vector3 GetNoiseVector(float standardDeviation)
    {
        // Generate Gaussian noise for each axis
        return new Vector3(
            GaussianNoise(0f, standardDeviation),
            GaussianNoise(0f, standardDeviation),
            GaussianNoise(0f, standardDeviation)
        );
    }
    
    float GaussianNoise(float mean, float standardDeviation)
    {
        // Box-Muller transform for Gaussian noise
        float u1 = 1f - (float)noiseGenerator.NextDouble();
        float u2 = 1f - (float)noiseGenerator.NextDouble();
        float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
        return mean + standardDeviation * randStdNormal;
    }
    
    float GetTemperatureDrift()
    {
        return temperature - 25f; // Drift relative to 25°C reference
    }
    
    void CalibrateIMU()
    {
        // Simulate calibration process
        Debug.Log($"IMU Calibrated at {DateTime.Now}");
        Debug.Log($"Coordinate System: {coordinateSystem}");
        Debug.Log($"Update Rate: {updateRate} Hz");
        Debug.Log($"Accel Range: ±{accelRange}g, Gyro Range: ±{gyroRange}°/s");
    }
    
    void LogIMUData()
    {
        Debug.Log($"IMU Data - Accel: {currentData.acceleration:F3}, Gyro: {currentData.angularVelocity:F3}, Temp: {currentData.temperature:F1}°C");
    }
    
    // Public API functions that return the 6-axis IMU data
    
    /// <summary>
    /// Get complete IMU data structure
    /// </summary>
    public IMUData GetIMUData()
    {
        return currentData;
    }
    
    /// <summary>
    /// Get 6-axis IMU vectors as separate arrays (acceleration + angular velocity)
    /// </summary>
    public void GetIMUVectors(out Vector3[] accelerations, out Vector3[] angularVelocities)
    {
        accelerations = new Vector3[] { currentData.acceleration };
        angularVelocities = new Vector3[] { currentData.angularVelocity };
    }
    
    /// <summary>
    /// Get linear acceleration vector (3-axis accelerometer)
    /// </summary>
    public Vector3 GetAcceleration()
    {
        return currentData.acceleration;
    }
    
    /// <summary>
    /// Get angular velocity vector (3-axis gyroscope)
    /// </summary>
    public Vector3 GetAngularVelocity()
    {
        return currentData.angularVelocity;
    }
    
    /// <summary>
    /// Get gravity vector in IMU frame
    /// </summary>
    public Vector3 GetGravity()
    {
        return currentData.gravity;
    }
    
    /// <summary>
    /// Get acceleration without gravity (true linear acceleration)
    /// </summary>
    public Vector3 GetLinearAcceleration()
    {
        return currentData.acceleration - currentData.gravity;
    }
    
    /// <summary>
    /// Get all 6 IMU values as a single array [ax, ay, az, gx, gy, gz]
    /// </summary>
    public float[] GetIMUArray()
    {
        return new float[]
        {
            currentData.acceleration.x, currentData.acceleration.y, currentData.acceleration.z,
            currentData.angularVelocity.x, currentData.angularVelocity.y, currentData.angularVelocity.z
        };
    }
    
    /// <summary>
    /// Get sensor temperature
    /// </summary>
    public float GetTemperature()
    {
        return currentData.temperature;
    }
    
    /// <summary>
    /// Get timestamp of last reading
    /// </summary>
    public double GetTimestamp()
    {
        return currentData.timestamp;
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // Draw coordinate system axes
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * 0.5f);
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 0.5f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
        
        // Draw acceleration vector
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 accelDir = transform.TransformDirection(currentData.acceleration.normalized);
            Gizmos.DrawRay(transform.position, accelDir * currentData.acceleration.magnitude * 0.1f);
        }
    }
}
