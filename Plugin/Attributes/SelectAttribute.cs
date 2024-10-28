namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Attribute to indicate a select input should be used for a property
/// </summary>
/// <param name="optionsProperty">the property with the list options are defined</param>
/// <param name="order">the order this input will appear</param>
public class SelectAttribute(string optionsProperty, int order) : FormInputAttribute(FormInputType.Select, order)
{
    /// <summary>
    /// Gets or sets the property with the list options are defined
    /// </summary>
    public string OptionsProperty
    {
        get => optionsProperty;
        set => optionsProperty = value;
    }
}