using InventoryApp.Client.Models;
using InventoryApp.Client.Services;

namespace InventoryApp.Client
{
    public partial class UsersPage : ContentPage
    {
        private readonly ApiService _api;
        private readonly UserSession _session;
        private UserItem? _editing;
        private List<UserItem> _all = new();

        public UsersPage(ApiService api, UserSession session)
        {
            InitializeComponent();
            _api = api;
            _session = session;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRolesAsync();
            await LoadAsync();
        }

        private async Task LoadRolesAsync()
        {
            if (RolePicker.ItemsSource != null) return;
            try
            {
                var roles = await _api.GetRolesAsync();
                RolePicker.ItemsSource = roles
                    .Select(r => new FilterOption(r, UserItem.RoleToText(r)))
                    .ToList();
                ResetForm();
            }
            catch (Exception ex)
            {
                ShowMessage($"A szerver nem érhető el: {ex.Message}", error: true);
            }
        }

        private async Task LoadAsync()
        {
            try
            {
                _all = await _api.GetUsersAsync();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowMessage($"A szerver nem érhető el: {ex.Message}", error: true);
            }
            finally
            {
                RefreshContainer.IsRefreshing = false;
            }
        }

        private void ApplyFilter()
        {
            var visible = ShowInactiveSwitch.IsToggled ? _all : _all.Where(u => u.IsActive).ToList();
            UserList.ItemsSource = visible;
            CountLabel.Text = $"{_all.Count} felhasználó, ebből {_all.Count(u => u.IsActive)} aktív";
        }

        private void OnShowInactiveToggled(object? sender, ToggledEventArgs e) => ApplyFilter();

        private async void OnRefreshing(object? sender, EventArgs e) => await LoadAsync();

        private async void OnSave(object? sender, EventArgs e)
        {
            var username = UsernameEntry.Text?.Trim() ?? "";
            var displayName = DisplayNameEntry.Text?.Trim() ?? "";
            var role = (RolePicker.SelectedItem as FilterOption)?.Value;

            if (username.Length == 0)
            {
                ShowMessage("Add meg a felhasználónevet.", error: true);
                return;
            }
            if (role is null)
            {
                ShowMessage("Válassz szerepkört.", error: true);
                return;
            }

            SaveButton.IsEnabled = false;
            try
            {
                if (_editing == null)
                {
                    var (result, error) = await _api.CreateUserAsync(username, displayName, role);
                    if (result == null) { ShowMessage(error ?? "Hiba a létrehozáskor.", error: true); return; }

                    ResetForm();
                    ShowMessage($"{result.User.Username} létrehozva.", error: false);
                    await LoadAsync();
                    await ShowTemporaryPasswordAsync(result, created: true);
                }
                else
                {
                    var (ok, error) = await _api.UpdateUserAsync(
                        _editing.Id, username, displayName, role, ActiveSwitch.IsToggled);
                    if (!ok) { ShowMessage(error ?? "Hiba a mentéskor.", error: true); return; }

                    ResetForm();
                    ShowMessage("Mentve.", error: false);
                    await LoadAsync();
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"A szerver nem érhető el: {ex.Message}", error: true);
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        private void OnEdit(object? sender, EventArgs e)
        {
            if ((sender as Button)?.BindingContext is not UserItem user) return;

            _editing = user;
            UsernameEntry.Text = user.Username;
            DisplayNameEntry.Text = user.DisplayName;
            SelectRole(user.Role);
            ActiveSwitch.IsToggled = user.IsActive;

            // A saját fiók felhasználóneve nem módosítható (a szerver is elutasítja)
            bool self = user.Username == _session.Current?.Username;
            UsernameEntry.IsEnabled = !self;

            FormTitle.Text = $"Felhasználó szerkesztése: {user.Username}";
            SaveButton.Text = "Mentés";
            ActiveRow.IsVisible = true;
            CreateHintLabel.IsVisible = false;
            CancelButton.IsVisible = true;
            MessageLabel.Text = "";
        }

        private void OnCancel(object? sender, EventArgs e) => ResetForm();

        private async void OnResetPassword(object? sender, EventArgs e)
        {
            if ((sender as Button)?.BindingContext is not UserItem user) return;

            if (user.Username == _session.Current?.Username)
            {
                await DisplayAlert("Új jelszó", "A saját jelszavadat a Fiók oldalon módosíthatod.", "OK");
                return;
            }

            bool ok = await DisplayAlert("Új jelszó generálása",
                $"Új ideiglenes jelszót generálsz {user.DisplayName} (@{user.Username}) számára? " +
                "A jelenlegi jelszava azonnal megszűnik, és a következő belépéskor saját jelszót kell megadnia.",
                "Generálás", "Mégse");
            if (!ok) return;

            try
            {
                var (result, error) = await _api.ResetPasswordAsync(user.Id);
                if (result == null) { ShowMessage(error ?? "Hiba a jelszó generálásakor.", error: true); return; }

                ShowMessage($"Új ideiglenes jelszó generálva: {user.Username}.", error: false);
                await LoadAsync();
                await ShowTemporaryPasswordAsync(result, created: false);
            }
            catch (Exception ex)
            {
                ShowMessage($"A szerver nem érhető el: {ex.Message}", error: true);
            }
        }

        // Az ideiglenes jelszó csak most látható, később nem kérhető le újra.
        private async Task ShowTemporaryPasswordAsync(TempPasswordResult result, bool created)
        {
            var title = created ? "Felhasználó létrehozva" : "Új ideiglenes jelszó";
            var message =
                $"Felhasználónév: {result.User.Username}\n" +
                $"Ideiglenes jelszó: {result.TemporaryPassword}\n\n" +
                $"Érvényes eddig: {result.ExpiresAt.ToLocalTime():yyyy.MM.dd HH:mm}\n\n" +
                "Add át a felhasználónak! Első belépéskor kötelezően új jelszót kell megadnia. " +
                "Ez a jelszó később nem jeleníthető meg újra.";

            bool copy = await DisplayAlert(title, message, "Másolás vágólapra", "Bezár");
            if (copy)
                await Clipboard.Default.SetTextAsync(result.TemporaryPassword);
        }

        private void SelectRole(string role)
        {
            if (RolePicker.ItemsSource is IEnumerable<FilterOption> options)
                RolePicker.SelectedItem = options.FirstOrDefault(o => o.Value == role);
        }

        private void ResetForm()
        {
            _editing = null;
            UsernameEntry.Text = "";
            UsernameEntry.IsEnabled = true;
            DisplayNameEntry.Text = "";
            SelectRole("Leltarozo");
            ActiveSwitch.IsToggled = true;
            ActiveRow.IsVisible = false;
            CreateHintLabel.IsVisible = true;
            FormTitle.Text = "Új felhasználó";
            SaveButton.Text = "Létrehozás";
            CancelButton.IsVisible = false;
        }

        private void ShowMessage(string text, bool error)
        {
            MessageLabel.Text = text;
            MessageLabel.TextColor = error ? Color.FromArgb("#C62828") : Color.FromArgb("#2E7D32");
        }
    }
}