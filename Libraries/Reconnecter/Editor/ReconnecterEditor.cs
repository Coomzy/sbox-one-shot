using Sandbox;
using Sandbox.Internal;
using System.Diagnostics;
using System.IO;

public static class ReconnecterEditor
{
	public static string packageFolders => $"{Project.Current.Package.Org.Ident}/{Project.Current.Package.Ident}";
	public static string dataFolder => Editor.FileSystem.Root.GetFullPath($"/data/{packageFolders}");

	public static bool autoReconnectEnabled
	{
		get
		{
			return GlobalToolsNamespace.EditorCookie.Get<bool>("reconnecter_enabled", true);
		}
		set
		{
			GlobalToolsNamespace.EditorCookie.Set<bool>("reconnecter_enabled", value);
		}
	}
	public static bool allowLaunchInstance
	{
		get
		{
			return GlobalToolsNamespace.EditorCookie.Get<bool>("reconnecter_allowLaunchInstance", true);
		}
		set
		{
			GlobalToolsNamespace.EditorCookie.Set<bool>("reconnecter_allowLaunchInstance", value);
		}
	}

	static ReconnecterEditor()
	{
		ReconnecterSystem.RegisterOnRequestWriteSession(CreateSessionText);
	}

	public static void CreateSessionText(bool force = false)
	{
		if (!autoReconnectEnabled && !force)
		{
			return;
		}

		string filePath = $"{dataFolder}/{ReconnecterSystem.SESSION_FILE_PATH}";

		string destinationDirectory = Path.GetDirectoryName(filePath);
		if (!Directory.Exists(destinationDirectory))
		{
			Directory.CreateDirectory(destinationDirectory);
		}

		File.WriteAllText(filePath, System.DateTime.UtcNow.ToString());
		//Log.Info($"CreateSessionText() filePath: {filePath}");
	}

	[Event("scene.play", Priority = int.MinValue)]
	public static void ScenePlay()
	{		
		ReconnecterSystem.OnPlayInEditor();

		if (!autoReconnectEnabled || !allowLaunchInstance)
		{
			return;
		}

		Process[] processes = Process.GetProcessesByName("sbox");
		bool isProcessRunning = processes.Length > 0;

		if (isProcessRunning)
		{
			return;
		}
		SpawnProcess();
	}

	public static void SpawnProcess()
	{
		var p = new Process();
		p.StartInfo.FileName = "sbox.exe";
		p.StartInfo.WorkingDirectory = System.Environment.CurrentDirectory;
		p.StartInfo.CreateNoWindow = true;
		p.StartInfo.RedirectStandardOutput = true;
		p.StartInfo.RedirectStandardError = true;
		p.StartInfo.UseShellExecute = false;

		p.StartInfo.ArgumentList.Add("-joinlocal");

		// This doesn't seem to work because it doesn't use Steam's Launch Options?
		p.StartInfo.ArgumentList.Add("-sw");

		p.Start();
	}
}