public static class GenerateBindConCmds
{
	[Menu("Editor", "Generate/bind ConCmds")]
	public static void OpenMyMenu()
	{
		EditorUtility.DisplayDialog("It worked!", "This is being called from your library's editor code!");
	}
}
