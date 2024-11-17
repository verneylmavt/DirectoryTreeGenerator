using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DirectoryTreeGenerator
{
    public partial class MainWindow : Window
    {
        private bool includeHidden = false;
        private bool isDarkTheme = false; // Light theme is default

        public MainWindow()
        {
            InitializeComponent();

            // Initialize includeHidden button tooltip
            UpdateHiddenItemsButton();

            // Initialize theme button tooltip and content
            btnToggleTheme.Content = "\uE706"; // Sun icon
            btnToggleTheme.ToolTip = "Switch to Dark Theme";
        }

        private void UpdateHiddenItemsButton()
        {
            if (includeHidden)
            {
                btnToggleHidden.Content = "\uE785"; // Eye icon
                btnToggleHidden.ToolTip = "Exclude Hidden Items";
            }
            else
            {
                btnToggleHidden.Content = "\uE72E"; // Eye with slash
                btnToggleHidden.ToolTip = "Include Hidden Items";
            }
        }

        private async void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            txtOutput.Text = "";
            var folderPath = SelectFolder();
            if (!string.IsNullOrEmpty(folderPath))
            {
                string basename = new DirectoryInfo(folderPath).Name;
                txtOutput.Text += basename + "/" + Environment.NewLine;
                await Task.Run(() => PrintDirectoryTree(folderPath));
            }
        }

        private void BtnCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            string textToCopy = string.IsNullOrEmpty(txtOutput.SelectedText) ? txtOutput.Text : txtOutput.SelectedText;
            Clipboard.SetText(textToCopy);
            MessageBox.Show("Copied", "Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSaveToFile_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Text File|*.txt",
                FileName = "DirectoryTree.txt"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, txtOutput.Text);
                MessageBox.Show("Saved", "File Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnToggleHidden_Click(object sender, RoutedEventArgs e)
        {
            includeHidden = !includeHidden;
            UpdateHiddenItemsButton();
        }

        private void BtnToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            if (isDarkTheme)
            {
                // Switch to Dark Theme
                ApplyTheme("DarkTheme.xaml");
                btnToggleTheme.Content = "\uE706"; // Sun icon
                btnToggleTheme.ToolTip = "Switch to Light Theme";
            }
            else
            {
                // Switch to Light Theme
                ApplyTheme("LightTheme.xaml");
                btnToggleTheme.Content = "\uE708"; // Moon icon
                btnToggleTheme.ToolTip = "Switch to Dark Theme";
            }
        }

        private void ApplyTheme(string themePath)
        {
            // Remove existing theme dictionaries
            RemoveThemeDictionary();

            // Add new theme
            var themeDict = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };
            Application.Current.Resources.MergedDictionaries.Add(themeDict);
        }

        private void RemoveThemeDictionary()
        {
            // Remove existing theme dictionaries
            var dictionariesToRemove = new List<ResourceDictionary>();
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (dictionary.Source != null &&
                    (dictionary.Source.OriginalString.EndsWith("LightTheme.xaml") ||
                     dictionary.Source.OriginalString.EndsWith("DarkTheme.xaml")))
                {
                    dictionariesToRemove.Add(dictionary);
                }
            }

            foreach (var dictionary in dictionariesToRemove)
            {
                Application.Current.Resources.MergedDictionaries.Remove(dictionary);
            }
        }

        private void PrintDirectoryTree(string dirPath, string prefix = "")
        {
            string[] entries;
            try
            {
                entries = Directory.GetFileSystemEntries(dirPath);
            }
            catch (UnauthorizedAccessException)
            {
                Dispatcher.Invoke(() =>
                {
                    txtOutput.Text += prefix + "[Access Denied]" + Environment.NewLine;
                });
                return;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return;
            }

            if (!includeHidden)
            {
                entries = Array.FindAll(entries, e =>
                {
                    var attr = File.GetAttributes(e);
                    return (attr & FileAttributes.Hidden) == 0;
                });
            }

            for (int i = 0; i < entries.Length; i++)
            {
                bool isLast = i == entries.Length - 1;
                string entry = entries[i];
                string entryName = Path.GetFileName(entry);
                bool isDirectory = Directory.Exists(entry);
                string symbol = isLast ? "└── " : "├── ";

                Dispatcher.Invoke(() =>
                {
                    txtOutput.Text += prefix + symbol + entryName + (isDirectory ? "/" : "") + Environment.NewLine;
                });

                if (isDirectory)
                {
                    string newPrefix = prefix + (isLast ? "    " : "│   ");
                    PrintDirectoryTree(entry, newPrefix);
                }
            }
        }

        private string SelectFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
            }
            return null;
        }

        // Drag-and-Drop Event Handlers
        private void MainGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private async void MainGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, true);
                if (droppedFiles.Length > 0 && Directory.Exists(droppedFiles[0]))
                {
                    txtOutput.Text = "";
                    string dirPath = droppedFiles[0];
                    string basename = new DirectoryInfo(dirPath).Name;
                    txtOutput.Text += basename + "/" + Environment.NewLine;
                    await Task.Run(() => PrintDirectoryTree(dirPath));
                }
            }
        }
    }
}