# UI comments API (`/ui-feedback`)

A UI comment is a post-it a tester pins to an area of a screen (Feature FDW-2141, story FDW-2143). The console saves, lists, opens, resolves and reopens them here.
The service is `IUiFeedbackService` (Fdw.Services.Messaging); these endpoint bases are thin: they bind the request, call the service and answer with the status the failure's result code carries.

## Who may do what

| Permission | Allows |
|---|---|
| `ui-feedback:write` | Save a comment. Change the note of, or delete, your own. |
| `ui-feedback:read` | List and open every comment, read a picture, and be told of a new one. |
| `ui-feedback:resolve` | Resolve and reopen a comment. |
| (signed in) | Ask a question: save a comment whose every tag asks for an answer (`Question`), and comment in its thread. |
| the permission mapped to a question's screen | Be told of the question, open it, read its picture and its thread, and answer in it (see Questions). |

The author and tenant of a comment are the caller's, never stated by the request. A reader reaches the comments of the tenant they act in. A caller acting in the global tenant (the platform administrator) reaches every tenant's. A comment out of reach is answered `404`, as if it did not exist. A permission is carried in the token, so a user who was just given one signs in again.

## Routes

| Route | Permission | Answers |
|---|---|---|
| `POST /ui-feedback` | signed in; write unless a question | `201` with the comment; `403` (`MESSAGING-100002`) when a caller without write saves anything but a question |
| `GET /ui-feedback` | signed in | `200` with `{ items: [...] }`, no pictures; a caller with neither read nor write lists their own comments and the questions put to them |
| `GET /ui-feedback/{id}` | signed in | `200` with the comment; `404` for one the caller may not see |
| `GET /ui-feedback/{id}/screenshot` | signed in | `200` with the PNG; `404` when the comment has none or the caller may not see it |
| `PATCH /ui-feedback/{id}` `{ body }` | write | `200`; `403` when the comment is another person's |
| `DELETE /ui-feedback/{id}` | write | `204`; `403` when it is another person's |
| `POST /ui-feedback/{id}/acknowledge` | read | `200` with `acknowledgedByMe` true; asking again changes nothing and still answers `200` |
| `POST /ui-feedback/{id}/agree` | write | `200` with `agreedByMe` true and `agreeCount` (people who agree, the author not counted); asking again changes nothing; the author gets `409` |
| `DELETE /ui-feedback/{id}/agree` | write | `200` with `agreedByMe` false; taking back an agreement not given changes nothing |
| `POST /ui-feedback/{id}/comments` | signed in; write unless a question the caller asked or was asked | `403` (`MESSAGING-100003`) otherwise; `201` with the comment (`id`, `feedbackId`, `authorUserId`, `authorName`, `body`, `createdAt`); `body` is required, at most 4000 characters |
| `GET /ui-feedback/{id}/comments?skip=&take=` | signed in, who may see the comment | `200` with `{items}`, oldest first; `take` from 1 to 200 |
| `POST /ui-feedback/{id}/resolve` | resolve | `200`; `409` when already resolved |
| `POST /ui-feedback/{id}/reopen` | resolve | `200`; `409` when already open |

### Save

`screen`, `route`, `area` `{ x, y, w, h }`, `viewport` `{ w, h }`, `urgency`, `tags` (a list, empty for none), `anchors` and `body` are required. `anchors` is the components the comment is about, in the order the reviewer chose them: at least one and at most 20, each a JSON object of at most 4000 characters, kept as sent and returned in the same order as `anchors` on every comment. `theme` and `screenshot` (a PNG data URL, at most 2 MB once decoded) are optional.
Nothing is cut or defaulted: a missing or wrong field is a `400` that names the field, and a picture that is too large or is not a PNG is refused whole. `urgency` and every tag must be an option of the `UiFeedbackUrgencies` and `UiFeedbackTags` type collections (`GET /type-collections/{name}/values`).

### List

Query: `state` (`Open` or `Resolved`), `route`, `urgency`, `tag`, `mine=true` (only the caller's own), `askedOfMe=true` (only the questions put to the caller), `sort=urgency` (most urgent first, by the rank the urgency states), `skip`, `take` (1 to 200, 50 when left out). Newest first when `sort` is left out.
The store cannot filter by tag, by the questions put to the caller or order by rank, so with `tag`, `askedOfMe` or `sort=urgency`, and for a caller with neither read nor write, the list looks at the 1000 newest comments that match the other conditions and pages over those.

## Questions

A comment tagged `Question` (a `UiFeedbackTags` option whose `AsksForAnAnswer` is true) asks someone who knows the screen for information or an answer. A console's guided tour posts it as an ordinary comment: the components chosen in `anchors`, the question in `body`.

- **Who may ask.** Any signed-in person: the save route asks only for a signed-in caller, and the service refuses a caller without `ui-feedback:write` whose comment has a tag that does not ask for an answer, or no tag (`403`, `MESSAGING-100002`).
- **Who answers.** The setting `UiFeedbackQuestionAnswerers` (a server setting, overridable per tenant; DataType `String`) holds a JSON array of `{ "routePrefix": "/documents", "permission": "document-setup:write" }`. The longest prefix that matches the question's `route` on whole segments wins (`/data` matches `/data/x`, not `/datasets`). The answerers are the people who hold that permission through a role in the question's tenant and, when the asker acts in an organization, through what that organization grants them (its roles' access rows, `IPermissionHolders.GetInOrganization`). No setting, or no prefix that matches: only the readers are told. A value that is not such a list is reported (`MESSAGING-61005`) and only the readers are told.
- **Telling them.** Each answerer gets one message (type `UiFeedback`, action URL `/feedback/{id}`, subject `"{author} asked a question on {route}"`, the subject the readers of a question get too); the author and anyone already told as a reader are left out. An answerer who cannot be told (`MESSAGING-71007`) or holders who cannot be found (`MESSAGING-71008`) are logged and returned beside the saved question; the save still answers `201`.
- **Opening and answering.** A person whose session holds the permission mapped to a question's route (the roles held in the tenant they act in and what the organization they act in grants) may open the question, read its picture and its thread, find its alert and comment in its thread, without `ui-feedback:read`. The person who asked may comment in its thread too. Agreeing, editing, deleting, acknowledging and resolving keep their rights. A comment that is not a question put to the caller is `404` to them, as any comment they may not see.

## Walking it on dev

The two users must act in the same tenant, or the second cannot reach what the first saved.

1. Sign in as `testuser` (role UiReviewer, write) and `POST /ui-feedback` with a small PNG data URL as `screenshot`, `urgency` `High`, `tags` `["UsabilityIssue"]`. Expect `201`, `state` `Open`, `isMine` true, `hasScreenshot` true.
2. Sign in as `afcuser` (role Developer, read and resolve). `GET /ui-feedback?state=Open&sort=urgency` lists it; `GET /ui-feedback/{id}` opens it; `GET /ui-feedback/{id}/screenshot` returns the PNG.
3. As `afcuser`, `POST /ui-feedback/{id}/resolve`: expect `state` `Resolved`, `resolvedByName` set. Resolve again: expect `409`. `POST /ui-feedback/{id}/reopen` clears `resolvedByName` and `resolvedAt`.
4. As `afcuser`, `PATCH` or `DELETE` the comment: expect `403`. As `testuser`, `PATCH` changes the note and sets `updatedAt`; `DELETE` removes the comment, its tags, its anchors and its picture.
5. A save with `urgency` left out answers `400` naming `urgency`.
6. Save a comment as `testuser`. `GET /messages` as `afcuser` (and as an Admin) lists a `UiFeedback` message about it; as `testuser` it lists none. Save one with urgency `SecurityIssue`: its message has severity `Error`.
7. As `afcuser`, `POST /ui-feedback/{id}/acknowledge`: `acknowledgedByMe` is true and the message is read. Do it again: the same answer, no second row.

## Who is told, and acknowledging (FDW-2144)

Saving a comment sends a message (type `UiFeedback`, severity from the urgency, action URL `/feedback/{id}`, reference the comment's id) to each person who holds `ui-feedback:read` in the comment's tenant or in the global tenant, except the author. The save answers `201` whatever happens to a message: a reader who could not be told is logged as an error and returned among the result's messages, and the comment is kept.

`POST /ui-feedback/{id}/acknowledge` records who acknowledged and when (one row per person), and marks that person's alert about the comment read. Every answer carries `acknowledgedByMe`. Whether an alert must be acknowledged is the urgency's `needsAcknowledgement`; the route itself accepts any comment the caller reaches.
