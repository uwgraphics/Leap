using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Class which holds data from a CSV file.
/// </summary>
public class CSVData
{
    /// <summary>
    /// Data attributes.
    /// </summary>
    public IList<IAttribute> Attributes
    {
        get { return _attributes.AsReadOnly(); }
    }

    /// <summary>
    /// Number of data rows.
    /// </summary>
    public int NumberOfRows
    {
        get { return _data.Count; }
    }

    /// <summary>
    /// Indexer for accessing data rows.
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <returns></returns>
    public IDataRow this[int rowIndex]
    {
        get { return _data[rowIndex]; }
    }

    /// <summary>
    /// Data attribute definition interface.
    /// </summary>
    public interface IAttribute
    {
        /// <summary>
        /// Attribute name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Attribute type.
        /// </summary>
        Type Type { get;  }
    }

    /// <summary>
    /// Data attribute definition implementation.
    /// </summary>
    private struct Attribute : IAttribute
    {
        /// <summary>
        /// <see cref="IAttribute.Name"/>
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// <see cref="IAttribute.Type"/>
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
        /// <param name="name">Attribute name</param>
        /// <param name="type">Attribute type</param>
        public Attribute(string name, Type type)
        {
            _name = name;
            _type = type;
        }
    }

    /// <summary>
    /// Data row interface.
    /// </summary>
    public interface IDataRow
    {
        /// <summary>
        /// Indexer for accessing data values.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        object this[int index] { get; set; }

        /// <summary>
        /// Get data value at index.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="index">Data value index</param>
        /// <returns>Data value</returns>
        T GetValue<T>(int index);

        /// <summary>
        /// Set data value at index
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="index">Data value index</param>
        /// <param name="value">Data value</param>
        void SetValue<T>(int index, T value);
    }

    /// <summary>
    /// Data row implementation.
    /// </summary>
    private struct DataRow : IDataRow
    {
        private CSVData _owner;
        private object[] _data;

        /// <summary>
        /// <see cref="IDataRow.this[]"/>
        /// </summary>
        public object this[int index]
        {
            get
            {
                return _data[index];
            }

            set
            {
                if (value.GetType() != _owner.Attributes[index].Type)
                    throw new Exception("Incorrect data type");

                _data[index] = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owning CSV data object</param>
        /// <param name="data">Data values</param>
        public DataRow(CSVData owner, object[] data)
        {
            _owner = owner;
            _data = data;
        }

        /// <summary>
        /// <see cref="IDataRow.GetValue<T>"/>
        /// </summary>
        public T GetValue<T>(int index)
        {
            if (typeof(T) != _owner.Attributes[index].Type)
                throw new Exception("Incorrect data type");

            return (T)_data[index];
        }

        /// <summary>
        /// <see cref="IDataRow.SetValue<T>"/>
        /// </summary>
        public void SetValue<T>(int index, T value)
        {
            if (typeof(T) != _owner.Attributes[index].Type)
                throw new Exception("Incorrect data type");

            _data[index] = value;
        }
    }

    private List<IAttribute> _attributes = new List<IAttribute>();
    private Dictionary<string, int> _attributeIndexes = new Dictionary<string, int>();
    private List<IDataRow> _data = new List<IDataRow>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public CSVData()
    {
    }

    /// <summary>
    /// Add data attribute.
    /// </summary>
    /// <param name="name">Attribute name</param>
    /// <param name="type">Attribute type</param>
    public void AddAttribute(string name, Type type)
    {
        if (_attributes.Any(a => a.Name == name))
            throw new ArgumentException(string.Format("Attribute {0} already defined", name), "name");

        _data.Clear();
        _attributes.Add(new Attribute(name, type));
        _attributeIndexes[name] = _attributes.Count - 1;
    }

    /// <summary>
    /// Remove data attribute
    /// </summary>
    /// <param name="name">Attribute name</param>
    public void RemoveAttribute(string name)
    {
        _data.Clear();
        _attributes.RemoveAll(a => a.Name == name);
    }

    /// <summary>
    /// Remove all data attributes.
    /// </summary>
    public void RemoveAllAttributes()
    {
        _data.Clear();
        _attributes.Clear();
    }

    /// <summary>
    /// Get data attribute index.
    /// </summary>
    /// <param name="name">Attribute name</param>
    /// <returns>Attribute index</returns>
    public int GetAttributeIndex(string name)
    {
        return _attributeIndexes[name];
    }

    /// <summary>
    /// Add a row of data.
    /// </summary>
    /// <param name="values">Data values</param>
    public void AddData(params object[] values)
    {
        if (values.Length != Attributes.Count)
            throw new ArgumentException("Number of data values does not match the number of defined attributes", "values");

        for (int attributeIndex = 0; attributeIndex < Attributes.Count; ++attributeIndex)
        {
            if (Attributes[attributeIndex].Type != values[attributeIndex].GetType())
                throw new ArgumentException("Data value's type does not match the defined attribute type", "values");
        }

        _data.Add(new DataRow(this, values));
    }

    /// <summary>
    /// Remove a row of data.
    /// </summary>
    /// <param name="index">Data row index</param>
    public void RemoveData(int index)
    {
        _data.RemoveAt(index);
    }

    /// <summary>
    /// Remove all data.
    /// </summary>
    public void RemoveAllData()
    {
        _data.Clear();
    }
    
    /// <summary>
    /// Read CSV data from a file.
    /// </summary>
    /// <param name="path">CSV file path</param>
    public void ReadFromFile(string path)
    {
        try
        {
            var reader = new StreamReader(path);
            bool firstLine = true;
            string line = "";
            string[] lineElements = null;

            while (!reader.EndOfStream && (line = reader.ReadLine()) != "")
            {
                if (line[0] == '#')
                {
                    // Comment line, skip
                    continue;
                }
                else if (firstLine)
                {
                    // Load attribute names from first line
                    firstLine = false;
                    lineElements = line.Split(",".ToCharArray());

                    if (Attributes.Count == 0)
                    {
                        // No attributes are defined, so define them and assume they are all string
                        for (int attributeIndex = 0; attributeIndex < lineElements.Length; ++attributeIndex)
                            AddAttribute(lineElements[attributeIndex], typeof(string));
                    }
                    else
                    {
                        // Attributes already defined, just check their number of names

                        if (lineElements.Length != Attributes.Count)
                            throw new Exception("Number of attributes in the CSV file does not match the number of defined attributes");

                        for (int attributeIndex = 0; attributeIndex < lineElements.Length; ++attributeIndex)
                            if (lineElements[attributeIndex] != Attributes[attributeIndex].Name)
                                throw new Exception("Attribute in the CSV file is not defined: " + lineElements[attributeIndex]);
                    }
                }
                else
                {
                    // Read data row
                    lineElements = line.Split(",".ToCharArray());
                    if (lineElements.Length != Attributes.Count)
                        throw new Exception("Number of vales in the data row does not match the number of attributes");

                    // Parse data values
                    object[] data = new object[Attributes.Count];
                    for (int attributeIndex = 0; attributeIndex < Attributes.Count; ++attributeIndex)
                    {
                        Type dataType = Attributes[attributeIndex].Type;
                        data[attributeIndex] = _ParseData(dataType, lineElements[attributeIndex]);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("Unable to read CSV file {0}", path), ex);
        }
    }

    /// <summary>
    /// Write CSV data to a file.
    /// </summary>
    /// <param name="path">CSV file path</param>
    public void WriteToFile(string path, bool writeAttributeNames = true)
    {
        try
        {
            var writer = new StreamWriter(path);
            var lineBuilder = new StringBuilder();

            if (writeAttributeNames)
            {
                // Write attribute names
                for (int attributeIndex = 0; attributeIndex < Attributes.Count; ++attributeIndex)
                {
                    var attribute = Attributes[attributeIndex];
                    lineBuilder.Append(attribute.Name);
                    if (attributeIndex < Attributes.Count - 1)
                        lineBuilder.Append(",");
                }
                writer.WriteLine(lineBuilder);
                lineBuilder.Length = 0;
            }

            // Write data
            for (int index = 0; index < NumberOfRows; ++index)
            {
                for (int attributeIndex = 0; attributeIndex < Attributes.Count; ++attributeIndex)
                {
                    var attribute = Attributes[attributeIndex];
                    lineBuilder.Append(_DataToString(attribute.Type, this[index][attributeIndex]));
                    if (attributeIndex < Attributes.Count - 1)
                        lineBuilder.Append(",");
                }
                writer.WriteLine(lineBuilder);
                lineBuilder.Length = 0;
            }

            writer.Close();
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("Unable to write CSV file {0}", path), ex);
        }
    }

    /// <summary>
    /// Utility function for quickly writing an array of data to a CSV file.
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    /// <param name="data">Data array</param>
    /// <param name="path">CSV file path</param>
    public static void WriteDataToFile<T>(T[] data, string path)
    {
        CSVData csvData = new CSVData();
        csvData.AddAttribute("Attribute", typeof(T));
        for (int index = 0; index < data.Length; ++index)
            csvData.AddData(data[index]);
        csvData.WriteToFile(path, false);
    }

    private object _ParseData(Type dataType, string dataStr)
    {
        if (dataType == typeof(string))
        {
            return dataStr;
        }
        else if (dataType == typeof(int) || dataType == typeof(uint) ||
            dataType == typeof(short) || dataType == typeof(ushort) ||
            dataType == typeof(long) || dataType == typeof(ulong))
        {
            // TODO: obv. should parse these separately, but don't care to right now
            return int.Parse(dataStr);
        }
        else if (dataType == typeof(float) || dataType == typeof(double))
        {
            // TODO: obv. should parse these separately, but don't care to right now
            return float.Parse(dataStr);
        }
        else if (dataType == typeof(bool))
        {
            return bool.Parse(dataStr);
        }
        else if (dataType == typeof(UnityEngine.Vector3))
        {
            var dataValues = dataStr.Trim('\"').Split(' ');
            if (dataValues.Length != 3)
                throw new Exception("Invalid Vector3 string ");
            var data = new UnityEngine.Vector3();
            data.x = float.Parse(dataValues[0]);
            data.y = float.Parse(dataValues[1]);
            data.z = float.Parse(dataValues[2]);

            return data;
        }
        else if (dataType == typeof(UnityEngine.Vector2))
        {
            var dataValues = dataStr.Trim('\"').Split(' ');
            if (dataValues.Length != 2)
                throw new Exception("Invalid Vector2 string ");
            var data = new UnityEngine.Vector2();
            data.x = float.Parse(dataValues[0]);
            data.y = float.Parse(dataValues[1]);

            return data;
        }
        else if (dataType == typeof(string[]))
        {
            var dataValues = dataStr.Trim('\"').Split(' ');
            return dataValues;
        }
        else
        {
            throw new Exception("Don't know how to parse strings of type " + dataType.Name);
        }
    }

    private string _DataToString(Type dataType, object data)
    {
        if (dataType == typeof(string))
        {
            return (string)data;
        }
        else if (dataType == typeof(int) || dataType == typeof(uint) ||
            dataType == typeof(short) || dataType == typeof(ushort) ||
            dataType == typeof(long) || dataType == typeof(ulong))
        {
            // TODO: obv. should write these separately, but don't care to right now
            return ((int)data).ToString();
        }
        else if (dataType == typeof(float) || dataType == typeof(double))
        {
            // TODO: obv. should write these separately, but don't care to right now
            return ((float)data).ToString();
        }
        else if (dataType == typeof(bool))
        {
            return ((bool)data).ToString();
        }
        else if (dataType == typeof(UnityEngine.Vector3))
        {
            UnityEngine.Vector3 dcData = (UnityEngine.Vector3)data;
            string dataStr = "\"" + dcData.x + " " + dcData.y + " " + dcData.z + "\"";
            return dataStr;
        }
        else if (dataType == typeof(UnityEngine.Vector2))
        {
            UnityEngine.Vector2 dcData = (UnityEngine.Vector2)data;
            string dataStr = "\"" + dcData.x + " " + dcData.y + "\"";
            return dataStr;
        }
        else if (dataType == typeof(string[]))
        {
            string[] dcData = (string[])data;
            string dataStr = "";
            for (int valueIndex = 0; valueIndex < dcData.Length; ++valueIndex)
            {
                dataStr += dcData[valueIndex];
                if (valueIndex < dcData.Length - 1)
                    dataStr += " ";
            }
            dataStr = "\"" + dcData + "\"";
            return dataStr;
        }
        else
        {
            throw new Exception("Don't know how to convert data of type " + dataType.Name + " to string");
        }
    }
}
