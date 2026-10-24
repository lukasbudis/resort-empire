
using UnityEngine;

public class SimulationRunner : MonoBehaviour
{
    private const float TickInterval = 0.1f;

    private GameSimulation simulation;
    private float accumulatedTime = 0f;

    private void Awake()
    {
        simulation = new GameSimulation();
    }

    private void Update()
    {
        if (simulation.IsPaused)
        {
            return;
        }

        accumulatedTime += Time.deltaTime;

        while (accumulatedTime >= TickInterval)
        {
            simulation.Step();
            accumulatedTime -= TickInterval;

            if (simulation.TickCount % 10 == 0)
            {
                Debug.Log("Simulation Tick: " + simulation.TickCount);
            }
        }
    }


    [ContextMenu("Pause Simulation")]
    public void PauseSimulation()
    {
        simulation.Pause();
    }

    [ContextMenu("Resume Simulation")]
    public void ResumeSimulation()
    {
        simulation.Resume();
    }

}
