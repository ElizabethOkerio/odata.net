﻿//---------------------------------------------------------------------
// <copyright file="OasisRelationshipChangesAcceptanceTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;
using Xunit;

namespace Microsoft.OData.Edm.Tests.ScenarioTests
{
    public partial class OasisRelationshipChangesAcceptanceTests
    {
        private const string RepresentativeEdmxDocument = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""EntityType"">
        <Key>
          <PropertyRef Name=""ID1"" />
          <PropertyRef Name=""ID2"" />
        </Key>
        <Property Name=""ID1"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""ID2"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""ForeignKeyId1"" Type=""Edm.Int32"" />
        <Property Name=""ForeignKeyId2"" Type=""Edm.Int32"" />
        <Property Name=""ForeignKeyProperty"" Type=""Edm.Int32"" />
        <NavigationProperty Name=""navigation"" Type=""Collection(Test.EntityType)"" Partner=""NAVIGATION"" ContainsTarget=""true"">
          <OnDelete Action=""Cascade"" />
        </NavigationProperty>
        <NavigationProperty Name=""NAVIGATION"" Type=""Test.EntityType"" Partner=""navigation"">
          <ReferentialConstraint Property=""ForeignKeyId2"" ReferencedProperty=""ID2"" />
          <ReferentialConstraint Property=""ForeignKeyId1"" ReferencedProperty=""ID1"" />
        </NavigationProperty>
        <NavigationProperty Name=""NonKeyPrincipalNavigation"" Type=""Test.EntityType"" Partner=""OtherNavigation"">
          <ReferentialConstraint Property=""ForeignKeyProperty"" ReferencedProperty=""ID1"" />
        </NavigationProperty>
        <NavigationProperty Name=""OtherNavigation"" Type=""Collection(Test.EntityType)"" Partner=""NonKeyPrincipalNavigation"" />
      </EntityType>
      <EntityType Name=""DerivedEntityType"" BaseType=""Test.EntityType"">
        <NavigationProperty Name=""DerivedNavigation"" Type=""Test.DerivedEntityType"" Nullable=""false"" />
      </EntityType>
      <EntityContainer Name=""Container"">
        <EntitySet Name=""EntitySet1"" EntityType=""Test.EntityType"">
          <NavigationPropertyBinding Path=""navigation"" Target=""EntitySet1"" />
          <NavigationPropertyBinding Path=""NAVIGATION"" Target=""EntitySet1"" />
          <NavigationPropertyBinding Path=""NonKeyPrincipalNavigation"" Target=""EntitySet1"" />
          <NavigationPropertyBinding Path=""Test.DerivedEntityType/DerivedNavigation"" Target=""EntitySet1"" />
        </EntitySet>
        <EntitySet Name=""EntitySet2"" EntityType=""Test.EntityType"">
          <NavigationPropertyBinding Path=""navigation"" Target=""EntitySet2"" />
          <NavigationPropertyBinding Path=""NAVIGATION"" Target=""EntitySet2"" />
          <NavigationPropertyBinding Path=""NonKeyPrincipalNavigation"" Target=""EntitySet2"" />
          <NavigationPropertyBinding Path=""Test.DerivedEntityType/DerivedNavigation"" Target=""EntitySet2"" />
        </EntitySet>
      </EntityContainer>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

        private const string navPropBindingtemplate = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityContainer Name=""Container"">
        <EntitySet Name=""EntitySet"" EntityType=""Test.EntityType"">
          {0}
        </EntitySet>
      </EntityContainer>
      <EntityType Name=""EntityType"">
        <Key>
          <PropertyRef Name=""ID""/>
        </Key>
        <Property Name=""ID"" Nullable=""false"" Type=""Edm.Int32""/>
        <NavigationProperty Name=""Navigation"" Type=""Collection(Test.EntityType)"" />
      </EntityType>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

        private readonly IEdmModel representativeModel;
        private readonly IEdmEntitySet entitySet1;
        private readonly IEdmEntitySet entitySet2;
        private readonly IEdmEntityType entityType;
        private readonly IEdmNavigationProperty navigation1;
        private readonly IEdmNavigationProperty navigation2;
        private readonly IEdmNavigationProperty nonKeyPrincipalNavigation;
        private readonly IEdmNavigationProperty derivedNavigation;

        public OasisRelationshipChangesAcceptanceTests()
        {
            this.representativeModel = CsdlReader.Parse(XElement.Parse(RepresentativeEdmxDocument).CreateReader());
            var container = this.representativeModel.EntityContainer;
            this.entitySet1 = container.FindEntitySet("EntitySet1");
            this.entitySet2 = container.FindEntitySet("EntitySet2");
            this.entityType = this.representativeModel.FindType("Test.EntityType") as IEdmEntityType;

            Assert.NotNull(this.entitySet1);
            Assert.NotNull(this.entitySet2);
            Assert.NotNull(this.entityType);

            this.navigation1 = this.entityType.FindProperty("navigation") as IEdmNavigationProperty;
            this.navigation2 = this.entityType.FindProperty("NAVIGATION") as IEdmNavigationProperty;
            nonKeyPrincipalNavigation = this.entityType.FindProperty("NonKeyPrincipalNavigation") as IEdmNavigationProperty;

            var derivedType = this.representativeModel.FindType("Test.DerivedEntityType") as IEdmEntityType;
            Assert.NotNull(derivedType);
            this.derivedNavigation = derivedType.FindProperty("DerivedNavigation") as IEdmNavigationProperty;

            Assert.NotNull(this.navigation1);
            Assert.NotNull(this.navigation2);
            Assert.NotNull(this.derivedNavigation);
        }

        [Fact]
        public void RepresentativeModelShouldBeValid()
        {
            IEnumerable<EdmError> errors;
            Assert.True(this.representativeModel.Validate(out errors));
            Assert.Empty(errors);
        }

        [Fact]
        public void FindNavigationTargetShouldUseBinding()
        {
            Assert.Same(this.entitySet2, this.entitySet2.FindNavigationTarget(this.navigation2));
            Assert.Same(this.entitySet1, this.entitySet1.FindNavigationTarget(this.derivedNavigation));
        }

        [Fact]
        public void ReferenceNavigationPropertyTypeShouldContinueToWork()
        {
            Assert.Same(this.entityType, this.navigation2.Type.Definition);
        }

        [Fact]
        public void CollectionNavigationPropertyTypeShouldContinueToWork()
        {
            Assert.Equal(EdmTypeKind.Collection, this.navigation1.Type.TypeKind());
            Assert.Same(this.entityType, this.navigation1.Type.AsCollection().ElementType().Definition);
        }

        [Fact]
        public void OnDeleteShouldContinueToWork()
        {
            Assert.Equal(EdmOnDeleteAction.Cascade, this.navigation1.OnDelete);
            Assert.Equal(EdmOnDeleteAction.None, this.navigation2.OnDelete);
        }

        [Fact]
        public void ReferentialConstraintShouldContinueToWork()
        {
            Assert.Null(this.navigation1.DependentProperties());
            var properties = this.navigation2.DependentProperties();
            Assert.Contains(this.entityType.FindProperty("ForeignKeyId1") as IEdmStructuralProperty, properties);
            Assert.Contains(this.entityType.FindProperty("ForeignKeyId2") as IEdmStructuralProperty, properties);
        }

        [Fact]
        public void ReferentialConstraintShouldWorkForNonKeyPrincipalProperties()
        {
            Assert.Contains(this.entityType.FindProperty("ForeignKeyProperty") as IEdmStructuralProperty, this.nonKeyPrincipalNavigation.DependentProperties());
        }

        [Fact]
        public void IsPrincipalShouldContinueToWork()
        {
            Assert.True(this.navigation1.IsPrincipal());
            Assert.False(this.navigation2.IsPrincipal());
        }

        [Fact]
        public void PartnerShouldContinueToWork()
        {
            Assert.Same(this.navigation2, this.navigation1.Partner);
            Assert.Same(this.navigation1, this.navigation2.Partner);
        }

        [Fact]
        public void ContainsTargetShouldContinueToWork()
        {
            Assert.True(this.navigation1.ContainsTarget);
            Assert.False(this.navigation2.ContainsTarget);
        }

        [Fact]
        public void NavigationTargetMappingsShouldContainAllBindings()
        {
            var bindings = this.entitySet1.NavigationPropertyBindings;
            Assert.Equal(4, bindings.Count());
            Assert.Contains(bindings, m => m.NavigationProperty == this.navigation1 && m.Target == this.entitySet1);
            Assert.Contains(bindings, m => m.NavigationProperty == this.navigation2 && m.Target == this.entitySet1);
            Assert.Contains(bindings, m => m.NavigationProperty == this.derivedNavigation && m.Target == this.entitySet1);
            bindings = this.entitySet2.NavigationPropertyBindings;
            Assert.Equal(4, bindings.Count());
            Assert.Contains(bindings, m => m.NavigationProperty == this.navigation1 && m.Target == this.entitySet2);
            Assert.Contains(bindings, m => m.NavigationProperty == this.navigation2 && m.Target == this.entitySet2);
            Assert.Contains(bindings, m => m.NavigationProperty == this.derivedNavigation && m.Target == this.entitySet2);
        }

        [Fact]
        public void WriterShouldContinueToWork()
        {
            var builder = new StringBuilder();
            using (var writer = XmlWriter.Create(builder))
            {
                IEnumerable<EdmError> errors;
                var result = CsdlWriter.TryWriteCsdl(this.representativeModel, writer, CsdlTarget.OData, out errors);
                Assert.True(result);
                Assert.Empty(errors);
                writer.Flush();
            }

            string actual = builder.ToString();
            var actualXml = XElement.Parse(actual);
            var actualNormalized = actualXml.ToString();

            Assert.Equal(RepresentativeEdmxDocument, actualNormalized);
        }

        [Fact]
        public void ValidationShouldFailIfABindingToANonExistentPropertyIsFound()
        {
            this.ValidateBindingWithExpectedErrors(
                @"<NavigationPropertyBinding Path=""NonExistent"" Target=""EntitySet"" />",
                EdmErrorCode.BadUnresolvedNavigationPropertyPath,
                Error.Format(SRResources.Bad_UnresolvedNavigationPropertyPath, "NonExistent", "Test.EntityType"));
        }

        [Fact]
        public void ValidationShouldFailIfABindingToANonExistentSetIsFound()
        {
            this.ValidateBindingWithExpectedErrors(
                @"<NavigationPropertyBinding Path=""Navigation"" Target=""NonExistent"" />",
                EdmErrorCode.BadUnresolvedEntitySet,
                Error.Format(SRResources.Bad_UnresolvedEntitySet, "NonExistent"));
        }

        [Fact]
        public void ValidationShouldFailIfADerivedPropertyIsUsedWithoutATypeCast()
        {
            this.ValidateBindingWithExpectedErrors(
                @"<NavigationPropertyBinding Path=""DerivedNavigation"" Target=""EntitySet"" />",
                EdmErrorCode.BadUnresolvedNavigationPropertyPath,
                Error.Format(SRResources.Bad_UnresolvedNavigationPropertyPath, "DerivedNavigation", "Test.EntityType"));
        }

        [Fact]
        public void ValidationShouldFailIfATypeCastIsFollowedByANonExistentProperty()
        {
            this.ValidateBindingWithExpectedErrors(
                @"<NavigationPropertyBinding Path=""Test.DerivedEntityType/NonExistent"" Target=""EntitySet"" />",
                EdmErrorCode.BadUnresolvedNavigationPropertyPath,
                Error.Format(SRResources.Bad_UnresolvedNavigationPropertyPath, "Test.DerivedEntityType/NonExistent", "Test.EntityType"));
        }

        [Fact]
        public void ParsingShouldFailIfABindingIsMissingTarget()
        {
            this.ParseBindingWithExpectedErrors(
                @"<NavigationPropertyBinding Path=""Navigation"" />",
                EdmErrorCode.MissingAttribute,
                Error.Format(SRResources.XmlParser_MissingAttribute, "Target", "NavigationPropertyBinding"));
        }

        [Fact]
        public void ParsingShouldFailIfABindingIsMissingPath()
        {
            this.ParseBindingWithExpectedErrors(
                @"<NavigationPropertyBinding Target=""EntitySet"" />",
                EdmErrorCode.MissingAttribute,
                Error.Format(SRResources.XmlParser_MissingAttribute, "Path", "NavigationPropertyBinding"));
        }

        [Fact]
        public void ParsingShouldFailIfABindingHasExtraAttributes()
        {
            this.ParseBindingWithExpectedErrors(
                @"<NavigationPropertyBinding Path=""Navigation"" Target=""EntitySet"" Something=""else"" Foo=""bar"" />",
                EdmErrorCode.UnexpectedXmlAttribute,
                Error.Format(SRResources.XmlParser_UnexpectedAttribute, "Something"),
                Error.Format(SRResources.XmlParser_UnexpectedAttribute, "Foo"));
        }

        [Fact]
        public void ParsingShouldNotFailIfABindingHasAnnotations()
        {
            const string validBinding = @"
              <NavigationPropertyBinding Path=""Navigation"" Target=""EntitySet"">
                <Annotation Term=""FQ.NS.Term""/>
              </NavigationPropertyBinding>";

            this.ParseBindingWithExpectedErrors(validBinding);
        }

        [Fact]
        public void ParsingShouldFailIfAConstraintIsMissingProperty()
        {
            this.ParseReferentialConstraintWithExpectedErrors(
                @"<ReferentialConstraint ReferencedProperty=""ID1"" />",
                EdmErrorCode.MissingAttribute,
                Error.Format(SRResources.XmlParser_MissingAttribute, "Property", "ReferentialConstraint"));
        }

        [Fact]
        public void ParsingShouldFailIfAConstraintIsMissingReferencedProperty()
        {
            this.ParseReferentialConstraintWithExpectedErrors(
                @"<ReferentialConstraint Property=""ForeignKeyId1"" />",
                EdmErrorCode.MissingAttribute,
                Error.Format(SRResources.XmlParser_MissingAttribute, "ReferencedProperty", "ReferentialConstraint"));
        }

        [Fact]
        public void ParsingShouldFailIfAConstraintHasExtraAttributes()
        {
            this.ParseReferentialConstraintWithExpectedErrors(
                @"
              <ReferentialConstraint Property=""ForeignKeyId1"" ReferencedProperty=""ID1"" Something=""else"" Foo=""bar"" />",
                EdmErrorCode.UnexpectedXmlAttribute,
                Error.Format(SRResources.XmlParser_UnexpectedAttribute, "Something"),
                Error.Format(SRResources.XmlParser_UnexpectedAttribute, "Foo"));
        }

        [Fact]
        public void ParsingShouldNotFailIfAConstraintHasAnnotations()
        {
            const string validConstraint = @"
              <ReferentialConstraint Property=""ForeignKeyId1"" ReferencedProperty=""ID1"">
                <Annotation Term=""FQ.NS.Term""/>
              </ReferentialConstraint>";

            this.ParseReferentialConstraint(validConstraint);
        }

        [Fact]
        public void ValidationShouldFailIfAConstraintOnANonExistentPropertyIsFound()
        {
            this.ValidateReferentialConstraintWithExpectedErrors(
                @"<ReferentialConstraint Property=""NonExistent"" ReferencedProperty=""ID1"" />",
                EdmErrorCode.BadUnresolvedProperty,
                Error.Format(SRResources.Bad_UnresolvedProperty, "NonExistent")
                );
        }

        [Fact]
        public void ValidationShouldFailIfAConstraintOnANonExistentReferencedPropertyIsFound()
        {
            this.ValidateReferentialConstraintWithExpectedErrors(
                @"<ReferentialConstraint Property=""ForeignKeyId1"" ReferencedProperty=""NonExistent"" />",
                EdmErrorCode.BadUnresolvedProperty,
                Error.Format(SRResources.Bad_UnresolvedProperty, "NonExistent"));
        }

        [Fact]
        public void ParsingShouldFailIfANavigationHasMultipleOnDeleteElements()
        {
            this.ParseNavigationExpectedErrors(
                @"<NavigationProperty Name=""Navigation"" Type=""Test.EntityType"">
                    <OnDelete Action=""Cascade"" />
                    <OnDelete Action=""None"" />
                  </NavigationProperty>",
                EdmErrorCode.UnexpectedXmlElement,
                Error.Format(SRResources.XmlParser_UnusedElement, "OnDelete"));
        }

        [Fact]
        public void ParsingShouldFailIfANavigationHasAnInvalidOnDeleteAction()
        {
            this.ParseNavigationExpectedErrors(
                @"<NavigationProperty Name=""Navigation"" Type=""Test.EntityType"">
                    <OnDelete Action=""Foo"" />
                  </NavigationProperty>",
                EdmErrorCode.InvalidOnDelete,
                Error.Format(SRResources.CsdlParser_InvalidDeleteAction, "Foo"));
        }

        [Fact]
        public void ParsingShouldFailIfANavigationIsMissingType()
        {
            this.ParseNavigationExpectedErrors(
                @"<NavigationProperty Name=""Navigation"" />",
                EdmErrorCode.MissingAttribute,
                Error.Format(SRResources.XmlParser_MissingAttribute, "Type", "NavigationProperty"));
        }

        [Fact]
        public void ParsingShouldNotFailIfANavigationIsMissingPartner()
        {
            this.ParseNavigationExpectedErrors(@"<NavigationProperty Name=""Navigation"" Type=""Collection(Test.EntityType)"" />");
        }

        [Fact]
        public void ParsingShouldFailIfNavigationTypeIsEmpty()
        {
            this.ParseNavigationExpectedErrors(@"<NavigationProperty Name=""Navigation"" Type="""" />",
                EdmErrorCode.InvalidTypeName,
                Error.Format(SRResources.CsdlParser_InvalidTypeName, ""));
        }

        [Fact]
        public void ParsingShouldFailIfNavigationNullableIsEmpty()
        {
            this.ParseNavigationExpectedErrors(@"<NavigationProperty Name=""Navigation"" Type=""Test.EntityType"" Nullable=""""/>",
                EdmErrorCode.InvalidBoolean,
                Error.Format(SRResources.ValueParser_InvalidBoolean, ""));
        }

        [Fact]
        public void ParsingShouldFailIfNavigationNullableIsNotTrueOrFalse()
        {
            this.ParseNavigationExpectedErrors(@"<NavigationProperty Name=""Navigation"" Type=""Test.EntityType"" Nullable=""foo""/>",
                EdmErrorCode.InvalidBoolean,
                Error.Format(SRResources.ValueParser_InvalidBoolean, "foo"));
        }

        [Fact]
        public void ValidationShouldFailIfNavigationNullableIsSpecifiedOnCollection()
        {
            this.ValidateNavigationWithExpectedErrors(@"<NavigationProperty Name=""Navigation"" Type=""Collection(Test.EntityType)"" Nullable=""false""/>",
                EdmErrorCode.NavigationPropertyWithCollectionTypeCannotHaveNullableAttribute,
                SRResources.CsdlParser_CannotSpecifyNullableAttributeForNavigationPropertyWithCollectionType);
        }

        [Fact]
        public void ValidationShouldFailIfNavigationTypeIsAPrimitiveType()
        {
            this.ValidateNavigationWithExpectedErrors(@"<NavigationProperty Name=""Navigation"" Type=""Edm.Int32"" />",
                EdmErrorCode.BadUnresolvedEntityType,
                Error.Format(SRResources.Bad_UnresolvedEntityType, "Edm.Int32"));
        }

        [Fact]
        public void ValidationShouldFailIfNavigationTypeIsPrimitiveCollectionType()
        {
            this.ValidateNavigationWithExpectedErrors(@"<NavigationProperty Name=""Navigation"" Type=""Collection(Edm.Int32)"" />",
                EdmErrorCode.BadUnresolvedEntityType,
                Error.Format(SRResources.Bad_UnresolvedEntityType, "Edm.Int32"));
        }

        [Fact]
        public void ValidationShouldFailIfNavigationTypeDoesNotExist()
        {
            this.ValidateNavigationWithExpectedErrors(@"<NavigationProperty Name=""Navigation"" Type=""Fake.Nonexistent"" />",
                EdmErrorCode.BadUnresolvedEntityType,
                Error.Format(SRResources.Bad_UnresolvedEntityType, "Fake.Nonexistent"));
        }

        [Fact]
        public void ValidationShouldFailIfNavigationTypeIsACollectionButElementTypeDoesNotExist()
        {
            this.ValidateNavigationWithExpectedErrors(@"<NavigationProperty Name=""Navigation"" Type=""Collection(Fake.Nonexistent)"" />",
                EdmErrorCode.BadUnresolvedEntityType,
                Error.Format(SRResources.Bad_UnresolvedEntityType, "Fake.Nonexistent"));
        }

        [Fact]
        public void ValidationShouldFailIfNavigationParterIsSpecifiedButCannotBeFound()
        {
            this.ValidateNavigationWithExpectedErrors(@"<NavigationProperty Name=""Navigation"" Type=""Test.EntityType"" Partner=""Nonexistent"" />",
                new[]
                {
                    EdmErrorCode.BadUnresolvedNavigationPropertyPath,
                    EdmErrorCode.UnresolvedNavigationPropertyPartnerPath
                },
                new[]
                {
                    Error.Format(SRResources.Bad_UnresolvedNavigationPropertyPath, "Nonexistent", "Test.EntityType"),
                    string.Format("Cannot resolve partner path for navigation property '{0}'.", "Navigation")
                });
        }

        [Fact]
        public void ValidationShouldFailIfEnumMemberIsSpecifiedButCannotBeFound()
        {
            IEdmModel model = GetEnumAnnotationModel(@"<EnumMember>TestNS2.UnknownColor/Blue</EnumMember>");
            IEnumerable<EdmError> errors;
            Assert.False(model.Validate(out errors));
            var error = Assert.Single(errors);
            Assert.Equal(EdmErrorCode.BadUnresolvedEnumMember, error.ErrorCode);
            Assert.Equal(Error.Format(SRResources.Bad_UnresolvedEnumMember, "Blue"), error.ErrorMessage);
        }

        [Fact]
        public void ValidationShouldSucceedIfAContainerQualifiedNameIsUsedForTheTargetOfABinding()
        {
            this.ValidateNavigationBindingSucceeds(
                @"<NavigationPropertyBinding Path=""Navigation"" Target=""Container.EntitySet"" />");
        }

        [Fact]
        public void ValidationShouldFailIfEnumMemberIsSpecifiedButCannotBeFoundTheMember()
        {
            IEdmModel model = GetEnumAnnotationModel(@"<EnumMember>TestNS2.Color/UnknownMember</EnumMember>");
            IEnumerable<EdmError> errors;
            Assert.False(model.Validate(out errors));
            Assert.Equal(2, errors.Count());
            Assert.Contains(errors, e => e.ErrorCode == EdmErrorCode.InvalidEnumMemberPath &&
            e.ErrorMessage == Error.Format(SRResources.CsdlParser_InvalidEnumMemberPath, "TestNS2.Color/UnknownMember"));
        }

        [Fact]
        public void ValidationShouldSucceedIfEnumMemberIsSpecifiedWithCorrectType()
        {
            IEdmModel model = GetEnumAnnotationModel(@"<EnumMember>TestNS2.Color/Blue</EnumMember>");
            IEnumerable<EdmError> errors;
            Assert.True(model.Validate(out errors));
        }

        private IEdmModel GetEnumAnnotationModel(string enumText)
        {
            const string template = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""EntityType"">
        <Key>
          <PropertyRef Name=""ID""/>
        </Key>
        <Property Name=""ID"" Nullable=""false"" Type=""Edm.Int32""/>
        <Annotation Term=""TestNS.OutColor"">
          {0}
        </Annotation>
      </EntityType>
      <Term Name=""OutColor"" Type=""TestNS2.Color"" />
    </Schema>
    <Schema Namespace=""TestNS2"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EnumType Name=""Color"" IsFlags=""true"">
        <Member Name=""Cyan"" Value=""1"" />
        <Member Name=""Blue"" Value=""2"" />
      </EnumType>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
            string modelText = string.Format(template, enumText);

            IEdmModel model;
            IEnumerable<EdmError> errors;
            Assert.True(CsdlReader.TryParse(XElement.Parse(modelText).CreateReader(), out model, out errors));
            return model;
        }

        private void ValidateBindingWithExpectedErrors(string bindingText, EdmErrorCode errorCode, params string[] messages)
        {
            string modelText = string.Format(navPropBindingtemplate, bindingText);

            IEdmModel model;
            IEnumerable<EdmError> errors;
            Assert.True(CsdlReader.TryParse(XElement.Parse(modelText).CreateReader(), out model, out errors));

            Assert.False(model.Validate(out errors));
            Assert.Equal(messages.Length, errors.Count());
            foreach (var message in messages)
            {
                Assert.Contains(errors, e => e.ErrorCode == errorCode && e.ErrorMessage == message);
            }
        }

        private void ValidateNavigationBindingSucceeds(string bindingText)
        {
            string modelText = string.Format(navPropBindingtemplate, bindingText);

            IEdmModel model;
            IEnumerable<EdmError> errors;
            Assert.True(CsdlReader.TryParse(XElement.Parse(modelText).CreateReader(), out model, out errors));

            Assert.True(model.Validate(out errors));
            Assert.Empty(errors);
        }

        private void ValidateReferentialConstraintWithExpectedErrors(string referentialConstraintText, EdmErrorCode errorCode, params string[] messages)
        {
            const string template = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""EntityType"">
        <Key>
          <PropertyRef Name=""ID1"" />
          <PropertyRef Name=""ID2"" />
        </Key>
        <Property Name=""ID1"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""ID2"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""ForeignKeyId1"" Type=""Edm.Int32"" />
        <Property Name=""ForeignKeyId2"" Type=""Edm.Int32"" />
        <NavigationProperty Name=""Navigation"" Type=""Test.EntityType"" Nullable=""true"">
          {0}
        </NavigationProperty>
      </EntityType>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
            string modelText = string.Format(template, referentialConstraintText);

            IEdmModel model;
            IEnumerable<EdmError> errors;
            Assert.True(CsdlReader.TryParse(XElement.Parse(modelText).CreateReader(), out model, out errors));

            Assert.False(model.Validate(out errors));
            Assert.Equal(messages.Length, errors.Count());
            foreach (var message in messages)
            {
                Assert.Contains(errors, e => e.ErrorCode == errorCode && e.ErrorMessage == message);
            }
        }

        private void ValidateNavigationWithExpectedErrors(string navigationText, EdmErrorCode? errorCode = null, string message = null)
        {
            if (errorCode != null)
            {
                ValidateNavigationWithExpectedErrors(navigationText, new[] { errorCode.Value }, new[] { message });
            }
            else
            {
                ValidateNavigationWithExpectedErrors(navigationText, new EdmErrorCode[0], new string[0]);
            }
        }

        private void ValidateNavigationWithExpectedErrors(string navigationText, EdmErrorCode[] errorCodes, string[] messages)
        {
            const string template = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""EntityType"">
        <Key>
          <PropertyRef Name=""ID"" />
        </Key>
        <Property Name=""ID"" Type=""Edm.Int32"" Nullable=""false"" />
        {0}
      </EntityType>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
            string modelText = string.Format(template, navigationText);

            IEdmModel model;
            IEnumerable<EdmError> errors;
            Assert.True(CsdlReader.TryParse(XElement.Parse(modelText).CreateReader(), out model, out errors));

            bool result = model.Validate(out errors);

            if (errorCodes.Length > 0)
            {
                Assert.False(result);

                Assert.Equal(messages.Length, errors.Count());
                for (int i = 0; i < messages.Length; i++)
                {
                    Assert.Contains(errors, e => e.ErrorCode == errorCodes[i] && e.ErrorMessage == messages[i]);
                }
            }
            else
            {
                Assert.True(result);
                Assert.Empty(errors);
            }
        }

        private void ParseBindingWithExpectedErrors(string bindingText, EdmErrorCode? errorCode = null, params string[] messages)
        {
            const string template = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityContainer Name=""Container"">
        <EntitySet Name=""EntitySet"" EntityType=""Test.EntityType"">
          {0}
        </EntitySet>
      </EntityContainer>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
            string modelText = string.Format(template, bindingText);

            IEdmModel model;
            IEnumerable<EdmError> errors;
            bool result = CsdlReader.TryParse(XElement.Parse(modelText).CreateReader(), out model, out errors);
            if (errorCode != null)
            {
                Assert.False(result);
                Assert.Equal(messages.Length, errors.Count());
                foreach (var message in messages)
                {
                    Assert.Contains(errors, e => e.ErrorCode == errorCode && e.ErrorMessage == message);
                }
            }
        }

        private void ParseReferentialConstraint(string referentialConstraintText, EdmErrorCode? errorCode = null, params string[] messages)
        {
            const string template = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""EntityType"">
        <Key>
          <PropertyRef Name=""ID1"" />
          <PropertyRef Name=""ID2"" />
        </Key>
        <Property Name=""ID1"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""ID2"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""ForeignKeyId1"" Type=""Edm.Int32"" />
        <Property Name=""ForeignKeyId2"" Type=""Edm.Int32"" />
        <NavigationProperty Name=""Navigation"" Type=""Test.EntityType"" Nullable=""true"">
          {0}
        </NavigationProperty>
      </EntityType>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
            string modelText = string.Format(template, referentialConstraintText);

            IEdmModel model;
            IEnumerable<EdmError> errors;
            bool result = CsdlReader.TryParse(XElement.Parse(modelText).CreateReader(), out model, out errors);
            if (errorCode != null)
            {
                Assert.False(result);
                Assert.Equal(messages.Length, errors.Count());
                foreach (var message in messages)
                {
                    Assert.Contains(errors, e => e.ErrorCode == errorCode && e.ErrorMessage == message);
                }
            }
        }

        private void ParseReferentialConstraintWithExpectedErrors(string referentialConstraintText, EdmErrorCode errorCode, params string[] messages)
        {
            ParseReferentialConstraint(referentialConstraintText, errorCode, messages);
        }

        private void ParseNavigationExpectedErrors(string navigationText, EdmErrorCode[] errorCodes, string[] messages)
        {
            Assert.Equal(messages.Length, errorCodes.Length);
            const string template = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""EntityType"">
        <Key>
          <PropertyRef Name=""ID"" />
        </Key>
        <Property Name=""ID"" Type=""Edm.Int32"" Nullable=""false"" />
        {0}
      </EntityType>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
            string modelText = string.Format(template, navigationText);

            IEdmModel model;
            IEnumerable<EdmError> errors;

            bool result = CsdlReader.TryParse(XElement.Parse(modelText).CreateReader(), out model, out errors);
            if (errorCodes.Length > 0)
            {
                Assert.False(result);

                Assert.Equal(messages.Length, errors.Count());
                for (int i = 0; i < messages.Length; i++)
                {
                    Assert.Contains(errors, e => e.ErrorCode == errorCodes[i] && e.ErrorMessage == messages[i]);
                }
            }
            else
            {
                Assert.True(result);
                Assert.Empty(errors);
            }
        }

        private void ParseNavigationExpectedErrors(string navigationText, EdmErrorCode? errorCode = null, string message = null)
        {
            if (errorCode != null)
            {
                ParseNavigationExpectedErrors(navigationText, new[] { errorCode.Value }, new[] { message });
            }
            else
            {
                ParseNavigationExpectedErrors(navigationText, new EdmErrorCode[0], new string[0]);
            }
        }
    }
}
