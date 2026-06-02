using UnityEngine;

public class Timer
{
    private float targetTime;
    private float startTime;

    public void Start(float _targetTime)
    {
        targetTime = _targetTime;
        startTime = Time.time;
    }

    public bool Stop()
    {
        return (Time.time - startTime) <= targetTime;
    }
}
