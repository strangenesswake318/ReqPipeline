using System;
using System.IO;
using System.Linq;

namespace ReqPipeline.Core.Utils;

public static class ResourceLocator
{
    // 指定した名前のフォルダを、ソリューション全体から探し出す
    public static string FindDirectory(string targetFolderName)
    {
        var rootDir = GetSolutionRoot();
        
        // 大文字小文字を無視して検索し、bin や obj フォルダの中身は除外する
        var foundDir = rootDir.GetDirectories("*", SearchOption.AllDirectories)
            .Where(d => d.Name.Equals(targetFolderName, StringComparison.OrdinalIgnoreCase))
            .Where(d => !d.FullName.Contains("/bin/") && !d.FullName.Contains("/obj/"))
            .FirstOrDefault();

        return foundDir?.FullName ?? targetFolderName;
    }

    // 指定した名前のファイルを、ソリューション全体から探し出す
    public static string FindFile(string targetFileName)
    {
        var rootDir = GetSolutionRoot();

        var foundFile = rootDir.GetFiles(targetFileName, SearchOption.AllDirectories)
            .Where(f => !f.FullName.Contains("/bin/") && !f.FullName.Contains("/obj/"))
            .FirstOrDefault();

        return foundFile?.FullName ?? targetFileName;
    }

    // .git や .sln がある場所を「プロジェクトの頂上（ルート）」として特定する
    private static DirectoryInfo GetSolutionRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current.Parent != null)
        {
            if (current.GetDirectories(".git").Any() || current.GetFiles("*.sln").Any())
            {
                return current;
            }
            current = current.Parent;
        }
        return new DirectoryInfo(Directory.GetCurrentDirectory()); // 見つからなければ現在地
    }
}