// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿
using System;
using System.Collections.Generic;
using System.Linq;

namespace AriaAPI.Core
{
    /// <summary>
    /// Lightweight value-object representing a single row from a terminology lookup table.
    /// </summary>
    /// <param name="System">The coding system URI (e.g., <c>http://snomed.info/sct</c>).</param>
    /// <param name="Code">The code value within the system.</param>
    /// <param name="Display">The human-readable display text for the code.</param>
    public sealed record CodeRow(string System, string Code, string Display);

    /// <summary>
    /// Domain-model wrapper around a FHIR CodeableConcept, pairing a coded value with its display text.
    /// </summary>
    public sealed class CodeableConcept
    {
        /// <summary>Gets the coding system URI.</summary>
        public string System { get; }

        /// <summary>Gets the code within the system.</summary>
        public string Code { get; }

        /// <summary>Gets the human-readable display text for the code.</summary>
        public string Display { get; }

        /// <summary>Gets the narrative text for the concept; defaults to <see cref="Display"/> when not supplied.</summary>
        public string? Text { get; }

        /// <summary>
        /// Initializes a <see cref="CodeableConcept"/> with the given system, code, display, and optional text.
        /// When <paramref name="text"/> is <see langword="null"/>, <see cref="Text"/> is set to <paramref name="display"/>.
        /// </summary>
        /// <param name="system">The coding system URI.</param>
        /// <param name="code">The code value.</param>
        /// <param name="display">The display string.</param>
        /// <param name="text">Optional narrative text; defaults to <paramref name="display"/> when null.</param>
        public CodeableConcept(string system, string code, string display, string? text = null)
        {
            System = system;
            Code = code;
            Display = display;
            Text = text ?? display;
        }

        /// <summary>
        /// Converts this instance into an <see cref="Hl7.Fhir.Model.CodeableConcept"/> with a single <see cref="Hl7.Fhir.Model.Coding"/> entry.
        /// </summary>
        /// <returns>A FHIR <see cref="Hl7.Fhir.Model.CodeableConcept"/> populated from this instance's properties.</returns>
        public Hl7.Fhir.Model.CodeableConcept ToFhirCodeableConcept()
        {
            var coding = new Hl7.Fhir.Model.Coding
            {
                System = System,
                Code = Code,
                Display = Display
            };
            var fhirConcept = new Hl7.Fhir.Model.CodeableConcept
            {
                Coding = new List<Hl7.Fhir.Model.Coding> { coding },
                Text = Text
            };
            return fhirConcept;
        }

        /// <summary>
        /// Returns a string in the format <c>System|Code (Display)</c>.
        /// </summary>
        public override string ToString() => $"{System}|{Code} ({Display})";
    }
}