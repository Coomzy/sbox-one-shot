using Sandbox;
using Sandbox.Internal;
using System.Diagnostics;
using System.IO;

public static class LaunchGameEditor
{
	public static string packageFolders => $"{Project.Current.Package.Org.Ident}.{Project.Current.Package.Ident}";

	[Menu("Editor", "Launch/Packaged Game")]
	public static void SpawnProcess()
	{
		var p = new Process();
		p.StartInfo.FileName = "sbox.exe";
		p.StartInfo.WorkingDirectory = System.Environment.CurrentDirectory;
		p.StartInfo.CreateNoWindow = true;
		p.StartInfo.RedirectStandardOutput = true;
		p.StartInfo.RedirectStandardError = true;
		p.StartInfo.UseShellExecute = false;

		var fullIndent = $"{Project.Current.Package.Org.Ident}.{Project.Current.Package.Ident}";
		p.StartInfo.ArgumentList.Add($"-rungame {fullIndent}");

		// This doesn't seem to work because it doesn't use Steam's Launch Options?
		//p.StartInfo.ArgumentList.Add("-sw");

		p.Start();
	}
}