#!/usr/bin/env bash
# FDW-6365 scenario fixture: the three test people, created and removed through the admin API of the host the scenarios run against.
#   people.sh create   creates the roles scenario-read-role (datastores:read), scenario-write-role (datastores:read and datastores:write:
#                      the scenarios list rows as the writer, so it holds both) and a user for each of scenario-read, scenario-write and
#                      scenario-none (no role), signs each in and writes a bearer token to a file;
#   people.sh token    signs the three in again and rewrites the token files (an access token expires: do this just before a run);
#   people.sh remove   deletes the three users and the two roles, and the files.
# Environment, ALL required, none has a default (a missing one stops the script naming it):
#   FDW_SCENARIO_HOST         base address of the host, the same value the scenarios use
#   FDW_SCENARIO_CONFIRM      must equal FDW_SCENARIO_HOST: the host you mean, typed twice, so the wrong host is not acted on by accident
#   FDW_ADMIN_TOKEN_FILE      a file holding a bearer token of an administrator who may create users and roles (users:write and the roles
#                             permissions) in the tenant that can see the data store; supplied by the host owner, never created here
#   FDW_SCENARIO_PEOPLE_DIR   a directory OUTSIDE any git work tree, mode 700, where the files go (token-*, password-*; mode 600)
# No token, password or key is ever printed, passed as an argument (curl gets the administrator token as a header file and each body as a file) or written anywhere but the mode-600 files above. The passwords are random,
# kept only so `token` can sign in again, and removed by `remove`. Dev only: it acts on the host you name and on no other.
# `create` reuses a password file left by an earlier run, but a user that already exists keeps its OLD password: if the sign-in then fails, run
# `remove` first and `create` again. The administrator token is passed to curl through a header FILE, never on the command line, and every temp file is
# removed on exit.
# NOT RUN by its author. Written from FDW's endpoints (POST /users, POST /roles, PATCH /roles/{Name}/permissions, DELETE /users/{IdOrName},
# DELETE /roles/{Name}) and from the route owner's proof script (POST /auth/login, flowName PasswordLogin, answer accessToken). The first run is the test.
set -u
umask 077
ACTION=${1:?create, token or remove}
need() { local n=$1; [ -n "${!n:-}" ] || { echo "STOP: $n is not set; nothing stands in for it." >&2; exit 2; }; }
need FDW_SCENARIO_HOST; need FDW_SCENARIO_CONFIRM; need FDW_ADMIN_TOKEN_FILE; need FDW_SCENARIO_PEOPLE_DIR
[ "$FDW_SCENARIO_CONFIRM" = "$FDW_SCENARIO_HOST" ] || { echo "STOP: FDW_SCENARIO_CONFIRM is not the same as FDW_SCENARIO_HOST." >&2; exit 2; }
[ -r "$FDW_ADMIN_TOKEN_FILE" ] || { echo "STOP: $FDW_ADMIN_TOKEN_FILE is not readable." >&2; exit 2; }
D=$FDW_SCENARIO_PEOPLE_DIR
[ -d "$D" ] || { echo "STOP: $D is not a directory." >&2; exit 2; }
[ "$(stat -c %a "$D")" = 700 ] || { echo "STOP: $D must be mode 700." >&2; exit 2; }
git -C "$D" rev-parse --is-inside-work-tree >/dev/null 2>&1 && { echo "STOP: $D is inside a git work tree." >&2; exit 2; }
API=${FDW_SCENARIO_HOST%/}/api/v1
TMPS=()
cleanup() { [ ${#TMPS[@]} -gt 0 ] && rm -f "${TMPS[@]}"; }
trap cleanup EXIT
mk() { MK=$(mktemp); TMPS+=("$MK"); }  # sets MK; not called in $(...), a subshell would lose the TMPS entry
# The administrator token is written to a mode-600 header file and given to curl as -H @file: a command-line argument is visible to any local user in ps.
mk; HDR=$MK
printf 'Authorization: Bearer %s\n' "$(tr -d '\r\n' < "$FDW_ADMIN_TOKEN_FILE")" > "$HDR"
PEOPLE=(read write none)
role_of() { case $1 in read) echo scenario-read-role;; write) echo scenario-write-role;; none) echo "";; esac; }
perms_of() { case $1 in read) echo '["datastores:read"]';; write) echo '["datastores:read","datastores:write"]';; esac; }
# call METHOD PATH [JSON]: prints "<status> <what>" and never the body unless it is an error; sets BODY to the answer file's path.
call() {
  local m=$1 p=$2 b=${3:-}; mk; BODY=$MK; local code
  if [ -n "$b" ]; then mk; local req=$MK; printf '%s' "$b" > "$req"   # the body of POST /users holds a password: a file, never -d
    code=$(curl -s -o "$BODY" -w '%{http_code}' -X "$m" "$API$p" -H @"$HDR" -H 'Content-Type: application/json' --data-binary @"$req")
  else code=$(curl -s -o "$BODY" -w '%{http_code}' -X "$m" "$API$p" -H @"$HDR"); fi
  echo "$m $p -> $code"; LASTCODE=$code
}
signin() { # name: signs in as scenario-<name>, writes token-<name>
  local n=scenario-$1 out; mk; out=$MK; TMPS+=("$out.req")
  python3 -I - "$n" "$D/password-$1" > "$out.req" <<'PY'
import json,sys
print(json.dumps({"username":sys.argv[1],"password":open(sys.argv[2]).read().strip(),"flowName":"PasswordLogin"}))
PY
  local code; code=$(curl -s -o "$out" -w '%{http_code}' -X POST "$API/auth/login" -H 'Content-Type: application/json' --data-binary @"$out.req"); rm -f "$out.req"
  python3 -I - "$out" "$D/token-$1" <<'PY'
import json,sys
try: t=json.load(open(sys.argv[1])).get("accessToken","")
except Exception: t=""
if not t: sys.exit(3)
open(sys.argv[2],"w").write(t)
PY
  local rc=$?; rm -f "$out"; chmod 600 "$D/token-$1" 2>/dev/null
  [ $rc -eq 0 ] && echo "signed in scenario-$1 (HTTP $code), token written" || { echo "STOP: sign-in of scenario-$1 failed (HTTP $code)."; exit 3; }
}
case $ACTION in
create)
  for p in read write; do
    r=$(role_of $p); call POST /roles "{\"name\":\"$r\",\"displayName\":\"$r\",\"description\":\"FDW-6365 scenario fixture\"}"
    [ "$LASTCODE" = 201 ] || [ "$LASTCODE" = 200 ] || [ "$LASTCODE" = 409 ] || { echo "STOP: creating role $r answered $LASTCODE:"; head -c 600 "$BODY"; exit 3; }
    call PATCH "/roles/$r/permissions" "{\"permissionNames\":$(perms_of $p)}"
    [ "$LASTCODE" = 200 ] || [ "$LASTCODE" = 204 ] || { echo "STOP: setting the permissions of $r answered $LASTCODE:"; head -c 600 "$BODY"; exit 3; }
  done
  for p in "${PEOPLE[@]}"; do
    [ -e "$D/password-$p" ] || python3 -I -c "import secrets;print(secrets.token_urlsafe(24))" > "$D/password-$p"
    chmod 600 "$D/password-$p"
    body=$(python3 -I - "scenario-$p" "$D/password-$p" "$(role_of $p)" <<'PY'
import json,sys
u,pw,role=sys.argv[1],open(sys.argv[2]).read().strip(),sys.argv[3]
d={"username":u,"signInMethods":{"password":{"initialPassword":pw}}}
if role: d["roles"]=[role]
print(json.dumps(d))
PY
)
    call POST /users "$body"
    [ "$LASTCODE" = 201 ] || [ "$LASTCODE" = 200 ] || [ "$LASTCODE" = 409 ] || { echo "STOP: creating user scenario-$p answered $LASTCODE:"; head -c 600 "$BODY" | sed -E 's/"(password|initialPassword)"[^,}]*/"\1":"..."/g'; exit 3; }
    signin "$p"
  done
  echo "Token files: $D/token-read, $D/token-write, $D/token-none. Set FDW_SCENARIO_TOKEN_READ/_WRITE/_NONE from them, without printing them."
  ;;
token) for p in "${PEOPLE[@]}"; do signin "$p"; done ;;
remove)
  for p in "${PEOPLE[@]}"; do call DELETE "/users/scenario-$p"; done
  for p in read write; do call DELETE "/roles/$(role_of $p)"; done
  rm -f "$D"/token-read "$D"/token-write "$D"/token-none "$D"/password-read "$D"/password-write "$D"/password-none
  echo "files removed" ;;
*) echo "STOP: create, token or remove." >&2; exit 2 ;;
esac
