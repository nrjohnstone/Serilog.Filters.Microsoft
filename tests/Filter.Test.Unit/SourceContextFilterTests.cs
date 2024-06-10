using Serilog.Events;
using Serilog.Filters.Microsoft;
using Serilog.Parsing;

namespace Filter.Test.Unit;

[TestFixture]
public class SourceContextFilterTests
{
    [TestCase(LogEventLevel.Information, true)]
    [TestCase(LogEventLevel.Debug, false)]
    public void When_FilterHasNoFilterValues_ShouldUseSpecifiedDefaultLogLevel(LogEventLevel logEventLevel, bool expectedIsEnabled)
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>(), LogEventLevel.Information);

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent("A.B", logEventLevel));

        isEnabled.Should().Be(expectedIsEnabled);
    }
    
    [Test]
    public void When_FilterDefaultLogLevelNotSpecified_ShouldUseVerboseAsDefault()
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>());

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent("A.B", LogEventLevel.Verbose));

        isEnabled.Should().BeTrue();
    }
    
    [TestCase(LogEventLevel.Verbose)]
    [TestCase(LogEventLevel.Debug)]
    [TestCase(LogEventLevel.Information)]
    public void When_SourceContextMatches_AndLogLevelIsLessThanFilter_ShouldNotBeEnabled(LogEventLevel logEventLevel)
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>() { {"A.B", LogEventLevel.Warning }});

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent("A.B", LogEventLevel.Information));

        isEnabled.Should().BeFalse();
    }

    [Test]
    public void When_SourceContextMatches_AndLogLevelIsEqualToFilter_ShouldBeEnabled()
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>() { {"A.B", LogEventLevel.Warning }});

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent("A.B", LogEventLevel.Warning));

        isEnabled.Should().BeTrue();
    }
    
    [TestCase(LogEventLevel.Error)]
    [TestCase(LogEventLevel.Fatal)]
    public void When_SourceContextMatches_AndLogLevelIsGreaterThanFilter_ShouldBeEnabled(LogEventLevel logEventLevel)
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>() { {"A.B", LogEventLevel.Warning }});

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent("A.B", logEventLevel));

        isEnabled.Should().BeTrue();
    }
    
    [Test]
    public void When_SourceContextMatches_AndLogLevelIsLessThanFilter_ShouldNotBeEnabled()
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>()
        {
            {"A.B", LogEventLevel.Warning }
        }, LogEventLevel.Debug);

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent("A.B", LogEventLevel.Information));

        // assert
        isEnabled.Should().BeFalse("Log event should be filtered due to its log level being < a matching source context level");
    }
    
    [Test]
    public void When_SourceContextMatches_AndLogLevelIsEqualToFilter_ShouldIgnoreDefaultLevel()
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>()
        {
            {"A.B", LogEventLevel.Warning }
        }, LogEventLevel.Error);

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent("A.B", LogEventLevel.Warning));

        // assert
        isEnabled.Should().BeTrue("Log event should not be filtered due to having a loglevel >= a matching source context level");
    }
    
    [Test]
    public void When_LogHasNoSourceContextProperty_AndLogLevelIsEqualToDefault_ShouldBeEnabled()
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>()
        {
            {"A.B", LogEventLevel.Warning }
        }, LogEventLevel.Information);

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent(LogEventLevel.Information));

        // assert
        isEnabled.Should().BeTrue("Log event should not be filtered due to having a loglevel >= a matching source context level");
    }
    
    [Test]
    public void When_LogHasNoSourceContextProperty_AndLogLevelIsLessThanDefault_ShouldNotBeEnabled()
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>()
        {
            {"A.B", LogEventLevel.Verbose }
        }, LogEventLevel.Information);

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent(LogEventLevel.Debug));

        // assert
        isEnabled.Should().BeFalse("Log event should be filtered due to having a loglevel < the default logging level");
    }
    
    [Test]
    public void When_LogHasNoSourceContextProperty_AndDefaultLogLevelIsNull_ShouldBeEnabled()
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>()
        {
            {"A.B", LogEventLevel.Warning }
        });

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent(LogEventLevel.Debug));

        // assert
        isEnabled.Should().BeTrue("Log event should be filtered due to having a loglevel < the default logging level");
    }
    
    [TestCase("A.B.C", LogEventLevel.Information)]
    [TestCase("A.B.C.C", LogEventLevel.Information)]
    [TestCase("A.B", LogEventLevel.Warning)]
    [TestCase("A.B.X", LogEventLevel.Warning)]
    [TestCase("A.B.C.D", LogEventLevel.Error)]
    [TestCase("A.B.C.D.X", LogEventLevel.Error)]
    public void When_SourceContextMatchesMultipleFilters_TheMostSpecificFilterShouldBeUsed(string sourceContext, LogEventLevel logEventLevel)
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>() {
        { "A.B", LogEventLevel.Warning },
        { "A.B.C", LogEventLevel.Debug },
        { "A.B.C.D", LogEventLevel.Error }
        });

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent(sourceContext, logEventLevel));

        isEnabled.Should().BeTrue();
    }
    
    /// <summary>
    /// Ensure that matching on the filters is only done for the full dot seperated category
    /// </summary>
    /// <param name="sourceContextFilter"></param>
    /// <param name="sourceContext"></param>
    [TestCase("A", "AAA")]
    [TestCase("A.B", "A.BBB")]
    public void When_SourceContextMatchesPartialCategory_ShouldNotBeEnabled(string sourceContextFilter, string sourceContext)
    {
        SourceContextFilter sut = new SourceContextFilter(new Dictionary<string, LogEventLevel>()
        {
            { sourceContextFilter, LogEventLevel.Warning }
        });

        // act
        bool isEnabled = sut.IsEnabled(CreateLogEvent(sourceContext, LogEventLevel.Information));

        // assert
        isEnabled.Should().BeTrue("Log event should not filtered due to not matching category name");
    }
    
    private LogEvent CreateLogEvent(string sourceContext, LogEventLevel logEventLevel)
    {
        var logEventProperties = new List<LogEventProperty>()
        {
            new LogEventProperty("SourceContext", new ScalarValue(sourceContext))
        };
        
        return new LogEvent(DateTimeOffset.Now, logEventLevel, null, new MessageTemplate("Some Log Template", new List<MessageTemplateToken>()),
            logEventProperties);
    }
    
    private LogEvent CreateLogEvent(LogEventLevel logEventLevel)
    {
        var logEventProperties = new List<LogEventProperty>();
        
        return new LogEvent(DateTimeOffset.Now, logEventLevel, null, new MessageTemplate("Some Log Template", new List<MessageTemplateToken>()),
            logEventProperties);
    }
}