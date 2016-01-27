using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class ConfigFile
{
    /// <summary>
    /// Parameters.
    /// </summary>
    public IList<IParam> Params
    {
        get { return _params.AsReadOnly(); }
    }

    /// <summary>
    /// Configuration parameter definition interface.
    /// </summary>
    public interface IParam
    {
        /// <summary>
        /// Parmeter name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Parmeter type.
        /// </summary>
        Type Type { get; }
    }

    /// <summary>
    /// Configuration parameter definition implementation.
    /// </summary>
    private struct Param : IParam
    {
        /// <summary>
        /// <see cref="IParam.Name"/>
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// <see cref="IParam.Type"/>
        /// </summary>
        public Type Type
        {
            get { return _type; }
        }

        private string _name;
        private Type _type;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <param name="type">Parameter type</param>
        public Param(string name, Type type)
        {
            _name = name;
            _type = type;
        }
    }

    /// <summary>
    /// Indexer for accessing parameter values.
    /// </summary>
    public object this[string paramName]
    {
        get
        {
            if (!_params.Any(p => p.Name == paramName))
                throw new Exception("Undefined parameter " + paramName);

            if (!_paramValues.ContainsKey(paramName))
                throw new Exception("No value set for parameter " + paramName);

            return _paramValues[paramName];
        }

        set
        {
            if (!_params.Any(p => p.Name == paramName))
                throw new Exception("Undefined parameter " + paramName);

            var param = _params.FirstOrDefault(p => p.Name == paramName);
            if (value.GetType() != param.Type)
                throw new Exception("Incorrect data type for parameter " + paramName);

            _paramValues[paramName] = value;
        }
    }

    private List<IParam> _params = new List<IParam>();
    private Dictionary<string, object> _paramValues = new Dictionary<string, object>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public ConfigFile()
    {
    }

    /// <summary>
    /// Add parameter.
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="type">Parameter type</param>
    public void AddParam(string name, Type type)
    {
        if (_params.Any(a => a.Name == name))
            throw new ArgumentException(string.Format("Parameter {0} already defined", name), "name");

        _params.Add(new Param(name, type));
    }

    /// <summary>
    /// Remove parameter.
    /// </summary>
    /// <param name="name">Parameter name</param>
    public void RemoveParam(string name)
    {
        _paramValues.Remove(name);
        _params.RemoveAll(p => p.Name == name);
    }

    /// <summary>
    /// Remove all parameters.
    /// </summary>
    public void RemoveAllParams()
    {
        _paramValues.Clear();
        _params.Clear();
    }

    /// <summary>
    /// true if the specified parameter's value is set, false otherwise.
    /// </summary>
    /// <param name="paramName">Parameter name</param>
    /// <returns>true if the value is set, false otherwise</returns>
    public bool HasValue(string paramName)
    {
        return _paramValues.ContainsKey(paramName);
    }

    /// <summary>
    /// Get parameter value.
    /// </summary>
    /// <param name="paramName">Parameter name</param>
    public T GetValue<T>(string paramName)
    {
        if (!_params.Any(p => p.Name == paramName))
            throw new Exception("Undefined parameter " + paramName);

        var param = _params.FirstOrDefault(p => p.Name == paramName);
        if (typeof(T) != param.Type)
            throw new Exception("Incorrect data type for parameter " + paramName);

        if (!_paramValues.ContainsKey(paramName))
            throw new Exception("No value set for parameter " + paramName);

        return (T)_paramValues[paramName];
    }

    /// <summary>
    /// Set parameter value.
    /// </summary>
    /// <param name="paramName">Parameter name</param>
    public void SetValue<T>(string paramName, T value)
    {
        if (!_params.Any(p => p.Name == paramName))
            throw new Exception("Undefined parameter " + paramName);

        var param = _params.FirstOrDefault(p => p.Name == paramName);
        if (typeof(T) != param.Type)
            throw new Exception("Incorrect data type for parameter " + paramName);

        _paramValues[paramName] = value;
    }

    /// <summary>
    /// Read configuration from a file.
    /// </summary>
    /// <param name="path">Config file path</param>
    public void ReadFromFile(string path)
    {
        StreamReader reader = null;

        try
        {
            reader = new StreamReader(path);
            string line;
            int lineIndex = -1;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine().Trim();
                ++lineIndex;
                if (line == "")
                    continue;

                int commentStartIndex = line.IndexOf('#');
                line = commentStartIndex >= 0 ? line.Substring(0, commentStartIndex) : line;
                line = line.Trim();
                if (line.Length <= 0)
                    // Comment-only line, skip it
                    continue;
                else if (line.IndexOf('=') <= 0)
                    // Invalid line
                    throw new Exception(string.Format("Error reading {0} at line {1}", path, lineIndex));

                string[] lineElements = line.Split('=');
                string paramName = lineElements[0].Trim();
                string valueStr = lineElements[1].Trim();

                if (!_params.Any(p => p.Name == paramName))
                    throw new Exception(string.Format("Error reading {0} at line {1}: undefined parameter {2}",
                        path, lineIndex, paramName));

                try
                {
                    var param = _params.FirstOrDefault(p => p.Name == paramName);
                    this[paramName] = StringUtil.FromString(param.Type, valueStr);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Error reading {0} at line {1}: invalid value for parameter {2}",
                        path, lineIndex, paramName), ex);
                }
            }

            reader.Close();
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("Unable to read configuration file {0}", path), ex);
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
    }
}
