namespace FileFlowsTests.Models;


public enum InputType
{
    Text,
    TextArea,
    Select,
    Toggle
}

public class FlowField
{
    public string Name { get; }

    public InputType Type { get; }
    public object Value { get; }

    public FlowField(string name, object value, InputType? type = null)
    {
        if (type == null)
        {
            if (value is bool)
                type = InputType.Toggle;
            else
                type = InputType.Text;
        }

        this.Name = name;
        this.Type = type.Value;
        this.Value = value;
    }
}