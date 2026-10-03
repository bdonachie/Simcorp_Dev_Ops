using Xunit;

namespace Dimension.ServiceControl.Tests
{
    public class CommandArgumentBuilderTests
    {
        [Fact]
        public void Build_WritesTheCommandWordsThenTheSwitchesInOrder()
        {
            var arguments = new CommandArgumentBuilder("service", "list")
                .AddSwitch("format", "xml")
                .AddFlag("online")
                .AddSwitchIfSet("agent", "APPSERVER01");

            Assert.Equal("service list -format:xml -online -agent:APPSERVER01", arguments.Build());
            Assert.Equal(arguments.Build(), arguments.ToString());
        }

        [Fact]
        public void AddFlag_IsLeftOutWhenNotIncluded()
        {
            var arguments = new CommandArgumentBuilder("service", "list").AddFlag("online", include: false);

            Assert.Equal("service list", arguments.Build());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddSwitchIfSet_DropsABlankValueRatherThanSendingAnEmptyFilter(string value)
        {
            var arguments = new CommandArgumentBuilder("service", "list").AddSwitchIfSet("agent", value);

            Assert.Equal("service list", arguments.Build());
        }

        [Fact]
        public void AddSwitchIfSet_DropsANullNumberAndWritesAPresentOne()
        {
            var arguments = new CommandArgumentBuilder("service", "start")
                .AddSwitchIfSet("count", (int?)null)
                .AddSwitchIfSet("count", 12);

            Assert.Equal("service start -count:12", arguments.Build());
        }

        [Theory]
        [InlineData(null, "service list")]
        [InlineData(true, "service list -stale:Yes")]
        [InlineData(false, "service list -stale:No")]
        public void AddYesNoSwitchIfSet_WritesTheLiteralTheUtilityExpects(bool? value, string expected)
        {
            var arguments = new CommandArgumentBuilder("service", "list").AddYesNoSwitchIfSet("stale", value);

            Assert.Equal(expected, arguments.Build());
        }

        [Theory]
        [InlineData("plain", "-nickname:plain")]
        [InlineData("", "-nickname:\"\"")]
        [InlineData("two words", "-nickname:\"two words\"")]
        [InlineData("say \"hi\"", "-nickname:\"say \\\"hi\\\"\"")]
        [InlineData("C:\\my dir\\", "-nickname:\"C:\\my dir\\\\\"")]
        [InlineData("a\\\"b", "-nickname:\"a\\\\\\\"b\"")]
        public void AddSwitch_QuotesOnlyWhenNeededAndEscapesTheWayTheWindowsParserExpects(string value, string expected)
        {
            var arguments = new CommandArgumentBuilder().AddSwitch("nickname", value);

            Assert.Equal(expected, arguments.Build());
        }
    }
}
