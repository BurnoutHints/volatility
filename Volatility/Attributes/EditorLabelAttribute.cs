﻿[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EditorLabelAttribute : Attribute
{
    public string Label { get; }

    public EditorLabelAttribute(string label)
    {
        Label = label;
    }
}