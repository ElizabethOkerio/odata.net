//---------------------------------------------------------------------
// <copyright file="ODataMessageWritingHelper.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace System.Data.Services.Client
{
    using System.Diagnostics;
    using System.Xml;
    using Microsoft.Data.OData;

    /// <summary>
    /// Helper class for creating ODataLib writers, settings, and other write-related classes based on an instance of <see cref="RequestInfo"/>.
    /// </summary>
    internal class ODataMessageWritingHelper
    {
        /// <summary>The current request info.</summary>
        private readonly RequestInfo requestInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataMessageWritingHelper"/> class.
        /// </summary>
        /// <param name="requestInfo">The request info.</param>
        internal ODataMessageWritingHelper(RequestInfo requestInfo)
        {
            Debug.Assert(requestInfo != null, "requestInfo != null");
            this.requestInfo = requestInfo;
        }

        /// <summary>
        /// Create message writer settings for producing requests.
        /// </summary>
        /// <param name="startEntryXmlCustomizationCallback">Optional XML entry customization callback to be used for the start of entries.</param>
        /// <param name="endEntryXmlCustomizationCallback">Optional XML entry customization callback to be used for the end of entries.</param>
        /// <param name="isBatchPartRequest">if set to <c>true</c> indicates that this is a part of a batch request.</param>
        /// <returns>Newly created message writer settings.</returns>
        internal ODataMessageWriterSettings CreateSettings(Func<ODataEntry, XmlWriter, XmlWriter> startEntryXmlCustomizationCallback, Action<ODataEntry, XmlWriter, XmlWriter> endEntryXmlCustomizationCallback, bool isBatchPartRequest)
        {
            ODataMessageWriterSettings writerSettings = new ODataMessageWriterSettings 
            {
                CheckCharacters = false,
                Indent = false,

                // For operations inside batch, we need to dispose the stream. For top level requests,
                // we do not need to dispose the stream. Since for inner batch requests, the request
                // message is an internal implementation of IODataRequestMessage in ODataLib,
                // we can do this here.
                DisableMessageStreamDisposal = !isBatchPartRequest
            };

            CommonUtil.SetDefaultMessageQuotas(writerSettings.MessageQuotas);

            // If no event handlers are registered, then don't provide the callbacks to ODataLib.
            if (!this.requestInfo.HasWritingEventHandlers)
            {
                startEntryXmlCustomizationCallback = null;
                endEntryXmlCustomizationCallback = null;
            }

            // Enable the Astoria client behavior in ODataLib.
            writerSettings.EnableWcfDataServicesClientBehavior(startEntryXmlCustomizationCallback, endEntryXmlCustomizationCallback, this.requestInfo.DataNamespace, this.requestInfo.TypeScheme.AbsoluteUri);

            this.requestInfo.Configurations.RequestPipeline.ExecuteWriterSettingsConfiguration(writerSettings);

            return writerSettings;
        }

        /// <summary>
        /// Creates a writer for the given request message and settings.
        /// </summary>
        /// <param name="requestMessage">The request message.</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="isParameterPayload">true if the writer is intended to for a parameter payload, false otherwise.</param>
        /// <returns>Newly created writer.</returns>
        internal ODataMessageWriter CreateWriter(IODataRequestMessage requestMessage, ODataMessageWriterSettings writerSettings, bool isParameterPayload)
        {
            Debug.Assert(requestMessage != null, "requestMessage != null");
            Debug.Assert(writerSettings != null, "writerSettings != null");

            this.requestInfo.Context.Format.ValidateCanWriteRequestFormat(requestMessage, isParameterPayload);
            return new ODataMessageWriter(requestMessage, writerSettings, this.requestInfo.Model);
        }

        /// <summary>
        /// Creates a request message with the given arguments.
        /// </summary>
        /// <param name="requestMessageArgs">The request message args.</param>
        /// <returns>Newly created request message.</returns>
        internal ODataRequestMessageWrapper CreateRequestMessage(BuildingRequestEventArgs requestMessageArgs)
        {
            return ODataRequestMessageWrapper.CreateRequestMessageWrapper(requestMessageArgs, this.requestInfo);
        }
    }
}