using Sandbox.Services;

public enum StatAggregation
{
	Sum,
	Min,
	Max,
	Average,
	First,
	Last,
	Value
}

public static partial class Stat
{
	public const string KILLS = "kills";
	public const string DEATHS = "deaths";
	public const string WINS_ROUNDS = "wins-rounds";
	public const string WINS_MATCHES = "wins-matches";
	public const string PLAYED_ROUNDS = "played-rounds";
	public const string PLAYED_MATCHES = "played-matches";

	public static int GetValue(this Stats.PlayerStats playerStats, string name, StatAggregation aggregation = StatAggregation.Sum)
	{
		return (int)GetRawValue(playerStats, name, aggregation);
	}

	public static double GetRawValue(this Stats.PlayerStats playerStats, string name, StatAggregation aggregation = StatAggregation.Sum)
	{
		var stat = playerStats.Get(Stat.XP);

		switch (aggregation)
		{
			case StatAggregation.Sum:
				return stat.Sum;
			case StatAggregation.Min:
				return stat.Min;
			case StatAggregation.Max:
				return stat.Max;
			case StatAggregation.Average:
				return stat.Avg;
			case StatAggregation.First:
				return stat.FirstValue;
			case StatAggregation.Last:
				return stat.LastValue;
			case StatAggregation.Value:
				return stat.Value;
		}

		Log.Warning($"Unknown StatAggregation type of '{aggregation}'");
		return 0;
	}
}