
public static class UIUtils
{
	public static Dictionary<PanelComponent, int> panelToDirtyIndex { get; set; } = new();

	public static void Dirty(this PanelComponent panelComponent)
	{
		if (!IsFullyValid(panelComponent))
		{
			return;
		}

		if (panelToDirtyIndex.ContainsKey(panelComponent))
		{
			panelToDirtyIndex[panelComponent]++;
		}
		else
		{
			panelToDirtyIndex[panelComponent] = 1;
		}
	}

	public static int GetDirty(this PanelComponent panelComponent)
	{
		if (!IsFullyValid(panelComponent))
		{
			return 0;
		}

		if (!panelToDirtyIndex.ContainsKey(panelComponent))
		{
			return 0;
		}

		return panelToDirtyIndex[panelComponent];
	}
}
