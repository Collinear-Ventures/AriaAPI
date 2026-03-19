// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Linq;
using AriaAPI.Networking.Core;
using AriaAPI.Networking.Helpers;
using AriaAPI.Resources.Includes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Xunit;

namespace AriaAPI.Tests.Networking
{
    /// <summary>
    /// Tests for the <see cref="BuilderExtensions"/> helpers in
    /// <c>AriaAPI.Networking.Core</c> (registry-based) and
    /// <c>AriaAPI.Networking.Helpers</c> (convention-based).
    /// </summary>
    public sealed class BuilderExtensionsTests
    {
        // -----------------------------------------------------------------------
        // Core.BuilderExtensions (registry-based)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Adding <see cref="DocumentReferenceInclude.Author"/> produces "DocumentReference:author"
        /// in the SearchParams include list.
        /// </summary>
        [Fact]
        public void Include_BuildsCorrectString()
        {
            var builder = new Builder<DocumentReference>();
            // Core.BuilderExtensions.Include<TResource, TInclude>
            AriaAPI.Networking.Core.BuilderExtensions.Include(builder, DocumentReferenceInclude.Author);
            var sp = builder.Build();

            Assert.Contains(sp.Include, i => i.Item1 == "DocumentReference:author");
        }

        /// <summary>
        /// Adding <see cref="AppointmentInclude.Patient"/> produces "Appointment:patient"
        /// in the SearchParams include list.
        /// </summary>
        [Fact]
        public void Include_AppointmentPatient_BuildsCorrectString()
        {
            var builder = new Builder<Appointment>();
            AriaAPI.Networking.Core.BuilderExtensions.Include(builder, AppointmentInclude.Patient);
            var sp = builder.Build();

            Assert.Contains(sp.Include, i => i.Item1 == "Appointment:patient");
        }

        /// <summary>
        /// Adding multiple <see cref="DocumentReferenceInclude"/> values via the IEnumerable overload
        /// produces all expected include strings.
        /// </summary>
        [Fact]
        public void Include_MultipleValues_AddsAll()
        {
            var builder = new Builder<DocumentReference>();
            AriaAPI.Networking.Core.BuilderExtensions.Include(
                builder,
                new[] { DocumentReferenceInclude.Author, DocumentReferenceInclude.Custodian });
            var sp = builder.Build();

            Assert.Contains(sp.Include, i => i.Item1 == "DocumentReference:author");
            Assert.Contains(sp.Include, i => i.Item1 == "DocumentReference:custodian");
        }

        /// <summary>
        /// Adding a _revinclude with <see cref="TaskInclude.BasedOn"/> on a
        /// <see cref="Patient"/> builder produces "Task:based-on" in the RevInclude list.
        /// </summary>
        [Fact]
        public void RevInclude_BuildsCorrectString()
        {
            var builder = new Builder<Patient>();
            AriaAPI.Networking.Core.BuilderExtensions.RevInclude(builder, TaskInclude.BasedOn);
            var sp = builder.Build();

            Assert.Contains(sp.RevInclude, i => i.Item1 == "Task:based-on");
        }

        /// <summary>
        /// Adding multiple _revinclude values via the IEnumerable overload
        /// produces all expected revinclude strings.
        /// </summary>
        [Fact]
        public void RevInclude_MultipleValues_AddsAll()
        {
            var builder = new Builder<Patient>();
            AriaAPI.Networking.Core.BuilderExtensions.RevInclude(
                builder,
                new[] { TaskInclude.BasedOn, TaskInclude.For });
            var sp = builder.Build();

            Assert.Contains(sp.RevInclude, i => i.Item1 == "Task:based-on");
            Assert.Contains(sp.RevInclude, i => i.Item1 == "Task:for");
        }

        // -----------------------------------------------------------------------
        // Helpers.BuilderExtensions (convention-based — validateResourceMatch)
        // -----------------------------------------------------------------------

        /// <summary>
        /// When the include enum's resource name matches the builder's TResource,
        /// the include is added correctly.
        /// </summary>
        [Fact]
        public void HelpersInclude_ValidResource_BuildsCorrectString()
        {
            var builder = new Builder<DocumentReference>();
            AriaAPI.Networking.Helpers.BuilderExtensions.Include(
                builder,
                DocumentReferenceInclude.Author,
                validateResourceMatch: true);
            var sp = builder.Build();

            Assert.Contains(sp.Include, i => i.Item1 == "DocumentReference:author");
        }

        /// <summary>
        /// When <c>validateResourceMatch</c> is true and the enum's inferred resource does not
        /// match the builder's TResource, an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        [Fact]
        public void HelpersInclude_WrongResourceEnum_ThrowsInvalidOperationException()
        {
            // Builder targets Patient, but TaskInclude infers "Task"
            var builder = new Builder<Patient>();
            Assert.Throws<InvalidOperationException>(() =>
                AriaAPI.Networking.Helpers.BuilderExtensions.Include(
                    builder,
                    TaskInclude.BasedOn,
                    validateResourceMatch: true));
        }

        /// <summary>
        /// When <c>validateResourceMatch</c> is false, a mismatched resource enum does not throw
        /// and the include is added using the enum-inferred resource type.
        /// </summary>
        [Fact]
        public void HelpersInclude_ValidateResourceMatchFalse_DoesNotThrow()
        {
            // Builder targets Patient, but TaskInclude infers "Task"
            var builder = new Builder<Patient>();
            var exception = Record.Exception(() =>
                AriaAPI.Networking.Helpers.BuilderExtensions.Include(
                    builder,
                    TaskInclude.BasedOn,
                    validateResourceMatch: false));

            Assert.Null(exception);
        }

        /// <summary>
        /// The convention-based revinclude infers the source resource from the enum name and
        /// builds the correct "SourceResource:segment" string.
        /// </summary>
        [Fact]
        public void HelpersRevIncludeFrom_BuildsCorrectString()
        {
            var builder = new Builder<Patient>();
            AriaAPI.Networking.Helpers.BuilderExtensions.RevIncludeFrom(
                builder,
                TaskInclude.BasedOn);
            var sp = builder.Build();

            Assert.Contains(sp.RevInclude, i => i.Item1 == "Task:based-on");
        }

        /// <summary>
        /// Multiple calls to <see cref="AriaAPI.Networking.Helpers.BuilderExtensions.RevIncludeFrom{TResource, TSourceInclude}(Builder{TResource}, TSourceInclude, string?, IncludeModifier)"/>
        /// add all expected revinclude strings.
        /// </summary>
        [Fact]
        public void HelpersRevIncludeFrom_MultipleIncludes_AddsAll()
        {
            var builder = new Builder<Patient>();
            AriaAPI.Networking.Helpers.BuilderExtensions.RevIncludeFrom(builder, TaskInclude.BasedOn);
            AriaAPI.Networking.Helpers.BuilderExtensions.RevIncludeFrom(builder, TaskInclude.For);
            var sp = builder.Build();

            Assert.Contains(sp.RevInclude, i => i.Item1 == "Task:based-on");
            Assert.Contains(sp.RevInclude, i => i.Item1 == "Task:for");
        }

        /// <summary>
        /// The <c>None</c> sentinel enum value (e.g., <see cref="BundleInclude.None"/>)
        /// results in a no-op (no include added).
        /// </summary>
        [Fact]
        public void HelpersInclude_NoneSentinel_IsNoOp()
        {
            var builder = new Builder<Patient>();
            // BundleInclude.None maps to an empty segment string — treated as no-op
            AriaAPI.Networking.Helpers.BuilderExtensions.RevIncludeFrom(
                builder,
                BundleInclude.None);
            var sp = builder.Build();

            Assert.Empty(sp.RevInclude);
        }
    }
}
