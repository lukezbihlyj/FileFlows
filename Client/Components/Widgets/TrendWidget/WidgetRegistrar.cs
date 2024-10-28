using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;


/// <summary>
/// Widget Registrar
/// </summary>
public interface IWidgetRegistrar
{
    /// <summary>
    /// Registers a component
    /// </summary>
    /// <param name="component">the component to register</param>
    void Register(object component);
}