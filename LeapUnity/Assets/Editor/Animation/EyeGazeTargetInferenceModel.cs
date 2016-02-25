using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Class representing an inference models for targets of a gaze shift sequence.
/// </summary>
public class EyeGazeTargetInferenceModel
{
    private enum DomeRenderMode
    {
        Disabled,
        ModelOnly,
        ShowScene
    }
    
    /// <summary>
    /// Character model.
    /// </summary>
    public GameObject Model
    {
        get;
        private set;
    }

    /// <summary>
    /// Root object of the environment.
    /// </summary>
    public GameObject Environment
    {
        get;
        private set;
    }

    /// <summary>
    /// Eye gaze camera.
    /// </summary>
    public Camera EyeGazeCamera
    {
        get;
        private set;
    }

    /// <summary>
    /// Pixel width of the eye gaze camera.
    /// </summary>
    public int CameraWidth
    {
        get { return LEAPCore.gazeInferenceRenderTextureWidth; }
    }

    /// <summary>
    /// Pixel height of the eye gaze camera.
    /// </summary>
    public int CameraHeight
    {
        get { return Mathf.RoundToInt(((float)CameraWidth) / EyeGazeCamera.aspect); }
    }

    // Gaze target inference textures:
    private RenderTexture _rtView;
    private RenderTexture _rtWorldPosX;
    private RenderTexture _rtWorldPosY;
    private RenderTexture _rtWorldPosZ;
    private RenderTexture _rtGameObjID;
    private RenderTexture _rtPGazeShiftDir;
    private RenderTexture _rtPTaskRel;
    private RenderTexture _rtPHandCon;
    private RenderTexture _rtPTotal;
    private Texture2D _texWriteToFile;
    private Texture2D _texWorldPosX;
    private Texture2D _texWorldPosY;
    private Texture2D _texWorldPosZ;
    private Texture2D _texGameObjID;

    // Gaze target inference shaders and materials:
    private Shader _shaderWorldPos;
    private Material _matGameObjID;
    private Material _matPGazeShiftDir;
    private Material _matPTaskRel;
    private Material _matPHandCon;
    private Material _matPTotal;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="envRoot">Environment root object</param>
    public EyeGazeTargetInferenceModel(GameObject model, GameObject env)
    {
        this.Model = model;
        this.Environment = env;
        EyeGazeCamera = _GetEyeGazeCamera();

        // Create target inference render textures
        _rtView = _CreateRenderTexture(RenderTextureFormat.ARGB32);
        _rtWorldPosX = _CreateRenderTexture(RenderTextureFormat.ARGB32);
        _rtWorldPosY = _CreateRenderTexture(RenderTextureFormat.ARGB32);
        _rtWorldPosZ = _CreateRenderTexture(RenderTextureFormat.ARGB32);
        _rtGameObjID = _CreateRenderTexture(RenderTextureFormat.ARGB32);
        _rtPGazeShiftDir = _CreateRenderTexture(RenderTextureFormat.ARGB32);
        _rtPTaskRel = _CreateRenderTexture(RenderTextureFormat.ARGB32);
        _rtPHandCon = _CreateRenderTexture(RenderTextureFormat.ARGB32);
        _rtPTotal = _CreateRenderTexture(RenderTextureFormat.ARGB32);
        _texWriteToFile = _CreateTexture2D();

        // Get target inference shaders and materials
        _shaderWorldPos = Shader.Find("EyeGazeInference/RenderWorldPosition");
        _matGameObjID = Resources.Load("AnimationEditor/RenderGameObjectID", typeof(Material)) as Material;
        _matPGazeShiftDir = Resources.Load("AnimationEditor/PGazeShiftDirection", typeof(Material)) as Material;
        _matPTaskRel = Resources.Load("AnimationEditor/PTaskRelevance", typeof(Material)) as Material;
        _matPHandCon = Resources.Load("AnimationEditor/PHandContact", typeof(Material)) as Material;
        _matPTotal = Resources.Load("AnimationEditor/PTotal", typeof(Material)) as Material;
    }

    /// <summary>
    /// Destroy textures, materials, and shaders used for gaze target inference.
    /// </summary>
    public void DestroyResources()
    {
        /*Texture.DestroyImmediate(_rtView);
        Texture.DestroyImmediate(_rtWorldPosX);
        Texture.DestroyImmediate(_rtWorldPosY);
        Texture.DestroyImmediate(_rtWorldPosZ);
        Texture.DestroyImmediate(_rtGameObjID);
        Texture.DestroyImmediate(_rtPGazeShiftDir);
        Texture.DestroyImmediate(_rtPTaskRel);
        Texture.DestroyImmediate(_rtPHandCon);
        Texture.DestroyImmediate(_rtPTotal);
        Texture.DestroyImmediate(_texWriteToFile);
        Shader.DestroyImmediate(_shaderWorldPos);
        Material.DestroyImmediate(_matGameObjID);
        Material.DestroyImmediate(_matPGazeShiftDir);
        Material.DestroyImmediate(_matPTaskRel);
        Material.DestroyImmediate(_matPHandCon);
        Material.DestroyImmediate(_matPTotal);*/
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// For a sequence of gaze shifts, infer gaze target locations in the scene.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Gaze animation layer name</param>
    /// <param name="envLayerName">Environment animation layer name</param>
    public void InferTargets(AnimationTimeline timeline, int baseInstanceId, string layerName, string envLayerName)
    {
        var baseInstance = timeline.GetAnimation(baseInstanceId) as AnimationClipInstance;
        if (baseInstance.Model != Model)
            throw new Exception(string.Format("Base animation instance {0} on wrong character model {1}",
                baseInstance.Name, baseInstance.Model.name));

        Debug.Log("Inferring gaze instance targets in the scene...");

        var root = ModelUtil.FindRootBone(Model);
        var bones = ModelUtil.GetAllBones(Model);
        var gazeController = Model.GetComponent<GazeController>();
        var gazeLayer = timeline.GetLayer(layerName);

        // Deactivate gaze
        bool gazeControllerEnabled = gazeController.enabled;
        bool gazeLayerActive = gazeLayer.Active;
        gazeController.enabled = false;
        gazeLayer.Active = false;

        foreach (var scheduledInstance in gazeLayer.Animations)
        {
            var eyeGazeInstance = scheduledInstance.Animation as EyeGazeInstance;
            if (eyeGazeInstance.Model != baseInstance.Model) // Gaze shift on a different character
                continue;

            if (eyeGazeInstance.Name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // Infer target position for gazing straight ahead
                var aheadTargetPos = _InferEyeGazeTargetAhead(timeline, baseInstanceId, scheduledInstance.InstanceId);
                eyeGazeInstance.Target = null;
                eyeGazeInstance.AheadTargetPosition = aheadTargetPos;
            }
            else
            {
                // Infer most likely gaze shift target
                var gazeTarget = LEAPCore.useSimpleGazeTargetInference ?
                    _InferEyeGazeTargetSimple(timeline, baseInstanceId, scheduledInstance.InstanceId) :
                    _InferEyeGazeTarget(timeline, baseInstanceId, scheduledInstance.InstanceId);

                // Set gaze target
                eyeGazeInstance.Target = gazeTarget;
                var targetInstance = timeline.GetLayer(envLayerName).Animations.FirstOrDefault(inst =>
                    inst.Animation.Model == gazeTarget);
                if (targetInstance != null)
                    eyeGazeInstance._SetTargetAnimationClip((targetInstance.Animation as AnimationClipInstance).AnimationClip);
            }
        }

        // Reset to initial state
        gazeController.enabled = gazeControllerEnabled;
        gazeLayer.Active = gazeLayerActive;
        timeline.GoToFrame(0);
        timeline.ResetModelsAndEnvironment();
    }

    // Infer most likely gaze shift location by sampling from a spatial probability distribution
    private GameObject _InferEyeGazeTarget(AnimationTimeline timeline, int baseAnimationInstanceId,
        int eyeGazeInstanceId)
    {
        var baseInstance = timeline.GetAnimation(baseAnimationInstanceId) as AnimationClipInstance;
        var bones = ModelUtil.GetAllBones(Model);
        var gazeController = Model.GetComponent<GazeController>();
        var eyeGazeInstance = timeline.GetAnimation(eyeGazeInstanceId) as EyeGazeInstance;
        int startFrame = timeline.GetAnimationStartFrame(eyeGazeInstanceId);
        int fixationStartFrame = startFrame + eyeGazeInstance.FixationStartFrame;

        // Get scene scale
        float sceneScale = 200f;
        // TODO: calculate based on scene extents

        // Get model height
        timeline.ResetModelsAndEnvironment();
        float modelHeight = gazeController.head.Top.position.y;

        // Get gaze shift path start point
        timeline.GoToFrame(startFrame);
        timeline.ApplyAnimation();
        var eyeDir = gazeController.head.Direction;
        var eyePos = gazeController.EyeCenter;
        var eyePathStartPos = eyePos + modelHeight * eyeDir;

        // Get gaze shift path end point
        timeline.GoToFrame(fixationStartFrame);
        timeline.ApplyAnimation();
        eyeDir = gazeController.head.Direction;
        eyePos = gazeController.EyeCenter;
        var eyePathEndPos = eyePos + modelHeight * eyeDir;

        // Compute OMR
        float OMR = Mathf.Min(gazeController.lEye.outOMR, gazeController.lEye.inOMR);
        // TODO: use adjusted OMR

        // Compute extended gaze shift path end point
        var startEyeDir = (eyePathStartPos - eyePos).normalized;
        var endEyeDir = (eyePathEndPos - eyePos).normalized;
        var eyePathRot = Quaternion.FromToRotation(startEyeDir, endEyeDir);
        float eyePathAngle;
        Vector3 eyePathAxis;
        eyePathRot.ToAngleAxis(out eyePathAngle, out eyePathAxis);
        var exEyePathRot = Quaternion.AngleAxis(OMR, eyePathAxis);
        var exEndEyeDir = exEyePathRot * endEyeDir;
        var eyePathExEndPos = eyePos + modelHeight * exEndEyeDir;

        // Set pose to gaze fixation start
        timeline.GoToFrame(fixationStartFrame);
        timeline.ApplyAnimation();

        // Render scene view from the character's viewpoint
        RenderTexture.active = _rtView;
        EyeGazeCamera.targetTexture = _rtView;
        EyeGazeCamera.Render();

        // Disable rendering of the current character model
        ModelUtil.ShowModel(Model, false);

        // Render scene world positions
        _ShowModels(true);
        _ShowDome(DomeRenderMode.ShowScene, sceneScale);
        Shader.SetGlobalFloat("_RenderWorldPosScale", sceneScale);
        // Render x positions
        RenderTexture.active = _rtWorldPosX;
        EyeGazeCamera.targetTexture = _rtWorldPosX;
        Shader.SetGlobalInt("_RenderWorldPosAxis", 0);
        EyeGazeCamera.RenderWithShader(_shaderWorldPos, "");
        _texWorldPosX = _CreateTexture2D(_rtWorldPosX);
        // Render y positions
        RenderTexture.active = _rtWorldPosY;
        EyeGazeCamera.targetTexture = _rtWorldPosY;
        Shader.SetGlobalInt("_RenderWorldPosAxis", 1);
        EyeGazeCamera.RenderWithShader(_shaderWorldPos, "");
        _texWorldPosY = _CreateTexture2D(_rtWorldPosY);
        // Render z positions
        RenderTexture.active = _rtWorldPosZ;
        EyeGazeCamera.targetTexture = _rtWorldPosZ;
        Shader.SetGlobalInt("_RenderWorldPosAxis", 2);
        EyeGazeCamera.RenderWithShader(_shaderWorldPos, "");
        _texWorldPosZ = _CreateTexture2D(_rtWorldPosZ);

        // Render scene game object IDs
        _ShowModels(true);
        _ShowDome(DomeRenderMode.Disabled);
        var materials = _GetMaterialsOnModels();
        _SetMaterialOnModels(_matGameObjID);
        _SetGameObjectIDPropertyOnModels();
        RenderTexture.active = _rtGameObjID;
        EyeGazeCamera.targetTexture = _rtGameObjID;
        EyeGazeCamera.Render();
        _texGameObjID = _CreateTexture2D(_rtGameObjID);
        _SetMaterialsOnModels(materials);

        // Get gaze shift direction properties
        var lEyePos = gazeController.lEye.Top.position;
        var rEyePos = gazeController.rEye.Top.position;
        gazeController.lEye.Yaw = gazeController.lEye.Pitch =
            gazeController.rEye.Yaw = gazeController.rEye.Pitch = 0f;
        var lEyeDir0 = gazeController.lEye.Direction;
        var rEyeDir0 = gazeController.rEye.Direction;

        // Set gaze shift direction properties in the material
        _matPGazeShiftDir.SetVector("_LEyePosition", new Vector4(lEyePos.x, lEyePos.y, lEyePos.z, 1f));
        _matPGazeShiftDir.SetVector("_REyePosition", new Vector4(rEyePos.x, rEyePos.y, rEyePos.z, 1f));
        _matPGazeShiftDir.SetVector("_LEyeDirectionAhead", new Vector4(lEyeDir0.x, lEyeDir0.y, lEyeDir0.z, 0f));
        _matPGazeShiftDir.SetVector("_REyeDirectionAhead", new Vector4(rEyeDir0.x, rEyeDir0.y, rEyeDir0.z, 0f));
        _matPGazeShiftDir.SetFloat("_OMR", OMR);
        _matPGazeShiftDir.SetVector("_EyePathStartPosition", new Vector4(eyePathEndPos.x, eyePathEndPos.y, eyePathEndPos.z, 0f));
        _matPGazeShiftDir.SetVector("_EyePathEndPosition", new Vector4(eyePathExEndPos.x, eyePathExEndPos.y, eyePathExEndPos.z, 0f));

        // Render gaze shift direction probability map
        _ShowModels(false);
        _ShowDome(DomeRenderMode.ModelOnly);
        RenderTexture.active = _rtPGazeShiftDir;
        EyeGazeCamera.targetTexture = _rtPGazeShiftDir;
        EyeGazeCamera.Render();
        if (LEAPCore.writeGazeInferenceRenderTextures)
            _WriteRenderTextureToFile(_rtPGazeShiftDir, "../Matlab/EyeGazeInference/" + eyeGazeInstance.Name + "-PGazeShiftDir.png");

        // Render object task relevance probability map
        _ShowModels(true);
        _ShowDome(DomeRenderMode.Disabled);
        _SetMaterialOnModels(_matPTaskRel);
        _SetTaskRelevancePropertyOnModels();
        RenderTexture.active = _rtPTaskRel;
        EyeGazeCamera.targetTexture = _rtPTaskRel;
        EyeGazeCamera.Render();
        _SetMaterialsOnModels(materials);
        if (LEAPCore.writeGazeInferenceRenderTextures)
            _WriteRenderTextureToFile(_rtPTaskRel, "../Matlab/EyeGazeInference/" + eyeGazeInstance.Name + "-PTaskRel.png");

        // Render object hand contact probability map
        _ShowModels(true);
        _ShowDome(DomeRenderMode.Disabled);
        _SetMaterialOnModels(_matPHandCon);
        _SetHandContactWeightPropertyOnModels(baseInstance, fixationStartFrame);
        RenderTexture.active = _rtPHandCon;
        EyeGazeCamera.targetTexture = _rtPHandCon;
        EyeGazeCamera.Render();
        _SetMaterialsOnModels(materials);
        if (LEAPCore.writeGazeInferenceRenderTextures)
            _WriteRenderTextureToFile(_rtPHandCon, "../Matlab/EyeGazeInference/" + eyeGazeInstance.Name + "-PHandCon.png");

        // Normalize weights of probability terms
        float wTotal = LEAPCore.gazeInferencePGazeShiftDirWeight + LEAPCore.gazeInferencePTaskRelWeight +
            LEAPCore.gazeInferencePHandConWeight;
        float wGazeShiftDir = LEAPCore.gazeInferencePGazeShiftDirWeight / wTotal;
        float wTaskRel = LEAPCore.gazeInferencePTaskRelWeight / wTotal;
        float wHandCon = LEAPCore.gazeInferencePHandConWeight / wTotal;

        // Render total probability map
        _matPTotal.SetTexture("_TexPGazeShiftDir", _rtPGazeShiftDir);
        _matPTotal.SetTexture("_TexPTaskRel", _rtPTaskRel);
        _matPTotal.SetTexture("_TexPHandCon", _rtPHandCon);
        _matPTotal.SetFloat("_PGazeShiftDirWeight", wGazeShiftDir);
        _matPTotal.SetFloat("_PTaskRelWeight", wTaskRel);
        _matPTotal.SetFloat("_PHandConWeight", wHandCon);
        Graphics.Blit(_rtPTotal, _rtPTotal, _matPTotal);

        // Show scene as normal
        _ShowDome(DomeRenderMode.Disabled);
        _ShowModels(true);
        ModelUtil.ShowModel(Model, true);

        // Determine gaze target location
        int tx = 0, ty = 0;
        _FindMaxValueTexel(_rtPTotal, out tx, out ty);
        var targetPos = _GetWorldPositionAtTexel(tx, ty, sceneScale);
        // TODO: remove this
        var targetPosTest = _GetWorldPositionAtTexel(tx + 10, ty - 10, sceneScale);
        //
        if (LEAPCore.writeGazeInferenceRenderTextures)
        {
            // TODO: mark the target location in the textures
            _WriteRenderTextureToFile(_rtPTotal, "../Matlab/EyeGazeInference/" + eyeGazeInstance.Name + "-PTotal.png", tx, ty);
            _WriteRenderTextureToFile(_rtView, "../Matlab/EyeGazeInference/" + eyeGazeInstance.Name + ".png", tx, ty);
        }

        // Determine gaze target parent object
        var gazeTargetParentObj = _GetGameObjectAtTexel(tx, ty);
        if (gazeTargetParentObj == null)
            gazeTargetParentObj = Environment;
        var gazeTargetParent = gazeTargetParentObj.transform;
        if (gazeTargetParentObj.tag == "Agent")
            // Gaze target is on a character model, find closest bone
            gazeTargetParent = ModelUtil.FindClosestBoneToPoint(gazeTargetParentObj, targetPos);

        var gazeTarget = _FindColocatedEyeGazeTarget(gazeTargetParent, targetPos);
        if (gazeTarget == null)
        {
            // Create gaze target object
            gazeTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Component.DestroyImmediate(gazeTarget.GetComponent<SphereCollider>());
            Component.DestroyImmediate(gazeTarget.GetComponent<MeshRenderer>());
            Component.DestroyImmediate(gazeTarget.GetComponent<MeshFilter>());
            gazeTarget.transform.parent = gazeTargetParent;
            gazeTarget.transform.position = targetPos;
            gazeTarget.tag = "GazeTarget";

            // Set name for gaze target object
            string targetName = "";
            int targetIndex = 0;
            do
                targetName = gazeTargetParentObj.name + "-" + (targetIndex++);
            while (ModelUtil.FindBone(gazeTargetParentObj.transform, targetName) != null);
            gazeTarget.name = targetName;
        }

        // Destroy all those materials that got instantiated for scene objects
        Resources.UnloadUnusedAssets();

        return gazeTarget;
    }

    // Enable/disable renderers on characters and environment models
    private void _ShowModels(bool enabled = true)
    {
        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
            if (curModel != Model && curModel.active)
                ModelUtil.ShowModel(curModel, enabled);
        ModelUtil.ShowModel(Environment, enabled);
    }

    // Enable/disable the rendering of a dome around the character/scene
    private void _ShowDome(DomeRenderMode mode, float sceneScale = 200f)
    {
        var dome = GameObject.FindGameObjectWithTag("GazeTargetDome");
        if (mode == DomeRenderMode.Disabled)
        {
            dome.renderer.enabled = false;
        }
        else
        {
            dome.renderer.enabled = true;

            if (mode == DomeRenderMode.ModelOnly)
            {
                var gazeController = Model.GetComponent<GazeController>();
                float modelHeight = gazeController.head.Top.position.y;
                dome.transform.position = gazeController.EyeCenter;
                dome.transform.localScale = new Vector3(modelHeight, modelHeight, modelHeight);
            }
            else
            {
                dome.transform.position = new Vector3(Model.transform.position.x, 0f, Model.transform.position.z);
                dome.transform.localScale = new Vector3(sceneScale / 2.5f, sceneScale / 2.5f, sceneScale / 2.5f);
            }
        }
    }

    // Get materials on characters and environment models in the scene
    private Material[] _GetMaterialsOnModels()
    {
        List<Material> materials = new List<Material>();

        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
            if (curModel != Model && curModel.active)
                materials.AddRange(ModelUtil.GetModelMaterials(curModel));
        materials.AddRange(ModelUtil.GetModelMaterials(Environment));

        return materials.ToArray();
    }

    // Set specified material on all characters and environment models in the scene
    private void _SetMaterialOnModels(Material mat)
    {
        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
            if (curModel != Model && curModel.active)
                ModelUtil.SetModelMaterial(curModel, mat);
        ModelUtil.SetModelMaterial(Environment, mat);
    }

    // Set materials on characters and environment models in the scene
    private void _SetMaterialsOnModels(Material[] materials)
    {
        int matIndex = 0;

        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
        {
            if (curModel != Model && curModel.active)
            {
                var curModelMaterials = ModelUtil.GetModelMaterials(curModel);
                var newModelMaterials = new Material[curModelMaterials.Length];
                Array.Copy(materials, matIndex, newModelMaterials, 0, newModelMaterials.Length);
                ModelUtil.SetModelMaterials(curModel, newModelMaterials);
                matIndex += curModelMaterials.Length;
            }
        }

        var curEnvMaterials = ModelUtil.GetModelMaterials(Environment);
        var newEnvMaterials = new Material[curEnvMaterials.Length];
        Array.Copy(materials, matIndex, newEnvMaterials, 0, newEnvMaterials.Length);
        ModelUtil.SetModelMaterials(Environment, newEnvMaterials);
    }

    // Set game object ID material property for all characters and environment models in the scene
    private void _SetGameObjectIDPropertyOnModels()
    {
        var models = GameObject.FindGameObjectsWithTag("Agent");
        var envModels = ModelUtil.GetSubModels(Environment);
        int scale = models.Length + envModels.Length + 2;

        int modelIndex = 1;
        foreach (var curModel in models)
        {
            if (curModel != Model && curModel.active)
            {
                float id = ((float)modelIndex) / scale;
                var modelMaterials = ModelUtil.GetModelMaterials(curModel, false);
                foreach (var mat in modelMaterials)
                    mat.SetFloat("_GameObjectID", id);
                ++modelIndex;
            }
        }

        foreach (var envModel in envModels)
        {
            if (envModel.renderer != null)
            {
                float id = ((float)modelIndex) / scale;
                envModel.renderer.material.SetFloat("_GameObjectID", id);
                ++modelIndex;
            }
        }
    }

    // Set task relevance material property for all characters and environment models in the scene
    private void _SetTaskRelevancePropertyOnModels()
    {
        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
        {
            if (curModel != Model && curModel.active)
            {
                var modelMaterials = ModelUtil.GetModelMaterials(curModel, false);
                foreach (var mat in modelMaterials)
                    mat.SetInt("_IsTaskRelevant", 1);
            }
        }

        var envModels = ModelUtil.GetSubModels(Environment);
        foreach (var envModel in envModels)
        {
            if (envModel.renderer != null)
            {
                int isTaskRelevant = envModel.tag == "ManipulatedObject" || envModel.tag == "GazeTarget" ? 1 : 0;
                envModel.renderer.material.SetInt("_IsTaskRelevant", isTaskRelevant);
            }
        }
    }

    // Set hand contact weight material property for all characters and environment models in the scene
    private void _SetHandContactWeightPropertyOnModels(AnimationClipInstance baseInstance, int baseFrameIndex)
    {
        // Determine upcoming hand contacts
        var endEffectorConstraints = baseInstance.EndEffectorConstraints.GetConstraintsForEndEffector(LEAPCore.lWristTag)
            .Union(baseInstance.EndEffectorConstraints.GetConstraintsForEndEffector(LEAPCore.rWristTag));
        var handContacts = endEffectorConstraints.Where(c =>
            LEAPCore.ToTime(baseFrameIndex - c.startFrame) >= LEAPCore.gazeInferenceHandContactStartTime
            && LEAPCore.ToTime(baseFrameIndex - c.startFrame) <= LEAPCore.gazeInferenceHandContactEndTime);

        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
        {
            if (curModel != Model && curModel.active)
            {
                var endEffectorTargets = ModelUtil.GetEndEffectorTargets(curModel);

                // Compute highest hand contact weight for end effector targets on the current model
                float handContactWeight = 0f;
                foreach (var endEffectorTarget in endEffectorTargets)
                {
                    foreach (var handContact in handContacts)
                    {
                        if (handContact.target == endEffectorTarget)
                        {
                            float baseTime = LEAPCore.ToTime(baseFrameIndex);
                            float startTime = LEAPCore.ToTime(handContact.startFrame) + LEAPCore.gazeInferenceHandContactStartTime;
                            float maxTime = LEAPCore.ToTime(handContact.startFrame) + LEAPCore.gazeInferenceHandContactMaxTime;
                            float endTime = LEAPCore.ToTime(handContact.startFrame) + LEAPCore.gazeInferenceHandContactEndTime;

                            float curWeight = baseTime >= startTime && baseTime <= maxTime ?
                                (baseTime - startTime) / (maxTime - startTime) :
                                (baseTime - maxTime) / (endTime - maxTime);
                            handContactWeight = Mathf.Max(handContactWeight, curWeight);
                        }
                    }
                }

                var modelMaterials = ModelUtil.GetModelMaterials(curModel, false);
                foreach (var mat in modelMaterials)
                    mat.SetFloat("_HandContactWeight", handContactWeight);
            }
        }

        // Initialize hand contact weights on environment objects to zero
        var envEndEffectorTargets = ModelUtil.GetSubModelsWithTag(Environment, "EndEffectorTarget");
        foreach (var envEndEffectorTarget in envEndEffectorTargets)
        {
            var envModel = envEndEffectorTarget.transform.parent.gameObject;
            if (envModel.renderer != null)
                envModel.renderer.material.SetFloat("_HandContactWeight", 0f);
        }

        foreach (var envEndEffectorTarget in envEndEffectorTargets)
        {
            var envModel = envEndEffectorTarget.transform.parent.gameObject;
            if (envModel.renderer != null)
            {
                // Compute highest hand contact weight for end effector targets on the current model
                float handContactWeight = 0f;
                foreach (var handContact in handContacts)
                {
                    if (handContact.target == envEndEffectorTarget)
                    {
                        float baseTime = LEAPCore.ToTime(baseFrameIndex);
                        float startTime = LEAPCore.ToTime(handContact.startFrame) + LEAPCore.gazeInferenceHandContactStartTime;
                        float maxTime = LEAPCore.ToTime(handContact.startFrame) + LEAPCore.gazeInferenceHandContactMaxTime;
                        float endTime = LEAPCore.ToTime(handContact.startFrame) + LEAPCore.gazeInferenceHandContactEndTime;

                        float curWeight = baseTime >= startTime && baseTime <= maxTime ?
                            (baseTime - startTime) / (maxTime - startTime) :
                            (baseTime - maxTime) / (endTime - maxTime);
                        handContactWeight = Mathf.Max(handContactWeight, curWeight);
                    }
                }

                float prevHandContactWeight = envModel.renderer.material.GetFloat("_HandContactWeight");
                envModel.renderer.material.SetFloat("_HandContactWeight", Mathf.Max(handContactWeight, prevHandContactWeight));
            }
        }
    }

    // Find texel location with the highest value in the active render texture
    private void _FindMaxValueTexel(RenderTexture rt, out int txMax, out int tyMax)
    {
        int width = rt.width;
        int height = rt.height;
        RenderTexture.active = rt;
        _texWriteToFile.ReadPixels(new Rect(0, 0, width, height), 0, 0);

        txMax = tyMax = 0;
        float maxVal = 0;
        for (int tx = 0; tx < width; ++tx)
        {
            for (int ty = 0; ty < height; ++ty)
            {
                float val = _texWriteToFile.GetPixel(tx, ty).r;
                if (val > maxVal)
                {
                    maxVal = val;
                    txMax = tx;
                    tyMax = ty;
                }
            }
        }
    }

    // Sample the world positions textures
    private Vector3 _GetWorldPositionAtTexel(int tx, int ty, float sceneScale)
    {
        Vector3 worldPos;

        // Sample world position
        worldPos.x = _GetWorldPositionAtTexel(_texWorldPosX, tx, ty, sceneScale);
        worldPos.y = _GetWorldPositionAtTexel(_texWorldPosY, tx, ty, sceneScale);
        worldPos.z = _GetWorldPositionAtTexel(_texWorldPosZ, tx, ty, sceneScale);

        return worldPos;
    }

    // Sample a world positions texture
    private float _GetWorldPositionAtTexel(Texture2D tex, int tx, int ty, float sceneScale)
    {
        float v = _DecodeFloatRGBA(tex.GetPixel(tx, ty));
        v = (v - 0.5f) * sceneScale;
        return v;
    }

    // Decode a 32-bit floating-point value in 0-1 range from an 8 bit/channel RGBA color value
    private float _DecodeFloatRGBA(Color rgba)
    {
        Vector4 dec = new Vector4(1f, 1f / 255f, 1f / 65025f, 1f / 16581375f);
        return dec.x * rgba.r + dec.y * rgba.g + dec.z * rgba.b + dec.w * rgba.a;
    }

    // Get game object by sampling the game object ID texture
    private GameObject _GetGameObjectAtTexel(int tx, int ty)
    {
        // Sample game object ID
        int width = _rtGameObjID.width;
        int height = _rtGameObjID.height;
        RenderTexture.active = _rtGameObjID;
        float id = _DecodeFloatRGBA(_texGameObjID.GetPixel(tx, ty));

        // Determine model index from the sampled ID
        var models = GameObject.FindGameObjectsWithTag("Agent");
        var envModels = ModelUtil.GetSubModels(Environment);
        int scale = models.Length + envModels.Length + 2;
        int modelIndexAtTexel = Mathf.RoundToInt(scale * id);

        int modelIndex = 1;
        foreach (var curModel in models)
        {
            if (curModel != Model && curModel.active)
            {
                if (modelIndex == modelIndexAtTexel)
                    return curModel;

                ++modelIndex;
            }
        }

        foreach (var envModel in envModels)
        {
            if (envModel.renderer != null)
            {
                if (modelIndex == modelIndexAtTexel)
                    return envModel;

                ++modelIndex;
            }
        }

        return null;
    }

    // Find gaze target that is at approximately the same location as specified
    private GameObject _FindColocatedEyeGazeTarget(Transform targetParent, Vector3 targetPos)
    {
        for (int childIndex = 0; childIndex < targetParent.childCount; ++childIndex)
        {
            var child = targetParent.GetChild(childIndex);
            if (child.tag == "GazeTarget" &&
                Vector3.Distance(targetPos, child.position) < LEAPCore.gazeInferenceMaxColocatedTargetDistance)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    // Write render texture to a PNG file
    private void _WriteRenderTextureToFile(RenderTexture rt, string path, int markerPosX = -1, int markerPosY = -1)
    {
        int width = rt.width;
        int height = rt.height;

        RenderTexture.active = rt;
        _texWriteToFile.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        if (markerPosX >= 0 && markerPosY >= 0)
        {
            _texWriteToFile.SetPixel(markerPosX, markerPosY, Color.red);
            _texWriteToFile.SetPixel(markerPosX - 1, markerPosY, Color.red);
            _texWriteToFile.SetPixel(markerPosX + 1, markerPosY, Color.red);
            _texWriteToFile.SetPixel(markerPosX, markerPosY - 1, Color.red);
            _texWriteToFile.SetPixel(markerPosX, markerPosY + 1, Color.red);
            _texWriteToFile.SetPixel(markerPosX - 1, markerPosY - 1, Color.red);
            _texWriteToFile.SetPixel(markerPosX + 1, markerPosY + 1, Color.red);
            _texWriteToFile.SetPixel(markerPosX + 1, markerPosY - 1, Color.red);
            _texWriteToFile.SetPixel(markerPosX - 1, markerPosY + 1, Color.red);
        }
        var texData = _texWriteToFile.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, texData);
    }

    // Infer gaze target location for looking straight ahead
    private Vector3 _InferEyeGazeTargetAhead(AnimationTimeline timeline, int baseAnimationInstanceId,
        int eyeGazeInstanceId)
    {
        var eyeGazeInstance = timeline.GetAnimation(eyeGazeInstanceId) as EyeGazeInstance;
        var gazeController = eyeGazeInstance.GazeController;
        int startFrame = timeline.GetAnimationStartFrame(eyeGazeInstanceId);
        int fixationStartFrame = startFrame + eyeGazeInstance.FixationStartFrame;

        // Set pose to gaze fixation start
        timeline.GoToFrame(fixationStartFrame);
        timeline.ApplyAnimation();

        // Compute target position for gaze shift straight ahead
        Vector3 aheadTargetPos = gazeController.head.Position + 5f * gazeController.head.Direction;

        return aheadTargetPos;
    }

    // Infer most likely gaze shift location using simple raycast
    private GameObject _InferEyeGazeTargetSimple(AnimationTimeline timeline, int baseAnimationInstanceId,
        int eyeGazeInstanceId)
    {
        var baseInstance = timeline.GetAnimation(baseAnimationInstanceId) as AnimationClipInstance;
        var model = baseInstance.Model;
        var bones = ModelUtil.GetAllBones(model);
        var gazeController = model.GetComponent<GazeController>();
        var eyeGazeInstance = timeline.GetAnimation(eyeGazeInstanceId) as EyeGazeInstance;
        int startFrame = timeline.GetAnimationStartFrame(eyeGazeInstanceId);
        int fixationStartFrame = startFrame + eyeGazeInstance.FixationStartFrame;

        // Set pose to gaze fixation start
        timeline.GoToFrame(fixationStartFrame);
        timeline.ApplyAnimation();

        GameObject targetParent = null;
        Vector3 targetPosition = Vector3.zero;
        var gazeDirection = new Ray(gazeController.EyeCenter, gazeController.head.Direction);
        var curGazeTargets = ModelUtil.GetSubModelsWithTag(Environment, "GazeTarget");
        var newGazeTargets = new List<GameObject>();

        RaycastHit hitInfo;
        if (Physics.Raycast(gazeDirection, out hitInfo, 50f))
        {
            targetParent = hitInfo.collider.gameObject;
            targetPosition = hitInfo.point;
        }
        else
        {
            targetParent = Environment;
            targetPosition = gazeDirection.origin + 5f * gazeDirection.direction;
        }

        // Get/create gaze target object
        GameObject gazeTarget = null;
        var curGazeTarget = curGazeTargets.FirstOrDefault(gt => (gt.transform.position - targetPosition).magnitude < 0.35f);
        if (curGazeTarget != null)
        {
            gazeTarget = curGazeTarget.gameObject;
        }
        else
        {
            // Generate gaze target name
            string gazeTargetName = "";
            int gazeTargetIndex = 1;
            do
            {
                gazeTargetName = targetParent.name + gazeTargetIndex;
                ++gazeTargetIndex;
            }
            while (curGazeTargets.Any(gt => gt.name == gazeTargetName) || newGazeTargets.Any(gt => gt.name == gazeTargetName));

            gazeTarget = new GameObject(gazeTargetName);
            gazeTarget.tag = "GazeTarget";
            gazeTarget.transform.position = targetPosition;
            gazeTarget.transform.SetParent(targetParent.transform, true);
            newGazeTargets.Add(gazeTarget);
        }

        return gazeTarget;
    }

    // Get eye gaze inference camera
    private Camera _GetEyeGazeCamera()
    {
        var gazeController = Model.GetComponent<GazeController>();
        var headBone = gazeController.head.Top;
        for (int childIndex = 0; childIndex < headBone.childCount; ++childIndex)
        {
            var child = headBone.GetChild(childIndex);
            if (child.gameObject.camera != null)
            {
                return child.gameObject.camera;
            }
        }

        return null;
    }

    // Create a render texture used in gaze target inference
    private RenderTexture _CreateRenderTexture(RenderTextureFormat format)
    {
        var tex = new RenderTexture(CameraWidth, CameraHeight, 24, format);
        tex.antiAliasing = 1;
        tex.filterMode = FilterMode.Point;

        return tex;
    }

    // Create a 2D texture used for visualizing the results of gaze target inference
    private Texture2D _CreateTexture2D(RenderTexture rt = null)
    {
        var tex = new Texture2D(CameraWidth, CameraHeight, TextureFormat.ARGB32, false);
        if (rt != null)
        {
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, CameraWidth, CameraHeight), 0, 0);
        }

        return tex;
    }
}
