using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GazeEditTestScenario : Scenario
{
    public GameObject[] models = new GameObject[0];
    public string[] animations = new string[0];

    /// <see cref="Scenario._Init()"/>
    protected override void _Init()
    {
    }

    /// <see cref="Scenario._Run()"/>
    protected override IEnumerator _Run()
    {
        float maxAnimationLength = 0f;
        for (int modelIndex = 0; modelIndex < models.Length; ++modelIndex)
        {
            var model = models[modelIndex];
            string animationName = animations[modelIndex];

            model.GetComponent<AnimControllerTree>().enabled = false;

            model.animation.Stop();
            model.animation[animationName].weight = 1f;
            model.animation[animationName].wrapMode = WrapMode.Once;
            model.animation[animationName].enabled = true;

            maxAnimationLength = Mathf.Max(maxAnimationLength, model.animation[animationName].length);
        }

        yield return new WaitForSeconds(maxAnimationLength);

        cameras.FirstOrDefault(c => c.Value.camera.enabled).Value.camera.enabled = false;

        yield break;
    }

    /// <see cref="Scenario._Finish()"/>
    protected override void _Finish()
    {
    }
}
