#!/usr/bin/env bash
# Pushes every package in ARTIFACTS_DIR to nuget.org one at a time, and when nuget.org refuses a
# push on its quota (403 Quota Exceeded) waits and retries the same package instead of failing
# the job half-pushed.
#
# Called by .github/workflows/publish-nuget.yml, once per sign-in round. The key NuGet/login
# issues lasts about an hour, so a long wait cannot be spent inside one round: this script stops
# a round when the key is KEY_MAX_AGE_SECONDS old, writes complete=false, and the workflow signs
# in again and calls it again. What is already pushed is remembered in STATE_DIR, so the next
# round carries on at the package that was waiting.
#
# EVERY value comes from the environment (the workflow declares the defaults once); this script
# holds none. A missing or non-numeric value fails the run and names the variable.
#
#   ARTIFACTS_DIR                  directory holding the *.nupkg files
#   NUGET_SOURCE                   the push source URL
#   NUGET_API_KEY                  the key from NuGet/login (never printed)
#   STATE_DIR                      directory that survives between rounds
#   QUOTA_MAX_WAITS                waits allowed per package before the run fails
#   QUOTA_WAIT_SECONDS             wait when nuget.org states no retry-after
#   QUOTA_BACKOFF_FACTOR           that wait is multiplied by this for each further refusal
#   QUOTA_RETRY_AFTER_MARGIN_SECONDS  added to a retry-after nuget.org states
#   KEY_MAX_AGE_SECONDS            age at which a round stops so the workflow signs in again
#   JOB_START_EPOCH                when the job started (epoch seconds)
#   JOB_TIMEOUT_MINUTES            the job's timeout-minutes
#   GITHUB_OUTPUT                  GitHub's step output file (complete=true|false)
#
# Exit 0: the round ended; complete=true means every package is on nuget.org, complete=false
# means the key is old and the workflow must sign in again. Exit 1: the run failed, and the
# message says which package and why.
set -uo pipefail

fail() {
  echo "::error::push-nuget: $*"
  exit 1
}

require_text() {
  local name="$1"
  [ -n "${!name:-}" ] || fail "environment variable $name is not set."
}

require_integer() {
  local name="$1"
  require_text "$name"
  [[ "${!name}" =~ ^[0-9]+$ ]] || fail "$name is '${!name}' but must be a whole number."
}

require_number() {
  local name="$1"
  require_text "$name"
  [[ "${!name}" =~ ^[0-9]+([.][0-9]+)?$ ]] || fail "$name is '${!name}' but must be a number."
}

for name in ARTIFACTS_DIR NUGET_SOURCE NUGET_API_KEY STATE_DIR GITHUB_OUTPUT; do
  require_text "$name"
done
for name in QUOTA_MAX_WAITS QUOTA_WAIT_SECONDS QUOTA_RETRY_AFTER_MARGIN_SECONDS \
            KEY_MAX_AGE_SECONDS JOB_START_EPOCH JOB_TIMEOUT_MINUTES; do
  require_integer "$name"
done
require_number QUOTA_BACKOFF_FACTOR

[ -d "$ARTIFACTS_DIR" ] || fail "ARTIFACTS_DIR '$ARTIFACTS_DIR' is not a directory."
mkdir -p "$STATE_DIR" || fail "cannot create STATE_DIR '$STATE_DIR'."

DONE_FILE="$STATE_DIR/pushed.txt"
WAIT_FILE="$STATE_DIR/waits.txt"   # "<package file> <waits spent so far>"
touch "$DONE_FILE" "$WAIT_FILE"

ROUND_START=$(date +%s)
JOB_DEADLINE=$((JOB_START_EPOCH + JOB_TIMEOUT_MINUTES * 60))

mapfile -t PACKAGES < <(find "$ARTIFACTS_DIR" -maxdepth 1 -name '*.nupkg' -printf '%f\n' | sort)
[ "${#PACKAGES[@]}" -gt 0 ] || fail "no *.nupkg file in '$ARTIFACTS_DIR'; nothing was packed."

waits_spent() {
  awk -v p="$1" '$1 == p { print $2; found = 1 } END { if (!found) print 0 }' "$WAIT_FILE"
}

record_waits() {
  local tmp="$WAIT_FILE.tmp"
  awk -v p="$1" -v n="$2" '$1 == p { next } { print } END { print p " " n }' "$WAIT_FILE" > "$tmp" \
    && mv "$tmp" "$WAIT_FILE"
}

key_is_old() {
  [ $(($(date +%s) - ROUND_START)) -ge "$KEY_MAX_AGE_SECONDS" ]
}

pending=0
for package in "${PACKAGES[@]}"; do
  grep -q -F -x -e "$package" "$DONE_FILE" || pending=$((pending + 1))
done
echo "push-nuget: ${#PACKAGES[@]} package(s), $pending still to push."

for package in "${PACKAGES[@]}"; do
  grep -q -F -x -e "$package" "$DONE_FILE" && continue

  while true; do
    if key_is_old; then
      echo "push-nuget: the sign-in key is at least ${KEY_MAX_AGE_SECONDS}s old; ending this round so the workflow signs in again before '$package'."
      echo "complete=false" >> "$GITHUB_OUTPUT"
      exit 0
    fi

    echo "push-nuget: pushing $package"
    output=$(dotnet nuget push "$ARTIFACTS_DIR/$package" \
      --source "$NUGET_SOURCE" \
      --api-key "$NUGET_API_KEY" \
      --skip-duplicate 2>&1)
    code=$?
    echo "$output"

    if [ "$code" -eq 0 ]; then
      echo "$package" >> "$DONE_FILE"
      break
    fi

    if ! grep -q -i -F "Quota Exceeded" <<< "$output"; then
      fail "pushing $package failed and it was not a quota refusal (exit code $code); nuget.org's reply is printed above. Packages already pushed stay on nuget.org and a rerun skips them."
    fi

    spent=$(waits_spent "$package")
    if [ "$spent" -ge "$QUOTA_MAX_WAITS" ]; then
      fail "pushing $package was refused for quota $((spent + 1)) times; QUOTA_MAX_WAITS is $QUOTA_MAX_WAITS and all of them are spent. Rerun once the quota has cleared, or raise NUGET_QUOTA_MAX_WAITS."
    fi

    stated=$(grep -o -i -E 'retry[ -]after:? *[0-9]+' <<< "$output" | grep -o -E '[0-9]+' | head -n 1)
    if [ -n "$stated" ]; then
      wait_seconds=$((stated + QUOTA_RETRY_AFTER_MARGIN_SECONDS))
      source_of_wait="the retry-after nuget.org stated ($stated s plus $QUOTA_RETRY_AFTER_MARGIN_SECONDS s margin)"
    else
      wait_seconds=$(awk -v base="$QUOTA_WAIT_SECONDS" -v f="$QUOTA_BACKOFF_FACTOR" -v n="$spent" \
        'BEGIN { w = base; for (i = 0; i < n; i++) w *= f; printf "%d", w }')
      source_of_wait="QUOTA_WAIT_SECONDS $QUOTA_WAIT_SECONDS grown by factor $QUOTA_BACKOFF_FACTOR for $spent earlier refusal(s), because nuget.org stated no retry-after"
    fi

    if [ $(($(date +%s) + wait_seconds)) -ge "$JOB_DEADLINE" ]; then
      fail "pushing $package was refused for quota and the wait of ${wait_seconds}s would pass the job's ${JOB_TIMEOUT_MINUTES}-minute timeout. Rerun the workflow; $(grep -c . "$DONE_FILE") package(s) are already pushed and are skipped."
    fi

    record_waits "$package" $((spent + 1))
    echo "push-nuget: quota refused $package; wait $((spent + 1)) of $QUOTA_MAX_WAITS, sleeping ${wait_seconds}s ($source_of_wait)."
    sleep "$wait_seconds"
  done
done

echo "push-nuget: all ${#PACKAGES[@]} package(s) are on nuget.org."
echo "complete=true" >> "$GITHUB_OUTPUT"
