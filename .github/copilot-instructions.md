# Copilot Instructions

## General Guidelines
- InteractR should not support a request-model API; keep it aligned with its own use-case/interactor paradigm rather than MediatR-style request semantics.
- Design publish around outlets with inbox/outbox semantics: publish should deliver in-process handlers and also dispatch to out-of-process broker outlets, with idempotent handling so broker echoes with the same message id are ignored by in-process handlers once already handled.
- INotificationInlet should represent registered inbound adapters that the Hub subscribes to; consumers should not call inlet APIs directly per message. The Hub should ingest from all registered inlets and route messages to in-process handlers.
- Refactor notifications toward opinionated DDD-style events: prefer events over notifications, have Hub start all registered inlets while inlets handle broker routing, avoid separate inlet/ingress abstractions if unnecessary, and support event subscription via implicit routing or explicit route attributes.