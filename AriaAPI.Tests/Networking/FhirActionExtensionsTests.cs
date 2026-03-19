// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using AriaAPI.Networking.Core;
using AriaAPI.Networking.Helpers;
using Xunit;

namespace AriaAPI.Tests.Networking
{
    /// <summary>
    /// Tests for <see cref="FhirActionExtensions.ToCode"/>.
    /// Verifies that every <see cref="FhirAction"/> enum value maps to the expected FHIR action string,
    /// and that an unknown cast value throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public sealed class FhirActionExtensionsTests
    {
        /// <summary>
        /// <see cref="FhirAction.Read"/> maps to "read".
        /// </summary>
        [Fact]
        public void ToCode_Read_ReturnsRead()
        {
            Assert.Equal("read", FhirAction.Read.ToCode());
        }

        /// <summary>
        /// <see cref="FhirAction.Search"/> maps to "search".
        /// </summary>
        [Fact]
        public void ToCode_Search_ReturnsSearch()
        {
            Assert.Equal("search", FhirAction.Search.ToCode());
        }

        /// <summary>
        /// <see cref="FhirAction.Update"/> maps to "update".
        /// </summary>
        [Fact]
        public void ToCode_Update_ReturnsUpdate()
        {
            Assert.Equal("update", FhirAction.Update.ToCode());
        }

        /// <summary>
        /// <see cref="FhirAction.Delete"/> maps to "delete".
        /// </summary>
        [Fact]
        public void ToCode_Delete_ReturnsDelete()
        {
            Assert.Equal("delete", FhirAction.Delete.ToCode());
        }

        /// <summary>
        /// <see cref="FhirAction.Create"/> maps to "create".
        /// </summary>
        [Fact]
        public void ToCode_Create_ReturnsCreate()
        {
            Assert.Equal("create", FhirAction.Create.ToCode());
        }

        /// <summary>
        /// <see cref="FhirAction.Expand"/> maps to "$expand".
        /// </summary>
        [Fact]
        public void ToCode_Expand_ReturnsDollarExpand()
        {
            Assert.Equal("$expand", FhirAction.Expand.ToCode());
        }

        /// <summary>
        /// <see cref="FhirAction.MarkAsExported"/> maps to "markAsExported".
        /// </summary>
        [Fact]
        public void ToCode_MarkAsExported_ReturnsMarkAsExported()
        {
            Assert.Equal("markAsExported", FhirAction.MarkAsExported.ToCode());
        }

        /// <summary>
        /// <see cref="FhirAction.CheckIn"/> maps to "$checkin".
        /// </summary>
        [Fact]
        public void ToCode_CheckIn_ReturnsDollarCheckin()
        {
            Assert.Equal("$checkin", FhirAction.CheckIn.ToCode());
        }

        /// <summary>
        /// <see cref="FhirAction.CheckOut"/> maps to "$checkout".
        /// </summary>
        [Fact]
        public void ToCode_CheckOut_ReturnsDollarCheckout()
        {
            Assert.Equal("$checkout", FhirAction.CheckOut.ToCode());
        }

        /// <summary>
        /// An integer cast to <see cref="FhirAction"/> that does not correspond to a defined enum
        /// member throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ToCode_UnknownValue_ThrowsArgumentOutOfRangeException()
        {
            var unknown = (FhirAction)999;
            Assert.Throws<ArgumentOutOfRangeException>(() => unknown.ToCode());
        }
    }
}
