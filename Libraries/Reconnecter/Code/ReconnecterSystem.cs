using System;
using Sandbox;
using System.Linq;
using Sandbox.Network;

public class ReconnecterSystem : GameObjectSystem
{
	public static ReconnecterComponent instance { get; set; }
	public static DateTime? startTime { get; set; }

	public const string SESSION_FILE_PATH = "reconnecter_session.txt";

	public ReconnecterSystem(Scene scene) : base(scene)
	{
		if (!Application.IsDebug)
		{
			return;
		}

		if (startTime == null)
		{
			startTime = DateTime.UtcNow;
		}

		if (Game.IsEditor)
		{
			return;
		}

		Listen(Stage.FinishUpdate, 0, FinishUpdate, "ReconnecterSystem.FinishUpdate");
	}

	// Called from ReconnecterEditor, don't know a better way
	// It's called after ReconnecterSystem()
	public static void OnPlayInEditor()
	{
		if (instance != null)
		{
			instance.Destroy();
			instance = null;
		}

		if (instance == null)
		{
			var newGO = new GameObject(true);
			instance = newGO.Components.Create<ReconnecterComponent>();
			newGO.Flags = GameObjectFlags.DontDestroyOnLoad;
		}

		startTime = null;
	}

	public static void OnStartedHosting()
	{
		if (!Game.IsEditor)
        {
			return;
		}
		TryRequestWriteSession();
	}

	void FinishUpdate()
	{
		if (!FileSystem.Data.FileExists(SESSION_FILE_PATH))
		{
			return;
		}

		var sessionText = FileSystem.Data.ReadAllText(SESSION_FILE_PATH);
		if (!DateTime.TryParse(sessionText, out var lastSessionStart))
		{
			Log.Info($"Reconnecter failed to parse session text: {sessionText}");
			return;
		}

		if (lastSessionStart <= startTime.Value)
		{
			//Log.Info($"You're in the most active session");
			return;
		}

		Networking.Disconnect();
		Networking.Connect("local");
	}

	public static event Action<bool> OnRequestWriteSession;
	public static void RegisterOnRequestWriteSession(Action<bool> handler)
	{
		if (OnRequestWriteSession != null && OnRequestWriteSession.GetInvocationList().Contains(handler))
		{
			return;
		}

		OnRequestWriteSession += handler;
	}

	public static void TryRequestWriteSession()
	{
		OnRequestWriteSession?.Invoke(false);
	}

	[ConCmd("reconnect_clients")]
	public static void RequestWriteSession()
	{
		OnRequestWriteSession?.Invoke(true);
	}
}

public class ReconnecterComponent : Component, Component.INetworkListener
{
	public void OnActive(Connection channel)
	{
		if (channel == null || !channel.IsHost)
		{
			return;	
		}

		ReconnecterSystem.OnStartedHosting();
	}
}