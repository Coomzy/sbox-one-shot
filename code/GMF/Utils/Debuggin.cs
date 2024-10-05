
using System;
using System.Net.Http.Headers;
using Sandbox.Tasks;
using static Sandbox.Gizmo;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Sandbox;

public struct ScreenLog
{
	public string message { get; set; }
	public float lifeTime { get; set; }
	public DateTime startTime { get; set; }
	public Color? color { get; set; }
	public Color? backgroundColor { get; set; }
}

public struct DrawGizmoData
{
	public Action drawAction;
	public DateTime? endTime;
	public Color? color;
}

public class Debuggin : GameObjectSystem
{
	public static ExtraDebug_Draw draw { get; private set; }

	public static List<ScreenLog> screenLogs { get; private set; } = new List<ScreenLog>();
	public static List<DrawGizmoData> drawGizmoDatas { get; private set; } = new List<DrawGizmoData>();

	public static Color defaultTextColor = Color.White;
	public static Color defaultBackgroundColor = Color.Black.WithAlpha(0.75f);

	public Debuggin(Scene scene) : base(scene)
	{
		/*if (!Application.IsDebug)
			return;*/

		screenLogs = screenLogs ?? new();
		draw = draw ?? new();

		Listen(Stage.FinishUpdate, 999998, DrawGizmos_Update, "DrawGizmos_Update");
		Listen(Stage.FinishUpdate, 999999, DrawScreenMessages_Update, "DrawScreenMessages_Update");
	}

	public static void ToScreen(string log, float logLifeTime = -1.0f, Color? color = null, Color? backgroundColor = null)
	{
		var screenLog = new ScreenLog();
		screenLog.message = log;
		screenLog.lifeTime = logLifeTime;
		screenLog.startTime = DateTime.UtcNow;
		screenLog.color = color;
		screenLog.backgroundColor = backgroundColor;
		screenLogs.Add(screenLog);
	}

	public static void DrawGizmo(Action drawAction, float lifeTime = -1.0f, Color? color = null)
	{
		if (drawAction == null)
			return;

		var drawGizmoData = new DrawGizmoData();
		drawGizmoData.drawAction = drawAction;
		if (lifeTime > 0)
		{
			drawGizmoData.endTime = DateTime.UtcNow + TimeSpan.FromSeconds(lifeTime);
		}
		drawGizmoData.color = color;
		drawGizmoDatas.Add(drawGizmoData);
	}

	[ConCmd]
	public static void PurgeToScreenMessages()
	{
		screenLogs.Clear();
	}

	void DrawScreenMessages_Update()
	{		
		DrawScreenMessages();
	}

	public static void DrawScreenMessages()
	{
		if (!Game.IsPlaying && !EditorBridge.isGizmoValid)
			return;

		var originalColor = Gizmo.Draw.Color;
		int count = 0;
		foreach (var message in screenLogs)
		{
			float textSize = 15.0f;
			float offsetX = 10;
			float offsetY = 10 + (textSize);
			float gapY = textSize;
			float x = offsetX;
			float y = offsetY + (count * gapY);
			float charWidth = 8.25f;
			float charBuffer = 8.75f;
			float boxBuffer = 2.0f;			

			var screenPos = new Vector2(x, y);

			var rectPos = screenPos;
			var rectSize = new Vector2(10, gapY);

			rectSize.x = (charWidth * message.message.Length) + charBuffer;
			rectSize.y += boxBuffer / 2;
			rectPos.y -= textSize / 2;
			rectPos.y -= boxBuffer / 2;

			float backgroundAlpha2 = 0.9f;
			var backgroundColor = message.backgroundColor.HasValue ? message.backgroundColor.Value : defaultBackgroundColor;
			if (!message.backgroundColor.HasValue && message.color.HasValue)
			{
				float darkenFactor = 0.35f;
				var darkenedBackgroundColor = message.color.Value;

				darkenedBackgroundColor.r = darkenedBackgroundColor.r * darkenFactor;
				darkenedBackgroundColor.g = darkenedBackgroundColor.g * darkenFactor;
				darkenedBackgroundColor.b = darkenedBackgroundColor.b * darkenFactor;
				darkenedBackgroundColor.a *= backgroundAlpha2;

				backgroundColor = darkenedBackgroundColor;
			}
			Gizmo.Draw.ScreenRect(new Rect(rectPos, rectSize), backgroundColor);

			var textColor = message.color.HasValue ? message.color.Value : defaultTextColor;
			Gizmo.Draw.Color = textColor;
			Gizmo.Draw.ScreenText(message.message, screenPos, "Consolas", textSize, TextFlag.LeftCenter);
			count++;
		}
				
		for (int i = screenLogs.Count - 1; i >= 0; i--)
		{
			var message = screenLogs[i];
			TimeSpan elapsedTime = DateTime.UtcNow - message.startTime;

			if (elapsedTime.TotalSeconds < message.lifeTime)
			{
				continue;
			}
			screenLogs.RemoveAt(i);
		}
		Gizmo.Draw.Color = originalColor;
	}

	void DrawGizmos_Update()
	{
		DrawGizmos();
	}

	public static void DrawGizmos()
	{
		// This is fucking dumb, but I'm getting really bored of editor shit
		if (!Game.IsPlaying && !EditorBridge.isGizmoValid)
			return;

		foreach (var drawGizmo in drawGizmoDatas)
		{
			Debuggin.WorldSpaceGizmo();
			//Gizmo.Transform = this.Scene.Transform.World;
			var originalColor = Gizmo.Draw.Color;
			if (drawGizmo.color != null)
			{
				Gizmo.Draw.Color = drawGizmo.color.Value;
			}

			drawGizmo.drawAction?.Invoke();

			Gizmo.Draw.Color = originalColor;
		}

		for (int i = drawGizmoDatas.Count - 1; i >= 0; i--)
		{
			var message = drawGizmoDatas[i];

			if (DateTime.UtcNow < message.endTime)
				continue;

			drawGizmoDatas.RemoveAt(i);
		}
	}

	public override void Dispose()
	{
		/*if (IsFullyValid(draw))
		{
			draw.Enabled = false;
		}
		if (IsFullyValid(draw?.GameObject))
		{
			draw.GameObject.Destroy();
		}*/

		base.Dispose();
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
		if (Game.ActiveScene?.Transform != null)
		{
			Gizmo.Transform = Game.ActiveScene.Transform.World;
		}
	}
}

public class ExtraDebug_Draw
{
	public void Line(Vector3 start, Vector3 end, float lifeTime = -1.0f, Color? color = null)
	{
		Debuggin.DrawGizmo(() => Gizmo.Draw.Line(start, end), lifeTime, color);
		//WaitLoop(() => Gizmo.Draw.Line(start, end), lifeTime, color);
	}

	public void Sphere(Vector3 center, float radius, int rings = 8, float lifeTime = -1.0f, Color? color = null) => Sphere(new Sandbox.Sphere(center, radius), rings, lifeTime, color);
	public void Sphere(Sphere sphere, int rings = 8, float lifeTime = -1.0f, Color? color = null)
	{
		Debuggin.DrawGizmo(() => Gizmo.Draw.LineSphere(sphere, rings), lifeTime, color);
		//WaitLoop(() => Gizmo.Draw.LineSphere(sphere, rings), lifeTime, color);
	}

	public void Capsule(Vector3 start, Vector3 end, float radius, float lifeTime = -1.0f, Color? color = null) => Capsule(new Capsule(start, end, radius), lifeTime, color);
	public void Capsule(Capsule capsule, float lifeTime = -1.0f, Color? color = null)
	{
		Debuggin.DrawGizmo(() => Gizmo.Draw.LineCapsule(capsule), lifeTime, color);
	}
}