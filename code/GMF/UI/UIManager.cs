
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Network;
using System;
using static Sandbox.Gizmo;

public class UIManager : Component, Component.INetworkListener
{
	public static UIManager instance { get; private set; }

	[Property] public RoundStateWidget roundState { get; private set; }
	[Property] public ScoreboardWidget scoreboard { get; private set; }
	[Property] public RankUpWidget rankUpWidget { get; private set; }
	[Property] public RoundCountWidget roundCountWidget { get; private set; }
	[Property] public CrosshairBuilder crosshairBuilder { get; private set; }

	protected override void OnAwake()
	{
		instance = this;

		crosshairBuilder.Enabled = false;
	}

	protected override void OnDestroy()
	{
		instance = null;
		base.OnDestroy();
	}

	protected override void OnStart()
	{
		base.OnStart();
	}
}
