using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using CometChar;
using CometChar.Structs;
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
            if (File.Exists(romPath) && File.Exists(patchPath))
            {
                try
                {
                    int fileSize;
                    bool usingTemp = false;
                    using (FileStream fs = new FileStream(romPath, FileMode.Open))
                    {
                        fileSize = (int)Math.Truncate((double)(fs.Length / 1000000));
                    }

                    bool valid = Patch.CheckROMBigEndian(romPath);

                    using (FileStream cmfs = new FileStream(patchPath, FileMode.Open, FileAccess.Read))
                    {
                        PatchInformation _pI = Patch.ReadPatchFile(cmfs);
                        if (_pI.versionMinor > 1)
                        {
                            MessageBox.Show("This CMTP file's version is not supported by this version of the patcher. You may need to update your patcher or contact GlitchyPSI.");
                            return;
                        }
                    }
                    
                    if (valid)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            UpdateProgress(0);
                            tcMain.IsEnabled = false;
                        });

                        if (fileSize == 8)
                        {
                            MessageBoxResult _dresult = MessageBox.Show("This ROM is a 8MB ROM.\nCMTP ROM EXPANSION IS EXPERIMENTAL. THIS ROM WILL *NOT* BE COMPATIBLE WITH ROM MANAGER. YOU ARE STRONGLY ADVISED TO EXPAND YOUR ROM BY OPENING IT IN SM64 ROM MANAGER FIRST.\nAdditionally, this does NOT work with Decomp ROMs!\nAre you sure you wish to continue?", "ROM is small!", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
                            else
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    tcMain.IsEnabled = true;
                                });
                                return;
                            }
                        }
                        if (fileSize > 8 && fileSize < 64)
                        {
                            MessageBox.Show("This ROM may be an older SM64 hack. This is not supported right now.", "Invalid ROM size", MessageBoxButton.OK, MessageBoxImage.Error);
                            Dispatcher.Invoke(() =>
                            {
                                tcMain.IsEnabled = true;
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
                            tcMain.IsEnabled = true;
                            UpdateProgress(4);
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

        private void btnSearchROM_c_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog odg = new OpenFileDialog();
            odg.Title = "Search ROM to make a patch from";
            odg.AddExtension = true;
            odg.CheckFileExists = true;
            odg.DefaultExt = "*.z64";
            odg.Filter = "Modded Super Mario 64 ROM | *.z64";
            bool? _dres = odg.ShowDialog();

            if (_dres != null && (bool)_dres)
            {
                tbROM_c.Text = odg.FileName;
            }
        }

        private void btnSearchPatch_c_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sdg = new SaveFileDialog();
            sdg.Title = "Save CMTP Patch as...";
            sdg.AddExtension = true;
            sdg.DefaultExt = "*.cmtp";
            sdg.Filter = "v0.1 CometCHAR Patch | *.cmtp";
            bool? _dres = sdg.ShowDialog();

            if (_dres != null && (bool)_dres)
            {
                tbPatch_c.Text = sdg.FileName;
            }
        }

        private void btnCreatePatch_Click(object sender, RoutedEventArgs e)
        {
            string romPath = tbROM_c.Text;
            string patchPath = tbPatch_c.Text;
            if (File.Exists(romPath))
            {
                try
                {
                    int fileSize;
                    using (FileStream fs = new FileStream(romPath, FileMode.Open))
                    {
                        fileSize = (int)Math.Truncate((double)(fs.Length / 1000000));
                    }

                    bool valid = Patch.CheckROMBigEndian(romPath);

                    if (valid)
                    {
                        if (fileSize < 65)
                        {
                            MessageBox.Show("Are you sure this is a valid character mod?\nKeep in mind 8MB (Vanilla/Decomp), 24MB and 48MB mods are not supported yet.");
                        }
                        else if (fileSize > 65)
                        {
                            using (FileStream fs = new FileStream(romPath, FileMode.Open, FileAccess.Read))
                            {
                                Patch.CreatePatchFile(fs, patchPath);
                                MessageBox.Show("Success", "Patch creation complete", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("The ROM doesn't appear to be a valid ROM. Please make sure to use a valid SM64 ROM (Big-Endian).", "Invalid file", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }


                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while writing to the files.\n" + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("We cannot access the ROM. Please make sure it is still at its original location and try again.", "Files missing", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void btnSearchPatch_i_Click(object sender, RoutedEventArgs e)
        {
            PatchInformation _pI;
            OpenFileDialog odg = new OpenFileDialog();
            odg.Title = "Search CMTP Patch";
            odg.AddExtension = true;
            odg.CheckFileExists = true;
            odg.DefaultExt = "*.cmtp";
            odg.Filter = "CometCHAR Patch | *.cmtp";
            bool? _dres = odg.ShowDialog();


            if (_dres != null && (bool)_dres)
            {
                if (File.Exists(odg.FileName))
                {
                    tbPatch_i.Text = odg.FileName;
                    using (FileStream cmfs = new FileStream(odg.FileName, FileMode.Open, FileAccess.Read))
                    {
                        _pI = Patch.ReadPatchFile(cmfs);
                    }
                    Dispatcher.Invoke(() =>
                    {
                        txbCmtpInfoArea.Text = $"CMTP file version: v{_pI.versionMajor}.{_pI.versionMinor}.\n{((_pI.Features & 1) == 1 ? "This patch appears to be from a Blender / Fast64 / Extended Classic mod." : "This patch appears to be from a Classic mod.")}";
                    });
                }
                else
                {
                    MessageBox.Show("We cannot access the patch file. Please make sure it is still at its original location and try again.", "Files missing", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnDiscord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://comet.glitchypsi.xyz");
        }

        private void btnGithub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/projectcomet64");
        }
    }
}
