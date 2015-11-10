using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;

/// <summary>
/// Structure holding a character model's skeletal pose.
/// </summary>
public struct ModelPose
{
    Vector3 rootPosition;
    Quaternion[] boneRotations;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    public ModelPose(GameObject model)
    {
        Transform[] bones = ModelUtils.GetAllBones(model);
        rootPosition = bones[0].position;
        boneRotations = new Quaternion[bones.Length];
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            boneRotations[boneIndex] = bones[boneIndex].localRotation;
    }

    /// <summary>
    /// Apply the pose to a character model.
    /// </summary>
    /// <param name="model">Character model</param>
    public void Apply(GameObject model)
    {
        Transform[] bones = ModelUtils.GetAllBones(model);
        bones[0].position = rootPosition;
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            bones[boneIndex].localRotation = boneRotations[boneIndex];
    }
}

/// <summary>
/// Some useful methods for working with LEAP character models.
/// </summary>
public static class ModelUtils
{
    /// <summary>
    /// Show/hide character model.
    /// </summary>
    /// <param name="model">Model</param>
    /// <param name="show">If true, the model will be shown, otherwise it will be hidden</param>
    public static void ShowModel(GameObject model, bool show = true)
    {
        SkinnedMeshRenderer[] skinnedMeshRenderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            skinnedMeshRenderer.enabled = show;

        MeshRenderer[] meshRenderers = model.GetComponentsInChildren<MeshRenderer>();
        foreach (var meshRenderer in meshRenderers)
            meshRenderer.enabled = show;
    }

    /// <summary>
    /// Get world scale of the specified bone.
    /// </summary>
    /// <param name="bone">Bone</param>
    /// <returns>World scale</returns>
    public static Vector3 GetBoneScale(Transform bone)
    {
        if (bone.parent == null)
        {
            return bone.localScale;
        }
        else
        {
            Vector3 parentScale = GetBoneScale(bone.parent);
            return new Vector3(bone.localScale.x * parentScale.x,
                bone.localScale.y * parentScale.y,
                bone.localScale.z * parentScale.z);
        }
    }

    /// <summary>
    /// Finds a bone in the model's skeleton by name.
    /// </summary>
    /// <param name="rootBone">
    /// Root bone.
    /// </param>
    /// <param name="bone">
    /// Bone name.
    /// </param>
    /// <returns>
    /// Bone, or null if the bone cannot be found <see cref="Transform"/>
    /// </returns>
    public static Transform FindBone(Transform rootBone, string boneName)
    {
        if (rootBone.name == boneName)
            return rootBone;

        Transform bone = null;

        foreach (Transform child in rootBone)
        {
            bone = FindBone(child, boneName);

            if (bone != null)
                break;
        }

        return bone;
    }

    /// <summary>
    /// Finds a bone in the model's skeleton by keyword.
    /// </summary>
    /// <param name="rootBone">
    /// Root bone <see cref="System.String"/>
    /// </param>
    /// <param name="nameKeyword">
    /// Bone name keyword <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Bone, or null if the bone cannot be found <see cref="Transform"/>
    /// </returns>
    public static Transform FindBoneByKeyword(Transform rootBone, string nameKeyword)
    {
        if (rootBone.name.ToLowerInvariant().Contains(nameKeyword.ToLowerInvariant()))
            return rootBone;

        Transform bone = null;

        foreach (Transform child in rootBone)
        {
            bone = FindBone(child, nameKeyword);

            if (bone != null)
                break;
        }

        return bone;
    }

    /// <summary>
    /// Finds a bone in the model's skeleton by tag.
    /// </summary>
    /// <param name="rootBone">
    /// Root bone
    /// </param>
    /// <param name="boneTag">
    /// Bone tag
    /// </param>
    /// <returns>
    /// Bone, or null if the bone cannot be found <see cref="Transform"/>
    /// </returns>
    public static Transform FindBoneWithTag(Transform rootBone, string boneTag)
    {
        if (rootBone.tag == boneTag)
            return rootBone;

        Transform bone = null;

        foreach (Transform child in rootBone)
        {
            bone = FindBoneWithTag(child, boneTag);

            if (bone != null)
                break;
        }

        return bone;
    }

    /// <summary>
    /// Finds all bones in the model's skeleton with tag.
    /// </summary>
    /// <param name="obj">
    /// Character model
    /// </param>
    /// <param name="boneTag">
    /// Bone tag
    /// </param>
    /// <returns>
    /// Array of bones, or null bones cannot be found
    /// </returns>
    public static Transform[] GetAllBonesWithTag(GameObject obj, string boneTag)
    {
        Transform[] allBones = GetAllBones(obj);
        List<Transform> bonesWithTag = new List<Transform>();

        foreach (var bone in allBones)
        {
            if (bone.tag == boneTag)
                bonesWithTag.Add(bone);
        }

        return bonesWithTag.ToArray();
    }

    /// <summary>
    /// Finds the root bone of a character model. 
    /// </summary>
    /// <param name="obj">
    /// Character model
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
    /// Find the index of the specified bone in the character model.
    /// </summary>
    /// <param name="obj">Character model</param>
    /// <param name="bone">Bone</param>
    /// <returns>Bone index or -1 if bone cannot be found</returns>
    public static int FindBoneIndex(GameObject obj, Transform bone)
    {
        Transform[] bones = GetAllBones(obj);
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            if (bones[boneIndex] == bone)
                return boneIndex;

        return -1;
    }

    /// <summary>
    /// Get all bones in a character model.
    /// </summary>
    /// <param name="obj">Character model</param>
    /// <returns>Array of all bones</returns>
    public static Transform[] GetAllBones(GameObject obj)
    {
        var bones = new List<Transform>();
        _GetAllBones(FindRootBone(obj), bones);
        return bones.ToArray();
    }

    /// <summary>
    /// Get all bones that are descendents of the specified bone.
    /// </summary>
    /// <param name="obj">Root bone</param>
    /// <returns>Array of all descendent bones</returns>
    /// <remarks>Input bone is included in the returned array of bones.</remarks>
    public static Transform[] GetAllBonesInSubtree(Transform root)
    {
        var bones = new List<Transform>();
        _GetAllBones(root, bones);
        return bones.ToArray();
    }

    /// <summary>
    /// Get a chain of bones between a start bone and one of its descendants.
    /// Both start bone and end bone are included in the chain.
    /// </summary>
    /// <param name="startBone">Chain start bone</param>
    /// <param name="endBone">Chain end bone, which is a descendant of the start bone</param>
    /// <returns>Bone chain</returns>
    public static Transform[] GetBoneChain(Transform startBone, Transform endBone)
    {
        List<Transform> _boneChain = new List<Transform>();
        _GetBoneChain(startBone, endBone, _boneChain);

        return _boneChain.ToArray();
    }

    /// <summary>
    /// Get path of the specified bone relative to the root.
    /// </summary>
    /// <param name="bone">Bone transform</param>
    /// <returns>Relative path of the bone</returns>
    public static string GetBonePath(Transform bone)
    {
        if (bone.tag == "Agent" || bone.tag == "ManipulatedObject")
            return "";

        string parentPath = GetBonePath(bone.parent);
        return (parentPath != "" ? parentPath + "/" : "") + bone.name;
    }

    // Traverse a model's bone hierarchy and add all bones to the list
    private static void _GetAllBones(Transform rootBone, List<Transform> bones)
    {
        if (rootBone == null || rootBone.tag != "Untagged" && !rootBone.tag.EndsWith("Bone"))
            return;

        bones.Add(rootBone);

        foreach (Transform child in rootBone)
        {
            _GetAllBones(child, bones);
        }
    }

    // Traverse the bone subtree until you get to the end bone and add all the bones in between
    private static bool _GetBoneChain(Transform startBone, Transform endBone, List<Transform> _boneChain)
    {
        if (startBone == endBone)
        {
            _boneChain.Add(startBone);
            return true;
        }

        for (int childIndex = 0; childIndex < startBone.GetChildCount(); ++childIndex)
        {
            var child = startBone.GetChild(childIndex);
            if (_GetBoneChain(child, endBone, _boneChain))
            {
                _boneChain.Add(startBone);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get all meshes with blend shapes in a character model.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <returns>Meshes with blend shapes</returns>
    public static SkinnedMeshRenderer[] GetAllMeshesWithBlendShapes(GameObject model)
    {
        List<SkinnedMeshRenderer> meshesWithBlendShapes = new List<SkinnedMeshRenderer>();
        _GetAllMeshesWithBlendShapes(model.transform, meshesWithBlendShapes);
        return meshesWithBlendShapes.ToArray();
    }

    // Traverse a model's hierarchy and add all meshes with blend shapes to the list
    private static void _GetAllMeshesWithBlendShapes(Transform bone, List<SkinnedMeshRenderer> meshesWithBlendShapes)
    {
        var mesh = bone.gameObject.GetComponent<SkinnedMeshRenderer>();
        if (mesh != null && mesh.sharedMesh.blendShapeCount > 0)
            meshesWithBlendShapes.Add(mesh);

        for (int childIndex = 0; childIndex < bone.GetChildCount(); ++childIndex)
        {
            _GetAllMeshesWithBlendShapes(bone.GetChild(childIndex), meshesWithBlendShapes);
        }
    }

    /// <summary>
    /// Get currently selected character model (or null if no model is selected)
    /// </summary>
    /// <returns>Selected character model</returns>
    public static GameObject GetSelectedModel()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null || obj.GetComponent<ModelController>() == null)
        {
            return null;
        }

        return obj;
    }

    /// <summary>
    /// Get all end-effectors in the specified character model
    /// </summary>
    /// <param name="model">Character model</param>
    /// <returns>Array of all end-effectors</returns>
    public static Transform[] GetEndEffectors(GameObject model)
    {
        List<Transform> endEffectors = new List<Transform>();

        endEffectors.AddRange(GetAllBonesWithTag(model, LEAPCore.lWristTag));
        endEffectors.AddRange(GetAllBonesWithTag(model, LEAPCore.rWristTag));
        endEffectors.AddRange(GetAllBonesWithTag(model, LEAPCore.lAnkleTag));
        endEffectors.AddRange(GetAllBonesWithTag(model, LEAPCore.rAnkleTag));

        return endEffectors.ToArray();
    }

    /// <summary>
    /// Get the name of the helper target for the specified end-effector.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="endEffectorTag">End effector tag</param>
    /// <returns>End-effector helper target</returns>
    public static string GetEndEffectorTargetHelperName(GameObject model, string endEffectorTag)
    {
        string name = "";

        if (endEffectorTag == LEAPCore.lWristTag)
            name = LEAPCore.lWristTargetHelper;
        else if (endEffectorTag == LEAPCore.rWristTag)
            name = LEAPCore.rWristTargetHelper;
        else if (endEffectorTag == LEAPCore.lAnkleTag)
            name = LEAPCore.lAnkleTargetHelper;
        else if (endEffectorTag == LEAPCore.rAnkleTag)
            name = LEAPCore.rAnkleTargetHelper;
        else
            throw new Exception("Unknown end-effector: " + endEffectorTag);

        name = model.name + name;
        return name;
    }

    /// <summary>
    /// Project specified rotation onto a spherical arc around the specified bone.
    /// </summary>
    /// <param name="bone">Bone</param>
    /// <param name="qs">Source rotation of the arc</param>
    /// <param name="qt">Target rotation of the arc</param>
    /// <param name="q">Rotation</param>
    /// <returns>Projected rotation</returns>
    public static Quaternion ProjectRotationOntoArc(Transform bone, Quaternion qs, Quaternion qt, Quaternion q)
    {
        Quaternion q0 = bone.localRotation;

        // Get rotational plane for the shortest-arc gaze shift
        bone.localRotation = qs;
        Vector3 vs = bone.forward;
        bone.localRotation = qt;
        Vector3 vt = bone.forward;
        Vector3 n = !GeomUtil.Equal(vs, vt) ? Vector3.Cross(vs, vt) : Vector3.Cross(vs, -bone.right);

        // Project current rotation onto that plane
        bone.localRotation = q;
        Vector3 v = bone.forward;
        Vector3 vp = GeomUtil.ProjectVectorOntoPlane(v, n);
        vp.Normalize();
        Quaternion qp = q * Quaternion.FromToRotation(v, vp);

        bone.localRotation = q0;

        return qp;
    }

    /// <summary>
    /// Get rotation that aligns the forward vector of the specified bone with a point in world space.
    /// </summary>
    /// <param name="bone">Bone that is being aligned</param>
    /// <param name="wTargetPos">Target position in world space</param>
    /// <returns></returns>
    public static Quaternion LookAtRotation(Transform bone, Vector3 wTargetPos)
    {
        Quaternion curRot = bone.localRotation;
        bone.LookAt(wTargetPos);
        Quaternion trgRot = bone.localRotation;
        bone.localRotation = curRot;

        return trgRot;
    }

    /// <summary>
    /// Get rotation that aligns the forward vector of the specified bone with a point in world space,
    /// projected into the horizontal x-z plane.
    /// </summary>
    /// <param name="bone">Bone that is being aligned</param>
    /// <param name="wTargetPos">Target position in world space</param>
    /// <returns></returns>
    public static Quaternion LookAtRotationInHorizontalPlane(Transform bone, Vector3 wTargetPos)
    {
        Quaternion curRot = bone.localRotation;
        Vector3 curDir = new Vector3(bone.forward.x, 0f, bone.forward.z);
        
        Quaternion trgRot = LookAtRotation(bone, wTargetPos);
        bone.localRotation = trgRot;
        Vector3 trgDir = new Vector3(bone.forward.x, 0f, bone.forward.z);

        bone.localRotation = curRot;

        trgRot = curRot * Quaternion.FromToRotation(curDir, trgDir);
        return trgRot;
    }
}
