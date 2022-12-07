using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.Services.Deployment.Editor.Shared.Assets;
using Unity.Services.Deployment.Editor.Shared.EditorUtils;
using UnityEngine;
using Sync = Unity.Services.Deployment.Editor.Shared.Threading.Sync;

namespace Unity.Services.Deployment.Editor.DeploymentDefinitions
{
    class DeploymentDefinition : ScriptableObject, ICopyable<DeploymentDefinition>, IPath, INotifyPropertyChanged, ISerializationCallbackReceiver
    {
        static readonly JsonSerializerSettings k_JsonSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public event PropertyChangedEventHandler PropertyChanged;

        [SerializeField]
        string m_DefinitionName;
        string m_Path;

        public string Path { get => m_Path; set => SetField(ref m_Path, value); }
        public string Name { get => m_DefinitionName; set => SetField(ref m_DefinitionName, value); }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(new { Name }, k_JsonSerializerSettings);
        }

        public void PopulateFromJson(string json)
        {
            JsonConvert.PopulateObject(json, this, k_JsonSerializerSettings);
        }

        public void SaveChanges()
        {
            File.WriteAllText(Path, ToJson());
        }

        public void ReloadFromFile()
        {
            PopulateFromJson(File.ReadAllText(Path));
        }

        public void CopyTo(DeploymentDefinition value)
        {
            value.Name = Name;
        }

        public void OnBeforeSerialize()
        {
            // nothing to serialize
        }

        public void OnAfterDeserialize()
        {
            Sync.RunNextUpdateOnMain(() =>
            {
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Path));
            });
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
