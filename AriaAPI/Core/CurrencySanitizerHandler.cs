// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.Core
{

    /// <summary>
    /// HTTP delegating handler that sanitizes non-standard currency codes in FHIR JSON responses.
    /// Replaces <c>"$"</c> values in <c>"currency"</c> fields with the ISO 4217 code <c>"USD"</c>
    /// to prevent downstream FHIR SDK deserialization failures.
    /// </summary>
    public sealed class CurrencySanitizerHandler : DelegatingHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrencySanitizerHandler"/> class.
        /// </summary>
        /// <param name="innerHandler">The inner HTTP message handler to delegate to.</param>
        public CurrencySanitizerHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = await base.SendAsync(request, ct).ConfigureAwait(false);

            if (response.Content?.Headers?.ContentType?.MediaType?.Contains("json") == true)
            {
                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                // Quick check: skip parsing if "$" doesn't appear in the body at all.
                if (json.Contains('$'))
                {
                    try
                    {
                        var node = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { MaxDepth = MaxJsonDepth * 2 });
                        if (node is not null && SanitizeCurrencyFields(node))
                        {
                            var sanitized = node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
                            response.Content = new StringContent(sanitized, Encoding.UTF8, "application/fhir+json");
                        }
                    }
                    catch (JsonException)
                    {
                        // Malformed JSON — return original response unmodified.
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Maximum allowed JSON nesting depth for recursive sanitization.
        /// </summary>
        private const int MaxJsonDepth = 64;

        /// <summary>
        /// Recursively walks the JSON tree and replaces any "currency" property
        /// whose value is "$" with "USD". Returns true if any replacement was made.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the JSON nesting depth exceeds <see cref="MaxJsonDepth"/>.
        /// </exception>
        private static bool SanitizeCurrencyFields(JsonNode node, int depth = 0)
        {
            if (depth > MaxJsonDepth)
                throw new InvalidOperationException($"JSON nesting depth exceeds maximum of {MaxJsonDepth}.");

            bool modified = false;

            if (node is JsonObject obj)
            {
                if (obj.TryGetPropertyValue("currency", out var currencyNode) &&
                    currencyNode is JsonValue val &&
                    val.TryGetValue<string>(out var currencyStr) &&
                    currencyStr == "$")
                {
                    obj["currency"] = "USD";
                    modified = true;
                }

                foreach (var kvp in obj)
                {
                    if (kvp.Value is not null)
                    {
                        modified |= SanitizeCurrencyFields(kvp.Value, depth + 1);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    if (item is not null)
                    {
                        modified |= SanitizeCurrencyFields(item, depth + 1);
                    }
                }
            }

            return modified;
        }
    }

}
