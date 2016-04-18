//---------------------------------------------------------------------
// <copyright file="ODataJsonLightReader.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData.Core.JsonLight
{
    #region Namespaces
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
#if ODATALIB_ASYNC
    using System.Threading.Tasks;
#endif
    using Microsoft.OData.Core.UriParser.Semantic;
    using Microsoft.OData.Edm;
    using Microsoft.OData.Core.Evaluation;
    using Microsoft.OData.Core.Json;
    using Microsoft.OData.Core.Metadata;
    using ODataErrorStrings = Microsoft.OData.Core.Strings;
    #endregion Namespaces

    /// <summary>
    /// OData reader for the JsonLight format.
    /// </summary>
    internal sealed class ODataJsonLightReader : ODataReaderCoreAsync
    {
        /// <summary>The input to read the payload from.</summary>
        private readonly ODataJsonLightInputContext jsonLightInputContext;

        /// <summary>The resource and resource set deserializer to read input with.</summary>
        private readonly ODataJsonLightResourceDeserializer jsonLightResourceDeserializer;

        /// <summary>The scope associated with the top level of this payload.</summary>
        private readonly JsonLightTopLevelScope topLevelScope;

        /// <summary>true if the reader is created for reading parameter; false otherwise.</summary>
        private readonly bool readingParameter;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="jsonLightInputContext">The input to read the payload from.</param>
        /// <param name="navigationSource">The navigation source we are going to read entities for.</param>
        /// <param name="expectedEntityType">The expected entity type for the resource to be read (in case of resource reader) or entries in the resource set to be read (in case of resource set reader).</param>
        /// <param name="readingFeed">true if the reader is created for reading a resource set; false when it is created for reading a resource.</param>
        /// <param name="readingParameter">true if the reader is created for reading a parameter; false otherwise.</param>
        /// <param name="readingDelta">true if the reader is created for reading expanded navigation property in delta response; false otherwise.</param>
        /// <param name="listener">If not null, the Json reader will notify the implementer of the interface of relevant state changes in the Json reader.</param>
        internal ODataJsonLightReader(
            ODataJsonLightInputContext jsonLightInputContext,
            IEdmNavigationSource navigationSource,
            IEdmEntityType expectedEntityType,
            bool readingFeed,
            bool readingParameter = false,
            bool readingDelta = false,
            IODataReaderWriterListener listener = null)
            : base(jsonLightInputContext, readingFeed, readingDelta, listener)
        {
            Debug.Assert(jsonLightInputContext != null, "jsonLightInputContext != null");
            Debug.Assert(
                expectedEntityType == null || jsonLightInputContext.Model.IsUserModel(),
                "If the expected type is specified we need model as well. We should have verified that by now.");

            this.jsonLightInputContext = jsonLightInputContext;
            this.jsonLightResourceDeserializer = new ODataJsonLightResourceDeserializer(jsonLightInputContext);
            this.readingParameter = readingParameter;
            this.topLevelScope = new JsonLightTopLevelScope(navigationSource, expectedEntityType);
            this.EnterScope(this.topLevelScope);
        }

        /// <summary>
        /// Returns the current resource state.
        /// </summary>
        private IODataJsonLightReaderResourceState CurrentResourceState
        {
            get
            {
                Debug.Assert(
                    this.State == ODataReaderState.ResourceStart || this.State == ODataReaderState.ResourceEnd,
                    "This property can only be accessed in the EntryStart or EntryEnd scope.");
                return (IODataJsonLightReaderResourceState)this.CurrentScope;
            }
        }

        /// <summary>
        /// Returns current scope cast to JsonLightResourceSetScope
        /// </summary>
        private JsonLightResourceSetScope CurrentJsonLightResourceSetScope
        {
            get
            {
                return ((JsonLightResourceSetScope)this.CurrentScope);
            }
        }

        /// <summary>
        /// Returns current scope cast to JsonLightNavigationLinkScope
        /// </summary>
        private JsonLightNavigationLinkScope CurrentJsonLightNavigationLinkScope
        {
            get
            {
                return ((JsonLightNavigationLinkScope)this.CurrentScope);
            }
        }

        /// <summary>
        /// Implementation of the reader logic when in state 'Start'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.None:      assumes that the JSON reader has not been used yet when not reading a nested payload.
        /// Post-Condition: when reading a resource set:    the reader is positioned on the first item in the resource set or the end array node of an empty resource set
        ///                 when reading a resource:  the first node of the first navigation link value, null for a null expanded link or an end object 
        ///                                         node if there are no navigation links.
        /// </remarks>
        protected override bool ReadAtStartImplementation()
        {
            Debug.Assert(this.State == ODataReaderState.Start, "this.State == ODataReaderState.Start");
            Debug.Assert(this.IsReadingNestedPayload || this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.None, "Pre-Condition: expected JsonNodeType.None when not reading a nested payload.");

            DuplicatePropertyNamesChecker duplicatePropertyNamesChecker =
                this.jsonLightInputContext.CreateDuplicatePropertyNamesChecker();

            // Position the reader on the first node depending on whether we are reading a nested payload or not.
            ODataPayloadKind payloadKind = this.ReadingFeed ? ODataPayloadKind.ResourceSet : ODataPayloadKind.Resource;
            this.jsonLightResourceDeserializer.ReadPayloadStart(
                payloadKind,
                duplicatePropertyNamesChecker,
                this.IsReadingNestedPayload,
                /*allowEmptyPayload*/false);

            return this.ReadAtStartImplementationSynchronously(duplicatePropertyNamesChecker);
        }

#if ODATALIB_ASYNC
        /// <summary>
        /// Implementation of the reader logic when in state 'Start'.
        /// </summary>
        /// <returns>A task which returns true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.None:      assumes that the JSON reader has not been used yet when not reading a nested payload.
        /// Post-Condition: when reading a resource set:    the reader is positioned on the first item in the resource set or the end array node of an empty resource set
        ///                 when reading a resource:  the first node of the first navigation link value, null for a null expanded link or an end object 
        ///                                         node if there are no navigation links.
        /// </remarks>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "API design calls for a bool being returned from the task here.")]
        protected override Task<bool> ReadAtStartImplementationAsync()
        {
            Debug.Assert(this.State == ODataReaderState.Start, "this.State == ODataReaderState.Start");
            Debug.Assert(this.IsReadingNestedPayload || this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.None, "Pre-Condition: expected JsonNodeType.None when not reading a nested payload.");

            DuplicatePropertyNamesChecker duplicatePropertyNamesChecker =
                this.jsonLightInputContext.CreateDuplicatePropertyNamesChecker();

            // Position the reader on the first node depending on whether we are reading a nested payload or not.
            ODataPayloadKind payloadKind = this.ReadingFeed ? ODataPayloadKind.ResourceSet : ODataPayloadKind.Resource;
            return this.jsonLightResourceDeserializer.ReadPayloadStartAsync(
                payloadKind,
                duplicatePropertyNamesChecker,
                this.IsReadingNestedPayload,
                /*allowEmptyPayload*/false)

                .FollowOnSuccessWith(t =>
                    this.ReadAtStartImplementationSynchronously(duplicatePropertyNamesChecker));
        }
#endif

        /// <summary>
        /// Implementation of the reader logic when in state 'ResourceSetStart'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  Any start node            - The first resource in the resource set
        ///                 JsonNodeType.EndArray     - The end of the resource set
        /// Post-Condition: The reader is positioned over the StartObject node of the first resource in the resource set or 
        ///                 on the node following the resource set end in case of an empty resource set
        /// </remarks>
        protected override bool ReadAtResourceSetStartImplementation()
        {
            return this.ReadAtResourceSetStartImplementationSynchronously();
        }

#if ODATALIB_ASYNC
        /// <summary>
        /// Implementation of the reader logic when in state 'ResourceSetStart'.
        /// </summary>
        /// <returns>A task which returns true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  Any start node            - The first resource in the resource set
        ///                 JsonNodeType.EndArray     - The end of the resource set
        /// Post-Condition: The reader is positioned over the StartObject node of the first resource in the resource set or 
        ///                 on the node following the resource set end in case of an empty resource set
        /// </remarks>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "API design calls for a bool being returned from the task here.")]
        protected override Task<bool> ReadAtResourceSetStartImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation<bool>(this.ReadAtResourceSetStartImplementationSynchronously);
        }
#endif

        /// <summary>
        /// Implementation of the reader logic when in state 'ResourceSetEnd'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition: JsonNodeType.Property        if the resource set has further instance or property annotations after the resource set property
        ///                JsonNodeType.EndObject       if the resource set has no further instance or property annotations after the resource set property
        /// Post-Condition: JsonNodeType.EndOfInput     for a top-level resource set when not reading a nested payload
        ///                 JsonNodeType.Property       more properties exist on the owning resource after the expanded link containing the resource set
        ///                 JsonNodeType.EndObject      no further properties exist on the owning resource after the expanded link containing the resource set
        ///                 JsonNodeType.EndArray       end of expanded link in request, in this case the resource set doesn't actually own the array object and it won't read it.
        ///                 Any                         in case of expanded resource set in request, this might be the next item in the expanded array, which is not a resource
        /// </remarks>
        protected override bool ReadAtResourceSetEndImplementation()
        {
            return this.ReadAtResourceSetEndImplementationSynchronously();
        }

#if ODATALIB_ASYNC
        /// <summary>
        /// Implementation of the reader logic when in state 'ResourceSetEnd'.
        /// </summary>
        /// <returns>A task which returns true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition: JsonNodeType.Property        if the resource set has further instance or property annotations after the resource set property
        ///                JsonNodeType.EndObject       if the resource set has no further instance or property annotations after the resource set property
        /// Post-Condition: JsonNodeType.EndOfInput     for a top-level resource set when not reading a nested payload
        ///                 JsonNodeType.Property       more properties exist on the owning resource after the expanded link containing the resource set
        ///                 JsonNodeType.EndObject      no further properties exist on the owning resource after the expanded link containing the resource set
        ///                 JsonNodeType.EndArray       end of expanded link in request, in this case the resource set doesn't actually own the array object and it won't read it.
        ///                 Any                         in case of expanded resource set in request, this might be the next item in the expanded array, which is not a resource
        /// </remarks>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "API design calls for a bool being returned from the task here.")]
        protected override Task<bool> ReadAtResourceSetEndImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation<bool>(this.ReadAtResourceSetEndImplementationSynchronously);
        }
#endif

        /// <summary>
        /// Implementation of the reader logic when in state 'EntryStart'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.StartObject            Start of the expanded resource of the navigation link to read next.
        ///                 JsonNodeType.StartArray             Start of the expanded resource set of the navigation link to read next.
        ///                 JsonNodeType.PrimitiveValue (null)  Expanded null resource of the navigation link to read next.
        ///                 JsonNodeType.Property               The next property after a deferred link or entity reference link
        ///                 JsonNodeType.EndObject              If no (more) properties exist in the resource's content
        /// Post-Condition: JsonNodeType.StartObject            Start of the expanded resource of the navigation link to read next.
        ///                 JsonNodeType.StartArray             Start of the expanded resource set of the navigation link to read next.
        ///                 JsonNodeType.PrimitiveValue (null)  Expanded null resource of the navigation link to read next.
        ///                 JsonNodeType.Property               The next property after a deferred link or entity reference link
        ///                 JsonNodeType.EndObject              If no (more) properties exist in the resource's content
        /// </remarks>
        protected override bool ReadAtResourceStartImplementation()
        {
            return this.ReadAtResourceStartImplementationSynchronously();
        }

#if ODATALIB_ASYNC
        /// <summary>
        /// Implementation of the reader logic when in state 'EntryStart'.
        /// </summary>
        /// <returns>A task which returns true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.StartObject            Start of the expanded resource of the navigation link to read next.
        ///                 JsonNodeType.StartArray             Start of the expanded resource set of the navigation link to read next.
        ///                 JsonNodeType.PrimitiveValue (null)  Expanded null resource of the navigation link to read next.
        ///                 JsonNodeType.Property               The next property after a deferred link or entity reference link
        ///                 JsonNodeType.EndObject              If no (more) properties exist in the resource's content
        /// Post-Condition: JsonNodeType.StartObject            Start of the expanded resource of the navigation link to read next.
        ///                 JsonNodeType.StartArray             Start of the expanded resource set of the navigation link to read next.
        ///                 JsonNodeType.PrimitiveValue (null)  Expanded null resource of the navigation link to read next.
        ///                 JsonNodeType.Property               The next property after a deferred link or entity reference link
        ///                 JsonNodeType.EndObject              If no (more) properties exist in the resource's content
        /// </remarks>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "API design calls for a bool being returned from the task here.")]
        protected override Task<bool> ReadAtResourceStartImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation<bool>(this.ReadAtResourceStartImplementationSynchronously);
        }
#endif

        /// <summary>
        /// Implementation of the reader logic when in state 'EntryEnd'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.EndObject              end of object of the resource
        ///                 JsonNodeType.PrimitiveValue (null)  end of null expanded resource
        /// Post-Condition: The reader is positioned on the first node after the resource's end-object node
        /// </remarks>
        protected override bool ReadAtResourceEndImplementation()
        {
            return this.ReadAtResourceEndImplementationSynchronously();
        }

#if ODATALIB_ASYNC
        /// <summary>
        /// Implementation of the reader logic when in state 'EntryEnd'.
        /// </summary>
        /// <returns>A task which returns true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.EndObject              end of object of the resource
        ///                 JsonNodeType.PrimitiveValue (null)  end of null expanded resource
        /// Post-Condition: The reader is positioned on the first node after the resource's end-object node
        /// </remarks>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "API design calls for a bool being returned from the task here.")]
        protected override Task<bool> ReadAtResourceEndImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation<bool>(this.ReadAtResourceEndImplementationSynchronously);
        }
#endif

        /// <summary>
        /// Implementation of the reader logic when in state 'NavigationLinkStart'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.StartObject            start of an expanded resource
        ///                 JsonNodeType.StartArray             start of an expanded resource set
        ///                 JsonNodeType.PrimitiveValue (null)  expanded null resource
        ///                 JsonNodeType.Property               deferred link with more properties in owning resource
        ///                 JsonNodeType.EndObject              deferred link as last property of the owning resource
        /// Post-Condition: JsonNodeType.StartArray:            start of expanded resource
        ///                 JsonNodeType.StartObject            start of expanded resource set
        ///                 JsonNodeType.PrimitiveValue (null)  expanded null resource
        ///                 JsonNodeType.Property               deferred link with more properties in owning resource
        ///                 JsonNodeType.EndObject              deferred link as last property of the owning resource
        /// </remarks>
        protected override bool ReadAtNavigationLinkStartImplementation()
        {
            return this.ReadAtNavigationLinkStartImplementationSynchronously();
        }

#if ODATALIB_ASYNC
        /// <summary>
        /// Implementation of the reader logic when in state 'NavigationLinkStart'.
        /// </summary>
        /// <returns>A task which returns true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.StartObject            start of an expanded resource
        ///                 JsonNodeType.StartArray             start of an expanded resource set
        ///                 JsonNodeType.PrimitiveValue (null)  expanded null resource
        ///                 JsonNodeType.Property               deferred link with more properties in owning resource
        ///                 JsonNodeType.EndObject              deferred link as last property of the owning resource
        /// Post-Condition: JsonNodeType.StartArray:            start of expanded resource
        ///                 JsonNodeType.StartObject            start of expanded resource set
        ///                 JsonNodeType.PrimitiveValue (null)  expanded null resource
        ///                 JsonNodeType.Property               deferred link with more properties in owning resource
        ///                 JsonNodeType.EndObject              deferred link as last property of the owning resource
        /// </remarks>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "API design calls for a bool being returned from the task here.")]
        protected override Task<bool> ReadAtNavigationLinkStartImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation<bool>(this.ReadAtNavigationLinkStartImplementationSynchronously);
        }
#endif

        /// <summary>
        /// Implementation of the reader logic when in state 'NavigationLinkEnd'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.EndObject:         navigation link is last property in owning resource
        ///                 JsonNodeType.Property:          there are more properties after the navigation link in the owning resource
        /// Post-Condition: JsonNodeType.StartObject        start of the expanded resource navigation link to read next
        ///                 JsonNodeType.StartArray         start of the expanded resource set navigation link to read next
        ///                 JsonNoteType.Primitive (null)   expanded null resource navigation link to read next
        ///                 JsonNoteType.Property           property after deferred link or entity reference link
        ///                 JsonNodeType.EndObject          end of the parent resource
        /// </remarks>
        protected override bool ReadAtNavigationLinkEndImplementation()
        {
            return this.ReadAtNavigationLinkEndImplementationSynchronously();
        }

#if ODATALIB_ASYNC
        /// <summary>
        /// Implementation of the reader logic when in state 'NavigationLinkEnd'.
        /// </summary>
        /// <returns>A task which returns true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.EndObject:         navigation link is last property in owning resource
        ///                 JsonNodeType.Property:          there are more properties after the navigation link in the owning resource
        /// Post-Condition: JsonNodeType.StartObject        start of the expanded resource navigation link to read next
        ///                 JsonNodeType.StartArray         start of the expanded resource set navigation link to read next
        ///                 JsonNoteType.Primitive (null)   expanded null resource navigation link to read next
        ///                 JsonNoteType.Property           property after deferred link or entity reference link
        ///                 JsonNodeType.EndObject          end of the parent resource
        /// </remarks>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "API design calls for a bool being returned from the task here.")]
        protected override Task<bool> ReadAtNavigationLinkEndImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation<bool>(this.ReadAtNavigationLinkEndImplementationSynchronously);
        }
#endif

        /// <summary>
        /// Implementation of the reader logic when in state 'EntityReferenceLink'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// This method doesn't move the reader
        /// Pre-Condition:  JsonNodeType.EndObject:         expanded link property is last property in owning resource
        ///                 JsonNodeType.Property:          there are more properties after the expanded link property in the owning resource
        ///                 Any:                            expanded collection link - the node after the entity reference link.
        /// Post-Condition: JsonNodeType.EndObject:         expanded link property is last property in owning resource
        ///                 JsonNodeType.Property:          there are more properties after the expanded link property in the owning resource
        ///                 Any:                            expanded collection link - the node after the entity reference link.
        /// </remarks>
        protected override bool ReadAtEntityReferenceLink()
        {
            return this.ReadAtEntityReferenceLinkSynchronously();
        }

#if ODATALIB_ASYNC
        /// <summary>
        /// Implementation of the reader logic when in state 'EntityReferenceLink'.
        /// </summary>
        /// <returns>A task which returns true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// This method doesn't move the reader
        /// Pre-Condition:  JsonNodeType.EndObject:         expanded link property is last property in owning resource
        ///                 JsonNodeType.Property:          there are more properties after the expanded link property in the owning resource
        ///                 Any:                            expanded collection link - the node after the entity reference link.
        /// Post-Condition: JsonNodeType.EndObject:         expanded link property is last property in owning resource
        ///                 JsonNodeType.Property:          there are more properties after the expanded link property in the owning resource
        ///                 Any:                            expanded collection link - the node after the entity reference link.
        /// </remarks>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "API design calls for a bool being returned from the task here.")]
        protected override Task<bool> ReadAtEntityReferenceLinkAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation<bool>(this.ReadAtEntityReferenceLinkSynchronously);
        }
#endif

        /// <summary>
        /// Implementation of the reader logic when in state 'Start'.
        /// </summary>
        /// <param name="duplicatePropertyNamesChecker">The duplicate property names checker to use for the top-level scope.</param>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.None:      assumes that the JSON reader has not been used yet when not reading a nested payload.
        /// Post-Condition: when reading a resource set:    the reader is positioned on the first item in the resource set or the end array node of an empty resource set
        ///                 when reading a resource:  the first node of the first navigation link value, null for a null expanded link or an end object 
        ///                                         node if there are no navigation links.
        /// </remarks>
        private bool ReadAtStartImplementationSynchronously(DuplicatePropertyNamesChecker duplicatePropertyNamesChecker)
        {
            Debug.Assert(duplicatePropertyNamesChecker != null, "duplicatePropertyNamesChecker != null");

            // For nested payload (e.g., expanded resource set or resource in delta $entity payload),
            // we usually don't have a context URL for the resource set or resource:
            // {
            //   "@odata.context":"...", <--- this context URL is for delta entity only
            //   "value": [
            //     {
            //       ...
            //       "NavigationProperty": <--- usually we don't have a context URL for this
            //       [ <--- nested payload start
            //         {...}
            //       ] <--- nested payload end
            //     }
            //    ]
            // }
            //
            // The consequence is that the resource we read out from a nested payload doesn't
            // have an entity metadata builder thus you cannot compute read link, edit link,
            // etc. from the resource object.
            if (this.jsonLightInputContext.ReadingResponse && !this.IsReadingNestedPayload)
            {
                Debug.Assert(this.jsonLightResourceDeserializer.ContextUriParseResult != null, "We should have failed by now if we don't have parse results for context URI.");

                // Validate the context URI parsed from the payload against the entity set and entity type passed in through the API.
                ReaderValidationUtils.ValidateFeedOrEntryContextUri(this.jsonLightResourceDeserializer.ContextUriParseResult, this.CurrentScope, true);
            }

            // Get the $select query option from the metadata link, if we have one.
            string selectQueryOption = this.jsonLightResourceDeserializer.ContextUriParseResult == null
                ? null
                : this.jsonLightResourceDeserializer.ContextUriParseResult.SelectQueryOption;

            SelectedPropertiesNode selectedProperties = SelectedPropertiesNode.Create(selectQueryOption);

            if (this.ReadingFeed)
            {
                ODataResourceSet resourceSet = new ODataResourceSet();

                // Store the duplicate property names checker to use it later when reading the resource set end 
                // (since we allow resource set-related annotations to appear after the resource set's data).
                this.topLevelScope.DuplicatePropertyNamesChecker = duplicatePropertyNamesChecker;

                bool isReordering = this.jsonLightInputContext.JsonReader is ReorderingJsonReader;
                if (!this.IsReadingNestedPayload)
                {
                    // Skip top-level resource set annotations for nested feeds.
                    this.jsonLightResourceDeserializer.ReadTopLevelFeedAnnotations(
                        resourceSet, duplicatePropertyNamesChecker, /*forResourceSetStart*/true, /*readAllFeedProperties*/isReordering);
                }

                this.ReadResourceSetStart(resourceSet, selectedProperties);
                return true;
            }

            this.ReadEntryStart(duplicatePropertyNamesChecker, selectedProperties);
            return true;
        }

        /// <summary>
        /// Implementation of the reader logic when in state 'ResourceSetStart'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  Any start node            - The first resource in the resource set
        ///                 JsonNodeType.EndArray     - The end of the resource set
        /// Post-Condition: The reader is positioned over the StartObject node of the first resource in the resource set or 
        ///                 on the node following the resource set end in case of an empty resource set
        /// </remarks>
        private bool ReadAtResourceSetStartImplementationSynchronously()
        {
            Debug.Assert(this.State == ODataReaderState.ResourceSetStart, "this.State == ODataReaderState.ResourceSetStart");
            this.jsonLightResourceDeserializer.AssertJsonCondition(JsonNodeType.EndArray, JsonNodeType.PrimitiveValue, JsonNodeType.StartObject, JsonNodeType.StartArray);

            // figure out whether the resource set contains entries or not
            switch (this.jsonLightResourceDeserializer.JsonReader.NodeType)
            {
                // we are at the beginning of a resource
                // The expected type for a resource in the resource set is the same as for the resource set itself.
                case JsonNodeType.StartObject:
                    // First resource in the resource set
                    this.ReadEntryStart(/*duplicatePropertyNamesChecker*/ null, this.CurrentJsonLightResourceSetScope.SelectedProperties);
                    break;
                case JsonNodeType.EndArray:
                    // End of the resource set
                    this.ReadFeedEnd();
                    break;
                default:
                    throw new ODataException(ODataErrorStrings.ODataJsonReader_CannotReadEntriesOfFeed(this.jsonLightResourceDeserializer.JsonReader.NodeType));
            }

            return true;
        }

        /// <summary>
        /// Implementation of the reader logic when in state 'ResourceSetEnd'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition: JsonNodeType.Property        if the resource set has further instance or property annotations after the resource set property
        ///                JsonNodeType.EndObject       if the resource set has no further instance or property annotations after the resource set property
        /// Post-Condition: JsonNodeType.EndOfInput     for a top-level resource set when not reading a nested payload
        ///                 JsonNodeType.Property       more properties exist on the owning resource after the expanded link containing the resource set
        ///                 JsonNodeType.EndObject      no further properties exist on the owning resource after the expanded link containing the resource set
        ///                 JsonNodeType.EndArray       end of expanded link in request, in this case the resource set doesn't actually own the array object and it won't read it.
        ///                 Any                         in case of expanded resource set in request, this might be the next item in the expanded array, which is not a resource
        /// </remarks>
        private bool ReadAtResourceSetEndImplementationSynchronously()
        {
            Debug.Assert(this.State == ODataReaderState.ResourceSetEnd, "this.State == ODataReaderState.ResourceSetEnd");
            Debug.Assert(
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.Property ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.EndObject ||
                !this.IsTopLevel && !this.jsonLightInputContext.ReadingResponse,
                "Pre-Condition: expected JsonNodeType.EndObject or JsonNodeType.Property");

            bool isTopLevelFeed = this.IsTopLevel;

            this.PopScope(ODataReaderState.ResourceSetEnd);

            // When we finish a top-level resource set in a nested payload (inside parameter or delta payload),
            // we can directly turn the reader into Completed state because we don't have any JSON token
            // (e.g., EndObject in a normal resource set payload) left in the stream.
            //
            // Nested resource set payload:
            // [
            //   {...},
            //   ...
            // ]
            // EOF <--- current reader position
            //
            // Normal resource set payload:
            // {
            //   "@odata.context":"...",
            //   ...,
            //   "value": [
            //     {...},
            //     ...
            //   ],
            //   "@odata.nextLink":"..."
            // } <--- current reader position
            // EOF
            if (this.IsReadingNestedPayload && isTopLevelFeed)
            {
                // replace the 'Start' scope with the 'Completed' scope
                this.ReplaceScope(ODataReaderState.Completed);
                return false;
            }

            if (isTopLevelFeed)
            {
                Debug.Assert(this.State == ODataReaderState.Start, "this.State == ODataReaderState.Start");

                // Read the end-object node of the resource set object and position the reader on the next input node
                // This can hit the end of the input.
                this.jsonLightResourceDeserializer.JsonReader.Read();

                // read the end-of-payload
                this.jsonLightResourceDeserializer.ReadPayloadEnd(this.IsReadingNestedPayload);

                // replace the 'Start' scope with the 'Completed' scope
                this.ReplaceScope(ODataReaderState.Completed);
                return false;
            }
            else
            {
                // finish reading the expanded link
                this.ReadExpandedNavigationLinkEnd(true);
                return true;
            }
        }

        /// <summary>
        /// Implementation of the reader logic when in state 'EntryStart'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.StartObject            Start of the expanded resource of the navigation link to read next.
        ///                 JsonNodeType.StartArray             Start of the expanded resource set of the navigation link to read next.
        ///                 JsonNodeType.PrimitiveValue (null)  Expanded null resource of the navigation link to read next.
        ///                 JsonNodeType.Property               The next property after a deferred link or entity reference link
        ///                 JsonNodeType.EndObject              If no (more) properties exist in the resource's content
        /// Post-Condition: JsonNodeType.StartObject            Start of the expanded resource of the navigation link to read next.
        ///                 JsonNodeType.StartArray             Start of the expanded resource set of the navigation link to read next.
        ///                 JsonNodeType.PrimitiveValue (null)  Expanded null resource of the navigation link to read next.
        ///                 JsonNodeType.Property               The next property after a deferred link or entity reference link
        ///                 JsonNodeType.EndObject              If no (more) properties exist in the resource's content
        /// </remarks>
        private bool ReadAtResourceStartImplementationSynchronously()
        {
            if (this.CurrentEntry == null)
            {
                Debug.Assert(this.IsExpandedLinkContent, "null resource can only be reported in an expanded link.");
                this.jsonLightResourceDeserializer.AssertJsonCondition(JsonNodeType.PrimitiveValue);
                Debug.Assert(this.jsonLightResourceDeserializer.JsonReader.Value == null, "The null resource should be represented as null value.");

                // Expanded null resource is represented as null primitive value
                // There's nothing to read, so move to the end resource state
                this.EndEntry();
            }
            else if (this.jsonLightInputContext.UseServerApiBehavior)
            {
                // In WCF DS Server mode we don't read ahead but report the resource right after type name.
                // So we need to read the resource content now.
                ODataJsonLightReaderNavigationLinkInfo navigationLinkInfo = this.jsonLightResourceDeserializer.ReadEntryContent(this.CurrentResourceState);
                if (navigationLinkInfo != null)
                {
                    this.StartNavigationLink(navigationLinkInfo);
                }
                else
                {
                    this.EndEntry();
                }
            }
            else if (this.CurrentResourceState.FirstNavigationLinkInfo != null)
            {
                this.StartNavigationLink(this.CurrentResourceState.FirstNavigationLinkInfo);
            }
            else
            {
                // End of resource
                // All the properties have already been read before we acually entered the EntryStart state (since we read as far as we can in any given state).
                this.jsonLightResourceDeserializer.AssertJsonCondition(JsonNodeType.EndObject);
                this.EndEntry();
            }

            Debug.Assert(
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.StartObject ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.StartArray ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.PrimitiveValue && this.jsonLightResourceDeserializer.JsonReader.Value == null ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.Property ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.EndObject,
                "Post-Condition: expected JsonNodeType.StartObject or JsonNodeType.StartArray or JsonNodeType.PrimitiveValue (null) or JsonNodeType.Property or JsonNodeType.EndObject");

            return true;
        }

        /// <summary>
        /// Implementation of the reader logic when in state 'EntryEnd'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.EndObject              end of object of the resource
        ///                 JsonNodeType.PrimitiveValue (null)  end of null expanded resource
        /// Post-Condition: The reader is positioned on the first node after the resource's end-object node
        /// </remarks>
        private bool ReadAtResourceEndImplementationSynchronously()
        {
            Debug.Assert(
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.EndObject ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.PrimitiveValue && this.jsonLightResourceDeserializer.JsonReader.Value == null,
                "Pre-Condition: JsonNodeType.EndObject or JsonNodeType.PrimitiveValue (null)");

            // We have to cache these values here, since the PopScope below will destroy them.
            bool isTopLevel = this.IsTopLevel;
            bool isExpandedLinkContent = this.IsExpandedLinkContent;

            this.PopScope(ODataReaderState.ResourceEnd);

            // Read over the end object node (or null value) and position the reader on the next node in the input.
            // This can hit the end of the input.
            this.jsonLightResourceDeserializer.JsonReader.Read();
            JsonNodeType nodeType = this.jsonLightResourceDeserializer.JsonReader.NodeType;

            // Analyze the next Json token to determine whether it is start object (next resource), end array (resource set end) or eof (top-level resource end)
            bool result = true;
            if (isTopLevel)
            {
                // NOTE: we rely on the underlying JSON reader to fail if there is more than one value at the root level.
                Debug.Assert(
                    this.IsReadingNestedPayload || this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.EndOfInput,
                    "Expected JSON reader to have reached the end of input when not reading a nested payload.");

                // read the end-of-payload
                Debug.Assert(this.State == ODataReaderState.Start, "this.State == ODataReaderState.Start");
                this.jsonLightResourceDeserializer.ReadPayloadEnd(this.IsReadingNestedPayload);
                Debug.Assert(
                    this.IsReadingNestedPayload || this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.EndOfInput,
                    "Expected JSON reader to have reached the end of input when not reading a nested payload.");

                // replace the 'Start' scope with the 'Completed' scope
                this.ReplaceScope(ODataReaderState.Completed);
                result = false;
            }
            else if (isExpandedLinkContent)
            {
                Debug.Assert(
                    nodeType == JsonNodeType.EndObject ||               // expanded link resource as last property of the owning resource
                    nodeType == JsonNodeType.Property,                  // expanded link resource with more properties on the resource
                    "Invalid JSON reader state for reading end of resource in expanded link.");

                // finish reading the expanded link
                this.ReadExpandedNavigationLinkEnd(false);
            }
            else
            {
                // End of resource in a resource set
                switch (nodeType)
                {
                    case JsonNodeType.StartObject:
                        // another resource in a resource set
                        Debug.Assert(this.State == ODataReaderState.ResourceSetStart, "Expected reader to be in state resource set start before reading the next resource.");
                        this.ReadEntryStart(/*duplicatePropertyNamesChecker*/ null, this.CurrentJsonLightResourceSetScope.SelectedProperties);
                        break;
                    case JsonNodeType.EndArray:
                        // we are at the end of a resource set
                        Debug.Assert(this.State == ODataReaderState.ResourceSetStart, "Expected reader to be in state resource set start after reading the last resource in the resource set.");
                        this.ReadFeedEnd();
                        break;
                    default:
                        throw new ODataException(ODataErrorStrings.ODataJsonReader_CannotReadEntriesOfFeed(this.jsonLightResourceDeserializer.JsonReader.NodeType));
                }
            }

            return result;
        }

        /// <summary>
        /// Implementation of the reader logic when in state 'NavigationLinkStart'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.StartObject            start of an expanded resource
        ///                 JsonNodeType.StartArray             start of an expanded resource set
        ///                 JsonNodeType.PrimitiveValue (null)  expanded null resource
        ///                 JsonNodeType.Property               deferred link with more properties in owning resource
        ///                 JsonNodeType.EndObject              deferred link as last property of the owning resource or
        ///                                                     reporting projected navigation links missing in the payload
        /// Post-Condition: JsonNodeType.StartArray:            start of expanded resource
        ///                 JsonNodeType.StartObject            start of expanded resource set
        ///                 JsonNodeType.PrimitiveValue (null)  expanded null resource
        ///                 JsonNodeType.Property               deferred link with more properties in owning resource
        ///                 JsonNodeType.EndObject              deferred link as last property of the owning resource or
        ///                                                     reporting projected navigation links missing in the payload
        /// </remarks>
        private bool ReadAtNavigationLinkStartImplementationSynchronously()
        {
            Debug.Assert(
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.Property ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.EndObject ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.StartObject ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.StartArray ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.PrimitiveValue && this.jsonLightResourceDeserializer.JsonReader.Value == null,
                "Pre-Condition: expected JsonNodeType.Property, JsonNodeType.EndObject, JsonNodeType.StartObject, JsonNodeType.StartArray or JsonNodeType.Primitive (null)");

            ODataNestedResourceInfo currentLink = this.CurrentNavigationLink;
            Debug.Assert(
                currentLink.IsCollection.HasValue || this.jsonLightInputContext.MessageReaderSettings.ReportUndeclaredLinkProperties,
                "Expect to know whether this is a singleton or collection link based on metadata.");

            IODataJsonLightReaderResourceState parentEntryState = (IODataJsonLightReaderResourceState)this.LinkParentEntityScope;

            if (this.jsonLightInputContext.ReadingResponse)
            {
                // If we are reporting a navigation link that was projected but not included in the payload,
                // simply change state to NavigationLinkEnd.
                if (parentEntryState.ProcessingMissingProjectedNavigationLinks)
                {
                    this.ReplaceScope(ODataReaderState.NavigationLinkEnd);
                }
                else if (!this.jsonLightResourceDeserializer.JsonReader.IsOnValueNode())
                {
                    // Deferred link (navigation link which doesn't have a value and is in the response)
                    ReaderUtils.CheckForDuplicateNavigationLinkNameAndSetAssociationLink(parentEntryState.DuplicatePropertyNamesChecker, currentLink, false, currentLink.IsCollection);
                    this.jsonLightResourceDeserializer.AssertJsonCondition(JsonNodeType.EndObject, JsonNodeType.Property);

                    // Record that we read the link on the parent resource's scope.
                    parentEntryState.NavigationPropertiesRead.Add(currentLink.Name);

                    this.ReplaceScope(ODataReaderState.NavigationLinkEnd);
                }
                else if (!currentLink.IsCollection.Value)
                {
                    // We should get here only for declared navigation properties.
                    Debug.Assert(this.CurrentEntityType != null, "We must have a declared navigation property to read expanded links.");

                    // Expanded resource
                    ReaderUtils.CheckForDuplicateNavigationLinkNameAndSetAssociationLink(parentEntryState.DuplicatePropertyNamesChecker, currentLink, true, false);
                    this.ReadExpandedEntryStart(currentLink);
                }
                else
                {
                    // Expanded resource set
                    ReaderUtils.CheckForDuplicateNavigationLinkNameAndSetAssociationLink(parentEntryState.DuplicatePropertyNamesChecker, currentLink, true, true);

                    // We store the precreated expanded resource set in the navigation link info since it carries the annotations for it.
                    ODataJsonLightReaderNavigationLinkInfo navigationLinkInfo = this.CurrentJsonLightNavigationLinkScope.NavigationLinkInfo;
                    Debug.Assert(navigationLinkInfo != null, "navigationLinkInfo != null");
                    Debug.Assert(navigationLinkInfo.ExpandedFeed != null, "We must have a precreated expanded resource set already.");
                    JsonLightResourceScope parentScope = (JsonLightResourceScope)this.LinkParentEntityScope;
                    SelectedPropertiesNode parentSelectedProperties = parentScope.SelectedProperties;
                    Debug.Assert(parentSelectedProperties != null, "parentProjectedProperties != null");
                    this.ReadResourceSetStart(navigationLinkInfo.ExpandedFeed, parentSelectedProperties.GetSelectedPropertiesForNavigationProperty(parentScope.EntityType, currentLink.Name));
                }
            }
            else
            {
                // Navigation link in request - report entity reference links and then possible expanded value.
                ODataJsonLightReaderNavigationLinkInfo navigationLinkInfo = this.CurrentJsonLightNavigationLinkScope.NavigationLinkInfo;
                ReaderUtils.CheckForDuplicateNavigationLinkNameAndSetAssociationLink(
                    parentEntryState.DuplicatePropertyNamesChecker,
                    currentLink,
                    navigationLinkInfo.IsExpanded,
                    currentLink.IsCollection);
                this.ReadNextNavigationLinkContentItemInRequest();
            }

            return true;
        }

        /// <summary>
        /// Implementation of the reader logic when in state 'NavigationLinkEnd'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.EndObject:         navigation link is last property in owning resource or
        ///                                                 reporting projected navigation links missing in the payload
        ///                 JsonNodeType.Property:          there are more properties after the navigation link in the owning resource
        /// Post-Condition: JsonNodeType.StartObject        start of the expanded resource navigation link to read next
        ///                 JsonNodeType.StartArray         start of the expanded resource set navigation link to read next
        ///                 JsonNoteType.Primitive (null)   expanded null resource navigation link to read next
        ///                 JsonNoteType.Property           property after deferred link or entity reference link
        ///                 JsonNodeType.EndObject          end of the parent resource
        /// </remarks>
        private bool ReadAtNavigationLinkEndImplementationSynchronously()
        {
            this.jsonLightResourceDeserializer.AssertJsonCondition(
                JsonNodeType.EndObject,
                JsonNodeType.Property);

            this.PopScope(ODataReaderState.NavigationLinkEnd);
            Debug.Assert(this.State == ODataReaderState.ResourceStart, "this.State == ODataReaderState.ResourceStart");

            ODataJsonLightReaderNavigationLinkInfo navigationLinkInfo = null;
            IODataJsonLightReaderResourceState resourceState = this.CurrentResourceState;

            if (this.jsonLightInputContext.ReadingResponse && resourceState.ProcessingMissingProjectedNavigationLinks)
            {
                // We are reporting navigation links that were projected but missing from the payload
                navigationLinkInfo = resourceState.Resource.MetadataBuilder.GetNextUnprocessedNavigationLink();
            }
            else
            {
                navigationLinkInfo = this.jsonLightResourceDeserializer.ReadEntryContent(resourceState);
            }

            if (navigationLinkInfo == null)
            {
                // End of the resource
                this.EndEntry();
            }
            else
            {
                // Next navigation link on the resource
                this.StartNavigationLink(navigationLinkInfo);
            }

            return true;
        }

        /// <summary>
        /// Implementation of the reader logic when in state 'EntityReferenceLink'.
        /// </summary>
        /// <returns>true if more items can be read from the reader; otherwise false.</returns>
        /// <remarks>
        /// This method doesn't move the reader
        /// Pre-Condition:  JsonNodeType.EndObject:         expanded link property is last property in owning resource
        ///                 JsonNodeType.Property:          there are more properties after the expanded link property in the owning resource
        ///                 Any:                            expanded collection link - the node after the entity reference link.
        /// Post-Condition: JsonNodeType.EndObject:         expanded link property is last property in owning resource
        ///                 JsonNodeType.Property:          there are more properties after the expanded link property in the owning resource
        ///                 Any:                            expanded collection link - the node after the entity reference link.
        /// </remarks>
        private bool ReadAtEntityReferenceLinkSynchronously()
        {
            this.PopScope(ODataReaderState.EntityReferenceLink);
            Debug.Assert(this.State == ODataReaderState.NavigationLinkStart, "this.State == ODataReaderState.NavigationLinkStart");

            this.ReadNextNavigationLinkContentItemInRequest();
            return true;
        }

        /// <summary>
        /// Reads the start of the JSON array for the content of the resource set and sets up the reader state correctly.
        /// </summary>
        /// <param name="resourceSet">The resource set to read the contents for.</param>
        /// <param name="selectedProperties">The selected properties node capturing what properties should be expanded during template evaluation.</param>
        /// <remarks>
        /// Pre-Condition:  The first node of the resource set property value; this method will throw if the node is not
        ///                 JsonNodeType.StartArray
        /// Post-Condition: The reader is positioned on the first item in the resource set, or on the end array of the resource set.
        /// </remarks>
        private void ReadResourceSetStart(ODataResourceSet resourceSet, SelectedPropertiesNode selectedProperties)
        {
            Debug.Assert(resourceSet != null, "resourceSet != null");

            this.jsonLightResourceDeserializer.ReadFeedContentStart();
            this.EnterScope(new JsonLightResourceSetScope(resourceSet, this.CurrentNavigationSource, this.CurrentEntityType, selectedProperties, this.CurrentScope.ODataUri));

            this.jsonLightResourceDeserializer.AssertJsonCondition(JsonNodeType.EndArray, JsonNodeType.StartObject);
        }

        /// <summary>
        /// Reads the end of the current resource set.
        /// </summary>
        private void ReadFeedEnd()
        {
            Debug.Assert(this.State == ODataReaderState.ResourceSetStart, "this.State == ODataReaderState.ResourceSetStart");

            this.jsonLightResourceDeserializer.ReadFeedContentEnd();

            ODataJsonLightReaderNavigationLinkInfo expandedNavigationLinkInfo = null;
            JsonLightNavigationLinkScope parentNavigationLinkScope = (JsonLightNavigationLinkScope)this.ExpandedLinkContentParentScope;
            if (parentNavigationLinkScope != null)
            {
                expandedNavigationLinkInfo = parentNavigationLinkScope.NavigationLinkInfo;
            }

            if (!this.IsReadingNestedPayload)
            {
                // Temp ban reading the instance annotation after the resource set in parameter payload. (!this.IsReadingNestedPayload => !this.readingParameter)
                // Nested resource set payload won't have a NextLink annotation after the resource set itself since the payload is NOT pageable.
                this.jsonLightResourceDeserializer.ReadNextLinkAnnotationAtResourceSetEnd(this.CurrentFeed,
                    expandedNavigationLinkInfo, this.topLevelScope.DuplicatePropertyNamesChecker);
            }

            this.ReplaceScope(ODataReaderState.ResourceSetEnd);
        }

        /// <summary>
        /// Reads the start of an expanded resource (null or non-null).
        /// </summary>
        /// <param name="navigationLink">The navigation link that is being expanded.</param>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.StartObject            The start of the resource object
        ///                 JsonNodeType.PrimitiveValue (null)  The null resource value
        /// Post-Condition: JsonNodeType.StartObject            Start of expanded resource of the navigation link to read next
        ///                 JsonNodeType.StartArray             Start of expanded resource set of the navigation link to read next
        ///                 JsonNodeType.PrimitiveValue (null)  Expanded null resource of the navigation link to read next, or the null value of the current null resource
        ///                 JsonNodeType.Property               Property after deferred link or expanded entity reference
        ///                 JsonNodeType.EndObject              If no (more) properties exist in the resource's content
        /// </remarks>
        private void ReadExpandedEntryStart(ODataNestedResourceInfo navigationLink)
        {
            Debug.Assert(navigationLink != null, "navigationLink != null");

            if (this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.PrimitiveValue)
            {
                Debug.Assert(this.jsonLightResourceDeserializer.JsonReader.Value == null, "If a primitive value is representing an expanded entity its value must be null.");

                // Expanded null resource
                // The expected type and expected navigation source for an expanded resource are the same as for the navigation link around it.
                this.EnterScope(new JsonLightResourceScope(ODataReaderState.ResourceStart, /*resource*/ null, this.CurrentNavigationSource, this.CurrentEntityType, /*duplicatePropertyNamesChecker*/null, /*projectedProperties*/null, this.CurrentScope.ODataUri));
            }
            else
            {
                // Expanded resource
                // The expected type for an expanded resource is the same as for the navigation link around it.
                JsonLightResourceScope parentScope = (JsonLightResourceScope)this.LinkParentEntityScope;
                SelectedPropertiesNode parentSelectedProperties = parentScope.SelectedProperties;
                Debug.Assert(parentSelectedProperties != null, "parentProjectedProperties != null");
                this.ReadEntryStart(/*duplicatePropertyNamesChecker*/ null, parentSelectedProperties.GetSelectedPropertiesForNavigationProperty(parentScope.EntityType, navigationLink.Name));
            }
        }

        /// <summary>
        /// Reads the start of a resource and sets up the reader state correctly
        /// </summary>
        /// <param name="duplicatePropertyNamesChecker">The duplicate property names checker to use for the resource; 
        /// or null if a new one should be created.</param>
        /// <param name="selectedProperties">The selected properties node capturing what properties should be expanded during template evaluation.</param>
        /// <remarks>
        /// Pre-Condition:  JsonNodeType.StartObject            If the resource is in a resource set - the start of the resource object
        ///                 JsonNodeType.Property               If the resource is a top-level resource and has at least one property
        ///                 JsonNodeType.EndObject              If the resource is a top-level resource and has no properties
        /// Post-Condition: JsonNodeType.StartObject            Start of expanded resource of the navigation link to read next
        ///                 JsonNodeType.StartArray             Start of expanded resource set of the navigation link to read next
        ///                 JsonNodeType.PrimitiveValue (null)  Expanded null resource of the navigation link to read next
        ///                 JsonNodeType.Property               Property after deferred link or expanded entity reference
        ///                 JsonNodeType.EndObject              If no (more) properties exist in the resource's content
        /// </remarks>
        private void ReadEntryStart(DuplicatePropertyNamesChecker duplicatePropertyNamesChecker, SelectedPropertiesNode selectedProperties)
        {
            this.jsonLightResourceDeserializer.AssertJsonCondition(JsonNodeType.StartObject, JsonNodeType.Property, JsonNodeType.EndObject);

            // If the reader is on StartObject then read over it. This happens for entries in resource set.
            // For top-level entries the reader will be positioned on the first resource property (after odata.context if it was present).
            if (this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.StartObject)
            {
                this.jsonLightResourceDeserializer.JsonReader.Read();
            }

            if (this.ReadingFeed || this.IsExpandedLinkContent)
            {
                string contextUriStr = this.jsonLightResourceDeserializer.ReadContextUriAnnotation(ODataPayloadKind.Resource, duplicatePropertyNamesChecker, false);
                if (contextUriStr != null)
                {
                    contextUriStr = UriUtils.UriToString(this.jsonLightResourceDeserializer.ProcessUriFromPayload(contextUriStr));
                    var parseResult = ODataJsonLightContextUriParser.Parse(
                            this.jsonLightResourceDeserializer.Model,
                            contextUriStr,
                            ODataPayloadKind.Resource,
                            this.jsonLightResourceDeserializer.MessageReaderSettings.ReaderBehavior,
                            this.jsonLightInputContext.ReadingResponse);
                    if (this.jsonLightInputContext.ReadingResponse && parseResult != null)
                    {
                        ReaderValidationUtils.ValidateFeedOrEntryContextUri(parseResult, this.CurrentScope, false);
                    }
                }
            }

            // Setup the new resource state
            this.StartEntry(duplicatePropertyNamesChecker, selectedProperties);

            // Read the odata.type annotation.
            this.jsonLightResourceDeserializer.ReadEntryTypeName(this.CurrentResourceState);

            // Resolve the type name
            Debug.Assert(
                this.CurrentNavigationSource != null || this.readingParameter,
                "We must always have an expected navigation source for each resource (since we can't deduce that from the type name).");
            this.ApplyEntityTypeNameFromPayload(this.CurrentEntry.TypeName);

            // Validate type with resource set validator if available
            if (this.CurrentFeedValidator != null)
            {
                this.CurrentFeedValidator.ValidateResource(this.CurrentEntityType);
            }

            if (this.CurrentEntityType != null)
            {
                // NOTE: once we do this for all formats we can do this in ApplyEntityTypeNameFromPayload.
                this.CurrentEntry.SetAnnotation(new ODataTypeAnnotation(this.CurrentNavigationSource, this.CurrentEntityType));
            }

            // In WCF DS Server mode we must not read ahead and report the type name only.
            if (this.jsonLightInputContext.UseServerApiBehavior)
            {
                this.CurrentResourceState.FirstNavigationLinkInfo = null;
            }
            else
            {
                this.CurrentResourceState.FirstNavigationLinkInfo = this.jsonLightResourceDeserializer.ReadEntryContent(this.CurrentResourceState);
            }

            this.jsonLightResourceDeserializer.AssertJsonCondition(
                JsonNodeType.Property,
                JsonNodeType.StartObject,
                JsonNodeType.StartArray,
                JsonNodeType.EndObject,
                JsonNodeType.PrimitiveValue);
        }

        /// <summary>
        /// Verifies that the current item is an <see cref="ODataNestedResourceInfo"/> instance,
        /// sets the cardinality of the link (IsCollection property) and moves the reader
        /// into state 'NavigationLinkEnd'.
        /// </summary>
        /// <param name="isCollection">A flag indicating whether the link represents a collection or not.</param>
        private void ReadExpandedNavigationLinkEnd(bool isCollection)
        {
            Debug.Assert(this.State == ODataReaderState.NavigationLinkStart, "this.State == ODataReaderState.NavigationLinkStart");
            this.CurrentNavigationLink.IsCollection = isCollection;

            // Record that we read the link on the parent resource's scope.
            IODataJsonLightReaderResourceState parentEntryState = (IODataJsonLightReaderResourceState)this.LinkParentEntityScope;
            parentEntryState.NavigationPropertiesRead.Add(this.CurrentNavigationLink.Name);

            // replace the 'NavigationLinkStart' scope with the 'NavigationLinkEnd' scope
            this.ReplaceScope(ODataReaderState.NavigationLinkEnd);
        }

        /// <summary>
        /// Reads the next item in a navigation link content in a request payload.
        /// </summary>
        private void ReadNextNavigationLinkContentItemInRequest()
        {
            Debug.Assert(this.CurrentScope.State == ODataReaderState.NavigationLinkStart, "Must be on 'NavigationLinkStart' scope.");

            ODataJsonLightReaderNavigationLinkInfo navigationLinkInfo = this.CurrentJsonLightNavigationLinkScope.NavigationLinkInfo;
            if (navigationLinkInfo.HasEntityReferenceLink)
            {
                this.EnterScope(new Scope(ODataReaderState.EntityReferenceLink, navigationLinkInfo.ReportEntityReferenceLink(), null, null, this.CurrentScope.ODataUri));
            }
            else if (navigationLinkInfo.IsExpanded)
            {
                if (navigationLinkInfo.NavigationLink.IsCollection == true)
                {
                    // because this is a request, there is no $select query option.
                    SelectedPropertiesNode selectedProperties = SelectedPropertiesNode.EntireSubtree;
                    this.ReadResourceSetStart(new ODataResourceSet(), selectedProperties);
                }
                else
                {
                    this.ReadExpandedEntryStart(navigationLinkInfo.NavigationLink);
                }
            }
            else
            {
                // replace the 'NavigationLinkStart' scope with the 'NavigationLinkEnd' scope
                this.ReplaceScope(ODataReaderState.NavigationLinkEnd);
            }
        }

        /// <summary>
        /// Starts the resource, initializing the scopes and such. This method starts a non-null resource only.
        /// </summary>
        /// <param name="duplicatePropertyNamesChecker">The duplicate property names checker to use for the resource; 
        /// or null if a new one should be created.</param>
        /// <param name="selectedProperties">The selected properties node capturing what properties should be expanded during template evaluation.</param>
        private void StartEntry(DuplicatePropertyNamesChecker duplicatePropertyNamesChecker, SelectedPropertiesNode selectedProperties)
        {
            this.EnterScope(new JsonLightResourceScope(
                ODataReaderState.ResourceStart,
                ReaderUtils.CreateNewEntry(),
                this.CurrentNavigationSource,
                this.CurrentEntityType,
                duplicatePropertyNamesChecker ?? this.jsonLightInputContext.CreateDuplicatePropertyNamesChecker(),
                selectedProperties,
                this.CurrentScope.ODataUri));
        }

        /// <summary>
        /// Starts the navigation link.
        /// Does metadata validation of the navigation link and sets up the reader to report it.
        /// </summary>
        /// <param name="navigationLinkInfo">The navigation link info for the navigation link to start.</param>
        private void StartNavigationLink(ODataJsonLightReaderNavigationLinkInfo navigationLinkInfo)
        {
            Debug.Assert(navigationLinkInfo != null, "navigationLinkInfo != null");
            ODataNestedResourceInfo navigationLink = navigationLinkInfo.NavigationLink;
            IEdmNavigationProperty navigationProperty = navigationLinkInfo.NavigationProperty;

            Debug.Assert(
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.Property ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.EndObject ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.StartObject ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.StartArray ||
                this.jsonLightResourceDeserializer.JsonReader.NodeType == JsonNodeType.PrimitiveValue && this.jsonLightResourceDeserializer.JsonReader.Value == null,
                "Post-Condition: expected JsonNodeType.StartObject or JsonNodeType.StartArray or JsonNodeType.Primitive (null), or JsonNodeType.Property, JsonNodeType.EndObject");
            Debug.Assert(
                navigationProperty != null || this.jsonLightInputContext.MessageReaderSettings.ReportUndeclaredLinkProperties,
                "A navigation property must be found for each link we find unless we're allowed to report undeclared links.");
            Debug.Assert(navigationLink != null, "navigationLink != null");
            Debug.Assert(!string.IsNullOrEmpty(navigationLink.Name), "Navigation links must have a name.");
            Debug.Assert(
                navigationProperty == null || navigationLink.Name == navigationProperty.Name,
                "The navigation property must match the navigation link.");

            // we are at the beginning of a link
            IEdmEntityType targetResourceType = null;
            if (navigationProperty != null)
            {
                IEdmTypeReference navigationPropertyType = navigationProperty.Type;
                targetResourceType = navigationPropertyType.IsCollection()
                    ? navigationPropertyType.AsCollection().ElementType().AsEntity().EntityDefinition()
                    : navigationPropertyType.AsEntity().EntityDefinition();
            }

            // Since we don't have the entity metadata builder for the resource read out from a nested payload
            // as stated in ReadAtResourceSetEndImplementationSynchronously(), we cannot access it here which otherwise
            // would lead to an exception.
            if (this.jsonLightInputContext.ReadingResponse && !this.IsReadingNestedPayload)
            {
                // Hookup the metadata builder to the navigation link.
                // Note that we set the metadata builder even when navigationProperty is null, which is the case when the link is undeclared.
                // For undeclared links, we will apply conventional metadata evaluation just as declared links.
                ODataResourceMetadataBuilder entityMetadataBuilder = this.jsonLightResourceDeserializer.MetadataContext.GetResourceMetadataBuilderForReader(this.CurrentResourceState, this.jsonLightInputContext.MessageReaderSettings.UseKeyAsSegment);
                navigationLink.MetadataBuilder = entityMetadataBuilder;
            }

            Debug.Assert(this.CurrentNavigationSource != null || this.readingParameter, "Json requires an navigation source when not reading parameter.");

            IEdmNavigationSource navigationSource = this.CurrentNavigationSource == null || navigationProperty == null ? null : this.CurrentNavigationSource.FindNavigationTarget(navigationProperty);
            ODataUri odataUri = null;
            if (navigationLinkInfo.NavigationLink.ContextUrl != null)
            {
                ODataPath odataPath = ODataJsonLightContextUriParser.Parse(
                        this.jsonLightResourceDeserializer.Model,
                        UriUtils.UriToString(navigationLinkInfo.NavigationLink.ContextUrl),
                        navigationLinkInfo.NavigationLink.IsCollection.GetValueOrDefault() ? ODataPayloadKind.ResourceSet : ODataPayloadKind.Resource,
                        this.jsonLightResourceDeserializer.MessageReaderSettings.ReaderBehavior,
                        this.jsonLightResourceDeserializer.JsonLightInputContext.ReadingResponse).Path;
                odataUri = new ODataUri()
                {
                    Path = odataPath
                };
            }

            this.EnterScope(new JsonLightNavigationLinkScope(navigationLinkInfo, navigationSource, targetResourceType, odataUri));
        }

        /// <summary>
        /// Replaces the current scope with a new scope with the specified <paramref name="state"/> and
        /// the item of the current scope.
        /// </summary>
        /// <param name="state">The <see cref="ODataReaderState"/> to use for the new scope.</param>
        private void ReplaceScope(ODataReaderState state)
        {
            this.ReplaceScope(new Scope(state, this.Item, this.CurrentNavigationSource, this.CurrentEntityType, this.CurrentScope.ODataUri));
        }

        /// <summary>
        /// Called to transition into the EntryEnd state.
        /// </summary>
        private void EndEntry()
        {
            IODataJsonLightReaderResourceState resourceState = this.CurrentResourceState;

            // NOTE: the current resource will be null for an expanded null resource; no template
            //       expansion for null entries.
            //       there is no entity metadata builder for a resource from a nested payload
            //       as stated in ReadAtResourceSetEndImplementationSynchronously().
            if (this.CurrentEntry != null && !this.IsReadingNestedPayload)
            {
                ODataResourceMetadataBuilder builder = this.jsonLightResourceDeserializer.MetadataContext.GetResourceMetadataBuilderForReader(this.CurrentResourceState, this.jsonLightInputContext.MessageReaderSettings.UseKeyAsSegment);
                if (builder != this.CurrentEntry.MetadataBuilder)
                {
                    // Builder should not be used outside the odataentry, lazy builder logic does not work here
                    // We should refactor this
                    foreach (string navigationPropertyName in this.CurrentResourceState.NavigationPropertiesRead)
                    {
                        builder.MarkNavigationLinkProcessed(navigationPropertyName);
                    }

                    ODataConventionalResourceMetadataBuilder conventionalEntityMetadataBuilder = builder as ODataConventionalResourceMetadataBuilder;

                    // If it's ODataConventionalEntityMetadataBuilder, then it means we need to build nested relation ship for it in containment case
                    if (conventionalEntityMetadataBuilder != null)
                    {
                        conventionalEntityMetadataBuilder.ODataUri = this.CurrentScope.ODataUri;
                    }

                    // Set the metadata builder for the resource itself
                    this.CurrentEntry.MetadataBuilder = builder;
                }
            }

            this.jsonLightResourceDeserializer.ValidateEntryMetadata(resourceState);

            // In responses, ensure that all projected properties get created.
            // Also ignore cases where the resource is 'null' which happens for expanded null entries.
            if (this.jsonLightInputContext.ReadingResponse && this.CurrentEntry != null)
            {
                // If we have a projected navigation link that was missing from the payload, report it now.
                ODataJsonLightReaderNavigationLinkInfo unprocessedNavigationLink = this.CurrentEntry.MetadataBuilder.GetNextUnprocessedNavigationLink();
                if (unprocessedNavigationLink != null)
                {
                    this.CurrentResourceState.ProcessingMissingProjectedNavigationLinks = true;
                    this.StartNavigationLink(unprocessedNavigationLink);
                    return;
                }
            }

            this.EndEntry(
                new JsonLightResourceScope(
                    ODataReaderState.ResourceEnd,
                    (ODataResource)this.Item,
                    this.CurrentNavigationSource,
                    this.CurrentEntityType,
                    this.CurrentResourceState.DuplicatePropertyNamesChecker,
                    this.CurrentResourceState.SelectedProperties,
                    this.CurrentScope.ODataUri));
        }

        /// <summary>
        /// A reader top-level scope; keeping track of the current reader state and an item associated with this state.
        /// </summary>
        private sealed class JsonLightTopLevelScope : Scope
        {
            /// <summary>
            /// Constructor creating a new reader scope.
            /// </summary>
            /// <param name="navigationSource">The navigation source we are going to read entities for.</param>
            /// <param name="expectedEntityType">The expected type for the scope.</param>
            /// <remarks>The <paramref name="expectedEntityType"/> has the following meaning
            ///   it's the expected base type of the top-level resource or entries in the top-level resource set.
            /// In all cases the specified type must be an entity type.</remarks>
            internal JsonLightTopLevelScope(IEdmNavigationSource navigationSource, IEdmEntityType expectedEntityType)
                : base(ODataReaderState.Start, /*item*/ null, navigationSource, expectedEntityType, null)
            {
            }

            /// <summary>
            /// The duplicate property names checker for the top level scope represented by the current state.
            /// </summary>
            public DuplicatePropertyNamesChecker DuplicatePropertyNamesChecker { get; set; }
        }

        /// <summary>
        /// A reader resource scope; keeping track of the current reader state and an item associated with this state.
        /// </summary>
        private sealed class JsonLightResourceScope : Scope, IODataJsonLightReaderResourceState
        {
            /// <summary>The set of names of the navigation properties we have read so far while reading the resource.</summary>
            private List<string> navigationPropertiesRead;

            /// <summary>
            /// Constructor creating a new reader scope.
            /// </summary>
            /// <param name="readerState">The reader state of the new scope that is being created.</param>
            /// <param name="resource">The item attached to this scope.</param>
            /// <param name="navigationSource">The navigation source we are going to read entities for.</param>
            /// <param name="expectedEntityType">The expected type for the scope.</param>
            /// <param name="duplicatePropertyNamesChecker">The duplicate property names checker for this resource scope.</param>
            /// <param name="selectedProperties">The selected properties node capturing what properties should be expanded during template evaluation.</param>
            /// <param name="odataUri">The odataUri parsed based on the context uri for current scope</param>
            /// <remarks>The <paramref name="expectedEntityType"/> has the following meaning
            ///   it's the expected base type of the resource. If the resource has no type name specified
            ///   this type will be assumed. Otherwise the specified type name must be
            ///   the expected type or a more derived type.
            /// In all cases the specified type must be an entity type.</remarks>
            internal JsonLightResourceScope(
                ODataReaderState readerState,
                ODataResource resource,
                IEdmNavigationSource navigationSource,
                IEdmEntityType expectedEntityType,
                DuplicatePropertyNamesChecker duplicatePropertyNamesChecker,
                SelectedPropertiesNode selectedProperties,
                ODataUri odataUri)
                : base(readerState, resource, navigationSource, expectedEntityType, odataUri)
            {
                Debug.Assert(
                    readerState == ODataReaderState.ResourceStart || readerState == ODataReaderState.ResourceEnd,
                    "readerState == ODataReaderState.ResourceStart || readerState == ODataReaderState.ResourceEnd");

                this.DuplicatePropertyNamesChecker = duplicatePropertyNamesChecker;
                this.SelectedProperties = selectedProperties;
            }

            /// <summary>
            /// The metadata builder instance for the resource.
            /// </summary>
            public ODataResourceMetadataBuilder MetadataBuilder { get; set; }

            /// <summary>
            /// Flag which indicates that during parsing of the resource represented by this state,
            /// any property which is not an instance annotation was found. This includes property annotations
            /// for property which is not present in the payload.
            /// </summary>
            /// <remarks>
            /// This is used to detect incorrect ordering of the payload (for example odata.id must not come after the first property).
            /// </remarks>
            public bool AnyPropertyFound { get; set; }

            /// <summary>
            /// If the reader finds a navigation link to report, but it must first report the parent resource
            /// it will store the navigation link info in this property. So this will only ever store the first navigation link of a resource.
            /// </summary>
            public ODataJsonLightReaderNavigationLinkInfo FirstNavigationLinkInfo { get; set; }

            /// <summary>
            /// The duplicate property names checker for the resource represented by the current state.
            /// </summary>
            public DuplicatePropertyNamesChecker DuplicatePropertyNamesChecker { get; private set; }

            /// <summary>
            /// The selected properties that should be expanded during template evaluation.
            /// </summary>
            public SelectedPropertiesNode SelectedProperties { get; private set; }

            /// <summary>
            /// The set of names of the navigation properties we have read so far while reading the resource.
            /// true if we have started processing missing projected navigation links, false otherwise.
            /// </summary>
            public List<string> NavigationPropertiesRead
            {
                get { return this.navigationPropertiesRead ?? (this.navigationPropertiesRead = new List<string>()); }
            }

            /// <summary>
            /// true if we have started processing missing projected navigation links, false otherwise.
            /// </summary>
            public bool ProcessingMissingProjectedNavigationLinks { get; set; }

            /// <summary>
            /// The resource being read.
            /// </summary>
            ODataResource IODataJsonLightReaderResourceState.Resource
            {
                get
                {
                    Debug.Assert(
                        this.State == ODataReaderState.ResourceStart || this.State == ODataReaderState.ResourceEnd,
                        "The IODataJsonReaderEntryState is only supported on EntryStart or EntryEnd scope.");
                    return (ODataResource)this.Item;
                }
            }

            /// <summary>
            /// The entity type for the resource (if available).
            /// </summary>
            IEdmEntityType IODataJsonLightReaderResourceState.EntityType
            {
                get
                {
                    Debug.Assert(
                        this.State == ODataReaderState.ResourceStart || this.State == ODataReaderState.ResourceEnd,
                        "The IODataJsonReaderEntryState is only supported on EntryStart or EntryEnd scope.");
                    return this.EntityType;
                }
            }
        }

        /// <summary>
        /// A reader resource set scope; keeping track of the current reader state and an item associated with this state.
        /// </summary>
        private sealed class JsonLightResourceSetScope : Scope
        {
            /// <summary>
            /// Constructor creating a new reader scope.
            /// </summary>
            /// <param name="resourceSet">The item attached to this scope.</param>
            /// <param name="navigationSource">The navigation source we are going to read entities for.</param>
            /// <param name="expectedEntityType">The expected type for the scope.</param>
            /// <param name="selectedProperties">The selected properties node capturing what properties should be expanded during template evaluation.</param>
            /// <param name="odataUri">The odataUri parsed based on the context uri for current scope</param>
            /// <remarks>The <paramref name="expectedEntityType"/> has the following meaning
            ///   it's the expected base type of the entries in the resource set.
            ///   note that it might be a more derived type than the base type of the entity set for the resource set.
            /// In all cases the specified type must be an entity type.</remarks>
            internal JsonLightResourceSetScope(ODataResourceSet resourceSet, IEdmNavigationSource navigationSource, IEdmEntityType expectedEntityType, SelectedPropertiesNode selectedProperties, ODataUri odataUri)
                : base(ODataReaderState.ResourceSetStart, resourceSet, navigationSource, expectedEntityType, odataUri)
            {
                this.SelectedProperties = selectedProperties;
            }

            /// <summary>
            /// The selected properties that should be expanded during template evaluation.
            /// </summary>
            public SelectedPropertiesNode SelectedProperties { get; private set; }
        }

        /// <summary>
        /// A reader scope; keeping track of the current reader state and an item associated with this state.
        /// </summary>
        private sealed class JsonLightNavigationLinkScope : Scope
        {
            /// <summary>
            /// Constructor creating a new reader scope.
            /// </summary>
            /// <param name="navigationLinkInfo">The navigation link info attached to this scope.</param>
            /// <param name="navigationSource">The navigation source we are going to read entities for.</param>
            /// <param name="expectedEntityType">The expected type for the scope.</param>
            /// <param name="odataUri">The odataUri parsed based on the context uri for current scope</param> 
            /// <remarks>The <paramref name="expectedEntityType"/> has the following meaning
            ///   it's the expected base type the entries in the expanded link (either the single resource
            ///   or entries in the expanded resource set).
            /// In all cases the specified type must be an entity type.</remarks>
            internal JsonLightNavigationLinkScope(ODataJsonLightReaderNavigationLinkInfo navigationLinkInfo, IEdmNavigationSource navigationSource, IEdmEntityType expectedEntityType, ODataUri odataUri)
                : base(ODataReaderState.NavigationLinkStart, navigationLinkInfo.NavigationLink, navigationSource, expectedEntityType, odataUri)
            {
                this.NavigationLinkInfo = navigationLinkInfo;
            }

            /// <summary>
            /// The navigation link info for the navigation link to report.
            /// This is only used on a StartNavigationLink scope in responses.
            /// </summary>
            public ODataJsonLightReaderNavigationLinkInfo NavigationLinkInfo { get; private set; }
        }
    }
}