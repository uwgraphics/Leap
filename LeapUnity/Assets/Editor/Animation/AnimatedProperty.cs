using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generic abstract class representing an animated property.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class AnimatedProperty<T>
{
    /// <summary>
    /// Source object containing the property.
    /// </summary>
    public object SrcObject
    {
        get;
        private set;
    }

    /// <summary>
    /// Property name.
    /// </summary>
    public string Name
    {
        get;
        private set;
    }

    /// <summary>
    /// Property value.
    /// </summary>
    public T Value
    {
        get;
        private set;
    }

    /// <summary>
    /// First-order forward difference of the property value.
    /// </summary>
    public T Diff
    {
        get;
        private set;
    }

    /// <summary>
    /// Second-order forward difference of the property value.
    /// </summary>
    public T Diff2
    {
        get;
        private set;
    }

    protected T _valueTm1;
    protected T _valueTm2;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="srcObject">Source object containing the property</param>
    /// <param name="name">Property name</param>
    public AnimatedProperty(object srcObject, string name)
    {
        SrcObject = srcObject;
        Name = name;
    }

    /// <summary>
    /// Update the property value.
    /// </summary>
    public virtual void Update()
    {
        _valueTm2 = _valueTm1;
        _valueTm1 = Value;
        Value = (T)SrcObject.GetType().GetProperty(Name).GetValue(SrcObject, null);
        Diff = _Sub(Value, _valueTm1);
        Diff2 = _Add(_Sub(Value, _Add(_valueTm1, _valueTm1)), _valueTm2);
    }

    protected abstract T _Add(T v1, T v2);
    protected abstract T _Sub(T v1, T v2);
}

/// <summary>
/// Class representing an animated float property.
/// </summary>
public class AnimatedFloatProperty : AnimatedProperty<float>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="srcObject">Source object containing the property</param>
    /// <param name="name">Property name</param>
    public AnimatedFloatProperty(object srcObject, string name)
        : base(srcObject, name)
    {
    }

    protected override float _Add(float v1, float v2)
    {
        return v1 + v2;
    }

    protected override float _Sub(float v1, float v2)
    {
        return v1 - v2;
    }
}
