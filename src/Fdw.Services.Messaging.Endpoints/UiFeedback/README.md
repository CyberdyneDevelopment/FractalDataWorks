# UI comments API (`/ui-feedback`)

A UI comment is a post-it a tester pins to an area of a screen (Feature FDW-2141, story FDW-2143). The console saves, lists, opens, resolves and reopens them here.
The service is `IUiFeedbackService` (Fdw.Services.Messaging); these endpoint bases are thin: they bind the request, call the service and answer with the status the failure's result code carries.

## Who may do what

| Permission | Allows |
|---|---|
| `ui-feedback:write` | Save a comment. Change the note of, or delete, your own. |
| `ui-feedback:read` | List and open comments, read a picture. |
| `ui-feedback:resolve` | Resolve and reopen a comment. |

The author and tenant of a comment are the caller's, never stated by the request. A reader reaches the comments of the tenant they act in. A caller acting in the global tenant (the platform administrator) reaches every tenant's. A comment out of reach is answered `404`, as if it did not exist. A permission is carried in the token, so a user who was just given one signs in again.

## Routes

| Route | Permission | Answers |
|---|---|---|
| `POST /ui-feedback` | write | `201` with the comment |
| `GET /ui-feedback` | read | `200` with `{ items: [...] }`, no pictures |
| `GET /ui-feedback/{id}` | read | `200` with the comment |
| `GET /ui-feedback/{id}/screenshot` | read | `200` with the PNG; `404` when the comment has none |
| `PATCH /ui-feedback/{id}` `{ body }` | write | `200`; `403` when the comment is another person's |
| `DELETE /ui-feedback/{id}` | write | `204`; `403` when it is another person's |
| `POST /ui-feedback/{id}/acknowledge` | read | `200` with `acknowledgedByMe` true; asking again changes nothing and still answers `200` |
| `POST /ui-feedback/{id}/resolve` | resolve | `200`; `409` when already resolved |
| `POST /ui-feedback/{id}/reopen` | resolve | `200`; `409` when already open |

### Save

`screen`, `route`, `area` `{ x, y, w, h }`, `viewport` `{ w, h }`, `urgency`, `tags` (a list, empty for none) and `body` are required. `theme`, `anchor` (any JSON, kept as sent) and `screenshot` (a PNG data URL, at most 2 MB once decoded) are optional.
Nothing is cut or defaulted: a missing or wrong field is a `400` that names the field, and a picture that is too large or is not a PNG is refused whole. `urgency` and every tag must be an option of the `UiFeedbackUrgencies` and `UiFeedbackTags` type collections (`GET /type-collections/{name}/values`).

### List

Query: `state` (`Open` or `Resolved`), `route`, `urgency`, `tag`, `sort=urgency` (most urgent first, by the rank the urgency states), `skip`, `take` (1 to 200, 50 when left out). Newest first when `sort` is left out.
The store cannot filter by tag or order by rank, so with `tag` or `sort=urgency` the list looks at the 1000 newest comments that match the other conditions and pages over those.

## Walking it on dev

The two users must act in the same tenant, or the second cannot reach what the first saved.

1. Sign in as `testuser` (role UiReviewer, write) and `POST /ui-feedback` with a small PNG data URL as `screenshot`, `urgency` `High`, `tags` `["UsabilityIssue"]`. Expect `201`, `state` `Open`, `isMine` true, `hasScreenshot` true.
2. Sign in as `afcuser` (role Developer, read and resolve). `GET /ui-feedback?state=Open&sort=urgency` lists it; `GET /ui-feedback/{id}` opens it; `GET /ui-feedback/{id}/screenshot` returns the PNG.
3. As `afcuser`, `POST /ui-feedback/{id}/resolve`: expect `state` `Resolved`, `resolvedByName` set. Resolve again: expect `409`. `POST /ui-feedback/{id}/reopen` clears `resolvedByName` and `resolvedAt`.
4. As `afcuser`, `PATCH` or `DELETE` the comment: expect `403`. As `testuser`, `PATCH` changes the note and sets `updatedAt`; `DELETE` removes the comment, its tags and its picture.
5. A save with `urgency` left out answers `400` naming `urgency`.
6. Save a comment as `testuser`. `GET /messages` as `afcuser` (and as an Admin) lists a `UiFeedback` message about it; as `testuser` it lists none. Save one with urgency `SecurityIssue`: its message has severity `Error`.
7. As `afcuser`, `POST /ui-feedback/{id}/acknowledge`: `acknowledgedByMe` is true and the message is read. Do it again: the same answer, no second row.

## Who is told, and acknowledging (FDW-2144)

Saving a comment sends a message (type `UiFeedback`, severity from the urgency, action URL `/feedback/{id}`, reference the comment's id) to each person who holds `ui-feedback:read` in the comment's tenant or in the global tenant, except the author. The save answers `201` whatever happens to a message: a reader who could not be told is logged as an error and returned among the result's messages, and the comment is kept.

`POST /ui-feedback/{id}/acknowledge` records who acknowledged and when (one row per person), and marks that person's alert about the comment read. Every answer carries `acknowledgedByMe`. Whether an alert must be acknowledged is the urgency's `needsAcknowledgement`; the route itself accepts any comment the caller reaches.
