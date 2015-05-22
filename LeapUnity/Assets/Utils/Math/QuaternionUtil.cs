using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Utility class containing some helpful quaternion functions. 
/// </summary>
public static class QuaternionUtil
{
    // Multiply quaternion with a scalar
    public static Quaternion Mul(Quaternion q, float v)
    {
        q.x *= v;
        q.y *= v;
        q.z *= v;
        q.w *= v;

        return q;
    }

    // Add two quaternions
    public static Quaternion Add(Quaternion q1, Quaternion q2)
    {
        q1.x += q2.x;
        q1.y += q2.y;
        q1.z += q2.z;
        q1.w += q2.w;

        return q1;
    }

    // Subtract two quaternions
    public static Quaternion Sub(Quaternion q1, Quaternion q2)
    {
        q1.x -= q2.x;
        q1.y -= q2.y;
        q1.z -= q2.z;
        q1.w -= q2.w;

        return q1;
    }

    // Compute the norm of a quaternion
    public static float Norm(Quaternion q)
    {
        return Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
    }

    // Quaternion log-map
    public static Vector3 Log(Quaternion q)
    {
        Vector3 v = new Vector3(q.x, q.y, q.z);

        if (Mathf.Abs(q.w) < 1f)
        {
            float a = Mathf.Acos(q.w);
            float sina = Mathf.Sin(a);

            if (Mathf.Abs(sina) >= 0.005f)
            {
                float c = a / sina;
                v.x *= c;
                v.y *= c;
                v.z *= c;
            }
        }

        return v;
    }

    // Quaternion exponential map
    public static Quaternion Exp(Vector3 v)
    {
        Quaternion q = new Quaternion(v.x, v.y, v.z, 1f);
        float a = v.magnitude;
        float sina = Mathf.Sin(a);

        if (Mathf.Abs(sina) >= 0.005f)
        {
            float c = sina / a;
            q.x *= c;
            q.y *= c;
            q.z *= c;
        }

        q.w = Mathf.Cos(a);

        return q;
    }

    // Find rotation vector displacement between two quaternions (from q1 to q2)
    public static Vector3 Disp(Quaternion q1, Quaternion q2)
    {
        return QuaternionUtil.Log(Quaternion.Inverse(q1) * q2);
    }

    //find quaternion displacement between two quaternions (from q1 to q2)
    public static Quaternion DispQ(Quaternion q1, Quaternion q2)
    {
        return Quaternion.Inverse(q1) * q2;
    }

    //check this function 
    //converts a list of rotation vectors back to orientation quaternions
    public static List<Quaternion> RotVecsToQuats(List<Vector3> rotVecs, Quaternion q0)
    {
        if (rotVecs == null || rotVecs.Count == 0)
        {
            throw new ArgumentException("Error in RotVecsToQuats function!");
        }
        var quaternions = new List<Quaternion>();

        //running product
        //Quaternion qProd = q0;
        //quaternions.Add(q0);
        //
        //for (int i = 0; i < rotVecs.Count-1; i++) {
        //    qProd = qProd * QuaternionUtil.Exp(rotVecs[i + 1] - rotVecs[i]);
        //    quaternions.Add(qProd);
        //}

        Quaternion qProd = q0;
        for (int i = 0; i < rotVecs.Count - 1; i++)
        {
            for (int j = 0; j < i - 1; j++)
            {
                qProd *= QuaternionUtil.Exp(rotVecs[j + 1] - rotVecs[j]);
            }
            quaternions.Add(qProd);
            qProd = q0;
        }

        return quaternions;
    }

    public static List<Vector3> QuatsToRotVecs(List<Quaternion> quats, Vector3 p0)
    {
        if (quats == null) throw new ArgumentException("Error in QuatstoRotVecs function.");

        var rotVecs = new List<Vector3>();
        Vector3 pLast = p0;
        Vector3 pNew = p0;
        rotVecs.Add(p0);
        for (int i = 0; i < quats.Count - 1; i++)
        {
            pNew = pLast + Disp(quats[i], quats[i + 1]);
            rotVecs.Add(pNew - pLast);
            pLast = pNew;
        }

        return rotVecs;
    }

    public static List<Vector3> QuatsToRotVecs(List<Quaternion> quats)
    {
        return QuatsToRotVecs(quats, Log(quats[0]));
    }

    // First-order partial derivative of quaternion exp. map
    // TODO: this function is not giving the expected results and needs to be checked over
    public static Quaternion DExp(Vector3 v, int vci)
    {
        Quaternion q = new Quaternion();
        float phi = v.magnitude;
        float phi2 = phi * phi;

        if (Mathf.Abs(phi) > 0.005f)
        {
            float phi3 = phi2 * phi;
            float cosphi = Mathf.Cos(0.5f * phi);
            float sinphi = Mathf.Sin(0.5f * phi);

            // Compute x, y, z components
            for (int vcj = 0; vcj < 3; ++vcj)
            {
                if (vci == vcj)
                {
                    q[vcj] = 0.5f * v[vcj] * v[vcj] * cosphi / phi2 - v[vcj] * v[vcj] * sinphi / phi3 + sinphi / phi;
                }
                else
                {
                    q[vcj] = 0.5f * v[vci] * v[vcj] * cosphi / phi2 - v[vci] * v[vcj] * sinphi / phi3;
                }
            }

            // Compute w component
            q.w = -0.5f * v[vci] * sinphi / phi;
        }
        else
        {
            // When phi is near zero, we use Taylor expansion of sin and cos

            // Compute x, y, z components
            for (int vcj = 0; vcj < 3; ++vcj)
            {
                if (vci == vcj)
                {
                    q[vcj] = (1f / 24f) * v[vcj] * v[vcj] * (phi2 / 40f - 1f) + 0.5f - phi2 / 48f;
                }
                else
                {
                    q[vcj] = (1f / 24f) * v[vci] * v[vcj] * (phi2 / 40f - 1f);
                }
            }

            // Compute w component
            q.w = -0.5f * v[vci] * (0.5f - phi2 / 48f);
        }

        return q;
    }
}