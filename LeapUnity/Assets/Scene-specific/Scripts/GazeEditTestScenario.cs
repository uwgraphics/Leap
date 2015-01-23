using UnityEngine;
using System.Collections;

public class GazeEditTestScenario : Scenario
{
    public string characterName = "Norman";
    public string editedAnimation = "";

    /// <see cref="Scenario._Init()"/>
    protected override void _Init()
    {
    }

    /// <see cref="Scenario._Run()"/>
    protected override IEnumerator _Run()
    {
        GameObject agent = agents[characterName];
        agent.animation.Stop();
        agent.animation[editedAnimation].weight = 1f;
        agent.animation[editedAnimation].wrapMode = WrapMode.Loop;
        agent.animation[editedAnimation].enabled = true;

        yield break;
    }

    /// <see cref="Scenario._Finish()"/>
    protected override void _Finish()
    {
    }
}
