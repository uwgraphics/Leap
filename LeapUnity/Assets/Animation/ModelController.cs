using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Leap model controller. This component implements
/// some helper functions which allow animation controllers
/// to interact with the character model.
/// </summary>
public class ModelController : MonoBehaviour
{
    private Transform rootBone = null; // Model skeleton root
    private Transform lEyeBone = null;
    private Transform rEyeBone = null;
    private Transform headBone = null;

    // Initial pose (both local and global)
    private Dictionary<int, Vector3> initLPos = new Dictionary<int, Vector3>();
    private Dictionary<int, Quaternion> initLRot = new Dictionary<int, Quaternion>();
    private Dictionary<int, Vector3> initLScal = new Dictionary<int, Vector3>();
    private Dictionary<int, Vector3> initWPos = new Dictionary<int, Vector3>();
    private Dictionary<int, Quaternion> initWRot = new Dictionary<int, Quaternion>();

    // Previous pose, or rather pose at the end of LateUpdate phase of the previous frame
    private Dictionary<int, Vector3> prevLPos = new Dictionary<int, Vector3>();
    private Dictionary<int, Quaternion> prevLRot = new Dictionary<int, Quaternion>();
    private Dictionary<int, Vector3> prevLScal = new Dictionary<int, Vector3>();

    /// <summary>
    /// Shorthand for getting the model root. 
    /// </summary>
    public virtual Transform Root
    {
        get
        {
            return rootBone;
        }
    }

    /// <summary>
    /// Shorthand for getting the left eye.
    /// </summary>
    public virtual Transform LEye
    {
        get
        {
            return lEyeBone;
        }
    }

    /// <summary>
    /// Shorthand for getting the right eye.
    /// </summary>
    public virtual Transform REye
    {
        get
        {
            return rEyeBone;
        }
    }

    /// <summary>
    /// Shorthand for getting the head.
    /// </summary>
    public virtual Transform Head
    {
        get
        {
            return headBone;
        }
    }

    /// <summary>
    /// Gets the previous (stored) position of the specified bone.
    /// </summary>
    /// <param name="bone">
    /// Bone reference.
    /// </param>
    /// <returns>
    /// Previous position.
    /// </returns>
    public Vector3 GetPrevPosition(Transform bone)
    {
        if (prevLPos.ContainsKey(bone.GetInstanceID()))
            return prevLPos[bone.GetInstanceID()];

        return new Vector3(0, 0, 0);
    }

    /// <summary>
    /// Gets the previous (stored) rotation of the specified bone.
    /// </summary>
    /// <param name="bone">
    /// Bone reference.
    /// </param>
    /// <returns>
    /// Previous rotation.
    /// </returns>
    public Quaternion GetPrevRotation(Transform bone)
    {
        if (prevLRot.ContainsKey(bone.GetInstanceID()))
        {
            return prevLRot[bone.GetInstanceID()];
        }

        return new Quaternion(0, 0, 0, 1);
    }

    /// <summary>
    /// Gets the previous (stored) scale of the specified bone.
    /// </summary>
    /// <param name="bone">
    /// Bone reference.
    /// </param>
    /// <returns>
    /// Previous scale.
    /// </returns>
    public Vector3 GetPrevScale(Transform bone)
    {
        if (prevLScal.ContainsKey(bone.GetInstanceID()))
            return prevLScal[bone.GetInstanceID()];

        return new Vector3(1, 1, 1);
    }

    /// <summary>
    /// Gets the initial position of the specified bone.
    /// </summary>
    /// <param name="bone">
    /// Bone name <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Initial position <see cref="Vector3"/>
    /// </returns>
    public Vector3 GetInitPosition(Transform bone)
    {
        if (initLPos.ContainsKey(bone.GetInstanceID()))
            return initLPos[bone.GetInstanceID()];

        return new Vector3(0, 0, 0);
    }

    /// <summary>
    /// Gets the initial rotation of the specified bone.
    /// </summary>
    /// <param name="bone">
    /// Bone name <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Initial rotation <see cref="Vector3"/>
    /// </returns>
    public Quaternion GetInitRotation(Transform bone)
    {
        if (initLRot.ContainsKey(bone.GetInstanceID()))
            return initLRot[bone.GetInstanceID()];

        return new Quaternion(0, 0, 0, 1);
    }

    /// <summary>
    /// Gets the initial scale of the specified bone.
    /// </summary>
    /// <param name="bone">
    /// Bone name <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Initial scale <see cref="Vector3"/>
    /// </returns>
    public Vector3 GetInitScale(Transform bone)
    {
        if (initLScal.ContainsKey(bone.GetInstanceID()))
            return initLScal[bone.GetInstanceID()];

        return new Vector3(1, 1, 1);
    }

    /// <summary>
    /// Gets the initial position of the specified bone.
    /// </summary>
    /// <param name="bone">
    /// Bone name <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Initial position <see cref="Vector3"/>
    /// </returns>
    public Vector3 GetInitWorldPosition(Transform bone)
    {
        if (initWPos.ContainsKey(bone.GetInstanceID()))
            return initWPos[bone.GetInstanceID()];

        return new Vector3(0, 0, 0);
    }

    /// <summary>
    /// Gets the initial rotation of the specified bone.
    /// </summary>
    /// <param name="bone">
    /// Bone name <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Initial rotation <see cref="Vector3"/>
    /// </returns>
    public Quaternion GetInitWorldRotation(Transform bone)
    {
        if (initWRot.ContainsKey(bone.GetInstanceID()))
            return initWRot[bone.GetInstanceID()];

        return new Quaternion(0, 0, 0, 1);
    }

    /// <summary>
    /// Resets the model to its initial pose. 
    /// </summary>
    public void _ResetToInitialPose()
    {
        _ResetToInitPose(rootBone);
    }

    /// <summary>
    /// Stores the current pose of the model.
    /// </summary>
    public void _StoreCurrentPose()
    {
        _StoreCurrentPose(rootBone);
    }

    /// <summary>
    /// Initialize the model controller.
    /// </summary>
    public void Init()
    {
        if (rootBone == null)
            _InitBones();

        // Get initial skeletal pose
        _GetInitBoneTransforms(rootBone);
    }

    void Awake()
    {
        _InitBones();
    }

    void Update()
    {
    }

    private void _InitBones()
    {
        // Try to find bones by their tags and set them
        if (rootBone == null)
            rootBone = ModelUtils.FindRootBone(gameObject);
        if (rootBone == null)
            rootBone = gameObject.transform;
        if (lEyeBone == null)
            lEyeBone = ModelUtils.FindBoneWithTag(rootBone, "LEyeBone");
        if (rEyeBone == null)
            rEyeBone = ModelUtils.FindBoneWithTag(rootBone, "REyeBone");
        if (headBone == null)
            headBone = ModelUtils.FindBoneWithTag(rootBone, "HeadBone");
    }

    private void _GetInitBoneTransforms(Transform bone)
    {
        initLPos[bone.GetInstanceID()] = bone.localPosition;
        initLRot[bone.GetInstanceID()] = bone.localRotation;
        initLScal[bone.GetInstanceID()] = bone.localScale;
        initWPos[bone.GetInstanceID()] = bone.position;
        initWRot[bone.GetInstanceID()] = bone.rotation;

        foreach (Transform child in bone)
        {
            _GetInitBoneTransforms(child);
        }
    }

    private void _ResetToInitPose(Transform root)
    {
        root.localPosition = GetInitPosition(root);
        root.localRotation = GetInitRotation(root);
        root.localScale = GetInitScale(root);

        foreach (Transform child in root)
        {
            _ResetToInitPose(child);
        }
    }

    private void _StoreCurrentPose(Transform root)
    {
        prevLPos[root.GetInstanceID()] = root.localPosition;
        prevLRot[root.GetInstanceID()] = root.localRotation;
        prevLScal[root.GetInstanceID()] = root.localScale;

        foreach (Transform child in root)
        {
            _StoreCurrentPose(child);
        }
    }
}

