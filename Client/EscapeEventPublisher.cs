namespace FileFlows.Client;

public delegate void EscapePushedEventHandler(OnEscapeArgs e);

public class EscapeEventPublisher
{
    private readonly List<EscapePushedEventHandler> _listeners = new();

    /// <summary>
    /// Event that is fired when the escape key is pushed.
    /// </summary>
    public event EscapePushedEventHandler OnEscapePushed
    {
        add
        {
            _listeners.Add(value);
        }
        remove
        {
            _listeners.Remove(value);
        }
    }

    public void RaiseEscapePushed()
    {
        var args = new OnEscapeArgs();
        // Iterate listeners in reverse order to give newer listeners priority
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            var listener = _listeners[i];
            listener?.Invoke(args);

            // If the listener signals to stop propagation (you can customize the signal)
            if (args.StopPropagation)
            {
                break;
            }
        }
    }
}