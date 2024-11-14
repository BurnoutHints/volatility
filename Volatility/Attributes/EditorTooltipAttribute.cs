[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class EditorTooltipAttribute : Attribute
{
    public string Tooltip { get; }

    public EditorTooltipAttribute(string tooltip)
    {
        Tooltip = tooltip;
    }
}