using Xunit.Sdk;

namespace BlazorServerAppTests.UnitTests
{
    public class ServerManagerLogicTests
    {
        public class AddOrUpdate
        {
            [Theory]
            [AutoDomainData]
            public void WhenAddingDtoThenItIsAdded(
                ServerManagerLogic sut)
            {
                // Arrange
                ServiceDto dto1 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5000,
                    Metadata = { {"test1", "value1"} }
                };

                bool fired = false;
                sut.ServiceModelListChanged += (sender, args) => fired = true;

                // Act
                sut.AddOrUpdate(dto1);
                var item = sut.ServiceDictionary.FirstOrDefault();

                // Act. adding null does nothing
                sut.AddOrUpdate(null);
                
                // Assert
                Assert.NotNull(item.Value);
                Assert.Equal(item.Value.Port, dto1.ServicePort);
                Assert.Equal(item.Value.Scope, dto1.Scope);
                Assert.Equal(item.Value.Address, dto1.ServiceHost);
                Assert.Equal(item.Value.HealthState, dto1.HealthState);
                Assert.Equal(item.Value.ServiceName, dto1.ServiceName);
                Assert.Equal(item.Value.Metadata["test1"], dto1.Metadata["test1"]);
                Assert.True(fired);
            }

            [Theory]
            [AutoDomainData]
            public void WhenUpdatingDtoThenItIsUpdated(
                ServerManagerLogic sut)
            {
                // Arrange
                ServiceDto dto1 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5000,
                    Metadata = { {"test1", "value1"} }
                };

                bool fired = false;
                sut.ServiceModelListChanged += (sender, args) => fired = true;

                // Act
                sut.AddOrUpdate(dto1);
                fired = false;
                
                dto1 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Unhealthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5000,
                    Metadata = { {"test1", "value2"} }
                };

                sut.AddOrUpdate(dto1);
                var item = sut.ServiceDictionary.FirstOrDefault();
                
                // Assert
                Assert.NotNull(item.Value);
                Assert.Equal(item.Value.Port, dto1.ServicePort);
                Assert.Equal(item.Value.Scope, dto1.Scope);
                Assert.Equal(item.Value.Address, dto1.ServiceHost);
                Assert.Equal(item.Value.HealthState, dto1.HealthState);
                Assert.Equal(item.Value.ServiceName, dto1.ServiceName);
                Assert.Equal(item.Value.Metadata["test1"], dto1.Metadata["test1"]);
                Assert.True(fired);
            }
        }

        public class FindService
        {
            [Theory]
            [AutoDomainData]
            public void WhenFindServiceCase1(
                ServerManagerLogic sut)
            {
                // Arrange
                ServiceDto dto1 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5000,
                    Metadata = { {"test1", "value1"} }
                };

                bool fired = false;
                sut.ServiceModelListChanged += (sender, args) => fired = true;

                // Act
                sut.AddOrUpdate(dto1);
                var item = sut.FindService("name1").FirstOrDefault();
                
                // Assert
                Assert.NotNull(item);
                Assert.Equal(item.ServicePort, dto1.ServicePort);
                Assert.Equal(item.Scope, dto1.Scope);
                Assert.Equal(item.ServiceHost, dto1.ServiceHost);
                Assert.Equal(item.HealthState, dto1.HealthState);
                Assert.Equal(item.ServiceName, dto1.ServiceName);
                Assert.Equal(item.Metadata["test1"], dto1.Metadata["test1"]);
                Assert.True(fired);
            }

            [Theory]
            [AutoDomainData]
            public void WhenFindServiceCase2(
                ServerManagerLogic sut)
            {
                // Arrange
                ServiceDto dto1 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5000,
                    Metadata = { {"test1", "value1"} }
                };

                bool fired = false;
                sut.ServiceModelListChanged += (sender, args) => fired = true;

                // Act
                sut.AddOrUpdate(dto1);
                var item = sut.FindService("name1", "env1").FirstOrDefault();

                sut.ServiceDictionary.First().Value.Enabled = false;
                var itemDisabled = sut.FindService("name1", "env1").FirstOrDefault();
                
                // Assert
                Assert.Null(itemDisabled);
                Assert.NotNull(item);
                Assert.Equal(item.ServicePort, dto1.ServicePort);
                Assert.Equal(item.Scope, dto1.Scope);
                Assert.Equal(item.ServiceHost, dto1.ServiceHost);
                Assert.Equal(item.HealthState, dto1.HealthState);
                Assert.Equal(item.ServiceName, dto1.ServiceName);
                Assert.Equal(item.Metadata["test1"], dto1.Metadata["test1"]);
                Assert.True(fired);
            }

            [Theory]
            [AutoDomainData]
            public void WhenFindServiceCase3(
                ServerManagerLogic sut)
            {
                // Arrange
                ServiceDto dto1 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5000,
                    Metadata = { {"test1", "value1"} }
                };
                
                sut.AddOrUpdate(dto1);
                
                // Act
                var item1 = sut.FindService("name1", "env2").FirstOrDefault();
                var item2 = sut.FindService("name2", "env2").FirstOrDefault();
                var item3 = sut.FindService("name2", "env1").FirstOrDefault();
                var item4 = sut.FindService("name2").FirstOrDefault();
                var item5 = sut.FindService("name1", "env2").FirstOrDefault();
                
                // Assert
                Assert.Null(item1);
                Assert.Null(item2);
                Assert.Null(item3);
                Assert.Null(item4);
                Assert.Null(item5);
            }
        }

        public class TimeOut
        {
            [Theory]
            [AutoDomainData]
            public void WhenServiceRegistrationTimesOutThenItChangesToOffline(
                [Frozen] ServiceConfigurationOptions options,
                ServerManagerLogic sut)
            {
                // Arrange
                sut.Dispose();
                options.TimeOutInSeconds = 1;
                sut = new ServerManagerLogic(options);

                ServiceDto dto1 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5000,
                    Metadata = { {"test1", "value1"} }
                };

                bool fired = false;
                sut.ServiceModelListChanged += (sender, args) => fired = true;

                // Act
                sut.AddOrUpdate(dto1);
                var item = sut.FindService("name1").FirstOrDefault();
                
                // Assert
                Assert.NotNull(item);
                Assert.Equal(item.ServicePort, dto1.ServicePort);
                Assert.Equal(item.Scope, dto1.Scope);
                Assert.Equal(item.ServiceHost, dto1.ServiceHost);
                Assert.Equal(item.HealthState, dto1.HealthState);
                Assert.Equal(item.ServiceName, dto1.ServiceName);
                Assert.Equal(item.Metadata["test1"], dto1.Metadata["test1"]);
                Assert.True(fired);

                // Now wait a second for timeout
                fired = false;

                // Act
                SpinWait.SpinUntil(() => fired, TimeSpan.FromSeconds(2));
                var item1 = sut.ServiceDictionary.FirstOrDefault().Value;
                
                // Assert
                Assert.Equal(EnumServiceHealth.Offline, item1.HealthState);
                Assert.True(fired);
            }

        }
    }
}
