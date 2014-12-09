using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Class specifying a controller for a facing-directable joint.
/// </summary>
[Serializable]
public class DirectableJoint
{
    /// <summary>
    /// Bone that corresponds to this joint.
    /// </summary>
    public Transform bone = null;

    /// <summary>
    /// Helper bone for determining facing direction.
    /// </summary>
    public Transform helper = null;

    /// <summary>
    /// Peak velocity of the joint.
    /// </summary>
    public float velocity = 50f; // 150 for the eyes

    /// <summary>
    /// Joint's upward motor range (in degs). 
    /// </summary>
    public float upMR = 90f; // 40 for the eyes

    /// <summary>
    /// Joint's downward motor range (in degs). 
    /// </summary>
    public float downMR = 90f;

    /// <summary>
    /// Joint's inward motor range (in degs).
    /// </summary>
    /// <remarks>"Inward" direction is right for all joints
    /// but the right eye (in which case it is left).</remarks>
    public float inMR = 180f; // 55 for the eyes

    /// <summary>
    /// Joint's outward motor range (in degs). 
    /// </summary>
    /// <remarks>"Outward" direction is left for all joints
    /// but the left eye (in which case it is right).</remarks>
    public float outMR = 180f;

    //protected GameObject agent; // This is the agent root!
    protected Transform rootBone = null;
    protected Quaternion COBRot;
    protected ModelController mdlCtrl;

    /// <summary>
    /// Joint pitch. 
    /// </summary>
    public virtual float Pitch
    {
        get
        {
            Quaternion rot = bone.localRotation * COBRot;
            rot = Quaternion.Inverse(InitRotation * COBRot) * rot;

            return ClampAngle(rot.eulerAngles.x);
        }
        set
        {
            Quaternion rot = bone.localRotation * COBRot;
            Quaternion li_rot = InitRotation * COBRot;
            rot = Quaternion.Inverse(li_rot) * rot;
            rot = Quaternion.Euler(value, rot.eulerAngles.y, rot.eulerAngles.z);

            rot = li_rot * rot;
            rot = rot * Quaternion.Inverse(COBRot);
            bone.localRotation = rot;
        }
    }

    /// <summary>
    /// Joint yaw. 
    /// </summary>
    public virtual float Yaw
    {
        get
        {
            Quaternion rot = bone.localRotation * COBRot;
            rot = Quaternion.Inverse(InitRotation * COBRot) * rot;

            return ClampAngle(-rot.eulerAngles.y);
        }
        set
        {
            Quaternion rot = bone.localRotation * COBRot;
            Quaternion li_rot = InitRotation * COBRot;
            rot = Quaternion.Inverse(li_rot) * rot;
            rot = Quaternion.Euler(rot.eulerAngles.x, -value, rot.eulerAngles.z);

            rot = li_rot * rot;
            rot = rot * Quaternion.Inverse(COBRot);
            bone.localRotation = rot;
        }
    }

    /// <summary>
    /// Joint roll. 
    /// </summary>
    public virtual float Roll
    {
        get
        {
            Quaternion rot = bone.localRotation * COBRot;
            rot = Quaternion.Inverse(InitRotation * COBRot) * rot;

            return ClampAngle(-rot.eulerAngles.z);
        }
        set
        {
            Quaternion rot = bone.localRotation * COBRot;
            Quaternion li_rot = InitRotation * COBRot;
            rot = Quaternion.Inverse(li_rot) * rot;
            rot = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, value);

            rot = li_rot * rot;
            rot = rot * Quaternion.Inverse(COBRot);
            bone.localRotation = rot;
        }
    }

    /// <summary>
    /// Initial joint pitch. 
    /// </summary>
    public virtual float InitPitch
    {
        get
        {
            Quaternion currot = bone.localRotation;
            bone.localRotation = InitRotation;
            float pitch = Pitch;
            bone.localRotation = currot;

            return pitch;
        }
    }

    /// <summary>
    /// Initial joint yaw. 
    /// </summary>
    public virtual float InitYaw
    {
        get
        {
            Quaternion currot = bone.localRotation;
            bone.localRotation = InitRotation;
            float yaw = Yaw;
            bone.localRotation = currot;

            return yaw;
        }
    }

    /// <summary>
    /// Initial joint roll. 
    /// </summary>
    public virtual float InitRoll
    {
        get
        {
            Quaternion currot = bone.localRotation;
            bone.localRotation = InitRotation;
            float roll = Roll;
            bone.localRotation = currot;

            return roll;
        }
    }

    /// <summary>
    /// Initial joint position.
    /// </summary>
    public virtual Vector3 InitPosition
    {
        get
        {
            return mdlCtrl.GetInitPosition(bone);
        }
    }

    /// <summary>
    /// Initial joint rotation.
    /// </summary>
    public virtual Quaternion InitRotation
    {
        get
        {
            return mdlCtrl.GetInitRotation(bone);
        }
    }

    /// <summary>
    /// Change in joint position relative to initial position,
    /// after all animation has been applied.
    /// </summary>
    public virtual Vector3 PositionOffset
    {
        get
        {
            return mdlCtrl.GetPrevPosition(bone) - InitPosition;
        }
    }

    /// <summary>
    /// Change in joint rotation relative to initial rotation,
    /// after all animation has been applied.
    /// </summary>
    public virtual Quaternion RotationOffset
    {
        get
        {
            return Quaternion.Inverse(InitRotation) * mdlCtrl.GetPrevRotation(bone);
        }
    }

    /// <summary>
    /// Facing direction. 
    /// </summary>
    public virtual Vector3 Direction
    {
        get
        {
            return helper != null ? (helper.position - bone.position).normalized : bone.forward;
        }
        set
        {
            Quaternion rot = Quaternion.FromToRotation(Direction, value);
            bone.rotation *= rot;
        }
    }

    /// <summary>
    /// Initializes the body joint.
    /// </summary>
    /// <param name="agent">
    /// Virtual agent.
    /// </param>
    public virtual void Init(GameObject agent)
    {
        rootBone = agent.transform;
        mdlCtrl = agent.GetComponent<ModelController>();

        if (!helper)
        {
            Debug.LogWarning(string.Format("Joint {0} has no helper bone defined.",
                bone.name));
            return;
        }

        // Cache change-of-basis rotation
        COBRot = _ComputeCOBRotation();
    }

    /// <summary>
    /// true if MR limits are broken, otherwise false.
    /// </summary>
    public virtual bool CheckMR()
    {
        float yaw = this.bone == mdlCtrl.REye ? (Yaw - InitYaw) : (-Yaw + InitYaw);
        float pitch = Pitch - InitPitch;

        bool res = false;
        if (yaw >= 0f && pitch >= 0f)
            res = yaw <= inMR && pitch <= downMR;
        else if (yaw <= 0f && pitch >= 0f)
            res = -yaw <= outMR && pitch <= downMR;
        else if (yaw <= 0f && pitch <= 0f)
            res = -yaw <= outMR && -pitch <= upMR;
        else if (yaw >= 0f && pitch <= 0f)
            res = yaw <= inMR && -pitch <= upMR;

        return !res;
    }

    /// <summary>
    /// Clamp joint orientation to MR limits. 
    /// </summary>
    public virtual bool ClampMR()
    {
        Quaternion srcrot = InitRotation;
        Quaternion trgrot = bone.localRotation;
        float rotd = DistanceToRotate(srcrot, trgrot);
        bone.localRotation = srcrot;
        for (float t = LEAPCore.eulerTimeStep * velocity / rotd; t <= 1f; )
        {
            // Update joint rotation
            Quaternion prevrot = bone.localRotation;
            bone.localRotation = Quaternion.Slerp(srcrot, trgrot, t);

            // Has the joint violated MR limits?
            if (CheckMR())
            {
                // Yes, previous rotation is as far as we can go
                bone.localRotation = prevrot;
                return false;
            }

            // Advance joint rotation
            t += (LEAPCore.eulerTimeStep * velocity / rotd);
        }
        return true;
    }

    private Quaternion _ComputeCOBRotation()
    {
        // Temporarily zero model orientation
        Quaternion orig_rot = rootBone.rotation;
        rootBone.rotation = new Quaternion(0, 0, 0, 1);

        Vector3 vroll = -Direction;
        // TODO: need a more general way of defining the basis (what if there's only one eye?)
        Vector3 vpitch = (mdlCtrl.LEye.position -
                          mdlCtrl.REye.position).normalized;
        Vector3 vyaw = Vector3.Cross(vroll, vpitch);
        Quaternion qb = Quaternion.AngleAxis(0, vroll) *
            Quaternion.AngleAxis(0, vpitch) *
                Quaternion.AngleAxis(0, vyaw);
        Quaternion cob_rot = Quaternion.Inverse(bone.rotation) * qb;

        // Restore original model orientation
        rootBone.rotation = orig_rot;

        return cob_rot;
    }

    // Angle between two orientations
    public static float DistanceToRotate(Quaternion srcRot, Quaternion trgRot)
    {
        float dist = Quaternion.Angle(srcRot, trgRot);
        if (Mathf.Abs(dist) <= 0.0001f)
            dist = 0;

        return dist;
    }

    // Clamp angle to [-180,180] range
    public static float ClampAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;
        while (angle < -180f)
            angle += 360f;

        return angle;
    }
}