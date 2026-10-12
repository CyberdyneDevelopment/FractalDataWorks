# Granting access to Managed Identity

The managed-identity endpoints introduce **no new access mechanism**. A person gets access the same
way they get access to connections, datasets or schedules: they are assigned a role, and the role
carries the permission. If you already know how to grant someone access to any other FDW domain, you
already know how to grant this one — skip to [§5](#5-how-to-confirm-it-worked) for the part that is
specific to identities.

For what the domain *is*, see [Managed Identity](12-17-Managed-Identity.md). This page is only about
who may call it.

---

## 1. The short answer

Assign the person **Viewer** to let them see the configured identities, or **Operator** to let them
verify one as well. **Admin** already has both.

| I want them to… | Assign |
|---|---|
| See which identities exist and which mechanisms are available | `Viewer` (or `User`) |
| Also verify an identity actually works | `Operator` |
| Everything, including every other FDW domain | `Admin` |

---

## 2. The two permissions

The domain is gated on exactly two permissions. Both are seeded by
`databases-seed/ConfigurationDb/seed/07-seed-authz.sql`.

| Permission | Grants | Endpoint |
|---|---|---|
| `identities:read` | List configured identities, list available mechanisms | `GET /identities`, `GET /identity-mechanisms` |
| `identities:write` | Verify an identity by acquiring a real token from its provider | `POST /identities/{name}/verify` |

**Why verifying is a write and not a read.** `POST /identities/{name}/verify` does not read stored
state — it performs a live client-credentials exchange against the configured provider. That mints a
real token, consumes provider-side rate limit, and leaves an authentication event in the IdP's audit
log. Treating it as a read would let anyone who can see the list also generate authentication
traffic at your identity provider on demand.

`VerifyIdentity` never returns the token it obtained; it reports only whether acquisition succeeded.
Two tests enforce that structurally.

> **Note for anyone adding a permission here later.** `ListIdentityMechanisms` has
> `ResourceName = "identity-mechanisms"` but its policy is explicitly `identities:read`, not the
> resource-derived `identity-mechanisms:read`. Derive permission names from the endpoint's
> `ReadPolicy`/`VerifyPolicy`, never from its resource name — a name that does not match the literal
> exactly produces a 403 with no other symptom.

---

## 3. Which role grants what

| Role | `identities:read` | `identities:write` | Net effect |
|---|:---:|:---:|---|
| **Admin** | ✅ | ✅ | Full access |
| **Operator** | ✅ | ✅ | List and verify |
| **Viewer** | ✅ | — | List only |
| **User** | ✅ | — | List only |
| **DataReader** | — | — | No access |
| **DataWriter** | — | — | No access |

`User` and `Viewer` carry identical permission sets by design. `DataReader` and `DataWriter` are
data-domain roles and deliberately see nothing here.

Operator holds `identities:write` even though it does not hold `users:write` or `settings/role:write`
— the Operator carve-out exists to keep *privilege* operations (creating users, granting roles,
resetting passwords) out of the role. Verifying an identity grants nobody anything, so it is not in
that category.

---

## 4. How to assign the role

Role membership lives in `usr.UserRoles`, keyed by role **name**. A seed run materialises it into
`authz.UserRole`, which is what the authorization resolver reads.

**In a development environment**, add the pairing to
`databases-seed/ConfigurationDb/seed/00-seed-users.sql` and re-run the seed. The inserts are
idempotent, so re-running is a no-op for rows that already exist:

```sql
INSERT INTO usr.UserRoles (Id, UserId, [Role], IsCurrent, IsDeleted)
SELECT NEWID(), u.Id, v.[Role], 1, 0
FROM (VALUES
    ('somebody', 'Operator')      -- username, role name
) v(UserName, [Role])
JOIN usr.Users u ON u.UserName = v.UserName AND u.IsCurrent = 1 AND u.IsDeleted = 0
WHERE NOT EXISTS (
    SELECT 1 FROM usr.UserRoles x
    WHERE x.UserId = u.Id AND x.[Role] = v.[Role] AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

Then re-run `07-seed-fdw-baseline-roles.sql` so the `authz.UserRole` row is materialised.

**In a deployed environment**, use the users/roles API rather than touching the database. Assigning a
role is itself a privileged operation and requires `users:write` — which, per the carve-out above,
means an **Admin**, not an Operator.

---

## 5. How to confirm it worked

Three checks, in the order that isolates the failure fastest.

**1. The permissions exist at all.** If they were never seeded, *every* call 403s for *every* role,
including Admin — and nothing in the response says why. This is the single most likely cause of
"I assigned the role and it still doesn't work":

```sql
SELECT Name FROM authz.Permission
WHERE Name IN ('identities:read', 'identities:write')
  AND IsCurrent = 1 AND IsDeleted = 0;
```

Two rows expected. Zero rows means `07-seed-authz.sql` has not been run against this database.

**2. The grant reached the role.**

```sql
SELECT r.Name AS RoleName, p.Name AS Permission
FROM authz.RolePermission rp
JOIN authz.Role r       ON r.Id = rp.RoleId       AND r.IsCurrent = 1 AND r.IsDeleted = 0
JOIN authz.Permission p ON p.Id = rp.PermissionId AND p.IsCurrent = 1 AND p.IsDeleted = 0
WHERE p.Name LIKE 'identities:%' AND rp.IsCurrent = 1 AND rp.IsDeleted = 0
ORDER BY r.Name, p.Name;
```

**3. The permission is in the token.** Permissions are **baked into the access token** as one `perm`
claim each, at the moment the token is issued. Decode the JWT and look for `"perm": "identities:read"`.

> **The gotcha this causes:** changing a role assignment has **no effect on a token that has already
> been issued**. The holder keeps their old permissions until that token expires and a new one is
> minted. After granting a role, sign out and back in — or wait for refresh — before concluding the
> grant did not work.

Then call the endpoint:

```bash
curl -s -H "Authorization: Bearer $TOKEN" https://<host>/identities
```

`200` with a list means read access is working. `403` after all three checks pass means the token
predates the grant — see the gotcha above.

To confirm write access, verify an identity. This performs a real token acquisition against the
provider, so expect it to appear in the IdP's audit log:

```bash
curl -s -X POST -H "Authorization: Bearer $TOKEN" https://<host>/identities/<name>/verify
```

A `Succeeded: true` response means the identity is correctly configured end to end. A `403` means the
caller lacks `identities:write`; a `200` with `Succeeded: false` means the caller had permission and
the *identity* is misconfigured — a different problem, described in
[Managed Identity](12-17-Managed-Identity.md).
