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
        private IServiceCollection services;
        public ServiceProviderExtensionsTests()
        {
            services = new ServiceCollection();
        }

        [Fact]
        public void GetNonExistingServiceGeneric()
        {
            services.AddTransient(typeof(Foo));
            IServiceProvider container = services.BuildServiceProvider();
            Assert.Null(container.GetService<IFoo>());
        }

        [Fact]
        public void GetServiceGeneric()
        {
            services.AddTransient(typeof(Foo));
            IServiceProvider container = services.BuildServiceProvider();
            Assert.NotNull(container.GetService<Foo>());
        }

        [Fact]
        public void GetServiceNonGeneric()
        {
            services.AddTransient(typeof(Foo));
            IServiceProvider container = services.BuildServiceProvider();
            Assert.NotNull(container.GetService(typeof(Foo)));
        }

        [Fact]
        public void GetNonExistingRequiredServiceThrows()
        {
            services.AddTransient(typeof(Foo));
            IServiceProvider container = services.BuildServiceProvider();
            Assert.Throws<InvalidOperationException>(() => container.GetRequiredService<IFoo>());
        }

        [Fact]
        public void GetRequiredServiceGeneric()
        {
            services.AddTransient(typeof(Foo));
            IServiceProvider container = services.BuildServiceProvider();
            Assert.NotNull(container.GetRequiredService<Foo>());
        }

        [Fact]
        public void GetRequiredServiceNonGeneric()
        {
            services.AddTransient(typeof(Foo));
            IServiceProvider container = services.BuildServiceProvider();
            Assert.NotNull(container.GetRequiredService(typeof(Foo)));
        }

        [Fact]
        public void GetServicesNonGeneric()
        {
            services.AddTransient<IFoo, Foo>();
            services.AddTransient<IFoo, Bar>();
            IServiceProvider container = services.BuildServiceProvider();
            Assert.Equal(2, container.GetServices(typeof(IFoo)).Count());
        }

        [Fact]
        public void GetServicesGeneric()
        {
            services.AddTransient<IFoo, Foo>();
            services.AddTransient<IFoo, Bar>();
            IServiceProvider container = services.BuildServiceProvider();
            Assert.Equal(2, container.GetServices<IFoo>().Count());
        }

        private interface IFoo { }

        private class Foo : IFoo { }

        private class Bar : IFoo { }
    }
}
