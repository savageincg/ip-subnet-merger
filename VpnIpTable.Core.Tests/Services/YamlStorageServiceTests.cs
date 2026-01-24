using Xunit;
using VpnIpTable.Core.Services;
using VpnIpTable.Core.Models;
using System.IO;
using System.Linq;

namespace VpnIpTable.Core.Tests.Services;

public class YamlStorageServiceTests
{
    private readonly YamlStorageService _storageService;
    private readonly CidrService _cidrService;
    private readonly string _testFilePath;

    public YamlStorageServiceTests()
    {
        _storageService = new YamlStorageService();
        _cidrService = new CidrService();
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_addresses_{Guid.NewGuid()}.yaml");
    }

    [Fact]
    public void SaveToFile_ValidRanges_CreatesFile()
    {
        var ranges = new List<IpRange>
        {
            _cidrService.ParseCidr("192.168.1.0/24"),
            _cidrService.ParseCidr("10.0.0.0/8")
        };

        _storageService.SaveToFile(ranges, _testFilePath);

        Assert.True(File.Exists(_testFilePath));
        var content = File.ReadAllText(_testFilePath);
        Assert.Contains("192.168.1.0/24", content);
        Assert.Contains("10.0.0.0/8", content);
    }

    [Fact]
    public void LoadFromFile_ExistingFile_ReturnsRanges()
    {
        var ranges = new List<IpRange>
        {
            _cidrService.ParseCidr("192.168.1.0/24"),
            _cidrService.ParseCidr("10.0.0.0/8")
        };

        _storageService.SaveToFile(ranges, _testFilePath);
        var loaded = _storageService.LoadFromFile(_testFilePath, _cidrService);

        Assert.Equal(2, loaded.Count);
        Assert.Contains(loaded, r => r.ToString() == "192.168.1.0/24");
        Assert.Contains(loaded, r => r.ToString() == "10.0.0.0/8");
    }

    [Fact]
    public void LoadFromFile_NonExistentFile_ReturnsEmptyList()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.yaml");

        var loaded = _storageService.LoadFromFile(nonExistentPath, _cidrService);

        Assert.Empty(loaded);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_ReturnsSameData()
    {
        var originalRanges = new List<IpRange>
        {
            _cidrService.ParseCidr("192.168.1.0/24"),
            _cidrService.ParseCidr("10.0.0.0/8"),
            _cidrService.ParseCidr("172.16.0.0/16")
        };

        _storageService.SaveToFile(originalRanges, _testFilePath);
        var loadedRanges = _storageService.LoadFromFile(_testFilePath, _cidrService);

        Assert.Equal(originalRanges.Count, loadedRanges.Count);
        foreach (var original in originalRanges)
        {
            Assert.Contains(loadedRanges, r => r.ToString() == original.ToString());
        }
    }

    // Cleanup
    ~YamlStorageServiceTests()
    {
        try
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
        }
        catch { }
    }
}
