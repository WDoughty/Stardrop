using Avalonia.Collections;
using ReactiveUI;
using Stardrop.Models;
using Stardrop.Models.SMAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Stardrop.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<Mod> Mods { get; set; }
        private int _enabledModCount;
        public int EnabledModCount { get { return _enabledModCount; } set { this.RaiseAndSetIfChanged(ref _enabledModCount, value); } }
        public DataGridCollectionView DataView { get; set; }

        private bool _hideDisabledMods;
        public bool HideDisabledMods { get { return _hideDisabledMods; } set { _hideDisabledMods = value; UpdateFilter(); } }
        private string _filterText;
        public string FilterText { get { return _filterText; } set { _filterText = value; UpdateFilter(); } }
        private string _columnFilter;
        public string ColumnFilter { get { return _columnFilter; } set { _columnFilter = value; UpdateFilter(); } }

        public MainWindowViewModel(string modsFilePath)
        {
            Mods = new ObservableCollection<Mod>();

            DirectoryInfo modDirectory = new DirectoryInfo(modsFilePath);
            foreach (var fileInfo in modDirectory.GetFiles("manifest.json", SearchOption.AllDirectories))
            {
                if (fileInfo.DirectoryName is null)
                {
                    continue;
                }

                try
                {
                    var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(fileInfo.FullName), new JsonSerializerOptions { AllowTrailingCommas = true });
                    if (manifest is null)
                    {
                        Program.helper.Log($"The manifest.json was empty or not deserializable from {fileInfo.DirectoryName}", Utilities.Helper.Status.Alert);
                        continue;
                    }

                    var mod = new Mod(fileInfo, manifest.UniqueID, manifest.Version, manifest.Name, manifest.Description, manifest.Author);
                    if (!Mods.Any(m => m.UniqueId.Equals(manifest.UniqueID, StringComparison.OrdinalIgnoreCase)))
                    {
                        Mods.Add(mod);
                    }
                    else if (Mods.First(m => m.UniqueId.Equals(manifest.UniqueID, StringComparison.OrdinalIgnoreCase) && m.Version < mod.Version) is Mod oldMod && oldMod is not null)
                    {
                        // Replace old mod with newer one
                        int oldModIndex = Mods.IndexOf(Mods.First(m => m.UniqueId.Equals(manifest.UniqueID, StringComparison.OrdinalIgnoreCase) && m.Version < mod.Version));
                        Mods[oldModIndex] = mod;
                    }
                }
                catch (Exception ex)
                {
                    Program.helper.Log($"Unable to load the manifest.json from {fileInfo.DirectoryName}: {ex}", Utilities.Helper.Status.Alert);
                }
            }

            // Create data view
            var dataGridSortDescription = DataGridSortDescription.FromPath(nameof(Mod.Name), ListSortDirection.Ascending);

            DataView = new DataGridCollectionView(Mods);
            DataView.SortDescriptions.Add(dataGridSortDescription);
        }
        public void EnableModsByProfile(Profile profile)
        {
            foreach (var mod in Mods)
            {
                mod.IsEnabled = false;
                if (profile.EnabledModIds.Any(id => id.Equals(mod.UniqueId, StringComparison.OrdinalIgnoreCase)))
                {
                    mod.IsEnabled = true;
                }
            }
        }

        internal void UpdateFilter()
        {
            DataView.Filter = null;
            DataView.Filter = ModFilter;
        }

        private bool ModFilter(object item)
        {
            var mod = item as Mod;

            if (_hideDisabledMods && !mod.IsEnabled)
            {
                return false;
            }
            if (!String.IsNullOrEmpty(_filterText) && !String.IsNullOrEmpty(_columnFilter))
            {
                if (_columnFilter == "Mod Name" && !mod.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                else if (_columnFilter == "Author" && !mod.Author.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                else if (_columnFilter == "Requirements" && !mod.Requirements.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
