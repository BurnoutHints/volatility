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
    public ObservableCollection<ClassViewModel> ClassGroups { get; private set; }
    
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

    public string CurrentlyEditingString => 
        string.IsNullOrEmpty(SelectedFilePath) 
            ? "Open a file to edit." 
            : $"Editing: {SelectedFilePath}";

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

        ClassGroups = [];
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

        var resource = ResourceFactory.CreateResource(SelectedResourceType, SelectedResourcePlatform, SelectedFilePath);

        currentResource = resource;
        
        var classViewModel = new ClassViewModel(resource);

        ClassGroups.Clear();
        ClassGroups.Add(classViewModel);

        OnPropertyChanged(nameof(ClassGroups));
    }
}