using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

using static Volatility.Utilities.ClassUtilities;

namespace Vantage.ViewModels;

public class ClassViewModel
{
    public ObservableCollection<CategoryGroupViewModel> FieldGroups { get; private set; }
    public ObservableCollection<ClassViewModel> NestedClassGroups { get; private set; }

    public ClassViewModel(object instance)
    {
        FieldGroups = new ObservableCollection<CategoryGroupViewModel>();
        NestedClassGroups = new ObservableCollection<ClassViewModel>();

        if (instance != null)
        {
            InitializeFieldGroups(instance);
        }
    }

    private void InitializeFieldGroups(object instance)
    {
        var members = new List<MemberInfo>();
        members.AddRange(instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
        members.AddRange(instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
    
        var groupedFields = new Dictionary<string, CategoryGroupViewModel>();
    
        foreach (var memberInfo in members)
        {
            if (GetAttribute<EditorHiddenAttribute>(memberInfo) != null)
                continue;
    
            var category = GetAttribute<EditorCategoryAttribute>(memberInfo)?.Category ?? "Uncategorized";
    
            object value = memberInfo switch
            {
                FieldInfo fieldInfo => fieldInfo.GetValue(instance),
                PropertyInfo propertyInfo => propertyInfo.GetValue(instance),
            };
    
            if (value != null && IsComplexType(value))
            {
                var nestedViewModel = new ClassViewModel(value);
                NestedClassGroups.Add(nestedViewModel);
            }
            else
            {
                if (!groupedFields.ContainsKey(category))
                {
                    groupedFields[category] = new CategoryGroupViewModel(category);
                }
                groupedFields[category].Fields.Add(new FieldViewModel(instance, memberInfo));
            }
        }
        FieldGroups = new ObservableCollection<CategoryGroupViewModel>(groupedFields.Values);
    }
}
