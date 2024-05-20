// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public abstract class ModelValidationRuleContext
    {
        public bool BailOnError { get; set; }
    }
}
