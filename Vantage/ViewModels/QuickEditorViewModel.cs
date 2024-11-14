using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reflection;

using ReactiveUI;

using Volatility.Resources;

namespace Vantage.ViewModels;

public class QuickEditorViewModel : ViewModelBase
{

    public ObservableCollection<FieldViewModel> Fields { get; set; }

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
            }
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
    }

    public void OnFileSelected(string filePath)
    {
        SelectedFilePath = filePath;

        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            Console.WriteLine("WARNING: No valid File selected.");
            return;
        }

        currentResource = ResourceFactory.CreateResource(SelectedResourceType, SelectedResourcePlatform, SelectedFilePath);

        Fields = [];

        var fieldInfos = currentResource.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var fieldInfo in fieldInfos)
        {
            Fields.Add(new FieldViewModel(currentResource, fieldInfo));
        }

        OnPropertyChanged(nameof(Fields));
    }
}