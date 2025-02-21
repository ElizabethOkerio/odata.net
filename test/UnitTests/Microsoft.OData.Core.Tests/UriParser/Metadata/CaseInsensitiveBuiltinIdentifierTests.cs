﻿//---------------------------------------------------------------------
// <copyright file="CaseInsensitiveBuiltinIdentifierUnitTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OData.Tests.UriParser.Binders;
using Microsoft.OData.UriParser;
using Microsoft.OData.Edm;
using Xunit;
using Microsoft.OData.Core;

namespace Microsoft.OData.Tests.UriParser.Metadata
{
    // not support by design: true false null $it
    // not support as not support (in ODL): $entity, $all, $crossjoin, $format, $skiptoken, $root, date, time
    public class CaseInsensitiveBuiltinIdentifierTests
        : ExtensionTestBase
    {
        #region path segment Tests
        [Fact]
        public void CaseInsensitiveBatchMetadataCountSegmentShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "$batch",
                "$Batch",
                uriParser => uriParser.ParsePath(),
                odataPath => odataPath.LastSegment.ShouldBeBatchSegment(),
                Error.Format(SRResources.RequestUriProcessor_ResourceNotFound, "$Batch"));

            this.TestCaseInsensitiveBuiltIn(
                "$metadata",
                "$METADATA",
                uriParser => uriParser.ParsePath(),
                odataPath => odataPath.LastSegment.ShouldBeMetadataSegment(),
                Error.Format(SRResources.RequestUriProcessor_ResourceNotFound, "$METADATA"));

            this.TestCaseInsensitiveBuiltIn(
                "People/$count",
                "People/$cOUNT",
                uriParser => uriParser.ParsePath(),
                odataPath => odataPath.LastSegment.ShouldBeCountSegment(),
                Error.Format(SRResources.RequestUriProcessor_CannotQueryCollections, "People"));
        }

        [Fact]
        public void CaseInsensitiveRefValueSegmentShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People(1)/MyDog/$ref",
                "People(1)/MyDog/$REF",
                uriParser => uriParser.ParsePath(),
                odataPath => odataPath.LastSegment.ShouldBeNavigationPropertyLinkSegment(HardCodedTestModel.GetPersonMyDogNavProp()),
                Error.Format(SRResources.RequestUriProcessor_ResourceNotFound, "$REF"));

            this.TestCaseInsensitiveBuiltIn(
                "People(1)/MyDog/$value",
                "People(1)/MyDog/$vaLue",
                uriParser => uriParser.ParsePath(),
                odataPath => odataPath.LastSegment.ShouldBeValueSegment(),
                Error.Format(SRResources.RequestUriProcessor_ResourceNotFound, "$vaLue"));
        }
        #endregion

        #region query option identifier Tests
        [Fact]
        public void CaseInsensitiveSelectExpandShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$select=Name",
                "People?$SELECT=Name",
                uriParser => uriParser.ParseSelectAndExpand(),
                clause => clause.SelectedItems.Single().ShouldBePathSelectionItem(new ODataSelectPath(
                new ODataPathSegment[]
                {
                    new PropertySegment(HardCodedTestModel.GetPersonNameProp()), 
                })),
                /*errorMessage*/ null);

            this.TestCaseInsensitiveBuiltIn(
                "People?$expand=MyDog",
                "People?$EXPAND=MyDog",
                uriParser => uriParser.ParseSelectAndExpand(),
                clause => clause.SelectedItems.Single().ShouldBeExpansionFor(HardCodedTestModel.GetPersonMyDogNavProp()),
                /*errorMessage*/ null);

            this.TestQueryOptionParserCaseInsensitiveBuiltIn(
                new Dictionary<string, string> { { "$expand", "MyDog" } },
                new Dictionary<string, string> { { "$EXPAND", "MyDog" } },
                uriParser => uriParser.ParseSelectAndExpand(),
                clause => clause.SelectedItems.Single().ShouldBeExpansionFor(HardCodedTestModel.GetPersonMyDogNavProp()),
                /*errorMessage*/ null);
        }

        [Fact]
        public void CaseInsensitiveFilterOrderbyShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=Name eq 'su'",
                "People?$FILTER=Name eq 'su'",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.Equal),
                /*errorMessage*/ null);

            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=Name",
                "People?$orderBY=Name",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeSingleValuePropertyAccessQueryNode(HardCodedTestModel.GetPersonNameProp()),
                /*errorMessage*/ null);
        }

        [Fact]
        public void CaseInsensitiveSearchShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$search=Name",
                "People?$SEARCH=Name",
                uriParser => uriParser.ParseSearch(),
                clause => clause.Expression.ShouldBeSearchTermNode("Name"),
                /*errorMessage*/ null);
        }

        [Fact]
        public void CaseInsensitiveTopSkipCountShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$top=1&$skip=2&$count=true",
                "People?$toP=1&$skIp=2&$COUnt=true",
                uriParser => uriParser.ParseTop(),
                val => Assert.Equal(1, val),
                /*errorMessage*/ null);

            this.TestCaseInsensitiveBuiltIn(
               "People?$top=1&$skip=2&$count=true",
               "People?$toP=1&$skIp=2&$COUnt=true",
               uriParser => uriParser.ParseSkip(),
               val => Assert.Equal(2, val),
                /*errorMessage*/ null);

            this.TestCaseInsensitiveBuiltIn(
               "People?$top=1&$skip=2&$count=true",
               "People?$toP=1&$skIp=2&$COUnt=true",
               uriParser => uriParser.ParseCount(),
               val => Assert.True(val),
                /*errorMessage*/ null);
        }

        [Fact]
        public void CaseInsensitiveIndexShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People(0)/RelatedIDs?$index=4",
                "People(0)/RelatedIDs?$iNDex=4",
                uriParser => uriParser.ParseIndex(),
                val => Assert.Equal(4, val),
                /*errorMessage*/ null);
        }

        [Fact]
        public void CaseInsensitiveIdShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People(0)/MyPaintings/$ref?$id=../../Paintings(3)",
                "People(0)/MyPaintings/$ref?$ID=../../Paintings(3)",
                uriParser => uriParser.ParseEntityId(),
                clause => Assert.Equal(clause.Id, new Uri("Paintings(3)", UriKind.Relative)),
                /*errorMessage*/ null);
        }

        [Fact]
        public void CaseInsensitiveNestedSelectExpandShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($select=Name)",
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($SELECT=Name)",
                uriParser => (uriParser.ParseSelectAndExpand().SelectedItems.Single() as ExpandedNavigationSelectItem).SelectAndExpand,
                clause => clause.SelectedItems.Single().ShouldBePathSelectionItem(new ODataSelectPath(
                new ODataPathSegment[]
                {
                    new PropertySegment(HardCodedTestModel.GetPersonNameProp()), 
                })),
                Error.Format(SRResources.UriSelectParser_TermIsNotValid, "($SELECT=Name)"));

            this.TestCaseInsensitiveBuiltIn(
              "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($expand=MyDog)",
              "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($EXPAND=MyDog)",
              uriParser => (uriParser.ParseSelectAndExpand().SelectedItems.Single() as ExpandedNavigationSelectItem).SelectAndExpand,
              clause => clause.SelectedItems.Single().ShouldBeExpansionFor(HardCodedTestModel.GetPersonMyDogNavProp()),
              Error.Format(SRResources.UriSelectParser_TermIsNotValid, "($EXPAND=MyDog)"));
        }

        [Fact]
        public void CaseInsensitiveNestedFilterOrderbyShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($filter=Name eq 'su')",
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($FILTER=Name eq 'su')",
                uriParser => (uriParser.ParseSelectAndExpand().SelectedItems.Single() as ExpandedNavigationSelectItem).FilterOption,
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.Equal),
                Error.Format(SRResources.UriSelectParser_TermIsNotValid, "($FILTER=Name eq 'su')"));

            this.TestCaseInsensitiveBuiltIn(
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($orderby=Name)",
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($orderBY=Name)",
                uriParser => (uriParser.ParseSelectAndExpand().SelectedItems.Single() as ExpandedNavigationSelectItem).OrderByOption,
                orderby => orderby.Expression.ShouldBeSingleValuePropertyAccessQueryNode(HardCodedTestModel.GetPersonNameProp()),
                Error.Format(SRResources.UriSelectParser_TermIsNotValid, "($orderBY=Name)"));
        }

        [Fact]
        public void CaseInsensitiveNestedSearchShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($search=Name)",
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($SEARCH=Name)",
                uriParser => (uriParser.ParseSelectAndExpand().SelectedItems.Single() as ExpandedNavigationSelectItem).SearchOption,
                clause => clause.Expression.ShouldBeSearchTermNode("Name"),
                Error.Format(SRResources.UriSelectParser_TermIsNotValid, "($SEARCH=Name)"));
        }

        [Fact]
        public void CaseInsensitiveNestedTopSkipCountShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($top=1;$skip=2;$count=true)",
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($toP=1;$skIp=2;$COUnt=true)",
                uriParser => (uriParser.ParseSelectAndExpand().SelectedItems.Single() as ExpandedNavigationSelectItem).TopOption,
                val => Assert.Equal(1, val),
                Error.Format(SRResources.UriSelectParser_TermIsNotValid, "($toP=1;$skIp=2;$COUnt=true)"));

            this.TestCaseInsensitiveBuiltIn(
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($top=1;$skip=2;$count=true)",
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($toP=1;$skIp=2;$COUnt=true)",
               uriParser => (uriParser.ParseSelectAndExpand().SelectedItems.Single() as ExpandedNavigationSelectItem).SkipOption,
               val => Assert.Equal(2, val),
               Error.Format(SRResources.UriSelectParser_TermIsNotValid, "($toP=1;$skIp=2;$COUnt=true)"));

            this.TestCaseInsensitiveBuiltIn(
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($top=1;$skip=2;$count=true)",
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($toP=1;$skIp=2;$COUnt=true)",
               uriParser => (uriParser.ParseSelectAndExpand().SelectedItems.Single() as ExpandedNavigationSelectItem).CountOption,
               val => Assert.True(val),
               Error.Format(SRResources.UriSelectParser_TermIsNotValid, "($toP=1;$skIp=2;$COUnt=true)"));
        }

        [Fact]
        public void CaseInsensitiveNestedLevelsMaxShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($levels=3)",
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($LEVELS=3)",
                uriParser => (uriParser.ParseSelectAndExpand().SelectedItems.Single() as ExpandedNavigationSelectItem).LevelsOption,
                clause => Assert.Equal(3, clause.Level),
                Error.Format(SRResources.UriSelectParser_TermIsNotValid, "($LEVELS=3)"));

            this.TestCaseInsensitiveBuiltIn(
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($levels=max)",
                "Boss/Fully.Qualified.Namespace.Manager?$expand=DirectReports($LEVELS=MAX)",
                uriParser => (uriParser.ParseSelectAndExpand().SelectedItems.Single() as ExpandedNavigationSelectItem).LevelsOption,
                clause => Assert.True(clause.IsMaxLevel),
                Error.Format(SRResources.UriSelectParser_TermIsNotValid, "($LEVELS=MAX)"));
        }

        [Fact]
        public void CaseInsensitiveSkipTokenShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$skiptoken=var1",
                "People?$SKIPTOKEN=var1",
                uriParser => uriParser.ParseSkipToken(),
                val => Assert.Equal("var1", val),
                /*errorMessage*/ null);
        }

        [Fact]
        public void CaseInsensitiveDeltaTokenShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$deltatoken=var1",
                "People?$DELTATOKEN=var1",
                uriParser => uriParser.ParseDeltaToken(),
                val => Assert.Equal("var1", val),
                /*errorMessage*/ null);
        }
        #endregion

        #region builtin functions Tests
        [Fact]
        public void CaseInsensitiveContainsShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=contains(Name,'SU')",
                "People?$filter=CONTAINS(Name,'SU')",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeSingleValueFunctionCallQueryNode("contains", EdmCoreModel.Instance.GetBoolean(false)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "CONTAINS"));
        }

        [Fact]
        public void CaseInsensitiveMatchesPatternShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=matchesPattern(Name,'SU')",
                "People?$filter=MATCHESPATTERN(Name,'SU')",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeSingleValueFunctionCallQueryNode("matchesPattern", EdmCoreModel.Instance.GetBoolean(false)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "MATCHESPATTERN"));
        }

        [Fact]
        public void CaseInsensitiveStartswithEndswithShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=startswith(Name,'SU')",
                "People?$filter=STARTSWITH(Name,'SU')",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeSingleValueFunctionCallQueryNode("startswith", EdmCoreModel.Instance.GetBoolean(false)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "STARTSWITH"));

            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=endswith(Name,'SU')",
                "People?$filter=ENDSWITH(Name,'SU')",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeSingleValueFunctionCallQueryNode("endswith", EdmCoreModel.Instance.GetBoolean(false)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "ENDSWITH"));
        }

        [Fact]
        public void CaseInsensitiveLengthwithShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=length(Name)",
                "People?$orderby=LENgTH(Name)",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("length", EdmCoreModel.Instance.GetInt32(false)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "LENgTH"));
        }

        [Fact]
        public void CaseInsensitiveIndexOfShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=indexof(Name, 'o')",
                "People?$orderby=INDEXOF(Name, 'o')",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("indexof", EdmCoreModel.Instance.GetInt32(false)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "INDEXOF"));
        }

        [Fact]
        public void CaseInsensitiveSubStringShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=substring(Name, 1, 2)",
                "People?$orderby=Substring(Name, 1, 2)",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("substring", EdmCoreModel.Instance.GetString(true)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "Substring"));
        }

        [Fact]
        public void CaseInsensitiveTolowerToupperShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=tolower(Name)",
                "People?$orderby=TOLOWER(Name)",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("tolower", EdmCoreModel.Instance.GetString(true)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "TOLOWER"));

            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=toupper(Name)",
                "People?$orderby=TOUPPER(Name)",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("toupper", EdmCoreModel.Instance.GetString(true)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "TOUPPER"));
        }

        [Fact]
        public void CaseInsensitiveTrimConcatShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=trim(Name)",
                "People?$orderby=TRIM(Name)",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("trim", EdmCoreModel.Instance.GetString(true)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "TRIM"));

            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=concat(Name,'sh')",
                "People?$orderby=Concat(Name,'sh')",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("concat", EdmCoreModel.Instance.GetString(true)),
                Error.Format(SRResources.MetadataBinder_UnknownFunction, "Concat"));
        }

        [Fact]
        public void CaseInsensitiveYearMonthDayShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=year(FavoriteDate)",
               "People?$orderby=YEAR(FavoriteDate)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("year", EdmCoreModel.Instance.GetInt32(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "YEAR"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=month(FavoriteDate)",
               "People?$orderby=MONTH(FavoriteDate)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("month", EdmCoreModel.Instance.GetInt32(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "MONTH"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=day(FavoriteDate)",
               "People?$orderby=DAY(FavoriteDate)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("day", EdmCoreModel.Instance.GetInt32(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "DAY"));
        }

        [Fact]
        public void CaseInsensitiveHourMinuteSecondFractionalsecondsShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=hour(FavoriteDate)",
               "People?$orderby=HoUR(FavoriteDate)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("hour", EdmCoreModel.Instance.GetInt32(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "HoUR"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=minute(FavoriteDate)",
               "People?$orderby=MinuTe(FavoriteDate)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("minute", EdmCoreModel.Instance.GetInt32(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "MinuTe"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=second(FavoriteDate)",
               "People?$orderby=sEcond(FavoriteDate)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("second", EdmCoreModel.Instance.GetInt32(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "sEcond"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=fractionalseconds(FavoriteDate)",
               "People?$orderby=frActionalseconds(FavoriteDate)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("fractionalseconds", EdmCoreModel.Instance.GetDecimal(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "frActionalseconds"));
        }

        [Fact]
        public void CaseInsensitiveRoundFloorCeilingShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=round(ID)",
               "People?$orderby=ROUND(ID)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("round", EdmCoreModel.Instance.GetDouble(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "ROUND"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=floor(ID)",
               "People?$orderby=flooR(ID)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("floor", EdmCoreModel.Instance.GetDouble(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "flooR"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=ceiling(ID)",
               "People?$orderby=CEILING(ID)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("ceiling", EdmCoreModel.Instance.GetDouble(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "CEILING"));
        }

        [Fact]
        public void CaseInsensitiveMindatetimeMaxdatetimeNowShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=mindatetime()",
               "People?$orderby=minDatetime()",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("mindatetime", EdmCoreModel.Instance.GetDateTimeOffset(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "minDatetime"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=maxdatetime()",
               "People?$orderby=MAxdatetime()",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("maxdatetime", EdmCoreModel.Instance.GetDateTimeOffset(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "MAxdatetime"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=now()",
               "People?$orderby=NOW()",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("now", EdmCoreModel.Instance.GetDateTimeOffset(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "NOW"));
        }

        [Fact]
        public void CaseInsensitiveTotalsecondsTotaloffsetminutesShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=totalseconds(TimeEmployed)",
               "People?$orderby=totalsecoNDs(TimeEmployed)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("totalseconds", EdmCoreModel.Instance.GetDecimal(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "totalsecoNDs"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=totaloffsetminutes(FavoriteDate)",
               "People?$orderby=totaloffsetminuteS(FavoriteDate)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("totaloffsetminutes", EdmCoreModel.Instance.GetInt32(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "totaloffsetminuteS"));
        }

        [Fact]
        public void CaseInsensitiveIsofCastShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=cast(1, Edm.String)",
               "People?$orderby=cAst(1, Edm.String)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("cast", EdmCoreModel.Instance.GetString(false)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "cAst"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=isof(1, Edm.String)",
               "People?$orderby=iSOf(1, Edm.String)",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("isof", EdmCoreModel.Instance.GetBoolean(true)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "iSOf"));
        }

        [Fact]
        public void CaseInsensitiveGeoFuncsShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=geo.distance(geometry'Point(10 30)', geometry'Point(7 28)')",
               "People?$orderby=geo.DIstance(geometry'Point(10 30)', geometry'Point(7 28)')",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("geo.distance", EdmCoreModel.Instance.GetDouble(true)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "geo.DIstance"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=geo.length(geometry'LineString(1 1, 2 2)')",
               "People?$orderby=geo.LENGTH(geometry'LineString(1 1, 2 2)')",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("geo.length", EdmCoreModel.Instance.GetDouble(true)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "geo.LENGTH"));

            this.TestCaseInsensitiveBuiltIn(
               "People?$orderby=geo.intersects(geometry'Point(10 30)',geometry'Polygon((10 30, 7 28, 6 6, 10 30))')",
               "People?$orderby=geo.iNTersects(geometry'Point(10 30)',geometry'Polygon((10 30, 7 28, 6 6, 10 30))')",
               uriParser => uriParser.ParseOrderBy(),
               orderby => orderby.Expression.ShouldBeSingleValueFunctionCallQueryNode("geo.intersects", EdmCoreModel.Instance.GetBoolean(true)),
               Error.Format(SRResources.MetadataBinder_UnknownFunction, "geo.iNTersects"));
        }
        #endregion

        #region expression Tests
        [Fact]
        public void CaseInsensitiveAnyAllShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "Dogs(1)?$filter=Nicknames/any(d:d eq 'a')",
                "Dogs(1)?$filter=Nicknames/ANY(d:d eq 'a')",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeAnyQueryNode(),
                Error.Format(SRResources.UriQueryExpressionParser_CloseParenOrCommaExpected, "15", "Nicknames/ANY(d:d eq 'a')"));

            this.TestCaseInsensitiveBuiltIn(
                "Dogs(1)?$filter=Nicknames/all(d:d eq 'a')",
                "Dogs(1)?$filter=Nicknames/aLL(d:d eq 'a')",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeAllQueryNode(),
                Error.Format(SRResources.UriQueryExpressionParser_CloseParenOrCommaExpected, "15", "Nicknames/aLL(d:d eq 'a')"));
        }

        [Fact]
        public void CaseInsensitiveAscDescShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=Name asc",
                "People?$orderby=Name aSC",
                uriParser => uriParser.ParseOrderBy(),
                orderby => Assert.Equal(OrderByDirection.Ascending, orderby.Direction),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "8", "Name aSC"));

            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=Name desc",
                "People?$orderby=Name DESC",
                uriParser => uriParser.ParseOrderBy(),
                orderby => Assert.Equal(OrderByDirection.Descending, orderby.Direction),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "9", "Name DESC"));
        }

        [Fact]
        public void CaseInsensitiveAndOrShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=true and false",
                "People?$filter=true AND false",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.And),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "8", "true AND false"));
            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=true or false",
                "People?$filter=true oR false",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.Or),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "7", "true oR false"));
        }

        [Fact]
        public void CaseInsensitiveAddSubShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=ID add 1",
                "People?$orderby=ID ADD 1",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.Add),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "6", "ID ADD 1"));

            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=ID sub 1",
                "People?$orderby=ID Sub 1",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.Subtract),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "6", "ID Sub 1"));
        }

        [Fact]
        public void CaseInsensitiveMulDivModShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=ID mul 1",
                "People?$orderby=ID mUl 1",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.Multiply),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "6", "ID mUl 1"));

            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=ID div 1",
                "People?$orderby=ID diV 1",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.Divide),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "6", "ID diV 1"));

            this.TestCaseInsensitiveBuiltIn(
                "People?$orderby=ID mod 1",
                "People?$orderby=ID MoD 1",
                uriParser => uriParser.ParseOrderBy(),
                orderby => orderby.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.Modulo),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "6", "ID MoD 1"));
        }

        [Fact]
        public void CaseInsensitiveNotShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=not false",
                "People?$filter=NOT false",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeUnaryOperatorNode(UnaryOperatorKind.Not),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "9", "NOT false"));
        }

        [Fact]
        public void CaseInsensitiveEqNeShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=Name eq 'su'",
                "People?$filter=Name EQ 'su'",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.Equal),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "7", "Name EQ 'su'"));

            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=Name ne 'su'",
                "People?$filter=Name NE 'su'",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.NotEqual),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "7", "Name NE 'su'"));
        }

        [Fact]
        public void CaseInsensitiveLtLeShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=ID lt 1",
                "People?$filter=ID LT 1",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.LessThan),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "5", "ID LT 1"));

            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=ID le 1",
                "People?$filter=ID LE 1",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.LessThanOrEqual),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "5", "ID LE 1"));
        }

        [Fact]
        public void CaseInsensitiveGtGeShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=ID gt 1",
                "People?$filter=ID GT 1",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.GreaterThan),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "5", "ID GT 1"));

            this.TestCaseInsensitiveBuiltIn(
                "People?$filter=ID ge 1",
                "People?$filter=ID GE 1",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.GreaterThanOrEqual),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "5", "ID GE 1"));
        }

        [Fact]
        public void CaseInsensitiveHasShouldWork()
        {
            this.TestCaseInsensitiveBuiltIn(
                "Pet2Set?$filter=PetColorPattern has Fully.Qualified.Namespace.ColorPattern'SolidYellow'",
                "Pet2Set?$filter=PetColorPattern HaS Fully.Qualified.Namespace.ColorPattern'SolidYellow'",
                uriParser => uriParser.ParseFilter(),
                filter => filter.Expression.ShouldBeBinaryOperatorNode(BinaryOperatorKind.Has),
                Error.Format(SRResources.ExpressionLexer_SyntaxError, "19", "PetColorPattern HaS Fully.Qualified.Namespace.ColorPattern'SolidYellow'"));
        }
        #endregion

        private void TestCaseInsensitiveBuiltIn<TResult>(string original, string caseInsensitive, Func<ODataUriParser, TResult> parse, Action<TResult> verify, string errorMessage)
        {
            this.TestUriParserExtension(original, caseInsensitive, parse, verify, errorMessage, HardCodedTestModel.TestModel, (parser) => parser.Resolver = new ODataUriResolver { EnableCaseInsensitive = true });
        }

        private void TestQueryOptionParserCaseInsensitiveBuiltIn<TResult>(
            Dictionary<string, string> original,
            Dictionary<string, string> caseInsensitive,
            Func<ODataQueryOptionParser, TResult> parse,
            Action<TResult> verify,
            string errorMessage)
        {
            this.TestExtension(
                () => new ODataQueryOptionParser(HardCodedTestModel.TestModel, HardCodedTestModel.GetPersonType(), HardCodedTestModel.GetPeopleSet(), original) { Resolver = new ODataUriResolver() { EnableCaseInsensitive = false } },
                () => new ODataQueryOptionParser(HardCodedTestModel.TestModel, HardCodedTestModel.GetPersonType(), HardCodedTestModel.GetPeopleSet(), caseInsensitive) { Resolver = new ODataUriResolver() { EnableCaseInsensitive = false } },
                parse, verify, errorMessage, HardCodedTestModel.TestModel, (parser) => parser.Resolver = new ODataUriResolver { EnableCaseInsensitive = true });
        }
    }
}
