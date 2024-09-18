
public class ExtraDebug : GameObjectSystem
{
	public static ExtraDebug_Draw draw {  get; private set; }

	public ExtraDebug(Scene scene) : base(scene)
	{
		if (draw == null)
		{
			var newDebugInstanceGO = new GameObject();
			newDebugInstanceGO.Flags = GameObjectFlags.DontDestroyOnLoad;
			draw = newDebugInstanceGO.Components.Create<ExtraDebug_Draw>();
		}
	}

	/*public static void DrawLine(Vector3 start, Vector3 end, float lifeTime = -1.0f)
	{
		instance.DrawLine(start, end, lifeTime);
	}

	public static void DrawCapsule(Vector3 start, Vector3 end, float radius, float lifeTime = -1.0f)
	{
		instance.DrawCapsule(start, end, radius, lifeTime);
	}

	public static void DrawCapsule(Capsule capsule, float lifeTime = -1.0f)
	{
		instance.DrawCapsule(capsule, lifeTime);
	}*/

	public static void WorldSpaceGizmo()
	{
		if (Game.ActiveScene.Transform != null)
		{
			Gizmo.Transform = Game.ActiveScene.Transform.World;
		}
	}
}

public class ExtraDebug_Draw : Component
{
	async void WaitLoop(System.Action drawAction, float lifeTime = -1.0f, Color? color = null)
	{
		TimeUntil lifeTimeEnd = lifeTime;
		bool doOnce = lifeTime <= 0.0f;

		while (!lifeTimeEnd || doOnce)
		{
			ExtraDebug.WorldSpaceGizmo();
			if (color != null)
			{
				Gizmo.Draw.Color = color.Value;
			}

			drawAction?.Invoke();

			if (doOnce)
			{
				break;
			}

			await Task.Frame();
		}
	}

	public void Line(Vector3 start, Vector3 end, float lifeTime = -1.0f, Color? color = null)
	{
		WaitLoop(() => Gizmo.Draw.Line(start, end), lifeTime, color);
	}

	public void Capsule(Vector3 start, Vector3 end, float radius, float lifeTime = -1.0f, Color? color = null) => Capsule(new Capsule(start, end, radius), lifeTime);
	public void Capsule(Capsule capsule, float lifeTime = -1.0f, Color? color = null)
	{
		WaitLoop(() => Gizmo.Draw.LineCapsule(capsule), lifeTime, color);
	}
}