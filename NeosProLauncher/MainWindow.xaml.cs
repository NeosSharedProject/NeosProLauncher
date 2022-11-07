using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace NeosProLauncher
{
    // Token: 0x02000005 RID: 5
    public partial class MainWindow : Window
    {
        // Token: 0x17000002 RID: 2
        // (get) Token: 0x0600000F RID: 15
        public static string VERSION_PATH
        {
            get
            {
                return Path.Combine(APP_PATH, "Version");
            }
        }

        // Token: 0x17000003 RID: 3
        // (get) Token: 0x06000010 RID: 16
        public static string CONFIG_PATH
        {
            get
            {
                return Path.Combine(APP_PATH, "Config.json");
            }
        }
        public enum CloudEndpoint
        {
            Production,
            Staging,
            Local,
        }

        public static CloudEndpoint CLOUD_ENDPOINT { get; set; } = CloudEndpoint.Production;

        public static string NEOS_API
        {
            get
            {
                switch (CLOUD_ENDPOINT)
                {
                    case CloudEndpoint.Production:
                        return "https://api.neos.com";
                    case CloudEndpoint.Staging:
                        return "https://cloudx-staging.azurewebsites.net";
                    case CloudEndpoint.Local:
                        return "http://localhost:60612";
                    default:
                        throw new Exception("Invalid Endpoint: " + CLOUD_ENDPOINT.ToString());
                }
            }
        }

        // Token: 0x17000004 RID: 4
        // (get) Token: 0x06000011 RID: 17
        private string LicenseParameter
        {
            get
            {
                if (!RequireLicense)
                {
                    return "";
                }
                return "-LicenseKey " + LoadLicenseKey();
            }
        }

        // Token: 0x06000012 RID: 18
        private void SaveString(string name, string str)
        {
            savedSettings.SetValue(name + (IsPublicBuild ? "-Regular" : ""), str);
        }

        // Token: 0x06000013 RID: 19
        private string LoadString(string name, string def)
        {
            return (savedSettings.GetValue(name + (IsPublicBuild ? "-Regular" : "")) as string) ?? def;
        }

        // Token: 0x06000014 RID: 20
        private void SaveBool(string name, bool value)
        {
            savedSettings.SetValue(name, value ? "True" : "False");
        }

        // Token: 0x06000015 RID: 21
        private bool LoadBool(string name, bool def)
        {
            return LoadString(name, def ? "True" : "False") == "True";
        }

        // Token: 0x06000016 RID: 22
        private string LoadLicenseKey()
        {
            return LoadString("License Key", null);
        }

        // Token: 0x06000017 RID: 23
        private void SaveLicenseKey(string key)
        {
            SaveString("License Key", key);
        }

        // Token: 0x06000018 RID: 24
        private DateTime LoadLastVerifiedTime()
        {
            DateTime time;
            if (DateTime.TryParse(LoadString("Verification Time", "0"), out time))
            {
                return time;
            }
            return DateTime.MinValue;
        }

        // Token: 0x06000019 RID: 25
        private void UpdateVerifiedTime()
        {
            SaveString("Verification Time", DateTime.UtcNow.ToString());
        }

        // Token: 0x0600001A RID: 26
        public MainWindow()
        {
            InitializeComponent();
        }

        // Token: 0x0600001C RID: 28
        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists("Branch.conf"))
            {
                IsPublicBuild = true;
                RequireLicense = false;
                OptionNeosVR.Visibility = Visibility.Hidden;
                OptionNeosClassroom.Visibility = Visibility.Hidden;
                Logo.Visibility = Visibility.Hidden;
                LogoRegular.Visibility = Visibility.Visible;
                Title = "Neos Launcher (tentative)";
            }
            else
            {
                LogoRegular.Visibility = Visibility.Hidden;
            }
            //MachineUUID = KeyVerification.GetHardwareUUID();
            LicenseKey.Visibility = Visibility.Hidden;
            RegisterButton.Visibility = Visibility.Hidden;
            KeyLabel.Visibility = Visibility.Hidden;
            LaunchVR_Button.IsEnabled = false;
            LaunchMeta_Button.IsEnabled = false;
            LaunchScreen_Button.IsEnabled = false;
            Launcher_Button.IsEnabled = false;
            Patch_Button.Visibility = Visibility.Hidden;
            Logo.Opacity = 0.5;
            ProgressBar.IsIndeterminate = true;
            OptionNeosClassroom.IsChecked = new bool?(LoadBool("Neos Classroom", false));
            if (!IsPublicBuild && File.Exists("Config.json"))
            {
                try
                {
                    RequireLicense = !System.Text.Json.JsonSerializer.Deserialize<Config>(File.ReadAllText("Config.json")).SupressKey;
                }
                catch (Exception ex3)
                {
                    string text = "Exceptions:\n";
                    Exception ex2 = ex3;
                    MessageBox.Show(text + ((ex2 != null) ? ex2.ToString() : null));
                }
            }
            Title = "Neos Launcher (UserMade mod by.kazu)";
            await CheckLicense();
        }

        // Token: 0x0600001D RID: 29
        private async Task CheckLicense()
        {
            StatusText.Foreground = Brushes.White;
            StatusText.Content = "Verifying license...";
            bool registered = false;
            if (RequireLicense)
            {
                DateTime lastVerified = LoadLastVerifiedTime();
                string key = LoadLicenseKey();
                if (string.IsNullOrWhiteSpace(key))
                {
                    SetUnregistered("Unregistered. Please provide a license key.");
                }
                else
                {
                    License license = new License
                    {
                        LicenseGroup = "Pro",
                        LicenseKey = key,
                        PairedMachineUUID = MachineUUID
                    };
                    HttpResponseMessage httpResponseMessage = await client.PutAsync(NEOS_API + "/api/licenses", new StringContent(JsonConvert.SerializeObject(license), Encoding.UTF8, "application/json"));
                    HttpResponseMessage result = httpResponseMessage;
                    await result.Content.ReadAsStringAsync();
                    if (result.IsSuccessStatusCode)
                    {
                        UpdateVerifiedTime();
                        registered = true;
                    }
                    else if (result.StatusCode == HttpStatusCode.Conflict || result.StatusCode == HttpStatusCode.BadRequest)
                    {
                        SetUnregistered("Invalid license Key. Ensure it is correct and unique.");
                    }
                    else if ((DateTime.UtcNow - lastVerified).TotalDays < 7.0)
                    {
                        registered = true;
                    }
                }
            }
            else
            {
                registered = true;
            }
            if (registered)
            {
                KeyLabel.Visibility = Visibility.Hidden;
                LicenseKey.Visibility = Visibility.Hidden;
                RegisterButton.Visibility = Visibility.Hidden;
                StatusText.Foreground = Brushes.White;
                await CheckUpdate();
            }
        }

        // Token: 0x0600001E RID: 30
        private void SetUnregistered(string licenseKeyMessage)
        {
            ProgressBar.IsIndeterminate = false;
            KeyLabel.Visibility = Visibility.Visible;
            LicenseKey.Visibility = Visibility.Visible;
            RegisterButton.Visibility = Visibility.Visible;
            StatusText.Content = licenseKeyMessage;
            StatusText.Foreground = Brushes.Red;
        }

        // Token: 0x0600001F RID: 31
        private async Task CheckUpdate()
        {
            StatusText.Content = "Checking update...";
            string branch = null;
            if (File.Exists("Branch.conf"))
            {
                branch = File.ReadAllText("Branch.conf");
            }
            else
            {
                branch = "Public";
            }
            WebClient client = new WebClient();
            string version = "";
            try
            {
                string text = await client.DownloadStringTaskAsync("https://assets.neos.com/install/Pro/" + branch);
                if (!string.IsNullOrEmpty(text)) version = text;
            }
            catch (Exception)
            {
            }
            try
            {
                string text = await client.DownloadStringTaskAsync("https://cloudxstorage.blob.core.windows.net/install/Pro/" + branch);
                if (!string.IsNullOrEmpty(text)) version = text;
            }
            catch (Exception)
            {
            }
            if (string.IsNullOrEmpty(version))
            {
                MessageBox.Show("Branch Version cannot detect.");
                Retry_Button.IsEnabled = true;
                Retry_Button.Visibility = Visibility.Visible;
                return;
            }
            string currentVersion = null;
            if (File.Exists(VERSION_PATH))
            {
                currentVersion = File.ReadAllText(VERSION_PATH);
            }
            if (version != currentVersion)
            {
                updated = true;
                if (File.Exists("Update.7z"))
                {
                    File.Delete("Update.7z");
                }
                StatusText.Content = "Downloading update...";
                try
                {
                    client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    await client.DownloadFileTaskAsync("https://assets.neos.com/install/Pro/Data/" + version + ".7z", "Update.7z");
                }
                catch (Exception)
                {
                }
                if (!File.Exists("Update.7z"))
                {
                    try
                    {
                        client.DownloadProgressChanged += Client_DownloadProgressChanged;
                        await client.DownloadFileTaskAsync("https://cloudxstorage.blob.core.windows.net/install/Pro/Data/" + version + ".7z", "Update.7z");
                    }
                    catch (Exception)
                    {
                    }
                }
                if (!File.Exists("Update.7z"))
                {
                    MessageBox.Show("Update file doesn't exist or missing. check network connection.");
                    Retry_Button.IsEnabled = true;
                    Retry_Button.Visibility = Visibility.Visible;
                    return;
                }
                if (!File.Exists("7zr.exe") | !File.Exists("7za.exe"))
                {
                    StatusText.Content = "Downloading unpacker...";
                    try
                    {
                        client.DownloadProgressChanged += Client_DownloadProgressChanged;
                        await client.DownloadFileTaskAsync("https://7-zip.org/a/7zr.exe", "7zr.exe");
                    }
                    catch (Exception)
                    {
                    }
                }
                ProgressBar.IsIndeterminate = true;
                StatusText.Content = "Installing update...";
                try
                {
                    string FileName = "7zr.exe";
                    if (File.Exists("7za.exe")) FileName = "7za.exe";
                    if (File.Exists("7zr.exe")) FileName = "7zr.exe";
                    await Process.Start(new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = FileName,
                        Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", "Update.7z", APP_PATH)
                    }).WaitForExitAsync(default(CancellationToken));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to install!\n\n" + ex.ToString());
                }
                File.Delete("Update.7z");
                File.WriteAllText(VERSION_PATH, version.ToString());
            }            //string branch = null;
            if (File.Exists("Branch.conf"))
            {
                branch = File.ReadAllText("Branch.conf");
            }
            else
            {
                branch = "Public";
            }
            WebClient client = new WebClient();
            string version = "";
            try
            {
                string text = await client.DownloadStringTaskAsync("https://assets.neos.com/install/Pro/" + branch);
                if (!string.IsNullOrEmpty(text)) version = text;
            }
            catch (Exception)
            {
            }
            try
            {
                string text = await client.DownloadStringTaskAsync("https://cloudxstorage.blob.core.windows.net/install/Pro/" + branch);
                if (!string.IsNullOrEmpty(text)) version = text;
            }
            catch (Exception)
            {
            }
            if (string.IsNullOrEmpty(version))
            {
                MessageBox.Show("Branch Version cannot detect.");
                Retry_Button.IsEnabled = true;
                Retry_Button.Visibility = Visibility.Visible;
                return;
            }
            string currentVersion = null;
            if (File.Exists(VERSION_PATH))
            {
                currentVersion = File.ReadAllText(VERSION_PATH);
            }
            if (version != currentVersion)
            {
                updated = true;
                if (File.Exists("Update.7z"))
                {
                    File.Delete("Update.7z");
                }
                StatusText.Content = "Downloading update...";
                try
                {
                    client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    await client.DownloadFileTaskAsync("https://assets.neos.com/install/Pro/Data/" + version + ".7z", "Update.7z");
                }
                catch (Exception)
                {
                }
                if (!File.Exists("Update.7z"))
                {
                    try
                    {
                        client.DownloadProgressChanged += Client_DownloadProgressChanged;
                        await client.DownloadFileTaskAsync("https://cloudxstorage.blob.core.windows.net/install/Pro/Data/" + version + ".7z", "Update.7z");
                    }
                    catch (Exception)
                    {
                    }
                }
                if (!File.Exists("Update.7z"))
                {
                    MessageBox.Show("Update file doesn't exist or missing. check network connection.");
                    Retry_Button.IsEnabled = true;
                    Retry_Button.Visibility = Visibility.Visible;
                    return;
                }
                if (!File.Exists("7zr.exe") | !File.Exists("7za.exe"))
                {
                    StatusText.Content = "Downloading unpacker...";
                    try
                    {
                        client.DownloadProgressChanged += Client_DownloadProgressChanged;
                        await client.DownloadFileTaskAsync("https://7-zip.org/a/7zr.exe", "7zr.exe");
                    }
                    catch (Exception)
                    {
                    }
                }
                ProgressBar.IsIndeterminate = true;
                StatusText.Content = "Installing update...";
                try
                {
                    string FileName = "7zr.exe";
                    if (File.Exists("7za.exe")) FileName = "7za.exe";
                    if (File.Exists("7zr.exe")) FileName = "7zr.exe";
                    await Process.Start(new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = FileName,
                        Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", "Update.7z", APP_PATH)
                    }).WaitForExitAsync(default(CancellationToken));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to install!\n\n" + ex.ToString());
                }
                File.Delete("Update.7z");
                File.WriteAllText(VERSION_PATH, version.ToString());
            }
            StatusText.Content = "Ready";
            LaunchVR_Button.IsEnabled = true;
            LaunchMeta_Button.IsEnabled = true;
            LaunchScreen_Button.IsEnabled = true;
            Launcher_Button.IsEnabled = true;
            Logo.Opacity = 1.0;
            ProgressBar.Value = 0.0;
            ProgressBar.IsIndeterminate = false;
            Patch_Button.Visibility = Visibility.Visible;
        }

        // Token: 0x06000020 RID: 32
        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(delegate ()
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = e.ProgressPercentage;
                StatusText.Content = string.Format("Downloading update... {0} % ({1} kB)", e.ProgressPercentage, e.BytesReceived / 1024L);
            });
        }

        // Token: 0x17000005 RID: 5
        // (get) Token: 0x06000021 RID: 33
        private string ExtraParams
        {
            get
            {
                if (OptionNeosClassroom.IsChecked.GetValueOrDefault())
                {
                    return "-Bootstrap BusinessX.NeosClassroom ";
                }
                return "";
            }
        }

        private async void Retry_Button_Click(object sender, RoutedEventArgs e)
        {
            await CheckLicense();
        }

        // Token: 0x06000022 RID: 34
        private void LaunchVR_Button_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("Config.json"))
            {
                File.Copy("Config.json", CONFIG_PATH, true);
            }
            Process.Start(Path.Combine(APP_PATH, "Neos.exe"), IsPublicBuild ? "" : ("-Pro " + this.LicenseParameter + " " + this.ExtraParams));
            Close();
        }

        // Token: 0x06000023 RID: 35
        private void LaunchScreen_Button_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("Config.json"))
            {
                File.Copy("Config.json", CONFIG_PATH, true);
            }
            Process.Start(Path.Combine(APP_PATH, "Neos.exe"), IsPublicBuild ? "-Screen" : ("-Pro " + this.LicenseParameter + " -Screen " + this.ExtraParams));
            Close();
        }

        // Token: 0x06000024 RID: 36
        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string key = LicenseKey.Text;
            if (string.IsNullOrWhiteSpace(key))
            {
                StatusText.Content = "Please input a license key";
            }
            else
            {
                SaveLicenseKey(key);
                await CheckLicense();
            }
        }

        // Token: 0x06000025 RID: 37
        private void Window_Closed(object sender, EventArgs e)
        {
            SaveBool("Neos Classroom", OptionNeosClassroom.IsChecked.GetValueOrDefault());
            if (updated)
            {
                string postupdate = Path.Combine("app", "PostUpdate.exe");
                if (File.Exists(postupdate))
                {
                    Process.Start(postupdate);
                }
            }
        }

        // Token: 0x0600005E RID: 94
        private void LaunchMeta_Button_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("Config.json"))
            {
                File.Copy("Config.json", CONFIG_PATH, true);
            }
            Process.Start(Path.Combine("app", "Neos.exe"), IsPublicBuild ? "-RiftTouch" : ("-Pro " + this.LicenseParameter + " " + this.ExtraParams));
            Close();
        }

        private void Launcher_Button_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("Config.json"))
            {
                File.Copy("Config.json", CONFIG_PATH, true);
            }
            Process.Start(Path.Combine("app", "NeosLauncher.exe"), IsPublicBuild ? "" : ("-Pro " + this.LicenseParameter + " " + this.ExtraParams));
            Close();
        }
        private async void Apply_Patch(object sender, RoutedEventArgs e)
        {
            LaunchVR_Button.IsEnabled = false;
            LaunchMeta_Button.IsEnabled = false;
            LaunchScreen_Button.IsEnabled = false;
            Launcher_Button.IsEnabled = false;
            Patch_Button.Visibility = Visibility.Hidden;
            await Install_Patch_LeapC();
            await Install_Patch_SRAnipal();
            ProgressBar.Value = 0.0;
            ProgressBar.IsIndeterminate = false;
            LaunchVR_Button.IsEnabled = true;
            LaunchMeta_Button.IsEnabled = true;
            LaunchScreen_Button.IsEnabled = true;
            Launcher_Button.IsEnabled = true;
            StatusText.Content = "Ready";
        }
        private async Task Install_Patch_LeapC()
        {
            StatusText.Content = "Download&Install patch (LeapC.dll)...";
            try
            {
                WebClient client = new WebClient();
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                await client.DownloadFileTaskAsync("https://github.com/ultraleap/UnityPlugin/raw/main/Packages/Tracking/Core/Runtime/Plugins/x86_64/LeapC.dll", APP_PATH + "/Neos_Data/Plugins/x86_64/LeapC.dll");
            }
            catch (Exception)
            {
                MessageBox.Show("Patchfile seems missing, abort...");
            }
            MessageBox.Show("Patched successfully!");
        }
        private async Task Install_Patch_SRAnipal()
        {
/*            StatusText.Content = "Downloading patch (SRAnipalSDK)...";
            try
            {
                WebClient client = new WebClient();
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                await client.DownloadFileTaskAsync("https://dl.vive.com/SDK/vivesense/SRanipal/SDK-v1.3.6.6.zip", "SRAnipalSDK.zip");
            }
            catch (Exception)
            {
                MessageBox.Show("Patchfile seems missing, abort...");
            }
            StatusText.Content = "Installing patch (SRAnipalSDK)...";
            ZipFile.ExtractToDirectory("SRAnipalSDK.zip", "Extract");
            Directory.Move("Extract/SDK/01_C/bin/",APP_PATH + "/Neos_Data/Plugins/x86_64/");
            MessageBox.Show("Patched successfully!");
*/        }

        // Token: 0x04000002 RID: 2
        public const double VERIFICATION_TIME_PERIOD = 7.0;

        // Token: 0x04000003 RID: 3
        public const string APP_PATH = "app";

        // Token: 0x04000004 RID: 4
        private static bool IsPublicBuild = false;

        // Token: 0x04000005 RID: 5
        private static bool RequireLicense = true;

        // Token: 0x04000006 RID: 6
        private static bool updated;

        // Token: 0x04000007 RID: 7
        private static string MachineUUID;

        // Token: 0x04000008 RID: 8
        private static HttpClient client = new HttpClient();

        // Token: 0x04000009 RID: 9
        private RegistryKey savedSettings = Registry.CurrentUser.CreateSubKey("Software").CreateSubKey("NeosProLauncher");
    }
}
