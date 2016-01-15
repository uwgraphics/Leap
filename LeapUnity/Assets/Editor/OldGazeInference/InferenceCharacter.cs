using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;



/// <summary>
/// This will be the parent class of all of the characters in the inference model
/// </summary>
public class InferenceCharacter {
    public virtual Color DefaultColor()
    {
        switch (CharName) { 
            case "Norman" :
                return Color.blue;
            case "Normanette" :
                return new Color(1.0f, 0.4f, 0.6f);
            case "Roman" :
                return Color.green;
            default:
                return Color.black;
        }
    }

    public GameObject CharModel
    {
        get;
        protected set;
    }

    //[0] head
    //[1] chest
    //[2] spine_a
    //[3] spine_b
    public Transform[] InferenceBones {
        get;
        protected set;
    }

    //hip bone must be separate since it is not a GazeJoint
    public Transform HipBone
    {
        get;
        set;
    }

    //neck bone is separate for head local space
    public Transform NeckBone
    {
        get;
        set;
    }

    //head bone separate for head local space
    public Transform HeadBone
    {
        get;
        set;
    }

    public Transform ChestBone
    {
        get;
        set;
    }

    public Transform SpineABone
    {
        get;
        set;
    }

    public Transform SpineBBone
    {
        get;
        set;
    }

    public Renderer[] BoneRenderers
    {
        get;
        protected set;
    }

    public string CharName
    {
        get;
        private set;
    }
    
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="charModel"></param>
    public InferenceCharacter(string charName) {
        CharName = charName;
        GameObject[] models = GameObject.FindGameObjectsWithTag("Agent");
        BoneRenderers = new Renderer[5]; //changed to 5 from 4 when hips were aded
        GameObject charModel = models.FirstOrDefault(m => m.name == charName);

        CharModel = charModel;
        //create SpineBones array
        var inferenceBones = new Transform[5]; //changed to 5 from 4 when neck was added (?)
        var gazeCtrl = charModel.GetComponent<GazeController>();
        var headBone = gazeCtrl.head.Top;

        Transform[] headJoints = (Transform[])gazeCtrl.head.gazeJoints.Clone();
        int headJointIndex = 0; //last I checked, there were two headJoints to pick from...
        var headJoint = headJoints[headJointIndex];
        inferenceBones[0] = headJoint;
        Transform[] tBones;
        tBones = charModel.GetComponentsInChildren<Transform>();
        //HipBone = tBones[2]; // May need to double check that index is correct...
        //NeckBone = tBones[6]; // May need to double check that index is correct...
        //HeadBone = tBones[7]; // May need to double check that index is correct...
        HipBone = ModelUtil.FindRootBone(charModel);
        NeckBone = ModelUtil.FindBone(HipBone, "srfBind_Cn_Neck");
        HeadBone = ModelUtil.FindBone(HipBone, "srfBind_Cn_Head");
        ChestBone = ModelUtil.FindBone(HipBone, "srfBind_Cn_SpineC");
        SpineABone = ModelUtil.FindBone(HipBone, "srfBind_Cn_SpineB");
        SpineBBone = ModelUtil.FindBone(HipBone, "srfBind_Cn_SpineA");

        Transform[] chestJoints = (Transform[])gazeCtrl.torso.gazeJoints.Clone();

        inferenceBones[1] = chestJoints[0]; // chest
        inferenceBones[2] = chestJoints[2]; // spine_a
        inferenceBones[3] = chestJoints[1]; // spine_b

        InferenceBones = inferenceBones;

        Renderer[] renderers = charModel.GetComponentsInChildren<Renderer>();
        BoneRenderers[0] = renderers[5]; // head
        BoneRenderers[1] = renderers[3]; // Chest
        BoneRenderers[2] = renderers[1]; // spineA
        BoneRenderers[3] = renderers[2]; // spineB
        BoneRenderers[4] = renderers[0]; // hips (root)

    }

    public InferenceCharacter() { }
}

/// <summary>
/// creates a norman character for the inference model
/// </summary>
public class InferenceCharacterNorman : InferenceCharacter {
    public InferenceCharacterNorman() : base("Norman") { }
    public override Color DefaultColor()
    {
        return Color.blue;
    }
}

public class InferenceCharacterNormanette : InferenceCharacter
{
    public InferenceCharacterNormanette() : base("Normanette") { }
    public override Color DefaultColor()
    {
        return new Color(1.0f, 0.4f, 0.6f);
    }
}

public class InferenceCharacterRoman : InferenceCharacter
{
    public InferenceCharacterRoman() : base("Roman") { }
    public override Color DefaultColor()
    {
        return Color.green;
    }
}

