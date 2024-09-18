
using System;

[Group("GMF")]
public class Projectile : Component, Component.INetworkSpawn
{
	[Group("Runtime"), Property, ReadOnly] public Equipment owner {  get; set; }

	protected override void OnStart()
	{

	}

	public void OnNetworkSpawn(Connection connection)
	{
		if (IsProxy)
		{
			return;
		}
	}
}
