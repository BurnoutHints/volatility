using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reflection;

using ReactiveUI;

using Volatility.Resources;

using static Volatility.Utilities.ClassUtilities;

namespace Vantage.ViewModels;

public class QuickEditorViewModel : ViewModelBase
{
    public ObservableCollection<CategoryGroupViewModel> FieldGroups { get; private set; }

    public ReactiveCommand<Unit, Unit> OpenCommand { get; }

    Resource currentResource {  get; set; }

    private string _selectedFilePath;
    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            if (_selectedFilePath != value)
            {
                _selectedFilePath = value;
                OnPropertyChanged(nameof(SelectedFilePath));
                OnPropertyChanged(nameof(CurrentlyEditingString));
            }
        }
    }

    public string CurrentlyEditingString
    {
        get
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                return "Open a file to edit.";
            }
            return $"Editing: {SelectedFilePath}";
        }
    }

    public IEnumerable<ResourceType> ResourceTypes { get; }
    public IEnumerable<Platform> ResourcePlatforms { get; }

    private ResourceType _selectedResourceType;
    public ResourceType SelectedResourceType
    {
        get => _selectedResourceType;
        set
        {
            _selectedResourceType = value;
            OnResourceTypeChanged(value);
        }
    }
    
    private Platform _selectedResourcePlatform;
    public Platform SelectedResourcePlatform
    {
        get => _selectedResourcePlatform;
        set
        {
            _selectedResourcePlatform = value;
            OnResourcePlatformChanged(value);
        }
    }

    private void OnResourcePlatformChanged(Platform value) { }

    private void OnResourceTypeChanged(ResourceType value) { }

    public QuickEditorViewModel()
    {
        ResourceTypes = Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().ToList();
        ResourcePlatforms = Enum.GetValues(typeof(Platform)).Cast<Platform>().ToList();

        FieldGroups = new ObservableCollection<CategoryGroupViewModel>
        {
            new CategoryGroupViewModel("Category 1"),
            new CategoryGroupViewModel("Category 2")
        };
    }

    public void OnFileSelected(string filePath, Platform format, ResourceType type)
    {
        SelectedFilePath = filePath;
        SelectedResourceType = type;
        SelectedResourcePlatform = format;

        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            Console.WriteLine("WARNING: No valid File selected.");
            return;
        }

        currentResource = ResourceFactory.CreateResource(SelectedResourceType, SelectedResourcePlatform, SelectedFilePath);

        var fieldInfos = currentResource.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // Group fields by category
        var groupedFields = new Dictionary<string, CategoryGroupViewModel>();

        foreach (var fieldInfo in fieldInfos)
        {
            // Check if the field is marked with [EditorHidden]
            if (GetAttribute<EditorHiddenAttribute>(fieldInfo) != null)
                continue;

            var category = GetAttribute<EditorCategoryAttribute>(fieldInfo)?.Category ?? "Uncategorized";

            if (!groupedFields.ContainsKey(category))
            {
                groupedFields[category] = new CategoryGroupViewModel(category);
            }

            groupedFields[category].Fields.Add(new FieldViewModel(currentResource, fieldInfo));
        }

        // Convert to an observable collection for binding
        FieldGroups = new ObservableCollection<CategoryGroupViewModel>(groupedFields.Values);

        OnPropertyChanged(nameof(FieldGroups));
    }
}