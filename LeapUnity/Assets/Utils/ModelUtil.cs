using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
        var bones = model.tag == "Agent" ? ModelUtil.GetAllBones(model) :
            new Transform[] { model.transform };

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
        var bones = model.tag == "Agent" ? ModelUtil.GetAllBones(model) :
            new Transform[] { model.transform };

        bones[0].position = rootPosition;
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            bones[boneIndex].localRotation = boneRotations[boneIndex];
    }
}

/// <summary>
/// Some useful methods for working with LEAP character models.
/// </summary>
public static class ModelUtil
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
    /// Get materials used by model renderers.
    /// </summary>
    /// <param name="model">Model</param>
    /// <param name="shared">If false, the method will return material instances unique to each renderer</param>
    /// <returns>Model materials</returns>
    public static Material[] GetModelMaterials(GameObject model, bool shared = true)
    {
        var materials = new List<Material>();

        SkinnedMeshRenderer[] skinnedMeshRenderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            materials.AddRange(shared ? skinnedMeshRenderer.sharedMaterials : skinnedMeshRenderer.materials);

        MeshRenderer[] meshRenderers = model.GetComponentsInChildren<MeshRenderer>();
        foreach (var meshRenderer in meshRenderers)
            materials.AddRange(shared ? meshRenderer.sharedMaterials : meshRenderer.materials);

        return materials.ToArray();
    }

    /// <summary>
    /// Set materials used by model renderers.
    /// </summary>
    /// <param name="model">Model</param>
    /// <param name="materials">Model materials</param>
    public static void SetModelMaterials(GameObject model, Material[] materials)
    {
        int matIndex = 0;

        SkinnedMeshRenderer[] skinnedMeshRenderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
        {
            var sharedMaterials = new Material[skinnedMeshRenderer.sharedMaterials.Length];
            for (int curMatIndex = 0; curMatIndex < skinnedMeshRenderer.sharedMaterials.Length; ++curMatIndex)
                sharedMaterials[curMatIndex] = materials[matIndex++];
            skinnedMeshRenderer.sharedMaterials = sharedMaterials;
        }

        MeshRenderer[] meshRenderers = model.GetComponentsInChildren<MeshRenderer>();
        foreach (var meshRenderer in meshRenderers)
        {
            var sharedMaterials = new Material[meshRenderer.sharedMaterials.Length];
            for (int curMatIndex = 0; curMatIndex < meshRenderer.sharedMaterials.Length; ++curMatIndex)
                sharedMaterials[curMatIndex] = materials[matIndex++];
            meshRenderer.sharedMaterials = sharedMaterials;
        }
    }

    /// <summary>
    /// Set material used by model renderers.
    /// </summary>
    /// <param name="model">Model</param>
    /// <param name="mat">Material</param>
    public static void SetModelMaterial(GameObject model, Material mat)
    {
        SkinnedMeshRenderer[] skinnedMeshRenderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
        {
            var sharedMaterials = new Material[skinnedMeshRenderer.sharedMaterials.Length];
            for (int curMatIndex = 0; curMatIndex < skinnedMeshRenderer.sharedMaterials.Length; ++curMatIndex)
                sharedMaterials[curMatIndex] = mat;
            skinnedMeshRenderer.sharedMaterials = sharedMaterials;
        }

        MeshRenderer[] meshRenderers = model.GetComponentsInChildren<MeshRenderer>();
        foreach (var meshRenderer in meshRenderers)
        {
            var sharedMaterials = new Material[meshRenderer.sharedMaterials.Length];
            for (int curMatIndex = 0; curMatIndex < meshRenderer.sharedMaterials.Length; ++curMatIndex)
                sharedMaterials[curMatIndex] = mat;
            meshRenderer.sharedMaterials = sharedMaterials;
        }
    }

    /// <summary>
    /// Get all models that are below the specified model in the scene hierarchy.
    /// </summary>
    /// <param name="model">Model</param>
    /// <returns>Child models</returns>
    public static GameObject[] GetSubModels(GameObject model)
    {
        var children = new List<GameObject>();
        _GetSubModels(model, children);
        children.RemoveAll(sm => sm.tag == "EndEffectorTarget" || sm.tag == "ManipulatedObjectHandle");

        return children.ToArray();
    }

    // Get all models that are below the specified model in the scene hierarchy.
    private static void _GetSubModels(GameObject model, List<GameObject> subModels)
    {
        subModels.Add(model);

        for (int childIndex = 0; childIndex < model.transform.childCount; ++childIndex)
        {
            var child = model.transform.GetChild(childIndex).gameObject;
            _GetSubModels(child, subModels);
        }
    }

    /// <summary>
    /// Get all models that are below the specified model in the scene hierarchy and have the specified tag.
    /// </summary>
    /// <param name="model">Model</param>
    /// <param name="tag">Model tag</param>
    /// <returns>Child models</returns>
    public static GameObject[] GetSubModelsWithTag(GameObject model, string tag)
    {
        var subModels = new List<GameObject>();
        _GetSubModels(model, subModels);
        subModels.RemoveAll(m => m.tag != tag);

        return subModels.ToArray();
    }

    /// <summary>
    /// Get all end-effector targets on the model.
    /// </summary>
    /// <param name="obj">Character model</param>
    /// <returns>Array of all end-effector targets</returns>
    public static Transform[] GetEndEffectorTargets(GameObject obj)
    {
        var endEffectorTargets = new List<Transform>();
        if (obj.tag == "Agent")
        {
            var bones = GetAllBones(obj);
            foreach (var bone in bones)
            {
                for (int childIndex = 0; childIndex < bone.childCount; ++childIndex)
                {
                    var child = bone.GetChild(childIndex);
                    if (child.tag == "EndEffectorTarget")
                        endEffectorTargets.Add(child);
                }
            }
        }
        else
        {
            var submodels = GetSubModels(obj);
            foreach (var submodel in submodels)
                if (submodel.tag == "EndEffectorTarget")
                    endEffectorTargets.Add(submodel.transform);
        }

        return endEffectorTargets.ToArray();
    }

    /// <summary>
    /// Show/hide skeleton visualization gizmos.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="show">If true, the skeleton will be shown, otherwise it will be hidden</param>
    public static void ShowBoneGizmos(GameObject model, bool show = true)
    {
        var root = FindBoneWithTag(model.transform, "RootBone");
        _ShowBoneGizmos(root, show);
    }

    // Show bone visualization gizmos in a specific subtree
    private static void _ShowBoneGizmos(Transform bone, bool show = true)
    {
        if (bone.tag != "RootBone")
        {
            string gizmoName = bone.name + "Gizmo";
            var gizmo = FindChild(bone.parent, gizmoName);
            if (gizmo != null)
            {
                gizmo.parent = null;
                GameObject.DestroyImmediate(gizmo.gameObject);
            }

            // Create bone gizmo
            var gizmoObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gizmoObj.name = gizmoName;
            gizmoObj.renderer.material = Resources.Load("BoneGizmo", typeof(Material)) as Material;
            gizmo = gizmoObj.transform;
            gizmo.tag = "BoneGizmo";
            gizmo.parent = bone.parent;
            Component.DestroyImmediate(gizmo.collider);

            // Position, orient, and scale the bone gizmo
            Vector3 gizmoPosition = 0.5f * bone.localPosition;
            Quaternion gizmoRotation = Quaternion.FromToRotation(new Vector3(0f, 1f, 0f), bone.localPosition.normalized);
            Vector3 gizmoScale = new Vector3(2f, bone.localPosition.magnitude, 2f);
            gizmo.localPosition = gizmoPosition;
            gizmo.localRotation = gizmoRotation;
            gizmo.localScale = gizmoScale;

            var gizmoComponent = gizmoObj.AddComponent<BoneGizmo>();
            gizmoComponent.bone = bone;
            gizmoObj.active = show;
        }

        for (int childIndex = 0; childIndex < bone.childCount; ++childIndex)
        {
            var child = bone.GetChild(childIndex);
            if (child.tag == "BoneGizmo" || child.tag == "Untagged")
                continue;

            _ShowBoneGizmos(child, show);
        }
    }

    /// <summary>
    /// Set all of character's models transformations to zero.
    /// </summary>
    /// <param name="model">Character model</param>
    public static void ResetModelToZeroPose(GameObject model)
    {
        var bones = GetAllBones(model);
        bones[0].position = Vector3.zero;
        foreach (var bone in bones)
            bone.localRotation = Quaternion.identity;
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
        return FindBoneWithTag(obj.transform, "RootBone");
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
    /// Find the index of the specified bone in the character model.
    /// </summary>
    /// <param name="allBones">Array of all bones</param>
    /// <param name="bone">Bone</param>
    /// <returns>Bone index or -1 if bone cannot be found</returns>
    public static int FindBoneIndex(Transform[] allBones, Transform bone)
    {
        for (int boneIndex = 0; boneIndex < allBones.Length; ++boneIndex)
            if (allBones[boneIndex] == bone)
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
    /// Check if specified bone is a descendant of another bone.
    /// </summary>
    /// <param name="descBone">Descendant bone</param>
    /// <param name="ancBone">Ancestor bone</param>
    /// <returns>true if bone is descendant, false otherwise</returns>
    public static bool IsDescendantOf(Transform descBone, Transform ancBone)
    {
        if (descBone == null)
            return false;
        
        if (descBone == ancBone)
            return true;

        return IsDescendantOf(descBone.parent, ancBone);
    }

    /// <summary>
    /// Get a chain of bones between the start bone and one of its descendants.
    /// Both start bone and end bone are included in the chain.
    /// </summary>
    /// <param name="startBone">Chain start bone</param>
    /// <param name="endBone">Chain end bone, which is a descendant of the start bone</param>
    /// <returns>Bone chain</returns>
    public static Transform[] GetBoneChain(Transform startBone, Transform endBone)
    {
        if (!IsDescendantOf(endBone, startBone))
            return null;

        List<Transform> _boneChain = new List<Transform>();
        _GetBoneChain(startBone, endBone, _boneChain);

        return _boneChain.ToArray();
    }

    /// <summary>
    /// Get length of the chain of bones between the start bone and one of its descendants.
    /// Both start bone and end bone are included in the chain.
    /// </summary>
    /// <param name="startBone">Chain start bone</param>
    /// <param name="endBone">Chain end bone, which is a descendant of the start bone</param>
    /// <returns>Bone chain</returns>
    public static float GetBoneChainLength(Transform startBone, Transform endBone)
    {
        var chain = GetBoneChain(startBone, endBone);
        if (chain == null)
            throw new ArgumentException("startBone and endBone do not form a chain", "startBone");

        float chainlength = 0f;
        for (int boneIndex = 1; boneIndex < chain.Length; ++boneIndex)
            chainlength += (chain[boneIndex].position - chain[boneIndex].parent.position).magnitude;

        return chainlength;
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

    /// <summary>
    /// true if  the specified transform is a bone, false otherwise.
    /// </summary>
    /// <param name="bone">Transform</param>
    /// <returns>true if  the specified transform is a bone, false otherwise</returns>
    public static bool IsBone(Transform bone)
    {
        return bone.tag == "Untagged" || bone.tag != "Untagged" && bone.tag.EndsWith("Bone");
    }

    /// <summary>
    /// Find a child of the specified bone by name.
    /// </summary>
    /// <param name="bone">Bone</param>
    /// <param name="childName">Child name</param>
    /// <returns>Child or null if the child could not be found</returns>
    public static Transform FindChild(Transform bone, string childName)
    {
        for (int childIndex = 0; childIndex < bone.childCount; ++childIndex)
        {
            var child = bone.GetChild(childIndex);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    // Traverse a model's bone hierarchy and add all bones to the list
    private static void _GetAllBones(Transform rootBone, List<Transform> bones)
    {
        if (rootBone == null || !IsBone(rootBone))
            return;

        bones.Add(rootBone);

        foreach (Transform child in rootBone)
            _GetAllBones(child, bones);
    }

    // Traverse the bone subtree until you get to the end bone and add all the bones in between
    private static bool _GetBoneChain(Transform startBone, Transform endBone, List<Transform> _boneChain)
    {
        if (endBone == null)
            return false;

        if (startBone == endBone)
        {
            _boneChain.Insert(0, startBone);
            return true;
        }

        _boneChain.Insert(0, endBone);

        return _GetBoneChain(startBone, endBone.parent, _boneChain);
    }

    /// <summary>
    /// Delete all bones in the subtree with the specified tag.
    /// </summary>
    /// <param name="root">Root bone</param>
    /// <param name="tag">Tag</param>
    public static void DeleteBonesWithTag(Transform root, string tag)
    {
        for (int childIndex = 0; childIndex < root.childCount; ++childIndex)
        {
            var child = root.GetChild(childIndex);
            DeleteBonesWithTag(child, tag);

            if (child.tag == tag && child.childCount <= 0)
            {
                child.parent = null;
                GameObject.DestroyImmediate(child.gameObject);
                --childIndex;
            }
        }
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
    /// Get/create the helper target object for the specified end-effector.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="endEffectorTag">End-effector tag</param>
    /// <returns>End-effector target helper</returns>
    public static GameObject GetEndEffectorTargetHelper(GameObject model, string endEffectorTag)
    {
        var endEffectorTargets = GameObject.FindGameObjectsWithTag("EndEffectorTarget");
        string helperName = GetEndEffectorTargetHelperName(model, endEffectorTag);
        var helper = endEffectorTargets.FirstOrDefault(h => h.name == helperName);
        if (helper == null)
        {
            // Target helper does not exist, create it
            helper = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helper.name = helperName;
            helper.tag = "EndEffectorTarget";
            GameObject.DestroyImmediate(helper.renderer);
        }
        
        return helper;
    }

    /// <summary>
    /// Find bone in the character model closest to the specified point in world coordinates.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="wPos">World position</param>
    /// <returns>Closest bone</returns>
    public static Transform FindClosestBoneToPoint(GameObject model, Vector3 wPos)
    {
        var bones = GetAllBones(model);
        float minDist = float.MaxValue;
        Transform closestBone = null;
        foreach (var bone in bones)
        {
            for (int childIndex = 0; childIndex < bone.childCount; ++childIndex)
            {
                var child = bone.GetChild(childIndex);
                var wPosProj = GeometryUtil.ProjectPointOntoLineSegment(wPos, bone.position, child.position);
                float dist = Vector3.Distance(wPos, wPosProj);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestBone = bone;
                }
            }
        }

        return closestBone;
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

        // Get rotational plane for the shortest-arc rotation
        bone.localRotation = qs;
        Vector3 vs = bone.forward;
        bone.localRotation = qt;
        Vector3 vt = bone.forward;
        Vector3 n = !GeometryUtil.Equal(vs, vt) ? Vector3.Cross(vs, vt) : Vector3.Cross(vs, -bone.right);

        // Project current rotation onto that plane
        bone.localRotation = q;
        Vector3 v = bone.forward;
        Vector3 vp = GeometryUtil.ProjectVectorOntoPlane(v, n);
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
