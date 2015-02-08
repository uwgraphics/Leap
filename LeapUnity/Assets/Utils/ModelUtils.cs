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
    public static string[] rootBoneNames = { "root", "pelvis", "hip", "Bip01" };
    public static string[] lEyeBoneNames = { "lEye" };
    public static string[] rEyeBoneNames = { "rEye" };
    public static string[] headBoneNames = { "head" };
    public static string[] gazeHelperNames = { "GazeHelper", "Dummy" };

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
        if (bone.tag == "Agent")
            return "";

        string parentPath = GetBonePath(bone.parent);
        return (parentPath != "" ? parentPath + "/" : "") + bone.name;
    }

    // Traverse a model's bone hierarchy and add all bones into a list
    private static void _GetAllBones(Transform rootBone, List<Transform> bones)
    {
        if (rootBone == null)
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
            bone = ModelUtils.FindBoneByKeyword(gameObj.transform, name);

            if (bone != null)
            {
                bone.tag = "RootBone";

                break;
            }
        }

        if (bone == null || bone.tag != "RootBone")
        {
            UnityEngine.Debug.LogWarning("Failed to automatically tag the model Root bone. You will need to identify and tag the bone manually.");
        }

        // Tag the left eye
        foreach (string name in lEyeBoneNames)
        {
            bone = ModelUtils.FindBoneByKeyword(gameObj.transform, name);

            if (bone != null)
            {
                bone.tag = "LEyeBone";

                // Find the helper, too
                foreach (string hname in gazeHelperNames)
                {
                    hbone = ModelUtils.FindBoneByKeyword(bone, name + hname);

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
            UnityEngine.Debug.LogWarning("Failed to automatically tag the model Left Eye bone. You will need to identify and tag the bone manually.");
        }
        if (hbone == null || hbone.tag != "LEyeGazeHelper")
        {
            UnityEngine.Debug.LogWarning("Failed to automatically tag the model Left Eye gaze helper bone. You will need to identify and tag the bone manually.");
        }

        // Tag the right eye
        foreach (string name in rEyeBoneNames)
        {
            bone = ModelUtils.FindBoneByKeyword(gameObj.transform, name);

            if (bone != null)
            {
                bone.tag = "REyeBone";

                // Find the helper, too
                foreach (string hname in gazeHelperNames)
                {
                    hbone = ModelUtils.FindBoneByKeyword(bone, name + hname);

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
            UnityEngine.Debug.LogWarning("Failed to automatically tag the model Right Eye bone. You will need to identify and tag the bone manually.");
        }
        if (hbone == null || hbone.tag != "REyeGazeHelper")
        {
            UnityEngine.Debug.LogWarning("Failed to automatically tag the model Right Eye gaze helper bone. You will need to identify and tag the bone manually.");
        }

        // TODO: tag torso and head bones
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
}
