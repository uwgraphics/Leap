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
    public static string[] rootBoneNames = { "root", "pelvis", "hip", "Bip01" };
    public static string[] lEyeBoneNames = { "lEye" };
    public static string[] rEyeBoneNames = { "rEye" };
    public static string[] headBoneNames = { "head" };
    public static string[] neckBoneNames = { "neck" };
    public static string[] gazeHelperNames = { "GazeHelper", "Dummy" };

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
        _StoreCurPose(rootBone);
    }

    /// <summary>
    /// Initializes the model controller.
    /// </summary>
    public void _Init()
    {
        // Get initial skeletal pose
        _GetInitBoneTransforms(rootBone);
    }

    void Awake()
    {
        // Try to find bones by their tags and set them
        if (rootBone == null)
            rootBone = FindRootBone(gameObject);
        if (rootBone == null)
            rootBone = gameObject.transform;
        if (lEyeBone == null)
            lEyeBone = FindBoneWithTag(rootBone, "LEyeBone");
        if (rEyeBone == null)
            rEyeBone = FindBoneWithTag(rootBone, "REyeBone");
        if (headBone == null)
            headBone = FindBoneWithTag(rootBone, "HeadBone");
    }

    void Update()
    {
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

    private void _StoreCurPose(Transform root)
    {
        prevLPos[root.GetInstanceID()] = root.localPosition;
        prevLRot[root.GetInstanceID()] = root.localRotation;
        prevLScal[root.GetInstanceID()] = root.localScale;

        foreach (Transform child in root)
        {
            _StoreCurPose(child);
        }
    }

    /// <summary>
    /// Finds a bone in the agent's skeleton by name.
    /// </summary>
    /// <param name="parent">
    /// Parent bone.
    /// </param>
    /// <param name="bone">
    /// Bone name.
    /// </param>
    /// <returns>
    /// Bone, or null if the bone cannot be found <see cref="Transform"/>
    /// </returns>
    public static Transform FindBone(Transform parent, string boneName)
    {
        if (parent.name == boneName)
            return parent;

        Transform bone = null;

        foreach (Transform child in parent)
        {
            bone = FindBone(child, boneName);

            if (bone != null)
                break;
        }

        return bone;
    }

    /// <summary>
    /// Finds a bone in the agent's skeleton by keyword.
    /// </summary>
    /// <param name="parent">
    /// Parent bone <see cref="System.String"/>
    /// </param>
    /// <param name="nameKeyword">
    /// Bone name keyword <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Bone, or null if the bone cannot be found <see cref="Transform"/>
    /// </returns>
    public static Transform FindBoneByKeyword(Transform parent, string nameKeyword)
    {
        if (parent.name.ToLowerInvariant().Contains(nameKeyword.ToLowerInvariant()))
            return parent;

        Transform bone = null;

        foreach (Transform child in parent)
        {
            bone = FindBone(child, nameKeyword);

            if (bone != null)
                break;
        }

        return bone;
    }

    /// <summary>
    /// Finds a bone in the agent's skeleton by tag.
    /// </summary>
    /// <param name="parent">
    /// Parent bone <see cref="System.String"/>
    /// </param>
    /// <param name="boneTag">
    /// Bone tag <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Bone, or null if the bone cannot be found <see cref="Transform"/>
    /// </returns>
    public static Transform FindBoneWithTag(Transform parent, string boneTag)
    {
        if (parent.tag == boneTag)
            return parent;

        Transform bone = null;

        foreach (Transform child in parent)
        {
            bone = FindBoneWithTag(child, boneTag);

            if (bone != null)
                break;
        }

        return bone;
    }

    /// <summary>
    /// Finds the root bone of a character model. 
    /// </summary>
    /// <param name="obj">
    /// Character model <see cref="GameObject"/>
    /// </param>
    /// <returns>
    /// Root bone, or null if it could not be found <see cref="Transform"/>
    /// </returns>
    public static Transform FindRootBone(GameObject obj)
    {
        Transform root = FindBoneWithTag(obj.transform, "RootBone");
        if (root == null)
        {
            foreach (Transform child in obj.transform)
            {
                if (child.childCount > 0)
                {
                    // We'll make an educated guess that this is the root
                    root = child;
                    break;
                }
            }
        }

        return root;
    }

    /// <summary>
    /// Automatically attaches tags to model parts. 
    /// </summary>
    /// <param name="gameObj">
    /// Model to tag. <see cref="GameObject"/>
    /// </param>
    public static void AutoTagModel(GameObject gameObj)
    {
        // TODO: we could improve this further, e.g. use string matching

        Transform bone = null, hbone = null;

        // Tag the model
        gameObj.tag = "Agent";

        // Tag the root bone
        foreach (string name in rootBoneNames)
        {
            bone = ModelController.FindBoneByKeyword(gameObj.transform, name);

            if (bone != null)
            {
                bone.tag = "RootBone";

                break;
            }
        }

        if (bone == null || bone.tag != "RootBone")
        {
            Debug.LogWarning("Failed to automatically tag the model Root bone. You will need to identify and tag the bone manually.");
        }

        // Tag the left eye
        foreach (string name in lEyeBoneNames)
        {
            bone = ModelController.FindBoneByKeyword(gameObj.transform, name);

            if (bone != null)
            {
                bone.tag = "LEyeBone";

                // Find the helper, too
                foreach (string hname in gazeHelperNames)
                {
                    hbone = ModelController.FindBoneByKeyword(bone, name + hname);

                    if (hbone != null)
                    {
                        hbone.tag = "LEyeGazeHelper";

                        break;
                    }
                }

                break;
            }
        }

        if (bone == null || bone.tag != "LEyeBone")
        {
            Debug.LogWarning("Failed to automatically tag the model Left Eye bone. You will need to identify and tag the bone manually.");
        }
        if (hbone == null || hbone.tag != "LEyeGazeHelper")
        {
            Debug.LogWarning("Failed to automatically tag the model Left Eye gaze helper bone. You will need to identify and tag the bone manually.");
        }

        // Tag the right eye
        foreach (string name in rEyeBoneNames)
        {
            bone = ModelController.FindBoneByKeyword(gameObj.transform, name);

            if (bone != null)
            {
                bone.tag = "REyeBone";

                // Find the helper, too
                foreach (string hname in gazeHelperNames)
                {
                    hbone = ModelController.FindBoneByKeyword(bone, name + hname);

                    if (hbone != null)
                    {
                        hbone.tag = "REyeGazeHelper";

                        break;
                    }
                }

                break;
            }
        }

        if (bone == null || bone.tag != "REyeBone")
        {
            Debug.LogWarning("Failed to automatically tag the model Right Eye bone. You will need to identify and tag the bone manually.");
        }
        if (hbone == null || hbone.tag != "REyeGazeHelper")
        {
            Debug.LogWarning("Failed to automatically tag the model Right Eye gaze helper bone. You will need to identify and tag the bone manually.");
        }

        // TODO: tag torso and head bones
    }
}

