using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using VpnIpTable.Core.Models;

namespace VpnIpTable.Core.Services;

/// <summary>
/// Сервис для сохранения и загрузки IP-диапазонов в YAML файл
/// </summary>
public class YamlStorageService
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public YamlStorageService()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
            
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Сохраняет список IP-диапазонов в YAML файл
    /// </summary>
    public void SaveToFile(List<IpRange> ranges, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        // Преобразуем диапазоны в список строк
        var addresses = ranges.Select(r => r.ToString()).ToList();
        
        var data = new { addresses = addresses };
        
        var yaml = _serializer.Serialize(data);
        File.WriteAllText(filePath, yaml);
    }

    /// <summary>
    /// Загружает список IP-диапазонов из YAML файла
    /// </summary>
    public List<IpRange> LoadFromFile(string filePath, CidrService cidrService)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            return new List<IpRange>();

        var yaml = File.ReadAllText(filePath);
        
        if (string.IsNullOrWhiteSpace(yaml))
            return new List<IpRange>();

        try
        {
            var data = _deserializer.Deserialize<Dictionary<string, List<string>>>(yaml);
            
            if (data == null || !data.ContainsKey("addresses"))
                return new List<IpRange>();

            var addresses = data["addresses"];
            var ranges = new List<IpRange>();

            foreach (var address in addresses)
            {
                try
                {
                    var range = cidrService.ParseCidr(address);
                    ranges.Add(range);
                }
                catch
                {
                    // Пропускаем невалидные записи
                }
            }

            return ranges;
        }
        catch
        {
            // Если не удалось распарсить YAML, возвращаем пустой список
            return new List<IpRange>();
        }
    }
}
