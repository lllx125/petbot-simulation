using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Motors : MonoBehaviour
{
    // forward/back spin target (deg/s) about local +X
    public float motor1SpeedDeg = 0f;

    // left/right internal weight motion (deg/s) around local Z, clamped angle
    public float motor2SpeedDeg = 0f;
    public float motor2AngleDeg = 0f;

    [Header("Mass & Geometry")]
    public float shellMass = 10f;
    public float weightMass = 1f;
    public float weightRadius = 0.075f; // distance from center INSIDE the sphere

    [Header("Weight (visual, optional)")]
    public Transform weight;       // make this a child of the sphere; no RB, no collider

    [Header("Motorization")]
    public bool useTorqueServo = true;     // true = physics-friendly servo; false = overwrite angular vel
    public float servoGain = 12f;          // tweak 6–24
    public float maxAngularVelocity = 50f; // safety

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = shellMass + weightMass;
        rb.maxAngularVelocity = maxAngularVelocity;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // optional: if your weight Transform isn't assigned, create a tiny gizmo
        if (!weight)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "Weight (gizmo)";
            weight = go.transform;
            weight.SetParent(transform, false);
            weight.localScale = Vector3.one * 0.03f;
        }
    }

    void FixedUpdate()
    {
        // 1) Advance the internal weight angle (deg), clamp to [-90, 90]
        motor2AngleDeg += motor2SpeedDeg * Time.fixedDeltaTime;
        motor2AngleDeg = Mathf.Clamp(motor2AngleDeg, -90f, 90f);

        // 2) Compute weight LOCAL position on a circle in the X–Y plane (around local Z)
        //    Start from -Y (bottom) then rotate around +Z by motor2AngleDeg
        Vector3 weightLocal =
            Quaternion.AngleAxis(motor2AngleDeg, Vector3.forward) * (-Vector3.up) * weightRadius;

        // 3) Move the visual weight (local space so it follows rotation/translation automatically)
        if (weight)
        {
            if (weight.parent != transform) weight.SetParent(transform, worldPositionStays: false);
            weight.localPosition = weightLocal;
        }

        // 4) Update center of mass in LOCAL space
        Vector3 comLocal = weightLocal * (weightMass / (shellMass + weightMass));
        rb.centerOfMass = comLocal;
        // If you change mass or scale dynamically, consider: rb.ResetInertiaTensor();

        // 5) Drive spin about local +X (forward/back motor)
        float targetOmega = motor1SpeedDeg * Mathf.Deg2Rad;              // rad/s
        Vector3 right = transform.right;

        if (useTorqueServo)
        {
            // physics-friendly velocity servo: push angular velocity toward target on the right axis
            float currentOmega = Vector3.Dot(rb.angularVelocity, right);
            float omegaError = targetOmega - currentOmega;
            rb.AddTorque(right * (omegaError * servoGain), ForceMode.Acceleration);
        }
        else
        {
            // hard set only the right-axis component, preserve other components from contacts
            rb.angularVelocity = rb.transform.right * motor1SpeedDeg;
        }
    }

    // These setters can be called from UI/Input; values are applied in FixedUpdate
    public void SetMotor1Speed(float newSpeedDegPerSec) => motor1SpeedDeg = newSpeedDegPerSec;
    public void SetMotor2Speed(float newSpeedDegPerSec) => motor2SpeedDeg = newSpeedDegPerSec;
    public void SetMotor2Angle(float newAngleDeg) => motor2AngleDeg = Mathf.Clamp(newAngleDeg, -90f, 90f);
}
