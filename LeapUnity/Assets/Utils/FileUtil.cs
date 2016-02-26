using System;
using System.IO;

/// <summary>
/// Utility methods for working with files and directories
/// </summary>
public static class FileUtil
{
    /// <summary>
    /// Delete all files in a directory.
    /// </summary>
    /// <param name="dir">Directory</param>
    public static void DeleteAllFilesInDirectory(string dir)
    {
        var dirInfo = new DirectoryInfo(dir);
        foreach (var fileInfo in dirInfo.GetFiles())
            fileInfo.Delete();
    }
}
