using Editor;
using System.Reflection;

[EditorTool]
[Title("Debuggin")]
[Icon("adb")]
public class ExtraDebugEditorTool : EditorTool
{
	public override void OnUpdate()
	{
		if (Game.IsPlaying)
			return;

		/*using (Gizmo.Scope())
		{
			Debuggin.DrawGizmos();
			Debuggin.DrawScreenMessages();
		}*/
		Debuggin.DrawGizmos();
		Debuggin.DrawScreenMessages();
	}
}

public static class ExtraDebugEditor
{
	[EditorEvent.Frame(Priority = 1)]
	public static void OnUpdate()
	{
		//Log.Info($"ExtraDebugEditor::OnUpdate()");
		/*using (Gizmo.Scope())
		{
			Debuggin.DrawGizmos();
		}
		using (SceneViewportWidget.LastSelected.GizmoInstance)
		{
			//Debuggin.DrawGizmos();
		}*/
		//Debuggin.DrawGizmos();
	}
}