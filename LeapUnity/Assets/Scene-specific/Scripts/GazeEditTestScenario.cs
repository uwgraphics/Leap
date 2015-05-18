using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GazeEditTestScenario : Scenario
{
    public GameObject[] models = new GameObject[0];
    public string[] animations = new string[0];
    public string[] cameraAnimations = new string[0];

    /// <see cref="Scenario._Init()"/>
    protected override void _Init()
    {
    }

    /// <see cref="Scenario._Run()"/>
    protected override IEnumerator _Run()
    {
        // Compute animation length
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

        // Enable camera animations (if any)
        for (int cameraAnimationIndex = 0; cameraAnimationIndex < cameraAnimations.Length; ++cameraAnimationIndex)
        {
            string cameraAnimationName = cameraAnimations[cameraAnimationIndex];
            foreach (var kvp in cameras)
            {
                if (kvp.Value.animation != null && kvp.Value.animation.GetClip(cameraAnimationName) != null)
                {
                    var cameraAnimation = kvp.Value.animation;
                    cameraAnimation.Stop();
                    cameraAnimation[cameraAnimationName].weight = 1f;
                    cameraAnimation[cameraAnimationName].wrapMode = WrapMode.Once;
                    cameraAnimation[cameraAnimationName].enabled = true;
                }
            }
        }

        yield return new WaitForSeconds(maxAnimationLength);

        // Disable all cameras
        foreach (var kvp in cameras)
            kvp.Value.camera.enabled = false;

        yield break;
    }

    /// <see cref="Scenario._Finish()"/>
    protected override void _Finish()
    {
    }
}
