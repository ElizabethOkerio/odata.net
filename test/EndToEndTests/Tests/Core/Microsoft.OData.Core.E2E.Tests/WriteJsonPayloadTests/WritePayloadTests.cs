//-----------------------------------------------------------------------------
// <copyright file="WritePayloadTests.cs" company=".NET Foundation">
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
using System.Text.RegularExpressions;

namespace Microsoft.OData.Core.E2E.Tests.WriteJsonPayloadTests
{
    public class WritePayloadTests : EndToEndTestBase<WritePayloadTests.TestsStartup>
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

        public WritePayloadTests(TestWebApplicationFactory<TestsStartup> fixture)
            : base(fixture)
        {
            _baseUri = new Uri(Client.BaseAddress, "odata/");
            _model = CommonEndToEndEdmModel.GetEdmModel();
        }

        [Fact]
        public async Task WritingAnODataFeed_WithFullMetadata_ShouldMatchExpectedPayload()
        {
            var mimeType = MimeTypeODataParameterFullMetadata;
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithModel;
            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", mimeType);

            var orderType = _model.FindDeclaredType(NameSpacePrefix + "Order") as IEdmEntityType;
            var orderSet = _model.EntityContainer.FindEntitySet("Orders");

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync(orderSet, orderType);
                outputWithModel = await WriteAndVerifyOrderFeedAsync(responseMessageWithModel, odataWriter, true, mimeType);
            }

            string outputWithoutModel;
            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);

            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync();
                outputWithoutModel = await WriteAndVerifyOrderFeedAsync(responseMessageWithoutModel, odataWriter, false, mimeType);
            }

            var rex = new Regex("\"\\w*@odata.associationLink\":\"[^\"]*\",");
            var outputWithModel2 = rex.Replace(outputWithModel, "");
            var outputWithoutModel2 = rex.Replace(outputWithoutModel, "");

            Assert.Equal(outputWithModel2, outputWithoutModel2);
        }

        [Fact]
        public async Task WritingAnODataFeed_WithMinimalMetadata_ShouldMatchExpectedPayload()
        {
            var mimeType = MimeTypeODataParameterMinimalMetadata;
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithModel;
            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", mimeType);

            var orderType = _model.FindDeclaredType(NameSpacePrefix + "Order") as IEdmEntityType;
            var orderSet = _model.EntityContainer.FindEntitySet("Orders");

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync(orderSet, orderType);
                outputWithModel = await WriteAndVerifyOrderFeedAsync(responseMessageWithModel, odataWriter, true, mimeType);
            }

            string outputWithoutModel;
            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);

            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync();
                outputWithoutModel = await WriteAndVerifyOrderFeedAsync(responseMessageWithoutModel, odataWriter, false, mimeType);
            }

            var rex = new Regex("\"\\w*@odata.type\":\"#[\\w\\(\\)\\.]*\",");
            var outputWithoutModel2 = rex.Replace(outputWithoutModel, "");

            Assert.Equal(outputWithModel, outputWithoutModel2);
        }

        [Fact]
        public async Task WritingAnODataFeed_WithNoMetadata_ShouldMatchExpectedPayload()
        {
            var mimeType = MimeTypeODataParameterNoMetadata;
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithModel;
            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", mimeType);

            var orderType = _model.FindDeclaredType(NameSpacePrefix + "Order") as IEdmEntityType;
            var orderSet = _model.EntityContainer.FindEntitySet("Orders");

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync(orderSet, orderType);
                outputWithModel = await WriteAndVerifyOrderFeedAsync(responseMessageWithModel, odataWriter, true, mimeType);
            }

            string outputWithoutModel;
            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);

            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync();
                outputWithoutModel = await WriteAndVerifyOrderFeedAsync(responseMessageWithoutModel, odataWriter, false, mimeType);
            }

            Assert.Equal(outputWithModel, outputWithoutModel);
        }

        [Fact]
        public async Task WritingAnExpandedEntry_WithFullMetadata_ShouldMatchExpectedPayload()
        {
            var mimeType = MimeTypeODataParameterFullMetadata;
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithModel = null;
            string outputWithoutModel = null;

            // Without Model
            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                outputWithoutModel = await WriteAndVerifyExpandedCustomerEntryAsync(responseMessageWithoutModel, odataWriter, false, mimeType);
            }

            // With Model
            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", mimeType);

            var customerType = _model.FindDeclaredType(NameSpacePrefix + "Customer") as IEdmEntityType;
            var customerSet = _model.EntityContainer.FindEntitySet("Customers");

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync(customerSet, customerType);
                outputWithModel = await WriteAndVerifyExpandedCustomerEntryAsync(responseMessageWithModel, odataWriter, false, mimeType);
            }

            var rex = new Regex("\"\\w*@odata.associationLink\":\"[^\"]*\",");
            var outputWithModel2 = rex.Replace(outputWithModel, "");
            var outputWithoutModel2 = rex.Replace(outputWithoutModel, "");

            Assert.Equal(outputWithModel2, outputWithoutModel2);
        }

        [Fact]
        public async Task WritingAnExpandedEntry_WithMinimalMetadata_ShouldMatchExpectedPayload()
        {
            var mimeType = MimeTypeODataParameterMinimalMetadata;
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithModel = null;
            string outputWithoutModel = null;

            // Without Model
            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                outputWithoutModel = await WriteAndVerifyExpandedCustomerEntryAsync(responseMessageWithoutModel, odataWriter, false, mimeType);
            }

            // With Model
            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", mimeType);

            var customerType = _model.FindDeclaredType(NameSpacePrefix + "Customer") as IEdmEntityType;
            var customerSet = _model.EntityContainer.FindEntitySet("Customers");

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync(customerSet, customerType);
                outputWithModel = await WriteAndVerifyExpandedCustomerEntryAsync(responseMessageWithModel, odataWriter, false, mimeType);
            }

            var rex = new Regex("\"\\w*@odata.type\":\"#[\\w\\(\\)\\.]*\",");
            var outputWithoutModel2 = rex.Replace(outputWithoutModel, "");

            Assert.Equal(outputWithModel, outputWithoutModel2);
        }

        [Fact]
        public async Task WritingAnExpandedEntryTest_WithNoMetadata_ShouldMatchExpectedPayload()
        {
            var mimeType = MimeTypeODataParameterNoMetadata;
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithModel = null;
            string outputWithoutModel = null;

            // Without Model
            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                outputWithoutModel = await WriteAndVerifyExpandedCustomerEntryAsync(responseMessageWithoutModel, odataWriter, false, mimeType);
            }

            // With Model
            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", mimeType);

            var customerType = _model.FindDeclaredType(NameSpacePrefix + "Customer") as IEdmEntityType;
            var customerSet = _model.EntityContainer.FindEntitySet("Customers");

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync(customerSet, customerType);
                outputWithModel = await WriteAndVerifyExpandedCustomerEntryAsync(responseMessageWithModel, odataWriter, false, mimeType);
            }

            // No Metadata with/out model should result in same output
            Assert.Equal(outputWithModel, outputWithoutModel);
        }

        [Fact]
        public async Task WritingAFeedContainingActionsAndDerivedTypes_WithFullMetadata_ShouldMatchExpectedPayload()
        {
            var mimeType = MimeTypeODataParameterFullMetadata;
            var settings = new ODataMessageWriterSettings
            {
                BaseUri = _baseUri,
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithModel = null;
            string outputWithoutModel = null;

            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", mimeType);

            var personType = _model.FindDeclaredType(NameSpacePrefix + "Person") as IEdmEntityType;
            var personSet = _model.EntityContainer.FindEntitySet("People");

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync(personSet, personType);
                outputWithModel = await WriteAndVerifyPersonFeedAsync(responseMessageWithModel, odataWriter, false, mimeType);
            }

            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync();
                outputWithoutModel = await WriteAndVerifyPersonFeedAsync(responseMessageWithoutModel, odataWriter, false, mimeType);
            }

            Assert.Equal(outputWithModel, outputWithoutModel);
            Assert.Contains(_baseUri + "$metadata#People\"", outputWithoutModel);
        }

        [Fact]
        public async Task WritingAFeedContainingActionsAndDerivedTypes_WithMinimalMetadata_ShouldMatchExpectedPayload()
        {
            var mimeType = MimeTypeODataParameterMinimalMetadata;
            var settings = new ODataMessageWriterSettings
            {
                BaseUri = _baseUri,
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithModel = null;
            string outputWithoutModel = null;

            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", mimeType);

            var personType = _model.FindDeclaredType(NameSpacePrefix + "Person") as IEdmEntityType;
            var personSet = _model.EntityContainer.FindEntitySet("People");

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync(personSet, personType);
                outputWithModel = await WriteAndVerifyPersonFeedAsync(responseMessageWithModel, odataWriter, false, mimeType);
            }

            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync();
                outputWithoutModel = await WriteAndVerifyPersonFeedAsync(responseMessageWithoutModel, odataWriter, false, mimeType);
            }

            Assert.Equal(outputWithModel, outputWithoutModel);
            Assert.Contains(_baseUri + "$metadata#People\"", outputWithoutModel);
            Assert.False(outputWithoutModel.Contains("{\"@odata.type\":\"" + "#" + NameSpacePrefix + "Person\","), "odata.type Person");
            Assert.True(outputWithoutModel.Contains("{\"@odata.type\":\"" + "#" + NameSpacePrefix + "Employee\","), "odata.type Employee");
            Assert.True(outputWithoutModel.Contains("{\"@odata.type\":\"" + "#" + NameSpacePrefix + "SpecialEmployee\","), "odata.type SpecialEmployee");
        }

        [Fact]
        public async Task WritingAFeedContainingActionsAndDerivedTypes_WithNoMetadata_ShouldMatchExpectedPayload()
        {
            var mimeType = MimeTypeODataParameterNoMetadata;
            var settings = new ODataMessageWriterSettings
            {
                BaseUri = _baseUri,
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithModel = null;
            string outputWithoutModel = null;

            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", mimeType);

            var personType = _model.FindDeclaredType(NameSpacePrefix + "Person") as IEdmEntityType;
            var personSet = _model.EntityContainer.FindEntitySet("People");

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync(personSet, personType);
                outputWithModel = await WriteAndVerifyPersonFeedAsync(responseMessageWithModel, odataWriter, false, mimeType);
            }

            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync();
                outputWithoutModel = await WriteAndVerifyPersonFeedAsync(responseMessageWithoutModel, odataWriter, false, mimeType);
            }

            Assert.Equal(outputWithModel, outputWithoutModel);
        }

        [Fact]
        public async Task WritingAnEntryWithOrWithoutTypeCast_WithFullMetadata_ShouldMatchExpectedMetadata()
        {
            var mimeType = MimeTypeODataParameterFullMetadata;
            var settings = new ODataMessageWriterSettings
            {
                BaseUri = _baseUri,
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithTypeCast = null;
            string outputWithoutTypeCast = null;

            // employee entry as response of person(1)
            var responseMessageWithoutTypeCast = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutTypeCast.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutTypeCast, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                outputWithoutTypeCast = await WriteAndVerifyEmployeeEntryAsync(
                    responseMessageWithoutTypeCast,
                    odataWriter,
                    false,
                    mimeType);
            }

            // employee entry as response of person(1)/EmployeeTyeName, in this case the test sets ExpectedTypeName as Employee in Serialization info
            var responseMessageWithTypeCast = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithTypeCast.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithTypeCast, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                outputWithTypeCast = await WriteAndVerifyEmployeeEntryAsync(
                    responseMessageWithTypeCast,
                    odataWriter,
                    true,
                    mimeType);
            }

            // expect type cast in odata.metadata if EntitySetElementTypeName != ExpectedTypeName
            Assert.Contains(_baseUri + "$metadata#People/$entity", outputWithoutTypeCast);
            Assert.Contains(_baseUri + "$metadata#People/" + NameSpacePrefix + "Employee/$entity", outputWithTypeCast);
        }

        [Fact]
        public async Task WritingAnEntryWithOrWithoutTypeCast_WithMinimalMetadata_ShouldMatchExpectedMetadata()
        {
            var mimeType = MimeTypeODataParameterMinimalMetadata;
            var settings = new ODataMessageWriterSettings
            {
                BaseUri = _baseUri,
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            string outputWithTypeCast = null;
            string outputWithoutTypeCast = null;

            // employee entry as response of person(1)
            var responseMessageWithoutTypeCast = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutTypeCast.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutTypeCast, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                outputWithoutTypeCast = await WriteAndVerifyEmployeeEntryAsync(
                    responseMessageWithoutTypeCast,
                    odataWriter,
                    false,
                    mimeType);
            }

            // employee entry as response of person(1)/EmployeeTyeName, in this case the test sets ExpectedTypeName as Employee in Serialization info
            var responseMessageWithTypeCast = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithTypeCast.SetHeader("Content-Type", mimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithTypeCast, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                outputWithTypeCast = await WriteAndVerifyEmployeeEntryAsync(
                    responseMessageWithTypeCast,
                    odataWriter,
                    true,
                    mimeType);
            }

            // expect type cast in odata.metadata if EntitySetElementTypeName != ExpectedTypeName
            Assert.Contains(_baseUri + "$metadata#People/$entity", outputWithoutTypeCast);
            Assert.Contains(_baseUri + "$metadata#People/" + NameSpacePrefix + "Employee/$entity", outputWithTypeCast);
            Assert.Contains("odata.type", outputWithoutTypeCast);
            Assert.DoesNotContain("odata.type", outputWithTypeCast);
        }

        /// <summary>
        /// Write an entry containing stream, named stream
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task WritingAnEntryContainingAStream_ShouldMatchExpectedPayload(string mimeType)
        {
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };
            string outputWithModel = null;
            string outputWithoutModel = null;

            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", mimeType);
            responseMessageWithModel.PreferenceAppliedHeader().AnnotationFilter = "*";

            var carType = _model.FindDeclaredType(NameSpacePrefix + "Car") as IEdmEntityType;
            var carSet = _model.EntityContainer.FindEntitySet("Cars");

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync(carSet, carType);
                outputWithModel = await WriteAndVerifyCarEntryAsync(responseMessageWithModel, odataWriter, true, mimeType);
            }

            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", mimeType);
            responseMessageWithoutModel.PreferenceAppliedHeader().AnnotationFilter = "*";
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                outputWithoutModel = await WriteAndVerifyCarEntryAsync(responseMessageWithoutModel, odataWriter, false, mimeType);
            }

            Assert.Equal(outputWithModel, outputWithoutModel);
        }

        /// <summary>
        /// Write complex collection response
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task WritingAFeedWithComplexCollections_ShouldMatchExpectedPayload(string mimeType)
        {
            string testMimeType = mimeType.Contains("xml") ? MimeTypes.ApplicationXml : mimeType;

            var settings = new ODataMessageWriterSettings
            {
                BaseUri = _baseUri,
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };
            string outputWithModel = null;
            string outputWithoutModel = null;

            var contactDetailType = _model.FindDeclaredType(NameSpacePrefix + "ContactDetails") as IEdmComplexType;

            var responseMessageWithModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithModel.SetHeader("Content-Type", testMimeType);

            using (var messageWriter = new ODataMessageWriter(responseMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync(null, contactDetailType);
                outputWithModel = await this.WriteAndVerifyCollectionAsync(
                    responseMessageWithModel,
                    odataWriter,
                    true,
                    testMimeType);
            }

            var responseMessageWithoutModel = new TestStreamResponseMessage(new MemoryStream());
            responseMessageWithoutModel.SetHeader("Content-Type", testMimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceSetWriterAsync(null, contactDetailType);
                outputWithoutModel = await this.WriteAndVerifyCollectionAsync(
                    responseMessageWithoutModel,
                    odataWriter,
                    false,
                    testMimeType);
            }

            Assert.Equal(outputWithModel, outputWithoutModel);
        }

        /// <summary>
        /// Write $ref response
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task LinksTest(string mimeType)
        {
            string testMimeType = mimeType;
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            var responseMessage = new TestStreamResponseMessage(new MemoryStream());
            responseMessage.SetHeader("Content-Type", testMimeType);
            using (var messageWriter = new ODataMessageWriter(responseMessage, settings, _model))
            {
                await this.WriteAndVerifyLinksAsync(responseMessage, messageWriter, testMimeType);
            }
        }

        /// <summary>
        /// Write $ref response with a single link
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task SingleLinkTest(string mimeType)
        {
            string testMimeType = mimeType.Contains("xml") ? MimeTypes.ApplicationXml : mimeType;

            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri },
                EnableMessageStreamDisposal = false
            };

            var responseMessage = new TestStreamResponseMessage(new MemoryStream());
            responseMessage.SetHeader("Content-Type", testMimeType);

            using var messageWriter = new ODataMessageWriter(responseMessage, settings, _model);
            await this.WriteAndVerifySingleLinkAsync(responseMessage, messageWriter, testMimeType);
        }

        /// <summary>
        /// Write a request message with an entry
        /// </summary>
        [Theory]
        [InlineData(MimeTypeODataParameterFullMetadata)]
        [InlineData(MimeTypeODataParameterMinimalMetadata)]
        [InlineData(MimeTypeODataParameterNoMetadata)]
        public async Task RequestMessageTest(string mimeType)
        {
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri() { ServiceRoot = _baseUri},
                EnableMessageStreamDisposal = false
            };

            string outputWithModel = null;
            string outputWithoutModel = null;

            var orderType = _model.FindDeclaredType(NameSpacePrefix + "Order") as IEdmEntityType;
            var orderSet = _model.EntityContainer.FindEntitySet("Orders");

            var requestMessageWithModel = new TestStreamRequestMessage(
                new MemoryStream(),
                new Uri(_baseUri + "Orders"), "POST");
            requestMessageWithModel.SetHeader("Content-Type", mimeType);

            using (var messageWriter = new ODataMessageWriter(requestMessageWithModel, settings, _model))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync(orderSet, orderType);
                outputWithModel = await this.WriteAndVerifyRequestMessageAsync(
                    requestMessageWithModel,
                    odataWriter,
                    true,
                    mimeType);
            }

            var requestMessageWithoutModel = new TestStreamRequestMessage(
                new MemoryStream(),
                new Uri(_baseUri + "Orders"), "POST");
            requestMessageWithoutModel.SetHeader("Content-Type", mimeType);

            using (var messageWriter = new ODataMessageWriter(requestMessageWithoutModel, settings))
            {
                var odataWriter = await messageWriter.CreateODataResourceWriterAsync();
                outputWithoutModel = await this.WriteAndVerifyRequestMessageAsync(
                    requestMessageWithoutModel,
                    odataWriter, false, mimeType);
            }

            Assert.Equal(outputWithModel, outputWithoutModel);
        }

        private async Task<string> WriteAndVerifyOrderFeedAsync(
            TestStreamResponseMessage responseMessage,
            ODataWriter odataWriter,
            bool hasModel,
            string mimeType)
        {
            var orderFeed = new ODataResourceSet()
            {
                Id = new Uri(_baseUri + "Orders"),
                NextPageLink = new Uri(_baseUri + "Orders?$skiptoken=-9"),
            };

            if (!hasModel)
            {
                orderFeed.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "Orders", NavigationSourceEntityTypeName = NameSpacePrefix + "Order" });
            }

            await odataWriter.WriteStartAsync(orderFeed);

            var orderEntry1 = TestsHelper.CreateOrderEntry1(hasModel);
            await odataWriter.WriteStartAsync(orderEntry1);

            var orderEntry1Navigation1 = new ODataNestedResourceInfo()
            {
                Name = "Customer",
                IsCollection = false,
                Url = new Uri(_baseUri + "Orders(-10)/Customer")
            };

            await odataWriter.WriteStartAsync(orderEntry1Navigation1);
            await odataWriter.WriteEndAsync();

            var orderEntry1Navigation2 = new ODataNestedResourceInfo()
            {
                Name = "Login",
                IsCollection = false,
                Url = new Uri(_baseUri + "Orders(-10)/Login")
            };

            await odataWriter.WriteStartAsync(orderEntry1Navigation2);
            await odataWriter.WriteEndAsync();

            // Finish writing orderEntry1.
            await odataWriter.WriteEndAsync();

            var orderEntry2Wrapper = TestsHelper.CreateOrderEntry2(hasModel);


            var orderEntry2Navigation1 = new ODataNestedResourceInfo()
            {
                Name = "Customer",
                IsCollection = false,
                Url = new Uri(_baseUri + "Orders(-9)/Customer")
            };

            var orderEntry2Navigation2 = new ODataNestedResourceInfo()
            {
                Name = "Login",
                IsCollection = false,
                Url = new Uri(_baseUri + "Orders(-9)/Login")
            };

            orderEntry2Wrapper.NestedResourceInfoWrappers = orderEntry2Wrapper.NestedResourceInfoWrappers.Concat(
                new[]
                {
                    new ODataNestedResourceInfoWrapper() { NestedResourceInfo = orderEntry1Navigation1 },
                    new ODataNestedResourceInfoWrapper() { NestedResourceInfo = orderEntry2Navigation2 }
                });

            await ODataWriterHelper.WriteResourceAsync(odataWriter, orderEntry2Wrapper);

            // Finish writing the feed.
            await odataWriter.WriteEndAsync();

            // Some very basic verification for the payload.
            bool verifyFeedCalled = false;
            bool verifyEntryCalled = false;
            bool verifyNavigationCalled = false;
            Action<ODataResourceSet> verifyFeed = (feed) =>
            {
                Assert.NotNull(feed.NextPageLink);
                verifyFeedCalled = true;
            };

            Action<ODataResource> verifyEntry = (entry) =>
            {
                if (entry.TypeName.Contains("Order"))
                {
                    //entry.Properties.Count
                    Assert.Equal(2, entry.Properties.Count());
                }
                else
                {
                    Assert.True(entry.TypeName.Contains("ConcurrencyInfo"), "complex Property Concurrency should be read into ODataResource");
                }
                verifyEntryCalled = true;
            };

            Action<ODataNestedResourceInfo> verifyNavigation = (navigation) =>
            {
                Assert.True(navigation.Name == "Customer" || navigation.Name == "Login" || navigation.Name == "Concurrency", "navigation.Name");
                verifyNavigationCalled = true;
            };

            var orderType = _model.FindDeclaredType(NameSpacePrefix + "Order") as IEdmEntityType;
            var orderSet = _model.EntityContainer.FindEntitySet("Orders");

            Stream stream = await responseMessage.GetStreamAsync();

            if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
            {
                stream.Seek(0, SeekOrigin.Begin);
                await TestsHelper.ReadAndVerifyFeedEntryMessageAsync(true, responseMessage, orderSet, orderType, verifyFeed, verifyEntry, verifyNavigation);
                Assert.True(verifyFeedCalled && verifyEntryCalled && verifyNavigationCalled, "Verification action not called.");
            }

            return await TestsHelper.ReadStreamContentAsync(stream);
        }

        private async Task<string> WriteAndVerifyExpandedCustomerEntryAsync(
            TestStreamResponseMessage responseMessage,
            ODataWriter odataWriter,
            bool hasModel,
            string mimeType)
        {
            ODataResourceWrapper customerEntry = TestsHelper.CreateCustomerEntry(hasModel);

            var loginFeed = new ODataResourceSet() { Id = new Uri(_baseUri + "Customers(-9)/Logins") };
            if (!hasModel)
            {
                loginFeed.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "Logins", NavigationSourceEntityTypeName = NameSpacePrefix + "Login", NavigationSourceKind = EdmNavigationSourceKind.EntitySet });
            }

            var loginEntry = TestsHelper.CreateLoginEntry(hasModel);


            customerEntry.NestedResourceInfoWrappers = customerEntry.NestedResourceInfoWrappers.Concat(TestsHelper.CreateCustomerNavigationLinks());
            customerEntry.NestedResourceInfoWrappers = customerEntry.NestedResourceInfoWrappers.Concat(new[]{  new ODataNestedResourceInfoWrapper()
            {
                NestedResourceInfo = new ODataNestedResourceInfo()
                {
                    Name = "Logins",
                    IsCollection = true,
                    Url = new Uri(_baseUri + "Customers(-9)/Logins")
                },
                NestedResourceOrResourceSet = new ODataResourceSetWrapper()
                {
                    ResourceSet = loginFeed,
                    Resources = new List<ODataResourceWrapper>()
                    {
                        new ODataResourceWrapper()
                        {
                            Resource = loginEntry,
                            NestedResourceInfoWrappers = TestsHelper.CreateLoginNavigationLinksWrapper().ToList()
                        }
                    }
                }
            }});

            await ODataWriterHelper.WriteResourceAsync(odataWriter, customerEntry);

            // Some very basic verification for the payload.
            bool verifyFeedCalled = false;
            int verifyEntryCalled = 0;
            bool verifyNavigationCalled = false;
            Action<ODataResourceSet> verifyFeed = (feed) =>
            {
                verifyFeedCalled = true;
            };

            Action<ODataResource> verifyEntry = (entry) =>
            {
                if (entry.TypeName.Contains("Customer"))
                {
                    Assert.Equal(4, entry.Properties.Count());
                    verifyEntryCalled++;
                }

                if (entry.TypeName.Contains("Login"))
                {
                    Assert.Equal(2, entry.Properties.Count());
                    verifyEntryCalled++;
                }
            };

            Action<ODataNestedResourceInfo> verifyNavigation = (navigation) =>
            {
                Assert.NotNull(navigation.Name);
                verifyNavigationCalled = true;
            };

            var customerType = _model.FindDeclaredType(NameSpacePrefix + "Customer") as IEdmEntityType;
            var customerSet = _model.EntityContainer.FindEntitySet("Customers");

            Stream stream = await responseMessage.GetStreamAsync();

            if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
            {
                stream.Seek(0, SeekOrigin.Begin);
                await TestsHelper.ReadAndVerifyFeedEntryMessageAsync(
                    false,
                    responseMessage,
                    customerSet,
                    customerType,
                    verifyFeed,
                    verifyEntry,
                    verifyNavigation);
                Assert.True(verifyFeedCalled && verifyEntryCalled == 2 && verifyNavigationCalled, "Verification action not called.");
            }

            return await TestsHelper.ReadStreamContentAsync(stream);
        }

        private async Task<string> WriteAndVerifyCarEntryAsync(
            TestStreamResponseMessage responseMessage,
            ODataWriter odataWriter,
            bool hasModel,
            string mimeType)
        {
            var carEntry = TestsHelper.CreateCarEntry(hasModel);

            await odataWriter.WriteStartAsync(carEntry);

            // Finish writing the entry.
            await odataWriter.WriteEndAsync();

            // Some very basic verification for the payload.
            bool verifyEntryCalled = false;
            Action<ODataResource> verifyEntry = (entry) =>
            {
                Assert.Equal(4, entry.Properties.Count());
                Assert.NotNull(entry.MediaResource);
                Assert.True(entry.EditLink.AbsoluteUri.Contains("Cars(11)"), "entry.EditLink");
                Assert.True(entry.ReadLink == null || entry.ReadLink.AbsoluteUri.Contains("Cars(11)"), "entry.ReadLink");
                Assert.Single(entry.InstanceAnnotations);

                verifyEntryCalled = true;
            };

            Stream stream = await responseMessage.GetStreamAsync();
            var carType = _model.FindDeclaredType(NameSpacePrefix + "Car") as IEdmEntityType;
            var carSet = _model.EntityContainer.FindEntitySet("Cars");

            if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
            {
                stream.Seek(0, SeekOrigin.Begin);
                await TestsHelper.ReadAndVerifyFeedEntryMessageAsync(false, responseMessage, carSet, carType, null, verifyEntry, null);
                Assert.True(verifyEntryCalled, "Verification action not called.");
            }

            return await TestsHelper.ReadStreamContentAsync(stream);
        }

        private async Task<string> WriteAndVerifyPersonFeedAsync(
            TestStreamResponseMessage responseMessage,
            ODataWriter odataWriter,
            bool hasModel,
            string mimeType)
        {
            var personFeed = new ODataResourceSet()
            {
                Id = new Uri(_baseUri + "People"),
                DeltaLink = new Uri(_baseUri + "People")
            };
            if (!hasModel)
            {
                personFeed.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "People", NavigationSourceEntityTypeName = NameSpacePrefix + "Person" });
            }

            await odataWriter.WriteStartAsync(personFeed);

            ODataResource personEntry = TestsHelper.CreatePersonEntry(hasModel);
            await odataWriter.WriteStartAsync(personEntry);

            var personNavigation = new ODataNestedResourceInfo()
            {
                Name = "PersonMetadata",
                IsCollection = true,
                Url = new Uri("People(-5)/PersonMetadata", UriKind.Relative)
            };
            await odataWriter.WriteStartAsync(personNavigation);
            await odataWriter.WriteEndAsync();

            // Finish writing personEntry.
            await odataWriter.WriteEndAsync();

            ODataResource employeeEntry = TestsHelper.CreateEmployeeEntry(hasModel);
            await odataWriter.WriteStartAsync(employeeEntry);

            var employeeNavigation1 = new ODataNestedResourceInfo()
            {
                Name = "PersonMetadata",
                IsCollection = true,
                Url = new Uri("People(-3)/" + NameSpacePrefix + "Employee" + "/PersonMetadata", UriKind.Relative)
            };
            await odataWriter.WriteStartAsync(employeeNavigation1);
            await odataWriter.WriteEndAsync();

            var employeeNavigation2 = new ODataNestedResourceInfo()
            {
                Name = "Manager",
                IsCollection = false,
                Url = new Uri("People(-3)/" + NameSpacePrefix + "Employee" + "/Manager", UriKind.Relative)
            };

            await odataWriter.WriteStartAsync(employeeNavigation2);
            await odataWriter.WriteEndAsync();

            // Finish writing employeeEntry.
            await odataWriter.WriteEndAsync();

            ODataResource specialEmployeeEntry = TestsHelper.CreateSpecialEmployeeEntry(hasModel);
            await odataWriter.WriteStartAsync(specialEmployeeEntry);

            var specialEmployeeNavigation1 = new ODataNestedResourceInfo()
            {
                Name = "PersonMetadata",
                IsCollection = true,
                Url = new Uri("People(-10)/" + NameSpacePrefix + "SpecialEmployee" + "/PersonMetadata", UriKind.Relative)
            };
            await odataWriter.WriteStartAsync(specialEmployeeNavigation1);
            await odataWriter.WriteEndAsync();

            var specialEmployeeNavigation2 = new ODataNestedResourceInfo()
            {
                Name = "Manager",
                IsCollection = false,
                Url = new Uri("People(-10)/" + NameSpacePrefix + "SpecialEmployee" + "/Manager", UriKind.Relative)
            };
            await odataWriter.WriteStartAsync(specialEmployeeNavigation2);
            await odataWriter.WriteEndAsync();

            var specialEmployeeNavigation3 = new ODataNestedResourceInfo()
            {
                Name = "Car",
                IsCollection = false,
                Url = new Uri("People(-10)/" + NameSpacePrefix + "SpecialEmployee" + "/Manager", UriKind.Relative)
            };
            await odataWriter.WriteStartAsync(specialEmployeeNavigation3);
            await odataWriter.WriteEndAsync();

            // Finish writing specialEmployeeEntry.
            await odataWriter.WriteEndAsync();

            // Finish writing the feed.
            await odataWriter.WriteEndAsync();

            // Some very basic verification for the payload.
            bool verifyFeedCalled = false;
            bool verifyEntryCalled = false;
            bool verifyNavigationCalled = false;
            Action<ODataResourceSet> verifyFeed = (feed) =>
            {
                if (mimeType != MimeTypes.ApplicationAtomXml)
                {
                    Assert.Contains("People", feed.DeltaLink.AbsoluteUri);
                }
                verifyFeedCalled = true;
            };

            Action<ODataResource> verifyEntry = (entry) =>
            {
                Assert.True(entry.EditLink.AbsoluteUri.EndsWith("People(-5)")
                    || entry.EditLink.AbsoluteUri.EndsWith("People(-3)/" + NameSpacePrefix + "Employee")
                    || entry.EditLink.AbsoluteUri.EndsWith("People(-10)/" + NameSpacePrefix + "SpecialEmployee"));
                verifyEntryCalled = true;
            };

            Action<ODataNestedResourceInfo> verifyNavigation = (navigation) =>
            {
                Assert.True(navigation.Name == "PersonMetadata" || navigation.Name == "Manager" || navigation.Name == "Car");
                verifyNavigationCalled = true;
            };

            var personType = _model.FindDeclaredType(NameSpacePrefix + "Person") as IEdmEntityType;
            var peopleSet = _model.EntityContainer.FindEntitySet("People");

            Stream stream = await responseMessage.GetStreamAsync();

            if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
            {
                stream.Seek(0, SeekOrigin.Begin);

                await TestsHelper.ReadAndVerifyFeedEntryMessageAsync(
                    true,
                    responseMessage,
                    peopleSet,
                    personType,
                    verifyFeed,
                    verifyEntry, verifyNavigation);

                Assert.True(verifyFeedCalled && verifyEntryCalled && verifyNavigationCalled, "Verification action not called.");
            }

            return await TestsHelper.ReadStreamContentAsync(stream);
        }

        private async Task<string> WriteAndVerifyEmployeeEntryAsync(
            TestStreamResponseMessage responseMessage,
            ODataWriter odataWriter,
            bool hasExpectedType,
            string mimeType)
        {

            ODataResource employeeEntry = TestsHelper.CreateEmployeeEntry(false);
            ODataResourceSerializationInfo serializationInfo = new ODataResourceSerializationInfo()
            {
                NavigationSourceName = "People",
                NavigationSourceEntityTypeName = NameSpacePrefix + "Person",
            };

            if (hasExpectedType)
            {
                serializationInfo.ExpectedTypeName = NameSpacePrefix + "Employee";
            }

            employeeEntry.SetSerializationInfo(serializationInfo);
            await odataWriter.WriteStartAsync(employeeEntry);

            var employeeNavigation1 = new ODataNestedResourceInfo()
            {
                Name = "PersonMetadata",
                IsCollection = true,
                Url = new Uri("People(-3)/" + NameSpacePrefix + "Employee" + "/PersonMetadata", UriKind.Relative)
            };
            await odataWriter.WriteStartAsync(employeeNavigation1);
            await odataWriter.WriteEndAsync();

            var employeeNavigation2 = new ODataNestedResourceInfo()
            {
                Name = "Manager",
                IsCollection = false,
                Url = new Uri("People(-3)/" + NameSpacePrefix + "Employee" + "/Manager", UriKind.Relative)
            };
            await odataWriter.WriteStartAsync(employeeNavigation2);
            await odataWriter.WriteEndAsync();

            // Finish writing employeeEntry.
            await odataWriter.WriteEndAsync();

            // Some very basic verification for the payload.
            bool verifyEntryCalled = false;
            bool verifyNavigationCalled = false;

            Action<ODataResource> verifyEntry = (entry) =>
            {
                Assert.True(entry.EditLink.AbsoluteUri.Contains("People"), "entry.EditLink");
                verifyEntryCalled = true;
            };

            Action<ODataNestedResourceInfo> verifyNavigation = (navigation) =>
            {
                Assert.True(navigation.Name == "PersonMetadata" || navigation.Name == "Manager", "navigation.Name");
                verifyNavigationCalled = true;
            };

            var personType = _model.FindDeclaredType(NameSpacePrefix + "Person") as IEdmEntityType;
            var peopleSet = _model.EntityContainer.FindEntitySet("People");

            Stream stream = await responseMessage.GetStreamAsync();

            if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
            {
                stream.Seek(0, SeekOrigin.Begin);
                await TestsHelper.ReadAndVerifyFeedEntryMessageAsync(false, responseMessage, peopleSet, personType, null, verifyEntry, verifyNavigation);
                Assert.True(verifyEntryCalled && verifyNavigationCalled, "Verification action not called.");
            }

            return await TestsHelper.ReadStreamContentAsync(stream);
        }

        private async Task<string> WriteAndVerifyCollectionAsync(
            TestStreamResponseMessage responseMessage,
            ODataWriter odataWriter,
            bool hasModel,
            string mimeType)
        {
            var resourceSet = new ODataResourceSetWrapper()
            {
                ResourceSet = new ODataResourceSet
                {
                    Count = 12,
                    NextPageLink = new Uri("http://localhost")
                },
                Resources = new List<ODataResourceWrapper>()
                {
                    TestsHelper.CreatePrimaryContactODataWrapper()
                }
            };

            if (!hasModel)
            {
                resourceSet.ResourceSet.SetSerializationInfo(new ODataResourceSerializationInfo()
                {
                    ExpectedTypeName = NameSpacePrefix + "ContactDetails"
                });
            }

            await ODataWriterHelper.WriteResourceSetAsync(odataWriter, resourceSet);
            Stream stream = await responseMessage.GetStreamAsync();

            if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var settings = new ODataMessageReaderSettings() { BaseUri = _baseUri, EnableMessageStreamDisposal = false };
                var contactDetailType = _model.FindDeclaredType(NameSpacePrefix + "ContactDetails") as IEdmComplexType;

                ODataMessageReader messageReader = new ODataMessageReader(responseMessage, settings, _model);
                ODataReader reader = await messageReader.CreateODataResourceSetReaderAsync(contactDetailType);
                bool collectionRead = false;

                while (await reader.ReadAsync())
                {
                    if (reader.State == ODataReaderState.ResourceSetEnd)
                    {
                        collectionRead = true;
                    }
                }

                Assert.True(collectionRead, "collectionRead");
                Assert.Equal(ODataReaderState.Completed, reader.State);
            }

            return await TestsHelper.ReadStreamContentAsync(stream);
        }

        private async Task<string> WriteAndVerifyLinksAsync(
            TestStreamResponseMessage responseMessage, 
            ODataMessageWriter messageWriter, 
            string mimeType)
        {
            var links = new ODataEntityReferenceLinks()
            {
                Links = new[]
                {
                    new ODataEntityReferenceLink() {Url = new Uri(_baseUri + "Orders(-10)")},
                    new ODataEntityReferenceLink() {Url = new Uri(_baseUri + "Orders(-7)")},
                },
                NextPageLink = new Uri(_baseUri + "Customers(-10)/Orders/$ref?$skiptoken=-7")
            };

            await messageWriter.WriteEntityReferenceLinksAsync(links);

            Stream stream = await responseMessage.GetStreamAsync();
            if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var settings = new ODataMessageReaderSettings() { BaseUri = _baseUri, EnableMessageStreamDisposal = false };

                ODataMessageReader messageReader = new ODataMessageReader(responseMessage, settings, _model);

                ODataEntityReferenceLinks linksRead = await messageReader.ReadEntityReferenceLinksAsync();
                Assert.Equal(2, linksRead.Links.Count());
                Assert.NotNull(linksRead.NextPageLink);
            }

            return await TestsHelper.ReadStreamContentAsync(stream);
        }

        private async Task<string> WriteAndVerifySingleLinkAsync(
            TestStreamResponseMessage responseMessage, 
            ODataMessageWriter messageWriter, 
            string mimeType)
        {
            var link = new ODataEntityReferenceLink() { Url = new Uri(_baseUri + "Orders(-10)") };

            await messageWriter.WriteEntityReferenceLinkAsync(link);
            var stream = await responseMessage.GetStreamAsync();

            if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var settings = new ODataMessageReaderSettings() { BaseUri = _baseUri, EnableMessageStreamDisposal = false };

                ODataMessageReader messageReader = new ODataMessageReader(responseMessage, settings, _model);

                ODataEntityReferenceLink linkRead = await messageReader.ReadEntityReferenceLinkAsync();
                Assert.True(linkRead.Url.AbsoluteUri.Contains("Orders(-10)"), "linkRead.Url");
            }

            return await TestsHelper.ReadStreamContentAsync(stream);
        }

        private async Task<string> WriteAndVerifyRequestMessageAsync(
            TestStreamRequestMessage requestMessageWithoutModel,
            ODataWriter odataWriter,
            bool hasModel, 
            string mimeType)
        {
            var order = new ODataResource()
            {
                Id = new Uri(_baseUri + "Orders(-10)"),
                TypeName = NameSpacePrefix + "Order"
            };

            var orderP1 = new ODataProperty { Name = "OrderId", Value = -10 };
            var orderp2 = new ODataProperty { Name = "CustomerId", Value = 8212 };
            var orderp3 = new ODataProperty { Name = "Concurrency", Value = null };
            order.Properties = new[] { orderP1, orderp2, orderp3 };

            if (!hasModel)
            {
                order.SetSerializationInfo(new ODataResourceSerializationInfo() { NavigationSourceName = "Orders", NavigationSourceEntityTypeName = NameSpacePrefix + "Order" });
                orderP1.SetSerializationInfo(new ODataPropertySerializationInfo() { PropertyKind = ODataPropertyKind.Key });
            }

            await odataWriter.WriteStartAsync(order);
            await odataWriter.WriteEndAsync();

            var orderType = _model.FindDeclaredType(NameSpacePrefix + "Order") as IEdmEntityType;
            var orderSet = _model.EntityContainer.FindEntitySet("Orders");
            Stream stream = await requestMessageWithoutModel.GetStreamAsync();

            if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var settings = new ODataMessageReaderSettings() { BaseUri = _baseUri, EnableMessageStreamDisposal = false };
                ODataMessageReader messageReader = new ODataMessageReader(requestMessageWithoutModel, settings, _model);
                ODataReader reader = await messageReader.CreateODataResourceReaderAsync(orderSet, orderType);
                ODataResource entry = null;

                while (await reader.ReadAsync())
                {
                    if (reader.State == ODataReaderState.ResourceEnd)
                    {
                        entry = reader.Item as ODataResource;
                    }
                }

                Assert.True(entry.Id.ToString().Contains("Orders(-10)"), "entry.Id");
                Assert.Equal(2, entry.Properties.Count());
                Assert.Equal(ODataReaderState.Completed, reader.State);
            }

            return await TestsHelper.ReadStreamContentAsync(stream);
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
