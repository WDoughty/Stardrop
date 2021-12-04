using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Collections;
using Stardrop.Models;
using System.ComponentModel;
using Avalonia.Data;
using Stardrop.ViewModels;
using System.IO;
using System.Linq;
using System;

namespace Stardrop.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly ProfileEditorViewModel _editorView;

        public MainWindow()
        {
            InitializeComponent();

            // Set the main window view
            _viewModel = new MainWindowViewModel(Program.defaultModPath);
            DataContext = _viewModel;

            // Set the path according to the environmental variable SMAPI_MODS_PATH
            // SMAPI_MODS_PATH is set via the profile dropdown on the UI
            var modGrid = this.FindControl<DataGrid>("modGrid");
            modGrid.IsReadOnly = true;
            modGrid.LoadingRow += Dg1_LoadingRow;
            modGrid.Items = _viewModel.DataView;

            // Handle the mainMenu bar for drag and related events
            var mainMenu = this.FindControl<Menu>("mainMenu");
            mainMenu.PointerPressed += MainMenu_PointerPressed;
            mainMenu.DoubleTapped += MainMenu_DoubleTapped;

            // Set profile list
            _editorView = new ProfileEditorViewModel(Path.Combine(Program.defaultHomePath, "Profiles"));
            var profileComboBox = this.FindControl<ComboBox>("profileComboBox");
            profileComboBox.Items = _editorView.Profiles;
            profileComboBox.SelectedIndex = 0;
            profileComboBox.SelectionChanged += ProfileComboBox_SelectionChanged;

            // Update selected mods
            var profile = profileComboBox.SelectedItem as Profile;
            _viewModel.EnableModsByProfile(profile);

            // Handle buttons
            this.FindControl<Button>("minimizeButton").Click += delegate { this.WindowState = WindowState.Minimized; };
            this.FindControl<Button>("maximizeButton").Click += delegate { AdjustWindowState(); };
            this.FindControl<Button>("exitButton").Click += ExitButton_Click;
            this.FindControl<Button>("editProfilesButton").Click += EditProfiles_Click;
            this.FindControl<CheckBox>("hideDisabledMods").Click += HideDisabledModsButton_Click;

            // Handle filtering via textbox
            this.FindControl<TextBox>("searchBox").AddHandler(KeyUpEvent, (s, e) => _viewModel.FilterText = (s as TextBox).Text);
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void HideDisabledModsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var hideDisabledModsCheckBox = this.FindControl<CheckBox>("hideDisabledMods");
            _viewModel.HideDisabledMods = (bool)hideDisabledModsCheckBox.IsChecked;
        }

        private void ProfileComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var profile = this.FindControl<ComboBox>("profileComboBox").SelectedItem as Profile;
            _viewModel.EnableModsByProfile(profile);

            var modGrid = this.FindControl<DataGrid>("modGrid");
        }

        private void EnabledBox_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var checkBox = e.Source as CheckBox;
            if (checkBox is null)
            {
                return;
            }

            // Update the profile's enabled mods
            var profile = this.FindControl<ComboBox>("profileComboBox").SelectedItem as Profile;
            var enabledModIds = this.FindControl<DataGrid>("modGrid").Items.Cast<Mod>().Where(m => m.IsEnabled).Select(m => m.UniqueId).ToList();
            _editorView.UpdateProfile(profile, enabledModIds);
        }

        private void EditProfiles_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var editorWindow = new ProfileEditor(_editorView);
            editorWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            editorWindow.ShowDialog(this);
        }

        private void ExitButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }

        private void MainMenu_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!e.Handled)
            {
                AdjustWindowState();
            }
        }

        private void MainMenu_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.Pointer.IsPrimary && !e.Handled)
            {
                this.BeginMoveDrag(e);
            }
        }

        private void Dg1_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void AdjustWindowState()
        {
            this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
