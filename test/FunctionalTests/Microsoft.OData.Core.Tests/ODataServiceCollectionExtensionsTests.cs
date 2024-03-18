//---------------------------------------------------------------------
// <copyright file="BufferUtilsTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Json;
using Microsoft.OData.UriParser;
using Xunit;

namespace Microsoft.OData.Tests
{

    /// <summary>
    /// Tests methods related to registering OData services in Dependency Injection containers.
    /// </summary>
    public class ODataServiceCollectionExtensionsTests
    {

        [Fact]
        public void AddDefaultODataServices_RegistersServicesCorrectly()
        {
            var services = new ServiceCollection();
            Assert.Empty(services);

            services.AddDefaultODataServices();
            Assert.True(services.Count == 11);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider);

            // @robertmclaws: Test for registered Singletons.
            Assert.NotNull(provider.GetService<IJsonReaderFactory>());
            Assert.NotNull(provider.GetService<IJsonWriterFactory>());
            Assert.NotNull(provider.GetService<ODataMediaTypeResolver>());
            Assert.NotNull(provider.GetService<ODataPayloadValueConverter>());
            Assert.NotNull(provider.GetService<IEdmModel>());
            Assert.NotNull(provider.GetService<ODataUriResolver>());

            // @robertmclaws: Test for request-scoped services.
            var scope = provider.CreateScope();
            Assert.NotNull(scope);
            Assert.NotNull(scope.ServiceProvider);

            Assert.NotNull(scope.ServiceProvider.GetService<ODataMessageInfo>());
            Assert.NotNull(scope.ServiceProvider.GetService<UriPathParser>());
            Assert.NotNull(scope.ServiceProvider.GetService<ODataMessageReaderSettings>());
            Assert.NotNull(scope.ServiceProvider.GetService<ODataMessageWriterSettings>());
            Assert.NotNull(scope.ServiceProvider.GetService<ODataUriParserSettings>());
        }

        // [Fact]
        // public void RentFromBufferShouldThrowsIfNullBufferReturns()
        // {
        //     // Arrange
        //     Action test = () => BufferUtils.RentFromBuffer(new BadCharArrayPool(), 1024);

        //     // Act & Assert
        //     var exception = Assert.Throws<ODataException>(test);
        //     Assert.Equal(Strings.BufferUtils_InvalidBufferOrSize(1024), exception.Message);
        // }

    }
}
