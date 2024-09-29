using System.Runtime.CompilerServices;
using System;
using Sandbox.Internal;

[AttributeUsage(AttributeTargets.Property)]
[CodeGenerator (CodeGeneratorFlags.WrapPropertySet | CodeGeneratorFlags.Static | CodeGeneratorFlags.Instance, "Sandbox.ConsoleSystem.OnChangePropertySet", 9)]
//[CodeGenerator (CodeGeneratorFlags.WrapPropertySet | CodeGeneratorFlags.Static | CodeGeneratorFlags.Instance, "OnRepCodeGen.OnChangePropertySet", 11)]
//[CodeGenerator( (CodeGeneratorFlags.Static | CodeGeneratorFlags.Instance, "Sandbox.ConsoleSystem.OnChangePropertySet", 9)]
public class OnRepAttribute : ChangeAttribute
{
	public OnRepAttribute([CallerMemberName] string propertyName = null)
		//: base($"OnRep_{propertyName}")
	{
		if (!string.IsNullOrEmpty(propertyName))
		{ 			
			Name = $"OnRep_{propertyName}";
			//Log.Info($"OnRepAttribute() Name: {Name}");
		}

	}

	/*public string Name { get; set; }

	public OnRepAttribute(string name = null)
	{
		Name = name;
	}*/
}

public static class OnRepCodeGen
{
	public static void OnChangePropertySet<T>(WrappedPropertySet<T> p)
	{
		ChangeAttribute attribute = p.Attributes.OfType<ChangeAttribute>().FirstOrDefault<ChangeAttribute>();
		PropertyDescription property = GlobalGameNamespace.TypeLibrary.GetMemberByIdent(p.MemberIdent) as PropertyDescription;
		TypeDescription type = GlobalGameNamespace.TypeLibrary.GetType(p.TypeName);
		string functionName = attribute.Name ?? ("OnRep_" + property.Name);
		bool isStatic = p.IsStatic;
		MethodDescription method = type.Methods.FirstOrDefault((MethodDescription x) => x.IsNamed(functionName) && x.IsStatic == isStatic);
		object oldValue = property.GetValue(p.Object);
		T value = p.Value;
		bool isTheSame = value.Equals(oldValue);
		p.Setter(p.Value);
		bool flag = isTheSame;
		if (!flag)
		{
			Component component = p.Object as Component;
			bool flag2 = component != null;
			if (flag2)
			{
				bool flag3 = component.Flags.HasFlag(ComponentFlags.Deserializing);
				if (flag3)
				{
					return;
				}
				GameObject go = component.GameObject;
				bool flag4 = go.IsValid() && (go.Flags.HasFlag(GameObjectFlags.Deserializing) || go.Flags.HasFlag(GameObjectFlags.Loading));
				if (flag4)
				{
					return;
				}
			}
			try
			{
				bool flag5 = method != null;
				if (flag5)
				{
					method.Invoke(p.Object, new object[]
					{
							oldValue,
							p.Value
					});
				}
				else
				{
					GlobalGameNamespace.Log.Warning(FormattableStringFactory.Create("{0}.{1} has [Change] but we can not find {2}( {3} oldValue, {4} newValue )", new object[]
					{
							type.Name,
							property.Name,
							functionName,
							property.PropertyType,
							property.PropertyType
					}));
				}
			}
			catch (Exception e)
			{
				GlobalGameNamespace.Log.Error(e);
			}
		}
	}
}
