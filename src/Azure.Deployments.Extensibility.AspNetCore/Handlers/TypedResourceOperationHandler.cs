// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

public abstract class TypedResourceOperationHandler<TProperties, TIdentifiers, TConfig> : IHandler
{
    private readonly JsonSerializerOptions jsonSerializerOptions;

    protected TypedResourceOperationHandler(IOptions<JsonOptions> jsonOptions)
    {
        this.jsonSerializerOptions = jsonOptions.Value.SerializerOptions;
    }

    protected TypedResourceSpecification ToTypedResourceSpecification(ResourceSpecification specification) => new()
    {
        Type = specification.Type,
        ApiVersion = specification.ApiVersion,
        Properties = this.DeserializeProperties(specification.Properties),
        Config = this.DeserializeConfig(specification.Config),
        ConfigId = specification.ConfigId,
    };

    protected ResourceSpecification ToResourceSpecification(TypedResourceSpecification specification) => new()
    {
        Type = specification.Type,
        ApiVersion = specification.ApiVersion,
        Properties = this.SerializeProperties(specification.Properties),
        Config = this.SerializeConfig(specification.Config),
        ConfigId = specification.ConfigId,
    };

    protected TypedResourcePreviewSpecification ToTypedResourcePreviewSpecification(ResourcePreviewSpecification specification) => new()
    {
        Type = specification.Type,
        ApiVersion = specification.ApiVersion,
        Properties = this.DeserializeProperties(specification.Properties),
        Config = this.DeserializeConfig(specification.Config),
        ConfigId = specification.ConfigId,
        Metadata = specification.Metadata,
    };

    protected ResourcePreviewSpecification ToResourcePreviewSpecification(TypedResourcePreviewSpecification specification) => new()
    {
        Type = specification.Type,
        ApiVersion = specification.ApiVersion,
        Properties = this.SerializeProperties(specification.Properties),
        Config = this.SerializeConfig(specification.Config),
        ConfigId = specification.ConfigId,
        Metadata = specification.Metadata,
    };

    protected TypedResourcePreview ToTypedResourcePreview(ResourcePreview preview) => new()
    {
        Type = preview.Type,
        ApiVersion = preview.ApiVersion,
        Identifiers = this.DeserializeIdentifier(preview.Identifiers),
        Properties = this.DeserializeProperties(preview.Properties),
        Config = this.DeserializeConfig(preview.Config),
        ConfigId = preview.ConfigId,
        Metadata = preview.Metadata,
    };

    protected ResourcePreview ToResourcePreview(TypedResourcePreview preview) => new()
    {
        Type = preview.Type,
        ApiVersion = preview.ApiVersion,
        Identifiers = this.SerializeIdentifiers(preview.Identifiers),
        Properties = this.SerializeProperties(preview.Properties),
        Config = this.SerializeConfig(preview.Config),
        ConfigId = preview.ConfigId,
        Metadata = preview.Metadata,
    };

    protected TypedResourceReference ToTypedResourceReference(ResourceReference reference) => new()
    {
        Type = reference.Type,
        ApiVersion = reference.ApiVersion,
        Identifiers = this.DeserializeIdentifier(reference.Identifiers),
        Config = this.DeserializeConfig(reference.Config),
        ConfigId = reference.ConfigId,
    };

    protected ResourceReference ToResourceReference(TypedResourceReference reference) => new()
    {
        Type = reference.Type,
        ApiVersion = reference.ApiVersion,
        Identifiers = this.SerializeIdentifiers(reference.Identifiers),
        Config = this.SerializeConfig(reference.Config),
        ConfigId = reference.ConfigId,
    };

    protected TypedResource ToTypedResource(Resource resource) => new()
    {
        Type = resource.Type,
        ApiVersion = resource.ApiVersion,
        Identifiers = this.DeserializeIdentifier(resource.Identifiers),
        Properties = this.DeserializeProperties(resource.Properties),
        Config = this.DeserializeConfig(resource.Config),
        ConfigId = resource.ConfigId,
    };

    protected Resource? ToNullableResource(TypedResource? resource) => resource is null ? null : this.ToResource(resource);

    protected Resource ToResource(TypedResource resource) => new()
    {
        Type = resource.Type,
        ApiVersion = resource.ApiVersion,
        Identifiers = this.SerializeIdentifiers(resource.Identifiers),
        Properties = this.SerializeProperties(resource.Properties),
        Config = this.SerializeConfig(resource.Config),
        ConfigId = resource.ConfigId,
    };
    
    private TProperties DeserializeProperties(JsonObject propertiesObject)
    {
        try
        {
            return propertiesObject.Deserialize<TProperties>(this.jsonSerializerOptions) ??
                throw new ErrorResponseException("NullProperties", "Resource properties must not be null", "/properties");
        }
        catch (JsonException exception)
        {
            throw new ErrorResponseException(
                "InvalidProperties",
                $"Failed to deserialize JSON object into {typeof(TProperties).Name}. {exception.Message}",
                "/properties");
        }
    }

    private TIdentifiers DeserializeIdentifier(JsonObject identrifiersObject)
    {
        try
        {
            return identrifiersObject.Deserialize<TIdentifiers>(this.jsonSerializerOptions) ??
                throw new ErrorResponseException("NullIdentifiers", "Resource identifiers must not be null", "/identifiers");
        }
        catch (JsonException exception)
        {
            throw new ErrorResponseException(
                "InvalidIdentifiers",
                $"Failed to deserialize JSON object into {typeof(TIdentifiers).Name}. {exception.Message}",
                "/identifiers");
        }
    }

    private TConfig? DeserializeConfig(JsonObject? configObject) => configObject.Deserialize<TConfig>(this.jsonSerializerOptions);

    private JsonObject SerializeProperties(TProperties properties)
    {
        return JsonSerializer.SerializeToNode(properties, this.jsonSerializerOptions) as JsonObject ??
            throw new InvalidOperationException($"Failed to serialize {typeof(TProperties).Name} into a JSON object.");
    }

    private JsonObject SerializeIdentifiers(TIdentifiers identifiers)
    {
        return JsonSerializer.SerializeToNode(identifiers, this.jsonSerializerOptions) as JsonObject ??
            throw new InvalidOperationException($"Failed to serialize {typeof(TIdentifiers).Name} into a JSON object.");
    }

    private JsonObject? SerializeConfig(TConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToNode(config, this.jsonSerializerOptions) as JsonObject ??
            throw new InvalidOperationException($"Failed to serialize {typeof(TConfig).Name} into a JSON object.");
    }

    protected record TypedResourceSpecification : ResourceSpecification<TProperties, TConfig>
    {
    }

    protected record TypedResourcePreviewSpecification : ResourcePreviewSpecification<TProperties, TConfig>
    {
    }

    protected record TypedResourcePreview : ResourcePreview<TProperties, TIdentifiers, TConfig>
    {
    }

    protected record TypedResourceReference : ResourceReference<TIdentifiers, TConfig>
    {
    }

    protected record TypedResource : Resource<TProperties, TIdentifiers, TConfig>
    {
    }
}
