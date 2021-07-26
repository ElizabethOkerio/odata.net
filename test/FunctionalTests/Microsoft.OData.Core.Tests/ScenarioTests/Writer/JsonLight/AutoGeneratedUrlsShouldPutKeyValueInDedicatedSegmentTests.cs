﻿//---------------------------------------------------------------------
// <copyright file="AutoGeneratedUrlsShouldPutKeyValueInDedicatedSegmentTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Core.Tests.DependencyInjection;
using Microsoft.OData.Edm;
using Xunit;

namespace Microsoft.OData.Tests.ScenarioTests.Writer.JsonLight
{
    public class AutoGeneratedUrlsShouldPutKeyValueInDedicatedSegmentTests
    {
        private EdmModel model;
        private IEdmEntitySet peopleSet;
        private EdmEntityType personType;
        private EdmEntityContainer entityContainer;

        public AutoGeneratedUrlsShouldPutKeyValueInDedicatedSegmentTests()
        {
            this.model = new EdmModel();
            this.entityContainer = new EdmEntityContainer("Namespace", "Container");

            this.personType = new EdmEntityType("Namespace", "Person");
            this.personType.AddKeys(this.personType.AddStructuralProperty("Key", EdmPrimitiveTypeKind.String));
            this.peopleSet = this.entityContainer.AddEntitySet("People", personType);
            model.AddElement(this.entityContainer);
            model.AddElement(this.personType);
        }

        [Fact]
        public void IfKeyAsSegmentSettingIsTrueAndNoModelIsGivenThenLinkShouldHaveKeyAsSegment()
        {
            string json = this.SerializeEntryInFullMetadataJson(true, /*model*/ null);
            Assert.Contains("People/KeyValue", json);
            Assert.DoesNotContain("People('KeyValue')", json);
        }

        [Fact]
        public void IfKeyAsSegmentSettingIsTrueAndAModelIsGivenThenLinkShouldHaveKeyAsSegment()
        {
            string json = this.SerializeEntryInFullMetadataJson(true, this.model, this.personType, this.peopleSet);
            Assert.Contains("People/KeyValue", json);
            Assert.DoesNotContain("People('KeyValue')", json);
        }

        [Fact]
        public void IfKeyAsSegmentSettingIsTrueAndModelHasKeyAsSegmentAnnotationThenLinkShouldHaveKeyAsSegment()
        {
            string json = this.SerializeEntryInFullMetadataJson(true, this.model, this.personType, this.peopleSet);
            Assert.Contains("People/KeyValue", json);
            Assert.DoesNotContain("People('KeyValue')", json);
        }

        [Fact]
        public void IfKeyAsSegmentSettingIsFalseAndNoModelIsGivenThenLinkShouldHaveKeyInParens()
        {
            string json = this.SerializeEntryInFullMetadataJson(false, /*model*/ null);
            Assert.Contains("People('KeyValue')", json);
            Assert.DoesNotContain("People/KeyValue", json);
        }

        [Fact]
        public void IfKeyAsSegmentSettingIsFalseAndAModelIsGivenThenLinkShouldHaveKeyInParens()
        {
            string json = this.SerializeEntryInFullMetadataJson(false, this.model, this.personType, this.peopleSet);
            Assert.Contains("People('KeyValue')", json);
            Assert.DoesNotContain("People/KeyValue", json);
        }

        [Fact]
        public void IfKeyAsSegmentSettingIsFalseAndModelHasKeyAsSegmentAnnotationThenLinkShouldNotHaveKeyAsSegment()
        {
            string json = this.SerializeEntryInFullMetadataJson(false, this.model, this.personType, this.peopleSet);
            Assert.Contains("People('KeyValue')", json);
            Assert.DoesNotContain("People/KeyValue", json);
        }

        private string SerializeEntryInFullMetadataJson(
            bool useKeyAsSegment,
            IEdmModel edmModel,
            IEdmEntityType entityType = null,
            IEdmEntitySet entitySet = null)
        {
            var settings = new ODataMessageWriterSettings { EnableWritingKeyAsSegment = useKeyAsSegment };
            settings.SetServiceDocumentUri(new Uri("http://example.com/"));
            var outputStream = new MemoryStream();
            var container = ServiceProviderBuilderHelper.BuildServiceProvider(null);
            container.GetRequiredService<ODataMessageWriterSettings>().EnableWritingKeyAsSegment = useKeyAsSegment;

            var responseMessage = new InMemoryMessage { Stream = outputStream, Container = container };
            responseMessage.SetHeader("Content-Type", "application/json;odata.metadata=full");
            string output;

            using (var messageWriter = new ODataMessageWriter((IODataResponseMessage)responseMessage, settings, edmModel))
            {
                var entryWriter = messageWriter.CreateODataResourceWriter(entitySet, entityType);
                ODataProperty keyProperty = new ODataProperty() { Name = "Key", Value = "KeyValue" };

                var entry = new ODataResource { Properties = new[] { keyProperty }, TypeName = "Namespace.Person" };

                if (edmModel == null)
                {
                    keyProperty.SetSerializationInfo(new ODataPropertySerializationInfo
                    {
                        PropertyKind = ODataPropertyKind.Key
                    });

                    entry.SetSerializationInfo(new ODataResourceSerializationInfo
                    {
                        NavigationSourceEntityTypeName = "Namespace.Person",
                        NavigationSourceName = "People",
                        ExpectedTypeName = "Namespace.Person",
                        NavigationSourceKind = EdmNavigationSourceKind.EntitySet
                    });
                }

                entryWriter.WriteStart(entry);
                entryWriter.WriteEnd();
                entryWriter.Flush();

                outputStream.Seek(0, SeekOrigin.Begin);
                output = new StreamReader(outputStream).ReadToEnd();
            }

            return output;
        }
    }
}
