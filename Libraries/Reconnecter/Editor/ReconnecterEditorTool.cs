using Sandbox;
using Editor;

public class ReconnecterBar : ToolbarGroup
{
	[Event("tools.headerbar.build", Priority = 150)]
	public static void OnBuildHeaderToolbar(HeadBarEvent e)
	{
		e.RightCenter.Add(new ReconnecterBar(null));
		e.RightCenter.AddSpacingCell(8);
	}

	public ReconnecterBar(Widget parent) : base(parent, "Reconnecter", null)
	{
		ToolTip = "Auto Reconnect Clients";
	}

	public override void Build()
	{
		AddToggleButton("Auto Reconnect Clients", "autorenew", () => ReconnecterEditor.autoReconnectEnabled, SetAutoReconnect);
		AddToggleButton("Allow Instance Launching", "person_add", () => ReconnecterEditor.allowLaunchInstance, SetAllowLaunchInstance);
		AddButton("Force Reconnect Clients", "group", ForceAutoReconnect);
	}

	public void SetAutoReconnect(bool enabled)
	{
		ReconnecterEditor.autoReconnectEnabled = enabled;
	}

	public void SetAllowLaunchInstance(bool enabled)
	{
		ReconnecterEditor.allowLaunchInstance = enabled;
	}

	public void ForceAutoReconnect()
	{
		ReconnecterEditor.CreateSessionText(true);
	}
}