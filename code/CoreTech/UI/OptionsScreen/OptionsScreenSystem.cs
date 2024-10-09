using System;
using Sandbox;
using System.Linq;
using Sandbox.Network;
using Sandbox.Audio;

public class OptionsScreenSystem : GameObjectSystem
{
	public OptionsScreenSystem(Scene scene) : base(scene)
	{
		Listen(Stage.FinishUpdate, 0, FinishUpdate, "OptionsScreen.FinishUpdate");
	}

	void FinishUpdate()
	{
		if (Input.Pressed("options_screen"))
		{
			ToggleOptions();
		}
	}

	[ConCmd("open_options")]
	public static void OpenOptions()
	{
		var inst = Game.ActiveScene.Components.GetInDescendants<OptionsScreen>(true);
		if (inst != null)
		{
			inst.Enabled = true;
		}
		else
		{
			UserPrefs.BuildUI();
			var optionsScreenGO = Game.ActiveScene.CreateObject();
			optionsScreenGO.Name = "Options Screen";
			var screenPanel = optionsScreenGO.Components.Create<ScreenPanel>();
			var optionsScreen = optionsScreenGO.Components.Create<OptionsScreen>();
		}
	}

	[ConCmd("toggle_options")]
	public static void ToggleOptions()
	{
		var inst = Game.ActiveScene.Components.GetInDescendants<OptionsScreen>(true);
		if (inst != null)
		{
			inst.Enabled = !inst.Enabled;
		}
		else
		{
			OpenOptions();
		}
	}
}