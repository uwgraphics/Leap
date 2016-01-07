using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Leap model controller. This component implements
/// some helper functions which allow animation controllers
/// to interact with the character model.
/// </summary>
public sealed class ModelController : MonoBehaviour
{
    // Bones:
    private Transform rootBone = null; // Model skeleton root
    private Transform lEyeBone = null;
    private Transform rEyeBone = null;
    private Transform[] bones;
    private Dictionary<Transform, int> boneIndexes = new Dictionary<Transform, int>();

    // Initial pose (both local and global)
    private Dictionary<int, Vector3> initLPos = new Dictionary<int, Vector3>();
    private Dictionary<int, Quaternion> initLRot = new Dictionary<int, Quaternion>();
    private Dictionary<int, Vector3> initLScal = new Dictionary<int, Vector3>();
    private Dictionary<int, Vector3> initWPos = new Dictionary<int, Vector3>();
    private Dictionary<int, Quaternion> initWRot = new Dictionary<int, Quaternion>();

    // Meshes with blend shapes:
    private SkinnedMeshRenderer[] meshesWithBlendShapes;

    /// <summary>
    /// Shorthand for getting the model root. 
    /// </summary>
    public Transform Root
    {
        get
        {
            return rootBone;
        }
    }

    /// <summary>
    /// Shorthand for getting the left eye.
    /// </summary>
    public Transform LEye
    {
        get
        {
            return lEyeBone;
        }
    }

    /// <summary>
    /// Shorthand for getting the right eye.
    /// </summary>
    public Transform REye
    {
        get
        {
            return rEyeBone;
        }
    }

    /// <summary>
    /// Number of bones in the model.
    /// </summary>
    public int NumberOfBones
    {
        get { return bones.Length; }
    }

    /// <summary>
    /// Number of blend shapes in the model.
    /// </summary>
    public int NumberOfBlendShapes
    {
        get
        {
            int numBlendShapes = 0;
            foreach (var mesh in meshesWithBlendShapes)
                numBlendShapes += mesh.sharedMesh.blendShapeCount;

            return numBlendShapes;
        }
    }

    /// <summary>
    /// Get bone at the specified index.
    /// </summary>
    /// <param name="iboneIndexndex">Bone index</param>
    /// <returns>Bone</returns>
    public Transform GetBone(int boneIndex)
    {
        return bones[boneIndex];
    }

    /// <summary>
    /// Get index of the specified bone.
    /// </summary>
    /// <param name="bone">Bone</param>
    /// <returns>Bone index</returns>
    public int GetBoneIndex(Transform bone)
    {
        return boneIndexes[bone];
    }

    /// <summary>
    /// Position of the character, computed as average of positions of both feet.
    /// </summary>
    public Vector3 BodyPosition
    {
        get
        {
            var lFoot = GameObject.FindGameObjectWithTag("LAnkleBone").transform;
            var rFoot = GameObject.FindGameObjectWithTag("RAnkleBone").transform;

            return 0.5f * (lFoot.transform.position + rFoot.transform.position);
        }
    }

    /// <summary>
    /// Facing direction of the character's whole body,
    /// computed from positions of both feet.
    /// </summary>
    public Vector3 BodyDirection
    {
        get
        {
            var lFoot = GameObject.FindGameObjectWithTag("LAnkleBone").transform;
            var rFoot = GameObject.FindGameObjectWithTag("RAnkleBone").transform;

            Vector3 bodyRight = (rFoot.position - lFoot.position).normalized;
            Vector3 bodyUp = new Vector3(0f, 1f, 0f);

            return -Vector3.Cross(bodyUp, bodyRight);
        }
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
    /// Get blend shape weight.
    /// </summary>
    /// <param name="blendShapeIndex">Blend shape index</param>
    /// <returns>Blend shape weight</returns>
    public float GetBlendShapeWeight(int blendShapeIndex)
    {
        int blendShapeIndexWithinMesh = blendShapeIndex;
        foreach (var mesh in meshesWithBlendShapes)
        {
            if (blendShapeIndexWithinMesh < mesh.sharedMesh.blendShapeCount)
            {
                return mesh.GetBlendShapeWeight(blendShapeIndexWithinMesh);
            }
            else
            {
                blendShapeIndexWithinMesh -= mesh.sharedMesh.blendShapeCount;
            }
        }

        return 0f;
    }

    /// <summary>
    /// Set blend shape weight.
    /// </summary>
    /// <param name="blendShapeIndex">Blend shape index</param>
    /// <param name="weight">Blend shape weight</param>
    public void SetBlendShapeWeight(int blendShapeIndex, float weight)
    {
        int blendShapeIndexWithinMesh = blendShapeIndex;
        foreach (var mesh in meshesWithBlendShapes)
        {
            if (blendShapeIndexWithinMesh < mesh.sharedMesh.blendShapeCount)
            {
                mesh.SetBlendShapeWeight(blendShapeIndexWithinMesh, weight);
            }
            else
            {
                blendShapeIndexWithinMesh -= mesh.sharedMesh.blendShapeCount;
            }
        }
    }

    /// <summary>
    /// Get blend shape mesh and index within mesh for the specified blend shape.
    /// </summary>
    /// <param name="blendShapeIndex">Blend shape index</param>
    /// <param name="meshWithBlendShape">Mesh with blend shape</param>
    /// <param name="blendShapeIndexWithinMesh">Blend shape index within mesh</param>
    public void GetBlendShape(int blendShapeIndex, out SkinnedMeshRenderer meshWithBlendShape, out int blendShapeIndexWithinMesh)
    {
        blendShapeIndexWithinMesh = blendShapeIndex;
        meshWithBlendShape = null;
        foreach (var mesh in meshesWithBlendShapes)
        {
            if (blendShapeIndexWithinMesh < mesh.sharedMesh.blendShapeCount)
            {
                meshWithBlendShape = mesh;
                break;
            }
            else
            {
                blendShapeIndexWithinMesh -= mesh.sharedMesh.blendShapeCount;
            }
        }
    }

    /// <summary>
    /// Resets the model to its initial pose. 
    /// </summary>
    public void _ResetToInitialPose()
    {
        foreach (var bone in bones)
        {
            bone.localPosition = initLPos.ContainsKey(bone.GetInstanceID()) ? GetInitPosition(bone) : bone.localPosition;
            bone.localRotation = initLRot.ContainsKey(bone.GetInstanceID()) ? GetInitRotation(bone) : bone.localRotation;
            bone.localScale = initLScal.ContainsKey(bone.GetInstanceID()) ? GetInitScale(bone) : bone.localScale;
        }
    }

    /// <summary>
    /// Initialize the model controller.
    /// </summary>
    public void Init()
    {
        _InitBones();
        _GetInitBoneTransforms();
        _InitBlendShapes();
    }

    public void Awake()
    {
        _InitBones();
    }

    public void Update()
    {
    }

    public void LateUpdate()
    {
    }

    private void _InitBones()
    {
        // Try to find bones by their tags and set them
        if (rootBone == null)
            rootBone = ModelUtil.FindRootBone(gameObject);
        if (rootBone == null)
            rootBone = gameObject.transform;
        if (lEyeBone == null)
            lEyeBone = ModelUtil.FindBoneWithTag(rootBone, "LEyeBone");
        if (rEyeBone == null)
            rEyeBone = ModelUtil.FindBoneWithTag(rootBone, "REyeBone");

        // Get list of all bones and their indexes
        bones = ModelUtil.GetAllBones(gameObject);
        boneIndexes.Clear();
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            boneIndexes[bones[boneIndex]] = boneIndex;
    }

    private void _GetInitBoneTransforms()
    {
        foreach (var bone in bones)
        {
            initLPos[bone.GetInstanceID()] = bone.localPosition;
            initLRot[bone.GetInstanceID()] = bone.localRotation;
            initLScal[bone.GetInstanceID()] = bone.localScale;
            initWPos[bone.GetInstanceID()] = bone.position;
            initWRot[bone.GetInstanceID()] = bone.rotation;
        }
    }

    private void _InitBlendShapes()
    {
        meshesWithBlendShapes = ModelUtil.GetAllMeshesWithBlendShapes(gameObject);
    }
}

