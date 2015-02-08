using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

/// <summary>
/// This class has static methods for performing eye gaze editing operations.
/// </summary>
public static class EyeGazeEditor
{
    /// <summary>
    /// Gaze editing operation. Add an eye gaze instance to the animation timeline.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="newInstance">New eye gaze instance</param>
    /// <param name="newStartFrame">New eye gaze instance start frame</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    public static void AddEyeGaze(AnimationTimeline timeline, EyeGazeInstance newInstance,
        int newStartFrame, string layerName = "Gaze")
    {
        // Search the timeline for any overlapping instances:
        // 1) If the overlapping instance starts before the new instance:
        //   a) Shorten the overlapping one to end just before the new one, but only if the shortened instance is still longer than 0.5s (1s for gaze-back)
        //        If the shortened instance is followed by a gaze-back instance, remove the gaze-back instance
        //   b) Otherwise remove the overlapping instance
        //        If the removed instance was a gaze-back instance, extend the previous instance to end just before the new one
        // 2) If the overlapping instance starts at or after the new instance:
        //   a) Delay the start of the overlapping one, but only if the shortened instance is still longer than 0.5s  (1s for gaze-back)
        //   b) Otherwise remove the overlapping instance
        // Find the next instance after the new one:
        // 1) If the next instance starts less than 0.5s before the new one ends, extend the new one to end just before the next one
        // 2) Otherwise, insert a gaze-back instance between the new and next instances
        // Finally, add the new instance

        // Trim or remove overlapping eye gaze instances
        int newEndFrame = newStartFrame + newInstance.FrameLength - 1;
        List<AnimationTimeline.ScheduledInstance> overlappingInstances = timeline.GetLayer(layerName).Animations.Where(inst =>
            !(inst.StartFrame + inst.Animation.FrameLength - 1 < newStartFrame || newEndFrame < inst.StartFrame) &&
            inst.Animation.Model.gameObject == newInstance.Model.gameObject).ToList();
        foreach (var overlappingInstance in overlappingInstances)
        {
            if (timeline.GetAnimation(overlappingInstance.InstanceId) == null)
            {
                // Overlapping instance was a gaze-back that was removed on a previous iteration
                continue;
            }
            
            int overlappingStartFrame = overlappingInstance.StartFrame;
            int overlappingEndFrame = overlappingInstance.StartFrame + overlappingInstance.Animation.FrameLength - 1;
            bool overlappingIsGazeBack = overlappingInstance.Animation.AnimationClip.name.EndsWith(LEAPCore.gazeBackSuffix);
            int overlappingMinLength = overlappingIsGazeBack ? Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate) :
                Mathf.RoundToInt(LEAPCore.minEyeGazeLength * LEAPCore.editFrameRate);

            if (overlappingStartFrame < newStartFrame)
            {
                if (newStartFrame - overlappingStartFrame >= overlappingMinLength)
                {
                    (overlappingInstance.Animation as EyeGazeInstance).SetFrameLength(newStartFrame - overlappingStartFrame);

                    if (!overlappingIsGazeBack)
                    {
                        var gazeBackOverlappingInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(
                            inst => inst.Animation.AnimationClip.name == overlappingInstance.Animation.AnimationClip.name + LEAPCore.gazeBackSuffix);
                        if (gazeBackOverlappingInstance != null)
                            timeline.RemoveAnimation(gazeBackOverlappingInstance.InstanceId);
                    }
                }
                else
                {
                    if (!overlappingIsGazeBack)
                    {
                        RemoveEyeGaze(timeline, overlappingInstance.InstanceId);
                    }
                    else
                    {
                        string prevOverlappingName = overlappingInstance.Animation.AnimationClip.name;
                        prevOverlappingName = prevOverlappingName.Remove(
                            prevOverlappingName.Length - LEAPCore.gazeBackSuffix.Length, LEAPCore.gazeBackSuffix.Length);
                        var prevOverlappingInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(
                            inst => inst.Animation.AnimationClip.name == prevOverlappingName);

                        timeline.RemoveAnimation(overlappingInstance.InstanceId);
                        if (prevOverlappingInstance != null)
                            (prevOverlappingInstance.Animation as EyeGazeInstance).SetFrameLength(newStartFrame - prevOverlappingInstance.StartFrame);
                    }
                }
            }
            else
            {
                if (overlappingEndFrame - newEndFrame >= overlappingMinLength)
                {
                    overlappingInstance._SetStartFrame(newEndFrame + 1);
                }
                else
                {
                    RemoveEyeGaze(timeline, overlappingInstance.InstanceId);
                }
            }
        }

        // Fill the gap to the next eye gaze instance
        var nextInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst => inst.StartFrame > newEndFrame &&
            inst.Animation.Model.gameObject == newInstance.Model.gameObject);
        if (nextInstance != null)
        {
            int nextStartFrame = nextInstance.StartFrame;

            if (nextStartFrame - newEndFrame - 1 < Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate))
            {
                newInstance.SetFrameLength(nextStartFrame - newStartFrame);
                newEndFrame = nextStartFrame - newStartFrame;
            }
            else
            {
                var gazeBackInstance = new EyeGazeInstance(newInstance.Model.gameObject, newInstance.AnimationClip.name + LEAPCore.gazeBackSuffix,
                    Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate), null);
                timeline.AddAnimation(layerName, gazeBackInstance, newEndFrame + 1);
            }
        }
        else if (timeline.FrameLength - newEndFrame - 1 >= Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate))
        {
            // No follow-up gaze instance, but there is more animation left on the timeline, so add a gaze-back animation
            var gazeBackInstance = new EyeGazeInstance(
                newInstance.Model.gameObject, newInstance.AnimationClip.name + LEAPCore.gazeBackSuffix,
                    Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate), null);
            timeline.AddAnimation(layerName, gazeBackInstance, newEndFrame + 1);
        }
        else
        {
            // No follow-up gaze instance, extend current gaze instance to the end of the animation timeline
            newInstance.SetFrameLength(timeline.FrameLength - newStartFrame + 1);
        }

        // Fill the gap to the previous eye gaze instance
        var prevInstance = timeline.GetLayer(layerName).Animations.LastOrDefault(
            inst => inst.StartFrame + inst.Animation.FrameLength - 1 < newStartFrame &&
                inst.Animation.Model.gameObject == newInstance.Model.gameObject);
        if (prevInstance != null && !prevInstance.Animation.AnimationClip.name.EndsWith(LEAPCore.gazeBackSuffix))
        {
            int prevStartFrame = prevInstance.StartFrame;
            int prevEndFrame = prevInstance.StartFrame + prevInstance.Animation.FrameLength - 1;

            if (newStartFrame - prevEndFrame - 1 < Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate))
            {
                (prevInstance.Animation as EyeGazeInstance).SetFrameLength(newStartFrame - prevStartFrame);
            }
        }

        // Add the new eye gaze instance
        timeline.AddAnimation(layerName, newInstance, newStartFrame);
    }

    /// <summary>
    /// Gaze editing operation. Remove an eye gaze instance from the animation timeline.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="instanceId">Eye gaze instance ID</param>
    public static void RemoveEyeGaze(AnimationTimeline timeline, int instanceId)
    {
        // First remove the follow-up gaze-back instance, if one exists
        string layerName = timeline.GetLayerForAnimation(instanceId).LayerName;
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        var gazeBackInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst =>
            inst.Animation.AnimationClip.name == instance.AnimationClip.name + LEAPCore.gazeBackSuffix);
        if (gazeBackInstance != null)
            timeline.RemoveAnimation(gazeBackInstance.InstanceId);

        // Then remove the actual instnace
        timeline.RemoveAnimation(instanceId);

        // TODO: this will actually require adding a "negative" displacement map
        // which also removes the gaze component from the underlying  body motion
    }

    /// <summary>
    /// Gaze editing operation. Set new start and end times for the eye gaze instance.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="instanceId">Eye gaze instance ID</param>
    /// <param name="startFrame">New start frame for the eye gaze instance</param>
    /// <param name="endFrame">New end frame for the eye gaze instance</param>
    public static void SetEyeGazeTiming(AnimationTimeline timeline, int instanceId,
        int startFrame, int endFrame)
    {
        if (endFrame - startFrame + 1 < Mathf.RoundToInt(LEAPCore.minEyeGazeLength * LEAPCore.editFrameRate))
        {
            throw new Exception(string.Format("Error setting eye gaze timing! Eye gaze instance must be longer than {0}s", LEAPCore.minEyeGazeLength));
        }

        // Changing the timing of an instance is equivalent to removing the instance and then re-adding it with new times
        string layerName = timeline.GetLayerForAnimation(instanceId).LayerName;
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        RemoveEyeGaze(timeline, instanceId);
        instance.SetFrameLength(endFrame - startFrame + 1);
        AddEyeGaze(timeline, instance, startFrame, layerName);
    }

    /// <summary>
    /// Gaze editing operation. Set new head and torso alignment values for the eye gaze instance.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="instanceId">Eye gaze instance ID</param>
    /// <param name="headAlign">New head alignment value for the eye gaze instance</param>
    /// <param name="torsoAlign">New torso alignment value for the eye gaze instance</param>
    public static void SetEyeGazeAlignments(AnimationTimeline timeline, int instanceId,
        float headAlign, float torsoAlign)
    {
        headAlign = Mathf.Clamp01(headAlign);
        torsoAlign = Mathf.Clamp01(torsoAlign);
        
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        instance.HeadAlign = headAlign;
        instance.TorsoAlign = torsoAlign;
    }

    /// <summary>
    /// Load eye gaze behavior specification for the specified base animation.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationName">Base animation name</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <returns>true if eye gaze instances were loaded successfully, false otherwise</returns>
    public static bool LoadEyeGazeForModel(AnimationTimeline timeline, string baseAnimationName, string layerName = "Gaze")
    {
        // Get gaze behavior file path
        string path = Application.dataPath + LEAPCore.eyeGazeDirectory.Substring(LEAPCore.eyeGazeDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimationName + ".csv");

        // Load gaze behaviors
        try
        {
            var reader = new StreamReader(path);
            bool firstLine = true;
            string line = "";
            string[] lineElements = null;
            Dictionary<string, int> attributeIndices = new Dictionary<string, int>();
            ModelController[] models = timeline.GetAllModels();
            GameObject[] gazeTargets = GameObject.FindGameObjectsWithTag("GazeTarget");
            while (!reader.EndOfStream && (line = reader.ReadLine()) != "")
            {
                if (line[0] == '#')
                {
                    // Comment line, skip
                    continue;
                }
                else if (firstLine)
                {
                    // Load attribute names from first line
                    firstLine = false;
                    lineElements = line.Split(",".ToCharArray());
                    for (int attributeIndex = 0; attributeIndex < lineElements.Length; ++attributeIndex)
                    {
                        attributeIndices[lineElements[attributeIndex]] = attributeIndex;
                    }
                }
                else
                {
                    // Load gaze shift specification
                    lineElements = line.Split(",".ToCharArray());

                    // Get gaze shift attributes
                    string characterName = lineElements[attributeIndices["Character"]];
                    ModelController modelController = models.FirstOrDefault(m => m.gameObject.name == characterName);
                    if (modelController == null)
                    {
                        UnityEngine.Debug.LogWarning(string.Format(
                            "Unable to load eye gaze shift for non-existent model {0}", characterName));
                        continue;
                    }
                    string animationClipName = lineElements[attributeIndices["AnimationClip"]];
                    int startFrame = int.Parse(lineElements[attributeIndices["StartFrame"]]);
                    int frameLength = int.Parse(lineElements[attributeIndices["EndFrame"]]) - startFrame + 1;
                    string gazeTargetName = lineElements[attributeIndices["Target"]];
                    GameObject gazeTarget = null;
                    if (gazeTargets != null)
                        gazeTarget = gazeTargets.FirstOrDefault(obj => obj.name == gazeTargetName);
                    if (gazeTarget == null)
                        UnityEngine.Debug.LogWarning(string.Format(
                            "Trying to create EyeGazeInstance towards target {0} on model {1}, but the target does not exist!",
                            gazeTargetName, modelController.gameObject.name));
                    float headAlign = (float)double.Parse(lineElements[attributeIndices["HeadAlign"]]);
                    float torsoAlign = (float)double.Parse(lineElements[attributeIndices["TorsoAlign"]]);

                    // Create and schedule gaze instance
                    var gazeInstance = new EyeGazeInstance(modelController.gameObject,
                        animationClipName, frameLength, gazeTarget, headAlign, torsoAlign);
                    AddEyeGaze(timeline, gazeInstance, startFrame, layerName);
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load eye gaze from asset file {0}: {1}", path, ex.Message));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Extend eye gaze instances such that gaze remains fixed to the target until
    /// the next eye gaze instance. That way, gaze behavior is always constrained by
    /// artist annotations.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationName">Base animation name</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    public static void FixEyeGazeBetweenShifts(AnimationTimeline timeline, string layerName = "Gaze")
    {
        bool timelineActive = timeline.Active;
        timeline.Active = true;
        var gazeLayer = timeline.GetLayer(layerName);
        EyeGazeInstance prevInstance = null;
        int prevStartFrame = 0;
        foreach (var curScheduledInstance in gazeLayer.Animations)
        {
            if (!(curScheduledInstance.Animation is EyeGazeInstance))
                continue;

            var curInstance = curScheduledInstance.Animation as EyeGazeInstance;
            if (prevInstance != null)
            {
                prevInstance.SetFrameLength(curScheduledInstance.StartFrame - prevStartFrame);
            }

            prevInstance = curInstance;
            prevStartFrame = curScheduledInstance.StartFrame;
        }

        if (prevInstance != null)
        {
            prevInstance.SetFrameLength(timeline.FrameLength - prevStartFrame);
        }
        timeline.Active = timelineActive;
    }

    /// <summary>
    /// Infer eye gaze target and alignment parameters for currently defined
    /// eye gaze instances on the character model.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationName">Base animation name</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <param name="createTargets">If gaze target could not be found for a gaze shift,
    /// a "dummy" gaze target object will be created in the scene</param>
    public static void InferEyeGazeAttributes(AnimationTimeline timeline, string baseAnimationName, string layerName = "Gaze",
        bool createTargets = true)
    {
        bool timelineActive = timeline.Active;
        timeline.Active = true;
        var gazeLayer = timeline.GetLayer(layerName);
        foreach (var scheduledInstance in gazeLayer.Animations)
        {
            if (!(scheduledInstance.Animation is EyeGazeInstance))
                continue;
            var instance = scheduledInstance.Animation as EyeGazeInstance;

            // Get gaze shift start and end frames
            int startFrame = scheduledInstance.StartFrame;
            int endFrame = scheduledInstance.StartFrame + instance.FrameLength;

            /*// Apply the animation at the end of the gaze shift
            timeline.GoToFrame(endFrame);
            timeline.Update(0);

            // Infer gaze target
            if (instance.Target == null && createTargets)
                _InferEyeGazeTarget(instance);

            // Infer head and torso alignments
            _InferEyeGazeAlignments(timeline, scheduledInstance.InstanceId);*/
            // TODO: fix problems with eye gaze attribute inference
            if (instance.Target != null)
            {
                instance.HeadAlign = 0f;
                instance.TorsoAlign = 0f;
            }
            else
            {
                instance.HeadAlign = 1f;
                instance.TorsoAlign = 1f;
            }
            //
        }
        timeline.Active = timelineActive;
    }

    private static void _InferEyeGazeTarget(EyeGazeInstance instance)
    {
        var gazeController = instance.GazeController;
        Vector3 headGazeDir = gazeController.Head.Direction;
        Collider[] colliders = UnityEngine.Object.FindObjectsOfType<Collider>();
        GameObject[] targets = GameObject.FindGameObjectsWithTag("GazeTarget");

        // Find object that is closest to current head gaze direction
        GameObject closestObject = null;
        Vector3 closestPoint = Vector3.zero;
        float closestPointDistance = float.MaxValue;
        foreach (var collider in colliders)
        {
            Vector3 point;
            float pointDistance;
            _FindClosestPointToRay(collider, new Ray(gazeController.Head.bone.position, headGazeDir), out point, out pointDistance);

            if (pointDistance < closestPointDistance && pointDistance < 0.2f)
            {
                closestObject = collider.gameObject;
                closestPoint = point;
                closestPointDistance = pointDistance;
            }
        }

        // If the new candidate target position is close to an existing gaze target,
        // use the existing gaze target
        foreach (var existingTarget in targets)
        {
            if (Vector3.Distance(existingTarget.transform.position, closestPoint) < 0.2f)
            {
                instance.Target = existingTarget;
                return;
            }
        }

        // Determine name for the new gaze target
        int targetIndex = 0;
        while (targets.Any(obj => obj.name == ("GazeTarget" + targetIndex.ToString())))
            ++targetIndex;
        string targetName = "GazeTarget" + targetIndex.ToString();

        // Create target object and assign it to the instance
        var target = new GameObject(targetName);
        target.tag = "GazeTarget";
        instance.Target = target;

        if (closestObject != null)
        {
            // Position the target at the closest point of the object closest to the head gaze direction
            target.transform.parent = closestObject.transform;
            target.transform.position = closestPoint;

            // Determine name for the new gaze target
            targetIndex = 0;
            while (targets.Any(obj => obj.name == (closestObject.name + targetIndex.ToString())))
                ++targetIndex;
            target.name = closestObject.name + targetIndex.ToString();
        }
        else
        {
            // No object is close to the head gaze direction - place it somewhere in front of the character
            target.transform.position = gazeController.EyeCenter + 1.5f * gazeController.Head.Direction;
        }
    }

    private static void _InferEyeGazeAlignments(AnimationTimeline timeline, int instanceId)
    {
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        int startFrame = timeline.GetAnimationStartFrame(instanceId);
        int endFrame = startFrame + instance.FrameLength;
        var gazeController = instance.GazeController;

        // Apply animation at the start of the gaze shift
        timeline.GoToFrame(startFrame);
        timeline.Update(0);

        // TODO: this implementation does not work correctly for stylized gaze
        gazeController.stylizeGaze = false;
        //

        // Set source rotations, alignments, and gaze target
        gazeController._CurrentGazeTarget = instance.Target;
        gazeController._InitGazeParams();
        foreach (var joint in gazeController.gazeJoints)
        {
            joint.srcRot = joint.bone.localRotation;
        }
        gazeController._CurrentGazeTarget = instance.Target;

        // Apply animation at the end of the gaze shift
        timeline.GoToFrame(endFrame);
        timeline.Update(0);

        // Get target rotations of the torso and head
        Quaternion torsoTrgRot = gazeController.Torso != null ? gazeController.Torso.bone.rotation : Quaternion.identity;
        Quaternion headTrgRot = gazeController.Head.bone.rotation;

        // Set gaze joint rotations back to source rotations
        gazeController.ApplySourcePose();

        if (gazeController.Torso != null)
        {
            // Get min. and full target rotations for the torso
            Quaternion torsoTrgRotFull = gazeController.Torso._ComputeTargetRotation(gazeController.EffGazeTargetPosition);
            float distRot = gazeController._ComputeGazeShiftAmplitude();
            gazeController.Amplitude = distRot;
            float torsoDistRotFull = GazeJoint.DistanceToRotate(gazeController.Torso.srcRot, torsoTrgRotFull);
            float torsoDistRotMin = instance.GazeController._ComputeTorsoDistanceToRotate();
            float alignMin = gazeController.Torso.srcRot != torsoTrgRot ? torsoDistRotMin / torsoDistRotFull : 0f;
            Quaternion torsoTrgRotMin = Quaternion.Slerp(gazeController.Torso.srcRot, torsoTrgRot, alignMin);
            
            // Compute torso alignment
            instance.TorsoAlign = torsoTrgRotMin != torsoTrgRotFull ?
                GazeJoint.DistanceToRotate(torsoTrgRotMin, torsoTrgRot) / GazeJoint.DistanceToRotate(torsoTrgRotMin, torsoTrgRotFull) : 0f;
            instance.TorsoAlign = Mathf.Clamp01(instance.TorsoAlign);
            gazeController.Torso.align = instance.TorsoAlign;
        }

        // Compute minimal head target rotation
        gazeController.Head.align = 0f;
        gazeController._InitTargetRotations();
        Quaternion headTrgRotMin = gazeController.Head.trgRotAlign;
        Quaternion headTrgRotFull = gazeController.Head._ComputeTargetRotation(gazeController.EffGazeTargetPosition);

        // Compute head alignment
        instance.HeadAlign = headTrgRotMin != headTrgRotFull ?
            GazeJoint.DistanceToRotate(headTrgRotMin, headTrgRot) / GazeJoint.DistanceToRotate(headTrgRotMin, headTrgRotFull) : 0f;
        instance.HeadAlign = Mathf.Clamp01(instance.HeadAlign);
        instance.HeadAlign = 0f;
        gazeController.Head.align = instance.HeadAlign;
    }

    private static void _FindClosestPointToRay(Collider collider, Ray ray, out Vector3 closestPoint, out float closestPointDistance)
    {
        closestPoint = Vector3.zero;
        closestPointDistance = 0f;

        // TODO: implement this properly
        closestPoint = ray.GetPoint(1.5f);
        closestPointDistance = 1.5f;
    }
}

