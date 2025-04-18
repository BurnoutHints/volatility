[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class EditorTooltipAttribute : Attribute
{
    public string Tooltip { get; }

    public EditorTooltipAttribute(string tooltip)
    {
        Tooltip = tooltip;
    }
}