[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class EditorCategoryAttribute : Attribute
{
    public string Category { get; }

    public EditorCategoryAttribute(string category)
    {
        Category = category;
    }
}