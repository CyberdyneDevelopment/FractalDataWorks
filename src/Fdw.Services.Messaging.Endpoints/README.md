# Fdw.Services.Messaging.Endpoints

Endpoint bases for messaging operations.

Endpoint base classes for this domain's HTTP surface. A host closes over a base with a sealed endpoint that supplies its route; the base supplies the validate → service → map → send shape.

An endpoint is an HTTP orchestrator. It does not open a gateway, and it does not carry business logic — anything a background job would also need belongs in the service.

## Endpoint bases

| Type | Kind | Purpose |
|---|---|---|
| `ApproveAccessRequestEndpointBase` | class | Abstract base class for approving an access request (PUT /access-requests/{Id}/approve). |
| `ArchiveMessageEndpointBase` | class | Abstract base class for archiving a message (PUT /messages/{Id}/archive). |
| `CreateAccessRequestEndpointBase` | class | Abstract base class for creating an access request (POST /access-requests). |
| `DeleteMessageEndpointBase` | class | Abstract base class for deleting a message (DELETE /messages/{Id}). Why: Messaging service has no… |
| `DenyAccessRequestEndpointBase` | class | Abstract base class for denying an access request (PUT /access-requests/{Id}/deny). |
| `DismissMessageEndpointBase` | class | Abstract base class for dismissing a message (PUT /messages/{Id}/dismiss). |
| `GetMessageEndpointBase` | class | Abstract base class for getting a single message by ID (GET /messages/{Id}). |
| `GetUnreadCountEndpointBase` | class | Abstract base class for getting unread message count (GET /messages/unread-count). |
| `ListAccessRequestsEndpointBase` | class | Abstract base class for listing access requests (GET /access-requests). Admins see all pending requests;… |
| `ListMessagesEndpointBase` | class | Abstract base class for listing messages for the current user (GET /messages). |
| `MarkAllReadEndpointBase` | class | Abstract base class for marking all messages as read (PUT /messages/mark-all-read). |
| `MarkMessageReadEndpointBase` | class | Abstract base class for marking a message as read (PUT /messages/{Id}/read). |

## Request and response models

Endpoint-layer models are named `Request` / `Response`; the client layer names its equivalents `Payload`. The two layers are deliberately separate.

| Type | Kind | Purpose |
|---|---|---|
| `AccessRequestIdRequest` | class | Request for getting an access request by ID. |
| `CreateAccessRequestEndpointRequest` | class | Request for creating an access request. |
| `ListMessagesRequest` | class | Request for listing messages with optional filters. |
| `MessageIdRequest` | class | Request for getting a message by ID. |
| `ReviewAccessRequestRequest` | class | Request for approving or denying an access request. |
| `UnreadCountResponse` | class | Response for unread message count. |

## Installation

```bash
dotnet add package Fdw.Services.Messaging.Endpoints --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Messaging.Abstractions` · `Fdw.Web.RestEndpoints`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
