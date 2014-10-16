using UnityEngine;
using System.Collections;

public class GazeEditTestScenario : Scenario
{
    /// <see cref="Scenario._Init()"/>
    protected override void _Init()
    {
    }

    /// <see cref="Scenario._Run()"/>
    protected override IEnumerator _Run()
    {
        GameObject agent = agents["NormanMittens"];
        agent.animation.Play("Sneaking", PlayMode.StopAll);

        yield break;
    }

    /// <see cref="Scenario._Finish()"/>
    protected override void _Finish()
    {
    }
}
