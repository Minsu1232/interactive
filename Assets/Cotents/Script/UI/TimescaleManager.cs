using UnityEngine;

public class TimescaleManager : MonoBehaviour
{
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable ()
    {
        Time.timeScale = 0f; // Set the time scale to normal speed
    }

    private void OnDisable()
    {
        // Reset the time scale to normal speed when this object is destroyed
        Time.timeScale = 1f;
    }

}
