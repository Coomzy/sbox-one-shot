
public class DebugCollision : Component, Component.ICollisionListener
{
	void ICollisionListener.OnCollisionStart(Sandbox.Collision collision)
	{
		Log.Info($"OnCollisionStart() collision.Other.Body: {collision.Other.GameObject} Tags: {collision.Other.GameObject.Tags}, MyTags: {GameObject.Tags}");
	}
}
