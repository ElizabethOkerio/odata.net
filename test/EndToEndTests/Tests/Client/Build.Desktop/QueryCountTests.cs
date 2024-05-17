﻿//---------------------------------------------------------------------
// <copyright file="QueryCountTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.Test.OData.Tests.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using Microsoft.OData.Client;
    using Microsoft.Test.OData.Framework.Client;
    using Microsoft.Test.OData.Framework.Verification;
    using Microsoft.Test.OData.Services.TestServices;
    using Microsoft.Test.OData.Services.TestServices.AstoriaDefaultServiceReference;
    using Microsoft.Test.OData.Tests.Client.Common;
    using Xunit;
    using Xunit.Abstractions;
    using HttpWebRequestMessage = Microsoft.Test.OData.Tests.Client.Common.HttpWebRequestMessage;

    /// <summary>
    /// Query $count test cases.
    /// </summary>
    public class QueryCountTests : EndToEndTestBase
    {
        public QueryCountTests(ITestOutputHelper helper)
            : base(ServiceDescriptors.AstoriaDefaultService, helper)
        {
        }

        protected List<string> mimeTypes = new List<string>()
        {
            //MimeTypes.ApplicationAtomXml,
            MimeTypes.ApplicationJson + MimeTypes.ODataParameterIEEE754Compatible,
            MimeTypes.ApplicationJson + MimeTypes.ODataParameterFullMetadata,
            MimeTypes.ApplicationJson + MimeTypes.ODataParameterMinimalMetadata,
            MimeTypes.ApplicationJson + MimeTypes.ODataParameterNoMetadata,
        };

        #region positive tests

        /// <summary>
        /// IncludeCount Test
        /// </summary>
        [Fact]
        public void CountLinqTest()
        {
            var context = this.CreateWrappedContext<DefaultContainer>().Context;

            var query = context.Customer.IncludeCount();
            Assert.Equal(true, query.ToString().Contains("$count=true"));

            var response = query.Execute() as QueryOperationResponse<Customer>;
            Assert.Equal(response.Count, 10);
        }

        /// <summary>
        /// IncludeCount(true) Test
        /// </summary>
        [Fact]
        public void CountWithBoolTrueParamLinqTest()
        {
            var context = this.CreateWrappedContext<DefaultContainer>().Context;

            var query = context.Customer.IncludeCount(true);
            Assert.Equal(true, query.ToString().Contains("$count=true"));

            var response = query.Execute() as QueryOperationResponse<Customer>;
            Assert.Equal(response.Count, 10);
        }

        /// <summary>
        /// Query Entity Set  With Server Driven Paging
        /// </summary>
        [Fact]
        public void CountLinqTestWithServerDrivenPaging()
        {
            var context = this.CreateWrappedContext<DefaultContainer>().Context;

            var query = context.Computer.IncludeCount();
            Assert.Equal(true, query.ToString().Contains("$count=true"));

            var response = query.Execute() as QueryOperationResponse<Computer>;
            Assert.Equal(response.Count, 10);
        }

        /// <summary>
        /// Normal $count request
        /// </summary>
        [Fact]
        public void CountUriTest()
        {
            var context = this.CreateWrappedContext<DefaultContainer>().Context;

            var response = context.Execute<Computer>(new Uri(this.ServiceUri.OriginalString + "/Computer?$count=true")) as QueryOperationResponse<Computer>;
            Assert.Equal(10, response.Count);

            response = context.Execute<Computer>(new Uri(this.ServiceUri.OriginalString + "/Computer?$expand=ComputerDetail&$count=true")) as QueryOperationResponse<Computer>;
            Assert.Equal(10, response.Count);

            response = context.Execute<Computer>(new Uri(this.ServiceUri.OriginalString + "/Computer?$top=5&$count=true")) as QueryOperationResponse<Computer>;
            Assert.Equal(10, response.Count);

            response = context.Execute<Computer>(new Uri(this.ServiceUri.OriginalString + "/Computer?$count=true&$skip=5")) as QueryOperationResponse<Computer>;
            Assert.Equal(10, response.Count);

            response = context.Execute<Computer>(new Uri(this.ServiceUri.OriginalString + "/Computer?$skip=5&$count=true&$top=10")) as QueryOperationResponse<Computer>;
            Assert.Equal(10, response.Count);
        }

        /// <summary>
        /// Normal $count request With Server Driven Paging
        /// </summary>
        [Fact]
        public void CountUriTestWithServerDrivenPaging()
        {
            var context = this.CreateWrappedContext<DefaultContainer>().Context;

            var response = context.Execute<Customer>(new Uri(this.ServiceUri.OriginalString + "/Customer?$count=true")) as QueryOperationResponse<Customer>;
            Assert.Equal(10, response.Count);

            response = context.Execute<Customer>(new Uri(this.ServiceUri.OriginalString + "/Customer?$expand=Orders&$count=true")) as QueryOperationResponse<Customer>;
            Assert.Equal(10, response.Count);

            response = context.Execute<Customer>(new Uri(this.ServiceUri.OriginalString + "/Customer?$top=5&$count=true")) as QueryOperationResponse<Customer>;
            Assert.Equal(10, response.Count);

            response = context.Execute<Customer>(new Uri(this.ServiceUri.OriginalString + "/Customer?$count=true&$skip=5")) as QueryOperationResponse<Customer>;
            Assert.Equal(10, response.Count);

            response = context.Execute<Customer>(new Uri(this.ServiceUri.OriginalString + "/Customer?$skip=5&$count=true&$top=10")) as QueryOperationResponse<Customer>;
            Assert.Equal(10, response.Count);

            var orders = context.Execute<Order>(new Uri(this.ServiceUri.OriginalString + "/Customer(-10)/Orders?$count=true")) as QueryOperationResponse<Order>;
            Assert.Equal(3, orders.Count);
        }

        /// <summary>
        /// when $count=true results 
        /// odata.count in json payload 
        /// </summary>
        [Fact]
        public void CountPayloadVerification()
        {
            foreach (var mimeType in mimeTypes)
            {
                var requestMessage = new HttpWebRequestMessage(new Uri(this.ServiceUri.OriginalString + "/Customer?$count=true", UriKind.Absolute));
                requestMessage.SetHeader("Accept", mimeType);

                var responseString = this.GetResponseContent(requestMessage);

                if (mimeType.Contains(MimeTypes.ApplicationJson))
                {
                    if (mimeType.Contains(MimeTypes.ODataParameterIEEE754Compatible))
                    {
                        Assert.True(responseString.Contains("\"@odata.count\":\"10\""));
                    }
                    else
                    {
                        Assert.True(responseString.Contains("\"@odata.count\":10"));
                    }
                }
            }
        }

        /// <summary>
        /// $count=false is the default value 
        /// payload should be same with specifying nothing
        /// </summary>
        [Fact]
        public void FalseCountIsDefaultValue()
        {
            foreach (var mimeType in mimeTypes)
            {
                var url = "/Computer";
                var requestMessage = new HttpWebRequestMessage(new Uri(this.ServiceUri.OriginalString + url, UriKind.Absolute));
                requestMessage.SetHeader("Accept", mimeType);

                var defaultResponsestring = this.GetResponseContent(requestMessage);

                url = "/Computer?$count=false";
                requestMessage = new HttpWebRequestMessage(new Uri(this.ServiceUri.OriginalString + url, UriKind.Absolute));
                requestMessage.SetHeader("Accept", mimeType);

                var responseString = this.GetResponseContent(requestMessage);

                Assert.Equal(defaultResponsestring, responseString);
            }
        }

        private string GetResponseContent(HttpWebRequestMessage requestMessage)
        {
            var response = requestMessage.GetResponse();
            var stream = response.GetStream();
            var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }

        #endregion

        #region negative tests

        [Fact]
        public void CountUriInvalidTest()
        {
            var context = this.CreateWrappedContext<DefaultContainer>().Context;

            String errorUrl = null;

            //$count is an invalid query option
            errorUrl = "/Computer?$count";
            this.VerifyErrorString(context, errorUrl, "HttpContextServiceHost_QueryParameterMustBeSpecifiedOnce", "$count");

            //$count=true is an invalid segment
            errorUrl = "/Computer/$count=true";
            this.VerifyErrorString(context, errorUrl, "RequestUriProcessor_CannotQueryCollections", "Computer");

            //
            errorUrl = "/Computer/$count?$count=true";
            this.VerifyErrorString(context, errorUrl, "RequestQueryProcessor_QueryCountWithSegmentCount");

            //case sensitive test
            errorUrl = "/Computer?$count=True";
            this.VerifyErrorString(context, errorUrl, "RequestQueryProcessor_InvalidCountOptionError");

            errorUrl = "/Computer?$Count=false";
            this.VerifyErrorString(context, errorUrl, "HttpContextServiceHost_UnknownQueryParameter", "$Count");

            //$count is only applicable for collection of entities
            errorUrl = "/Computer(11)?$count=true";
            this.VerifyErrorString(context, errorUrl, "RequestQueryProcessor_QuerySetOptionsNotApplicable");

            errorUrl = "/Computer(11)/ComputerDetail/SpecificationsBag?$count=true";
            this.VerifyErrorString(context, errorUrl, "RequestQueryProcessor_QuerySetOptionsNotApplicable");
        }

        private void VerifyErrorString(DataServiceContext context, string errorUrl, string errorString, params object[] arguments)
        {
            try
            {
                context.Execute<Computer>(new Uri(this.ServiceUri.OriginalString + errorUrl));
                Assert.True(false, "Expected Exception not thrown for " + errorUrl);
            }
            catch (DataServiceQueryException ex)
            {
                Assert.NotNull(ex.InnerException);
                Assert.IsType<DataServiceClientException>(ex.InnerException);
                StringResourceUtil.VerifyDataServicesString(ClientExceptionUtil.ExtractServerErrorMessage(ex), errorString, arguments);
            }
        }

        /// <summary>
        /// Invalid getting count when $count=false
        /// </summary>
        [Fact]
        public void GetTotalCountInvalidTest()
        {
            string[] requests = new string[]
            {
                "/Customer",
                "/Customer?$count=false" 
            };
            var context = this.CreateWrappedContext<DefaultContainer>().Context;

            foreach (var request in requests)
            {
                try
                {
                    var response = context.Execute<Customer>(new Uri(this.ServiceUri.OriginalString + request)) as QueryOperationResponse<Customer>;
                    var count = response.Count;
                    Assert.True(false, "Should fail on getting customer count");
                }
                catch (Exception ex)
                {
                    Assert.Equal(true, ex.Message.Contains("Count value is not part of the response stream"));
                }
            }
        }

        /// <summary>
        /// Invalid getting count when IncludeCount(false)
        /// </summary>
        [Fact]
        public void CountWithBoolFalseParamLinqTest()
        {
            var context = this.CreateWrappedContext<DefaultContainer>().Context;
            var query = context.Customer.IncludeCount(false);

            try
            {
                var response = query.Execute() as QueryOperationResponse<Customer>;
                var count = response.Count;
                Assert.True(false, "Should fail on getting customer count");
            }
            catch (Exception ex)
            {
                Assert.Equal(true, ex.Message.Contains("Count value is not part of the response stream"));
            }
        }
        #endregion
    }
}
