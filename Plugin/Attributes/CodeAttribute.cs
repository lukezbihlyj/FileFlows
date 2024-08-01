namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Attribute to indicate a field is for code
/// </summary>
/// <param name="order">the order it appears</param>
/// <param name="language">The language for the code</param>
public class CodeAttribute(int order, string language = "js") : FormInputAttribute(FormInputType.Code, order)
{
    /// <summary>
    /// Gets the language of the code
    /// </summary>
    public string Language => language;
}