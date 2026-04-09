namespace TheToobe;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
        
        InitializeComponent();
    }

    async void OnModeSwitch(object sender, ToggledEventArgs e)
    {
        if (modeLabel.Text == "Commuter Mode - DISABLED")
        {
            modeLabel.Text = "Commuter Mode - ENABLED";
            SecureStorage.Default.RemoveAll();
            await SecureStorage.Default.SetAsync("Mode", "commuter");
        }
        else
        {
            modeLabel.Text = "Commuter Mode - DISABLED";
            SecureStorage.Default.RemoveAll();
            await SecureStorage.Default.SetAsync("Mode", "tourist");
        }
    }
}