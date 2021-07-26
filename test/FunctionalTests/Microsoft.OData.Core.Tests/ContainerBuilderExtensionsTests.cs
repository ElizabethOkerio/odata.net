//---------------------------------------------------------------------
// <copyright file="ContainerBuilderExtensionsTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Test.OData.DependencyInjection;
using Xunit;

namespace Microsoft.OData.Tests
{
    public class ContainerBuilderExtensionsTests
    {
        private IServiceCollection services;
        public ContainerBuilderExtensionsTests()
        {
            services = new ServiceCollection();
        }

        [Fact]
        public void AddServiceWithServiceType()
        {
            services.AddTransient(typeof(Foo));
            IServiceProvider container = services.BuildServiceProvider();
            Assert.NotNull(container.GetService<Foo>());
        }

        [Fact]
        public void AddServiceWithTServiceAndTImplementation()
        {
            services.AddTransient<IFoo, Foo>();
            IServiceProvider container = services.BuildServiceProvider();
            Assert.NotNull(container.GetService<IFoo>());
        }

        [Fact]
        public void AddServiceWithTServiceOnly()
        {
            services.AddTransient<Foo>();
            IServiceProvider container = services.BuildServiceProvider();
            Assert.NotNull(container.GetService<Foo>());
        }

        [Fact]
        public void AddServiceWithTServiceFactory()
        {
            services.AddTransient(sp => new Foo());
            IServiceProvider container = services.BuildServiceProvider();
            Assert.NotNull(container.GetService<Foo>());
        }

        [Fact]
        public void AddServiceWithTServiceAndTImplementationFactory()
        {
            services.AddTransient<IFoo>(sp => new Foo());
            IServiceProvider container = services.BuildServiceProvider();
            Assert.NotNull(container.GetService<IFoo>());
        }

        private interface IFoo { }

        private class Foo : IFoo { }
    }
}
