using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class TestScenario : Scenario
{
    public enum ConditionType
    {
        NG,
        HG,
        SG_VDEA,
        SG_CER,
        SG_Timing,
        SG_GEB,
        SG_ED,
        SG
    }

    [Serializable]
    public class GazeShift
    {
        public int targetId;
        public float headAlign = 1f;
        public float eyeAlign = 1f;
        public bool innerHemisphere = false;

        public GazeShift(int targetId, float headAlign, float eyeAlign, bool innerHS)
        {
            this.targetId = targetId;
            this.headAlign = headAlign;
            this.eyeAlign = eyeAlign;
            this.innerHemisphere = innerHS;
        }
    }

    public ConditionType condition;
    public float eyeAlign = 1f;
    public string agentName;
    public float timeScale = 1f;
    public float gsPauseTime = 0.5f;
    public bool measureGazeStats = false;
    public bool customTest = false;

    protected List<Vector3> gtTestPos1 = new List<Vector3>();
    protected List<Vector3> gtTestPos2 = new List<Vector3>();
    protected List<GazeShift> gazeShifts = new List<GazeShift>();
    protected StreamWriter testLog = null;

    ///<see cref="Scenario.GazeAt"/>
    public override int GazeAt(string agentName, string targetName)
    {
        if (condition == ConditionType.NG)
            return targetName != "GTEyeContact" ? -1 : base.GazeAt(agentName, targetName, 1f, 0f);

        return base.GazeAt(agentName, targetName);
    }

    /// <see cref="Scenario.GazeAt"/>
    public override int GazeAt(string agentName, string targetName, float headAlign)
    {
        if (condition == ConditionType.NG)
            return targetName != "GTEyeContact" ? -1 : base.GazeAt(agentName, targetName, 1f, 0f);

        return base.GazeAt(agentName, targetName, headAlign);
    }

    /// <see cref="Scenario.GazeAt"/>
    public override int GazeAt(string agentName, string targetName,
                               float headAlign, float bodyAlign)
    {
        if (condition == ConditionType.NG)
            return targetName != "GTEyeContact" ? -1 : base.GazeAt(agentName, targetName, 1f, 0f);

        return base.GazeAt(agentName, targetName, headAlign, bodyAlign);
    }

    /// <summary>
    /// Sets the gaze shift path curving parameters. 
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent name.
    /// </param>
    /// <param name="lEyeArcs">
    /// Left eye path curving.
    /// </param>
    /// <param name="rEyeArcs">
    /// Right eye path curving.
    /// </param>
    /// <param name="headArcs">
    /// Head path curving/
    /// </param>
    public virtual void SetGazePathArcs(string agentName, float lEyeArcs,
                                   float rEyeArcs, float headArcs)
    {
        // TODO: remove this method
    }

    /// <summary>
    /// Hide the scene by deactivating lights and unlit objects.
    /// </summary>
    public virtual void HideScene()
    {
        SetObjectActive("LightKey", false);
        SetObjectActive("LightBack", false);
        SetObjectActive("LightFill1", false);
        SetObjectActive("LightFill2", false);
    }

    /// <summary>
    /// Show the scene by activating lights and unlit objects. 
    /// </summary>
    public virtual void ShowScene()
    {
        SetObjectActive("LightKey", true);
        SetObjectActive("LightBack", true);
        SetObjectActive("LightFill1", true);
        SetObjectActive("LightFill2", true);
    }

    protected virtual void _StartTestLog()
    {
        if (!measureGazeStats)
            return;

        if (!File.Exists("StylizedGazeTest/data.csv"))
        {
            testLog = new StreamWriter("StylizedGazeTest/data.csv");

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("char,cond,eyeAlign,");
            sb.Append("Cross_HA0,");
            sb.Append("OMR_HA0,");
            sb.Append("EC_HA0,");
            sb.Append("EA_HA0,");
            sb.Append("EHA_HA0,");
            sb.Append("ED_HA0,");
            sb.Append("T_HA0,");
            sb.Append("Cross_HA1,");
            sb.Append("OMR_HA1,");
            sb.Append("EC_HA1,");
            sb.Append("EA_HA1,");
            sb.Append("EHA_HA1,");
            sb.Append("ED_HA1,");
            sb.Append("T_HA1");
            testLog.WriteLine(sb.ToString());
        }
        else
        {
            testLog = new StreamWriter("StylizedGazeTest/data.csv", true);
        }
    }

    protected virtual void _LogTestResults(List<float> scores)
    {
        if (!measureGazeStats)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(agentName);
        sb.Append(",");
        sb.Append(condition.ToString());
        sb.Append(",");
        sb.Append(eyeAlign.ToString());
        sb.Append(",");
        for (int stati = 0; stati < scores.Count; ++stati)
        {
            sb.Append(scores[stati]);
            sb.Append(",");
        }
        testLog.WriteLine(sb.ToString());

        testLog.Close();
    }

    // Initialize gaze techniques for the current condition
    protected virtual void _InitGazeTechniques()
    {
        GazeController gctrl = agents[agentName].GetComponent<GazeController>();

        // TODO: bring all this back when you bring back stylized gaze
        gctrl.stylizeGaze = condition != ConditionType.NG && condition != ConditionType.HG;
        /*gctrl.eyeAlign = (condition != ConditionType.HG ? 0.5f : 1f);
        gctrl.maxCrossEyedness = (condition != ConditionType.SG_CER && condition != ConditionType.SG ?
                                  110f : gctrl.maxCrossEyedness);
        gctrl.quickness = (condition != ConditionType.SG_Timing && condition != ConditionType.SG ?
                           1f : gctrl.quickness);
        gctrl.eyeSize = (condition != ConditionType.SG_Timing && condition != ConditionType.SG ?
                         1f : gctrl.eyeSize);
        gctrl.eyeTorque = (condition != ConditionType.SG_Timing && condition != ConditionType.SG ?
                           1f : gctrl.eyeTorque);*/
        /*gctrl.stageGazeBlinks = ( condition != ConditionType.SG_GEB && condition != ConditionType.SG ?
                               false : true );*/
        // TODO: now SG_CER condition includes both target pose adaptation techniques
        /*gctrl.enableED = condition == ConditionType.SG_ED || condition == ConditionType.SG || condition == ConditionType.SG_CER;
        gctrl.enableAEM = condition == ConditionType.SG_Timing || condition == ConditionType.SG;
        gctrl.enableEAH = condition == ConditionType.SG_Timing || condition == ConditionType.SG;*/

        SetGazePathArcs(agentName, 0f, 0f, 0f);
    }

    // Generate test targets along a hemisphere in front of the character
    protected virtual void _GenerateGazeTargets()
    {
        GazeController gctrl = agents[agentName].GetComponent<GazeController>();

        // Compute eye centroid
        Vector3 ep = new Vector3(0, 0, 0);
        ep = (gctrl.lEye.Top.position + gctrl.rEye.Top.position) / 2f;

        // Compute eye-camera vector
        Vector3 vp = GameObject.FindGameObjectWithTag("EyeContactHelper").transform.position;
        Vector3 ev = vp - ep;

        // Compute gaze target positions
        for (int hi = -3; hi <= 3; ++hi)
        {
            for (int vi = -3; vi <= 3; ++vi)
            {
                if ((hi == -3 || hi == 3) &&
                   vi != 0)
                    continue;

                float th = hi * 82f / 3f;
                float ph = vi * 82f / 3f;
                Quaternion rot = Quaternion.Euler(-th, ph, 0f);
                Vector3 ev1 = rot * ev;
                gtTestPos1.Add(ep + ev1);
                gtTestPos2.Add(ep + 0.38f * ev1);
                //
                /*GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.transform.localScale = new Vector3(0.05f,0.05f,0.05f);
                obj.transform.position = ep+ev1;*/
                //
            }
        }
    }

    // Load a gaze shift sequence from file
    protected virtual void _LoadGazeShifts()
    {
        if (!File.Exists("StylizedGazeTest/gs.csv"))
        {
            _GenerateGazeShifts();
            return;
        }

        StreamReader sr = new StreamReader("StylizedGazeTest/gs.csv");
        string gsline;
        while ((gsline = sr.ReadLine()) != null)
        {
            string[] gsdata = gsline.Split(",".ToCharArray());
            gazeShifts.Add(new GazeShift(int.Parse(gsdata[0]), float.Parse(gsdata[1]), eyeAlign,
                                          bool.Parse(gsdata[3])));
        }
        sr.Close();
    }

    // Generate a sequence of random gaze shifts
    protected virtual void _GenerateGazeShifts()
    {
        gazeShifts.Clear();
        int num_gs = gtTestPos1.Count * gtTestPos1.Count / 10;
        for (int gsi = 0; gsi < num_gs; ++gsi)
        {
            // Draw next target
            int gtid = UnityEngine.Random.Range(0, gtTestPos1.Count - 1);
            if ((gsi + 1) % 5 == 0)
            {
                // Gaze at viewer every now and then
                gtid = 18;
            }
            else
            {
                // How vertical is the gaze shift?
                GazeController gctrl = agents[agentName].GetComponent<GazeController>();
                Quaternion currot = gctrl.lEye.Top.localRotation;
                gctrl.lEye.RotateTowards(
                    gctrl.lEye.GetTargetDirection(gsi > 0 ? gtTestPos1[gazeShifts[gsi - 1].targetId] :
                    cameras["Main Camera"].transform.position));
                float y0 = gctrl.lEye.Yaw;
                float p0 = gctrl.lEye.Pitch;
                gctrl.lEye.RotateTowards(
                    gctrl.lEye.GetTargetDirection(gtTestPos1[gtid]));
                float y1 = gctrl.lEye.Yaw;
                float p1 = gctrl.lEye.Pitch;
                gctrl.lEye.Top.localRotation = currot;
                float y01 = y1 - y0;
                float p01 = p1 - p0;
                float mag = y01 * y01 + p01 * p01;
                if (mag < 0.001f)
                {
                    // Whoops, gaze target same as previous one...
                    --gsi;
                    continue;
                }
                float vert1 = Mathf.Clamp01(Mathf.Abs(p01) / Mathf.Sqrt(mag));
                float vert2 = Mathf.Clamp01(Mathf.Abs(p1) / 82f);
                float vert = 0.5f * (vert1 + vert2);

                // Vertical gaze shifts less probable
                float pgs = (1f - 0.66f * vert1) * (1f - Mathf.Exp(-2.5f * vert2));
                if (UnityEngine.Random.value <= pgs)
                {
                    --gsi;
                    continue;
                }
            }

            float ha = UnityEngine.Random.value < 0.5f ? 0f : 1f;
            bool ihs = UnityEngine.Random.value < 0.33f ? true : false;
            gazeShifts.Add(new GazeShift(gtid, ha, eyeAlign, ihs));
        }

        StreamWriter sw = new StreamWriter("StylizedGazeTest/gs.csv");
        foreach (GazeShift gs in gazeShifts)
            sw.WriteLine(string.Format("{0},{1},{2},{3}", gs.targetId, gs.headAlign,
                                        gs.eyeAlign, gs.innerHemisphere));
        sw.Close();
    }

    /// <see cref="Scenario._Init()"/>
    protected override void _Init()
    {
    }

    /// <see cref="Scenario._Run()"/>
    protected override IEnumerator _Run()
    {
        yield return new WaitForSeconds(0.1f);

        // Configure scenario
        Time.timeScale = measureGazeStats ? 4f : timeScale;
        gsPauseTime = measureGazeStats ? 0 : gsPauseTime;
        if (!customTest)
        {
            _GenerateGazeTargets();
            _LoadGazeShifts();
        }

        // Configure animation controllers
        GazeController gctrl = agents[agentName].GetComponent<GazeController>();
        ExpressionController exprCtrl = agents[agentName].GetComponent<ExpressionController>();
        BlinkController bctrl = agents[agentName].GetComponent<BlinkController>();
        //gctrl.measureGazeStats = measureGazeStats;
        _InitGazeTechniques();

        int curgaze = -1;
        int curexpr = -1;

        HideScene();

        // Smile!
        curexpr = ChangeExpression(agentName, "ExpressionSmileClosed", 0.5f, 0f);
        yield return StartCoroutine(WaitUntilFinished(curexpr));
        exprCtrl.FixExpression();

        // Initialize gaze
        yield return new WaitForSeconds(0.1f * Time.timeScale);
        //curgaze = GazeAtCamera(agentName,1f,0f,0f,0f,1f);
        //curgaze = GazeAt(agentName,"GTTestR",0f,0f,0f,0f,0.6f);
        //yield return StartCoroutine( WaitUntilFinished(curgaze) );

        ShowScene();
        if (customTest)
            yield break;
        yield return new WaitForSeconds(gsPauseTime * Time.timeScale);

        // Mean values of gaze scores
        /*int nha0 = 0;
        int nha1 = 0;
        float mCross_HA0 = 0;
        float mOMR_HA0 = 0;
        float mEC_HA0 = 0;
        float mEA_HA0 = 0;
        float mEHA_HA0 = 0;
        float mED_HA0 = 0;
        float mTime_HA0 = 0;
        float mCross_HA1 = 0;
        float mOMR_HA1 = 0;
        float mEC_HA1 = 0;
        float mEA_HA1 = 0;
        float mEHA_HA1 = 0;
        float mED_HA1 = 0;
        float mTime_HA1 = 0;
        List<float> stats = new List<float>();
		
        _StartTestLog();*/

        // Execute test gaze shifts
        foreach (GazeShift gs in gazeShifts)
        {
            gazeTargets["GTTest"].transform.position = gs.innerHemisphere ?
                gtTestPos2[gs.targetId] : gtTestPos1[gs.targetId];
            //curgaze = GazeAt(agentName, "GTTest", gs.headAlign, 0f, 0f, 0f, gs.eyeAlign);
            // TODO: bring this back when you bring back stylized gaze
            yield return StartCoroutine(WaitUntilFinished(curgaze));

            /*if( gs.headAlign < 0.5f )
            {
                mCross_HA0 += gctrl.statCross;
                mOMR_HA0 += gctrl.statOMR;
                mEC_HA0 += gctrl.statEC;
                mEA_HA0 += gctrl.statEA;
                mEHA_HA0 += gctrl.statEHA;
                mED_HA0 += gctrl.statED;
                mTime_HA0 += gctrl.statTime;
                ++nha0;
            }
            else
            {
                mCross_HA1 += gctrl.statCross;
                mOMR_HA1 += gctrl.statOMR;
                mEC_HA1 += gctrl.statEC;
                mEA_HA1 += gctrl.statEA;
                mEHA_HA1 += gctrl.statEHA;
                mED_HA1 += gctrl.statED;
                mTime_HA1 += gctrl.statTime;
                ++nha1;
            }*/

            yield return new WaitForSeconds(gsPauseTime * Time.timeScale);
        }

        /*mCross_HA0 /= nha0;
        mOMR_HA0 /= nha0;
        mEC_HA0 /= nha0;
        mEA_HA0 /= nha0;
        mEHA_HA0 /= nha0;
        mED_HA0 /= nha0;
        mTime_HA0 /= nha0;
        stats.Add(mCross_HA0);
        stats.Add(mOMR_HA0);
        stats.Add(mEC_HA0);
        stats.Add(mEA_HA0);
        stats.Add(mEHA_HA0);
        stats.Add(mED_HA0);
        stats.Add(mTime_HA0);
        mCross_HA1 /= nha1;
        mOMR_HA1 /= nha1;
        mEC_HA1 /= nha1;
        mEA_HA1 /= nha1;
        mEHA_HA1 /= nha1;
        mED_HA1 /= nha1;
        mTime_HA1 /= nha1;
        stats.Add(mCross_HA1);
        stats.Add(mOMR_HA1);
        stats.Add(mEC_HA1);
        stats.Add(mEA_HA1);
        stats.Add(mEHA_HA1);
        stats.Add(mED_HA1);
        stats.Add(mTime_HA1);
		
        _LogTestResults(stats);*/

        yield return new WaitForSeconds(Time.timeScale);
    }

    /// <see cref="Scenario._Finish()"/>
    protected override void _Finish()
    {
    }
}
