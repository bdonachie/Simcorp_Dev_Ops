using System.Collections.Generic;
using Dimension.ServiceControl.Model;
using Xunit;

namespace Dimension.ServiceControl.Tests
{
    public class ServiceListFilterTests
    {
        [Fact]
        public void Validate_AcceptsAnEmptyFilter()
        {
            Assert.Empty(new ServiceListFilter().Validate());
        }

        [Fact]
        public void Validate_RejectsAConfigurationWithoutAServiceType()
        {
            var filter = new ServiceListFilter { ServiceConfiguration = "7" };

            Assert.Single(filter.Validate(), problem => problem.Contains("service type"));
        }

        [Fact]
        public void Validate_RejectsWithoutConfigurationWithoutAServiceType()
        {
            var filter = new ServiceListFilter { WithoutConfiguration = true };

            Assert.Single(filter.Validate(), problem => problem.Contains("service type"));
        }

        [Fact]
        public void Validate_RejectsANicknameCombinedWithWithoutNickname()
        {
            var filter = new ServiceListFilter { Nickname = "calc-a", WithoutNickname = true };

            Assert.Single(filter.Validate(), problem => problem.Contains("nickname"));
        }

        [Fact]
        public void Validate_RejectsAConfigurationCombinedWithWithoutConfiguration()
        {
            var filter = new ServiceListFilter
            {
                ServiceType = "Calculation service",
                ServiceConfiguration = "7",
                WithoutConfiguration = true
            };

            Assert.Single(filter.Validate(), problem => problem.Contains("without one"));
        }
    }

    public class ServiceStartRequestTests
    {
        [Fact]
        public void Validate_RequiresAServiceType()
        {
            Assert.Single(new ServiceStartRequest().Validate(), problem => problem.Contains("service type"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        [InlineData(-5)]
        public void Validate_RejectsACountOutsideTheUtilityRange(int count)
        {
            var request = new ServiceStartRequest { ServiceType = "Calculation service", Count = count };

            Assert.Single(request.Validate(), problem => problem.Contains("between 1 and 100"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public void Validate_AcceptsACountAtTheEdgesOfTheRange(int count)
        {
            var request = new ServiceStartRequest { ServiceType = "Calculation service", Count = count };

            Assert.Empty(request.Validate());
        }
    }

    public class ServiceStopRequestTests
    {
        [Fact]
        public void Validate_RejectsARequestThatNamesNothingAndFiltersNothing()
        {
            Assert.Single(new ServiceStopRequest().Validate(), problem => problem.Contains("by accident"));
        }

        [Fact]
        public void Validate_RejectsNamesCombinedWithFilters()
        {
            var request = new ServiceStopRequest
            {
                ServiceInstanceNames = new List<string> { "APPSERVER01\\215-1" },
                ServiceType = "Calculation service"
            };

            Assert.Single(request.Validate(), problem => problem.Contains("ignores the filters"));
        }

        [Fact]
        public void Validate_AcceptsExplicitNames()
        {
            var request = new ServiceStopRequest
            {
                ServiceInstanceNames = new List<string> { "APPSERVER01\\215-1", "APPSERVER01\\215-2" }
            };

            Assert.True(request.NamesInstances);
            Assert.False(request.HasFilters);
            Assert.Empty(request.Validate());
        }

        [Fact]
        public void Validate_AcceptsASingleFilter()
        {
            var request = new ServiceStopRequest { ManagementMode = ManagementMode.Manual };

            Assert.True(request.HasFilters);
            Assert.Empty(request.Validate());
        }

        [Fact]
        public void Validate_RejectsANonPositiveCount()
        {
            var request = new ServiceStopRequest { ServiceType = "Calculation service", Count = 0 };

            Assert.Single(request.Validate(), problem => problem.Contains("positive"));
        }

        [Fact]
        public void NamesInstances_IsFalseWhenTheListIsNull()
        {
            var request = new ServiceStopRequest { ServiceInstanceNames = null };

            Assert.False(request.NamesInstances);
        }
    }
}
