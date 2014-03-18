//---------------------------------------------------------------------
// <copyright file="HttpWebRequest.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
// <summary>
//      Provides an HTTP-specific implementation of the WebRequest class.
// </summary>
//
// @owner  markash
//---------------------------------------------------------------------

namespace System.Data.Services.Http
{
    #region Namespaces.

    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Diagnostics;

    #endregion Namespaces.

    /// <summary>
    /// Provides an HTTP-specific implementation of the WebRequest class.
    /// </summary>
    internal abstract class HttpWebRequest : System.Data.Services.Http.WebRequest, IDisposable
    {
        /// <summary>Gets or sets the 'Accept' header.</summary>
        public abstract string Accept
        {
            get;
            set;
        }

        /// <summary>Sets the 'Content-Length' header.</summary>
        public abstract long ContentLength
        {
            set;
        }

        /// <summary>Gets or sets a value that indicates whether to buffer the data read from the Internet resource.</summary>
        public abstract bool AllowReadStreamBuffering
        {
            get;
            set;
        }

        /// <summary>Gets and sets the authentication information used by each query created using the context object.</summary>
        public abstract System.Net.ICredentials Credentials
        {
            get;
            set;
        }

        /// <summary>Gets or sets a System.Boolean value that controls whether default credentials are sent with requests.</summary>
        public abstract bool UseDefaultCredentials 
        { 
            get;
            set;
        }

        /// <summary>Releases all resources held onto by this object.</summary>
        void IDisposable.Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Creates an empty instance of the System.Net.WebHeaderCollection with the right settings for this
        /// HTTP request. The two SL stacks need a bit different way to create this object for backward compatibility.
        /// </summary>
        /// <returns>A new empty instance of the System.Net.WebHeaderCollection.</returns>
        public abstract System.Net.WebHeaderCollection CreateEmptyWebHeaderCollection();

        /// <summary>
        /// Returns the underlying httpwebrequest instance if the httpStack is clientHttp. Otherwise returns null.
        /// </summary>
        /// <returns>The underlying httpwebrequest instance if the httpStack is clientHttp. Otherwise returns null.</returns>
        internal abstract System.Net.HttpWebRequest GetUnderlyingHttpRequest();

        /// <summary>Releases resources.</summary>
        /// <param name="disposing">Whether the dispose is being called explicitly rather than by the GC.</param>
        protected abstract void Dispose(bool disposing);
    }
}
