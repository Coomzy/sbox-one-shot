
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Network;
using System;
using static Sandbox.Gizmo;

// TODO: I don't like this, move an Interface event based system
public partial class UIManager : Component, Component.INetworkListener
{
	public static UIManager instance { get; private set; }

	[Property] public RoundStateWidget roundStateUpWidget { get; private set; }
	[Property] public GameResultsWidget gameResultsWidget { get; private set; }
	[Property] public ScoreboardWidget scoreboardUpWidget { get; private set; }
	[Property] public RoundCountWidget roundCountWidget { get; private set; }
	[Property] public KillFeedWidget killFeedUpWidget { get; private set; }

	protected override void OnAwake()
	{
		instance = this;
	}

	protected override void OnDestroy()
	{
		instance = null;
		base.OnDestroy();
	}
}