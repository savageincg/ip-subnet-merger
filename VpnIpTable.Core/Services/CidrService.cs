using System.Net;
using System.Text.RegularExpressions;
using VpnIpTable.Core.Models;

namespace VpnIpTable.Core.Services;

/// <summary>
/// Сервис для работы с CIDR-нотацией и IP-адресами
/// </summary>
public class CidrService
{
    /// <summary>
    /// Парсит строку в формате CIDR (например, "192.168.1.0/24") или IP-адрес (например, "192.168.0.1") в объект IpRange
    /// IP-адреса без маски интерпретируются как /32 (один адрес)
    /// </summary>
    public IpRange ParseCidr(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            throw new ArgumentException("CIDR string cannot be null or empty", nameof(cidr));

        var trimmed = cidr.Trim();
        var parts = trimmed.Split('/');
        
        if (parts.Length == 1)
        {
            // IP-адрес без маски - интерпретируем как /32
            if (!IPAddress.TryParse(trimmed, out var address))
                throw new FormatException($"Invalid IP address: {trimmed}");
            
            return new IpRange(address, 32);
        }
        
        if (parts.Length != 2)
            throw new FormatException($"Invalid CIDR format: {cidr}. Expected format: x.x.x.x/prefix or x.x.x.x");

        if (!IPAddress.TryParse(parts[0], out var addressWithMask))
            throw new FormatException($"Invalid IP address: {parts[0]}");

        if (!int.TryParse(parts[1], out var prefixLength) || prefixLength < 0 || prefixLength > 32)
            throw new FormatException($"Invalid prefix length: {parts[1]}. Must be between 0 and 32");

        return new IpRange(addressWithMask, prefixLength);
    }

    /// <summary>
    /// Проверяет, входит ли один диапазон в другой
    /// </summary>
    public bool IsSubsetOf(IpRange subset, IpRange superset)
    {
        var subsetNetwork = subset.GetNetworkAddress().GetAddressBytes();
        var supersetNetwork = superset.GetNetworkAddress().GetAddressBytes();
        var supersetMask = superset.GetSubnetMask().GetAddressBytes();

        // Если префикс subset больше, то он не может быть подмножеством
        if (subset.PrefixLength < superset.PrefixLength)
            return false;

        // Проверяем, что сетевой адрес subset попадает в сеть superset
        for (int i = 0; i < 4; i++)
        {
            if ((subsetNetwork[i] & supersetMask[i]) != supersetNetwork[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Умное объединение: добавляет новый диапазон и удаляет все подмножества
    /// </summary>
    public List<IpRange> MergeRanges(List<IpRange> existingRanges, IpRange newRange)
    {
        var result = new List<IpRange>(existingRanges);

        // Удаляем все диапазоны, которые являются подмножествами нового диапазона
        result.RemoveAll(r => IsSubsetOf(r, newRange));

        // Добавляем новый диапазон только если он не является подмножеством существующих
        bool isSubset = result.Any(r => IsSubsetOf(newRange, r));
        if (!isSubset)
        {
            result.Add(newRange);
        }

        return result;
    }

    /// <summary>
    /// Добавляет один или несколько CIDR-адресов в список с умным объединением
    /// </summary>
    public List<IpRange> AddRanges(List<IpRange> existingRanges, IEnumerable<string> cidrStrings)
    {
        var currentRanges = new List<IpRange>(existingRanges);

        foreach (var cidrString in cidrStrings)
        {
            var range = ParseCidr(cidrString);
            currentRanges = MergeRanges(currentRanges, range);
        }

        return currentRanges;
    }

    /// <summary>
    /// Экспортирует список диапазонов в CSV-подобный формат (одна строка через запятую)
    /// </summary>
    public string ExportToCsv(List<IpRange> ranges)
    {
        return string.Join(",", ranges.Select(r => r.ToString()));
    }

    /// <summary>
    /// Экспортирует список диапазонов в формат Route команд
    /// </summary>
    public string ExportToRouteCommands(List<IpRange> ranges)
    {
        var commands = ranges.Select(r =>
        {
            var address = r.GetNetworkAddress();
            var mask = r.GetSubnetMask();
            return $"route ADD {address} MASK {mask} 0.0.0.0";
        });

        return string.Join(Environment.NewLine, commands);
    }

    /// <summary>
    /// Валидирует строку CIDR
    /// </summary>
    public bool IsValidCidr(string cidr)
    {
        try
        {
            ParseCidr(cidr);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Извлекает IP-адреса и CIDR из текста с использованием регулярного выражения
    /// </summary>
    public List<string> ExtractAddressesFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // Regex для поиска IP-адресов (x.x.x.x) и CIDR-нотации (x.x.x.x/prefix)
        // Поддерживает как полные IP-адреса, так и CIDR-нотацию
        var pattern = @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(?:/\d{1,2})?\b";
        
        var matches = Regex.Matches(text, pattern);
        var addresses = new List<string>();

        foreach (Match match in matches)
        {
            var address = match.Value;
            // Проверяем валидность адреса
            if (IsValidCidr(address))
            {
                addresses.Add(address);
            }
        }

        return addresses.Distinct().ToList();
    }
}
