using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Class implementing scenario management features.
/// It can be used to schedule scenario execution and
/// configure their settings.
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    /// <summary>
    /// Scenarios to execute.
    /// </summary>
    public Scenario[] scenarios;

    /// <summary>
    /// If true, scenarios will execute in random sequence.
    /// </summary>
    public bool randomOrder = false;

    /// <summary>
    /// Fixed frame rate for scenario execution.
    /// </summary>
    /// <remarks>Set this to -1 to allow variable frame rate.</remarks>
    public int frameRate = 30;

    private void Awake()
    {
        LEAPCore.LoadConfiguration();
    }

    private IEnumerator Start()
    {
        // Set frame rate and resolution
        Application.targetFrameRate = frameRate;

        if (scenarios == null)
            yield break;

        // Scenario manager must persist
        // until all scenarios are executed
        DontDestroyOnLoad(gameObject);

        if (randomOrder)
            // Generate random scenario execution sequence
            _GenerateScenarioOrder();

        // Execute each scenario in sequence
        foreach (Scenario scen in scenarios)
        {
            if (scen.sceneName == "")
                scen.sceneName = scen.GetType().Name;

            // Load the scenario's scene
            if (Application.loadedLevelName != scen.sceneName)
            {
                Application.LoadLevel(scen.sceneName);

                // Wait until load complete
                while (Application.isLoadingLevel)
                {
                    yield return 0;
                }

                if (Application.loadedLevelName != scen.sceneName)
                {
                    // Failed to load level for some reason...

                    Debug.LogError("Failed to load scenario " + scen.sceneName);
                    break;
                }
            }

            yield return StartCoroutine(scen.Run());
        }

        Application.Quit();
    }

    private void _GenerateScenarioOrder()
    {
        List<Scenario> scen0 = new List<Scenario>(scenarios);
        List<Scenario> scen = new List<Scenario>();

        while (scen0.Count > 0)
        {
            int scen_i = Random.Range(0, scen0.Count - 1);
            scen.Add(scen0[scen_i]);
            scen0.RemoveAt(scen_i);
        }

        scenarios = scen.ToArray();
    }

}
