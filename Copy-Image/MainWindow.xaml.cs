using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Copy_Image
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> args = new List<string>();
        string installedPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Copy-Image\Copy-Image.exe";
        string targetPath;
        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            foreach (var arg in Environment.GetCommandLineArgs().Skip(1))
            {
                // If true, invalid argument was passed.
                if (!arg.ToLower().StartsWith("-file") && arg.ToLower() != "-wpfautoupdate" && !File.Exists(arg.ToLower()))
                {
                    Environment.Exit(1);
                    return;
                }
                if (arg.ToLower().StartsWith("-file") && arg.Length > 5)
                {
                    args.Add("-file");
                    args.Add(arg.Substring(5));
                }
                // Maintain case of file name.
                else if (File.Exists(arg))
                {
                    args.Add(arg);
                }
                else
                {
                    args.Add(arg.ToLower());
                }
            }
            if (args.Count > 0)
            {
                if (args.Contains("-file"))
                {
                    if (args.IndexOf("-file") + 1 >= args.Count || !File.Exists(args[args.IndexOf("-file") + 1]))
                    {
                        MessageBox.Show("The -file argument should be the full path to a PS1 or BAT file that you want to package.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(1);
                        return;
                    }
                    else
                    {
                        targetPath = args[args.IndexOf("-file") + 1];
                    }
                }
            }
            InitializeComponent();
            WPF_Auto_Update.Updater.RemoteFileURI = "https://lucency.co/Downloads/" + WPF_Auto_Update.Updater.FileName;
            WPF_Auto_Update.Updater.ServiceURI = "https://lucency.co/Services/VersionCheck.cshtml?Path=/Downloads/" + WPF_Auto_Update.Updater.FileName;
            WPF_Auto_Update.Updater.UpdateTimeout = Duration.Forever;
            WPF_Auto_Update.Updater.CheckCommandLineArgs();
            WPF_Auto_Update.Updater.CheckForUpdates(true);
        }
        private void Current_DispatcherUnhandledException(Object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            if (args.Contains("-silent"))
            {
                return;
            }
            var sb = new StringBuilder();
            sb.AppendLine("Oops.  There was an error.  Please report the below message.");
            sb.AppendLine();
            sb.AppendLine("Error: " + e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
            sb.AppendLine();
            var error = e.Exception;
            while (error.InnerException != null)
            {
                sb.AppendLine(error.InnerException.Message + Environment.NewLine + error.InnerException.StackTrace);
                sb.AppendLine();
                error = error.InnerException;
            }
            MessageBox.Show(sb.ToString(), "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void Window_Loaded(Object sender, RoutedEventArgs e)
        {
            if (targetPath != null)
            {
                this.Hide();
                try
                {
                    Clipboard.SetImage(new BitmapImage(new Uri(targetPath.Trim())));
                    await ShowTooltip("Image copied to clipboard!");
                }
                catch
                {
                }
                this.Close();
            }
            else
            {
                if (File.Exists(installedPath))
                {
                    buttonInstall.IsEnabled = false;
                    buttonRemove.IsEnabled = true;
                }
                else
                {
                    buttonInstall.IsEnabled = true;
                    buttonRemove.IsEnabled = false;
                }
            }

        }

        private async Task ShowTooltip(string v)
        {
            var cursor = System.Windows.Forms.Cursor.Position;
            var tooltip = new ToolTip();
            tooltip.Content = "Image copied to clipboard!";
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
            tooltip.HorizontalOffset = cursor.X + 5;
            tooltip.VerticalOffset = cursor.Y;
            tooltip.IsOpen = true;
            await Task.Delay(2000);
            tooltip.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(2)));
            await Task.Delay(2500);
        }

        private void buttonInstall_Click(Object sender, RoutedEventArgs e)
        {
            var assem = System.Reflection.Assembly.GetExecutingAssembly();
            var reStream = assem.GetManifestResourceStream("Copy_Image.Assets.ImageReg.reg");
            var reRead = new StreamReader(reStream);
            var content = reRead.ReadToEnd();
            reRead.Close();
            reStream.Close();
            File.WriteAllText(Path.GetTempPath() + @"\ImageReg.reg", content);
            var psi = new ProcessStartInfo("cmd.exe", String.Format("/c reg.exe import {0}\\ImageReg.reg&mkdir \"{1}\"&copy \"{2}\" \"{3}\" /y", Path.GetTempPath(), Path.GetDirectoryName(installedPath), Application.ResourceAssembly.ManifestModule.Assembly.Location, installedPath));
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.Verb = "runas";
            try
            {
                var proc = Process.Start(psi);
                proc.WaitForExit();
            }
            catch
            {
                return;
            }
            buttonInstall.IsEnabled = false;
            buttonRemove.IsEnabled = true;
            MessageBox.Show("Install completed!  If the 'Copy Image' option isn't showing up, reset your program defaults and reinstall Copy-Image.", "Install Completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void buttonRemove_Click(Object sender, RoutedEventArgs e)
        {
            var psi = new ProcessStartInfo("cmd.exe", "/c rd \"" + Path.GetDirectoryName(installedPath) + @""" /s /q&reg.exe delete HKCR\*\shell\CopyImage /f");
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.Verb = "runas";
            try
            {
                var proc = Process.Start(psi);
                proc.WaitForExit();
            }
            catch
            {
                return;
            }
            MessageBox.Show("Copy-Image has been removed.", "Uninstall Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            buttonInstall.IsEnabled = true;
            buttonRemove.IsEnabled = false;
        }

        private void buttonInfo_Click(Object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }
    }
}
