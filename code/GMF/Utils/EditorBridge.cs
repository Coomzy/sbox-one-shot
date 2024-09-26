using System;
using System.Reflection;

public static class EditorBridge
{
	public static EditorBridgeReflection reflection { get; set; } = new EditorBridgeReflection();

	public static bool isGizmoValid
	{
		get
		{
			if (!Application.IsEditor)
				return true;

			if (!reflection.IsValid(typeof(Gizmo), "Active"))
				return false;

			return true;
		}
	}
}

public class EditorBridgeReflection
{
	public static Func<Type, string, BindingFlags, object> OnGetValue;
	public static Func<Type, string, BindingFlags, bool> OnIsValid;

	public T GetValue<T>(Type type, string memberName, BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.NonPublic) where T : class
	{
		if (OnGetValue != null)
		{
			return OnGetValue(type, memberName, bindingFlags) as T;
		}
		return default(T);
	}

	public bool IsValid(Type type, string memberName, BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.NonPublic)
	{
		if (OnIsValid != null)
		{
			return OnIsValid(type, memberName, bindingFlags);
		}
		return false;
	}
}