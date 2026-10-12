# 12-19 Choosing Who May Use Each Sign-In Method

Each sign-in method (password, an identity provider, an API key) can be off, open to every active
user, or limited to users who hold one permission. You decide this per method, in configuration.
Allowing one more person is a role grant, not a configuration change.

## The three settings

| You want | Do this |
|---|---|
| Nobody may use the method | Set `IsEnabled = 0` on the flow's `auth.AuthenticationFlowConfiguration` row |
| Every active user may use it | Leave the `AuthorizeSignInMethod` step out of the flow |
| Only some users may use it | Add an `AuthorizeSignInMethod` step after `BakePermissions`, and give those users a role that carries the permission |

A switched-off flow keeps its row. Selecting it answers `403`; a flow nobody configured answers
`404`; a flow configured but broken answers `500`. A switched-off flow is not offered, cannot resume a
suspended sign-in, and its sessions and unredeemed one-time codes stop working. Hosts read flows at
startup, so restart the host after changing a flow row by hand. A change made through the routes below needs no restart on the host that made it.

## Change it from a console or the API

An administrator holding `settings/server:write` can make all three changes without editing rows:

| Route | Does |
|---|---|
| `GET /auth/flows` | Lists every configured flow: switched on or off, served or broken (with the reason), the methods it proves, and the permissions it requires. Needs `settings/server:read`. |
| `PATCH /auth/flows/{Name}` `{"isEnabled": false}` | Switches the flow off or on. |
| `POST /auth/flows/{Name}/required-permission` `{"permission": "signin:password"}` | Limits the flow to holders of the permission. The `AuthorizeSignInMethod` step is placed straight after `BakePermissions`, or its permission is rewritten if it is already there. |
| `DELETE /auth/flows/{Name}/required-permission` | Opens the flow to every active user. |

A change is written like any other configuration change (versioned) and takes effect on the host that
served the request at once. Another host serving the same flows picks it up when it restarts. The
reference console's **Sign-in methods** administration tab uses these routes.

The routes refuse only what would leave sign-in unable to work. Everything else a flow can express stays
expressible in configuration:

| Refused | Answer |
|---|---|
| A permission the platform does not define (a typo) | `400` |
| A permission no role carries: the flow would admit nobody. Grant it to a role first. | `409` |
| Switching off or limiting a flow when that leaves **you** no enabled flow that proves how you signed in and admits you | `409` |
| A flow with nothing that bakes permissions, one already requiring several, or one limited by a gate this host cannot configure | `409`, change it in configuration |

The lock-out check reads the methods your sign-in proved (the token's `amr`) and the permissions you hold
in your default tenant. Opening a flow or switching one on only widens who may sign in, so it is never
refused. A caller authenticated by an API key did not sign in through a flow, so no flow change can lock
it out.

A gate step can be placed and pointed at a permission from the console only when it implements
`IAdministrablePermissionGate`, which writes its own settings. The shipped `AuthorizeSignInMethod` does.
A gate that implements only `IPermissionGatedAuthenticationStep` still works: its permission is shown,
the flow can be opened, and it is otherwise changed in configuration.

`GET /users/{IdOrName}/sign-in-methods` says whether a password would work for the account
(`password.isUsable`) and whether the caller may remove it (`password.canRemove`). A console uses these
so it never offers a password the route would refuse.

## Limit a flow to some users

1. Add the step row, after `BakePermissions` and before `AuthorizeIssuance`:

   | StepOrder | StepName | Configuration |
   |---|---|---|
   | 3 | `AuthorizeSignInMethod` | `{"RequiredPermission":"signin:password"}` |

2. Grant the permission through a role. The seeds create `PasswordSignIn`, which carries
   `signin:password`. Assign it in the tenant the user's sign-in lands in (their default tenant).

The step reads the permissions `BakePermissions` baked, so it cannot run before it; the flow fails
to load if you order it earlier. A principal without the permission is refused, and no later step
can overrule the refusal. Malformed settings, or settings that name no `RequiredPermission`, make the
flow fail to load with the reason in the log.

Gate every flow that proves the same method. The seeds gate both password flows, `PasswordLogin` and
`PasswordSession`; gating only one leaves the other as a way round.

## Passwords follow the same rule

`POST /users/{IdOrName}/password-login` and `POST /users/{IdOrName}/reset-password` refuse, with
`409`, an account that no enabled password flow would admit. A password nobody can use is never
issued. `POST /users` with a password method applies the same rule to the roles it is given, before
it writes anything: it answers `409` with a validation error named `roles` (the route's other `409`
is a taken username). Nothing is granted to make a password usable; name a role that allows it,
for example `["Viewer", "PasswordSignIn"]`. `DELETE /users/{IdOrName}/password-login` removes an account's password and leaves its other
sign-in methods alone.

## API keys

The ApiKey authentication service row carries one permission per key kind:

| Column | Applies to |
|---|---|
| `RequiredUserKeyPermission` | personal access tokens |
| `RequiredAgentKeyPermission` | agent keys |

`NULL` leaves that kind open. When set, the key's owner must hold the permission in the tenant the
key acts in (their default tenant), or the key does not authenticate. A key sees exactly what its
owner sees there. In the reference API the service row is
`server-config/auth/ApiKeyAuthenticationServiceConfiguration.json`.

## Onboard an external party

A vendor building an ingestion needs a user, a sign-in method you allow, and a key that sees what the
user sees:

1. Create the user with the roles their work needs, and no `PasswordSignIn` unless they should use a
   password. `Roles` is required on `POST /users`; send `[]` for none.
2. Give them a way in:
   - an identity provider: bind the issuer's subject, or, when you know only their address, invite
     it (`invitedEmail` instead of `subject`). The first sign-in from that issuer carrying the address
     verified redeems the invitation, once, through a flow whose `ResolvePrincipal` step opts in:
     `{"Invitations":{"EmailClaim":"email","EmailVerifiedClaim":"email_verified"}}`, or
     `"TrustIssuerEmail": true` instead of `EmailVerifiedClaim` for an issuer that sends no
     verification claim and controls its users' addresses.
   - an API key, created on the user; if the key kind is gated, grant the role carrying its permission.

The seeds also create `signin:entra` and `signin:api-key` for gating an Entra flow and API keys.
