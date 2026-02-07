using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Tibia.Protobuf.Staticdata;
using static Canary_monster_editor.Data;

namespace Canary_monster_editor
{
    public class InternalItemList
    {
        public InternalItemList(string name, uint id)
        {
            this.name = name;
            this.id = id;
        }

        public string name { get; set; }
        public uint id { get; set; }
    }
    public enum ListType
    {
        Monsters = 0,
        Bosses = 1
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool HasChangeMade = false;
        public bool HasGlobalChangeMade = false;
        public InternalItemList SelectedCreature = null;
        public ListType SelectedListType_t = ListType.Monsters;
        private bool _isSearchReloading = false;

        private static readonly string InternalFacebookUri = "https://fb.me/otservbrasil";
        private static readonly string InternalGithubUri = "https://github.com/opentibiabr";
        private static readonly string InternalDiscordUri = "https://discordapp.com/invite/3NxYnyV";
        private static readonly string InternalForumUri = "https://forums.otserv.com.br/";
        public MainWindow()
        {
            InitializeComponent();
            InitializeCultureTexts();
        }

        #region 'Monster', 'bosses' and "Export/Import" button. (Secondary)
        private void ParseSecondaryButtonClick(string name)
        {
            if (string.IsNullOrEmpty(name)) {
                return;
            }

            switch (name) {
                case "MonsterButton_rectangle": {
                        if (SelectedListType_t == ListType.Monsters) {
                            return;
                        }

                        if (HasChangeMade) {
                            MessageBoxResult warnResult = MessageBox.Show(this, String.Format(GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChanges),
                                (SelectedListType_t == ListType.Monsters ? GetCultureText(TranslationDictionaryIndex.Monster).ToLower() : GetCultureText(TranslationDictionaryIndex.Boss).ToLower())),
                                GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesTitle),
                                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                            if (warnResult == MessageBoxResult.No) {
                                return;
                            }
                            HasChangeMade = false;
                        }

                        MainList_textblock.Text = GetCultureText(TranslationDictionaryIndex.Monsters);
                        SelectedListType_t = ListType.Monsters;
                        ReloadMainListBox();
                        MainList_listbox.SelectedItem = null;
                        MainList_listbox.SelectedIndex = 0;
                        break;
                    }
                case "BossButton_rectangle": {
                        if (SelectedListType_t == ListType.Bosses) {
                            return;
                        }

                        if (HasChangeMade) {
                            MessageBoxResult warnResult = MessageBox.Show(this, String.Format(GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChanges),
                                (SelectedListType_t == ListType.Monsters ? GetCultureText(TranslationDictionaryIndex.Monster).ToLower() : GetCultureText(TranslationDictionaryIndex.Boss).ToLower())),
                                GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesTitle),
                                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                            if (warnResult == MessageBoxResult.No) {
                                return;
                            }
                            HasChangeMade = false;
                        }

                        MainList_textblock.Text = GetCultureText(TranslationDictionaryIndex.Bosses);
                        SelectedListType_t = ListType.Bosses;
                        ReloadMainListBox();
                        MainList_listbox.SelectedItem = null;
                        MainList_listbox.SelectedIndex = 0;
                        break;
                    }
                default:
                    break;
            }
        }
        private void ExportImportButtonMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            if (Data.GlobalStaticData == null) {
                MessageBox.Show("Please open a static data file first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ExportImportWindow exportImportWindow = new ExportImportWindow();
            exportImportWindow.Owner = this;
            exportImportWindow.ShowDialog();
        }
        private void SecondaryButtonMouseEnter_rectangle(object sender, MouseEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle)) {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0.25;
        }
        private void SecondaryButtonMouseLeave_rectangle(object sender, MouseEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle)) {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0;
        }
        private void SecondaryButtonMouseDown_rectangle(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle) || e.LeftButton != MouseButtonState.Pressed) {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0.50;
        }
        private void SecondaryButtonMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle) || e.LeftButton == MouseButtonState.Pressed) {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0.25;
            ParseSecondaryButtonClick(rect.Name);
        }
        #endregion

        #region 'Open', 'Compile', 'new', 'delete' and 'save' button. (Main)
        private void ParseMainButtonClick(string name)
        {
            if (string.IsNullOrEmpty(name)) {
                return;
            }

            switch (name) {
                case "MainButton_rectangle": {
                        if (GlobalStaticData == null) {
                            OpenFileDialog openFileDialog = new OpenFileDialog
                            {
                                Title = GetCultureText(TranslationDictionaryIndex.SelectStaticDataFile),
                                Filter = GetCultureText(TranslationDictionaryIndex.SelectStaticDataFileFilter),
                                FilterIndex = 1,
                                Multiselect = false,
                                CheckFileExists = true,
                            };

                            if (!(bool)openFileDialog.ShowDialog()) {
                                return;
                            }

                            if (!LoadStaticDataProbufBinaryFileFromPath(openFileDialog.FileName)) {
                                return;
                            }

                            MainButon_textblock.Text = GetCultureText(TranslationDictionaryIndex.Compile).ToUpper();
                            LastSave_textblock.Text = GetCultureText(TranslationDictionaryIndex.LastSaved) + GlobalFileLastTimeEdited.ToString();
                            FileOpenned_textblock.Text = GetCultureText(TranslationDictionaryIndex.FileOpenned) + GlobalStaticDataPath;
                            ReloadMainListBox();
                            return;
                        }

                        if (HasChangeMade) {
                            MessageBoxResult warnResult = MessageBox.Show(this, String.Format(GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChanges),
                                (SelectedListType_t == ListType.Monsters ? GetCultureText(TranslationDictionaryIndex.Monster).ToLower() : GetCultureText(TranslationDictionaryIndex.Boss).ToLower())),
                                GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesTitle),
                                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                            if (warnResult == MessageBoxResult.No) {
                                return;
                            }
                            HasChangeMade = false;
                        }

                        if (!HasGlobalChangeMade) {
                            MessageBox.Show($"Nothing to compile.", "Error", MessageBoxButton.OK);
                            return;
                        }

                        HasGlobalChangeMade = false;
                        SaveStaticDataProtobufBinaryFile();
                        LastSave_textblock.Text = GetCultureText(TranslationDictionaryIndex.LastSaved) + GlobalFileLastTimeEdited.ToString();
                        break;
                    }
                case "ShowDelete_rectangle": {
                        if (GlobalStaticData == null || SelectedCreature == null) {
                            return;
                        }

                        MessageBoxResult warnResult = MessageBox.Show(this, String.Format(GetCultureText(TranslationDictionaryIndex.DeleteObject),
                            (SelectedListType_t == ListType.Monsters ? GetCultureText(TranslationDictionaryIndex.Monster).ToLower() : GetCultureText(TranslationDictionaryIndex.Boss).ToLower()),
                            SelectedCreature.id.ToString(), SelectedCreature.name),
                            GetCultureText(TranslationDictionaryIndex.DeleteObjectTitle),
                            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        if (warnResult == MessageBoxResult.No) {
                            return;
                        }

                        HasChangeMade = false;
                        HasGlobalChangeMade = true;
                        if (SelectedListType_t == ListType.Bosses && DeleteBossByd(SelectedCreature.id)) {
                            ReloadMainListBox();
                            MainList_listbox.SelectedItem = null;
                            MainList_listbox.SelectedIndex = 0;
                        } else if (SelectedListType_t == ListType.Monsters && DeleteMonsterByRaceId(SelectedCreature.id)) {
                            ReloadMainListBox();
                            MainList_listbox.SelectedItem = null;
                            MainList_listbox.SelectedIndex = 0;
                        }

                        break;
                    }
                case "ShowNew_rectangle": {
                        if (GlobalStaticData == null) {
                            return;
                        }

                        if (HasChangeMade) {
                            MessageBoxResult warnResult = MessageBox.Show(this, String.Format(GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChanges),
                                (SelectedListType_t == ListType.Monsters ? GetCultureText(TranslationDictionaryIndex.Monster).ToLower() : GetCultureText(TranslationDictionaryIndex.Boss).ToLower())),
                                GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesTitle),
                                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                            if (warnResult == MessageBoxResult.No) {
                                return;
                            }
                            HasChangeMade = false;
                        }

                        MessageBoxResult warnResult2 = MessageBox.Show(this, String.Format(GetCultureText(TranslationDictionaryIndex.NewObject),
                            (SelectedListType_t == ListType.Monsters ? GetCultureText(TranslationDictionaryIndex.Monster).ToLower() : GetCultureText(TranslationDictionaryIndex.Boss).ToLower())),
                            GetCultureText(TranslationDictionaryIndex.NewObjectTitle),
                            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        if (warnResult2 == MessageBoxResult.No) {
                            return;
                        }

                        if (SelectedListType_t == ListType.Monsters) {
                            CreateBrandNewMonster();
                        } else if (SelectedListType_t == ListType.Bosses) {
                            CreateBrandNewBoss();
                        }

                        ReloadMainListBox();
                        MainList_listbox.SelectedIndex = MainList_listbox.Items.Count - 1;
                        HasGlobalChangeMade = true;
                        break;
                    }
                case "ShowSave_rectangle": {
                        if (SelectedCreature == null || !HasChangeMade) {
                            return;
                        }

                        HasChangeMade = false;
                        HasGlobalChangeMade = true;
                        if (SelectedListType_t == ListType.Monsters) {
                            Monster monster = GetMonsterByRaceId(SelectedCreature.id);
                            if (monster == null) {
                                return;
                            }

                            monster.Name = ShowName_textbox.Text;
                            if (monster.AppearanceType == null) {
                                monster.AppearanceType = new Appearance_Type();
                            }

                            uint parsedUint = 0;
                            uint.TryParse(ShowLookType_textbox.Text, out parsedUint);
                            monster.AppearanceType.Outfittype = 0;
                            parsedUint = 0;

                            uint.TryParse(ShowAddon_textbox.Text, out parsedUint);
                            monster.AppearanceType.Outfitaddon = parsedUint;
                            parsedUint = 0;

                            if (monster.AppearanceType.Colors == null) {
                                 monster.AppearanceType.Colors = new Tibia.Protobuf.Staticdata.Colors();
                            }

                            uint.TryParse(ShowLookHead_textbox.Text, out parsedUint);
                            monster.AppearanceType.Colors.Lookhead = parsedUint;
                            parsedUint = 0;

                            uint.TryParse(ShowLookBody_textbox.Text, out parsedUint);
                            monster.AppearanceType.Colors.Lookbody = parsedUint;
                            parsedUint = 0;

                            uint.TryParse(ShowLookLegs_textbox.Text, out parsedUint);
                            monster.AppearanceType.Colors.Looklegs = parsedUint;
                            parsedUint = 0;

                            uint.TryParse(ShowLookFeet_textbox.Text, out parsedUint);
                            monster.AppearanceType.Colors.Lookfeet = parsedUint;
                            parsedUint = 0;

                            uint.TryParse(ShowLookTypeEx_textbox.Text, out parsedUint);
                            monster.AppearanceType.Itemtype = parsedUint;

                        } else if (SelectedListType_t == ListType.Bosses) {
                            Boss boss = GetBossById(SelectedCreature.id);
                            if (boss == null) {
                                return;
                            }

                            boss.Name = ShowName_textbox.Text;
                            if (GlobalBossAppearancesObjects) {
                                if (boss.AppearanceType == null) {
                                    boss.AppearanceType = new Appearance_Type();
                                }

                                uint parsedUint = 0;
                                uint.TryParse(ShowLookType_textbox.Text, out parsedUint);
                                boss.AppearanceType.Outfittype = parsedUint;
                                parsedUint = 0;

                                uint.TryParse(ShowAddon_textbox.Text, out parsedUint);
                                boss.AppearanceType.Outfitaddon = parsedUint;
                                parsedUint = 0;

                                if (boss.AppearanceType.Colors == null) {
                                    boss.AppearanceType.Colors = new Tibia.Protobuf.Staticdata.Colors();
                                }

                                uint.TryParse(ShowLookHead_textbox.Text, out parsedUint);
                                boss.AppearanceType.Colors.Lookhead = parsedUint;
                                parsedUint = 0;

                                uint.TryParse(ShowLookBody_textbox.Text, out parsedUint);
                                boss.AppearanceType.Colors.Lookbody = parsedUint;
                                parsedUint = 0;

                                uint.TryParse(ShowLookLegs_textbox.Text, out parsedUint);
                                boss.AppearanceType.Colors.Looklegs = parsedUint;
                                parsedUint = 0;

                                uint.TryParse(ShowLookFeet_textbox.Text, out parsedUint);
                                boss.AppearanceType.Colors.Lookfeet = parsedUint;
                                parsedUint = 0;

                                uint.TryParse(ShowLookTypeEx_textbox.Text, out parsedUint);
                                boss.AppearanceType.Itemtype = parsedUint;
                            }
                        } else {
                            return;
                        }

                        SelectedCreature.name = ShowName_textbox.Text;
                        MainList_listbox.Items.Refresh();
                        break;
                    }
                default:
                    break;
            }
        }
        private void MainButtonMouseDown_rectangle(object sender, MouseEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle) || e.LeftButton != MouseButtonState.Pressed) {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 1;
        }
        private void MainButtonMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle) || e.LeftButton == MouseButtonState.Pressed) {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0.75;
            ParseMainButtonClick(rect.Name);
        }
        private void MainButtonMouseLeave_rectangle(object sender, MouseEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle)) {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0.75;
        }
        #endregion

        #region 'Close', 'minimize' and 'resize' button.
        private void MainCloseMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            OnClosed(e);
        }
        private void MainResizeMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized) {
                WindowState = WindowState.Normal;
            } else {
                WindowState = WindowState.Maximized;
            }
        }
        private void MainMinimizeMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion

        #region Global and internal functions.
        private void TextChanged_textblock(object sender, TextChangedEventArgs e)
        {
            if (SelectedCreature == null) {
                return;
            }

            HasChangeMade = true;
        }
        protected override void OnClosed(EventArgs e)
        {
            if (HasGlobalChangeMade) {
                MessageBoxResult warnResult = MessageBox.Show(this, GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesOnAssets),
                    GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesOnAssetsTitle),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (warnResult == MessageBoxResult.No) {
                    return;
                }
                HasGlobalChangeMade = false;
            }

            base.OnClosed(e);
            Application.Current.Shutdown();
        }
        private void MouseDownToDrag_var(object sender, MouseButtonEventArgs e)
        {
            try { DragMove(); } catch { }
        }
        private void MainListSelectionChange_listbox(object sender, SelectionChangedEventArgs e)
        {
            if (sender.GetType() != typeof(ListBox)) {
                return;
            }

            // Skip discard warning during search reloads
            if (SelectedCreature != null && HasChangeMade && !_isSearchReloading) {
                MessageBoxResult warnResult = MessageBox.Show(this, String.Format(GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChanges),
                    (SelectedListType_t == ListType.Monsters ? GetCultureText(TranslationDictionaryIndex.Monster).ToLower() : GetCultureText(TranslationDictionaryIndex.Boss).ToLower())),
                    GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesTitle),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (warnResult == MessageBoxResult.No) {
                    return;
                }
            }

            HasChangeMade = false;
            ListBox listBox = (ListBox)sender;
            InternalItemList selectedItem = listBox.SelectedItem as InternalItemList;
            SelectedCreature = selectedItem;
            ReloadShowGrid();
        }
        private void Search_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _isSearchReloading = true;
            ReloadMainListBox();
            _isSearchReloading = false;
        }
        private void MainURIMouseDown_rectangle(object sender, MouseButtonEventArgs e)
        {
            string name = string.Empty;
            if (sender.GetType() == typeof(Rectangle)) {
                var rect = (Rectangle)sender;
                name = rect.Name;
            } else if (sender.GetType() == typeof(Image)) {
                var img = (Image)sender;
                name = img.Name;
            }

            switch (name) {
                case "MainFacebook_rectangle": {
                        System.Diagnostics.Process.Start(InternalFacebookUri);
                        break;
                    }
                case "MainGithub_rectangle": {
                        System.Diagnostics.Process.Start(InternalGithubUri);
                        break;
                    }
                case "MainDiscord_rectangle": {
                        System.Diagnostics.Process.Start(InternalDiscordUri);
                        break;
                    }
                case "MainForum_Image": {
                        System.Diagnostics.Process.Start(InternalForumUri);
                        break;
                    }
                default:
                    break;
            }
        }
        #endregion

        #region Culture (Translation)
        private void CultureMouseDown_image(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType() != typeof(Image) || e.LeftButton != MouseButtonState.Pressed) {
                return;
            }

            var img = (Image)sender;
            switch (img.Name) {
                case "PTBR_image": {
                        if (GlobalTranslationType == TranslationCulture_t.Portuguese) {
                            return;
                        }

                        GlobalTranslationType = TranslationCulture_t.Portuguese;
                        InitializeCultureTexts();
                        break;
                    }
                case "ENUS_image": {
                        if (GlobalTranslationType == TranslationCulture_t.English) {
                            return;
                        }

                        GlobalTranslationType = TranslationCulture_t.English;
                        InitializeCultureTexts();
                        break;
                    }
                default:
                    break;
            }

            if (img.Parent == null || img.Parent.GetType() != typeof(StackPanel)) {
                return;
            }

            var stackPanel = (StackPanel)img.Parent;
            foreach (var child in stackPanel.Children) {
                if (child == null || child.GetType() != typeof(Image)) {
                    continue;
                }

                var childImg = (Image)child;
                if (childImg.Name != img.Name) {
                    childImg.Opacity = 0.5;
                } else {
                    childImg.Opacity = 1;
                }
            }
        }
        private void InitializeCultureTexts()
        {
            if (GlobalStaticData == null) {
                MainButon_textblock.Text = GetCultureText(TranslationDictionaryIndex.Open).ToUpper();
            } else {
                MainButon_textblock.Text = GetCultureText(TranslationDictionaryIndex.Compile).ToUpper();
            }
            MonsterButon_textblock.Text = GetCultureText(TranslationDictionaryIndex.Monsters);
            BossButon_textblock.Text = GetCultureText(TranslationDictionaryIndex.Bosses);
            Author_textBlock.Text = GetCultureText(TranslationDictionaryIndex.Author) + "Marcosvf132";
            ToolVersion_textBlock.Text = GlobalVersion;
            FileOpenned_textblock.Text = GetCultureText(TranslationDictionaryIndex.FileOpenned) + GlobalStaticDataPath;
            LastSave_textblock.Text = GetCultureText(TranslationDictionaryIndex.LastSaved) + GlobalFileLastTimeEdited.ToString();
            ShowName_textblock.Text = GetCultureText(TranslationDictionaryIndex.Name);
            ShowDelete_textblock.Text = GetCultureText(TranslationDictionaryIndex.Delete);
            ShowNew_textblock.Text = GetCultureText(TranslationDictionaryIndex.New);
            ShowSave_textblock.Text = GetCultureText(TranslationDictionaryIndex.Save);
            ToolName_textblock.Text = "Canary monster editor " + GlobalVersion;
            ExportImportButon_textblock.Text = GetCultureText(TranslationDictionaryIndex.ExportImport);
            SearchPlaceholder_textblock.Text = GetCultureText(TranslationDictionaryIndex.SearchPlaceholder);

            if (SelectedListType_t == ListType.Monsters) {
                MainList_textblock.Text = GetCultureText(TranslationDictionaryIndex.Monsters);
                BossAppearance_textblock.Text = String.Empty;
            } else {
                MainList_textblock.Text = GetCultureText(TranslationDictionaryIndex.Bosses);
                if (!GlobalBossAppearancesObjects) {
                    BossAppearance_textblock.Text = GetCultureText(TranslationDictionaryIndex.BossAppearanceDisabled);
                } else {
                    BossAppearance_textblock.Text = String.Empty;
                }
            }
        }
        #endregion

        #region Load and initializers
        public void ReloadMainListBox()
        {
            if (GlobalStaticData == null) {
                return;
            }

            string filter = string.Empty;
            if (Search_textbox != null) {
                filter = Search_textbox.Text?.Trim().ToLower() ?? string.Empty;
            }

            MainList_listbox.BeginInit();
            MainList_listbox.Items.Clear();

            if (SelectedListType_t == ListType.Monsters) {
                foreach (var monster in GlobalStaticData.Monster) {
                    // Null-safe comparison
                    bool matches = string.IsNullOrEmpty(filter) ||
                                (monster.Name ?? string.Empty).ToLower().Contains(filter) ||
                                monster.Raceid.ToString().Contains(filter);
                    if (matches) {
                        MainList_listbox.Items.Add(new InternalItemList(monster.Name ?? string.Empty, monster.Raceid));
                    }
                }
            } else if (SelectedListType_t == ListType.Bosses) {
                foreach (var boss in GlobalStaticData.Boss) {
                    // Null-safe comparison
                    bool matches = string.IsNullOrEmpty(filter) ||
                                (boss.Name ?? string.Empty).ToLower().Contains(filter) ||
                                boss.Id.ToString().Contains(filter);
                    if (matches) {
                        MainList_listbox.Items.Add(new InternalItemList(boss.Name ?? string.Empty, boss.Id));
                    }
                }
            }

            MainList_listbox.EndInit();
        }
        private void ReloadShowGrid()
        {
            if (SelectedCreature == null) {
                return;
            }

            if (SelectedListType_t == ListType.Bosses) {
                Boss boss = GetBossById(SelectedCreature.id);
                if (boss == null) {
                    return;
                }

                ShowRaceId_textblock.Text = "ID: " + boss.Id.ToString();
                SelectedCreature.name = boss.Name;
                ShowName_textbox.Text = boss.Name;

                if (GlobalBossAppearancesObjects) {
                    ShowLookType_textbox.Text = boss.AppearanceType != null ? boss.AppearanceType.Outfittype.ToString() : "";
                    ShowLookTypeEx_textbox.Text = boss.AppearanceType != null ? (boss.AppearanceType.Outfittype == 0 ? boss.AppearanceType.Itemtype.ToString() : "") : "";
                    ShowAddon_textbox.Text = boss.AppearanceType != null ? boss.AppearanceType.Outfitaddon.ToString() : "";
                    ShowLookHead_textbox.Text = boss.AppearanceType != null ? (boss.AppearanceType.Colors != null ? boss.AppearanceType.Colors.Lookhead.ToString() : "") : "";
                    ShowLookBody_textbox.Text = boss.AppearanceType != null ? (boss.AppearanceType.Colors != null ? boss.AppearanceType.Colors.Lookbody.ToString() : "") : "";
                    ShowLookLegs_textbox.Text = boss.AppearanceType != null ? (boss.AppearanceType.Colors != null ? boss.AppearanceType.Colors.Looklegs.ToString() : "") : "";
                    ShowLookFeet_textbox.Text = boss.AppearanceType != null ? (boss.AppearanceType.Colors != null ? boss.AppearanceType.Colors.Lookfeet.ToString() : "") : "";
                } else {
                    ShowLookType_textbox.IsEnabled = false;
                    ShowLookTypeEx_textbox.IsEnabled = false;
                    ShowAddon_textbox.IsEnabled = false;
                    ShowLookHead_textbox.IsEnabled = false;
                    ShowLookBody_textbox.IsEnabled = false;
                    ShowLookLegs_textbox.IsEnabled = false;
                    ShowLookFeet_textbox.IsEnabled = false;

                    ShowLookType_textbox.Text = "";
                    ShowLookTypeEx_textbox.Text = "";
                    ShowAddon_textbox.Text = "";
                    ShowLookHead_textbox.Text = "";
                    ShowLookBody_textbox.Text = "";
                    ShowLookLegs_textbox.Text = "";
                    ShowLookFeet_textbox.Text = "";
                    BossAppearance_textblock.Text = GetCultureText(TranslationDictionaryIndex.BossAppearanceDisabled);
                    BossAppearance_textblock.Opacity = 1;
                }

            } else {
                Monster monster = GetMonsterByRaceId(SelectedCreature.id);
                if (monster == null) {
                    return;
                }

                ShowRaceId_textblock.Text = "ID: " + monster.Raceid.ToString();
                SelectedCreature.name = monster.Name;
                ShowName_textbox.Text = monster.Name;

                if (!GlobalBossAppearancesObjects) {
                    ShowLookType_textbox.IsEnabled = true;
                    ShowLookTypeEx_textbox.IsEnabled = true;
                    ShowAddon_textbox.IsEnabled = true;
                    ShowLookHead_textbox.IsEnabled = true;
                    ShowLookBody_textbox.IsEnabled = true;
                    ShowLookLegs_textbox.IsEnabled = true;
                    ShowLookFeet_textbox.IsEnabled = true;
                    BossAppearance_textblock.Text = "";
                    BossAppearance_textblock.Opacity = 0;
                }

                ShowLookType_textbox.Text = monster.AppearanceType != null ? monster.AppearanceType.Outfittype.ToString() : "";
                ShowLookTypeEx_textbox.Text = monster.AppearanceType != null ? (monster.AppearanceType.Outfittype == 0 ? monster.AppearanceType.Itemtype.ToString() : "") : "";
                ShowAddon_textbox.Text = monster.AppearanceType != null ? monster.AppearanceType.Outfitaddon.ToString() : "";
                ShowLookHead_textbox.Text = monster.AppearanceType != null ? (monster.AppearanceType.Colors != null ? monster.AppearanceType.Colors.Lookhead.ToString() : "") : "";
                ShowLookBody_textbox.Text = monster.AppearanceType != null ? (monster.AppearanceType.Colors != null ? monster.AppearanceType.Colors.Lookbody.ToString() : "") : "";
                ShowLookLegs_textbox.Text = monster.AppearanceType != null ? (monster.AppearanceType.Colors != null ? monster.AppearanceType.Colors.Looklegs.ToString() : "") : "";
                ShowLookFeet_textbox.Text = monster.AppearanceType != null ? (monster.AppearanceType.Colors != null ? monster.AppearanceType.Colors.Lookfeet.ToString() : "") : "";
            }

            HasChangeMade = false;
        }
        #endregion

    }
}
