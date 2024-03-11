//---------------------------------------------------------------------
// <copyright file="ODataServiceColectionExtensions.cs" company="Microsoft">
// Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Json;
using Microsoft.OData.UriParser;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ODataServiceColectionExtensions
    {
        /// <summary>
        /// Adds the default OData services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance itself.</returns>
        public static IServiceCollection AddDefaultODataServices(this IServiceCollection services)
        {
            return AddDefaultODataServices(services, ODataVersion.V4);
        }

        /// <summary>
        /// Adds the default OData services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="odataVersion">ODataVersion for the default services.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance itself</returns>
        public static IServiceCollection AddDefaultODataServices(this IServiceCollection services, ODataVersion odataVersion)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IJsonReaderFactory, DefaultJsonReaderFactory>();
            services.AddSingleton<IJsonWriterFactory, DefaultJsonWriterFactory>();
            services.AddSingleton(sp => ODataMediaTypeResolver.GetMediaTypeResolver(null));
            services.AddScoped<ODataMessageInfo>();
            services.AddSingleton(new ODataMessageReaderSettings());
            services.AddScoped(sp => sp.GetRequiredService<ODataMessageReaderSettings>().Clone());
            services.AddSingleton(new ODataMessageWriterSettings());
            services.AddScoped(sp => sp.GetRequiredService<ODataMessageWriterSettings>().Clone());
            services.AddSingleton(sp => ODataPayloadValueConverter.GetPayloadValueConverter(null));
            services.AddSingleton<IEdmModel>(sp => EdmCoreModel.Instance);
            services.AddSingleton(sp => ODataUriResolver.GetUriResolver(null));
            services.AddScoped<ODataUriParserSettings>();
            services.AddScoped<UriPathParser>();

            return services;
        }
    }
}
