using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class CollisionTest
{
    public CollisionTest()
    {
        var sc = new SceneCollisionsWalking90deg();
        for (int i = 0; i < sc.SceneObjects[0].Vertices.Length; i++)
        {
            UnityEngine.Debug.Log(sc.SceneObjects[0].Vertices[i]);
        }
    }
}
