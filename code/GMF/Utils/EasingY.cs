
public static class EasingY
{
	public static float InBack(float t)
	{
		float s = 1.70158f;
		return t * t * ((s + 1) * t - s);
	}
	public static float OutBack(float t) => 1 - InBack(1 - t);
	public static float InOutBack(float t)
	{
		if (t < 0.5) return InBack(t * 2) / 2;
		return 1 - InBack((1 - t) * 2) / 2;
	}
}
