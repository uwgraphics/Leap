using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Utility methods for working with strings.
/// </summary>
public static class StringUtil
{
    /// <summary>
    /// Parse string as object of type.
    /// </summary>
    /// <param name="objType">Object type</param>
    /// <param name="objStr">Object string</param>
    /// <returns>Object</returns>
    public static object FromString(Type objType, string objStr)
    {
        if (objType == typeof(string))
        {
            return objStr;
        }
        else if (objType == typeof(int) || objType == typeof(uint) ||
            objType == typeof(short) || objType == typeof(ushort) ||
            objType == typeof(long) || objType == typeof(ulong))
        {
            // TODO: obv. should parse these separately, but don't care to right now
            return int.Parse(objStr);
        }
        else if (objType == typeof(float) || objType == typeof(double))
        {
            // TODO: obv. should parse these separately, but don't care to right now
            return float.Parse(objStr);
        }
        else if (objType == typeof(bool))
        {
            return bool.Parse(objStr);
        }
        else if (objType == typeof(UnityEngine.Vector3))
        {
            var dataValues = objStr.Trim('\"').Split(' ');
            if (dataValues.Length != 3)
                throw new Exception("Invalid Vector3 string ");
            var data = new UnityEngine.Vector3();
            data.x = float.Parse(dataValues[0]);
            data.y = float.Parse(dataValues[1]);
            data.z = float.Parse(dataValues[2]);

            return data;
        }
        else if (objType == typeof(UnityEngine.Vector2))
        {
            var dataValues = objStr.Trim('\"').Split(' ');
            if (dataValues.Length != 2)
                throw new Exception("Invalid Vector2 string ");
            var data = new UnityEngine.Vector2();
            data.x = float.Parse(dataValues[0]);
            data.y = float.Parse(dataValues[1]);

            return data;
        }
        else if (objType == typeof(string[]))
        {
            var dataValues = objStr.Trim('\"').Split(' ');
            return dataValues;
        }
        else if (objType == typeof(int[]) || objType == typeof(uint[]) ||
            objType == typeof(short[]) || objType == typeof(ushort[]) ||
            objType == typeof(long[]) || objType == typeof(ulong[]))
        {
            // TODO: obv. should parse these separately, but don't care to right now
            var dataValuesStr = objStr.Trim('\"').Split(' ');
            var dataValues = new int[dataValuesStr.Length];
            for (int dataValueIndex = 0; dataValueIndex < dataValues.Length; ++dataValueIndex)
                dataValues[dataValueIndex] = int.Parse(dataValuesStr[dataValueIndex]);

            return dataValues;
        }
        else if (objType == typeof(float[]) || objType == typeof(double[]))
        {
            // TODO: obv. should parse these separately, but don't care to right now
            var dataValuesStr = objStr.Trim('\"').Split(' ');
            var dataValues = new float[dataValuesStr.Length];
            for (int dataValueIndex = 0; dataValueIndex < dataValues.Length; ++dataValueIndex)
                dataValues[dataValueIndex] = float.Parse(dataValuesStr[dataValueIndex]);

            return dataValues;
        }
        else
        {
            throw new Exception("Don't know how to parse strings of type " + objType.Name);
        }
    }

    /// <summary>
    /// Write object to string.
    /// </summary>
    /// <param name="objType">Object type</param>
    /// <param name="obj">Object</param>
    /// <returns>String</returns>
    public static string ToString(Type objType, object obj)
    {
        if (objType == typeof(string))
        {
            return (string)obj;
        }
        else if (objType == typeof(int) || objType == typeof(uint) ||
            objType == typeof(short) || objType == typeof(ushort) ||
            objType == typeof(long) || objType == typeof(ulong))
        {
            // TODO: obv. should write these separately, but don't care to right now
            return ((int)obj).ToString();
        }
        else if (objType == typeof(float) || objType == typeof(double))
        {
            // TODO: obv. should write these separately, but don't care to right now
            return ((float)obj).ToString();
        }
        else if (objType == typeof(bool))
        {
            return ((bool)obj).ToString();
        }
        else if (objType == typeof(UnityEngine.Vector3))
        {
            UnityEngine.Vector3 dcData = (UnityEngine.Vector3)obj;
            string dataStr = "\"" + dcData.x + " " + dcData.y + " " + dcData.z + "\"";

            return dataStr;
        }
        else if (objType == typeof(UnityEngine.Vector2))
        {
            UnityEngine.Vector2 dcData = (UnityEngine.Vector2)obj;
            string dataStr = "\"" + dcData.x + " " + dcData.y + "\"";

            return dataStr;
        }
        else if (objType == typeof(string[]))
        {
            string[] dcData = (string[])obj;
            string dataStr = "";
            for (int valueIndex = 0; valueIndex < dcData.Length; ++valueIndex)
            {
                dataStr += dcData[valueIndex];
                if (valueIndex < dcData.Length - 1)
                    dataStr += " ";
            }
            dataStr = "\"" + dataStr + "\"";

            return dataStr;
        }
        else if (objType == typeof(int[]) || objType == typeof(uint[]) ||
            objType == typeof(short[]) || objType == typeof(ushort[]) ||
            objType == typeof(long[]) || objType == typeof(ulong[]))
        {
            // TODO: obv. should write these separately, but don't care to right now
            int[] dcData = null;
            if (objType == typeof(int[]))
                dcData = (obj as int[]).Cast<int>().ToArray();
            else if (objType == typeof(uint[]))
                dcData = (obj as uint[]).Cast<int>().ToArray();
            else if (objType == typeof(short[]))
                dcData = (obj as short[]).Cast<int>().ToArray();
            else if (objType == typeof(ushort[]))
                dcData = (obj as ushort[]).Cast<int>().ToArray();
            else if (objType == typeof(long[]))
                dcData = (obj as long[]).Cast<int>().ToArray();
            else if (objType == typeof(ulong[]))
                dcData = (obj as ulong[]).Cast<int>().ToArray();

            string dataStr = "";
            for (int valueIndex = 0; valueIndex < dcData.Length; ++valueIndex)
            {
                dataStr += dcData[valueIndex].ToString();
                if (valueIndex < dcData.Length - 1)
                    dataStr += " ";
            }
            dataStr = "\"" + dataStr + "\"";

            return dataStr;
        }
        else if (objType == typeof(float[]) || objType == typeof(double[]))
        {
            // TODO: obv. should write these separately, but don't care to right now
            float[] dcData = null;
            if (objType == typeof(float[]))
                dcData = (obj as float[]).Cast<float>().ToArray();
            else if (objType == typeof(double[]))
                dcData = (obj as double[]).Cast<float>().ToArray();

            string dataStr = "";
            for (int valueIndex = 0; valueIndex < dcData.Length; ++valueIndex)
            {
                dataStr += dcData[valueIndex].ToString();
                if (valueIndex < dcData.Length - 1)
                    dataStr += " ";
            }
            dataStr = "\"" + dataStr + "\"";

            return dataStr;
        }
        else
        {
            throw new Exception("Don't know how to convert object of type " + objType.Name + " to string");
        }
    }
}
