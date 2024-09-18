using System;
using Editor;
using Sandbox;
using System.Reflection;
using Sandbox.Internal;
using static Editor.EditorEvent;
using System.Collections.Generic;
using System.Linq;
using static Editor.Label;

[Dock("Editor", "Project Settings", "manage_search")]
public class ProjectSettingsDock : Widget
{
	NavigationView view;

	public ProjectSettingsDock(Widget parent) : base(parent)
	{
		Layout = Layout.Column();
		Layout.Margin = 4;
		Layout.Spacing = 4;

		view = new NavigationView(this);
		view.MinimumSize = 0;
		//Layout.Add(View);
		view.SetSizeMode(SizeMode.Default, SizeMode.Flexible);

		var scroller = new ScrollArea(this);
		scroller.Canvas = view;
		scroller.Canvas.SetSizeMode(SizeMode.Default, SizeMode.Flexible);
		scroller.Canvas.Layout = Layout.Column();
		Layout.Add(scroller);
		scroller.Canvas.Layout.AddStretchCell();

		view.MenuTop.Add(new Label("Project Settings"));
		view.MenuTop.AddSpacingCell(16);

		Rebuild();
	}

	[Hotload(Priority = 10)]
	public void Rebuild()
	{
		var lastOptionTitle = view?.CurrentOption?.Title;

		view.ClearPages();

		var types = GlobalGameNamespace.TypeLibrary.GetTypes<ProjectSettingNonGenericBase>();
		var optionTitleToOption = new Dictionary<string, NavigationView.Option>();
		types = types.OrderBy(t => t.TargetType.Name);

		foreach (var type in types)
		{
			if (type.IsAbstract)
				continue;

			var targetType = type.TargetType;
			Type constructedType = typeof(ProjectSetting<>).MakeGenericType(targetType);
			var instanceProperty = constructedType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);

			var inst = instanceProperty.GetValue(null);
			var gameResourceInst = inst as GameResource;

			string title = FormatToReadable(targetType.Name);

			var option = view.AddPage(title, null, new ProjectSettingsInspector(this, gameResourceInst));
			optionTitleToOption[title] = option;
		}

		// Custom Widget Support
		var attributesTypes = GlobalToolsNamespace.EditorTypeLibrary.GetTypesWithAttribute<ProjectSettingsWidgetAttribute>();

		foreach (var attributeInst in attributesTypes)
		{
			if (attributeInst.Type?.TargetType == null)
				continue;

			bool isWidgetType = attributeInst.Type.TargetType.IsSubclassOf(typeof(Widget));
			if (!isWidgetType)
			{
				Log.Warning($"ProjectSettingsDock failed to create widget instance for '{attributeInst.Type.TargetType}' are you use this inherits from Widget?");
				continue;
			}

			var widgetInst = Activator.CreateInstance(attributeInst.Type.TargetType, this) as Widget;
			if (widgetInst == null)
			{
				Log.Error($"ProjectSettingsDock failed to create instance for type '{attributeInst.Type.TargetType}'");
				continue;
			}

			string title = attributeInst.Attribute.title;
			if (string.IsNullOrEmpty(title))
			{
				title = FormatToReadable(attributeInst.Type.Name);
			}

			var option = view.AddPage(title, null, widgetInst);
			optionTitleToOption[title] = option;
		}

		// Keeps the select option after a rebuild, this was really annoying me
		if (!string.IsNullOrEmpty(lastOptionTitle))
		{
			if (optionTitleToOption.TryGetValue(lastOptionTitle, out var option))
			{
				if (option != null)
				{
					view.CurrentOption = option;
				}
			}
		}
	}

	public static string FormatToReadable(string input)
	{
		// Add space before each uppercase letter that is followed by a lowercase letter or another uppercase letter group.
		string formattedInput = System.Text.RegularExpressions.Regex.Replace(input, "(?<=.)([A-Z][a-z])", " $1");

		// Add space between an acronym and the next capitalized word (e.g., "XMLParser" becomes "XML Parser")
		formattedInput = System.Text.RegularExpressions.Regex.Replace(formattedInput, "(?<=[a-z])([A-Z])", " $1");

		// Capitalize the first letter of the resulting string
		return Char.ToUpper(formattedInput[0]) + formattedInput.Substring(1);
	}
}

public class ProjectSettingsInspector : Widget
{
	public ProjectSettingsInspector(Widget parent, GameResource gameResource) : base(parent)
	{
		Layout = Layout.Column();
		SetSizeMode(SizeMode.Default, SizeMode.Flexible);

		var sheet = new ControlSheet();

		var so = gameResource.GetSerialized();

		var scroller = new ScrollArea(this);
		scroller.Canvas = new Widget(this);
		scroller.Canvas.SetSizeMode(SizeMode.Default, SizeMode.Flexible);
		scroller.Canvas.Layout = Layout.Column();
		Layout.Add(scroller);

		so.OnPropertyChanged += x =>
		{
			var asset = AssetSystem.FindByPath(gameResource.ResourcePath);
			if (asset != null)
			{
				bool saved = asset.SaveToDisk(gameResource);
				if (!saved)
				{
					Log.Error($"ProjectSettingsInspector '{gameResource?.ResourceName}' failed to save, is the file read only? Please report it");
				}
			}
			else
			{
				Log.Error($"ProjectSettingsInspector '{gameResource?.ResourceName}' failed to get asset, this should not happen! Please report it");
			}
		};

		sheet.AddObject(so);

		scroller.Canvas.Layout.Add(sheet);
		scroller.Canvas.Layout.AddStretchCell();
	}
}

// Can't do because you can't reference library editor projects
/*[AttributeUsage(AttributeTargets.Class)]
public class ProjectSettingsWidgetAttribute : Attribute
{
	public string group { get; }
	public string title { get; }

	public ProjectSettingsWidgetAttribute(string group = null, string title = null)
	{
		this.group = group;
		this.title = title;
	}
}*/