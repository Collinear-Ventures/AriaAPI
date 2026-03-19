// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchHelpers;
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR <see cref="Device"/> resources using
    /// <see cref="ClientConfigurator"/> and <c>Builder&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// This version intentionally removes all usage of FHIR <c>_include</c> since the target
    /// server does not support it for <c>Device</c> searches.
    ///
    /// Supported search parameters:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Parameter</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item><term><c>_id</c></term><description>Logical ID of the resource.</description></item>
    ///   <item><term><c>device-name</c></term><description>Name of the device.</description></item>
    ///   <item><term><c>identifier</c></term><description>External identifier for the device (token).</description></item>
    ///   <item><term><c>schedulable</c></term><description>Boolean flag indicating whether the device is schedulable (true | false).</description></item>
    ///   <item><term><c>service-organization</c></term><description>Organization responsible for the device.</description></item>
    ///   <item><term><c>status</c></term><description>Status of the device (active | inactive).</description></item>
    ///   <item><term><c>type</c></term><description>Device type code or token.</description></item>
    /// </list>
    /// </remarks>
    public static class DeviceSearch
    {
        /// <summary>
        /// Encapsulates search parameters for Device queries.
        /// </summary>
        /// <remarks>
        /// All parameters are optional; supply any combination to constrain results.
        /// Use <see cref="ListReturnLimit"/> to defensively cap large result sets on the client side.
        /// </remarks>
        public sealed class DeviceSearchParams
        {
            /// <summary>
            /// Logical ID of the Device resource (FHIR <c>_id</c>).
            /// </summary>
            public string? Id { get; init; }

            /// <summary>
            /// Name of the device (FHIR search parameter <c>device-name</c>).
            /// </summary>
            public string? DeviceName { get; init; }

            /// <summary>
            /// External identifier for the device (FHIR token search <c>identifier</c>).
            /// Accepts value-only or <c>system|value</c> form depending on server support.
            /// </summary>
            public string? Identifier { get; init; }

            /// <summary>
            /// Boolean flag indicating if the device is schedulable (FHIR search parameter <c>schedulable</c>).
            /// </summary>
            public bool? Schedulable { get; init; }

            /// <summary>
            /// Organization responsible for the device (FHIR search parameter <c>service-organization</c>).
            /// </summary>
            public string? ServiceOrganization { get; init; }

            /// <summary>
            /// Status of the device (active | inactive) (FHIR search parameter <c>status</c>).
            /// </summary>
            public DeviceStatus? Status { get; init; }

            /// <summary>
            /// Device type code or token (FHIR search parameter <c>type</c>).
            /// </summary>
            public string? Type { get; init; }

            /// <summary>
            /// Maximum number of Device resources to return client-side.
            /// Defaults to <see cref="SearchExecutor.DefaultServerMaxResults"/> (500).
            /// </summary>
            /// <remarks>
            /// This is a client-side defensive trim applied after aggregation across pages.
            /// </remarks>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a Device search using the provided parameter bag.
        /// Builds FHIR <see cref="SearchParams"/> via <c>Builder&lt;T&gt;</c> and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag containing id, device-name, identifier, schedulable, service-organization, status, type, and limits.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of <see cref="Device"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<Device>> SearchDevicesAsync(
            ClientConfigurator configurator,
            DeviceSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new DeviceSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            return await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<Device>();

                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (!string.IsNullOrWhiteSpace(p.DeviceName))
                        builder.With("device-name", p.DeviceName);

                    if (!string.IsNullOrWhiteSpace(p.Identifier))
                        builder.With("identifier", p.Identifier);

                    if (p.Schedulable.HasValue)
                        builder.With("schedulable", p.Schedulable.Value ? "true" : "false");

                    if (!string.IsNullOrWhiteSpace(p.ServiceOrganization))
                        builder.With("service-organization", p.ServiceOrganization);

                    if (p.Status.HasValue)
                        builder.With("status", StatusToToken(p.Status.Value));

                    if (!string.IsNullOrWhiteSpace(p.Type))
                        builder.With("type", p.Type);

                    // No _include usage — server does not support it for Device

                    builder.WithCount(limit);
                    return builder;
                },
                null,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Strongly typed convenience methods
        // -----------------------------

        /// <summary>
        /// Returns Devices by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Device>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            int listReturnLimit = int.MaxValue,
            CancellationToken ct = default)
        {
            var p = new DeviceSearchParams
            {
                Id = id,
                ListReturnLimit = listReturnLimit
            };
            return SearchDevicesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Devices by name.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="name">Device name to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Device>> ByDeviceNameAsync(
            ClientConfigurator configurator,
            string name,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new DeviceSearchParams
            {
                DeviceName = name,
                ListReturnLimit = listReturnLimit
            };
            return SearchDevicesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Devices by status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="status">Device status to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Device>> ByStatusAsync(
            ClientConfigurator configurator,
            DeviceStatus status,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new DeviceSearchParams
            {
                Status = status,
                ListReturnLimit = listReturnLimit
            };
            return SearchDevicesAsync(configurator, p, ct);
        }


    }
}
