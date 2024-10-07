using System.Collections;
using System.Collections.Generic;

public class WeightedRandom<T>
{
    public T defaultValue = default(T);
    public readonly Dictionary<T, int> nameToWeight = new Dictionary<T, int>();

    public WeightedRandom() { }
    public WeightedRandom(T defaultValue) { this.defaultValue = defaultValue; }

    public int count => nameToWeight.Count;
    public int totalWeight
    {
        get
        {
            int calculateWeight = 0;
            foreach (var weight in nameToWeight.Values)
            {
                calculateWeight += weight;
            }
            return calculateWeight;
        }
    }

    public bool SetWeight(T name, int weight)
    {
        if (!nameToWeight.ContainsKey(name))
        {
            return false;
        }

        nameToWeight[name] = MathY.Max(weight, 0);
        return true;
    }

    public bool Add(T name, int weight)
    {
        weight = MathY.Max(weight, 0);
        if (nameToWeight.ContainsKey(name))
        {
            nameToWeight[name] = weight;
            return false;
        }

        nameToWeight.Add(name, weight);
        return true;
    }

    public bool Remove(T name)
    {
        if (!nameToWeight.ContainsKey(name))
        {
            return false;
        }

        nameToWeight.Remove(name);
        return true;
    }

    public bool Contains(T name)
    {
        return nameToWeight.ContainsKey(name);
    }

    public void Clear()
    {
        nameToWeight.Clear();
    }

    public T GetByPercentage(float pct)
    {
        if (totalWeight <= 0)
        {
            return defaultValue;
        }

        int calculatedWeight = 0;
        foreach (var item in nameToWeight.Keys)
        {
            calculatedWeight += nameToWeight[item];
            float calculatedPct = (float)calculatedWeight / totalWeight;

            if (pct <= calculatedPct)
            {
                return item;
            }
        }

        return defaultValue;
    }

    public T Random()
    {
        return GetByPercentage(Game.Random.Float());
    }

    public float GetChancePercentage(T name)
    {
        if (nameToWeight.TryGetValue(name, out int weight))
        {
            return (float)weight / totalWeight;
        }
        return 0.0f;
    }

    public Vector2 GetPercentageRange(T name)
    {
        if (!nameToWeight.ContainsKey(name))
        {
            return Vector2.Zero;
        }

        int calculatedWeight = 0;
        foreach (var item in nameToWeight.Keys)
        {
            if (Equals(item, name))
            {
                Vector2 range = Vector2.Zero;
                range.x = (float)calculatedWeight / totalWeight;
                calculatedWeight += nameToWeight[item];
                range.y = (float)calculatedWeight / totalWeight;
                return range;
            }

            calculatedWeight += nameToWeight[item];
        }

        return Vector2.Zero;
    }

    public void DebugDump()
    {
        Log.Info($"<b>Weighted Random</b> - <b>Total Weight:</b> {totalWeight}");
        foreach (var item in nameToWeight.Keys)
        {
            var range = GetPercentageRange(item);
			Log.Info($"<b>{item}</b> - <b>Weight:</b> {nameToWeight[item]} <b>Chance:</b> {Format((GetChancePercentage(item) * 100))}% <b>Range:</b> {Format(range.x * 100)}-{Format(range.y * 100)}%");
        }
    }

    string Format(float input)
    {
        return input.ToString("0.#");
        //return input.ToString("0.##");
    }
}
