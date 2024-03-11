//---------------------------------------------------------------------
// <copyright file="ServiceProviderExtensionsTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.OData.Tests
{
    public class ServiceProviderExtensionsTests
    {
        private IServiceCollection Services;
        public ServiceProviderExtensionsTests()
        {
            Services = new ServiceCollection();
        }
        [Fact]
        public void GetNonExistingServiceGeneric()
        {
            Services.AddTransient(typeof(Foo));
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.Null(container.GetService<IFoo>());
        }

        [Fact]
        public void GetServiceGeneric()
        {
            Services.AddTransient(typeof(Foo));
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.NotNull(container.GetService<Foo>());
        }

        [Fact]
        public void GetServiceNonGeneric()
        {
            Services.AddTransient(typeof(Foo));
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.NotNull(container.GetService(typeof(Foo)));
        }

        [Fact]
        public void GetNonExistingRequiredServiceThrows()
        {
            Services.AddTransient(typeof(Foo));
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.Throws<InvalidOperationException>(() => container.GetRequiredService<IFoo>());
        }

        [Fact]
        public void GetRequiredServiceGeneric()
        {
            Services.AddTransient(typeof(Foo));
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.NotNull(container.GetRequiredService<Foo>());
        }

        [Fact]
        public void GetRequiredServiceNonGeneric()
        {
            Services.AddTransient(typeof(Foo));
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.NotNull(container.GetRequiredService(typeof(Foo)));
        }

        [Fact]
        public void GetServicesNonGeneric()
        {
            Services.AddTransient<IFoo, Foo>();
            Services.AddTransient<IFoo, Bar>();
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.Equal(2, container.GetServices(typeof(IFoo)).Count());
        }

        [Fact]
        public void GetServicesGeneric()
        {
            Services.AddTransient<IFoo, Foo>();
            Services.AddTransient<IFoo, Bar>();
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.Equal(2, container.GetServices<IFoo>().Count());
        }

        private interface IFoo { }

        private class Foo : IFoo { }

        private class Bar : IFoo { }
    }
}
