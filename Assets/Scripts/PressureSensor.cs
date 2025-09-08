using UnityEngine;

public class PressureSensor : MonoBehaviour
{

    public bool logPressure = false;

    private void Update()
    {
        if (logPressure)
        {
            Debug.Log($"Current pressure: {GetPressureValue()}");
        }
    }
    public float GetPressureValue()
    {
        float pressure = Input.GetKey(KeyCode.Space) ? 1.0f : 0.0f;
        return pressure;
    }
}