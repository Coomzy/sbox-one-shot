global using Editor;
global using System.Collections.Generic;
global using System.Linq;
global using Sandbox;
using System;
using System.IO;
using Sandbox.Internal;

public static class UIImporter
{
	public static string stagingFolder => $@"{Project.Current.RootDirectory}\staging\easysaving";
	public static string hasOfferedLibraryImportFilePath => $@"{stagingFolder}\hasOfferedLibraryImport.txt";
	public static PopupWindow confirm = null;
	public static bool hasCheckedForFile = false;
	public static bool waitingForConfirmToGo = false;

	//[Event("localaddons.changed")]
	[EditorEvent.Frame]
	public static void CheckCookiePrompt()
	{
		if (waitingForConfirmToGo && confirm == null)
		{
			waitingForConfirmToGo = false;
			OpenRestartPopup();
		}

		CheckFileExists();
	}

	public static void CheckFileExists()
	{
		if (hasCheckedForFile)
		{
			return;
		}
		hasCheckedForFile = true;

		if (File.Exists(hasOfferedLibraryImportFilePath))
		{
			OpenRestartPopup();
			return;
		}

		File.Create(hasOfferedLibraryImportFilePath);

		OpenMyMenu();
	}

	public static void OpenMyMenu()
	{
		confirm = new PopupWindow(
			"Import UI from Library? (Easy Saving)",
			"UI doesn't work in libraries, so if you want the options screens that comes with this library you need to import it into your project",
			"No",
			new Dictionary<string, Action>()
			{
				{ "Yes", () => ImportDisabledLibraryFiles() }
			}
		);

		confirm.DeleteOnClose = true;
		confirm.Show();
		waitingForConfirmToGo = true;
	}

	public static void OpenRestartPopup()
	{
		var confirm = new PopupWindow(
			"Please restart editor after installing library",
			"For some reason you ",
			"Ok",
			new Dictionary<string, Action>()
			{

			}
		);

		confirm.Show();
	}

	[Menu("Editor", "Library/Easy Saving/Import Disabled Library Files")]
	public static void ImportDisabledLibraryFiles()
	{
		string libraryFolder = new System.IO.DirectoryInfo(Editor.FileSystem.Content.GetFullPath("/")).FullName.Replace("Assets", "");
		string projectFolder = new System.IO.DirectoryInfo(Editor.FileSystem.ProjectSettings.GetFullPath("/")).FullName.Replace("ProjectSettings", "");

		string source = $"{libraryFolder}code";
		string target = @$"{projectFolder}code\libraries";

		string[] files = Directory.GetFiles(source, "*.DISABLED", SearchOption.AllDirectories);

		Log.Info($"Moving .DISABLED files from source: {source}");

		foreach (string file in files)
		{
			string relativePath = Path.GetRelativePath(source, file);
			string targetPath = Path.Combine(target, relativePath);

			targetPath = RemoveDisabledExtension(targetPath);

			Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

			if (File.Exists(targetPath))
			{
				if ((File.GetAttributes(targetPath) & FileAttributes.ReadOnly) != 0)
				{
					File.SetAttributes(targetPath, FileAttributes.Normal);
				}
				File.Delete(targetPath);
			}
			File.Copy(file, targetPath);

			Log.Info($"Moved: {file} -> {targetPath}");
		}

		Log.Info("All files have been moved successfully.");
	}

	static string RemoveDisabledExtension(string filePath)
	{
		if (filePath.EndsWith(".DISABLED", StringComparison.OrdinalIgnoreCase))
		{
			return filePath.Substring(0, filePath.Length - ".DISABLED".Length);
		}
		return filePath;
	}
}