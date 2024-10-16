namespace FileFlows.Plugin.Attributes;

/// <summary>
/// An Input Tag GUI element that will allow for Tags to be selected
/// </summary>
public class TagSelectionAttribute(int order) : FormInputAttribute(FormInputType.TagSelection, order)
{
}
