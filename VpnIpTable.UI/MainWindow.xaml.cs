using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
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
        _cidrService = new CidrService();
        _storageService = new YamlStorageService();
        _ranges = new List<IpRange>();
        LoadData();
        UpdateListBox();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var inputText = InputTextBox.Text;
        if (string.IsNullOrWhiteSpace(inputText))
        {
            MessageBox.Show("Введите текст с CIDR адресами для добавления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Извлекаем адреса из текста с помощью regex
        var cidrStrings = _cidrService.ExtractAddressesFromText(inputText);

        if (!cidrStrings.Any())
        {
            MessageBox.Show("В тексте не найдено валидных CIDR адресов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _ranges = _cidrService.AddRanges(_ranges, cidrStrings);
            UpdateListBox();
            InputTextBox.Clear();
            SaveData();
            MessageBox.Show($"Добавлено {cidrStrings.Count} адрес(ов).", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при добавлении адресов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadFromFileButton_Click(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Title = "Загрузить текст из файла",
            Filter = "Supported Files|*.txt;*.json;*.csv|Text Files|*.txt|JSON Files|*.json|All Files|*.*"
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
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("Выберите адрес для удаления.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_ranges.Any())
        {
            MessageBox.Show("Список адресов пуст.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var csv = _cidrService.ExportToCsv(_ranges);
        ShowExportDialog(csv, "Экспорт CSV", "CSV Files|*.csv|All Files|*.*");
    }

    private void ExportRouteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_ranges.Any())
        {
            MessageBox.Show("Список адресов пуст.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var routeCommands = _cidrService.ExportToRouteCommands(_ranges);
        ShowExportDialog(routeCommands, "Экспорт Route команд", "Bat Files|*.bat|All Files|*.*");
    }

    private void ExportJsonButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_ranges.Any())
        {
            MessageBox.Show("Список адресов пуст.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var json = _cidrService.ExportToAmneziaJson(_ranges);
        ShowExportDialog(json, "Экспорт JSON", "JSON Files|*.json|All Files|*.*");
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
            MessageBox.Show($"Ошибка при автоматическом сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateListBox()
    {
        AddressListBox.ItemsSource = _ranges.Select(r => r.ToString()).OrderBy(s => s).ToList();
    }

    private void ShowExportDialog(string content, string title, string filter)
    {
        var saveDialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = GetExportFileName(title)
        };

        if (saveDialog.ShowDialog() == true)
        {
            File.WriteAllText(saveDialog.FileName, content);
            MessageBox.Show($"Файл сохранен: {saveDialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static string GetExportFileName(string title)
    {
        if (title.Contains("CSV"))
            return "addresses.csv";

        if (title.Contains("JSON"))
            return "amnezia-sites.json";

        return "route-commands.bat";
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
