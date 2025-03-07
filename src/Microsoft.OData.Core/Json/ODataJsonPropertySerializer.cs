﻿//---------------------------------------------------------------------
// <copyright file="ODataJsonPropertySerializer.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData.Json
{
    #region Namespaces
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.OData.Core;
    using Microsoft.OData.Edm;
    using Microsoft.OData.Evaluation;
    #endregion Namespaces

    /// <summary>
    /// OData Json serializer for properties.
    /// </summary>
    internal class ODataJsonPropertySerializer : ODataJsonSerializer
    {
        /// <summary>
        /// Serializer to use to write property values.
        /// </summary>
        private readonly ODataJsonValueSerializer jsonValueSerializer;

        /// <summary>
        /// Serialization info for current property.
        /// </summary>
        private PropertySerializationInfo currentPropertyInfo;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="jsonOutputContext">The output context to write to.</param>
        /// <param name="initContextUriBuilder">Whether contextUriBuilder should be initialized.</param>
        internal ODataJsonPropertySerializer(ODataJsonOutputContext jsonOutputContext, bool initContextUriBuilder = false)
            : base(jsonOutputContext, initContextUriBuilder)
        {
            this.jsonValueSerializer = new ODataJsonValueSerializer(this, initContextUriBuilder);
        }

        /// <summary>
        /// Gets the Json value writer.
        /// </summary>
        internal ODataJsonValueSerializer JsonValueSerializer
        {
            get
            {
                return this.jsonValueSerializer;
            }
        }

        /// <summary>
        /// Write an <see cref="ODataProperty" /> to the given stream. This method creates an
        /// async buffered stream and writes the property to it.
        /// </summary>
        /// <param name="property">The property to write.</param>
        internal void WriteTopLevelProperty(ODataProperty property)
        {
            Debug.Assert(property != null, "property != null");
            Debug.Assert(!(property.Value is ODataStreamReferenceValue), "!(property.Value is ODataStreamReferenceValue)");

            this.WriteTopLevelPayload(
                () =>
                {
                    this.JsonWriter.StartObjectScope();
                    ODataPayloadKind kind = GetPayloadKind();

                    if (!(this.JsonOutputContext.MetadataLevel is JsonNoMetadataLevel))
                    {
                        ODataContextUrlInfo contextUrlInfo = GetContextUrlInfo(property);
                        this.WriteContextUriProperty(kind, () => contextUrlInfo);
                    }

                    // Note we do not allow named stream properties to be written as top level property.
                    this.JsonValueSerializer.AssertRecursionDepthIsZero();
                    IDuplicatePropertyNameChecker duplicatePropertyNameChecker = this.GetDuplicatePropertyNameChecker();

                    this.WriteProperty(
                        property,
                        owningType: null,
                        isTopLevel: true,
                        duplicatePropertyNameChecker: duplicatePropertyNameChecker,
                        metadataBuilder : null);

                    this.JsonValueSerializer.AssertRecursionDepthIsZero();
                    this.ReturnDuplicatePropertyNameChecker(duplicatePropertyNameChecker);
                    this.JsonWriter.EndObjectScope();
                });
        }

        /// <summary>
        /// Writes property names and value pairs.
        /// </summary>
        /// <param name="owningType">The <see cref="IEdmStructuredType"/> of the resource (or null if not metadata is available).</param>
        /// <param name="properties">The enumeration of properties to write out.</param>
        /// <param name="isComplexValue">
        /// Whether the properties are being written for complex value. Also used for detecting whether stream properties
        /// are allowed as named stream properties should only be defined on ODataResource instances
        /// </param>
        /// <param name="duplicatePropertyNameChecker">The DuplicatePropertyNameChecker to use.</param>
        /// <param name="metadataBuilder">The metadatabuilder for writing the property.</param>
        internal void WriteProperties(
            IEdmStructuredType owningType,
            IEnumerable<ODataPropertyInfo> properties,
            bool isComplexValue,
            IDuplicatePropertyNameChecker duplicatePropertyNameChecker,
            ODataResourceMetadataBuilder metadataBuilder)
        {
            if (properties == null)
            {
                return;
            }

            foreach (ODataPropertyInfo property in properties)
            {
                this.WriteProperty(
                    property,
                    owningType,
                    false /* isTopLevel */,
                    duplicatePropertyNameChecker,
                    metadataBuilder);
            }
        }

        /// <summary>
        /// Writes a name/value pair for a property.
        /// </summary>
        /// <param name="propertyInfo">The property to write out.</param>
        /// <param name="owningType">The owning type for the <paramref name="propertyInfo"/> or null if no metadata is available.</param>
        /// <param name="isTopLevel">true when writing a top-level property; false for nested properties.</param>
        /// <param name="duplicatePropertyNameChecker">The DuplicatePropertyNameChecker to use.</param>
        /// <param name="metadataBuilder">The metadatabuilder for the resource</param>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Splitting the code would make the logic harder to understand; class coupling is only slightly above threshold.")]
        internal void WriteProperty(
            ODataPropertyInfo propertyInfo,
            IEdmStructuredType owningType,
            bool isTopLevel,
            IDuplicatePropertyNameChecker duplicatePropertyNameChecker,
            ODataResourceMetadataBuilder metadataBuilder)
        {
            this.WritePropertyInfo(propertyInfo, owningType, isTopLevel, duplicatePropertyNameChecker, metadataBuilder);

            if (propertyInfo is not ODataProperty property)
            {
                return;
            }

            ODataValue value = property.ODataValue;

            // handle ODataUntypedValue
            ODataUntypedValue untypedValue = value as ODataUntypedValue;
            if (untypedValue != null)
            {
                WriteUntypedValue(untypedValue);
                return;
            }

            ODataStreamReferenceValue streamReferenceValue = value as ODataStreamReferenceValue;
            if (streamReferenceValue != null && !(this.JsonOutputContext.MetadataLevel is JsonNoMetadataLevel))
            {
                Debug.Assert(!isTopLevel, "Stream properties are not allowed at the top level.");
                WriteStreamValue(streamReferenceValue, property.Name, metadataBuilder);
                return;
            }

            if (value is ODataNullValue || value == null)
            {
                this.WriteNullProperty(property);
                return;
            }

            bool isOpenPropertyType = this.IsOpenProperty(property);

            ODataPrimitiveValue primitiveValue = value as ODataPrimitiveValue;
            if (primitiveValue != null)
            {
                this.WritePrimitiveProperty(primitiveValue, isOpenPropertyType);
                return;
            }

            ODataEnumValue enumValue = value as ODataEnumValue;
            if (enumValue != null)
            {
                this.WriteEnumProperty(enumValue, isOpenPropertyType);
                return;
            }

            ODataResourceValue resourceValue = value as ODataResourceValue;
            if (resourceValue != null)
            {
                if (isTopLevel)
                {
                    throw new ODataException(Error.Format(SRResources.ODataMessageWriter_NotAllowedWriteTopLevelPropertyWithResourceValue, property.Name));
                }

                this.WriteResourceProperty(property, resourceValue, isOpenPropertyType);
                return;
            }

            ODataCollectionValue collectionValue = value as ODataCollectionValue;
            if (collectionValue != null)
            {
                if (isTopLevel)
                {
                    if (collectionValue.Items != null && collectionValue.Items.Any(i => i is ODataResourceValue))
                    {
                        throw new ODataException(Error.Format(SRResources.ODataMessageWriter_NotAllowedWriteTopLevelPropertyWithResourceValue, property.Name));
                    }
                }

                this.WriteCollectionProperty(collectionValue, isOpenPropertyType);
                return;
            }

            ODataBinaryStreamValue streamValue = value as ODataBinaryStreamValue;
            if (streamValue != null)
            {
                this.WriteStreamProperty(streamValue, isOpenPropertyType);
                return;
            }

            if (value is ODataJsonElementValue jsonElementValue)
            {
                this.WriteJsonElementProperty(jsonElementValue);
                return;
            }
        }


        /// <summary>
        /// Writes the property information for a property.
        /// </summary>
        /// <param name="propertyInfo">The property info to write out.</param>
        /// <param name="owningType">The owning type for the <paramref name="propertyInfo"/> or null if no metadata is available.</param>
        /// <param name="isTopLevel">true when writing a top-level property; false for nested properties.</param>
        /// <param name="duplicatePropertyNameChecker">The DuplicatePropertyNameChecker to use.</param>
        /// <param name="metadataBuilder">The metadatabuilder for the resource</param>
        internal void WritePropertyInfo(
            ODataPropertyInfo propertyInfo,
            IEdmStructuredType owningType,
            bool isTopLevel,
            IDuplicatePropertyNameChecker duplicatePropertyNameChecker,
            ODataResourceMetadataBuilder metadataBuilder)
        {
            ValidatePropertyInfo(propertyInfo, owningType, isTopLevel, duplicatePropertyNameChecker);

            if (currentPropertyInfo.MetadataType.IsUndeclaredProperty)
            {
                WriteODataTypeAnnotation(propertyInfo, isTopLevel);
            }

            WriteInstanceAnnotation(propertyInfo, isTopLevel, currentPropertyInfo.MetadataType.IsUndeclaredProperty);

            ODataStreamPropertyInfo streamInfo = propertyInfo as ODataStreamPropertyInfo;
            if (streamInfo != null && !(this.JsonOutputContext.MetadataLevel is JsonNoMetadataLevel))
            {
                Debug.Assert(!isTopLevel, "Stream properties are not allowed at the top level.");
                WriteStreamValue(streamInfo, propertyInfo.Name, metadataBuilder);
            }
        }

        /// <summary>
        /// Asynchronously writes an <see cref="ODataProperty" /> to the given stream. This method creates an
        /// async buffered stream and writes the property to it.
        /// </summary>
        /// <param name="property">The property to write.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        internal Task WriteTopLevelPropertyAsync(ODataProperty property)
        {
            Debug.Assert(property != null, "property != null");
            Debug.Assert(!(property.Value is ODataStreamReferenceValue), "!(property.Value is ODataStreamReferenceValue)");

            return this.WriteTopLevelPayloadAsync(
                async (thisParam, propertyParam) =>
                {
                    await thisParam.JsonWriter.StartObjectScopeAsync().ConfigureAwait(false);
                    ODataPayloadKind kind = thisParam.GetPayloadKind();

                    if (!(thisParam.JsonOutputContext.MetadataLevel is JsonNoMetadataLevel))
                    {
                        ODataContextUrlInfo contextUrlInfo = thisParam.GetContextUrlInfo(propertyParam);
                        await thisParam.WriteContextUriPropertyAsync(
                            kind,
                            (contextUrlInfoParam) => contextUrlInfoParam, contextUrlInfo).ConfigureAwait(false);
                    }

                    // Note we do not allow named stream properties to be written as top level property.
                    thisParam.JsonValueSerializer.AssertRecursionDepthIsZero();
                    IDuplicatePropertyNameChecker duplicatePropertyNameChecker = thisParam.GetDuplicatePropertyNameChecker();

                    await thisParam.WritePropertyAsync(
                        propertyInfo: propertyParam,
                        owningType : null,
                        isTopLevel : true,
                        duplicatePropertyNameChecker : duplicatePropertyNameChecker,
                        metadataBuilder : null).ConfigureAwait(false);

                    thisParam.JsonValueSerializer.AssertRecursionDepthIsZero();
                    thisParam.ReturnDuplicatePropertyNameChecker(duplicatePropertyNameChecker);
                    await thisParam.JsonWriter.EndObjectScopeAsync().ConfigureAwait(false);
                },
                this,
                property);
        }

        /// <summary>
        /// Asynchronously writes property names and value pairs.
        /// </summary>
        /// <param name="owningType">The <see cref="IEdmStructuredType"/> of the resource (or null if not metadata is available).</param>
        /// <param name="properties">The enumeration of properties to write out.</param>
        /// <param name="isComplexValue">
        /// Whether the properties are being written for complex value. Also used for detecting whether stream properties
        /// are allowed as named stream properties should only be defined on ODataResource instances
        /// </param>
        /// <param name="duplicatePropertyNameChecker">The DuplicatePropertyNameChecker to use.</param>
        /// <param name="metadataBuilder">The metadatabuilder for writing the property.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        internal async Task WritePropertiesAsync(
            IEdmStructuredType owningType,
            IEnumerable<ODataPropertyInfo> properties,
            bool isComplexValue,
            IDuplicatePropertyNameChecker duplicatePropertyNameChecker,
            ODataResourceMetadataBuilder metadataBuilder)
        {
            if (properties == null)
            {
                return;
            }

            foreach (ODataPropertyInfo property in properties)
            {
                await this.WritePropertyAsync(
                    property,
                    owningType,
                    false /* isTopLevel */,
                    duplicatePropertyNameChecker,
                    metadataBuilder).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously writes a name/value pair for a property.
        /// </summary>
        /// <param name="propertyInfo">The property to write out.</param>
        /// <param name="owningType">The owning type for the <paramref name="propertyInfo"/> or null if no metadata is available.</param>
        /// <param name="isTopLevel">true when writing a top-level property; false for nested properties.</param>
        /// <param name="duplicatePropertyNameChecker">The DuplicatePropertyNameChecker to use.</param>
        /// <param name="metadataBuilder">The metadatabuilder for the resource</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Splitting the code would make the logic harder to understand; class coupling is only slightly above threshold.")]
        internal async Task WritePropertyAsync(
            ODataPropertyInfo propertyInfo,
            IEdmStructuredType owningType,
            bool isTopLevel,
            IDuplicatePropertyNameChecker duplicatePropertyNameChecker,
            ODataResourceMetadataBuilder metadataBuilder)
        {
            await this.WritePropertyInfoAsync(propertyInfo, owningType, isTopLevel, duplicatePropertyNameChecker, metadataBuilder)
                .ConfigureAwait(false);

            if (propertyInfo is not ODataProperty property)
            {
                return;
            }

            ODataValue value = property.ODataValue;

            // handle ODataUntypedValue
            ODataUntypedValue untypedValue = value as ODataUntypedValue;
            if (untypedValue != null)
            {
                await WriteUntypedValueAsync(untypedValue).ConfigureAwait(false);
                return;
            }

            ODataStreamReferenceValue streamReferenceValue = value as ODataStreamReferenceValue;
            if (streamReferenceValue != null && !(this.JsonOutputContext.MetadataLevel is JsonNoMetadataLevel))
            {
                Debug.Assert(!isTopLevel, "Stream properties are not allowed at the top level.");
                await WriteStreamValueAsync(streamReferenceValue, property.Name, metadataBuilder)
                    .ConfigureAwait(false);
                return;
            }

            if (value is ODataNullValue || value == null)
            {
                await this.WriteNullPropertyAsync(property).ConfigureAwait(false);
                return;
            }

            bool isOpenPropertyType = this.IsOpenProperty(property);

            ODataPrimitiveValue primitiveValue = value as ODataPrimitiveValue;
            if (primitiveValue != null)
            {
                await this.WritePrimitivePropertyAsync(primitiveValue, isOpenPropertyType)
                    .ConfigureAwait(false);
                return;
            }

            ODataEnumValue enumValue = value as ODataEnumValue;
            if (enumValue != null)
            {
                await this.WriteEnumPropertyAsync(enumValue, isOpenPropertyType)
                    .ConfigureAwait(false);
                return;
            }

            ODataResourceValue resourceValue = value as ODataResourceValue;
            if (resourceValue != null)
            {
                if (isTopLevel)
                {
                    throw new ODataException(Error.Format(SRResources.ODataMessageWriter_NotAllowedWriteTopLevelPropertyWithResourceValue, property.Name));
                }

                await this.WriteResourcePropertyAsync(property, resourceValue, isOpenPropertyType)
                    .ConfigureAwait(false);
                return;
            }

            ODataCollectionValue collectionValue = value as ODataCollectionValue;
            if (collectionValue != null)
            {
                if (isTopLevel)
                {
                    if (collectionValue.Items != null && collectionValue.Items.Any(i => i is ODataResourceValue))
                    {
                        throw new ODataException(Error.Format(SRResources.ODataMessageWriter_NotAllowedWriteTopLevelPropertyWithResourceValue, property.Name));
                    }
                }

                await this.WriteCollectionPropertyAsync(collectionValue, isOpenPropertyType)
                    .ConfigureAwait(false);
                return;
            }

            ODataBinaryStreamValue streamValue = value as ODataBinaryStreamValue;
            if (streamValue != null)
            {
                await this.WriteStreamPropertyAsync(streamValue, isOpenPropertyType)
                    .ConfigureAwait(false);
                return;
            }

            if (value is ODataJsonElementValue jsonElementValue)
            {
                await this.WriteJsonElementPropertyAsync(jsonElementValue)
                    .ConfigureAwait(false);
                return;
            }
        }

        /// <summary>
        /// Asynchronously writes the property information for a property.
        /// </summary>
        /// <param name="propertyInfo">The property info to write out.</param>
        /// <param name="owningType">The owning type for the <paramref name="propertyInfo"/> or null if no metadata is available.</param>
        /// <param name="isTopLevel">true when writing a top-level property; false for nested properties.</param>
        /// <param name="duplicatePropertyNameChecker">The DuplicatePropertyNameChecker to use.</param>
        /// <param name="metadataBuilder">The metadatabuilder for the resource</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        internal async Task WritePropertyInfoAsync(
            ODataPropertyInfo propertyInfo,
            IEdmStructuredType owningType,
            bool isTopLevel,
            IDuplicatePropertyNameChecker duplicatePropertyNameChecker,
            ODataResourceMetadataBuilder metadataBuilder)
        {
            ValidatePropertyInfo(propertyInfo, owningType, isTopLevel, duplicatePropertyNameChecker);

            if (currentPropertyInfo.MetadataType.IsUndeclaredProperty)
            {
                await WriteODataTypeAnnotationAsync(propertyInfo, isTopLevel)
                    .ConfigureAwait(false);
            }

            await WriteInstanceAnnotationAsync(propertyInfo, isTopLevel, currentPropertyInfo.MetadataType.IsUndeclaredProperty)
                .ConfigureAwait(false);

            ODataStreamPropertyInfo streamInfo = propertyInfo as ODataStreamPropertyInfo;
            if (streamInfo != null && !(this.JsonOutputContext.MetadataLevel is JsonNoMetadataLevel))
            {
                Debug.Assert(!isTopLevel, "Stream properties are not allowed at the top level.");
                await WriteStreamValueAsync(streamInfo, propertyInfo.Name, metadataBuilder)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Test to see if <paramref name="property"/> is an open property or not.
        /// </summary>
        /// <param name="property">The property in question.</param>
        /// <returns>true if the property is an open property; false if it is not, or if openness cannot be determined</returns>
        private bool IsOpenProperty(ODataPropertyInfo property)
        {
            Debug.Assert(property != null, "property != null");

            bool isOpenProperty;

            if (property.SerializationInfo != null)
            {
                isOpenProperty = property.SerializationInfo.PropertyKind == ODataPropertyKind.Open;
            }
            else
            {
                // TODO: (issue #888) this logic results in type annotations not being written for dynamic properties on types that are not
                // marked as open. Type annotations should always be written for dynamic properties whose type cannot be heuristically
                // determined. Need to change this.currentPropertyInfo.MetadataType.IsOpenProperty to this.currentPropertyInfo.MetadataType.IsDynamic,
                // and fix related tests and other logic (this change alone results in writing type even if it's already implied by context).
                isOpenProperty = (!this.WritingResponse && this.currentPropertyInfo.MetadataType.OwningType == null) // Treat property as dynamic property when writing request and owning type is null
                || this.currentPropertyInfo.MetadataType.IsOpenProperty;
            }

            return isOpenProperty;
        }

        private void WriteUntypedValue(ODataUntypedValue untypedValue)
        {
            this.JsonWriter.WriteName(this.currentPropertyInfo.WireName);
            this.jsonValueSerializer.WriteUntypedValue(untypedValue);
            return;
        }

        /// <summary>
        /// Writes a <see cref="System.Text.Json.JsonElement"/> property.
        /// </summary>
        /// <param name="jsonElementValue">The value to be written.</param>
        private void WriteJsonElementProperty(ODataJsonElementValue jsonElementValue)
        {
            this.JsonWriter.WriteName(this.currentPropertyInfo.WireName);
            this.JsonWriter.WriteValue(jsonElementValue.Value);
        }

        private void WriteStreamValue(IODataStreamReferenceInfo streamInfo, string propertyName, ODataResourceMetadataBuilder metadataBuilder)
        {
            WriterValidationUtils.ValidateStreamPropertyInfo(streamInfo, currentPropertyInfo.MetadataType.EdmProperty, propertyName, this.WritingResponse);
            this.WriteStreamInfo(propertyName, streamInfo);
            if (metadataBuilder != null)
            {
                metadataBuilder.MarkStreamPropertyProcessed(propertyName);
            }
        }

        /// <summary>
        /// Writes instance annotation for property
        /// </summary>
        /// <param name="property">The property to handle.</param>
        /// <param name="isTopLevel">If writing top level property.</param>
        /// <param name="isUndeclaredProperty">If writing an undeclared property.</param>
        private void WriteInstanceAnnotation(ODataPropertyInfo property, bool isTopLevel, bool isUndeclaredProperty)
        {
            if (property.InstanceAnnotations.Count != 0)
            {
                if (isTopLevel)
                {
                    this.InstanceAnnotationWriter.WriteInstanceAnnotations(property.InstanceAnnotations);
                }
                else
                {
                    this.InstanceAnnotationWriter.WriteInstanceAnnotations(property.InstanceAnnotations, property.Name, isUndeclaredProperty);
                }
            }
        }

        /// <summary>
        /// Writes odata type annotation for property
        /// </summary>
        /// <param name="property">The property to handle.</param>
        /// <param name="isTopLevel">If writing top level property.</param>
        private void WriteODataTypeAnnotation(ODataPropertyInfo property, bool isTopLevel)
        {
            if (property.TypeAnnotation != null && property.TypeAnnotation.TypeName != null)
            {
                string typeName = property.TypeAnnotation.TypeName;
                IEdmPrimitiveType primitiveType = EdmCoreModel.Instance.FindType(typeName) as IEdmPrimitiveType;
                if (primitiveType == null ||
                    (primitiveType.PrimitiveKind != EdmPrimitiveTypeKind.String &&
                    primitiveType.PrimitiveKind != EdmPrimitiveTypeKind.Decimal &&
                    primitiveType.PrimitiveKind != EdmPrimitiveTypeKind.Boolean))
                {
                    if (isTopLevel)
                    {
                        this.ODataAnnotationWriter.WriteODataTypeInstanceAnnotation(typeName);
                    }
                    else
                    {
                        this.ODataAnnotationWriter.WriteODataTypePropertyAnnotation(property.Name, typeName);
                    }
                }
            }
        }

        /// <summary>
        /// Writes stream property information.
        /// </summary>
        /// <param name="propertyName">The name of the stream property to write.</param>
        /// <param name="streamInfo">The stream reference value to be written</param>
        private void WriteStreamInfo(string propertyName, IODataStreamReferenceInfo streamInfo)
        {
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "!string.IsNullOrEmpty(propertyName)");
            Debug.Assert(streamInfo != null, "streamReferenceValue != null");

            Uri mediaEditLink = streamInfo.EditLink;
            if (mediaEditLink != null)
            {
                this.ODataAnnotationWriter.WritePropertyAnnotationName(propertyName, ODataAnnotationNames.ODataMediaEditLink);
                this.JsonWriter.WriteValue(this.UriToString(mediaEditLink));
            }

            Uri mediaReadLink = streamInfo.ReadLink;
            if (mediaReadLink != null)
            {
                this.ODataAnnotationWriter.WritePropertyAnnotationName(propertyName, ODataAnnotationNames.ODataMediaReadLink);
                this.JsonWriter.WriteValue(this.UriToString(mediaReadLink));
            }

            string mediaContentType = streamInfo.ContentType;
            if (mediaContentType != null)
            {
                this.ODataAnnotationWriter.WritePropertyAnnotationName(propertyName, ODataAnnotationNames.ODataMediaContentType);
                this.JsonWriter.WriteValue(mediaContentType);
            }

            string mediaETag = streamInfo.ETag;
            if (mediaETag != null)
            {
                this.ODataAnnotationWriter.WritePropertyAnnotationName(propertyName, ODataAnnotationNames.ODataMediaETag);
                this.JsonWriter.WriteValue(mediaETag);
            }
        }

        /// <summary>
        /// Writes a Null property.
        /// </summary>
        /// <param name="property">The property to write out.</param>
        private void WriteNullProperty(
            ODataPropertyInfo property)
        {
            this.WriterValidator.ValidateNullPropertyValue(
                this.currentPropertyInfo.MetadataType.TypeReference, property.Name,
                this.currentPropertyInfo.IsTopLevel, this.Model);

            if (this.currentPropertyInfo.IsTopLevel)
            {
                if (this.JsonOutputContext.MessageWriterSettings.LibraryCompatibility.HasFlag(ODataLibraryCompatibility.WriteTopLevelODataNullAnnotation)
                    && this.JsonOutputContext.MessageWriterSettings.Version < ODataVersion.V401)
                {
                    // The 6.x library used an OData 3.0 protocol element in this case: @odata.null=true
                    this.ODataAnnotationWriter.WriteInstanceAnnotationName(ODataAnnotationNames.ODataNull);
                    this.JsonWriter.WriteValue(true);
                }
                else
                {
                    // From the spec:
                    // 11.2.3 Requesting Individual Properties
                    // ...
                    // If the property is single-valued and has the null value, the service responds with 204 No Content.
                    // ...
                    throw new ODataException(SRResources.ODataMessageWriter_CannotWriteTopLevelNull);
                }
            }
            else
            {
                this.JsonWriter.WriteName(property.Name);
                this.JsonValueSerializer.WriteNullValue();
            }
        }

        /// <summary>
        /// Writes a resource property.
        /// </summary>
        /// <param name="property">The property to write out.</param>
        /// <param name="resourceValue">The resource value to be written</param>
        /// <param name="isOpenPropertyType">If the property is open.</param>
        private void WriteResourceProperty(
            ODataProperty property,
            ODataResourceValue resourceValue,
            bool isOpenPropertyType)
        {
            Debug.Assert(!this.currentPropertyInfo.IsTopLevel, "Resource property should not be top level");
            this.JsonWriter.WriteName(property.Name);

            IDuplicatePropertyNameChecker duplicatePropertyNameChecker = this.GetDuplicatePropertyNameChecker();

            this.JsonValueSerializer.WriteResourceValue(
                resourceValue,
                this.currentPropertyInfo.MetadataType.TypeReference,
                isOpenPropertyType,
                duplicatePropertyNameChecker);

            this.ReturnDuplicatePropertyNameChecker(duplicatePropertyNameChecker);
        }

        /// <summary>
        /// Writes a enum property.
        /// </summary>
        /// <param name="enumValue">The enum value to be written.</param>
        /// <param name="isOpenPropertyType">If the property is open.</param>
        private void WriteEnumProperty(
            ODataEnumValue enumValue,
            bool isOpenPropertyType)
        {
            ResolveEnumValueTypeName(enumValue, isOpenPropertyType);

            this.WritePropertyTypeName();
            this.JsonWriter.WriteName(this.currentPropertyInfo.WireName);
            this.JsonValueSerializer.WriteEnumValue(enumValue, this.currentPropertyInfo.MetadataType.TypeReference);
        }

        private void ResolveEnumValueTypeName(ODataEnumValue enumValue, bool isOpenPropertyType)
        {
            if (this.currentPropertyInfo.ValueType == null || this.currentPropertyInfo.ValueType.TypeName != enumValue.TypeName)
            {
                IEdmTypeReference typeFromValue = TypeNameOracle.ResolveAndValidateTypeForEnumValue(
                    this.Model,
                    enumValue,
                    isOpenPropertyType);

                // This is a work around, needTypeOnWire always = true for client side:
                // ClientEdmModel's reflection can't know a property is open type even if it is, so here
                // make client side always write 'odata.type' for enum.
                bool needTypeOnWire = string.Equals(this.JsonOutputContext.Model.GetType().Name, "ClientEdmModel",
                    StringComparison.OrdinalIgnoreCase);
                string typeNameToWrite = this.JsonOutputContext.TypeNameOracle.GetValueTypeNameForWriting(
                    enumValue, this.currentPropertyInfo.MetadataType.TypeReference, typeFromValue, needTypeOnWire || isOpenPropertyType);

                this.currentPropertyInfo.ValueType = new PropertyValueTypeInfo(enumValue.TypeName, typeFromValue);
                this.currentPropertyInfo.TypeNameToWrite = typeNameToWrite;
            }
            else
            {
                string typeNameToWrite;
                if (TypeNameOracle.TryGetTypeNameFromAnnotation(enumValue, out typeNameToWrite))
                {
                    this.currentPropertyInfo.TypeNameToWrite = typeNameToWrite;
                }
            }
        }

        /// <summary>
        /// Writes a collection property.
        /// </summary>
        /// <param name="collectionValue">The collection value to be written</param>
        /// <param name="isOpenPropertyType">If the property is open.</param>
        private void WriteCollectionProperty(
            ODataCollectionValue collectionValue,
            bool isOpenPropertyType)
        {
            ResolveCollectionValueTypeName(collectionValue, isOpenPropertyType);

            this.WritePropertyTypeName();
            this.JsonWriter.WriteName(this.currentPropertyInfo.WireName);

            // passing false for 'isTopLevel' because the outer wrapping object has already been written.
            this.JsonValueSerializer.WriteCollectionValue(
                collectionValue,
                this.currentPropertyInfo.MetadataType.TypeReference,
                this.currentPropertyInfo.ValueType.TypeReference,
                this.currentPropertyInfo.IsTopLevel,
                false /*isInUri*/,
                isOpenPropertyType);
        }

        private void ResolveCollectionValueTypeName(ODataCollectionValue collectionValue, bool isOpenPropertyType)
        {
            if (this.currentPropertyInfo.ValueType == null || this.currentPropertyInfo.ValueType.TypeName != collectionValue.TypeName)
            {
                IEdmTypeReference typeFromValue = TypeNameOracle.ResolveAndValidateTypeForCollectionValue(
                    this.Model,
                    this.currentPropertyInfo.MetadataType.TypeReference,
                    collectionValue,
                    isOpenPropertyType,
                    this.WriterValidator);

                this.currentPropertyInfo.ValueType = new PropertyValueTypeInfo(collectionValue.TypeName, typeFromValue);
                this.currentPropertyInfo.TypeNameToWrite =
                    this.JsonOutputContext.TypeNameOracle.GetValueTypeNameForWriting(collectionValue,
                        this.currentPropertyInfo, isOpenPropertyType);
            }
            else
            {
                string typeNameToWrite;
                if (TypeNameOracle.TryGetTypeNameFromAnnotation(collectionValue, out typeNameToWrite))
                {
                    this.currentPropertyInfo.TypeNameToWrite = typeNameToWrite;
                }
            }
        }

        /// <summary>
        /// Writes a stream property.
        /// </summary>
        /// <param name="streamValue">The stream value to be written</param>
        /// <param name="isOpenPropertyType">If the property is open.</param>
        private void WriteStreamProperty(ODataBinaryStreamValue streamValue, bool isOpenPropertyType)
        {
            this.JsonWriter.WriteName(this.currentPropertyInfo.WireName);
            this.JsonValueSerializer.WriteStreamValue(streamValue);
        }

        /// <summary>
        /// Writes a primitive property.
        /// </summary>
        /// <param name="primitiveValue">The primitive value to be written</param>
        /// <param name="isOpenPropertyType">If the property is open.</param>
        private void WritePrimitiveProperty(
            ODataPrimitiveValue primitiveValue,
            bool isOpenPropertyType)
        {
            ResolvePrimitiveValueTypeName(primitiveValue, isOpenPropertyType);

            WriterValidationUtils.ValidatePropertyDerivedTypeConstraint(this.currentPropertyInfo);

            this.WritePropertyTypeName();
            this.JsonWriter.WriteName(this.currentPropertyInfo.WireName);
            this.JsonValueSerializer.WritePrimitiveValue(primitiveValue.Value, this.currentPropertyInfo.ValueType.TypeReference, this.currentPropertyInfo.MetadataType.TypeReference);
        }

        private void ResolvePrimitiveValueTypeName(
            ODataPrimitiveValue primitiveValue,
            bool isOpenPropertyType)
        {
            string typeName = primitiveValue.Value.GetType().Name;
            if (this.currentPropertyInfo.ValueType == null || this.currentPropertyInfo.ValueType.TypeName != typeName)
            {
                IEdmTypeReference typeFromValue = TypeNameOracle.ResolveAndValidateTypeForPrimitiveValue(primitiveValue);

                this.currentPropertyInfo.ValueType = new PropertyValueTypeInfo(typeName, typeFromValue);
                this.currentPropertyInfo.TypeNameToWrite = this.JsonOutputContext.TypeNameOracle.GetValueTypeNameForWriting(primitiveValue,
                        this.currentPropertyInfo, isOpenPropertyType);
            }
            else
            {
                string typeNameToWrite;
                if (TypeNameOracle.TryGetTypeNameFromAnnotation(primitiveValue, out typeNameToWrite))
                {
                    this.currentPropertyInfo.TypeNameToWrite = typeNameToWrite;
                }
            }
        }

        /// <summary>
        /// Writes the type name on the wire.
        /// </summary>
        private void WritePropertyTypeName()
        {
            string typeNameToWrite = this.currentPropertyInfo.TypeNameToWrite;
            if (typeNameToWrite != null)
            {
                // We write the type name as an instance annotation (named "odata.type") for top-level properties, but as a property annotation (e.g., "...@odata.type") if not top level.
                if (this.currentPropertyInfo.IsTopLevel)
                {
                    this.ODataAnnotationWriter.WriteODataTypeInstanceAnnotation(typeNameToWrite);
                }
                else
                {
                    this.ODataAnnotationWriter.WriteODataTypePropertyAnnotation(this.currentPropertyInfo.PropertyName, typeNameToWrite);
                }
            }
        }

        /// <summary>
        /// Gets OData payload kind.
        /// </summary>
        /// <returns>OData payload kind.</returns>
        private ODataPayloadKind GetPayloadKind()
        {
            return this.JsonOutputContext.MessageWriterSettings.IsIndividualProperty ? ODataPayloadKind.IndividualProperty : ODataPayloadKind.Property;
        }

        /// <summary>
        /// Gets the <see cref="ODataContextUrlInfo"/> for the top-level <paramref name="property"/>'s ODataValue.
        /// </summary>
        /// <param name="property">The top-level property.</param>
        /// <returns>The <see cref="ODataContextUrlInfo"/>.</returns>
        private ODataContextUrlInfo GetContextUrlInfo(ODataProperty property)
        {
            return ODataContextUrlInfo.Create(property.ODataValue, this.MessageWriterSettings.Version ?? ODataVersion.V4, JsonOutputContext.MessageWriterSettings.ODataUri, this.Model);
        }

        /// <summary>
        /// Validates that the given property info fulfils the expected requirements.
        /// </summary>
        /// <param name="propertyInfo">The property info to write out.</param>
        /// <param name="owningType">The owning type for the <paramref name="propertyInfo"/> or null if no metadata is available.</param>
        /// <param name="isTopLevel">true when writing a top-level property; false for nested properties.</param>
        /// <param name="duplicatePropertyNameChecker">The DuplicatePropertyNameChecker to use.</param>
        private void ValidatePropertyInfo(
            ODataPropertyInfo propertyInfo,
            IEdmStructuredType owningType,
            bool isTopLevel,
            IDuplicatePropertyNameChecker duplicatePropertyNameChecker)
        {
            WriterValidationUtils.ValidatePropertyNotNull(propertyInfo);

            string propertyName = propertyInfo.Name;

            if (this.JsonOutputContext.MessageWriterSettings.Validations != ValidationKinds.None)
            {
                WriterValidationUtils.ValidatePropertyName(propertyName);
            }

            if (!this.JsonOutputContext.PropertyCacheHandler.InResourceSetScope())
            {
                this.currentPropertyInfo = new PropertySerializationInfo(this.JsonOutputContext.Model, propertyName, owningType) { IsTopLevel = isTopLevel };
            }
            else
            {
                this.currentPropertyInfo = this.JsonOutputContext.PropertyCacheHandler.GetProperty(this.JsonOutputContext.Model, propertyName, owningType);
            }

            WriterValidationUtils.ValidatePropertyDefined(this.currentPropertyInfo, this.MessageWriterSettings.ThrowOnUndeclaredPropertyForNonOpenType);

            duplicatePropertyNameChecker.ValidatePropertyUniqueness(propertyInfo);
        }

        /// <summary>
        /// Asynchronously writes an untyped value.
        /// </summary>
        /// <param name="untypedValue">The untyped value to write.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteUntypedValueAsync(ODataUntypedValue untypedValue)
        {
            await this.JsonWriter.WriteNameAsync(this.currentPropertyInfo.WireName)
                .ConfigureAwait(false);
            await this.jsonValueSerializer.WriteUntypedValueAsync(untypedValue)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously writes an <see cref="System.Text.Json.JsonElement"/> value.
        /// </summary>
        /// <param name="jsonElementValue">The value to be written.</param>
        private async Task WriteJsonElementPropertyAsync(ODataJsonElementValue jsonElementValue)
        {
            await this.JsonWriter.WriteNameAsync(this.currentPropertyInfo.WireName)
                .ConfigureAwait(false);
            await this.JsonWriter.WriteValueAsync(jsonElementValue.Value)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronosly writes a stream reference value.
        /// </summary>
        /// <param name="streamInfo">The stream reference value.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="metadataBuilder">The metadata builder for the resource.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteStreamValueAsync(
            IODataStreamReferenceInfo streamInfo,
            string propertyName,
            ODataResourceMetadataBuilder metadataBuilder)
        {
            WriterValidationUtils.ValidateStreamPropertyInfo(streamInfo, currentPropertyInfo.MetadataType.EdmProperty, propertyName, this.WritingResponse);
            await this.WriteStreamInfoAsync(propertyName, streamInfo)
                .ConfigureAwait(false);
            if (metadataBuilder != null)
            {
                metadataBuilder.MarkStreamPropertyProcessed(propertyName);
            }
        }

        /// <summary>
        /// Asynchronously writes instance annotation for property
        /// </summary>
        /// <param name="property">The property to handle.</param>
        /// <param name="isTopLevel">If writing top level property.</param>
        /// <param name="isUndeclaredProperty">If writing an undeclared property.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private Task WriteInstanceAnnotationAsync(ODataPropertyInfo property, bool isTopLevel, bool isUndeclaredProperty)
        {
            if (property.InstanceAnnotations.Count != 0)
            {
                if (isTopLevel)
                {
                    return this.InstanceAnnotationWriter.WriteInstanceAnnotationsAsync(property.InstanceAnnotations);
                }
                else
                {
                    return this.InstanceAnnotationWriter.WriteInstanceAnnotationsAsync(property.InstanceAnnotations, property.Name, isUndeclaredProperty);
                }
            }

            return TaskUtils.CompletedTask;
        }

        /// <summary>
        /// Writes odata type annotation for property
        /// </summary>
        /// <param name="property">The property to handle.</param>
        /// <param name="isTopLevel">If writing top level property.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private Task WriteODataTypeAnnotationAsync(ODataPropertyInfo property, bool isTopLevel)
        {
            if (property.TypeAnnotation != null && property.TypeAnnotation.TypeName != null)
            {
                string typeName = property.TypeAnnotation.TypeName;
                IEdmPrimitiveType primitiveType = EdmCoreModel.Instance.FindType(typeName) as IEdmPrimitiveType;
                if (primitiveType == null ||
                    (primitiveType.PrimitiveKind != EdmPrimitiveTypeKind.String &&
                    primitiveType.PrimitiveKind != EdmPrimitiveTypeKind.Decimal &&
                    primitiveType.PrimitiveKind != EdmPrimitiveTypeKind.Boolean))
                {
                    if (isTopLevel)
                    {
                        return this.ODataAnnotationWriter.WriteODataTypeInstanceAnnotationAsync(typeName);
                    }
                    else
                    {
                        return this.ODataAnnotationWriter.WriteODataTypePropertyAnnotationAsync(property.Name, typeName);

                    }
                }
            }

            return TaskUtils.CompletedTask;
        }

        /// <summary>
        /// Writes stream property information.
        /// </summary>
        /// <param name="propertyName">The name of the stream property to write.</param>
        /// <param name="streamInfo">The stream reference value to be written</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteStreamInfoAsync(string propertyName, IODataStreamReferenceInfo streamInfo)
        {
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "!string.IsNullOrEmpty(propertyName)");
            Debug.Assert(streamInfo != null, "streamReferenceValue != null");

            Uri mediaEditLink = streamInfo.EditLink;
            if (mediaEditLink != null)
            {
                await this.ODataAnnotationWriter.WritePropertyAnnotationNameAsync(propertyName, ODataAnnotationNames.ODataMediaEditLink)
                    .ConfigureAwait(false);
                await this.JsonWriter.WriteValueAsync(this.UriToString(mediaEditLink))
                    .ConfigureAwait(false);
            }

            Uri mediaReadLink = streamInfo.ReadLink;
            if (mediaReadLink != null)
            {
                await this.ODataAnnotationWriter.WritePropertyAnnotationNameAsync(propertyName, ODataAnnotationNames.ODataMediaReadLink)
                    .ConfigureAwait(false);
                await this.JsonWriter.WriteValueAsync(this.UriToString(mediaReadLink))
                    .ConfigureAwait(false);
            }

            string mediaContentType = streamInfo.ContentType;
            if (mediaContentType != null)
            {
                await this.ODataAnnotationWriter.WritePropertyAnnotationNameAsync(propertyName, ODataAnnotationNames.ODataMediaContentType)
                    .ConfigureAwait(false);
                await this.JsonWriter.WriteValueAsync(mediaContentType)
                    .ConfigureAwait(false);
            }

            string mediaETag = streamInfo.ETag;
            if (mediaETag != null)
            {
                await this.ODataAnnotationWriter.WritePropertyAnnotationNameAsync(propertyName, ODataAnnotationNames.ODataMediaETag)
                    .ConfigureAwait(false);
                await this.JsonWriter.WriteValueAsync(mediaETag)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously writes a null property.
        /// </summary>
        /// <param name="property">The property to write out.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteNullPropertyAsync(
            ODataPropertyInfo property)
        {
            this.WriterValidator.ValidateNullPropertyValue(
                this.currentPropertyInfo.MetadataType.TypeReference, property.Name,
                this.currentPropertyInfo.IsTopLevel, this.Model);

            if (this.currentPropertyInfo.IsTopLevel)
            {
                if (this.JsonOutputContext.MessageWriterSettings.LibraryCompatibility.HasFlag(ODataLibraryCompatibility.WriteTopLevelODataNullAnnotation) && 
                    this.JsonOutputContext.MessageWriterSettings.Version < ODataVersion.V401)
                {
                    // The 6.x library used an OData 3.0 protocol element in this case: @odata.null=true
                    await this.ODataAnnotationWriter.WriteInstanceAnnotationNameAsync(ODataAnnotationNames.ODataNull)
                        .ConfigureAwait(false);
                    await this.JsonWriter.WriteValueAsync(true)
                        .ConfigureAwait(false);
                }
                else
                {
                    // From the spec:
                    // 11.2.3 Requesting Individual Properties
                    // ...
                    // If the property is single-valued and has the null value, the service responds with 204 No Content.
                    // ...
                    throw new ODataException(SRResources.ODataMessageWriter_CannotWriteTopLevelNull);
                }
            }
            else
            {
                await this.JsonWriter.WriteNameAsync(property.Name)
                    .ConfigureAwait(false);
                await this.JsonValueSerializer.WriteNullValueAsync()
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously writes a resource property.
        /// </summary>
        /// <param name="property">The property to write out.</param>
        /// <param name="resourceValue">The resource value to be written</param>
        /// <param name="isOpenPropertyType">If the property is open.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteResourcePropertyAsync(
            ODataProperty property,
            ODataResourceValue resourceValue,
            bool isOpenPropertyType)
        {
            Debug.Assert(!this.currentPropertyInfo.IsTopLevel, "Resource property should not be top level");
            await this.JsonWriter.WriteNameAsync(property.Name)
                .ConfigureAwait(false);

            IDuplicatePropertyNameChecker duplicatePropertyNameChecker = this.GetDuplicatePropertyNameChecker();

            await this.JsonValueSerializer.WriteResourceValueAsync(
                resourceValue,
                this.currentPropertyInfo.MetadataType.TypeReference,
                isOpenPropertyType,
                duplicatePropertyNameChecker).ConfigureAwait(false);

            this.ReturnDuplicatePropertyNameChecker(duplicatePropertyNameChecker);
        }

        /// <summary>
        /// Asynchronously writes an enum property.
        /// </summary>
        /// <param name="enumValue">The enum value to be written.</param>
        /// <param name="isOpenPropertyType">If the property is open.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteEnumPropertyAsync(
            ODataEnumValue enumValue,
            bool isOpenPropertyType)
        {
            ResolveEnumValueTypeName(enumValue, isOpenPropertyType);

            await this.WritePropertyTypeNameAsync().ConfigureAwait(false);
            await this.JsonWriter.WriteNameAsync(this.currentPropertyInfo.WireName)
                .ConfigureAwait(false);
            await this.JsonValueSerializer.WriteEnumValueAsync(enumValue, this.currentPropertyInfo.MetadataType.TypeReference)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously writes a collection property.
        /// </summary>
        /// <param name="collectionValue">The collection value to be written</param>
        /// <param name="isOpenPropertyType">If the property is open.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteCollectionPropertyAsync(
            ODataCollectionValue collectionValue,
            bool isOpenPropertyType)
        {
            ResolveCollectionValueTypeName(collectionValue, isOpenPropertyType);

            await this.WritePropertyTypeNameAsync()
                .ConfigureAwait(false);
            await this.JsonWriter.WriteNameAsync(this.currentPropertyInfo.WireName)
                .ConfigureAwait(false);

            // passing false for 'isTopLevel' because the outer wrapping object has already been written.
            await this.JsonValueSerializer.WriteCollectionValueAsync(
                collectionValue,
                this.currentPropertyInfo.MetadataType.TypeReference,
                this.currentPropertyInfo.ValueType.TypeReference,
                this.currentPropertyInfo.IsTopLevel,
                false /*isInUri*/,
                isOpenPropertyType).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously writes a stream property.
        /// </summary>
        /// <param name="streamValue">The stream value to be written</param>
        /// <param name="isOpenPropertyType">If the property is open.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteStreamPropertyAsync(ODataBinaryStreamValue streamValue, bool isOpenPropertyType)
        {
            await this.JsonWriter.WriteNameAsync(this.currentPropertyInfo.WireName)
                .ConfigureAwait(false);
            await this.JsonValueSerializer.WriteStreamValueAsync(streamValue)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously writes a primitive property.
        /// </summary>
        /// <param name="primitiveValue">The primitive value to be written</param>
        /// <param name="isOpenPropertyType">If the property is open.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WritePrimitivePropertyAsync(
            ODataPrimitiveValue primitiveValue,
            bool isOpenPropertyType)
        {
            ResolvePrimitiveValueTypeName(primitiveValue, isOpenPropertyType);

            WriterValidationUtils.ValidatePropertyDerivedTypeConstraint(this.currentPropertyInfo);

            await this.WritePropertyTypeNameAsync().ConfigureAwait(false);
            await this.JsonWriter.WriteNameAsync(this.currentPropertyInfo.WireName)
                .ConfigureAwait(false);
            await this.JsonValueSerializer.WritePrimitiveValueAsync(primitiveValue.Value, this.currentPropertyInfo.ValueType.TypeReference, this.currentPropertyInfo.MetadataType.TypeReference)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously writes the type name on the wire.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private Task WritePropertyTypeNameAsync()
        {
            string typeNameToWrite = this.currentPropertyInfo.TypeNameToWrite;
            if (typeNameToWrite != null)
            {
                // We write the type name as an instance annotation (named "odata.type") for top-level properties, but as a property annotation (e.g., "...@odata.type") if not top level.
                if (this.currentPropertyInfo.IsTopLevel)
                {
                    return this.ODataAnnotationWriter.WriteODataTypeInstanceAnnotationAsync(typeNameToWrite);
                }
                else
                {
                    return this.ODataAnnotationWriter.WriteODataTypePropertyAnnotationAsync(this.currentPropertyInfo.PropertyName, typeNameToWrite);
                }
            }

            return TaskUtils.CompletedTask;
        }
    }
}
