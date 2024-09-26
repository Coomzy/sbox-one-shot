
using System;

public static class MathY
{
	public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
	{
		Vector3 direction = target - current;

		float distance = direction.Length;

		if (distance <= maxDistanceDelta)
		{
			return target;
		}
		else
		{

			Vector3 scaledDirection = direction.Normal * maxDistanceDelta;

			Vector3 newPosition = current + scaledDirection;

			return newPosition;
		}
	}

	public static float MoveTowards(float current, float target, float maxDelta)
	{
		if (System.Math.Abs(target - current) <= maxDelta)
		{
			return target;
		}

		return current + System.Math.Sign(target - current) * maxDelta;
	}

	public static Vector3 GetRandomizedDirection(Vector3 originalDirection, float maxAngle)
	{
		float pitch = Game.Random.Float(-maxAngle, maxAngle);
		float yaw = Game.Random.Float(-maxAngle, maxAngle);
		float roll = Game.Random.Float(-maxAngle, maxAngle);

		Rotation randomRotation = Rotation.From(pitch, yaw, roll);
		Vector3 randomizedDirection = randomRotation * originalDirection;

		return randomizedDirection.Normal;
	}

	public static float RandomRange(this Vector2 inst)
	{
		return Game.Random.Float(inst.x, inst.y);
	}

	public static float Lerp(this Vector2 vector, float t)
	{
		return MathX.Lerp(vector.x, vector.y, t);
	}

	public static float InverseLerp(this Vector2 vector, float t)
	{
		return MathX.LerpInverse(t, vector.x, vector.y);
	}
}

public class Remap
{
	[KeyProperty] public Vector2 fromRange { get; set; }
	[KeyProperty] public Vector2 toRange { get; set; }
	[KeyProperty] public Curve remapCurve { get; set; }

	public Remap()
	{
		fromRange = new Vector2(0.0f, 1.0f);
		toRange = new Vector2(0.0f, 1.0f);
		remapCurve = new Curve(new Curve.Frame(0.0f, 0.0f, 0.0f, 1.0f), new Curve.Frame(1.0f, 1.0f, -1.0f, 0.0f));
	}

	public Remap(float minFromRange, float maxFromRange)
	{
		fromRange = new Vector2(minFromRange, maxFromRange);
		toRange = new Vector2(0.0f, 1.0f);
		remapCurve = new Curve(new Curve.Frame(0.0f, 0.0f, 0.0f, 1.0f), new Curve.Frame(1.0f, 1.0f, -1.0f, 0.0f));
	}

	public Remap(float minFromRange, float maxFromRange, float minToRange, float maxToRange)
	{
		fromRange = new Vector2(minFromRange, maxFromRange);
		toRange = new Vector2(minToRange, maxToRange);
		remapCurve = new Curve(new Curve.Frame(0.0f, 0.0f, 0.0f, 1.0f), new Curve.Frame(1.0f, 1.0f, -1.0f, 0.0f));
	}

	public float Evaluate(float value)
	{
		float result = MathX.LerpInverse(value, fromRange.x, fromRange.y);
		result = remapCurve.Evaluate(result);
		result = MathX.Lerp(toRange.x, toRange.y, result);
		return result;
	}
}