using UnityEngine;

public struct Matrix3x3
{
    public static Matrix3x3 identity
    {
        get { return _identity; }
    }

    public static Matrix3x3 zero
    {
        get { return _zero; }
    }

    private static Matrix3x3 _identity, _zero;

    public float m00, m01, m02, m10, m11, m12, m20, m21, m22;

    static Matrix3x3()
    {
        _identity = new Matrix3x3();
        _zero = new Matrix3x3();
        _zero.m00 = _zero.m01 = _zero.m02 =
            _zero.m10 = _zero.m11 = _zero.m12 =
            _zero.m20 = _zero.m21 = _zero.m22 = 0f;
    }

    public float determinant
    {
        get
        {
            return m00 * (m11 * m22 - m12 * m21) -
                m01 * (m10 * m22 - m12 * m21) +
                m02 * (m10 * m21 - m11 * m20);
        }
    }

    public float this[int i, int j]
    {
        get
        {
            if (i == 0 && j == 0)
                return m00;
            else if (i == 0 && j == 1)
                return m01;
            else if (i == 0 && j == 2)
                return m02;
            else if (i == 1 && j == 0)
                return m10;
            else if (i == 1 && j == 1)
                return m11;
            else if (i == 1 && j == 2)
                return m12;
            else if (i == 2 && j == 0)
                return m20;
            else if (i == 2 && j == 1)
                return m21;
            else // if (i == 2 && j == 2)
                return m22;
        }

        set
        {
            if (i == 0 && j == 0)
                m00 = value;
            else if (i == 0 && j == 1)
                m01 = value;
            else if (i == 0 && j == 2)
                m02 = value;
            else if (i == 1 && j == 0)
                m10 = value;
            else if (i == 1 && j == 1)
                m11 = value;
            else if (i == 1 && j == 2)
                m12 = value;
            else if (i == 2 && j == 0)
                m20 = value;
            else if (i == 2 && j == 1)
                m21 = value;
            else // if (i == 2 && j == 2)
                m22 = value;
        }
    }

    public Matrix3x3 inverse
    {
        get
        {
            float detA = determinant;
            Matrix3x3 invA = Matrix3x3.zero;
            invA.m00 = (m11 * m22 - m12 * m21) / detA;
            invA.m01 = (m02 * m21 - m01 * m22) / detA;
            invA.m02 = (m01 * m12 - m02 * m11) / detA;
            invA.m10 = (m12 * m20 - m10 * m22) / detA;
            invA.m11 = (m00 * m22 - m02 * m20) / detA;
            invA.m12 = (m02 * m10 - m00 * m12) / detA;
            invA.m20 = (m10 * m21 - m11 * m20) / detA;
            invA.m21 = (m01 * m20 - m00 * m21) / detA;
            invA.m22 = (m00 * m11 - m01 * m10) / detA;

            return invA;
        }
    }

    public Matrix3x3 transpose
    {
        get
        {
            Matrix3x3 transA = this;
            transA.m01 = this.m10;
            transA.m02 = this.m20;
            transA.m10 = this.m01;
            transA.m12 = this.m21;
            transA.m20 = this.m02;
            transA.m21 = this.m12;

            return transA;
        }
    }

    public Vector3 GetColumn(int i)
    {
        return new Vector3(this[0, i], this[1, i], this[2, i]);
    }

    public void SetColumn(int i, Vector3 v)
    {
        this[0, i] = v.x;
        this[1, i] = v.y;
        this[2, i] = v.z;
    }

    public Vector3 GetRow(int i)
    {
        return new Vector3(this[i, 0], this[i, 1], this[i, 2]);
    }

    public void SetRow(int i, Vector3 v)
    {
        this[i, 0] = v.x;
        this[i, 1] = v.y;
        this[i, 2] = v.z;
    }

    public Vector3 MultiplyPoint(Vector3 p)
    {
        return new Vector3(m00 * p.x + m01 * p.y + m02 * p.z,
            m10 * p.x + m11 * p.y + m12 * p.z,
            m20 * p.x + m21 * p.y + m22 * p.z);
    }

    public void ToAngleAxis(out float angle, out Vector3 axis)
    {
        float eps = 0.01f;
        if (Mathf.Abs(m01 - m10) < eps &&
            Mathf.Abs(m02 - m20) < eps &&
            Mathf.Abs(m12 - m21) < eps)
        {
            if (Mathf.Abs(m01 + m10) < eps &&
                Mathf.Abs(m02 + m20) < eps &&
                Mathf.Abs(m12 + m21) < eps &&
                Mathf.Abs(m00 + m11 + m22 - 3f) < eps)
            {
                angle = 0f;
                axis = new Vector3(1f, 0f, 0f);

                return;
            }

            angle = 180f;
            float xx = (m00 + 1f) / 2f;
            float yy = (m11 + 1f) / 2f;
            float zz = (m22 + 1f) / 2f;
            float xy = (m01 + m10) / 4f;
            float xz = (m02 + m20) / 4f;
            float yz = (m12 + m21) / 4f;
            float sqrt22 = Mathf.Sqrt(2f)/2f;
            if (xx > yy && xx > zz)
            {
                if (xx < eps)
                {
                    axis = new Vector3(0f, sqrt22, sqrt22);
                }
                else
                {
                    float sqrtxx = Mathf.Sqrt(xx);
                    axis = new Vector3(sqrtxx, xy / sqrtxx, xz / sqrtxx);
                }
            }
            else if (yy > zz)
            {
                if (yy < eps)
                {
                    axis = new Vector3(sqrt22, 0f, sqrt22);
                }
                else
                {
                    float sqrtyy = Mathf.Sqrt(yy);
                    axis = new Vector3(xy / sqrtyy, sqrtyy, yz / sqrtyy);
                }
            }
            else
            {
                if (zz < eps)
                {
                    axis = new Vector3(sqrt22, sqrt22, 0f);
                }
                else
                {
                    float sqrtzz = Mathf.Sqrt(zz);
                    axis = new Vector3(xz / sqrtzz, yz / sqrtzz, sqrtzz);
                }
            }

            return;
        }

        float m2112 = m21 - m12;
        float m0220 = m02 - m20;
        float m1001 = m10 - m01;
        float s = Mathf.Sqrt(m2112 * m2112 + m0220 * m0220 + m1001 * m1001);
        if (Mathf.Abs(s) < 0.00001f)
            s = 1f;
        angle = Mathf.Acos((m00 + m11 + m22 - 1f) / 2f) * Mathf.Rad2Deg;
        axis = new Vector3(m2112 / s, m0220 / s, m1001 / s);
    }

    public override string ToString()
    {
        return string.Format("({0} {1} {2}; {3} {4} {5}; {6} {7} {8})",
            m00, m01, m02, m10, m11, m12, m20, m21, m22);
    }

    public static Matrix3x3 operator *(Matrix3x3 mat1, Matrix3x3 mat2)
    {
        Matrix3x3 mat = Matrix3x3.zero;
        mat.m00 = mat1.m00 * mat2.m00 + mat1.m01 * mat2.m10 + mat1.m02 * mat2.m20;
        mat.m01 = mat1.m00 * mat2.m01 + mat1.m01 * mat2.m11 + mat1.m02 * mat2.m21;
        mat.m02 = mat1.m00 * mat2.m02 + mat1.m01 * mat2.m12 + mat1.m02 * mat2.m22;
        mat.m10 = mat1.m10 * mat2.m00 + mat1.m11 * mat2.m10 + mat1.m12 * mat2.m20;
        mat.m11 = mat1.m10 * mat2.m01 + mat1.m11 * mat2.m11 + mat1.m12 * mat2.m21;
        mat.m12 = mat1.m10 * mat2.m02 + mat1.m11 * mat2.m12 + mat1.m12 * mat2.m22;
        mat.m20 = mat1.m20 * mat2.m00 + mat1.m21 * mat2.m10 + mat1.m22 * mat2.m20;
        mat.m21 = mat1.m20 * mat2.m01 + mat1.m21 * mat2.m11 + mat1.m22 * mat2.m21;
        mat.m22 = mat1.m20 * mat2.m02 + mat1.m21 * mat2.m12 + mat1.m22 * mat2.m22;

        return mat;
    }

    public static Matrix3x3 operator +(Matrix3x3 mat1, Matrix3x3 mat2)
    {
        Matrix3x3 mat = Matrix3x3.zero;
        for (int i = 0; i < 3; ++i)
            for (int j = 0; j < 3; ++j)
                mat[i, j] = mat1[i, j] + mat2[i, j];

        return mat;
    }

    public static Matrix3x3 operator -(Matrix3x3 mat1, Matrix3x3 mat2)
    {
        Matrix3x3 mat = Matrix3x3.zero;
        for (int i = 0; i < 3; ++i)
            for (int j = 0; j < 3; ++j)
                mat[i, j] = mat1[i, j] - mat2[i, j];

        return mat;
    }

    public static Matrix3x3 operator *(Matrix3x3 mat, float s)
    {
        Matrix3x3 mat1 = Matrix3x3.zero;
        for (int i = 0; i < 3; ++i)
            for (int j = 0; j < 3; ++j)
                mat1[i, j] = mat[i, j] * s;

        return mat1;
    }

    public static Matrix3x3 operator *(float s, Matrix3x3 mat)
    {
        return mat * s;
    }

    public static Matrix3x3 operator /(Matrix3x3 mat, float s)
    {
        return mat * (1f / s);
    }

    public static Matrix3x3 MultiplyVectors(Vector3 v1, Vector3 v2)
    {
        Matrix3x3 mat = Matrix3x3.zero;
        mat.m00 = v1.x * v2.x;
        mat.m01 = v1.x * v2.y;
        mat.m02 = v1.x * v2.z;
        mat.m10 = v1.y * v2.x;
        mat.m11 = v1.y * v2.y;
        mat.m12 = v1.y * v2.z;
        mat.m20 = v1.z * v2.x;
        mat.m21 = v1.z * v2.y;
        mat.m22 = v1.z * v2.z;

        return mat;
    }

    public static Matrix3x3 Rotation(Quaternion q)
    {
        Matrix3x3 mat = Matrix3x3.zero;
        mat.m00 = 1f - 2f * q.y * q.y - 2f * q.z * q.z;
        mat.m01 = 2f * q.x * q.y - 2f * q.z * q.w;
        mat.m02 = 2f * q.x * q.z + 2f * q.y * q.w;
        mat.m10 = 2f * q.x * q.y + 2f * q.z * q.w;
        mat.m11 = 1f - 2f * q.x * q.x - 2f * q.z * q.z;
        mat.m12 = 2f * q.y * q.z - 2f * q.x * q.w;
        mat.m20 = 2f * q.x * q.z - 2f * q.y * q.w;
        mat.m21 = 2f * q.y * q.z + 2f * q.x * q.w;
        mat.m22 = 1f - 2f * q.x * q.x - 2f * q.y * q.y;

        return mat;
    }

    public static Matrix3x3 Scale(Vector3 s)
    {
        Matrix3x3 mat = Matrix3x3.identity;
        mat.m00 = s.x;
        mat.m11 = s.y;
        mat.m22 = s.z;

        return mat;
    }
}
