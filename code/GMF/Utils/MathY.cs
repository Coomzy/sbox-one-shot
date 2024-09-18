
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
}
