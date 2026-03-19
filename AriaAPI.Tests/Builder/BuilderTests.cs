// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Linq;
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Xunit;

namespace AriaAPI.Tests.Builder
{
    /// <summary>
    /// Tests for the fluent <see cref="Builder{TResource}"/> class.
    /// </summary>
    public sealed class BuilderTests
    {
        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.With"/> adds the expected key/value pair
        /// to the resulting <see cref="SearchParams"/>.
        /// </summary>
        [Fact]
        public void With_AddsCorrectKeyAndValue()
        {
            var builder = new Builder<Patient>();
            builder.With("status", "active");
            var sp = builder.Build();

            Assert.Contains(sp.Parameters, p => p.Item1 == "status" && p.Item2 == "active");
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.ByIdentifier"/> adds an "identifier" parameter.
        /// </summary>
        [Fact]
        public void ByIdentifier_AddsIdentifierParam()
        {
            var builder = new Builder<Patient>();
            builder.ByIdentifier("MRN-12345");
            var sp = builder.Build();

            Assert.Contains(sp.Parameters, p => p.Item1 == "identifier" && p.Item2 == "MRN-12345");
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.ForPatient"/> with default parameter adds a "patient" param.
        /// </summary>
        [Fact]
        public void ForPatient_AddsPatientParam()
        {
            var builder = new Builder<Appointment>();
            builder.ForPatient("patient-42");
            var sp = builder.Build();

            Assert.Contains(sp.Parameters, p => p.Item1 == "patient" && p.Item2 == "patient-42");
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.ForPatient"/> with <c>useSubject=true</c>
        /// adds a "subject" param with the "Patient/" prefix.
        /// </summary>
        [Fact]
        public void ForPatient_UseSubject_AddsSubjectParam()
        {
            var builder = new Builder<Appointment>();
            builder.ForPatient("patient-42", useSubject: true);
            var sp = builder.Build();

            Assert.Contains(sp.Parameters, p => p.Item1 == "subject" && p.Item2 == "Patient/patient-42");
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.WithCount"/> sets the page size on <see cref="SearchParams"/>.
        /// </summary>
        [Fact]
        public void WithCount_AddsCountParam()
        {
            var builder = new Builder<Patient>();
            builder.WithCount(50);
            var sp = builder.Build();

            Assert.Equal(50, sp.Count);
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.WithCount"/> throws when count is zero or negative.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void WithCount_InvalidValue_ThrowsArgumentOutOfRangeException(int count)
        {
            var builder = new Builder<Patient>();
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithCount(count));
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.Include(string, string?, IncludeModifier)"/> adds
        /// the expected include string.
        /// </summary>
        [Fact]
        public void Include_AddsIncludeParam()
        {
            var builder = new Builder<Appointment>();
            builder.Include("Appointment:actor");
            var sp = builder.Build();

            Assert.Contains(sp.Include, i => i.Item1 == "Appointment:actor");
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.RevInclude(string, string?, IncludeModifier)"/> adds
        /// the expected revinclude string.
        /// </summary>
        [Fact]
        public void RevInclude_AddsRevIncludeParam()
        {
            var builder = new Builder<Patient>();
            builder.RevInclude("Appointment:patient");
            var sp = builder.Build();

            Assert.Contains(sp.RevInclude, i => i.Item1 == "Appointment:patient");
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.Build"/> returns a <see cref="SearchParams"/> instance
        /// that contains all parameters added to the builder.
        /// </summary>
        [Fact]
        public void Build_ReturnsSearchParamsWithAllAddedParams()
        {
            var builder = new Builder<Patient>();
            builder
                .With("status", "active")
                .ByIdentifier("MRN-99")
                .WithCount(10)
                .Include("Patient:managing-organization")
                .RevInclude("Condition:patient");

            var sp = builder.Build();

            Assert.Contains(sp.Parameters, p => p.Item1 == "status" && p.Item2 == "active");
            Assert.Contains(sp.Parameters, p => p.Item1 == "identifier" && p.Item2 == "MRN-99");
            Assert.Equal(10, sp.Count);
            Assert.Contains(sp.Include, i => i.Item1 == "Patient:managing-organization");
            Assert.Contains(sp.RevInclude, i => i.Item1 == "Condition:patient");
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.With"/> throws when the key is null or whitespace.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void With_NullOrWhitespaceKey_ThrowsArgumentException(string? key)
        {
            var builder = new Builder<Patient>();
            Assert.Throws<ArgumentException>(() => builder.With(key!, "value"));
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.With"/> throws when the value is null or whitespace.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void With_NullOrWhitespaceValue_ThrowsArgumentException(string? value)
        {
            var builder = new Builder<Patient>();
            Assert.Throws<ArgumentException>(() => builder.With("key", value!));
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.Include(string, string?, IncludeModifier)"/>
        /// composes the target type into the include string when provided.
        /// </summary>
        [Fact]
        public void Include_WithTargetType_ComposesString()
        {
            var builder = new Builder<Appointment>();
            builder.Include("Appointment:actor", targetType: "Practitioner");
            var sp = builder.Build();

            Assert.Contains(sp.Include, i => i.Item1 == "Appointment:actor:Practitioner");
        }

        /// <summary>
        /// Verifies that <see cref="Builder{TResource}.ClearIncludes"/> removes all includes and revIncludes.
        /// </summary>
        [Fact]
        public void ClearIncludes_RemovesAllIncludes()
        {
            var builder = new Builder<Patient>();
            builder
                .Include("Patient:managing-organization")
                .RevInclude("Condition:patient")
                .ClearIncludes();

            var sp = builder.Build();

            Assert.Empty(sp.Include);
            Assert.Empty(sp.RevInclude);
        }
    }
}
