//---------------------------------------------------------------------
// <copyright file="ChangesetResponse.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
// <summary>
// Response from SaveChanges.
// </summary>
//---------------------------------------------------------------------

namespace System.Data.Services.Client
{
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>Response from SaveChanges.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1010", Justification = "required for this feature")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710", Justification = "required for this feature")]
    public sealed class ChangeOperationResponse : OperationResponse
    {
        /// <summary>Descriptor containing the response object.</summary>
        private Descriptor descriptor;

        /// <summary>Initializes a new instance of the <see cref="T:System.Data.Services.Client.ChangeOperationResponse" /> class. </summary>
        /// <param name="headers">HTTP headers</param>
        /// <param name="descriptor">response object containing information about resources that got changed.</param>
        internal ChangeOperationResponse(HeaderCollection headers, Descriptor descriptor)
            : base(headers)
        {
            Debug.Assert(descriptor != null, "descriptor != null");
            this.descriptor = descriptor;
        }

        /// <summary>Gets the <see cref="T:System.Data.Services.Client.EntityDescriptor" /> or <see cref="T:System.Data.Services.Client.LinkDescriptor" /> modified by a change operation.</summary>
        /// <returns>An <see cref="T:System.Data.Services.Client.EntityDescriptor" /> or <see cref="T:System.Data.Services.Client.LinkDescriptor" /> modified by a change operation. </returns>
        public Descriptor Descriptor
        {
            get { return this.descriptor; }
        }
    }
}
