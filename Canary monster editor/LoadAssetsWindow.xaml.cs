using System.Windows;
using System.Windows.Input;
using static Canary_monster_editor.Data;
using System.IO;
using System.Linq;

namespace Canary_monster_editor
{
    public partial class LoadAssetsWindow : Window
    {
        public string StaticDataPath { get; private set; }
        public string AssetsPath { get; private set; }
        public bool LoadSuccessful { get; private set; } = false;

        public LoadAssetsWindow()
        {
            InitializeComponent();
            InitializeTexts();
  
            AssetsPath_textbox.IsEnabled = false;
            BrowseAssetsButton.IsEnabled = false;
        }

        private void InitializeTexts()
        {
            Title_textblock.Text = GetCultureText(TranslationDictionaryIndex.LoadAssetsAndStaticData);
            StaticDataPathLabel_textblock.Text = GetCultureText(TranslationDictionaryIndex.StaticDataPath);
            AssetsPathLabel_textblock.Text = GetCultureText(TranslationDictionaryIndex.AssetsPath);
            BrowseStaticDataButton.Content = GetCultureText(TranslationDictionaryIndex.Browse);
            BrowseAssetsButton.Content = GetCultureText(TranslationDictionaryIndex.Browse);
            LoadButton.Content = GetCultureText(TranslationDictionaryIndex.Load);
            CancelButton.Content = GetCultureText(TranslationDictionaryIndex.Cancel);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BrowseStaticData_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = GetCultureText(TranslationDictionaryIndex.SelectFolderStaticData),
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = GetCultureText(TranslationDictionaryIndex.Browse)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedPath = Path.GetDirectoryName(openFileDialog.FileName);
                StaticDataPath_textbox.Text = selectedPath;
                AssetsPath_textbox.Text = selectedPath;
                AssetsPath_textbox.IsEnabled = true;
                BrowseAssetsButton.IsEnabled = true;
            }
        }

        private void BrowseAssets_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = GetCultureText(TranslationDictionaryIndex.SelectFolderAssets),
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = GetCultureText(TranslationDictionaryIndex.Browse)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedPath = Path.GetDirectoryName(openFileDialog.FileName);
                AssetsPath_textbox.Text = selectedPath;
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(StaticDataPath_textbox.Text))
            {
                MessageBox.Show(GetCultureText(TranslationDictionaryIndex.SelectFolderStaticData),
                    GetCultureText(TranslationDictionaryIndex.ErrorTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(StaticDataPath_textbox.Text))
            {
                MessageBox.Show(GetCultureText(TranslationDictionaryIndex.ErrorStaticDataNotFound),
                    GetCultureText(TranslationDictionaryIndex.ErrorTitle), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var staticDataFiles = Directory.GetFiles(StaticDataPath_textbox.Text, "staticdata-*.dat", SearchOption.TopDirectoryOnly);
            if (staticDataFiles.Length == 0)
            {
                MessageBox.Show(GetCultureText(TranslationDictionaryIndex.ErrorStaticDataNotFound),
                    GetCultureText(TranslationDictionaryIndex.ErrorTitle), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(AssetsPath_textbox.Text))
            {
                MessageBox.Show(GetCultureText(TranslationDictionaryIndex.SelectFolderAssets),
                    GetCultureText(TranslationDictionaryIndex.ErrorTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(AssetsPath_textbox.Text))
            {
                MessageBox.Show(GetCultureText(TranslationDictionaryIndex.ErrorAssetsNotFound),
                    GetCultureText(TranslationDictionaryIndex.ErrorTitle), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string assetsPath = AssetsPath_textbox.Text;
            bool hasValidAssets = false;

            if (File.Exists(Path.Combine(assetsPath, "catalog-content.json")))
            {
                hasValidAssets = true;
            }

            else if (Directory.Exists(Path.Combine(assetsPath, "assets")) &&
                     File.Exists(Path.Combine(assetsPath, "assets", "catalog-content.json")))
            {
                hasValidAssets = true;
            }

            else
            {
                string[] datFiles = { "appearances.dat", "Tibia.dat" };

                foreach (var dat in datFiles)
                {
                    string baseName = Path.GetFileNameWithoutExtension(dat);
                    string spr = baseName + ".spr";

                    if (File.Exists(Path.Combine(assetsPath, dat)) &&
                        File.Exists(Path.Combine(assetsPath, spr)))
                    {
                        hasValidAssets = true;
                        break;
                    }
                }

                if (!hasValidAssets && Directory.Exists(Path.Combine(assetsPath, "assets")))
                {
                    string subPath = Path.Combine(assetsPath, "assets");
                    foreach (var dat in datFiles)
                    {
                        string baseName = Path.GetFileNameWithoutExtension(dat);
                        string spr = baseName + ".spr";

                        if (File.Exists(Path.Combine(subPath, dat)) &&
                            File.Exists(Path.Combine(subPath, spr)))
                        {
                            hasValidAssets = true;
                            break;
                        }
                    }
                }
            }

            if (!hasValidAssets)
            {
                MessageBox.Show(
                    GetCultureText(TranslationDictionaryIndex.ErrorInvalidAssetsFolder),
                    "Error - Invalid Assets Path",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            StaticDataPath = StaticDataPath_textbox.Text;
            AssetsPath = AssetsPath_textbox.Text;
            LoadSuccessful = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSuccessful = false;
            this.Close();
        }
    }
}