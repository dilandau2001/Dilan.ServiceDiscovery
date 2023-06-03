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
                    Metadata = { { "test1", "value1" } }
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
                    Metadata = { { "test1", "value1" } }
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
                    Metadata = { { "test1", "value2" } }
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
                    Metadata = { { "test1", "value1" } }
                };

                bool fired = false;
                sut.ServiceModelListChanged += (sender, args) => fired = true;

                // Act
                sut.AddOrUpdate(dto1);
                var item = sut.FindService("name1").FirstOrDefault();

                // Assert
                Assert.NotNull(item);
                Assert.Equal(item.Port, dto1.ServicePort);
                Assert.Equal(item.Scope, dto1.Scope);
                Assert.Equal(item.Address, dto1.ServiceHost);
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
                    Metadata = { { "test1", "value1" } }
                };

                bool fired = false;
                sut.ServiceModelListChanged += (sender, args) => fired = true;

                // Act
                sut.AddOrUpdate(dto1);
                var item = sut.FindService("name1", "env1").FirstOrDefault();

                sut.ServiceDictionary.First().Value.Enabled = false;
                sut.ForceRefresh();
                var itemDisabled = sut.FindService("name1", "env1").FirstOrDefault();

                // Assert
                Assert.Null(itemDisabled);
                Assert.NotNull(item);
                Assert.Equal(item.Port, dto1.ServicePort);
                Assert.Equal(item.Scope, dto1.Scope);
                Assert.Equal(item.Address, dto1.ServiceHost);
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
                    Metadata = { { "test1", "value1" } }
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
                    Metadata = { { "test1", "value1" } }
                };

                bool fired = false;
                sut.ServiceModelListChanged += (sender, args) => fired = true;

                // Act
                sut.AddOrUpdate(dto1);
                var item = sut.FindService("name1").FirstOrDefault();

                // Assert
                Assert.NotNull(item);
                Assert.Equal(item.Port, dto1.ServicePort);
                Assert.Equal(item.Scope, dto1.Scope);
                Assert.Equal(item.Address, dto1.ServiceHost);
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

        public class Principal
        {
            /// <summary>
            /// When several services of the same group registers, only one gets principal.
            /// </summary>
            [Theory]
            [AutoDomainData]
            public void WhenMultipleServicesThenOnlyOnePrincipal(
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
                    Metadata = { { "test1", "value1" } }
                };

                ServiceDto dto2 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5001,
                    Metadata = { { "test1", "value1" } }
                };

                ServiceDto dto3 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5002,
                    Metadata = { { "test1", "value1" } }
                };

                // Act
                sut.AddOrUpdate(dto1);
                sut.AddOrUpdate(dto2);
                sut.AddOrUpdate(dto3);
                var item = sut.FindService("name1");

                // Assert
                Assert.Equal(3, item.Count);
                Assert.True(item.Count(dto => dto.Principal) == 1);
            }

            /// <summary>
            /// When principal gets unhealthy, next service to register gets Principal.
            /// </summary>
            [Theory]
            [AutoDomainData]
            public void WhenPrincipalIsLostThenNextServiceGetsPrincipal(
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
                    Metadata = { { "test1", "value1" } }
                };

                ServiceDto dto2 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5001,
                    Metadata = { { "test1", "value1" } }
                };

                ServiceDto dto3 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5002,
                    Metadata = { { "test1", "value1" } }
                };

                // Act
                sut.AddOrUpdate(dto1);
                sut.AddOrUpdate(dto2);
                sut.AddOrUpdate(dto3);
                var item = sut.FindService("name1");

                // Assert
                Assert.Equal(3, item.Count);
                Assert.True(item.Count(dto => dto.Principal) == 1);

                // Act
                dto1.HealthState = EnumServiceHealth.Unhealthy;
                sut.AddOrUpdate(dto1);
                item = sut.FindService(dto1.ServiceName);

                // Assert
                Assert.Equal(2, item.Count);
                Assert.DoesNotContain(item, dto => dto.Principal);

                // Act
                sut.AddOrUpdate(dto2);
                item = sut.FindService(dto1.ServiceName);

                // Assert
                Assert.Equal(2, item.Count);
                Assert.True(item.Count(dto => dto.Principal) == 1);
            }

            
            /// <summary>
            /// When principal gets disabled, next service to register gets Principal.
            /// </summary>
            [Theory]
            [AutoDomainData]
            public void WhenPrincipalIsDisabledThenNextServiceGetsPrincipal(
               ServiceConfigurationOptions options)
            {
                // Arrange
                options.TimeOutInSeconds = 100;
                var sut = new ServerManagerLogic(options);

                ServiceDto dto1 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5000,
                    Metadata = { { "test1", "value1" } }
                };

                ServiceDto dto2 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5001,
                    Metadata = { { "test1", "value1" } }
                };

                // Act
                var s1 = sut.AddOrUpdate(dto1);
                var s2 = sut.AddOrUpdate(dto2);
                Assert.True(s1.Principal);
                Assert.False(s2.Principal);
                Assert.True(s1.Enabled);
                Assert.True(s2.Enabled);
                s1.Enabled = false;

                sut.ForceRefresh();
                var item = sut.FindService(dto1.ServiceName);

                // Assert
                Assert.Single(item);
                Assert.False(item[0].Principal);

                // Act
                sut.AddOrUpdate(dto2);
                item = sut.FindService(dto1.ServiceName);

                // Assert
                Assert.Single(item);
                Assert.True(item.Count(dto => dto.Principal) == 1);
            }
        }
    }
}
