[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class EditorCategoryAttribute : Attribute
{
    public string Category { get; }

    public EditorCategoryAttribute(string category)
    {
        Category = category;
    }
}