using System;
using System.IO;
using System.Linq;

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

    /// <summary>
    /// Make file path string out of a directory path and filename.
    /// </summary>
    /// <param name="dirName">Directory path</param>
    /// <param name="filename">Filename</param>
    /// <returns>File path</returns>
    public static string MakeFilePath(string dir, string filename)
    {
        dir = dir.Length > 0 && !"\\/".Any(c => c == dir[dir.Length - 1]) ?
            dir + "/" : dir;
        return dir + filename;
    }
}
