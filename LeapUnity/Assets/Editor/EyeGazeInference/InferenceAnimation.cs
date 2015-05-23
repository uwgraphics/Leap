using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;

public class InferenceAnimation {
    //public abstract void analyze();

    //each character in the scene will have a corresponding AngleData object
    public AngleData[] AngleData {
        get;
        protected set;
    }

    public InferenceCharacter[] Characters {
        get;
        protected set;
    }

    public InferenceAnimation(string animationName, string[] characterNames) {
        AngleData = new AngleData[characterNames.Length];
        Characters = new InferenceCharacter[characterNames.Length];
        for (int i = 0; i < characterNames.Length; i++) {
            AngleData[i] = new AngleData(animationName, characterNames[i]);
            Characters[i] = new InferenceCharacter(characterNames[i]);
        }
    }

    //alternate constructor where different characters have different animations
     public InferenceAnimation(string[] animationName, string[] characterNames) {
        AngleData = new AngleData[characterNames.Length];
        Characters = new InferenceCharacter[characterNames.Length];
        for (int i = 0; i < characterNames.Length; i++) {
            AngleData[i] = new AngleData(animationName[i], characterNames[i]);
            Characters[i] = new InferenceCharacter(characterNames[i]);
        }
    }

    protected InferenceAnimation() { }
}


/// <summary>
/// creates an instance of the window washing animation
/// </summary>
public class InferenceAnimationWindowWashing : InferenceAnimation {
    public InferenceAnimationWindowWashing() : base("WindowWashingA", new string[]{"Norman"} ) {}
}

public class InferenceAnimationPassSoda : InferenceAnimation {
    public InferenceAnimationPassSoda()
        : base(new string[] { "PassSodaA", "PassSodaB", "PassSoda" },
            new string[] { "Norman", "Roman", "Normanette" }) { }
}

public class InferenceAnimationWalking : InferenceAnimation {
    public InferenceAnimationWalking() : base("Walking90deg", new string[]{"Norman"} ) {}
}

public class InferenceAnimationStealDiamond : InferenceAnimation { 
    public InferenceAnimationStealDiamond() : base("StealDiamond", new string[]{"Norman"} ) {}
}


