using System;

[AttributeUsage(AttributeTargets.Class)]
public class ProjectSettingsWidgetAttribute : Attribute
{
	public string title { get; }

	public ProjectSettingsWidgetAttribute(string title = null)
	{
		this.title = title;
	}
}