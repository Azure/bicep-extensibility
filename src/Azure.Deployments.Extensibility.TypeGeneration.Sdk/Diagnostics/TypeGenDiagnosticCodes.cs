// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;

public static class TypeGenDiagnosticCodes
{
    public const string InvalidExtensionName = "TG1001";

    public const string InvalidExtensionVersion = "TG1002";

    public const string CSharpCompilation = "TG1101";

    public const string InvalidAuthoringSdk = "TG1102";

    public const string InvalidResourceName = "TG1201";

    public const string InvalidApiVersions = "TG1202";

    public const string ConflictingApiVersions = "TG1203";

    public const string DuplicateResourceName = "TG1204";

    public const string MultipleConfigurations = "TG1205";

    public const string UnsupportedSchema = "TG1301";

    public const string DuplicatePropertyName = "TG1401";

    public const string InvalidPropertyFlags = "TG1402";

    public const string UnsupportedIndexer = "TG1403";

    public const string UnsupportedType = "TG1501";

    public const string UnsupportedIntegerRange = "TG1502";

    public const string InvalidDictionaryKey = "TG1503";

    public const string AmbiguousCollection = "TG1504";

    public const string ObliviousNullability = "TG1505";

    public const string TypeGraphTooDeep = "TG1506";

    public const string InvalidSemanticModel = "TG1601";

    public const string BackendInvalidMetadata = "TG2001";

    public const string BackendInvalidResource = "TG2002";

    public const string BackendInvalidApiVersions = "TG2003";

    public const string BackendConflictingResources = "TG2004";

    public const string BackendInvalidRoot = "TG2005";

    public const string BackendInvalidGraph = "TG2006";

    public const string BackendInvalidProperty = "TG2007";

    public const string BackendInvalidPropertyFlags = "TG2008";

    public const string BackendInvalidScopes = "TG2009";

    public const string BackendInvalidConstraint = "TG2010";

    public const string BackendUnsupportedSemantic = "TG2011";

    public const string BackendUnsupportedType = "TG2012";

    public const string BackendResourceIdentityCollision = "TG2201";

    public const string BackendSerializationFailure = "TG2301";

    public const string BackendInvalidJsonDocument = "TG2401";

    public const string BackendInvalidJsonShape = "TG2402";

    public const string BackendInvalidJsonReference = "TG2403";

    public const string BackendInvalidJsonValue = "TG2404";

    public const string BackendJsonSemanticMismatch = "TG2405";

    public const string BackendArchiveWriteFailure = "TG2501";

    public const string BackendInvalidArchive = "TG2502";
}
