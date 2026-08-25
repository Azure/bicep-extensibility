# Contract

> [!WARNING]
> The extensibility platform and contract are under active development. The contract is
> not yet ready for extension-author implementation and may change before release.

The Bicep Extension API Contract defines the protocol between the Bicep Extensibility Host (a component of `Microsoft.Resources/deployments`) and your extension. All extensions must conform to this contract.

This section is protocol reference. To build an extension, begin with the
[managed extension quickstart](../tutorials/getting-started.md) or the
[self-hosted FirstParty handoff](../get-started/first-party.md), then return here for
wire-level requirements.

## Documents

| Document | Description |
|----------|-------------|
| [API Contract](contract.md) | The complete V2 contract, including operations, models, HTTP binding, authentication, and limits. |
| [Preview Operation](preview-operation.md) | Deep-dive on the preview operation: unevaluated expressions, preview metadata, What-If integration. |
| [Async Operations](async-operations.md) | Long-running operation patterns: RELO (recommended) and LRO, with sequence diagrams and examples. |
