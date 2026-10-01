using System.Xml.Linq;

namespace BudgetFriend.App.Tests;

/// <summary>
/// The English and Arabic <c>.resx</c> files have to stay in lockstep. A key that
/// exists in only one of them is not a build error — the lookup just falls back
/// to the key name and the user sees "Legal.Privacy.Storage.P1" on screen.
/// </summary>
public class LocalizationParityTests {
    private static readonly string ResxDirectory = FindResxDirectory();

    [Fact]
    public void Arabic_and_English_define_the_same_keys() {
        var english = ReadKeys("AppStrings.resx").ToHashSet(StringComparer.Ordinal);
        var arabic = ReadKeys("AppStrings.ar.resx").ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(english);
        Assert.Empty(english.Except(arabic, StringComparer.Ordinal));
        Assert.Empty(arabic.Except(english, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("AppStrings.resx")]
    [InlineData("AppStrings.ar.resx")]
    public void Every_key_has_a_translated_value(string fileName) {
        foreach (var entry in ReadEntries(fileName)) {
            Assert.False(string.IsNullOrWhiteSpace(entry.Value), $"{fileName}: '{entry.Key}' is blank.");
            // LocalizationService returns the key itself when a lookup misses.
            Assert.NotEqual(entry.Key, entry.Value);
        }
    }

    [Theory]
    [InlineData("AppStrings.resx")]
    [InlineData("AppStrings.ar.resx")]
    public void No_value_contains_markup_because_strings_are_rendered_encoded(string fileName) {
        foreach (var entry in ReadEntries(fileName)) {
            Assert.DoesNotContain('<', entry.Value);
            Assert.DoesNotContain('>', entry.Value);
        }
    }

    // ---- Helpers ------------------------------------------------------------

    private static IEnumerable<string> ReadKeys(string fileName) =>
        ReadEntries(fileName).Select(entry => entry.Key);

    private static IEnumerable<(string Key, string Value)> ReadEntries(string fileName) {
        var document = XDocument.Load(Path.Combine(ResxDirectory, fileName));
        return document.Root!
            .Elements("data")
            .Select(element => (
                Key: (string?)element.Attribute("name") ?? "",
                Value: element.Element("value")?.Value ?? ""))
            .OrderBy(entry => entry.Key, StringComparer.Ordinal);
    }

    private static string FindResxDirectory() {
        const string relative = "src/BudgetFriend.App/Resources/Localization";

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent) {
            var candidate = Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            if (Directory.Exists(candidate)) {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException($"Could not locate '{relative}' above '{AppContext.BaseDirectory}'.");
    }
}