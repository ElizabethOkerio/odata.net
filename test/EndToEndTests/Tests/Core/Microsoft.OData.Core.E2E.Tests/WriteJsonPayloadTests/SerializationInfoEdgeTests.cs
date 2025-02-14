//-----------------------------------------------------------------------------
// <copyright file="SerializationInfoEdgeTests.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      See License.txt in the project root for license information.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.E2E.TestCommon;
using Microsoft.OData.E2E.TestCommon.Common;
using Microsoft.OData.E2E.TestCommon.Common.Server.EndToEnd;
using Microsoft.OData.Edm;

namespace Microsoft.OData.Core.E2E.Tests.WriteJsonPayloadTests
{
    public class SerializationInfoEdgeTests : EndToEndTestBase<SerializationInfoEdgeTests.TestsStartup>
    {
        private readonly Uri _baseUri;
        private readonly IEdmModel _model;
        private static string NameSpacePrefix = "Microsoft.OData.E2E.TestCommon.Common.Server.EndToEnd.";

        public class TestsStartup : TestStartupBase
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                services.ConfigureControllers(typeof(MetadataController));

                services.AddControllers().AddOData(opt => opt.Count().Filter().Expand().Select().OrderBy().SetMaxTop(null)
                    .AddRouteComponents("odata", CommonEndToEndEdmModel.GetEdmModel()));
            }
        }

        public SerializationInfoEdgeTests(TestWebApplicationFactory<TestsStartup> fixture)
            : base(fixture)
        {
            _baseUri = new Uri(Client.BaseAddress, "odata/");
            _model = CommonEndToEndEdmModel.GetEdmModel();
        }

        /// <summary>
        /// Write payloads without model or serialization info.
        /// Note that ODL will succeed if writing requests or writing nometadata reponses.
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task WriteWithoutSerializationInfo(string mimeType)
        {
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            // write entry without serialization info
            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                var entry = this.CreatePersonEntryWithoutSerializationInfo();
                var expectedError = mimeType.Contains(MimeTypes.ODataParameterNoMetadata)
                    ? null
                    : Error.Format(SRResources.ODataContextUriBuilder_NavigationSourceOrTypeNameMissingForResourceOrResourceSet);
                await AssertThrowsAsync<ODataException>((async () => await odataWriter.WriteStartAsync(entry)), expectedError);
            }

            // write feed without serialization info
            responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync();
                var feed = this.CreatePersonFeed();
                var entry = this.CreatePersonEntryWithoutSerializationInfo();
                entry.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "People", NavigationSourceEntityTypeName = NameSpacePrefix + "Person" });
                var expectedError = mimeType.Contains(MimeTypes.ODataParameterNoMetadata)
                    ? null
                    : Error.Format(SRResources.ODataContextUriBuilder_NavigationSourceOrTypeNameMissingForResourceOrResourceSet);
                await AssertThrowsAsync<ODataException>(async () => await odataWriter.WriteStartAsync(feed), expectedError);
            }

            // write collection without serialization info
            responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataCollectionWriterAsync();
                var collectionStart = new ODataCollectionStart() { Name = "BackupContactInfo" };
                var expectedError = mimeType.Contains(MimeTypes.ODataParameterNoMetadata)
                    ? null
                    : Error.Format(SRResources.ODataContextUriBuilder_TypeNameMissingForTopLevelCollection);
                await AssertThrowsAsync<ODataException>(async () => await odataWriter.WriteStartAsync(collectionStart), expectedError);
            }

            // write a reference link without serialization info
            responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var link = new ODataEntityReferenceLink() { Url = new Uri(_baseUri + "Orders(-10)") };
                await messageWriter.WriteEntityReferenceLinkAsync(link);

                // No exception is expected. Simply verify the writing succeeded.
                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    Stream stream = await responseMessageWithoutModel.GetStreamAsync();
                    Assert.Contains("$metadata#$ref", await TestsHelper.ReadStreamContentAsync(stream));
                }
            }

            // write reference links without serialization info
            responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var links = new ODataEntityReferenceLinks()
                {
                    Links = new[]
                    {
                        new ODataEntityReferenceLink(){Url = new Uri(_baseUri + "Orders(-10)")}
                    },
                };

                await messageWriter.WriteEntityReferenceLinksAsync(links);

                // No exception is expected. Simply verify the writing succeeded.
                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    Stream stream = await responseMessageWithoutModel.GetStreamAsync();
                    Assert.Contains("$metadata#Collection($ref)", (await TestsHelper.ReadStreamContentAsync(stream)));
                }
            }

            // write request message containing an entry
            var requestMessageWithoutModel = new TestStreamRequestMessage(new MemoryStream(),new Uri(_baseUri + "People"), "POST");
            requestMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(requestMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                var entry = this.CreatePersonEntryWithoutSerializationInfo();
                await odataWriter.WriteStartAsync(entry);
                await odataWriter.WriteEndAsync();
                Stream stream = await requestMessageWithoutModel.GetStreamAsync();
                Assert.Contains("People(-5)\",\"PersonId\":-5,\"Name\":\"xhsdckkeqzvlnprheujeycqrglfehtdocildrequohlffazfgtvmddyqsaxrojqxrsckohrakdxlrghgmzqnyruzu\"", await TestsHelper.ReadStreamContentAsync(stream));
            }
        }

        /// <summary>
        /// Specify serialization info for both feed and entry should fail.
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task SpecifySerializationInfoForFeedAndEntry(string mimeType)
        {
            var settings = new ODataMessageWriterSettings();
            settings.ODataUri = new ODataUri() { ServiceRoot = _baseUri };

            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync();
                var feed = this.CreatePersonFeed();
                feed.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "People", NavigationSourceEntityTypeName = NameSpacePrefix + "Person" });

                var entry = new ODataResource()
                {
                    Id = new Uri(_baseUri + "People(-5)"),
                    TypeName = NameSpacePrefix + "Employee"
                };

                var personEntryP1 = new ODataProperty { Name = "PersonId", Value = -5 };
                var personEntryP2 = new ODataProperty
                {
                    Name = "Name",
                    Value = "xhsdckkeqzvlnprheujeycqrglfehtdocildrequohlffazfgtvmddyqsaxrojqxrsckohrakdxlrghgmzqnyruzu"
                };

                var personEntryP3 = new ODataProperty { Name = "ManagersPersonId", Value = -465010984 };

                entry.Properties = new[] { personEntryP1, personEntryP2, personEntryP3 };
                entry.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "Person", NavigationSourceEntityTypeName = NameSpacePrefix + "Person" });

                await odataWriter.WriteStartAsync(feed);
                await odataWriter.WriteStartAsync(entry);
                await odataWriter.WriteEndAsync();
                await odataWriter.WriteEndAsync();
                Stream stream = await responseMessageWithoutModel.GetStreamAsync();
                string result = await TestsHelper.ReadStreamContentAsync(stream);
                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    Assert.Contains(NameSpacePrefix + "Employee", result);
                    Assert.Contains("$metadata#People", result);
                }
                else
                {
                    Assert.DoesNotContain(NameSpacePrefix + "Employee", result);
                }
            }
        }

        /// <summary>
        /// Write response payload with serialization info containing wrong values
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task WriteEntryWithWrongSerializationInfo(string mimeType)
        {
            var settings = new ODataMessageWriterSettings();
            settings.ODataUri = new ODataUri() { ServiceRoot = _baseUri };

            // wrong EntitySetName for entry
            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                var entry = this.CreatePersonEntryWithoutSerializationInfo();

                entry.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "Parsen", NavigationSourceEntityTypeName = NameSpacePrefix + "Person" });
                await odataWriter.WriteStartAsync(entry);
                await odataWriter.WriteEndAsync();
                var result = await TestsHelper.ReadStreamContentAsync(responseMessageWithoutModel.GetStream());
                Assert.Contains("\"PersonId\":-5", result);
                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    // no metadata does not write odata.metadata
                    Assert.Contains("$metadata#Parsen/$entity", result);
                    Assert.Contains("People(-5)\",\"PersonId\":-5", result);
                }
                else
                {
                    Assert.DoesNotContain("People(-5)\",\"PersonId\":-5", result);
                }
            }

            // wrong EntitySetName for feed
            responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync();

                var feed = this.CreatePersonFeed();
                feed.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "Parsen", NavigationSourceEntityTypeName = NameSpacePrefix + "Person" });
                var entry = this.CreatePersonEntryWithoutSerializationInfo();
                await odataWriter.WriteStartAsync(feed);
                await odataWriter.WriteStartAsync(entry);
                await odataWriter.WriteEndAsync();
                await odataWriter.WriteEndAsync();
                var result = await TestsHelper.ReadStreamContentAsync(await responseMessageWithoutModel.GetStreamAsync());

                Assert.Contains("\"PersonId\":-5", result);

                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    // no metadata does not write odata.metadata
                    Assert.Contains("$metadata#Parsen\"", result);
                    Assert.Contains("People(-5)\",\"PersonId\":-5", result);
                }
                else
                {
                    Assert.DoesNotContain("People(-5)\",\"PersonId\":-5", result);
                }
            }

            // wrong complex collection type name
            responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);

            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync();
                var complexCollection = new ODataResourceSetWrapper()
                {
                    ResourceSet = new ODataResourceSet(),
                    Resources = new List<ODataResourceWrapper>()
                        {
                            TestsHelper.CreatePrimaryContactODataWrapper()
                        }
                };

                complexCollection.ResourceSet.SetSerializationInfo(new ODataResourceSerializationInfo()
                {
                    ExpectedTypeName = NameSpacePrefix + "ContactDETAIL"
                });

                await ODataWriterHelper.WriteResourceSetAsync(odataWriter, complexCollection);
                var result = await TestsHelper.ReadStreamContentAsync(responseMessageWithoutModel.GetStream());

                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    // [{"@odata.type":"#Microsoft.Test.OData.Services.AstoriaDefaultService.ContactDetails","EmailBag@odata.type":"#Collection(String)"...
                    Assert.Contains("\"value\":[{\"@odata.type\":\"#Microsoft.OData.E2E.TestCommon.Common.Server.EndToEnd.ContactDetails\",\"EmailBag", result);

                    // no metadata does not write odata.metadata
                    Assert.Contains("$metadata#Collection(" + NameSpacePrefix + "ContactDETAIL)", result);
                }
                else
                {
                    Assert.DoesNotContain("\"@odata.type\":\"#Microsoft.OData.E2E.TestCommon.Common.Server.EndToEnd.ContactDetails\"", result);
                }
            }
        }

        /// <summary>
        /// Provide EDM model, and then write an entry with wrong serialization info
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task WriteEntryWithWrongSerializationInfoWithModel(string mimeType)
        {
            bool[] autoComputeMetadataBools = new bool[] { true, false, };

            foreach (var autoComputeMetadata in autoComputeMetadataBools)
            {
                var settings = new ODataMessageWriterSettings();
                settings.ODataUri = new ODataUri() { ServiceRoot = _baseUri };

                var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
                responseMessageWithoutModel.SetHeader("Content-Type", mimeType);

                var personType = _model.FindDeclaredType(NameSpacePrefix + "Person") as IEdmEntityType;
                var peopleSet = _model.EntityContainer.FindEntitySet("People");

                using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings, _model))
                {
                    var odataWriter = await messageWriter.CreateODataResourceWriterAsync(peopleSet, personType);
                    var entry = this.CreatePersonEntryWithoutSerializationInfo();

                    entry.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "Parsen", NavigationSourceEntityTypeName = NameSpacePrefix + "Person" });
                    await odataWriter.WriteStartAsync(entry);
                    await odataWriter.WriteEndAsync();
                    var result = await TestsHelper.ReadStreamContentAsync(responseMessageWithoutModel.GetStream());
                    Assert.Contains("\"PersonId\":-5", result);
                    if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                    {
                        // no metadata does not write odata.metadata
                        Assert.Contains("$metadata#Parsen/$entity", result);
                    }
                }
            }
        }

        /// <summary>
        /// Change serialization info value after WriteStart
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task ChangeSerializationInfoAfterWriteStart(string mimeType)
        {
            var settings = new ODataMessageWriterSettings();
            settings.ODataUri = new ODataUri() { ServiceRoot = _baseUri };

            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                var entry = this.CreatePersonEntryWithoutSerializationInfo();

                entry.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "People", NavigationSourceEntityTypeName = NameSpacePrefix + "Person" });
                await odataWriter.WriteStartAsync(entry);
                entry.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "Parsen", NavigationSourceEntityTypeName = NameSpacePrefix + "Person" });
                await odataWriter.WriteEndAsync();

                Stream stream = await responseMessageWithoutModel.GetStreamAsync();

                // nometadata option does not write odata.metadata in the payload
                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    Assert.Contains("People/$entity", (await TestsHelper.ReadStreamContentAsync(stream)));
                }
            }
        }

        /// <summary>
        /// Do not provide model, but hand craft IEdmEntitySet and IEdmEntityType for writer
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task HandCraftEdmType(string mimeType)
        {
            EdmModel edmModel = new EdmModel();

            EdmEntityType edmEntityType = new EdmEntityType(NameSpacePrefix, "Person");
            edmModel.AddElement(edmEntityType);
            var keyProperty = new EdmStructuralProperty(edmEntityType, "PersonId", EdmCoreModel.Instance.GetInt32(false));
            edmEntityType.AddKeys(new IEdmStructuralProperty[] { keyProperty });
            edmEntityType.AddProperty(keyProperty);
            var property = new EdmStructuralProperty(edmEntityType, "Name", EdmCoreModel.Instance.GetString(true));
            edmEntityType.AddKeys(new IEdmStructuralProperty[] { property });
            edmEntityType.AddProperty(property);

            var defaultContainer = new EdmEntityContainer(NameSpacePrefix, "DefaultContainer");
            edmModel.AddElement(defaultContainer);

            EdmEntitySet entitySet = new EdmEntitySet(defaultContainer, "People", edmEntityType);
            defaultContainer.AddElement(entitySet);

            var settings = new ODataMessageWriterSettings();
            settings.ODataUri = new ODataUri() { ServiceRoot = _baseUri };
            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync(entitySet, edmEntityType);

                var entry = this.CreatePersonEntryWithoutSerializationInfo();
                await odataWriter.WriteStartAsync(entry);
                await odataWriter.WriteEndAsync();

                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    Assert.Contains("People/$entity", await TestsHelper.ReadStreamContentAsync(await responseMessageWithoutModel.GetStreamAsync()));
                }
            }
        }

        private ODataResource CreatePersonEntryWithoutSerializationInfo()
        {
            var personEntry = new ODataResource()
            {
                Id = new Uri(_baseUri + "People(-5)"),
                TypeName = NameSpacePrefix + "Person"
            };

            var personEntryP1 = new ODataProperty { Name = "PersonId", Value = -5 };
            var personEntryP2 = new ODataProperty
            {
                Name = "Name",
                Value = "xhsdckkeqzvlnprheujeycqrglfehtdocildrequohlffazfgtvmddyqsaxrojqxrsckohrakdxlrghgmzqnyruzu"
            };

            personEntry.Properties = new[] { personEntryP1, personEntryP2 };
            personEntry.EditLink = new Uri(_baseUri + "People(-5)");
            return personEntry;
        }

        private ODataResourceSet CreatePersonFeed()
        {
            var orderFeed = new ODataResourceSet()
            {
                Id = new Uri(_baseUri + "People"),
            };

            return orderFeed;
        }

        private static async Task AssertThrowsAsync<T>(Func<Task> action, string expectedError) where T : Exception
        {
            try
            {
                await action();
                // Expected exception not thrown
                Assert.Null(expectedError);
            }
            catch (T e)
            {
                // Unexpected exception " + e.Message
                Assert.NotNull(expectedError);
                Assert.Equal(expectedError, e.Message);
            }
        }

        private WritePayloadTestsHelper TestsHelper
        {
            get
            {
                return new WritePayloadTestsHelper(_baseUri, _model);
            }
        }
    }
}
