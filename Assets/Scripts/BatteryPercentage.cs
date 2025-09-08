using UnityEngine;

public class BatteryPercentage : MonoBehaviour
{
    [Header("Battery Settings")]
    public int startingBattery = 100;
    public float drainRate = 1f; // percentage per second

    private float currentBattery;
    private bool isActive = true;
    [Header("Logging Settings")]
    public bool logBattery = false;
    void Start()
    {
        currentBattery = startingBattery;
    }

    void Update()
    {
        if (isActive && currentBattery > 0)
        {
            currentBattery -= drainRate * Time.deltaTime;

            if (currentBattery <= 0)
            {
                currentBattery = 0;
                isActive = false;
                Debug.Log("Battery depleted - System stopped");
            }
        }
        if (logBattery)
        {
            Debug.Log($"Current Battery: {currentBattery}%");
        }
    }

    public int GetBatteryPercentage()
    {
        return Mathf.RoundToInt(currentBattery);
    }
}