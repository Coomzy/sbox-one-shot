using Sandbox.Audio;
using System.Collections.Generic;
using System;
using Sandbox;

public class UITab
{
	public string tabName { get; set; }
	public int order { get; set; }

	public List<UIGroup> groups = new List<UIGroup>();

	public UITab(string tabName, int order = 0)
	{
		this.tabName = tabName;
		this.order = order;
	}

	public UIGroup AddGroup(string groupName)
	{
		var group = new UIGroup(groupName);
		groups.Add(group);
		return group;
	}
}

public class UIGroup
{
	public string groupName { get; }

	public List<UIElement> elements { get; } = new List<UIElement>();

	public UIGroup(string groupName)
	{
		this.groupName = groupName;
	}

	public UIToggle AddToggle(string displayName, Func<bool> getter, Action<bool> setter)
	{
		var toggle = new UIToggle(displayName, getter, setter);
		elements.Add(toggle);
		return toggle;
	}

	public UISlider AddSlider(string displayName, Func<float> getter, Action<float> setter, float min = 0.0f, float max = 1.0f, float step = 0.1f)
	{
		var slider = new UISlider(displayName, getter, setter, min, max, step);
		elements.Add(slider);
		return slider;
	}

	public UICycler<TEnum> AddCycler<TEnum>(string displayName, Func<TEnum> getter, Action<TEnum> setter) where TEnum : Enum
	{
		var cycler = new UICycler<TEnum>(displayName, getter, setter);
		elements.Add(cycler);
		return cycler;
	}

	/*public UICycler AddCycler(string displayName, List<string> options, Func<int> getter, Action<int> setter)
	{
		var cycler = new Cycler(displayName, options, getter, setter);
		elements.Add(cycler);
		return cycler;
	}*/
}

public class UIElement
{
	public string displayName { get; } = "ELEMENT";

	public UIElement(string displayName)
	{
		this.displayName = displayName;
	}
}

public class UIToggle : UIElement
{
	public Func<bool> getter { get; }
	public Action<bool> setter { get; }

	public UIToggle(string displayName, Func<bool> getter, Action<bool> setter) : base(displayName)
	{
		this.getter = getter;
		this.setter = setter;
	}
}

public class UISlider : UIElement
{
	public Func<float> getter { get; }
	public Action<float> setter { get; }
	public float min = 0.0f;
	public float max = 1.0f;
	public float step = 0.1f;

	public UISlider(string displayName, Func<float> getter, Action<float> setter, float min = 0.0f, float max = 1.0f, float step = 0.1f) : base(displayName)
	{
		this.getter = getter;
		this.setter = setter;
		this.min = min;
		this.max = max;
		this.step = step;
	}
}

public class UICycler<TEnum> : UICyclerBase where TEnum : Enum
{
	public Func<TEnum> getter { get; }
	public Action<TEnum> setter { get; }

	public UICycler(string displayName, Func<TEnum> getter, Action<TEnum> setter) : base(displayName)
	{
		this.getter = getter;
		this.setter = setter;
	}

	public override string onGet() => getter.Invoke().ToString();
	public override void onSet(object value) => setter((TEnum)value);

	public override Array GetEnumValues()
	{
		return Enum.GetValues(typeof(TEnum));
	}

	public override void CycleLeft()
	{
		var values = GetEnumValues();
		var value = getter();
		var index = Array.IndexOf(values, value);

		// Move to the previous index if it's not the first one.
		if (index > 0)
		{
			index--;
		}

		var newValue = (TEnum)values.GetValue(index);
		setter(newValue);
	}

	public override void CycleRight()
	{
		var values = GetEnumValues();
		var value = getter();
		var index = Array.IndexOf(values, value);

		// Move to the next index if it's not the last one.
		if (index < values.Length - 1)
		{
			index++;
		}

		var newValue = (TEnum)values.GetValue(index);
		setter(newValue);
	}
}

public abstract class UICyclerBase : UIElement
{
	public UICyclerBase(string displayName) : base(displayName)
	{

	}

	public abstract object onGet();
	public abstract void onSet(object value);
	public abstract Array GetEnumValues();
	public abstract void CycleLeft();
	public abstract void CycleRight();
}

// Not sure if there is any point in supporting this
/*public class UICycler : UIElement
{
	public List<string> options { get; }
	public Func<int> getter { get; }
	public Action<int> setter { get; }

	public UICycler(string displayName, List<string> options, Func<int> getter, Action<int> setter) : base(displayName)
	{
		this.options = options;
		this.getter = getter;
		this.setter = setter;
	}
}*/