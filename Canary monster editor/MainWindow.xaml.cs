using Canary_monster_editor.Localization;
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
        Bosses = 1,
    }

    public partial class MainWindow : Window
    {
        public bool HasChangeMade = false;
        public bool HasGlobalChangeMade = false;
        public InternalItemList SelectedCreature = null;
        public ListType SelectedListType_t = ListType.Monsters;

        private static readonly string InternalFacebookUri = "https://fb.me/otservbrasil";
        private static readonly string InternalGithubUri = "https://github.com/opentibiabr";
        private static readonly string InternalDiscordUri = "https://discordapp.com/invite/3NxYnyV";
        private static readonly string InternalForumUri = "https://forums.otserv.com.br/";

        private bool HasLoadedStaticData
        {
            get { return GlobalStaticData != null; }
        }

        private bool IsBossList
        {
            get { return SelectedListType_t == ListType.Bosses; }
        }

        private bool IsMonsterList
        {
            get { return SelectedListType_t == ListType.Monsters; }
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeCultureTexts();
        }

        #region Secondary button helpers
        private void ParseSecondaryButtonClick(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            switch (name)
            {
                case "MonsterButton_rectangle":
                    SwitchListIfNeeded(ListType.Monsters);
                    break;
                case "BossButton_rectangle":
                    SwitchListIfNeeded(ListType.Bosses);
                    break;
                default:
                    break;
            }
        }

        private void SwitchListIfNeeded(ListType targetListType)
        {
            if (SelectedListType_t == targetListType)
            {
                return;
            }

            if (!TryDiscardPendingSelectionChanges())
            {
                return;
            }

            SelectedListType_t = targetListType;
            UpdateListHeaderText();
            ReloadMainListBox();
            ResetListSelection();
        }

        private bool TryDiscardPendingSelectionChanges()
        {
            if (!HasChangeMade)
            {
                return true;
            }

            var cultureIndex = IsMonsterList ? TranslationDictionaryIndex.Monster : TranslationDictionaryIndex.Boss;
            var warnResult = MessageBox.Show(this,
                string.Format(GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChanges), GetCultureText(cultureIndex).ToLower()),
                GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesTitle),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (warnResult == MessageBoxResult.No)
            {
                return false;
            }

            HasChangeMade = false;
            return true;
        }

        private void ResetListSelection()
        {
            MainList_listbox.SelectedItem = null;
            if (MainList_listbox.Items.Count > 0)
            {
                MainList_listbox.SelectedIndex = 0;
            }
        }
        #endregion

        #region Secondary button UI events
        private void ExportImportButtonMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            if (!HasLoadedStaticData)
            {
                MessageBox.Show("Please open a static data file first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ExportImportWindow exportImportWindow = new ExportImportWindow();
            exportImportWindow.Owner = this;
            exportImportWindow.ShowDialog();
        }

        private void SecondaryButtonMouseEnter_rectangle(object sender, MouseEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle))
            {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0.25;
        }

        private void SecondaryButtonMouseLeave_rectangle(object sender, MouseEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle))
            {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0;
        }

        private void SecondaryButtonMouseDown_rectangle(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle) || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0.50;
        }

        private void SecondaryButtonMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle) || e.LeftButton == MouseButtonState.Pressed)
            {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0.25;
            ParseSecondaryButtonClick(rect.Name);
        }
        #endregion

        #region Main button helpers
        private void ParseMainButtonClick(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            switch (name)
            {
                case "MainButton_rectangle":
                    HandleOpenOrCompileAction();
                    break;
                case "ShowDelete_rectangle":
                    HandleDeleteAction();
                    break;
                case "ShowNew_rectangle":
                    HandleNewAction();
                    break;
                case "ShowSave_rectangle":
                    HandleSaveAction();
                    break;
                default:
                    break;
            }
        }

        private void HandleOpenOrCompileAction()
        {
            if (!HasLoadedStaticData)
            {
                OpenStaticData();
                return;
            }

            if (!HasChangeMade)
            {
                CompileChangesIfNeeded();
                return;
            }

            if (!TryDiscardPendingSelectionChanges())
            {
                return;
            }

            CompileChangesIfNeeded();
        }

        private void OpenStaticData()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = GetCultureText(TranslationDictionaryIndex.SelectStaticDataFile),
                Filter = GetCultureText(TranslationDictionaryIndex.SelectStaticDataFileFilter),
                FilterIndex = 1,
                Multiselect = false,
                CheckFileExists = true,
            };

            if (!(bool)openFileDialog.ShowDialog())
            {
                return;
            }

            if (!LoadStaticDataProbufBinaryFileFromPath(openFileDialog.FileName))
            {
                return;
            }

            OnStaticDataLoaded();
        }

        private void OnStaticDataLoaded()
        {
            MainButon_textblock.Text = GetCultureText(TranslationDictionaryIndex.Compile).ToUpper();
            LastSave_textblock.Text = GetCultureText(TranslationDictionaryIndex.LastSaved) + GlobalFileLastTimeEdited.ToString();
            FileOpenned_textblock.Text = GetCultureText(TranslationDictionaryIndex.FileOpenned) + GlobalStaticDataPath;
            ReloadMainListBox();
        }

        private void CompileChangesIfNeeded()
        {
            if (!HasGlobalChangeMade)
            {
                MessageBox.Show("Nothing to compile.", "Error", MessageBoxButton.OK);
                return;
            }

            HasGlobalChangeMade = false;
            SaveStaticDataProtobufBinaryFile();
            LastSave_textblock.Text = GetCultureText(TranslationDictionaryIndex.LastSaved) + GlobalFileLastTimeEdited.ToString();
        }

        private void HandleDeleteAction()
        {
            if (!HasLoadedStaticData || SelectedCreature == null)
            {
                return;
            }

            var typeText = GetCultureText(IsMonsterList ? TranslationDictionaryIndex.Monster : TranslationDictionaryIndex.Boss).ToLower();
            var warnResult = MessageBox.Show(this,
                string.Format(GetCultureText(TranslationDictionaryIndex.DeleteObject), typeText, SelectedCreature.id.ToString(), SelectedCreature.name),
                GetCultureText(TranslationDictionaryIndex.DeleteObjectTitle),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (warnResult == MessageBoxResult.No)
            {
                return;
            }

            HasChangeMade = false;
            HasGlobalChangeMade = true;

            if (IsBossList && DeleteBossByd(SelectedCreature.id))
            {
                ReloadMainListBox();
                ResetListSelection();
                return;
            }

            if (IsMonsterList && DeleteMonsterByRaceId(SelectedCreature.id))
            {
                ReloadMainListBox();
                ResetListSelection();
            }
        }

        private void HandleNewAction()
        {
            if (!HasLoadedStaticData)
            {
                return;
            }

            if (!TryDiscardPendingSelectionChanges())
            {
                return;
            }

            var typeText = GetCultureText(IsMonsterList ? TranslationDictionaryIndex.Monster : TranslationDictionaryIndex.Boss).ToLower();
            var warnResult = MessageBox.Show(this,
                string.Format(GetCultureText(TranslationDictionaryIndex.NewObject), typeText),
                GetCultureText(TranslationDictionaryIndex.NewObjectTitle),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (warnResult == MessageBoxResult.No)
            {
                return;
            }

            if (IsMonsterList)
            {
                CreateBrandNewMonster();
            }
            else if (IsBossList)
            {
                CreateBrandNewBoss();
            }

            ReloadMainListBox();
            if (MainList_listbox.Items.Count > 0)
            {
                MainList_listbox.SelectedIndex = MainList_listbox.Items.Count - 1;
            }

            HasGlobalChangeMade = true;
        }

        private void HandleSaveAction()
        {
            if (SelectedCreature == null || !HasChangeMade)
            {
                return;
            }

            HasChangeMade = false;
            HasGlobalChangeMade = true;

            if (IsMonsterList)
            {
                SaveMonsterChanges();
            }
            else if (IsBossList)
            {
                SaveBossChanges();
            }

            if (SelectedCreature != null)
            {
                SelectedCreature.name = ShowName_textbox.Text;
            }

            MainList_listbox.Items.Refresh();
        }

        private void SaveMonsterChanges()
        {
            Monster monster = GetMonsterByRaceId(SelectedCreature.id);
            if (monster == null)
            {
                return;
            }

            monster.Name = ShowName_textbox.Text;
            EnsureAppearance(monster);

            monster.AppearanceType.Outfittype = ParseUintFromTextbox(ShowLookType_textbox.Text);
            monster.AppearanceType.Outfitaddon = ParseUintFromTextbox(ShowAddon_textbox.Text);
            EnsureColors(monster.AppearanceType);
            monster.AppearanceType.Colors.Lookhead = ParseUintFromTextbox(ShowLookHead_textbox.Text);
            monster.AppearanceType.Colors.Lookbody = ParseUintFromTextbox(ShowLookBody_textbox.Text);
            monster.AppearanceType.Colors.Looklegs = ParseUintFromTextbox(ShowLookLegs_textbox.Text);
            monster.AppearanceType.Colors.Lookfeet = ParseUintFromTextbox(ShowLookFeet_textbox.Text);
            monster.AppearanceType.Itemtype = ParseUintFromTextbox(ShowLookTypeEx_textbox.Text);
        }

        private void SaveBossChanges()
        {
            Boss boss = GetBossById(SelectedCreature.id);
            if (boss == null)
            {
                return;
            }

            boss.Name = ShowName_textbox.Text;
            if (!GlobalBossAppearancesObjects)
            {
                return;
            }

            EnsureAppearance(boss);

            boss.AppearanceType.Outfittype = ParseUintFromTextbox(ShowLookType_textbox.Text);
            boss.AppearanceType.Outfitaddon = ParseUintFromTextbox(ShowAddon_textbox.Text);
            EnsureColors(boss.AppearanceType);
            boss.AppearanceType.Colors.Lookhead = ParseUintFromTextbox(ShowLookHead_textbox.Text);
            boss.AppearanceType.Colors.Lookbody = ParseUintFromTextbox(ShowLookBody_textbox.Text);
            boss.AppearanceType.Colors.Looklegs = ParseUintFromTextbox(ShowLookLegs_textbox.Text);
            boss.AppearanceType.Colors.Lookfeet = ParseUintFromTextbox(ShowLookFeet_textbox.Text);
            boss.AppearanceType.Itemtype = ParseUintFromTextbox(ShowLookTypeEx_textbox.Text);
        }

        private static void EnsureAppearance(Monster monster)
        {
            if (monster.AppearanceType == null)
            {
                monster.AppearanceType = new Appearance_Type();
            }
        }

        private static void EnsureAppearance(Boss boss)
        {
            if (boss.AppearanceType == null)
            {
                boss.AppearanceType = new Appearance_Type();
            }
        }

        private static void EnsureColors(Appearance_Type appearance)
        {
            if (appearance.Colors == null)
            {
                appearance.Colors = new Colors();
            }
        }

        private static uint ParseUintFromTextbox(string text)
        {
            uint.TryParse(text, out var parsed);
            return parsed;
        }
        #endregion

        #region Main button UI events
        private void MainButtonMouseDown_rectangle(object sender, MouseEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle) || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 1;
        }

        private void MainButtonMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle) || e.LeftButton == MouseButtonState.Pressed)
            {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0.75;
            ParseMainButtonClick(rect.Name);
        }

        private void MainButtonMouseLeave_rectangle(object sender, MouseEventArgs e)
        {
            if (sender.GetType() != typeof(Rectangle))
            {
                return;
            }

            var rect = (Rectangle)sender;
            rect.Opacity = 0.75;
        }
        #endregion

        #region Window controls
        private void MainCloseMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            OnClosed(e);
        }

        private void MainResizeMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void MainMinimizeMouseUp_rectangle(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion

        #region Global and internal functions
        private void TextChanged_textblock(object sender, TextChangedEventArgs e)
        {
            if (SelectedCreature == null)
            {
                return;
            }

            HasChangeMade = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (HasGlobalChangeMade)
            {
                var warnResult = MessageBox.Show(this,
                    GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesOnAssets),
                    GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesOnAssetsTitle),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (warnResult == MessageBoxResult.No)
                {
                    return;
                }

                HasGlobalChangeMade = false;
            }

            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void MouseDownToDrag_var(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch
            {
            }
        }

        private void MainListSelectionChange_listbox(object sender, SelectionChangedEventArgs e)
        {
            if (sender.GetType() != typeof(ListBox))
            {
                return;
            }

            if (!TryHandleSelectionChange())
            {
                return;
            }

            var listBox = (ListBox)sender;
            SelectedCreature = listBox.SelectedItem as InternalItemList;
            ReloadShowGrid();
        }

        private bool TryHandleSelectionChange()
        {
            if (SelectedCreature == null || !HasChangeMade)
            {
                HasChangeMade = false;
                return true;
            }

            var cultureIndex = IsMonsterList ? TranslationDictionaryIndex.Monster : TranslationDictionaryIndex.Boss;
            var warnResult = MessageBox.Show(this,
                string.Format(GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChanges), GetCultureText(cultureIndex).ToLower()),
                GetCultureText(TranslationDictionaryIndex.DiscardUnsavedChangesTitle),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (warnResult == MessageBoxResult.No)
            {
                return false;
            }

            HasChangeMade = false;
            return true;
        }

        private void MainURIMouseDown_rectangle(object sender, MouseButtonEventArgs e)
        {
            string name = string.Empty;
            if (sender.GetType() == typeof(Rectangle))
            {
                var rect = (Rectangle)sender;
                name = rect.Name;
            }
            else if (sender.GetType() == typeof(Image))
            {
                var img = (Image)sender;
                name = img.Name;
            }

            switch (name)
            {
                case "MainFacebook_rectangle":
                    System.Diagnostics.Process.Start(InternalFacebookUri);
                    break;
                case "MainGithub_rectangle":
                    System.Diagnostics.Process.Start(InternalGithubUri);
                    break;
                case "MainDiscord_rectangle":
                    System.Diagnostics.Process.Start(InternalDiscordUri);
                    break;
                case "MainForum_Image":
                    System.Diagnostics.Process.Start(InternalForumUri);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Culture (Translation)
        private void CultureMouseDown_image(object sender, MouseButtonEventArgs e)
        {
            if (sender.GetType() != typeof(Image) || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var img = (Image)sender;
            switch (img.Name)
            {
                case "PTBR_image":
                    if (GlobalTranslationType == TranslationCulture_t.Portuguese)
                    {
                        return;
                    }

                    GlobalTranslationType = TranslationCulture_t.Portuguese;
                    InitializeCultureTexts();
                    break;
                case "ENUS_image":
                    if (GlobalTranslationType == TranslationCulture_t.English)
                    {
                        return;
                    }

                    GlobalTranslationType = TranslationCulture_t.English;
                    InitializeCultureTexts();
                    break;
                default:
                    break;
            }

            if (img.Parent == null || img.Parent.GetType() != typeof(StackPanel))
            {
                return;
            }

            var stackPanel = (StackPanel)img.Parent;
            foreach (var child in stackPanel.Children)
            {
                if (child == null || child.GetType() != typeof(Image))
                {
                    continue;
                }

                var childImg = (Image)child;
                if (childImg.Name != img.Name)
                {
                    childImg.Opacity = 0.5;
                }
                else
                {
                    childImg.Opacity = 1;
                }
            }
        }

        private void InitializeCultureTexts()
        {
            if (HasLoadedStaticData)
            {
                MainButon_textblock.Text = GetCultureText(TranslationDictionaryIndex.Compile).ToUpper();
            }
            else
            {
                MainButon_textblock.Text = GetCultureText(TranslationDictionaryIndex.Open).ToUpper();
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

            UpdateListHeaderText();
        }

        private void UpdateListHeaderText()
        {
            if (IsMonsterList)
            {
                MainList_textblock.Text = GetCultureText(TranslationDictionaryIndex.Monsters);
                BossAppearance_textblock.Text = string.Empty;
                BossAppearance_textblock.Opacity = 0;
                return;
            }

            MainList_textblock.Text = GetCultureText(TranslationDictionaryIndex.Bosses);
            if (!GlobalBossAppearancesObjects)
            {
                BossAppearance_textblock.Text = GetCultureText(TranslationDictionaryIndex.BossAppearanceDisabled);
                BossAppearance_textblock.Opacity = 1;
            }
            else
            {
                BossAppearance_textblock.Text = string.Empty;
                BossAppearance_textblock.Opacity = 0;
            }
        }
        #endregion

        #region Load and initializers
        public void ReloadMainListBox()
        {
            if (!HasLoadedStaticData)
            {
                return;
            }

            MainList_listbox.BeginInit();
            MainList_listbox.Items.Clear();

            if (IsMonsterList)
            {
                foreach (var monster in GlobalStaticData.Monster)
                {
                    MainList_listbox.Items.Add(new InternalItemList(monster.Name, monster.Raceid));
                }
            }
            else if (IsBossList)
            {
                foreach (var boss in GlobalStaticData.Boss)
                {
                    MainList_listbox.Items.Add(new InternalItemList(boss.Name, boss.Id));
                }
            }

            MainList_listbox.EndInit();
        }

        private void ReloadShowGrid()
        {
            if (SelectedCreature == null)
            {
                return;
            }

            if (IsBossList)
            {
                LoadBossIntoGrid();
            }
            else
            {
                LoadMonsterIntoGrid();
            }

            HasChangeMade = false;
        }

        private void LoadBossIntoGrid()
        {
            Boss boss = GetBossById(SelectedCreature.id);
            if (boss == null)
            {
                return;
            }

            ShowRaceId_textblock.Text = "ID: " + boss.Id.ToString();
            SelectedCreature.name = boss.Name;
            ShowName_textbox.Text = boss.Name;

            if (!GlobalBossAppearancesObjects)
            {
                DisableBossAppearanceControls();
                return;
            }

            EnableAppearanceControls();
            PopulateAppearanceFields(boss.AppearanceType);
        }

        private void LoadMonsterIntoGrid()
        {
            Monster monster = GetMonsterByRaceId(SelectedCreature.id);
            if (monster == null)
            {
                return;
            }

            ShowRaceId_textblock.Text = "ID: " + monster.Raceid.ToString();
            SelectedCreature.name = monster.Name;
            ShowName_textbox.Text = monster.Name;

            EnableAppearanceControls();
            PopulateAppearanceFields(monster.AppearanceType);
        }

        private void PopulateAppearanceFields(Appearance_Type appearance)
        {
            if (appearance == null)
            {
                ClearAppearanceFields();
                return;
            }

            ShowLookType_textbox.Text = appearance.Outfittype.ToString();
            ShowAddon_textbox.Text = appearance.Outfitaddon.ToString();
            ShowLookTypeEx_textbox.Text = appearance.Outfittype == 0 ? appearance.Itemtype.ToString() : string.Empty;

            if (appearance.Colors == null)
            {
                ClearColorFields();
                return;
            }

            ShowLookHead_textbox.Text = appearance.Colors.Lookhead.ToString();
            ShowLookBody_textbox.Text = appearance.Colors.Lookbody.ToString();
            ShowLookLegs_textbox.Text = appearance.Colors.Looklegs.ToString();
            ShowLookFeet_textbox.Text = appearance.Colors.Lookfeet.ToString();
        }

        private void ClearAppearanceFields()
        {
            ShowLookType_textbox.Text = string.Empty;
            ShowLookTypeEx_textbox.Text = string.Empty;
            ShowAddon_textbox.Text = string.Empty;
            ClearColorFields();
        }

        private void ClearColorFields()
        {
            ShowLookHead_textbox.Text = string.Empty;
            ShowLookBody_textbox.Text = string.Empty;
            ShowLookLegs_textbox.Text = string.Empty;
            ShowLookFeet_textbox.Text = string.Empty;
        }

        private void DisableBossAppearanceControls()
        {
            ShowLookType_textbox.IsEnabled = false;
            ShowLookTypeEx_textbox.IsEnabled = false;
            ShowAddon_textbox.IsEnabled = false;
            ShowLookHead_textbox.IsEnabled = false;
            ShowLookBody_textbox.IsEnabled = false;
            ShowLookLegs_textbox.IsEnabled = false;
            ShowLookFeet_textbox.IsEnabled = false;

            ClearAppearanceFields();
            BossAppearance_textblock.Text = GetCultureText(TranslationDictionaryIndex.BossAppearanceDisabled);
            BossAppearance_textblock.Opacity = 1;
        }

        private void EnableAppearanceControls()
        {
            ShowLookType_textbox.IsEnabled = true;
            ShowLookTypeEx_textbox.IsEnabled = true;
            ShowAddon_textbox.IsEnabled = true;
            ShowLookHead_textbox.IsEnabled = true;
            ShowLookBody_textbox.IsEnabled = true;
            ShowLookLegs_textbox.IsEnabled = true;
            ShowLookFeet_textbox.IsEnabled = true;
            BossAppearance_textblock.Text = string.Empty;
            BossAppearance_textblock.Opacity = 0;
        }
        #endregion
    }
}

