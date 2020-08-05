using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CometChar;
using System.IO;
using System.Diagnostics;

namespace CometCharGUI
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnSearchROM_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog odg = new OpenFileDialog();
            odg.Title = "Search ROM to patch";
            odg.AddExtension = true;
            odg.CheckFileExists = true;
            odg.DefaultExt = "*.z64";
            odg.Filter = "Super Mario 64 ROM | *.z64";
            bool? _dres = odg.ShowDialog();

            if (_dres != null && (bool)_dres)
            {
                tbROM.Text = odg.FileName;
            }
        }

        private void btnSearchPatch_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog odg = new OpenFileDialog();
            odg.Title = "Search CMTP file to patch from";
            odg.AddExtension = true;
            odg.CheckFileExists = true;
            odg.DefaultExt = "*.cmtp";
            odg.Filter = "ComatCHAR Patch | *.cmtp";
            bool? _dres = odg.ShowDialog();

            if (_dres != null && (bool)_dres)
            {
                tbPatch.Text = odg.FileName;
            }
        }

        private void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sdg = new SaveFileDialog();
            sdg.Title = "Save patched ROM as...";
            sdg.AddExtension = true;
            sdg.DefaultExt = "*.z64";
            sdg.Filter = "Big-Endian N64 ROM| *.z64";
            bool? _dres = sdg.ShowDialog();

            if (_dres != null && (bool)_dres)
            {
                tbSaveAs.Text = sdg.FileName;
            }
        }

        private void btnPatch_Click(object sender, RoutedEventArgs e)
        {
            string romPath = tbROM.Text;
            string patchPath = tbPatch.Text;
            string saveRomPath = tbSaveAs.Text;
            if (File.Exists(romPath) && File.Exists(tbPatch.Text))
            {
                try
                {
                    int fileSize;
                    bool usingTemp = false;
                    using (FileStream fs = new FileStream(tbROM.Text, FileMode.Open))
                    {
                        fileSize = (int)Math.Truncate((double)(fs.Length / 1000000));
                    }

                    bool valid = Patch.CheckROMBigEndian(romPath);

                    if (valid)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            pbProgress.Value = 0;
                            tbPatch.IsReadOnly = true;
                            tbROM.IsReadOnly = true;
                            tbSaveAs.IsReadOnly = true;
                            btnPatch.IsEnabled = false;
                        });

                        if (fileSize == 8)
                        {
                            MessageBoxResult _dresult = MessageBox.Show("This ROM is a 8MB ROM.\nYour ROM will now be expanded to 64MB to proceed with the patching.\nDO NOTE THAT THIS PATCH FORMAT DOES NOT WORK IN ROMS BUILT FROM DECOMP.\nContinue?", "ROM is small!", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (_dresult == MessageBoxResult.Yes)
                            {
                                Dispatcher.Invoke(() => pbProgress.IsIndeterminate = true);
                                usingTemp = true;
                                Task prepareROM = Task.Run(async () =>
                                {
                                    romPath = await Task.Run(() => PrepareVanillaROM(romPath));
                                    pbProgress.Dispatcher.Invoke(() => pbProgress.IsIndeterminate = false);
                                    patchFile();
                                });
                               
                            }
                        }
                        if (fileSize > 8 && fileSize < 64)
                        {
                            MessageBox.Show("The size of this ROM is not 8MB or 64MB. This is not supported so far.", "Invalid ROM size", MessageBoxButton.OK, MessageBoxImage.Error);
                            Dispatcher.Invoke(() =>
                            {
                                tbPatch.IsReadOnly = false;
                                tbROM.IsReadOnly = false;
                                tbSaveAs.IsReadOnly = false;
                                btnPatch.IsEnabled = true;
                            });
                            return;
                        }
                        if (fileSize > 65)
                        {
                            patchFile();
                        }
                    }
                    else
                    {
                        MessageBox.Show("The ROM doesn't appear to be a valid ROM. Please make sure to use a valid SM64 ROM.", "Invalid file", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    void patchFile()
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Task.Run(() =>
                            {
                            IProgress<float> progress = new Progress<float>((i) => UpdateProgress(i));
                            using (FileStream cmtp = new FileStream(patchPath, FileMode.Open, FileAccess.Read))
                            {
                                Patch.PatchROM(romPath, cmtp, saveRomPath, progress);
                                if (usingTemp)
                                {
                                    File.Delete(romPath);
                                    Directory.Delete($"{AppDomain.CurrentDomain.BaseDirectory}\\temp", true);
                                }
                            }
                            MessageBox.Show("Success", "Patching complete", MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                            tbPatch.IsReadOnly = false;
                            tbROM.IsReadOnly = false;
                            tbSaveAs.IsReadOnly = false;
                            btnPatch.IsEnabled = true;
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while writing to the files.\n" + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("We cannot access either the ROM or the patch. Please make sure these are still at their original location and try again.", "Files missing", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        string PrepareVanillaROM(string romPath)
        {
            Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}\\temp");
            // Expand and extend ROM
            ProcessStartInfo _extendProcInfo = new ProcessStartInfo
            {
                FileName = $"{AppDomain.CurrentDomain.BaseDirectory}\\tools\\sm64extend.exe",
                Arguments = $"-a 16 -f \"{romPath}\" \"{AppDomain.CurrentDomain.BaseDirectory}\\temp\\rom.ext.z64\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process _extendProc = Process.Start(_extendProcInfo);
            romPath = $"{AppDomain.CurrentDomain.BaseDirectory}/temp/rom.ext.z64";
            _extendProc.WaitForExit();

            // Apply PPF
            ProcessStartInfo _ppfPatcherInfo = new ProcessStartInfo
            {
                FileName = $"{AppDomain.CurrentDomain.BaseDirectory}\\tools\\ApplyPPF3.exe",
                Arguments = $"a {AppDomain.CurrentDomain.BaseDirectory}/temp/rom.ext.z64 {AppDomain.CurrentDomain.BaseDirectory}\\tools\\obj_import195S.ppf",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process _ppfPatcherProc = Process.Start(_ppfPatcherInfo);
            _ppfPatcherProc.Start();
            _ppfPatcherProc.WaitForExit();

            // More miscellaneous bits
            using (FileStream fs = new FileStream(romPath, FileMode.Open, FileAccess.Write))
            {
                byte[] p1 = File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\binhacks\\101B84.bin");
                byte[] p2 = File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\binhacks\\1202000.bin");
                byte[] p3 = File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\binhacks\\1203000.bin");
                fs.Seek(0x101B84, SeekOrigin.Begin);
                fs.Write(p1, 0, p1.Length);
                fs.Seek(0x1202000, SeekOrigin.Begin);
                fs.Write(p2, 0, p2.Length);
                fs.Seek(0x1203000, SeekOrigin.Begin);
                fs.Write(p3, 0, p3.Length);
            }
            return romPath;
        }

        void UpdateProgress(float prog)
        {
            prog = (prog / 4) * 100;
            pbProgress.Dispatcher.Invoke(() => pbProgress.Value = prog);
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }
            return false;
        }

    }
}
