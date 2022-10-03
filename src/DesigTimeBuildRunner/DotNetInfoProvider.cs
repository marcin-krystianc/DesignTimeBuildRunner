using System.Diagnostics;

namespace DesigTimeBuildRunner;

public static class DotNetInfoProvider
{
    public static (string Directory, string Executeable) GetDotNetHostPath()
    {
        using var process = new Process();
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.ArgumentList.Add("--info");
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        process.StartInfo.CreateNoWindow = true; //not diplay a windows
        process.Start();

        var exe = process.MainModule.FileName;
        return (Path.GetDirectoryName(exe), Path.GetFileName(exe));
    }

}