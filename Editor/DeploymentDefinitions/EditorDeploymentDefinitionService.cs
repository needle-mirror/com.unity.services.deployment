using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Unity.Services.Deployment.Core;
using Unity.Services.Deployment.Editor.Shared.Infrastructure.Collections;
using Unity.Services.Deployment.Editor.Tracking;
using Unity.Services.DeploymentApi.Editor;
using UnityEngine;
using IDeploymentDefinition = Unity.Services.Deployment.Core.Model.IDeploymentDefinition;
using Object = UnityEngine.Object;

namespace Unity.Services.Deployment.Editor.DeploymentDefinitions
{
    sealed class EditorDeploymentDefinitionService :
        DeploymentDefinitionServiceBase,
        IEditorDeploymentDefinitionService,
        IDisposable
    {
        internal const string DefaultName = "Default";
        internal const string DefaultPath = "DefaultPath";

        public IEditorDeploymentDefinition DefaultDefinition { get; }
        public ObservableCollection<DeploymentDefinition> ObservableDeploymentDefinitions => m_DeploymentDefinitions;
        public override IReadOnlyList<IDeploymentDefinition> DeploymentDefinitions => m_DeploymentDefinitions.AsReadonly();
        readonly DeploymentDefinitionCollection m_DeploymentDefinitions = new DeploymentDefinitionCollection(true);

        public event Action DeploymentDefinitionPathChanged;
        public event Action DeploymentDefinitionExcludePathsChanged;
        public event Action DeploymentDefinitionAdditionalPropertyChanged;
        public event Action DeploymentItemPathChanged;

        public EditorDeploymentDefinitionService(
            IDeploymentItemCollectionTracker itemCollectionTracker)
        {
            itemCollectionTracker.ItemPropertyChanged += OnDeploymentItemPropertyChanged;

            var defaultDdef = ScriptableObject.CreateInstance<DeploymentDefinition>();
            defaultDdef.Name = DefaultName;
            defaultDdef.Path = DefaultPath;
            DefaultDefinition = defaultDdef;

            ObservableDeploymentDefinitions.CollectionChanged += ObservableDeploymentDefinitionsOnCollectionChanged;
        }

        public override IDeploymentDefinition DefinitionForPath(string path)
        {
            return base.DefinitionForPath(path) ?? DefaultDefinition;
        }

        void OnDeploymentItemPropertyChanged(IDeploymentItem item, string propertyName)
        {
            if (propertyName == nameof(IDeploymentItem.Path))
            {
                DeploymentItemPathChanged?.Invoke();
            }
        }

        void ObservableDeploymentDefinitionsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    m_DeploymentDefinitions
                        .ForEach(d =>
                    {
                        d.PropertyChanged -= OnDeploymentDefinitionPropertyChanged;
                        d.ExcludePaths.CollectionChanged -= ObservableExcludePathsOnCollectionChanged;
                    });
                    break;
                default:
                    if (e.OldItems != null)
                    {
                        e.OldItems.Cast<DeploymentDefinition>()
                            .ForEach(d =>
                            {
                                d.PropertyChanged -= OnDeploymentDefinitionPropertyChanged;
                                d.ExcludePaths.CollectionChanged -= ObservableExcludePathsOnCollectionChanged;
                            });
                    }

                    if (e.NewItems != null)
                    {
                        e.NewItems.Cast<DeploymentDefinition>()
                            .ForEach(d =>
                            {
                                d.PropertyChanged += OnDeploymentDefinitionPropertyChanged;
                                d.ExcludePaths.CollectionChanged += ObservableExcludePathsOnCollectionChanged;
                            });
                    }
                    break;
            }
        }

        void ObservableExcludePathsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DeploymentDefinitionExcludePathsChanged?.Invoke();
        }

        void OnDeploymentDefinitionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IDeploymentDefinition.Path))
            {
                DeploymentDefinitionPathChanged?.Invoke();
            }
            else if (e.PropertyName == nameof(IDeploymentDefinition.AdditionalProperties))
            {
                DeploymentDefinitionAdditionalPropertyChanged?.Invoke();
            }
        }

        public void Dispose()
        {
            Object.DestroyImmediate((DeploymentDefinition)DefaultDefinition);
            m_DeploymentDefinitions.Dispose();
        }
    }
}
