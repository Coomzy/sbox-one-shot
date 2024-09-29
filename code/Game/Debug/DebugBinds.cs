
public class DebugBinds : Component
{
	[Property] public string actionName {  get; set; } = "Duck_Alt";
	[Property] public string buttonName {  get; set; }

	// TODO: Figure out codegen for creating bind convars dynamically
	protected override void OnAwake()
	{

	}

	protected override void OnUpdate()
	{
		if (Input.Down(actionName))
		{
			Log.Info($"{actionName} is down!");
		}
	}

	[Button]
	public void DumpActions()
	{
		foreach (var action in Input.GetActions())
		{
			Log.Info($"action: {action.Name}, {action.KeyboardCode}, {action.GroupName}");
		}
	}

	[Button]
	public void SetBind()
	{
		IGameInstance.Current.SetBind(actionName, buttonName);
		IGameInstance.Current.SaveBinds();
	}

	[Button]
	public void GetBind()
	{
		string boundButtonName = IGameInstance.Current.GetBind(actionName, out bool isDefault, out bool isCommon);
		Log.Info($"actionName: {actionName}, boundButtonName: {boundButtonName}, isDefault: {isDefault}, isCommon: {isCommon}");
	}

	[ConCmd("setbind_duck_alt")]
	public static void SetBind_Duck_Alt(string buttonName)
	{
		IGameInstance.Current.SetBind("Duck_Alt", buttonName);
		IGameInstance.Current.SaveBinds();
	}
}
