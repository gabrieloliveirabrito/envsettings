using DotSettings;
using Xunit;

namespace DotSettingsTests;

public sealed class EnvFileBootstrapTests
{
    [Theory]
    [InlineData("1234567890", "1234567890")]
    [InlineData("1234567890  # canal de audit", "1234567890")]
    [InlineData("1234567890         #", "1234567890")]
    [InlineData("1234567890#sem-espaco", "1234567890")]
    [InlineData("\"keep # inside\"", "keep # inside")]
    [InlineData("'keep # inside'", "keep # inside")]
    [InlineData("\"1234567890\"         # canal", "1234567890")]
    [InlineData("  plain  ", "plain")]
    [InlineData("", "")]
    public void NormalizeEnvValue_StripsUnquotedInlineComments(string raw, string expected) =>
        Assert.Equal(expected, EnvFileBootstrap.NormalizeEnvValue(raw));
}
