using Microsoft.AspNetCore.Components;

namespace Microsoft.Blazor.FluentUI.Icons;

public partial class FluentIcon
{
    [Parameter]
    [EditorRequired]
    public bool Filled { get; set; }

    [Parameter]
    [EditorRequired]
    public string? Name { get; set; }

    [Parameter]
    [EditorRequired]
    public int Size { get; set; } = 20;

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        return base.SetParametersAsync(parameters);
    }
    public string ClassName
    {
        get
        {
            string result = "";
            if (Name is not null)
                result = $"icon-{Name}_{Size}_{(Filled ? "filled" : "regular")}";

            return result;

        }
    }

    public string FontStyle
    {
        get
        {
            return $@"font-family: FluentSystemIcons-{(Filled ? "Filled" : "Regular")}";
        }
    }

    public string ComposedName
    {
        get
        {
            string result = "";
            if (Name is not null)
                result = $"{Name}_{Size}_{(Filled ? "filled" : "regular")}";

            return result;

        }
    }
}
