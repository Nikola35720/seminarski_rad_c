namespace CsvChartClient;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = Handler!.MauiContext!.Services.GetRequiredService<Views.MainPage>();
        return new Window(new NavigationPage(page));
    }
}