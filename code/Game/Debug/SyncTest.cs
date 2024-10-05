
using Sandbox.Network;
using System;
using System.Net.Quic;
using System.Threading.Tasks;

public class SyncTest : Component
{
	[Property, HostSync, Sync] public PlayerInfo playerInfo { get; set; }
	[Property, HostSync, Sync] public SyncTest syncTest { get; set; }

	protected override void OnAwake()
	{
		Log.Info($"SyncTest::OnAwake() {GameObject}");
	}

	protected override void OnStart()
	{
		Log.Info($"SyncTest::OnStart() {GameObject}");
	}

	protected override Task OnLoad()
	{
		Log.Info($"SyncTest::OnLoad() {GameObject}");
		return base.OnLoad();
	}

	[ConCmd]
	public static void do_sync_test()
	{
		spawn_synctests(Connection.Local.Id);
	}

	[ConCmd]
	public static void do_sync_test_host()
	{
		do_sync_test_host_server(Connection.Local.Id);
	}

	[Broadcast]
	public static void do_sync_test_host_server(Guid connection)
	{
		if (Networking.IsHost)
			return;

		spawn_synctests(connection);
	}

	public static void spawn_synctests(Guid connectionID)
	{
		var connection = Connection.Find(connectionID);

		var gameObject = new GameObject(true, "Sync Test");
		var syncTest = gameObject.AddComponent<SyncTest>();
		syncTest.playerInfo = PlayerInfo.local;
		gameObject.NetworkSpawn(connection);

		var gameObject2 = new GameObject(true, "Sync Test 2");
		var syncTest2 = gameObject2.AddComponent<SyncTest>();
		syncTest2.playerInfo = PlayerInfo.local;
		syncTest2.syncTest = syncTest;
		gameObject2.NetworkSpawn(connection);
	}
}
