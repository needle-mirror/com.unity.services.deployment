using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Deployment.Editor.JsonUtils;
using Unity.Services.Deployment.Editor.Shared.Assets;
using Unity.Services.Deployment.Editor.Shared.EditorUtils;
using Unity.Services.Deployment.Editor.Shared.Infrastructure.Collections;
using UnityEngine;
using Logger = Unity.Services.Deployment.Editor.Shared.Logging.Logger;

namespace Unity.Services.Deployment.Editor.DeploymentDefinitions
{
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.services.deployment@1.7/manual/deployment_definition.html"),
     Icon("Packages/com.unity.services.deployment/Editor/Interface/UI/Assets/Icons/ddef.png")]
    class DeploymentDefinition : ScriptableObject, ICopyable<DeploymentDefinition>, IEditorDeploymentDefinition, IPath, ISerializationCallbackReceiver
    {
        static readonly JsonSerializerSettings k_JsonSerializerSettings = new() { Formatting = Formatting.Indented };

        static readonly Dictionary<string, Action<object>> k_Validators = new()
        {
        };

        public event PropertyChangedEventHandler PropertyChanged;

        string m_Path;

        [SerializeField]
        string m_DefinitionName;
        [SerializeField]
        List<string> m_ExcludePaths = new();
        [SerializeField]
        string m_SerializedAdditionalProperties = "{}";

        Dictionary<string, object> m_AdditionalProperties = new();
        bool m_SuppressPropertyChanged;
        public IReadOnlyDictionary<string, object> AdditionalProperties => m_AdditionalProperties;

        ObservableCollection<string> m_ObservableExcludePaths = new();

        public string Path { get => m_Path; set => SetField(ref m_Path, value); }
        public string Name { get => m_DefinitionName; set => SetField(ref m_DefinitionName, value); }

        public ObservableCollection<string> ExcludePaths
        {
            get => m_ObservableExcludePaths;
            set => SetField(ref m_ObservableExcludePaths, value);
        }

        void Awake()
        {
            EnsureHandlersRegistered();
        }

        void EnsureHandlersRegistered()
        {
            m_ObservableExcludePaths.CollectionChanged -= ObservableExcludePathsOnCollectionChanged;
            m_ObservableExcludePaths.CollectionChanged += ObservableExcludePathsOnCollectionChanged;
        }

        /// <summary>
        /// Sets a value in <see cref="AdditionalProperties"/>, running the registered validator for
        /// <paramref name="key"/> (if any) before storing it. Fires a <see cref="PropertyChanged"/>
        /// notification on success. The caller is responsible for persisting via
        /// <see cref="SaveChanges"/> when appropriate.
        /// </summary>
        /// <exception cref="InvalidVariantTagsException">
        /// </exception>
        public void SetAdditionalProperty(string key, object value)
        {
            if (k_Validators.TryGetValue(key, out var validator))
                validator(value);

            m_AdditionalProperties[key] = value;

            if (!m_SuppressPropertyChanged)
            {
                OnPropertyChanged(nameof(AdditionalProperties));
            }
        }

        /// <summary>
        /// Returns the value for <paramref name="key"/> cast to <typeparamref name="T"/>,
        /// or <paramref name="defaultValue"/> if the key is absent or the stored value is not
        /// of type <typeparamref name="T"/>.
        /// </summary>
        public T GetAdditionalProperty<T>(string key, T defaultValue = default)
        {
            if (m_AdditionalProperties.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return defaultValue;
        }

        /// <summary>
        /// Removes a key from <see cref="AdditionalProperties"/> and fires a
        /// <see cref="PropertyChanged"/> notification. Returns true if the key was present.
        /// </summary>
        public bool RemoveAdditionalProperty(string key)
        {
            if (!m_AdditionalProperties.Remove(key))
                return false;
            OnPropertyChanged(nameof(AdditionalProperties));
            return true;
        }

        public string ToJson()
        {
            var dto = new DeploymentDefinitionDTO { Name = Name, ExcludePaths = ExcludePaths.ToList(), AdditionalProperties = new Dictionary<string, object>(m_AdditionalProperties) };

            return JsonConvert.SerializeObject(dto, k_JsonSerializerSettings);
        }

        public void FromJson(string json)
        {
            m_ObservableExcludePaths.CollectionChanged -= ObservableExcludePathsOnCollectionChanged;

            var dto = JsonConvert.DeserializeObject<DeploymentDefinitionDTO>(json, k_JsonSerializerSettings);
            Name = dto.Name;
            ExcludePaths.Clear();
            dto.ExcludePaths?.ForEach(p => ExcludePaths.Add(p));
            m_AdditionalProperties.Clear();
            SetAdditionalPropertiesFromJson(dto.AdditionalProperties);

            m_ObservableExcludePaths.CollectionChanged += ObservableExcludePathsOnCollectionChanged;
        }

        static Dictionary<string, object> NormalizeJTokenValues(Dictionary<string, object> properties)
        {
            if (properties == null)
                return new Dictionary<string, object>();

            var normalized = new Dictionary<string, object>(properties.Count);
            foreach (var kvp in properties)
            {
                normalized[kvp.Key] = kvp.Value is JToken token ? token.ToValue() : kvp.Value;
            }

            return normalized;
        }

        void SetAdditionalPropertiesFromJson(Dictionary<string, object> properties)
        {
            m_SuppressPropertyChanged = true;
            try
            {
                foreach (var kvp in properties)
                {
                    try
                    {
                        var value = kvp.Value is JToken token ? token.ToValue() : kvp.Value;
                        SetAdditionalProperty(kvp.Key, value);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            finally
            {
                m_SuppressPropertyChanged = false;
            }

            OnPropertyChanged(nameof(AdditionalProperties));
        }

        void ObservableExcludePathsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    e.NewItems.Cast<string>()
                        .ForEach(p => m_ExcludePaths
                            .Insert(
                                Mathf.Max(ExcludePaths.IndexOf(p), m_ExcludePaths.Count),
                                p));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    e.OldItems.Cast<string>()
                        .ForEach(p => m_ExcludePaths.Remove(p));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    m_ExcludePaths[e.OldStartingIndex] = e.NewItems.Cast<string>().First();
                    break;
                case NotifyCollectionChangedAction.Reset:
                    m_ExcludePaths.Clear();
                    break;
                default:
                    Logger.LogError($"Deployment Definition does not support action `{e.Action}`");
                    break;
            }
        }

        public void SaveChanges()
        {
            var json = ToJson();
            File.WriteAllText(Path, json);
        }

        public void ReloadFromFile()
        {
            m_ObservableExcludePaths.CollectionChanged -= ObservableExcludePathsOnCollectionChanged;
            try
            {
                FromJson(File.ReadAllText(Path));
            }
            catch (JsonReaderException e)
            {
                Debug.LogError($"[Deployment Definition] '{Path}': {e.Message}", this);
            }

            m_ObservableExcludePaths.CollectionChanged += ObservableExcludePathsOnCollectionChanged;
        }

        public void CopyTo(DeploymentDefinition value)
        {
            value.EnsureHandlersRegistered();
            value.Name = Name;

            value.ExcludePaths.Clear();
            ExcludePaths.ForEach(p => value.ExcludePaths.Add(p));
            value.m_ExcludePaths.Clear();
            value.m_ExcludePaths.AddRange(m_ExcludePaths);

            value.m_AdditionalProperties = DeepCopyAdditionalProperties(m_AdditionalProperties);
        }

        static Dictionary<string, object> DeepCopyAdditionalProperties(Dictionary<string, object> source)
        {
            var json = JsonConvert.SerializeObject(source);
            return NormalizeJTokenValues(
                JsonConvert.DeserializeObject<Dictionary<string, object>>(json));
        }

        public void OnBeforeSerialize()
        {
            m_ObservableExcludePaths.CollectionChanged -= ObservableExcludePathsOnCollectionChanged;
            m_ExcludePaths.Clear();
            m_ExcludePaths.AddRange(ExcludePaths);
            m_SerializedAdditionalProperties = JsonConvert.SerializeObject(m_AdditionalProperties);
            m_ObservableExcludePaths.CollectionChanged += ObservableExcludePathsOnCollectionChanged;
        }

        public void OnAfterDeserialize()
        {
            m_ObservableExcludePaths.CollectionChanged -= ObservableExcludePathsOnCollectionChanged;
            ExcludePaths.Clear();
            m_ExcludePaths.ForEach(p => ExcludePaths.Add(p));
            try
            {
                m_AdditionalProperties = string.IsNullOrEmpty(m_SerializedAdditionalProperties)
                    ? new Dictionary<string, object>()
                    : NormalizeJTokenValues(
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(m_SerializedAdditionalProperties));
            }
            catch
            {
                m_AdditionalProperties = new Dictionary<string, object>();
            }

            m_ObservableExcludePaths.CollectionChanged += ObservableExcludePathsOnCollectionChanged;
            Sync.RunNextUpdateOnMain(() =>
            {
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Path));
                OnPropertyChanged(nameof(AdditionalProperties));
            });
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(propertyName);
        }

    }
}
