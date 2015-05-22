using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class CollisionData : IComparable<CollisionData> {
    public int CollisionFrame
    {
        get;
        set;
    }
    public string CollisionObjectName
    {
        get;
        set;
    }

    //constructor
    public CollisionData(int frame, string name) {
        CollisionFrame = frame;
        CollisionObjectName = name;
    }

    public int CompareTo(CollisionData other) {
        return this.CollisionFrame - other.CollisionFrame;
    }

    public override string ToString()
    {
        return "[" + CollisionFrame + ", " + CollisionObjectName + "]";
    }

}
