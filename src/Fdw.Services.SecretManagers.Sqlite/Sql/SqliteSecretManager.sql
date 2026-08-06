-- SQLite DDL for FractalDataWorks.Services.SecretManagers.Sqlite
--
-- TableName is configurable (default "Secret") via SqliteSecretManagerConfiguration.TableName.
-- The EnsureTableExists() call in SqliteSecretManagerFactory substitutes the actual
-- table name at runtime.  This file documents the canonical structure.
--
-- Version-on-write: a SET is deactivate (IsCurrent=0) + INSERT new row.
-- Soft delete:      SET IsCurrent=0, IsDeleted=1 on the active row.
-- Partial unique index enforces at most one active (IsCurrent=1, IsDeleted=0) row per SecretKey.
-- Dates stored as ISO 8601 TEXT (SQLite has no native DATETIME type).

CREATE TABLE IF NOT EXISTS "Secret" (
    "RowId"        TEXT    NOT NULL DEFAULT (lower(hex(randomblob(16)))),
    "SecretKey"    TEXT    NOT NULL,
    "SecretValue"  TEXT    NOT NULL,
    "Version"      INTEGER NOT NULL DEFAULT 1,
    "SecretType"   TEXT    NOT NULL DEFAULT 'Password',
    "Description"  TEXT    NULL,
    "ExpiresAt"    TEXT    NULL,           -- ISO 8601, e.g. 2030-01-01T00:00:00Z
    "CreateDate"   TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    "ModifyDate"   TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    "IsCurrent"    INTEGER NOT NULL DEFAULT 1,
    "IsDeleted"    INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT "PK_Secret" PRIMARY KEY ("RowId")
);

-- Enforce one active row per key.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Secret_Current"
    ON "Secret" ("SecretKey")
    WHERE "IsCurrent" = 1 AND "IsDeleted" = 0;
