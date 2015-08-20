using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Leap environment controller. This component implements some helper functions
/// which allow manipulation of environment objects by agents.
/// </summary>
public sealed class EnvironmentController : MonoBehaviour
{
    private List<GameObject> manipulatedObjects = new List<GameObject>();
    private Dictionary<GameObject, Vector3> initObjPositions = new Dictionary<GameObject,Vector3>();
    private Dictionary<GameObject, Quaternion> initObjRotations = new Dictionary<GameObject,Quaternion>();

    /// <summary>
    /// Root environment object.
    /// </summary>
    public GameObject RootObject
    {
        get;
        private set;
    }

    /// <summary>
    /// List of objects in the environment that can be manipulated.
    /// </summary>
    public IList<GameObject> ManipulatedObjects
    {
        get { return manipulatedObjects.AsReadOnly(); }
    }

    /// <summary>
    /// Initialize the environment controller.
    /// </summary>
    public void Init()
    {
        _InitObjects();
    }

    /// <summary>
    /// Reset environmnet objects to their initial layout.
    /// </summary>
    public void ResetToInitialLayout()
    {
        foreach (var obj in manipulatedObjects)
        {
            obj.transform.localPosition = initObjPositions[obj];
            obj.transform.localRotation = initObjRotations[obj];
        }
    }

    private void _InitObjects()
    {
        RootObject = gameObject;
        manipulatedObjects.Clear();
        initObjPositions.Clear();
        initObjRotations.Clear();

        _InitObjects(RootObject);
    }

    private void _InitObjects(GameObject obj)
    {
        if (obj.tag == "ManipulatedObject")
        {
            manipulatedObjects.Add(obj);
            initObjPositions[obj] = obj.transform.localPosition;
            initObjRotations[obj] = obj.transform.localRotation;
        }

        var transf = obj.transform;
        for (int childIndex = 0; childIndex < transf.childCount; ++childIndex)
            _InitObjects(transf.GetChild(childIndex).gameObject);
    }
}
