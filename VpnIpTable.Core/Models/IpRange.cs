using System.Net;

namespace VpnIpTable.Core.Models;

/// <summary>
/// Представляет IP-адрес или подсеть в CIDR-нотации
/// </summary>
public class IpRange
{
    public IPAddress Address { get; set; }
    public int PrefixLength { get; set; }

    public IpRange(IPAddress address, int prefixLength)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        PrefixLength = prefixLength;
        
        if (prefixLength < 0 || prefixLength > 32)
            throw new ArgumentException("Prefix length must be between 0 and 32", nameof(prefixLength));
    }

    /// <summary>
    /// Возвращает строковое представление в формате CIDR (например, "192.168.1.0/24")
    /// </summary>
    public override string ToString()
    {
        return $"{Address}/{PrefixLength}";
    }

    /// <summary>
    /// Вычисляет маску подсети на основе префикса
    /// </summary>
    public IPAddress GetSubnetMask()
    {
        uint mask = 0xFFFFFFFF;
        mask <<= (32 - PrefixLength);
        
        // Преобразуем uint в байты (big-endian для IPv4)
        var bytes = new byte[4];
        bytes[0] = (byte)((mask >> 24) & 0xFF);
        bytes[1] = (byte)((mask >> 16) & 0xFF);
        bytes[2] = (byte)((mask >> 8) & 0xFF);
        bytes[3] = (byte)(mask & 0xFF);
        
        return new IPAddress(bytes);
    }

    /// <summary>
    /// Вычисляет сетевой адрес (базовый адрес сети)
    /// </summary>
    public IPAddress GetNetworkAddress()
    {
        var addressBytes = Address.GetAddressBytes();
        var maskBytes = GetSubnetMask().GetAddressBytes();
        
        var networkBytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            networkBytes[i] = (byte)(addressBytes[i] & maskBytes[i]);
        }
        
        return new IPAddress(networkBytes);
    }
}
