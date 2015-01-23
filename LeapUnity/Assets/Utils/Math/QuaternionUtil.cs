using UnityEngine;
using System.Collections;

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

    // First-order partial derivative of quaternion exp. map
    // TODO: this function is not giving the expected results and needs to be checked over
    public static Quaternion DExp(Vector3 v, int vci)
    {
        Quaternion q = new Quaternion();
        float phi = v.magnitude;
        float phi2 = phi*phi;

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

    public static bool Equal(Vector3 v1, Vector3 v2)
    {
        return Mathf.Abs(v1.x - v2.x) < 0.001f &&
            Mathf.Abs(v1.y - v2.y) < 0.001f &&
                Mathf.Abs(v1.z - v2.z) < 0.001f;
    }
}
