using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation instance corresponding to a pre-made animation clip.
/// </summary>
public class EnvironmentObjectAnimationInstance : AnimationClipInstance
{
    /// <summary>
    /// Always null for environment objects.
    /// </summary>
    public override ModelController ModelController
    {
        get { return null; }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation clip name</param>
    /// <param name="model">Character model</param>
    public EnvironmentObjectAnimationInstance(string name, GameObject model) : base(name, model)
    {
        // Write animation clip to file
        /*string path = LEAPCore.environmentModelsDirectory + "/" + AnimationClip.name + ".anim";
        if (AssetDatabase.GetAssetPath(AnimationClip) != path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(AnimationClip, path);
        }
        AssetDatabase.SaveAssets();*/
    }
}
