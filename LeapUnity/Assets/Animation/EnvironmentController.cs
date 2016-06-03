using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Leap environment controller. This component implements some helper functions
/// which allow manipulation of environment objects by agents.
/// </summary>
public sealed class EnvironmentController : MonoBehaviour
{
    private List<GameObject> _manipulatedObjects = new List<GameObject>();
    private List<Camera> _cameras = new List<Camera>();
    private Dictionary<GameObject, Vector3> _initObjPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> _initObjRotations = new Dictionary<GameObject, Quaternion>();

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
        get { return _manipulatedObjects.AsReadOnly(); }
    }

    /// <summary>
    /// List of cameras in the environment.
    /// </summary>
    public IList<Camera> Cameras
    {
        get { return _cameras.AsReadOnly(); }
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
        foreach (var obj in _manipulatedObjects)
        {
            obj.transform.localPosition = _initObjPositions[obj];
            obj.transform.localRotation = _initObjRotations[obj];
        }

        foreach (var cam in _cameras)
        {
            cam.gameObject.transform.localPosition = _initObjPositions[cam.gameObject];
            cam.gameObject.transform.localRotation = _initObjRotations[cam.gameObject];
        }
    }

    private void _InitObjects()
    {
        RootObject = gameObject;
        _manipulatedObjects.Clear();
        _cameras.Clear();
        _initObjPositions.Clear();
        _initObjRotations.Clear();

        _InitObjects(RootObject);
    }

    private void _InitObjects(GameObject obj)
    { 
        if (obj.tag == "ManipulatedObject")
        {
            _manipulatedObjects.Add(obj);
            _initObjPositions[obj] = obj.transform.localPosition;
            _initObjRotations[obj] = obj.transform.localRotation;
        }
        else if (obj.tag == "MainCamera")
        {
            _cameras.Add(obj.GetComponent<Camera>());
            _initObjPositions[obj] = obj.transform.localPosition;
            _initObjRotations[obj] = obj.transform.localRotation;
        }

        var transf = obj.transform;
        for (int childIndex = 0; childIndex < transf.childCount; ++childIndex)
            _InitObjects(transf.GetChild(childIndex).gameObject);
    }
}
