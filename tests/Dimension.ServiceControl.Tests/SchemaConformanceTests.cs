using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Xunit;

namespace Dimension.ServiceControl.Tests
{
    /// <summary>
    /// Checks the sample documents every other test relies on against the schemas the
    /// utility exported, so a fixture cannot quietly drift from the real contract and
    /// leave the model tests passing against XML the utility would never write.
    /// </summary>
    public class SchemaConformanceTests
    {
        [Theory]
        [InlineData("ServiceList.xsd", nameof(SampleDocuments.ServiceList))]
        [InlineData("ServiceList.xsd", nameof(SampleDocuments.ServiceListWithUnexpectedError))]
        [InlineData("AgentList.xsd", nameof(SampleDocuments.AgentList))]
        [InlineData("QueueitemList.xsd", nameof(SampleDocuments.QueueItemList))]
        public void SampleDocument_ConformsToTheExportedSchema(string schemaFile, string sampleName)
        {
            IReadOnlyList<string> errors = Validate(schemaFile, Sample(sampleName));

            Assert.Empty(errors);
        }

        [Fact]
        public void WrongVersionSample_IsRejectedByTheSchemaItself()
        {
            // Proves the check above has teeth: the schema fixes Version at 2307, so the
            // document the client must reject is one the schema rejects too.
            IReadOnlyList<string> errors = Validate("ServiceList.xsd", SampleDocuments.ServiceListWithWrongVersion);

            Assert.Contains(errors, error => error.Contains("Version"));
        }

        [Theory]
        [InlineData("ServiceList.xsd", "ServiceList", 2307, 0)]
        [InlineData("AgentList.xsd", "AgentList", 641, 0)]
        [InlineData("QueueitemList.xsd", "QueueitemList", 2201, 0)]
        public void ExpectedVersions_MatchTheVersionsTheSchemasFix(
            string schemaFile, string rootElement, int version, int minorVersion)
        {
            // The constants the client checks responses against are copied from these
            // schemas. After a re-export this fails first, pointing at the model to update.
            XmlSchema schema = LoadSchema(schemaFile);
            var root = (XmlSchemaElement)schema.Elements[new XmlQualifiedName(rootElement)];
            var type = (XmlSchemaComplexType)root.ElementSchemaType;

            Assert.Equal(version.ToString(), FixedValue(type, "Version"));
            Assert.Equal(minorVersion.ToString(), FixedValue(type, "MinorVersion"));
        }

        [Fact]
        public void ExpectedVersionConstants_AreTheOnesThisTestChecks()
        {
            Assert.Equal(2307, Model.ServiceListResponse.ExpectedVersion);
            Assert.Equal(641, Model.AgentListResponse.ExpectedVersion);
            Assert.Equal(2201, Model.QueueItemListResponse.ExpectedVersion);
        }

        private static string Sample(string name)
        {
            return (string)typeof(SampleDocuments).GetField(name).GetValue(null);
        }

        private static string SchemaPath(string schemaFile)
        {
            return Path.Combine(AppContext.BaseDirectory, "schemas", schemaFile);
        }

        private static XmlSchema LoadSchema(string schemaFile)
        {
            var set = new XmlSchemaSet();
            XmlSchema schema = set.Add(null, SchemaPath(schemaFile));
            set.Compile();
            return schema;
        }

        private static string FixedValue(XmlSchemaComplexType type, string attributeName)
        {
            foreach (XmlSchemaAttribute attribute in type.AttributeUses.Values)
            {
                if (attribute.QualifiedName.Name == attributeName)
                {
                    return attribute.FixedValue;
                }
            }

            throw new InvalidOperationException("No " + attributeName + " attribute in the schema.");
        }

        private static IReadOnlyList<string> Validate(string schemaFile, string xml)
        {
            var errors = new List<string>();
            var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
            settings.Schemas.Add(null, SchemaPath(schemaFile));
            settings.ValidationEventHandler += (sender, arguments) =>
                errors.Add(arguments.Severity + ": " + arguments.Message);

            using (var reader = XmlReader.Create(new StringReader(xml), settings))
            {
                while (reader.Read())
                {
                }
            }

            return errors;
        }
    }
}
