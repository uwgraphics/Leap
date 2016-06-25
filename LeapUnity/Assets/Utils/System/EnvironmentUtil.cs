using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Utility class for working with the system environment.
/// </summary>
public static class EnvironmentUtil
{
    /// <summary>
    /// Get the value of the specified command line argument.
    /// </summary>
    /// <typeparam name="T">Argument type</typeparam>
    /// <param name="argName">Argument name</param>
    /// <returns>Argument value</returns>
    public static T GetCommandLineArgValue<T>(string argName)
    {
        var args = Environment.GetCommandLineArgs();
        for (int argIndex = 0; argIndex < args.Length; ++argIndex)
        {
            if (args[argIndex].ToLower() == "-" + argName.ToLower() && argIndex + 1 < args.Length)
            {
                return (T)Convert.ChangeType(args[argIndex + 1], typeof(T));
            }
        }

        return default(T);
    }
}
