using Xunit;
using VpnIpTable.Core.Services;
using System.Net;
using VpnIpTable.Core.Models;

namespace VpnIpTable.Core.Tests.Services;

public class CidrServiceTests
{
    private readonly CidrService _service;

    public CidrServiceTests()
    {
        _service = new CidrService();
    }

    [Theory]
    [InlineData("192.168.1.0/24")]
    [InlineData("10.0.0.0/8")]
    [InlineData("172.16.0.0/16")]
    [InlineData("192.168.1.1/32")]
    public void ParseCidr_ValidInput_ReturnsIpRange(string cidr)
    {
        var result = _service.ParseCidr(cidr);

        Assert.NotNull(result);
        Assert.NotNull(result.Address);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("192.168.1.0/")]
    [InlineData("/24")]
    public void ParseCidr_InvalidInput_ThrowsException(string cidr)
    {
        Assert.ThrowsAny<Exception>(() => _service.ParseCidr(cidr));
    }

    [Theory]
    [InlineData("192.168.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    public void ParseCidr_IpAddressWithoutMask_ReturnsIpRangeWithPrefix32(string ipAddress)
    {
        var result = _service.ParseCidr(ipAddress);

        Assert.NotNull(result);
        Assert.NotNull(result.Address);
        Assert.Equal(32, result.PrefixLength);
    }

    [Fact]
    public void IsSubsetOf_SmallerNetworkIsSubset_ReturnsTrue()
    {
        var superset = _service.ParseCidr("192.168.0.0/16");
        var subset = _service.ParseCidr("192.168.1.0/24");

        var result = _service.IsSubsetOf(subset, superset);

        Assert.True(result);
    }

    [Fact]
    public void IsSubsetOf_LargerNetworkIsNotSubset_ReturnsFalse()
    {
        var superset = _service.ParseCidr("192.168.1.0/24");
        var subset = _service.ParseCidr("192.168.0.0/16");

        var result = _service.IsSubsetOf(subset, superset);

        Assert.False(result);
    }

    [Fact]
    public void MergeRanges_AddsNewRange_RemovesSubsets()
    {
        var existing = new List<IpRange>
        {
            _service.ParseCidr("192.168.1.0/24"),
            _service.ParseCidr("192.168.2.0/24")
        };

        var newRange = _service.ParseCidr("192.168.0.0/16");

        var result = _service.MergeRanges(existing, newRange);

        // Новый диапазон должен быть добавлен, старые должны быть удалены
        Assert.Contains(result, r => r.ToString() == "192.168.0.0/16");
        Assert.DoesNotContain(result, r => r.ToString() == "192.168.1.0/24");
        Assert.DoesNotContain(result, r => r.ToString() == "192.168.2.0/24");
    }

    [Fact]
    public void ExportToCsv_MultipleRanges_ReturnsCommaSeparatedString()
    {
        var ranges = new List<IpRange>
        {
            _service.ParseCidr("192.168.1.0/24"),
            _service.ParseCidr("10.0.0.0/8")
        };

        var result = _service.ExportToCsv(ranges);

        Assert.Contains("192.168.1.0/24", result);
        Assert.Contains("10.0.0.0/8", result);
        Assert.Contains(",", result);
    }

    [Fact]
    public void ExportToRouteCommands_MultipleRanges_ReturnsRouteCommands()
    {
        var ranges = new List<IpRange>
        {
            _service.ParseCidr("192.168.1.0/24")
        };

        var result = _service.ExportToRouteCommands(ranges);

        Assert.Contains("route ADD", result);
        Assert.Contains("MASK", result);
        Assert.Contains("0.0.0.0", result);
    }

    [Theory]
    [InlineData("192.168.1.0/24", true)]
    [InlineData("192.168.0.1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void IsValidCidr_ValidatesInput_ReturnsCorrectResult(string cidr, bool expected)
    {
        var result = _service.IsValidCidr(cidr);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractAddressesFromText_ValidAddresses_ExtractsCorrectly()
    {
        var text = "Here are some addresses: 192.168.1.0/24, 10.0.0.1, and 172.16.0.0/16";
        var result = _service.ExtractAddressesFromText(text);

        Assert.Equal(3, result.Count);
        Assert.Contains("192.168.1.0/24", result);
        Assert.Contains("10.0.0.1", result);
        Assert.Contains("172.16.0.0/16", result);
    }

    [Fact]
    public void ExtractAddressesFromText_TextWithInvalidAddresses_ExtractsOnlyValid()
    {
        var text = "Valid: 192.168.1.0/24, Invalid: 999.999.999.999, Valid: 10.0.0.1";
        var result = _service.ExtractAddressesFromText(text);

        Assert.Equal(2, result.Count);
        Assert.Contains("192.168.1.0/24", result);
        Assert.Contains("10.0.0.1", result);
    }

    [Fact]
    public void ExtractAddressesFromText_EmptyText_ReturnsEmptyList()
    {
        var result = _service.ExtractAddressesFromText("");

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractAddressesFromText_NoAddresses_ReturnsEmptyList()
    {
        var result = _service.ExtractAddressesFromText("This is just text without any IP addresses.");

        Assert.Empty(result);
    }
}
