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
        private IServiceCollection Services;
        public ContainerBuilderExtensionsTests()
        {
            Services = new ServiceCollection();
        }
        [Fact]
        public void AddServiceWithServiceType()
        {
            Services.AddTransient(typeof(Foo));
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.NotNull(container.GetService<Foo>());
        }

        [Fact]
        public void AddServiceWithTServiceAndTImplementation()
        {
            Services.AddTransient<IFoo, Foo>();
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.NotNull(container.GetService<IFoo>());
        }

        [Fact]
        public void AddServiceWithTServiceOnly()
        {
            Services.AddTransient<Foo>();
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.NotNull(container.GetService<Foo>());
        }

        [Fact]
        public void AddServiceWithTServiceFactory()
        {
            Services.AddTransient(sp => new Foo());
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.NotNull(container.GetService<Foo>());
        }

        [Fact]
        public void AddServiceWithTServiceAndTImplementationFactory()
        {
            Services.AddTransient<IFoo>(sp => new Foo());
            IServiceProvider container = Services.BuildServiceProvider();
            Assert.NotNull(container.GetService<IFoo>());
        }

        private interface IFoo { }

        private class Foo : IFoo { }
    }
}
