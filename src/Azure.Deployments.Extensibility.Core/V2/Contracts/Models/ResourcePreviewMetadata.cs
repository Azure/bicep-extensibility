// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Contains metadata about a resource preview response, classifying properties by their
/// mutability and evaluability characteristics for use by the deployment engine in
/// What-If and preflight validation.
/// </summary>
/// <remarks>
/// See <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/preview-operation.md#preview-metadata">preview-operation.md</see>.
/// </remarks>
public record ResourcePreviewMetadata
{
    /// <summary>Creates a new ResourcePreviewMetadata builder.</summary>
    public static Builder NewBuilder() => new();
    
    /// <summary>
    /// JSON Pointers to properties managed entirely by the service. The user cannot set these.
    /// </summary>
    public ImmutableArray<JsonPointer>? ReadOnly { get; init; }

    /// <summary>
    /// JSON Pointers to properties that can be set at creation but cannot be changed on subsequent updates.
    /// </summary>
    public ImmutableArray<JsonPointer>? Immutable { get; init; }

    /// <summary>
    /// JSON Pointers to properties whose values cannot be determined at preview time,
    /// typically because they depend on unevaluated expressions or unavailable external state.
    /// </summary>
    public ImmutableArray<JsonPointer>? Unknown { get; init; }

    /// <summary>
    /// JSON Pointers to properties whose values are computed at operation time and would
    /// differ if the operation were performed later.
    /// </summary>
    public ImmutableArray<JsonPointer>? Calculated { get; init; }

    /// <summary>
    /// JSON Pointers to properties containing ARM template language expressions that the
    /// deployment engine could not resolve. A path should only appear here when no more
    /// specific category (readOnly, immutable, calculated, unknown) applies.
    /// </summary>
    public ImmutableArray<JsonPointer>? Unevaluated { get; init; }

    public class Builder
    {
        private IEnumerable<JsonPointer> Calculated { get; set; } = [];
        private IEnumerable<JsonPointer> Immutable { get; set; } = [];
        private IEnumerable<JsonPointer> ReadOnly { get; set; } = [];
        private IEnumerable<JsonPointer> Unevaluated { get; set; } = [];
        private IEnumerable<JsonPointer> Unknown { get; set; } = [];

        public Builder WithMetadataFromSpec(ResourcePreviewSpecificationMetadata metadata) => this.WithUnevaluated(metadata.Unevaluated);

        public Builder WithCalculated(JsonPointer pointer)
        {
            this.Calculated = this.Calculated.Append(pointer);

            return this;
        }

        public Builder WithCalculated(params JsonPointer[] pointers)
        {
            this.Calculated = this.Calculated.Concat(pointers);

            return this;
        }

        public Builder WithCalculated(IEnumerable<JsonPointer>? pointers)
        {
            if (pointers is not null)
            {
                this.Calculated = this.Calculated.Concat(pointers);
            }

            return this;
        }

        public Builder WithImmutable(JsonPointer pointer)
        {
            this.Immutable = this.Immutable.Append(pointer);

            return this;
        }

        public Builder WithImmutable(params JsonPointer[] pointers)
        {
            this.Immutable = this.Immutable.Concat(pointers);

            return this;
        }

        public Builder WithImmutable(IEnumerable<JsonPointer>? pointers)
        {
            if (pointers is not null)
            {
                this.Immutable = this.Immutable.Concat(pointers);
            }

            return this;
        }

        public Builder WithReadOnly(JsonPointer pointer)
        {
            this.ReadOnly = this.ReadOnly.Append(pointer);

            return this;
        }

        public Builder WithReadOnly(params JsonPointer[] pointers)
        {
            this.ReadOnly = this.ReadOnly.Concat(pointers);

            return this;
        }

        public Builder WithReadOnly(IEnumerable<JsonPointer>? pointers)
        {
            if (pointers is not null)
            {
                this.ReadOnly = this.ReadOnly.Concat(pointers);
            }

            return this;
        }

        public Builder WithUnevaluated(JsonPointer pointer)
        {
            this.Unevaluated = this.Unevaluated.Append(pointer);

            return this;
        }

        public Builder WithUnevaluated(params JsonPointer[] pointers)
        {
            this.Unevaluated = this.Unevaluated.Concat(pointers);

            return this;
        }

        public Builder WithUnevaluated(IEnumerable<JsonPointer>? pointers)
        {
            if (pointers is not null)
            {
                this.Unevaluated = this.Unevaluated.Concat(pointers);
            }

            return this;
        }

        public Builder WithUnknown(JsonPointer pointer)
        {
            this.Unknown = this.Unknown.Append(pointer);

            return this;
        }

        public Builder WithUnknown(params JsonPointer[] pointers)
        {
            this.Unknown = this.Unknown.Concat(pointers);

            return this;
        }

        public Builder WithUnknown(IEnumerable<JsonPointer>? pointers)
        {
            if (pointers is not null)
            {
                this.Unknown = this.Unknown.Concat(pointers);
            }

            return this;
        }

        public ResourcePreviewMetadata Build() =>
            new()
            {
                Calculated = this.Calculated.Distinct().ToImmutableArray() is { Length: > 0 } calculated ? calculated : null,
                Immutable = this.Immutable.Distinct().ToImmutableArray() is { Length: > 0 } immutable ? immutable : null,
                ReadOnly = this.ReadOnly.Distinct().ToImmutableArray() is { Length: > 0 } readOnly ? readOnly : null,
                Unevaluated = this.Unevaluated.Distinct().ToImmutableArray() is { Length: > 0 } unevaluated ? unevaluated : null,
                Unknown = this.Unknown.Distinct().ToImmutableArray() is { Length: > 0 } unknown ? unknown : null
            };
    }
}
