using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// An escapable component
/// </summary>
public abstract class VisibleEscapableComponent : ComponentBase, IDisposable
{
    /// <summary>
    /// The visible value
    /// </summary>
    private bool _Visible;
    /// <summary>
    /// Gtes or sets if this is visible
    /// </summary>
    public bool Visible
    {
        get => _Visible;
        protected set
        {
            if (_Visible == value)
                return;
            if(value)
                App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
            else
                App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
            _Visible = value;
        }
    }

    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Cancel();
            this.StateHasChanged();
            args.StopPropagation = true;
        }
    }

    /// <summary>
    /// The action when this canceled
    /// </summary>
    public abstract void Cancel();

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}