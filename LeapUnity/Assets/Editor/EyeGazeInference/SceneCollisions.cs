using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Update 4/28/15: updated from using collision spheres to using the mesh colliders of scene objects
/// </summary>
public class SceneCollisions
{

    public List<SceneObject> SceneObjects
    {
        get;
        set;
    }

    //constructor
    /// <summary>
    /// Note: any added animations must be added to this section, and
    /// you can also add a set class for each animation below
    /// </summary>
    /// <param name="animationTitle"></param>
    public SceneCollisions(string animationTitle)
    {
        SceneObjects = new List<SceneObject>();

        switch (animationTitle)
        {
            case "WindowWashingA":
                WindowWashingSetup();
                break;
            case "Walking90deg":
                Walking90degSetup();
                break;
            case "PassSodaA":
                PassSodaASetup();
                break;
            case "PassSodaB":
                PassSodaBSetup();
                break;
            case "StealDiamond":
                StealDiamondSetup();
                break;
            case "BookShelf":
                BookShelfSetup();
                break;
            case "WaitForBus":
                WaitForBusSetup();
                break;
            case "HandShakeA":
                HandShakeASetup();
                break;
            case "HandShakeB":
                HandShakeBSetup();
                break;
        }
    }

    private void HandShakeASetup() {
        //SceneObjects.Add(new SceneObject("Roman Hand", GameObject.Find("Roman/Char_Norman_1/srfBind_Cn_Pelvis/srfBind_Cn_SpineA/srfBind_Cn_SpineB/srfBind_Cn_SpineC/" +
           // "srfBind_Rt_ArmA/srfBind_Rt_ArmB/srfBind_Rt_ArmC/srfBind_Rt_ArmD/Hand_R")));
        //SceneObjects.Add(new SceneObject("Floor", GameObject.Find("Floor"))); 
    }

    private void HandShakeBSetup()
    {
        //SceneObjects.Add(new SceneObject("Norman_Hand", GameObject.Find("Norman/Char_Norman_1/srfBind_Cn_Pelvis/srfBind_Cn_SpineA/srfBind_Cn_SpineB/srfBind_Cn_SpineC/" +
            //"srfBind_Rt_ArmA/srfBind_Rt_ArmB/srfBind_Rt_ArmC/srfBind_Rt_ArmD/Hand_R")));
        //SceneObjects.Add(new SceneObject("Floor", GameObject.Find("Floor")));
    }

    private void WindowWashingSetup()
    {
        SceneObjects.Add(new SceneObject("WindowPane", GameObject.Find("WindowWashingEnv/Plane001")));
        SceneObjects.Add(new SceneObject("Chair", GameObject.Find("WindowWashingEnv/Chair 1")));
        SceneObjects.Add(new SceneObject("Bucket", GameObject.Find("WindowWashingEnv/Obj_000003")));
        SceneObjects.Add(new SceneObject("Floor", GameObject.Find("WindowWashingEnv/Floor")));
    }

    private void Walking90degSetup()
    {
        SceneObjects.Add(new SceneObject("Cone1", GameObject.Find("Walking90degEnv/Road Cone")));
        SceneObjects.Add(new SceneObject("Cone2", GameObject.Find("Walking90degEnv/Road Cone 1")));
        SceneObjects.Add(new SceneObject("Cone3", GameObject.Find("Walking90degEnv/Road Cone 2")));
        SceneObjects.Add(new SceneObject("Cone4", GameObject.Find("Walking90degEnv/Road Cone 3")));
        SceneObjects.Add(new SceneObject("Cone5", GameObject.Find("Walking90degEnv/Road Cone 4")));
        SceneObjects.Add(new SceneObject("Cone6", GameObject.Find("Walking90degEnv/Road Cone 5")));
        SceneObjects.Add(new SceneObject("Cone7", GameObject.Find("Walking90degEnv/Road Cone 6")));
        SceneObjects.Add(new SceneObject("Cone8", GameObject.Find("Walking90degEnv/Road Cone 7")));
        SceneObjects.Add(new SceneObject("Floor", GameObject.Find("Walking90degEnv/Floor")));
    }

    private void PassSodaASetup()
    {
        SceneObjects.Add(new SceneObject("Normanette", GameObject.Find("Normanette/Char_Norman_1/srfBind_Cn_Pelvis/srfBind_Cn_SpineA/srfBind_Cn_SpineB/srfBind_Cn_SpineC")));

        SceneObjects.Add(new SceneObject("Roman_Hand", GameObject.Find("Roman/Char_Norman_1/srfBind_Cn_Pelvis/srfBind_Cn_SpineA/srfBind_Cn_SpineB/srfBind_Cn_SpineC/" +
            "srfBind_Rt_ArmA/srfBind_Rt_ArmB/srfBind_Rt_ArmC/srfBind_Rt_ArmD/Hand_R")));

        var head = GameObject.Find("Roman_Head");
        var agents = GameObject.FindGameObjectsWithTag("Agent");
        var roman = agents.FirstOrDefault(m => m.name.Equals("Roman"));
        var romanRoot = ModelUtils.FindRootBone(roman);
        var romanHead = ModelUtils.FindBone(romanRoot, "srfBind_Cn_Head");
        head.transform.position = romanHead.position;
        head.transform.parent = romanHead;
        SceneObjects.Add(new SceneObject("Roman_Head", head));

        SceneObjects.Add(new SceneObject("Floor", GameObject.Find("PassSodaEnv/Floor")));
        SceneObjects.Add(new SceneObject("Soda Can", GameObject.Find("PassSodaEnv/SodaBottle")));
    }

    private void PassSodaBSetup()
    {
        SceneObjects.Add(new SceneObject("Normanette", GameObject.Find("Normanette/Char_Norman_1/srfBind_Cn_Pelvis/srfBind_Cn_SpineA/srfBind_Cn_SpineB/srfBind_Cn_SpineC")));
        SceneObjects.Add(new SceneObject("Norman_Hand", GameObject.Find("Norman/Char_Norman_1/srfBind_Cn_Pelvis/srfBind_Cn_SpineA/srfBind_Cn_SpineB/srfBind_Cn_SpineC/" +
            "srfBind_Rt_ArmA/srfBind_Rt_ArmB/srfBind_Rt_ArmC/srfBind_Rt_ArmD/Hand_R")));

        var head = GameObject.Find("Norman_Head");
        var agents = GameObject.FindGameObjectsWithTag("Agent");
        var norman = agents.FirstOrDefault(m => m.name.Equals("Norman"));
        var normanRoot = ModelUtils.FindRootBone(norman);
        var normanHead = ModelUtils.FindBone(normanRoot, "srfBind_Cn_Head");
        head.transform.position = normanHead.position;
        head.transform.parent = normanHead;
        SceneObjects.Add(new SceneObject("Norman_Head", head));
        
        SceneObjects.Add(new SceneObject("Floor", GameObject.Find("PassSodaEnv/Floor")));
        SceneObjects.Add(new SceneObject("Soda Can", GameObject.Find("PassSodaEnv/SodaBottle")));
    }

    private void StealDiamondSetup() {
        SceneObjects.Add(new SceneObject("Diamond", GameObject.Find("StealDiamondEnv/Gem")));
        SceneObjects.Add(new SceneObject("DiamondStand", GameObject.Find("StealDiamondEnv/Circle")));
        SceneObjects.Add(new SceneObject("Floor", GameObject.Find("StealDiamondEnv/Floor")));
    }

    private void BookShelfSetup() {
        SceneObjects.Add(new SceneObject("Bookshelf", GameObject.Find("BookShelfEnv/Cube")));
        SceneObjects.Add(new SceneObject("Book1", GameObject.Find("BookShelfEnv/Book1")));
        SceneObjects.Add(new SceneObject("Book2", GameObject.Find("BookShelfEnv/Book2")));
        SceneObjects.Add(new SceneObject("Book3", GameObject.Find("BookShelfEnv/Book3")));
        SceneObjects.Add(new SceneObject("Floor", GameObject.Find("BookShelfEnv/Floor")));

    }

    private void WaitForBusSetup() {
        SceneObjects.Add(new SceneObject("Road", GameObject.Find("WaitForBusEnv/Plane001")));
        SceneObjects.Add(new SceneObject("Grass1", GameObject.Find("WaitForBusEnv/Grass1")));
        SceneObjects.Add(new SceneObject("Grass2", GameObject.Find("WaitForBusEnv/Grass002")));
        SceneObjects.Add(new SceneObject("Sidewalk", GameObject.Find("WaitForBusEnv/Grass003")));
        SceneObjects.Add(new SceneObject("Sign", GameObject.Find("WaitForBusEnv/roadsign5a/one way")));
        SceneObjects.Add(new SceneObject("Sign_pole", GameObject.Find("WaitForBusEnv/roadsign5a/Cylinder04")));
        SceneObjects.Add(new SceneObject("Watch", GameObject.Find("Norman/Char_Norman_1/srfBind_Cn_Pelvis/srfBind_Cn_SpineA/srfBind_Cn_SpineB/srfBind_Cn_SpineC/" +
            "srfBind_Lf_ArmA/srfBind_Lf_ArmB/srfBind_Lf_ArmC/srfBind_Lf_ArmD/Hand_L")));
    }

    public SceneCollisions() { }
}

public class SceneCollisionsWindowWashing : SceneCollisions
{
    //constructor
    public SceneCollisionsWindowWashing() : base("WindowWashingA") { }
}

public class SceneCollisionsWalking90deg : SceneCollisions
{
    public SceneCollisionsWalking90deg() : base("Walking90deg") { }
}

public class SceneCollisionsPassSoda : SceneCollisions
{
    public SceneCollisionsPassSoda() : base("PassSoda") { }
}


/// <summary>
/// houses information about scene objects, including their attached mesh colliders.
/// will be used for gaze targeting
/// </summary>
public class SceneObject
{
    public MeshCollider Collider
    {
        get;
        set;
    }
    public Mesh ColliderMesh
    {
        get;
        set;
    }
    public GameObject GameObject
    {
        get;
        set;
    }
    public int[] TriangleIndices
    {
        get;
        set;
    }
    public Vector3[] Vertices
    {
        get;
        set;
    }
    public string ObjectName
    {
        get;
        set;
    }

    //constructor
    public SceneObject(string name, GameObject obj)
    {
        GameObject = obj;
        ObjectName = name;
        Collider = GameObject.GetComponent<MeshCollider>();
        if (Collider == null) {
            GameObject.AddComponent("MeshCollider");
            Collider = GameObject.GetComponent<MeshCollider>();
        } 
        ColliderMesh = Collider.sharedMesh;
        TriangleIndices = ColliderMesh.triangles;
        Vertices = vertexConversion();
    }

    //converts mesh vertices to global space
    private Vector3[] vertexConversion()
    {
        var localToGlobalMat = GameObject.transform.localToWorldMatrix;
        var globalVertices = new Vector3[Collider.sharedMesh.vertices.Length];
        for (int i = 0; i < Collider.sharedMesh.vertices.Length; i++)
        {
            var vec4 = new Vector4(Collider.sharedMesh.vertices[i].x,
                Collider.sharedMesh.vertices[i].y,
                Collider.sharedMesh.vertices[i].z, 1.0f);

            var globalVec = localToGlobalMat * vec4;
            globalVertices[i] = new Vector3(globalVec.x, globalVec.y, globalVec.z);

        }

        return globalVertices;
    }

    //TODO: will need a separate function to convert the vertices to global coordinates at a 
    //particular frame if the scene objects are MOVING in the animation

}
