using CsvChartClient.Models;
using CsvChartClient.Services;
using CsvChartClient.ViewModels;
using System.Diagnostics;

namespace CsvChartClient.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;
    private readonly ChartDrawable _drawable;

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        _drawable = new ChartDrawable(vm);

        BindingContext = _vm;
        FileList.ItemsSource = _vm.Files;
        ChartView.Drawable = _drawable;

        _vm.OnChartChanged += () => ChartView.Invalidate();
        _vm.OnNotification += PrikaziNotifikaciju;
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Status))
                StatusLabel.Text = _vm.Status;
        };

        _vm.AllColumns.CollectionChanged += (s, e) =>
        {
            XAxisPicker.ItemsSource = _vm.AllColumns.ToList();
            if (_vm.AllColumns.Count > 0 && XAxisPicker.SelectedIndex < 0)
            {
                int idx = _vm.AllColumns.IndexOf(_vm.XAxisColumn);
                if (idx >= 0) XAxisPicker.SelectedIndex = idx;
            }
        };

        _vm.SeriesToggles.CollectionChanged += (s, e) => RebuildSeriesCheckboxes();

        DisplayCountLabel.Text = ((int)DisplayCountStepper.Value).ToString();

        Loaded += async (s, e) => await _vm.InitializeAsync();
    }

    private void RebuildSeriesCheckboxes()
    {
        SeriesListLayout.Children.Clear();

        var title = new Label
        {
            Text = "Serije:",
            FontAttributes = FontAttributes.Bold,
            FontSize = 14
        };
        SeriesListLayout.Children.Add(title);

        foreach (var toggle in _vm.SeriesToggles)
        {
            var row = new HorizontalStackLayout { Spacing = 5 };

            var cb = new CheckBox
            {
                IsChecked = toggle.IsVisible,
                VerticalOptions = LayoutOptions.Center
            };
            cb.CheckedChanged += (s, e) => toggle.IsVisible = e.Value;

            var label = new Label
            {
                Text = toggle.Name,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 12,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            row.Children.Add(cb);
            row.Children.Add(label);
            SeriesListLayout.Children.Add(row);
        }
    }

    private void OnRefreshClicked(object sender, EventArgs e)
        => _ = _vm.RefreshFilesAsync();

    private void OnFileSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is FileInfoModel file)
            _vm.SelectedFile = file;
    }

    private void OnChartTypeChanged(object sender, EventArgs e)
        => _vm.ChartType = ChartTypePicker.SelectedIndex;

    private void OnXAxisChanged(object sender, EventArgs e)
    {
        if (XAxisPicker.SelectedItem is string col)
            _vm.XAxisColumn = col;
    }

    private void OnDisplayCountChanged(object sender, ValueChangedEventArgs e)
    {
        int value = (int)e.NewValue;
        DisplayCountLabel.Text = value.ToString();
        _vm.DisplayCount = value;
    }

    private void OnSortChanged(object sender, CheckedChangedEventArgs e)
        => _vm.SortByXAxis = e.Value;

    private async void PrikaziNotifikaciju(string poruka)
    {
        Debug.WriteLine($"Notifikacija: {poruka}");
        await DisplayAlert("Notifikacija", poruka, "OK");
    }
}