
using Sandbox;
using System;
using System.Diagnostics;

[Group("GMF")]
public class HarpoonSpearPlayerCollision : Projectile, Component.ICollisionListener
{
	void ICollisionListener.OnCollisionStart(Sandbox.Collision collision)
	{
		Log.Info($"HarpoonSpearPlayerCollision() collision: {collision.Other.GameObject.Name}");

		if (collision.Contact.Point == Vector3.Zero)
		{
			Log.Info("HarpoonSpearPlayerCollision Collision point at world origin?");
		}
	}
}
