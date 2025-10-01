using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tibia.Protobuf.Staticdata;

namespace Canary_monster_editor
{
    public partial class ExportImportWindow : Window
    {
        public class CreatureExportData
        {
            public string Type { get; set; }
            public uint Id { get; set; }
            public string Name { get; set; }
            public uint LookType { get; set; }
            public uint LookTypeEx { get; set; }
            public uint Addon { get; set; }
            public uint LookHead { get; set; }
            public uint LookBody { get; set; }
            public uint LookLegs { get; set; }
            public uint LookFeet { get; set; }
        }

        public class CreatureSelectionItem
        {
            public string DisplayText { get; set; }
            public CreatureExportData Data { get; set; }
            public bool IsSelected { get; set; }
        }

        private int lastIndex = -1;

        public ExportImportWindow()
        {
            InitializeComponent();
            LoadCreatures(ListType.Monsters);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TypeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (MonsterRadio.IsChecked == true)
            {
                LoadCreatures(ListType.Monsters);
            }
            else if (BossRadio.IsChecked == true)
            {
                LoadCreatures(ListType.Bosses);
            }
        }

        private void LoadCreatures(ListType type)
        {
            if (Data.GlobalStaticData == null)
            {
                return;
            }

            CreatureListBox.Items.Clear();
            foreach (var item in EnumerateCreatures(type))
            {
                CreatureListBox.Items.Add(item);
            }
        }

        private IEnumerable<CreatureSelectionItem> EnumerateCreatures(ListType type)
        {
            if (type == ListType.Monsters)
            {
                foreach (var monster in Data.GlobalStaticData.Monster)
                {
                    yield return CreateSelectionItem(monster.Name, monster.Raceid, monster.AppearanceType, "monster");
                }

                yield break;
            }

            foreach (var boss in Data.GlobalStaticData.Boss)
            {
                yield return CreateSelectionItem(boss.Name, boss.Id, boss.AppearanceType, "boss");
            }
        }

        private static CreatureSelectionItem CreateSelectionItem(string name, uint id, Appearance_Type appearance, string type)
        {
            return new CreatureSelectionItem
            {
                DisplayText = $"{name} (ID: {id})",
                Data = new CreatureExportData
                {
                    Type = type,
                    Id = id,
                    Name = name,
                    LookType = appearance?.Outfittype ?? 0,
                    LookTypeEx = appearance?.Itemtype ?? 0,
                    Addon = appearance?.Outfitaddon ?? 0,
                    LookHead = appearance?.Colors?.Lookhead ?? 0,
                    LookBody = appearance?.Colors?.Lookbody ?? 0,
                    LookLegs = appearance?.Colors?.Looklegs ?? 0,
                    LookFeet = appearance?.Colors?.Lookfeet ?? 0,
                },
                IsSelected = false,
            };
        }

        private void CreatureListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var listBox = (ListBox)sender;
            var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item == null)
            {
                return;
            }

            int currentIndex = listBox.Items.IndexOf(item.DataContext);
            if (currentIndex < 0)
            {
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Shift && lastIndex != -1 && currentIndex != lastIndex)
            {
                SelectRange(listBox, lastIndex, currentIndex, true);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                SelectAllItems(listBox, false);
                SetItemSelection(listBox, currentIndex, true);
                listBox.Items.Refresh();
            }
            else
            {
                ToggleItemSelection(listBox, currentIndex);
            }

            lastIndex = currentIndex;
        }

        private static void SelectRange(ListBox listBox, int startIndex, int endIndex, bool isSelected)
        {
            int start = Math.Min(startIndex, endIndex);
            int end = Math.Max(startIndex, endIndex);
            for (int i = start; i <= end; i++)
            {
                SetItemSelection(listBox, i, isSelected);
            }

            listBox.Items.Refresh();
        }

        private static void SelectAllItems(ListBox listBox, bool isSelected)
        {
            foreach (var listItem in listBox.Items)
            {
                if (listItem is CreatureSelectionItem creature)
                {
                    creature.IsSelected = isSelected;
                }
            }

            listBox.Items.Refresh();
        }

        private static void SetItemSelection(ListBox listBox, int index, bool isSelected)
        {
            if (index < 0 || index >= listBox.Items.Count)
            {
                return;
            }

            if (listBox.Items[index] is CreatureSelectionItem creature)
            {
                creature.IsSelected = isSelected;
            }
        }

        private static void ToggleItemSelection(ListBox listBox, int index)
        {
            if (index < 0 || index >= listBox.Items.Count)
            {
                return;
            }

            if (listBox.Items[index] is CreatureSelectionItem creature)
            {
                creature.IsSelected = !creature.IsSelected;
            }

            listBox.Items.Refresh();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            SelectAllItems(CreatureListBox, true);
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            SelectAllItems(CreatureListBox, false);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = CreatureListBox.Items
                .Cast<CreatureSelectionItem>()
                .Where(item => item.IsSelected)
                .Select(item => item.Data)
                .ToList();

            if (!selectedItems.Any())
            {
                MessageBox.Show("Select at least one creature to export.");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Save JSON File",
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(selectedItems, Formatting.Indented);
                    File.WriteAllText(saveFileDialog.FileName, json);
                    MessageBox.Show("Creatures exported successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export creatures: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Import JSON File",
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    var importedCreatures = JsonConvert.DeserializeObject<List<CreatureExportData>>(json);

                    if (importedCreatures == null || importedCreatures.Count == 0)
                    {
                        MessageBox.Show("No creatures were imported.");
                        return;
                    }

                    foreach (var creature in importedCreatures)
                    {
                        if (creature.Type == "monster")
                        {
                            var monster = Data.GetMonsterByRaceId(creature.Id);
                            if (monster != null)
                            {
                                UpdateAppearance(monster, creature);
                            }
                            else
                            {
                                CreateMonsterFromImport(creature);
                            }
                        }
                        else if (creature.Type == "boss")
                        {
                            var boss = Data.GetBossById(creature.Id);
                            if (boss != null)
                            {
                                UpdateAppearance(boss, creature);
                            }
                            else
                            {
                                CreateBossFromImport(creature);
                            }
                        }
                    }

                    MessageBox.Show("Creatures imported successfully!");
                    LoadCreatures(MonsterRadio.IsChecked == true ? ListType.Monsters : ListType.Bosses);

                    if (Application.Current.MainWindow is MainWindow mw)
                    {
                        mw.HasGlobalChangeMade = true;
                        mw.ReloadMainListBox();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import creatures: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static void UpdateAppearance(Monster monster, CreatureExportData creature)
        {
            monster.Name = creature.Name;
            if (monster.AppearanceType == null)
            {
                monster.AppearanceType = new Appearance_Type();
            }

            monster.AppearanceType.Outfittype = creature.LookType;
            monster.AppearanceType.Itemtype = creature.LookTypeEx;
            monster.AppearanceType.Outfitaddon = creature.Addon;
            EnsureColors(monster.AppearanceType);
            monster.AppearanceType.Colors.Lookhead = creature.LookHead;
            monster.AppearanceType.Colors.Lookbody = creature.LookBody;
            monster.AppearanceType.Colors.Looklegs = creature.LookLegs;
            monster.AppearanceType.Colors.Lookfeet = creature.LookFeet;
        }

        private static void UpdateAppearance(Boss boss, CreatureExportData creature)
        {
            boss.Name = creature.Name;
            if (!Data.GlobalBossAppearancesObjects)
            {
                return;
            }

            if (boss.AppearanceType == null)
            {
                boss.AppearanceType = new Appearance_Type();
            }

            boss.AppearanceType.Outfittype = creature.LookType;
            boss.AppearanceType.Itemtype = creature.LookTypeEx;
            boss.AppearanceType.Outfitaddon = creature.Addon;
            EnsureColors(boss.AppearanceType);
            boss.AppearanceType.Colors.Lookhead = creature.LookHead;
            boss.AppearanceType.Colors.Lookbody = creature.LookBody;
            boss.AppearanceType.Colors.Looklegs = creature.LookLegs;
            boss.AppearanceType.Colors.Lookfeet = creature.LookFeet;
        }

        private static void CreateMonsterFromImport(CreatureExportData creature)
        {
            var monster = new Monster
            {
                Raceid = creature.Id,
                Name = creature.Name,
                AppearanceType = new Appearance_Type(),
            };

            UpdateAppearance(monster, creature);
            Data.GlobalStaticData.Monster.Add(monster);
        }

        private static void CreateBossFromImport(CreatureExportData creature)
        {
            var boss = new Boss
            {
                Id = creature.Id,
                Name = creature.Name,
                AppearanceType = Data.GlobalBossAppearancesObjects ? new Appearance_Type() : null,
            };

            UpdateAppearance(boss, creature);
            Data.GlobalStaticData.Boss.Add(boss);
        }

        private static void EnsureColors(Appearance_Type appearance)
        {
            if (appearance.Colors == null)
            {
                appearance.Colors = new Colors();
            }
        }
    }
}

