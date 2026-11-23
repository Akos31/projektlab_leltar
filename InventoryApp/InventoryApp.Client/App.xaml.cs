using InventoryApp.Client.Services;

namespace InventoryApp.Client
{
    public partial class App : Application
    {
        public App(UserSession session)
        {
            InitializeComponent();
            session.Expired += OnSessionExpired;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        // Lejárt munkamenet: vissza a bejelentkezéshez, rövid magyarázattal
        private void OnSessionExpired(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Shell.Current is null) return;

                while (Shell.Current.Navigation.ModalStack.Count > 0)
                    await Shell.Current.Navigation.PopModalAsync(false);

                await Shell.Current.GoToAsync("//LoginPage");
                await Shell.Current.DisplayAlert("Kijelentkeztetve",
                    "A munkameneted lejárt, vagy a fiókod adatai megváltoztak. Jelentkezz be újra.",
                    "OK");
            });
        }
    }
}