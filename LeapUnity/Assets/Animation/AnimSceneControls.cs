using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This component exposes methods for setting various properties of the scene
/// and objects in it, for use in animation events. The idea is to attach this
/// component to an animated object and then clips on that object
/// can be annotated with events that call the component's methods. The component
/// effectively acts as a dispatcher for animation events local to the object.
/// </summary>
public class AnimSceneControls : MonoBehaviour
{
    /// <summary>
    /// Show specified model.
    /// </summary>
    /// <param name="modelName">Model name</param>
    public void ShowModel(string modelName)
    {
        /*GameObject[] models = GameObject.FindGameObjectsWithTag("Agent");
        var model = models.FirstOrDefault(m => m.name == modelName);
        if (model != null)
        {
            model.SetActive(true);
        }*/
        Debug.LogWarning("ShowModel");
    }

    /// <summary>
    /// Hide specified model.
    /// </summary>
    /// <param name="modelName">Model name</param>
    public void HideModel(string modelName)
    {
        /*GameObject[] models = GameObject.FindGameObjectsWithTag("Agent");
        var model = models.FirstOrDefault(m => m.name == modelName);
        if (model != null)
        {
            model.SetActive(false);
        }*/
        Debug.LogWarning("HideModel");
    }
}
