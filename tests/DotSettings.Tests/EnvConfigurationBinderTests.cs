using DotSettings;
using Xunit;

namespace DotSettingsTests;

public sealed class EnvConfigurationBinderTests
{
    private sealed class SampleSettings : DotSettings<SampleSettings>
    {
        [EnvKey("REQUIRED_KEY")]
        public string Required { get; set; } = "default";

        [EnvKey("OPTIONAL_KEY")]
        public string? Optional { get; set; }

        [EnvKey("INT_KEY")]
        public int Count { get; set; }

        public string Ignored { get; set; } = "stay";
    }

    private sealed class SampleHost : DotSettings<SampleHost>
    {
        [EnvSubConfiguration]
        public SampleSettings SampleSettings { get; set; } = new();
    }

    public EnvConfigurationBinderTests()
    {
        Env.ResetForTesting();
    }

    [Fact]
    public void Bind_OnlyEnvKeyProperties()
    {
        Environment.SetEnvironmentVariable("REQUIRED_KEY", "ok");
        Environment.SetEnvironmentVariable("INT_KEY", "42");

        try
        {
            var source = new EnvValueSource();
            var settings = new SampleSettings();
            EnvConfigurationBinder.BindObject(settings, source, registerInstances: false);

            Assert.Equal("ok", settings.Required);
            Assert.Equal(42, settings.Count);
            Assert.Null(settings.Optional);
            Assert.Equal("stay", settings.Ignored);
        }
        finally
        {
            Environment.SetEnvironmentVariable("REQUIRED_KEY", null);
            Environment.SetEnvironmentVariable("INT_KEY", null);
        }
    }

    [Fact]
    public void Bind_MissingRequiredKey_Throws()
    {
        Environment.SetEnvironmentVariable("REQUIRED_KEY", null);

        var source = new EnvValueSource();
        var settings = new SampleSettings();
        var ex = Assert.Throws<EnvConfigurationException>(() =>
            EnvConfigurationBinder.BindObject(settings, source, registerInstances: false));

        Assert.Contains("REQUIRED_KEY", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Bind_MissingNullableKey_KeepsDefault()
    {
        Environment.SetEnvironmentVariable("REQUIRED_KEY", "ok");
        Environment.SetEnvironmentVariable("INT_KEY", "1");
        Environment.SetEnvironmentVariable("OPTIONAL_KEY", null);

        try
        {
            var source = new EnvValueSource();
            var settings = new SampleSettings();
            EnvConfigurationBinder.BindObject(settings, source, registerInstances: false);
            Assert.Null(settings.Optional);
        }
        finally
        {
            Environment.SetEnvironmentVariable("REQUIRED_KEY", null);
            Environment.SetEnvironmentVariable("INT_KEY", null);
        }
    }

    [Fact]
    public void Load_RegistersSharedAndSubConfiguration()
    {
        Environment.SetEnvironmentVariable("REQUIRED_KEY", "shared-ok");
        Environment.SetEnvironmentVariable("INT_KEY", "7");

        try
        {
            Env.Load<SampleHost>();
            Assert.Equal("shared-ok", SampleSettings.Shared.Required);
            Assert.Same(SampleSettings.Shared, DotSettingsCatalog.GetSettings<SampleSettings>());
            Assert.Same(SampleSettings.Shared, SampleHost.Shared.SampleSettings);
        }
        finally
        {
            Env.ResetForTesting();
            Environment.SetEnvironmentVariable("REQUIRED_KEY", null);
            Environment.SetEnvironmentVariable("INT_KEY", null);
        }
    }

    [Fact]
    public void LogLevels_ParseAndRejectInvalid()
    {
        var map = LogLevelsParser.Parse("Microsoft:Warning;Other:Info");
        Assert.Equal(SystemLogLevel.Warning, map["Microsoft"]);
        Assert.Equal(SystemLogLevel.Information, map["Other"]);

        Assert.Throws<EnvConfigurationException>(() => LogLevelsParser.Parse("BadSegment"));
    }

    [Fact]
    public void EnvKeyHelper_ResolvesAttributeName()
    {
        Assert.Equal(
            "REQUIRED_KEY",
            EnvKeyHelper.GetVariableName<SampleSettings>(nameof(SampleSettings.Required)));
    }
}
