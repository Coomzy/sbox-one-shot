
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

	[ConCmd]
	public static void dumpbinds2()
	{
		foreach (var action in Input.GetActions())
		{
			string boundButtonName = IGameInstance.Current.GetBind(action.Name, out bool isDefault, out bool isCommon);
			Log.Info($"The '{action.Name}' action is bound to '{boundButtonName}'{(isDefault ? " (Default binding)" : "")}{(isCommon ? " (Common binding)" : "")}");
		}
	}

	[Button]
	public void SetBind()
	{
		IGameInstance.Current.SetBind(actionName, buttonName);
		IGameInstance.Current.SaveBinds();
	}

	[ConCmd]
	public static void resetbinds2()
	{
		IGameInstance.Current.ResetBinds();
		IGameInstance.Current.SaveBinds();
	}

	[ConCmd]
	public static void getbind_Jump2()
	{
		string boundButtonName = IGameInstance.Current.GetBind("Jump", out bool isDefault, out bool isCommon);
		Log.Info($"The 'Jump' action is bound to '{boundButtonName}'{(isDefault ? " (Default binding)" : "")}{(isCommon ? " (Common binding)" : "")}");

	}

	[Button]
	public void GetBind()
	{
		string boundButtonName = IGameInstance.Current.GetBind(actionName, out bool isDefault, out bool isCommon);
		Log.Info($"actionName: {actionName}, boundButtonName: {boundButtonName}, isDefault: {isDefault}, isCommon: {isCommon}");
	}
}
