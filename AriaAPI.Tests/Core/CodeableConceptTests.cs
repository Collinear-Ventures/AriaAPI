// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using Xunit;

namespace AriaAPI.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="CodeableConcept"/> constructor, properties, and FHIR conversion.
    /// </summary>
    public sealed class CodeableConceptTests
    {
        /// <summary>Constructor stores all supplied property values.</summary>
        [Fact]
        public void Constructor_SetsAllProperties()
        {
            var concept = new CodeableConcept("http://example.com", "ABC", "Alpha Bravo Charlie", "ABC text");

            Assert.Equal("http://example.com", concept.System);
            Assert.Equal("ABC", concept.Code);
            Assert.Equal("Alpha Bravo Charlie", concept.Display);
            Assert.Equal("ABC text", concept.Text);
        }

        /// <summary>When text is null, Text defaults to the display value.</summary>
        [Fact]
        public void Constructor_TextDefaultsToDisplay_WhenNull()
        {
            var concept = new CodeableConcept("http://example.com", "XYZ", "My Display");

            Assert.Equal("My Display", concept.Text);
        }

        /// <summary>ToString returns the expected "System|Code (Display)" format.</summary>
        [Fact]
        public void ToString_ReturnsExpectedFormat()
        {
            var concept = new CodeableConcept("http://snomed.info/sct", "12345", "Snomed Thing");

            Assert.Equal("http://snomed.info/sct|12345 (Snomed Thing)", concept.ToString());
        }

        /// <summary>ToFhirCodeableConcept produces a Coding entry with correct System, Code, and Display.</summary>
        [Fact]
        public void ToFhirCodeableConcept_SetsCorrectCoding()
        {
            var concept = new CodeableConcept("http://loinc.org", "8867-4", "Heart rate");

            var fhir = concept.ToFhirCodeableConcept();

            Assert.Single(fhir.Coding);
            var coding = fhir.Coding[0];
            Assert.Equal("http://loinc.org", coding.System);
            Assert.Equal("8867-4", coding.Code);
            Assert.Equal("Heart rate", coding.Display);
        }

        /// <summary>ToFhirCodeableConcept sets Text on the returned FHIR concept.</summary>
        [Fact]
        public void ToFhirCodeableConcept_SetsText()
        {
            var concept = new CodeableConcept("http://loinc.org", "8867-4", "Heart rate", "HR narrative");

            var fhir = concept.ToFhirCodeableConcept();

            Assert.Equal("HR narrative", fhir.Text);
        }

        /// <summary>CodeRow record stores all positional constructor values.</summary>
        [Fact]
        public void CodeRow_StoresProperties()
        {
            var row = new CodeRow("http://snomed.info/sct", "99999", "Test Concept");

            Assert.Equal("http://snomed.info/sct", row.System);
            Assert.Equal("99999", row.Code);
            Assert.Equal("Test Concept", row.Display);
        }
    }
}
