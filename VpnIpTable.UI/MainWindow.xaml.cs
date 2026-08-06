using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VpnIpTable.Core.Models;
using VpnIpTable.Core.Services;

namespace VpnIpTable.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly CidrService _cidrService;
    private readonly YamlStorageService _storageService;
    private List<IpRange> _ranges;
    private const string DefaultFileName = "addresses.yaml";

    public MainWindow()
    {
        InitializeComponent();
        ApplyLanguage("en");
        _cidrService = new CidrService();
        _storageService = new YamlStorageService();
        _ranges = new List<IpRange>();
        labelResult.Content = "";
        LanguageComboBox.SelectedIndex = 0;
        LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;
        LoadData();
        UpdateListBox();
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem &&
            selectedItem.Tag is string language)
        {
            ApplyLanguage(language);
            labelResult.Content = "";
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        labelResult.Content = "";
        var inputText = InputTextBox.Text;
        if (string.IsNullOrWhiteSpace(inputText))
        {
            labelResult.Content = Text("ErrorEmptyInput");
            return;
        }

        var cidrStrings = _cidrService.ExtractAddressesFromText(inputText);

        if (!cidrStrings.Any())
        {
            labelResult.Content = Text("ErrorNoValidCidrs");
            return;
        }

        try
        {
            _ranges = _cidrService.AddRanges(_ranges, cidrStrings, out var addedCount, out var removedCount);
            UpdateListBox();
            InputTextBox.Clear();
            SaveData();
            labelResult.Content = Text("AddSuccess", cidrStrings.Count, addedCount, removedCount);
        }
        catch (Exception ex)
        {
            labelResult.Content = Text("ErrorAddingAddresses", ex.Message);
        }
    }

    private void LoadFromFileButton_Click(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Title = Text("DialogTitleLoadTextFromFile"),
            Filter = Text("FilterSupportedFiles")
        };

        if (openDialog.ShowDialog() == true)
        {
            try
            {
                var fileContent = File.ReadAllText(openDialog.FileName);
                var addresses = GetAddressesFromFileContent(fileContent, Path.GetExtension(openDialog.FileName));
                InputTextBox.Text = addresses.Any()
                    ? string.Join(Environment.NewLine, addresses)
                    : fileContent;
            }
            catch (Exception ex)
            {
                ShowMessage(Text("ErrorLoadingFile", ex.Message), "DialogTitleError", MessageBoxImage.Error);
            }
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (AddressListBox.SelectedItem is string selectedCidr)
        {
            try
            {
                var rangeToRemove = _cidrService.ParseCidr(selectedCidr);
                _ranges.RemoveAll(r => r.ToString() == rangeToRemove.ToString());
                UpdateListBox();
                SaveData();
            }
            catch (Exception ex)
            {
                ShowMessage(Text("ErrorRemovingAddress", ex.Message), "DialogTitleError", MessageBoxImage.Error);
            }
        }
        else
        {
            ShowMessage(Text("SelectAddressToRemove"), "DialogTitleWarning", MessageBoxImage.Warning);
        }
    }

    private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_ranges.Any())
        {
            ShowMessage(Text("AddressListEmpty"), "DialogTitleWarning", MessageBoxImage.Warning);
            return;
        }

        var csv = _cidrService.ExportToCsv(_ranges);
        ShowExportDialog(csv, "DialogTitleExportCsv", "FilterCsvFiles", "addresses.csv");
    }

    private void ExportRouteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_ranges.Any())
        {
            ShowMessage(Text("AddressListEmpty"), "DialogTitleWarning", MessageBoxImage.Warning);
            return;
        }

        var routeCommands = _cidrService.ExportToRouteCommands(_ranges);
        ShowExportDialog(routeCommands, "DialogTitleExportRoute", "FilterBatFiles", "route-commands.bat");
    }

    private void ExportJsonButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_ranges.Any())
        {
            ShowMessage(Text("AddressListEmpty"), "DialogTitleWarning", MessageBoxImage.Warning);
            return;
        }

        var json = _cidrService.ExportToAmneziaJson(_ranges);
        ShowExportDialog(json, "DialogTitleExportJson", "FilterJsonFiles", "amnezia-sites.json");
    }

    private void SaveData()
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultFileName);
            _storageService.SaveToFile(_ranges, filePath);
        }
        catch (Exception ex)
        {
            ShowMessage(Text("ErrorAutoSave", ex.Message), "DialogTitleError", MessageBoxImage.Error);
        }
    }

    private void LoadData()
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultFileName);
            if (File.Exists(filePath))
            {
                _ranges = _storageService.LoadFromFile(filePath, _cidrService);
            }
        }
        catch (Exception ex)
        {
            ShowMessage(Text("ErrorLoadingData", ex.Message), "DialogTitleError", MessageBoxImage.Warning);
        }
    }

    private void UpdateListBox()
    {
        var list = _ranges.Select(r => r.ToString()).OrderBy(s => s).ToList();
        AddressListBox.ItemsSource = list;
        CountLabel.Content = list.Count;
    }

    private void ShowExportDialog(string content, string titleKey, string filterKey, string fileName)
    {
        var saveDialog = new SaveFileDialog
        {
            Title = Text(titleKey),
            Filter = Text(filterKey),
            FileName = fileName
        };

        if (saveDialog.ShowDialog() == true)
        {
            File.WriteAllText(saveDialog.FileName, content);
            ShowMessage(Text("FileSaved", saveDialog.FileName), "DialogTitleSuccess", MessageBoxImage.Information);
        }
    }

    private static void ApplyLanguage(string language)
    {
        var culture = CultureInfo.GetCultureInfo(language == "ru" ? "ru-RU" : "en-US");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var currentDictionary = dictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.StartsWith("Resources/Strings", StringComparison.OrdinalIgnoreCase) == true);

        if (currentDictionary is not null)
        {
            dictionaries.Remove(currentDictionary);
        }

        var suffix = language == "ru" ? ".ru" : "";
        dictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"Resources/Strings{suffix}.xaml", UriKind.Relative)
        });
    }

    private static string Text(string key, params object[] args)
    {
        var template = Application.Current.TryFindResource(key) as string ?? key;
        return args.Length == 0
            ? template
            : string.Format(CultureInfo.CurrentCulture, template, args);
    }

    private static void ShowMessage(string message, string titleKey, MessageBoxImage image)
    {
        MessageBox.Show(message, Text(titleKey), MessageBoxButton.OK, image);
    }

    private List<string> GetAddressesFromFileContent(string fileContent, string extension)
    {
        if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
        {
            return _cidrService.ExtractAddressesFromAmneziaJson(fileContent);
        }

        return _cidrService.ExtractAddressesFromText(fileContent);
    }
}
