# Claude-Led Codex Implementation Workflow — v3

> Status: Draft for review. Design and implementation plan only. Does not authorize
> installation, profile changes, commits, pushes, deployments, or external system changes.
>
> Changes from v2: the context layer is repo-scoped rather than worktree-scoped; the
> workflow is specified as a terminal command sequence rather than an abstract pipeline; a
> mechanical enforcement layer replaces several prompt-only prohibitions; intake lanes are
> restored; the learning layer is removed to a separate document.
>
> **Pilot repository: `pbir-design-analyzer`.** Phase 0 checked against the real repo and
> passes — see §12.0, including two existing conventions the harness had to be changed to
> respect.
>
> **This revision reuses Atomic Claude instead of installing Graphify, Socratic and
> Knowl** (§2.1). Those three are not installed anywhere on this machine, and the pilot
> repo already has a planning, review, evidence and context stack — plus a containment
> policy in `CLAUDE.local.md` that independently reaches §14's conclusion. What remains
> genuinely new is the Codex delegation contract, the plan-defect requirement, the
> mechanical gate, and the run ledger.
>
> **State as of 2026-09-09.** Harness + handoff template installed under `~/.agents`;
> `agentrun.sh init` run and verified; push guard verified against a real push; Codex plugin
> and CLI installed and authenticated with the review gate off. Tasks 1–4 of the
> decomposition program are **complete and verified on local `main` @ `7e8b66f6`**, pushed to
> `origin/codex/tasks1-4-main-merge-20260909` but **not yet on `origin/main`** (still
> `4c56eaf3`). Task 5 is the chosen pilot task and its preconditions are intact. **No plan,
> delegation, gate or review has been run yet.** §18 is the live checklist.

---

## 1. Goal

A reusable, terminal-driven workflow in which Claude Code coordinates, plans and reviews,
Codex implements and tests, and the boundary between them is enforced by scripts rather
than by instructions a model may or may not follow.

Everything runs from the shell in a working copy. No GUI step is required at any point.

### Non-goals for pilot #1

- Automatic deployment or Fabric control-plane mutation.
- Pushes to any shared remote.
- A learning or self-modification layer (see §14).
- Eigenwise or any model-routing change (see §15.2).
- More than one context engine.
- Any workflow change mid-pilot (see §9.2).

---

## 2. Architecture

Claude Code owns intake, context, planning, delegation, review and verification. Codex
owns implementation, tests and evidence. **Atomic Claude, already installed in this repo,
supplies the context, preflight and review layers** (§2.1). The correction budget is fixed
and the completion gate is a script.

```text
terminal
  └─ agentrun.sh start <slug>          → run id, worktree off origin/main, plan stub
      └─ claude  (in that worktree)
           ├─ /gather-evidence  (only if the task rests on a hunch)  §6.0
           ├─ /atomic-plan → tier = lane  (+ atomic.db context)     §4, §6.1, §11
           ├─ plan → docs/plans/<run-id>.md      §6.2
           ├─ /codex:adversarial-review on THE PLAN   ← the different model  §6.3
           ├─ user approval if the tier requires it
           ├─ /codex:rescue --wait  (implementation + tests)   §7
           ├─ agentrun.sh gate <run-id>          ← mechanical  §8.1
           ├─ atomic-reviewer (code-mode) → docs/reviews/<run-id>-review.md  §5.3, §8
           ├─ ≤1 correction pass, 🔴/🟡 only                    §9.1
           └─ agentrun.sh finish <run-id> <verdict> <attended-min>
```

The official Codex plugin is the delegation mechanism. It invokes the local Codex CLI on
the same machine and the same checkout, makes no commits of its own, and returns a
resumable session id. Claude must initiate the review itself — the plugin does not close
the loop, and its optional automatic review gate stays **off** (OpenAI's own README warns
it "can create a long-running Claude/Codex loop and may drain usage limits quickly").

### 2.1 Mapping onto Atomic Claude

`pbir-design-analyzer` already runs Atomic Claude as a repo-local layer, and its
`CLAUDE.local.md` already contains a containment policy — no auto-memory, no
retrospective/self-sharpening learning, no inter-session bus, no autopilot, no hook or MCP
registration, no governed-memory writes. That policy reaches the same conclusion §14 argues
for, independently. So the pilot **reuses Atomic rather than installing a parallel stack**:

| v3 concept | What actually runs | Not installing |
|---|---|---|
| Code-intel graph | `.claude/.atomic-index/atomic.db` via `atomic code explore` / `atomic code impact` | Graphify |
| Repo prose context | `docs/wiki/` signals, `/refresh-wiki`, `atomic-wiki-inferrer` | — |
| Triviality / lane classification | `/atomic-plan`'s tier table (§11) | my invented lanes |
| Preflight reasoning | `/atomic-plan`, `atomic-strategist` (Opus, read-only) | Socratic |
| Adversarial pass on approach | `/pressure-test`, `/challenge-swarm` | Socratic Full |
| Pre-design hypothesis check | `/gather-evidence` → SUPPORTED / UNSUPPORTED / MIXED / INCONCLUSIVE, cited | — |
| Spec review | `atomic-reviewer` **spec-mode** (see §6.3) | — |
| Diff review + severity ladder | `atomic-reviewer` code-mode, 🔴/🟡/🔵/❓ (§5.3) | my 5-tier ladder |
| Completion check | `atomic-verify` | — |
| Test discipline | `atomic-tdd`; the reviewer verifies signals were actually run | — |
| Commit discipline | `atomic-git-discipline`, `/commit`, `/git-cleanup` | — |
| Durable memory | the separate AI Memory system, manual only | Knowl |
| Learning loop | `/retrospective-learning`, which `CLAUDE.local.md` **disables** | Refine layer |

`atomic.db` is a real graph, not a summary layer: 36MB, queried with `atomic code explore`
and `atomic code impact`, and `atomic-strategist` already carries the grounding discipline
this document wrote by hand in §4.3 — *"one query per distinct claim, the graph corroborates
reasoning rather than replacing it."* What Graphify would still add is SQL-schema, config
and document coverage, which is why §12 revisits it before the Fabric repos and not before.

What v3 still contributes, and why the pilot is worth running at all:

1. **The Codex delegation contract** (§7) — Atomic has an `atomic-implementer`, but nothing
   that hands a bounded package to a *second model* and takes evidence back.
2. **A different model on the plan** (§6.3, §7.3). Atomic's reviewer *does* have a
   spec-mode that gates a draft spec against its design doc — so the plan is reviewed. It is
   reviewed by Claude, reviewing Claude's plan. What `/codex:adversarial-review` and the
   plan-defect section add is not review; it is an outside model. That is the single
   clearest thing this pilot tests.
3. **The mechanical gate** (§8.1) — scope drift and evidence presence as exit codes rather
   than as agent judgement.
4. **Run identity and the ledger** (§13) — comparable measurements across runs.

Everything else in this document that names Graphify, Socratic or Knowl should be read as
naming its Atomic counterpart above. Three skills that are not installed anywhere on this
machine are not a dependency the pilot needs.

**Resolved 2026-09-09:** `CLAUDE.local.md`'s hooks prohibition now names Claude Code
lifecycle hooks, MCP, PostgreSQL, AI Memory and governed-memory writes, and states that git
hooks are permitted project tooling — which is what the line already had to mean, since the
repo ships `.githooks/pre-commit` and `pre-push` via `core.hooksPath`. `init` has been run.
See §15.8.

---

## 3. Terminal operating model

### 3.1 One-time setup, per repository

Done for `pbir-design-analyzer`: `agentrun.sh` at `~/.agents/bin/`, `handoff.tmpl` at
`~/.agents/`, and `agentrun.sh init` run — repo identity pinned, `extensions.worktreeConfig`
on, pilot guard generated with `.githooks` as its chain target, `docs/{plans,reviews,
decisions}` created, repo registered. `init` modified no tracked file.

Still yours, once per machine and once per repo:

```bash
export PATH="$HOME/.agents/bin:$PATH"                      # §3.3

cd ~/Documents/GitHub/pbir-design-analyzer
(cd vscode-extension && npm ci)       # node_modules is currently absent
(cd vscode-extension && npm test)     # green baseline BEFORE run one
dotnet test service-dotnet/tests/Tests.csproj -c Release   # ditto
```

For a **new** repo later, `agentrun.sh init` is the whole per-repo setup.

Then, once, by hand, in the **main** worktree (§4.1) — the index is dated Aug 18 against an
Aug 21 mainline, so this is cheap insurance rather than urgent:

```bash
claude
  > /refresh-wiki                     # rebuild atomic.db + docs/wiki signals
```

`agentrun.sh start` symlinks each pilot worktree's `.claude/.atomic-index` to this one
(§4.1). Never run `/refresh-wiki` from inside a pilot worktree — through the symlink it
would rewrite the shared index from pilot-branch state.

### 3.2 Per-run sequence

```bash
# 1. isolate — base defaults to origin/main, not the checked-out branch
agentrun.sh start signal-enrichment
#    → run id 20260909-signal-enrichment
#    → worktree .worktrees/run-20260909-signal-enrichment on branch pilot/<run-id>
#    → base printed explicitly; a base ref can be passed as the 2nd argument
#    → plan stub with machine-readable frontmatter

cd .worktrees/run-20260909-signal-enrichment

# 2. plan  (inside claude)
claude
  > /gather-evidence "<hunch>"        # ONLY if the task rests on an unverified assumption
  >                                   # returns SUPPORTED/UNSUPPORTED/MIXED/INCONCLUSIVE
  > /atomic-plan <the task>           # classifies the tier → that IS the lane (§11)
  > <optionally /pressure-test or /challenge-swarm for non-trivial work>
  > write docs/plans/20260909-signal-enrichment.md, including scope_files
  > /codex:adversarial-review docs/plans/20260909-signal-enrichment.md

# 3. delegate  (inside the same claude session)
  > !agentrun.sh handoff 20260909-signal-enrichment    # renders from the plan
  > /codex:rescue --wait "<paste the rendered handoff>"

# 4. gate, then review
  > !agentrun.sh gate 20260909-signal-enrichment      # exit code decides, not judgement
  > <atomic-reviewer code-mode, per the §5.3 verdict + severity mapping>
  > <atomic-verify for the completion check>
  > <write docs/reviews/20260909-signal-enrichment-review.md>

# 5. close
agentrun.sh finish 20260909-signal-enrichment APPROVED_WITH_NOTES 11 "2 medium findings"
#                                             verdict ─────────────┘  │  └── notes
#                                             attended minutes (8b) ──┘
cd ../../ && agentrun.sh clean 20260909-signal-enrichment   # branch is kept
```

### 3.3 Shell helpers

Add to `~/.zshrc`, alongside the existing `cc()` function:

```zsh
export PATH="$HOME/.agents/bin:$PATH"        # agentrun.sh is already installed here
# State defaults to ~/.agents/state/agentrun — no export needed. Override only
# if you deliberately want it elsewhere; `start` bakes an absolute hooks path
# into per-worktree git config, so the value must be stable per machine.

# start a run and drop into it
axs() {
  local out; out="$(agentrun.sh start "$1")" || return 1
  printf '%s\n' "$out"
  cd "$(printf '%s' "$out" | awk '/^ok    worktree/{print $3}')" || return 1
}

# the run id of the worktree you are standing in
axid() { git symbolic-ref --short HEAD | sed 's|^pilot/||'; }

axg() { agentrun.sh gate    "$(axid)"; }
axf() { agentrun.sh finish  "$(axid)" "$@"; }
```

### 3.4 Why the filesystem is the bus

The plan, the Codex report and the review are **files on the pilot branch**, not prompt
strings. A plan passed as prompt text gets paraphrased, truncated or lost to a compaction
event; a plan passed as a path is read by whoever needs it and is still there afterwards.
The same applies in reverse: `agentrun.sh evidence` fails the run if Codex returned a
narrative instead of writing `docs/reviews/<run-id>-codex.md`.

This also makes the audit trail and the measurements in §13 fall out for free, because
every artifact of a run shares that run's id.

---

## 4. Repository context layer (`.atomic-index`)

### 4.1 A fresh worktree has NO index, and nothing says so

`.claude/.atomic-index/` holds `atomic.db` — 36MB as of 2026-09-09 — and it is **gitignored**
(root `.gitignore` line 29). So a new worktree does not get a stale copy of the index. It
gets **nothing**. And `atomic-strategist` is specified to "degrade silently to Grep/Glob when
the binary is absent or a query fails."

Put together: a pilot run in a fresh worktree loses code intelligence entirely, says nothing
about it, and criterion 10 fails open. This is worse than the staleness problem an earlier
draft of this document described, and it is the single most likely way run one produces a
misleading result.

**Fix — symlink the pilot worktree's index to the main worktree's:**

```bash
ln -s <main-worktree>/.claude/.atomic-index <pilot-worktree>/.claude/.atomic-index
```

`agentrun.sh start` does this automatically when the main worktree has an index, the pilot
path is free, and the path is gitignored; it reports which of those it did.

One subtlety, found by testing rather than by reading: the ignore rule is
`.claude/.atomic-index/` — **directory-only** — so it does not match the *symlink* at that
path, which therefore shows up as an untracked file. Left alone, every run would report
scope drift on a path the harness itself created. `agentrun.sh scope` excludes the shared
index paths explicitly for that reason. Do not "fix" this by dropping the trailing slash in
`.gitignore`: that file is managed by atomic repo init.

Rebuilding the index per run is the alternative and it is the wrong one: 36MB per run, and
it would index a diverged, half-finished pilot branch.

**Refresh only in the main worktree**, against the mainline. Never `/refresh-wiki` from
inside a pilot worktree — through the symlink that rewrites the *shared* index from
pilot-branch state, poisoning every later run. This is verified behaviour, not a
theoretical risk: writing through the link in a test repo changed what the main worktree
saw. The symlink buys the index's presence at the cost of making a careless refresh
destructive, and that trade is worth taking only because the alternative — no index and no
warning — corrupts the pilot's conclusions instead of its inputs.

### 4.2 Freshness

The index is a build artifact with a timestamp, so freshness is a manual check in the main
worktree before `start`:

| Condition | Action |
|---|---|
| `atomic.db` older than `origin/main`'s tip | `/refresh-wiki` before starting |
| `atomic.db` absent | build it in the main worktree; do not start a run without it |
| `docs/wiki/` signals stale or absent | `/setup-wiki` or `/refresh-wiki` |
| Refreshed while a pilot worktree exists | the symlinked worktree sees the new index mid-run — note it in the review |

**As of 2026-09-09:** `atomic.db` is dated Aug 18 and `origin/main`'s tip is Aug 21 — so
the index is a few days behind the pilot base, not badly stale. (An earlier draft said three
weeks; that compared against the `codex/` branch at Aug 28, not against main.) Refresh
before run zero anyway: it is cheap and it removes a variable.

`agentrun.sh graph-status` and `graph-path` remain in the harness for the Graphify path if a
future repo uses it. They are inert here and should not be run as part of this pilot.

### 4.3 The trust rule

The index reflects one commit. It does not know about uncommitted work, and it does not know
about anything Codex just changed. Therefore:

> Structural questions about untouched areas of the repo may be answered from the index.
> Any file in the plan's `scope_files`, and any file in Codex's changed list, must be read
> directly during review.

A confidently stale structural claim in a review is worse than having no index at all. This
rule is the difference between the two, and it is criterion 10 — which is why criterion 10
now also requires proving the index was *present*, not merely that it was consulted.

`atomic-strategist` already states the same discipline from the other direction: the graph
"corroborates reasoning rather than replacing it." Nothing here contradicts that; §4.3 just
adds which files are off-limits to corroboration.

### 4.4 Multi-repo expansion

`agentrun.sh init` appends to `~/.agents/state/agentrun/registry.tsv`:

```text
<repo-slug>	<main worktree path>	<graph dir>
```

Expansion to a second repository is `agentrun.sh init` in it and nothing else. Whether that
repo's context layer is Atomic, Graphify, or neither is a per-repo decision — the harness
does not care, and no context is shared across repositories. Repository isolation is the
point.

---

## 5. Agent responsibilities

### 5.1 Claude — coordinator

- Classify the request into a lane (§11) and say which lane out loud.
- Verify repository root, branch, worktree state, and repo instructions
  (`AGENTS.md`, `AGENT.md`, `CLAUDE.md`, `.github/`) before anything else.
- Use `.atomic-index` before broad repeated searching; honour §4.3.
- Run `/atomic-plan` by default; add `/pressure-test` or `/challenge-swarm` for the
  full-lane triggers in §6.1.
- Escalate only consequential questions.
- Write the plan to `docs/plans/<run-id>.md` with machine-readable `scope_files`.
- Submit the plan to `/codex:adversarial-review` before delegating it.
- Delegate only the approved scope.
- Run `agentrun.sh gate` **before** reviewing; never review an ungated run.
- Read the actual diff. Never claim completion from Codex's narrative.
- Hold the correction budget in §9.1.

### 5.2 Codex — implementer

Must:

- Implement only the approved plan, read from its path.
- Write or update tests. Run every command in `required_commands`.
- Make WIP commits inside the pilot worktree (§10.2).
- Write the report to `docs/reviews/<run-id>-codex.md` in the §7.2 format.
- Report **plan defects** — see §7.3, this is a required section.

Must not:

- Touch a file outside `scope_files`.
- Push, deploy, or mutate any external Fabric or Dataverse resource.
- Claim success without command output.

### 5.3 Claude — reviewer

Run through `atomic-reviewer` in code-mode (`/review-branch` for a whole branch). It already
reviews the diff against the spec, verifies TDD signals were actually run, emits
`path:line: <emoji> severity: problem. fix.`, and returns a VERDICT. Citations and evidence
verification are therefore **already required** — two of the things earlier drafts of this
document asked for.

**Use Atomic's severity ladder. Do not add a parallel one.**

| Emoji | Atomic tier | Spends the correction pass? |
|---|---|---|
| 🔴 | bug — wrong output, crash, security hole, data loss, missing TDD | **yes** |
| 🟡 | risk — edge case, race, leak, perf cliff, missing guard, weak test | **yes** |
| 🔵 | nit — style, naming, micro-perf (emitted with a confidence level) | no |
| ❓ | question — needs author intent before judging | no — escalate or resolve |

Earlier drafts imposed a five-tier Critical/High/Medium/Low/Informational ladder on top of
this. That was the parallel stack §2.1 warns against, built by this document. 🔴/🟡 are the
correction-pass tiers; 🔵 is recorded and not corrected.

**Verdicts.** Atomic returns PASS / CHANGES_REQUESTED. This workflow needs two more states,
so map as:

| Verdict | Atomic equivalent | Meaning |
|---|---|---|
| `APPROVED` | PASS, no findings | Nothing outstanding. |
| `APPROVED_WITH_NOTES` | PASS with 🔵 findings | Ship it; nits recorded, not corrected. |
| `CHANGES_REQUIRED` | CHANGES_REQUESTED | ≥1 🔴 or 🟡. Spends the correction pass. |
| `BLOCKED_NEEDS_USER_DECISION` | — (new) | A decision only Barry can make. Stop (§9.3). |

`APPROVED_WITH_NOTES` is the one genuinely load-bearing addition: PASS/CHANGES_REQUESTED
alone routes every imperfect diff into a Codex round trip, which is the realistic path to
token exhaustion.

**The one requirement Atomic does not already impose:** every 🔴 or 🟡 must state a concrete
failure scenario — inputs or state, and the resulting wrong output or crash. No scenario
demotes it to 🔵. This is the defence against a reviewer manufacturing findings to look
useful on a correct diff, and it is what keeps the correction budget spent on defects.

---

## 6. Planning

### 6.0 Evidence before design (`/gather-evidence`)

Only when the task rests on an unverified assumption — "the library supports this", "we
already have a pattern for this", "approach A is faster". `/gather-evidence` chases it
through primary sources and returns SUPPORTED / UNSUPPORTED / MIXED / INCONCLUSIVE with a
cited trail.

It **precedes** `/atomic-plan` by design, and an earlier draft of this document had it in
the wrong place — after the gate, as if it were the post-implementation evidence step. That
role belongs to `atomic-verify`. Getting this backwards wastes a planning session on an
unverified premise, which is exactly what the command exists to prevent.

Skip it when the task has no hypothesis in it. Most bounded signal-enrichment work will.

### 6.1 Preflight (`/atomic-plan`)

`/atomic-plan` by default; its tier classification **is** the lane selector (§11). Add
`/pressure-test` or `/challenge-swarm` for: production, credentials, security, customer
data, migration, deployment, irreversible or costly actions. Dispatch `atomic-strategist`
when the question is "is this the right approach?" rather than "is this correct?".
Interactive questioning only where user authority is genuinely required.

`/atomic-plan` emits a design doc plus a checkpoint-table spec. That is a different shape
from the eight fields below, and its shape is the better one for implementation — so treat
this list as a **completeness checklist against the spec**, not as a replacement format.
§6.2's frontmatter and §7.1's handoff consume these, so the spec must let them be filled:

```text
Objective:
Scope:
Relevant repository areas:
Assumptions:
Open questions:
Top risks:
Required tests:
Proposed implementation:
```

### 6.2 The plan file

`docs/plans/<run-id>.md`, frontmatter first because the harness parses it:

```yaml
---
run_id: 20260909-signal-enrichment
base_commit: <sha>
lane: full                    # direct | delegated-light | full
scope_files:
  - src/analyzer/signals/density.ts
  - test/signals/density.test.ts
allow_new_files: false
required_commands:
  - npm test -- signals
---
```

Then: objective, scope boundaries, assumptions, open questions, top risks, verification
evidence required, rollback.

`scope_files` is not documentation. It is the input to `agentrun.sh scope`, which fails
the run on any file changed outside the list.

### 6.3 Adversarial review of the plan — before implementation

```bash
/codex:adversarial-review docs/plans/<run-id>.md
```

**Correction to earlier drafts:** the plan is *not* unreviewed without this step.
`atomic-reviewer` has a **spec-mode** that gates a draft spec against its design doc for
coverage, verifiable success criteria, checkpoint cohesion and over-prescription — and
`/atomic-plan`'s own spec loop already dispatches it.

So what this step adds is not review. It is a **different model**. Spec-mode is Claude
reviewing Claude's plan, which is exactly the structural blindness that matters: a plan
whose flaw is invisible to the reasoning that produced it stays invisible to a second pass
of the same reasoning. A faithful implementation of a wrong plan is approved every time.

Run spec-mode *and* this. They are not substitutes, and this is the single clearest thing
the pilot tests — if `/codex:adversarial-review` and the §7.3 plan-defect section never
surface anything spec-mode missed, the cross-model check is not earning its cost, and
criterion 4 is how you find out.

Its output is recorded in the plan under `## Pre-implementation challenge` with Claude's
response to each point. Disagreeing is fine; ignoring is not.

---

## 7. Handoff and return

### 7.1 Handoff

```bash
agentrun.sh handoff <run-id> | pbcopy      # or read it, then paste into /codex:rescue
```

`agentrun.sh handoff` renders the whole package from the plan's own frontmatter —
worktree, branch, base commit, plan path, `scope_files` verbatim, `allow_new_files`,
`required_commands`, and the report path. It **refuses** to render when `scope_files` is
empty.

Do not assemble this by hand. Transcribing the scope list or the command list per run is
exactly where a path gets paraphrased or a command gets dropped, and the gate then fails
the run for it. The template is at `~/.agents/handoff.tmpl` and is yours to edit; the
tokens it substitutes are `@@RUN_ID@@`, `@@WORKTREE@@`, `@@BRANCH@@`, `@@BASE@@`,
`@@PLAN@@`, `@@SCOPE_FILES@@`, `@@NEW_FILES@@`, `@@REQUIRED_COMMANDS@@`, `@@REPORT@@`.

What it hands Codex:

- Worktree path, pilot branch, base commit.
- **The path** to the approved plan — not its contents.
- The `scope_files` list, verbatim, as a hard boundary.
- `required_commands`.
- Prohibited actions (§5.2), including refreshing the shared context index (§4.1).
- The required report path and format.

### 7.2 Required report format

Codex writes `docs/reviews/<run-id>-codex.md`. `agentrun.sh evidence` fails the run if any
heading is missing or if a `required_commands` entry does not appear in the file:

```markdown
## Status
## Files changed
## Commands run
## Test results
## Plan defects
## Assumptions
## Unresolved risks
## Recommended review focus
```

### 7.3 Plan defects — the section that matters most

> Where was the approved plan wrong, underspecified, or contradicted by the code you read?

Codex is the only agent that read the problem fresh while implementing it. It is the
cheapest available detector of a bad plan, and v1 and v2 both threw that away. Claude must
answer each entry on the record in its review — accepted, rejected with reason, or
escalated — and an accepted plan defect is a **success signal** for the pilot (§12), not a
failure.

---

## 8. Review

### 8.1 The gate runs first, and it is a script

```bash
agentrun.sh gate <run-id>
```

- **scope** — every file changed since `base_commit` (committed, uncommitted and
  untracked, `docs/` excluded) must appear in `scope_files`. Any other file fails.
- **evidence** — the Codex report must exist, be non-empty, carry all required headings,
  and mention every `required_commands` entry.

Non-zero exit means no review and no completion claim. Scope drift is a git question, not
a judgement call, and it should not cost a model call to answer.

### 8.2 The review file

`docs/reviews/<run-id>-review.md`: verdict, findings by severity with file and line
citations and a failure scenario each, the response to every Codex plan defect, and the
evidence relied on. v2 saved the plan but saved no review; half an audit trail is not one.

---

## 9. Loop, cost and freeze controls

### 9.1 Budget

- One `/atomic-plan` preflight.
- One adversarial review of the plan.
- One Codex implementation pass.
- One `agentrun.sh gate`.
- One Claude review.
- One Codex correction pass — **only** on 🔴/🟡 findings (§5.3).
- One Claude final review.

No automatic retry. No automatic review gate. Anything beyond this needs explicit
approval, per run, stated in the chat.

### 9.2 Freeze the workflow during the pilot

**Zero workflow changes during a pilot series.** v2 allowed three per week; any change
invalidates every measurement taken before it, and the pilot exists to characterise
baseline behaviour. Collect ten runs on an unchanged workflow, then change it between
series. Improvement ideas go in a scratch list, not into the workflow.

### 9.3 Cost ceiling and the stop rules

These are **abort triggers with a ratchet**, not predictions. Generous for the first three
runs, tightened once there is data. A ceiling that fires on run one because the number was
invented teaches nothing.

One number for all lanes would be useless — a delegated-light run should cost a fraction
of a full one. Claude side, read off `/cost`:

| Lane | Abort trigger, runs 1–3 | Expected | Tighten to after run 3 |
|---|---|---|---|
| direct | $2 | cents | $1 |
| delegated-light | $6 | $0.50–1.50 | $3 |
| full | $15 | $2–5 | $8 |

Basis for the full-lane estimate: intake plus this repo's `AGENTS.md` and `CLAUDE.local.md`
~15k, `.atomic-index` context ~10k, `/atomic-plan` preflight ~25k, plan ~15k, report plus a ~150-line diff ~25k, the review
~50k, correction handoff and re-review ~35k — call it 150–250k Claude-side tokens, less in
practice because repo context caches across the session. The adversarial plan review and
the implementation land on **Codex's** budget, not Claude's.

**Verify the instrument in Phase 0.** If `/cost` reports a notional figure rather than
dollars on the company seat, substitute whatever it does report and keep the 1 : 3 : 7.5
ratio between lanes.

Codex side, cap by **sessions, not tokens**: two per run — one implementation, one
correction. That is already the §9.1 budget; `/codex:status` makes it observable, which is
what makes it enforceable.

#### The compaction trip-wire

> **If the Claude session compacts mid-run, abort the run and restart from the plan file.**

This is a correctness rule, not a cost rule. §3.4 and §7.1 both depend on the plan
surviving verbatim, and a compaction event is precisely the thing that silently
paraphrases it away. Free to observe, and likelier to catch a real failure than the dollar
ceiling is.

#### Stop rules

Exceeding a ceiling, or compacting mid-run, aborts the run and writes
`BLOCKED_NEEDS_USER_DECISION`.

`BLOCKED_NEEDS_USER_DECISION` default with no answer: **stop, write state to
`docs/reviews/<run-id>-review.md`, guess nothing.** This workflow runs while Barry is
doing other things; the alternative is an agent picking a path unattended.

---

## 10. Enforcement: mechanical vs. prompt-only

Being explicit about which is which, because v2 listed both together and called them
controls.

### 10.1 Mechanical

| Control | Mechanism |
|---|---|
| Worktree isolation | `git worktree add -b pilot/<run-id>` into `.worktrees/` |
| No pushing pilot work | generated `pre-push`, armed per-worktree via `core.hooksPath` |
| Scope drift | `agentrun.sh scope` against `scope_files` |
| Evidence present | `agentrun.sh evidence` against the report file |
| Run identity | run id in every artifact path |
| Fabric mutation | credentials absent from the launching shell (§10.3) |

The pre-push guard is generated into `~/.agents/state/agentrun/<repo-slug>/hooks/` and armed
by `agentrun.sh start` with `git config --worktree core.hooksPath` — so it applies to the
pilot worktree Codex works in, and nothing else. The main worktree keeps its own hooks
untouched, and for any non-pilot branch the guard replays stdin and chains through to the
repository's own `pre-push`, propagating its exit status.

Two details that were bugs before testing caught them:

- It checks the resolved current branch, `local_ref` **and** `remote_ref`. `git push
  origin HEAD` reports `local_ref` as `HEAD`, so a guard checking only `local_ref` waves
  pilot branches straight through.
- It does **not** write into `.git/hooks`. A repo that sets `core.hooksPath` — as
  `pbir-design-analyzer` does — makes git ignore `.git/hooks` entirely, so a guard
  installed there reports success and never runs.

**Documented limitation:** the guard is name-based. Renaming the branch defeats it. It is
a reliable safety net against accident, not a sandbox against a determined agent — do not
treat it as containment.

### 10.2 Commits: inverted from v2

v2 prohibited commits. Commits are local and cheap; **pushes** are the irreversible thing.
Prohibiting commits leaves a failed run as a dirty worktree with no revert point — in a
plan whose §6.2 requires a rollback story.

So: **Codex commits freely inside the pilot worktree.** Those WIP commits *are* the
rollback mechanism (`git reset --hard <base_commit>` restores the start state exactly).
Pushing is blocked mechanically. On acceptance, squash the pilot branch into a real branch
from the main worktree, where the guard does not apply.

`agentrun.sh clean` removes the worktree and **keeps the branch**, so a rejected run stays
inspectable.

### 10.3 Credentials

Fabric and Dataverse credentials must not be in the environment of the shell that launches
`claude`, because the plugin invokes Codex as a child process and it inherits that
environment. Practically: keep them out of `~/.zshrc`, put them in a file sourced only by
explicit deploy scripts, and launch pilot sessions from a clean shell. Verify whether
`.codex/config.toml` supports an environment allowlist; until that is verified, the shell
is the control. `.codex/hooks.json` stays absent until verified — v2 flagged it as "only
when verified," which meant the enforcement layer was the unverified part.

---

## 11. Intake lanes

`/atomic-plan` already classifies triviality, with better-specified triggers than earlier
drafts of this document invented. **Use its tiers as the lane selector** rather than a
parallel scheme:

| `atomic-plan` tier | Its signals | This workflow's lane |
|---|---|---|
| **Trivial** | one cohesive slice; ≤3 files; no architectural choice; no public-contract change; one obvious approach | **direct** — Claude does it. No Codex, no run id, no plan file. |
| **Borderline** | 4–8 files; unfamiliar code; small architectural or business-rule choice; partial unknowns | **delegated-light** — run id + worktree, inline spec, Codex implements, `gate`, diff review. No adversarial plan review. |
| **Non-trivial** *(its default)* | cross-system; new module; multiple viable approaches; touches contracts; >8 files | **full** — everything in §3.2. |

Atomic's Borderline tier already asks the user to pick `full / inline / abort` and surfaces
the signals it weighed. That *is* the lane confirmation, so do not add a second one.

Two overrides on top of its tiers:

- Security, credentials, customer data, migration, deployment or any external effect forces
  **full**, whatever the file count.
- Atomic's own bias — "under-planning a non-trivial feature costs more than over-planning a
  trivial one" — is the right default. Do not tune it during a frozen pilot.

Claude states the tier, the lane it maps to, and the reason before proceeding. Barry can
override in either direction with one word.

---

## 12. Pilot plan

### Phase 0.0 — what the pilot repo actually contains (checked 2026-09-09)

`pbir-design-analyzer` passes the hard stop. It has real offline validation, documented
in its own `AGENTS.md`:

| Command | Scope |
|---|---|
| `cd vscode-extension && npm test` | `jest && jest -c jest.webview.config.cjs` — extension + webview suites |
| `cd vscode-extension && npm run compile` | `tsc -p ./` |
| `cd vscode-extension && npm run lint` | ESLint on `src/**/*.ts` |
| `dotnet test service-dotnet/tests/Tests.csproj -c Release` | xUnit backend suite |
| `npm run validate:contract` | score-panel contract generate + check |
| `npm run validate:protocol-contracts` | host/webview protocol boundary |
| `npm run validate:release-contract`, `validate:docs` | release + docs invariants |

Those last three are static oracles — exactly the kind of validation a Fabric-shaped task
could not have offered. The repo's own `AGENTS.md` already says "add or update tests with
every behavior change" and "if a fix cannot be validated, record that explicitly," which
means the evidence requirement in §8 is enforcing a norm the repo already states rather
than one this workflow invented.

The `PBIR_REAL_FIXTURE_PATH=... dotnet test --filter Category=PBITesting` suite is the
opt-in real-corpus coverage. That is the Phase-1/Phase-2 validation gate — keep it out of
`required_commands` for routine runs and require it for anything touching scoring
behaviour.

**`required_commands` for this repo must start with a build.** `npm test`'s `pretest` runs
only `compile` and `bundle:extension`; it does **not** run `build:webview`, and
`webview-src/design-studio/__tests__/bundleRuntime.test.ts` reads
`webview-dist/design-studio.js` from disk. In a fresh worktree that file does not exist and
the suite reports one failure with nothing wrong with the code. AGENTS.md line 89 names
`npm run build` as the command that "builds both webviews."

So the canonical verify sequence in a pilot worktree is:

```bash
cd vscode-extension
npm ci
npm run build:webview
npm test
npm run lint
cd ..
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

Put `npm run build:webview` ahead of `npm test` in every plan's `required_commands`, or
Codex will report a failure it did not cause and `agentrun.sh evidence` will happily pass
the run through with a red suite in the report.

Worth fixing in the repo separately, and it is a 🟡 by your own reviewer's ladder: a test
that depends on undeclared build state. Either add `build:webview` to `pretest`, or make
that test skip when the bundle is absent.

**Prerequisite:** `node_modules` is gitignored, so every fresh worktree needs `npm ci`
(≈4s here).

**⚠ STALE — re-measure per §18 step 4.** This table was taken on `4c56eaf3`, before Tasks
1–4 landed. Tasks 1–4 added the scoring-stage seam, the read-only analysis context, the
baseline script and new tests, so every count below has moved. Kept only to show the shape
of the table and the two interpretation notes underneath it.

**Baseline measured 2026-09-09 — `origin/main` @ `4c56eaf3`, in a clean pilot worktree:**

| Check | Result |
|---|---|
| `npm test` (extension) | 100 suites, 532 passed, 0 failed |
| `npm test` (webview) | 11 suites, 68 passed, 0 failed — including `bundleRuntime` once `build:webview` ran |
| `npm run lint` | clean |
| `dotnet test -c Release` | 1045 total, 1034 passed, 0 failed, 11 skipped |
| .NET build warnings | 30, pre-existing (nullable reference) |

Record these; they are what makes a run's report attributable. The 11 skips are
`Phase35IWindowsIntegrationTests` ("Windows OS is required") and are expected on macOS — a
report showing 0 skips means something else changed. The 30 warnings are a free extra check:
a diff that raises the count introduced them. Note the earlier 1047/1036 figure came from the
`codex/` branch, which carries two more tests than main; **1045/1034 is the pilot baseline.**

This table lives in the repo as `docs/decisions/pilot-verify-and-review-contract.md`
alongside the review contract, so the reviewer and the implementer can be pointed at it.

#### Two existing conventions the harness now respects

1. **`core.hooksPath = .githooks`, with a project `pre-push` already installed**
   (`exec node scripts/pre-push.mjs`, which runs `dotnet test` and `npm test` on changed
   paths). Writing a guard into `.git/hooks/pre-push` would have been installed and then
   **silently ignored by git** — the push protection would have looked armed and done
   nothing. The harness now generates its own hooks directory and points *only pilot
   worktrees* at it via per-worktree `core.hooksPath`, chaining through to
   `.githooks/pre-push` for non-pilot branches. Your main worktree is never modified, and
   your project hook still runs on ordinary pushes.

2. **`.worktrees/` is already the convention** (gitignored, currently holding
   `architecture-post-v1-decomposition`). Runs now land in `.worktrees/run-<run-id>`
   instead of a sibling directory.

Two more observations, neither blocking:

- The branch prefix stays `pilot/`, and it must **not** be `codex/`: this repo already
  uses `codex/*` branches and pushes them to origin (`origin/codex/hosted-v1-readiness-…`).
  A guard on `codex/*` would break a workflow you already rely on.
- `git worktree list` shows three prunable entries, two under `/private/tmp`. Worth a
  `git worktree prune` before the pilot so the run inventory starts clean.
- `extensions.worktreeConfig` is already `true` here, which is what makes the per-worktree
  hooks path possible. `agentrun.sh init` is idempotent about it.

### Phase 0 — authority and baseline, with a hard stop

- Confirm Claude Code and Codex auth, and available limits.
- `agentrun.sh init`; record branch, commit and `git status`.
- Build the shared graph from the main worktree; record `.base_commit`.
- Enumerate the test and validation commands that run **offline**.

> **Hard stop.** If there is no command that produces an offline pass/fail result, pilot
> #1 does not run in this repository. The completion gate requires test evidence; a
> repository that cannot produce it will yield either a false `APPROVED` or a run that
> cannot legally finish. v2 made "confirm available tests" a step with no branch for the
> answer *no*.
>
> **And the baseline must be green in a FRESH WORKTREE, not in the main one.** Verified
> 2026-09-09 the hard way: `npm test` passes in the main worktree and fails in a new
> worktree, because `vscode-extension/webview-dist/` is gitignored build output that
> `pretest` does not produce. The main worktree had it left over from earlier work. A
> baseline taken there is a false green, and run one would have opened with a failing test
> that Codex did not cause — the exact false signal that makes a pilot unreadable. Every
> repo gets this check before its first run.

Resolved for pilot #1: `pbir-design-analyzer` passes (§12.0). When this workflow later
reaches `sbccs-fabric`, the same stop applies — and if the answer there is *no*, scope the
task so validation is purely static: SQL parse, notebook lint, schema diff against a
checked-in snapshot.

**Delete "where practical" from the test requirement.** A required test list with an
escape clause is not a gate.

#### Rollout order across the 21 repositories

Do not `init` them all. One repo at a time, in increasing blast radius:

1. `pbir-design-analyzer` — pilot, ten runs, workflow frozen.
2. One more of your own low-risk tools (`toggl-api-client` or `openconnect-manager`) to
   prove the second `init` costs nothing and that the registry holds up.
3. Fabric/Power BI repos (`sbccs-fabric`, `AlphaFabric`, `Fabric-PowerBI-Workspace`) —
   these need the static-validation treatment and Fabric credentials kept out of the
   launching shell (§10.3).
4. Client-facing repos (`Shimadzu`, `SSICloudProject-sales`, `cmmc-assessment`) last, and
   only after the enforcement layer has a clean record. `cmmc-assessment` in particular
   should not be an early experiment.

`AI-Hermes` and `Consulting-AI-Memory` are the natural homes for the shared layer
(`agentrun.sh`, the handoff template, the review contract) — one versioned copy
installed to `~/.agents/bin`, which already exists on this machine. Per-repo, only the
repository-specialist skill and `docs/` stay local. That keeps 21 repos on one harness
instead of 21 copies drifting apart.

### Phase 1 — context

`/refresh-wiki` in the **main** worktree first (§4.1). Then, in the pilot worktree, **first confirm the index resolves**
(`ls -l .claude/.atomic-index` should show the symlink, and `atomic code explore` should
return rather than degrade). Only then put one real structural question to it. Note in the
review which files were answered from the index and which were read directly. Both halves
are criterion 10; a run where the index was silently absent proves nothing about it.

### Phase 2 — plan

`/atomic-plan`, expressed as the eight §6.1 fields. No Fabric pack — that belongs with the
Fabric repos in rollout stage 3, and adding it here would be a workflow change during a
frozen pilot. Write the plan with real `scope_files`. Run `/codex:adversarial-review` on it.
Escalate only material questions.

### Phase 3 — implementation

One bounded task via `/codex:rescue --wait`. Codex commits in the worktree, runs
`required_commands`, writes the report file.

### Phase 4 — gate and review

`agentrun.sh gate`, then `atomic-reviewer` code-mode, then at most one correction pass on
🔴/🟡 findings (§5.3).
Write the review file. `agentrun.sh finish`.

### Phase 5 — evaluate

No learning layer (§14). Append candidate lessons to a plain journal file — raw material
for a later design, nothing more.

---

## 13. Measurement and the run ledger

`agentrun.sh finish` appends one row to
`~/.agents/state/agentrun/<repo-slug>/ledger.tsv`:

```text
run_id  started  finished  base_commit  files_changed  insertions  deletions
correction_passes  attended_min  verdict  notes
```

`attended_min` is criterion 8b — your own time on the run — and is passed as the third
argument to `finish`. Ten rows make ten runs comparable without any learning machinery.
Record alongside each run in the notes field: `/cost` delta, Codex session count, whether
the graph was queried, and the count of accepted plan defects.

Row one should be the run-zero control (§16.1), logged as `00000000-control`.

**The context-layer claim needs a baseline.** Run zero (§16.1) is that baseline: doing the
task directly, without the loop, is also doing it without `/atomic-plan` ceremony. If you
want the narrower measurement — does `.atomic-index` reduce exploration — run the same
bounded task twice, once consulting the index and once forbidden from it. Do that at most
once, on the first pilot task, and never again.

---

## 14. Learning layer — removed to its own document

v2's Refine-style layer (capture, classification, recurrence gate, promotion, journaling,
rollback snapshots, effectiveness verdicts) is cut from this document entirely.

It cannot be evaluated by this pilot. Recurrence detection needs fingerprints across
distinct sessions; with one task — or ten — nothing recurs, and every effectiveness verdict
needs a *later* session to assign. Phase 6 of v2 would therefore have issued a verdict on
a system that never ran. It is also roughly as complex as the loop it attaches to, and it
was shaping decisions in a plan with zero runs behind it.

For now: append candidate lessons to a journal file after each run. No classification, no
promotion, no writes from the pilot's own machinery. Design the refinery after ten clean
runs have produced actual ore.

**Durable memory during pilot #1:** nothing automatic. `CLAUDE.local.md` already governs
this — Atomic context must not be promoted into the AI Memory system automatically, and
`/retrospective-learning` stays off. Anything worth keeping from a run is written by hand,
after the run, by you. Knowl is not installed and should not be for this pilot.

If a durable-memory layer is added later, judge it on *retrieval* — how many stored lessons
were actually read back and used in a later run — not on volume. Forty stored lessons and
zero retrievals means cut it, not tune it.

---

## 15. Open decisions

1. ~~**Pilot repository.**~~ Resolved: `pbir-design-analyzer`, Phase 0 verified (§12.0).
   Remaining sub-decision: which bounded task. Recommended — one signal in the Phase 1
   backend signal-enrichment work, `required_commands` set to `npm run compile`,
   `npm test` and `npm run lint`, with `validate:contract` added if the change touches the
   score-panel boundary. Nothing in `service-dotnet/` for run one: it doubles the
   toolchain surface for no extra learning.
2. **Model topology.** Recommended: Claude plans and reviews, official plugin delegates to
   Codex, no Eigenwise. Eigenwise is a model *router* — it swaps which model drives one
   session, so it cannot provide the second independent context this design depends on, and
   its documented caveats (no visible reasoning, a `[1m]` suffix workaround against
   auto-compaction that would silently paraphrase the plan, client-fingerprinting
   fragility) are actively harmful during a measured pilot. It is also a third-party local
   proxy terminating claude.ai session traffic, which deserves its own security look before
   it goes near a client repository. Revisit after ten clean runs of A.
3. ~~**Token ceiling.**~~ Resolved: per-lane triggers in §9.3, ratcheted after run 3.
   Remaining sub-decision — confirm in Phase 0 whether `/cost` reports dollars on the
   company seat; if not, substitute its unit and keep the 1 : 3 : 7.5 lane ratio.
4. ~~**Context engine.**~~ Resolved: `.atomic-index`, already in the repo. Graphify,
   Repowise and Serena all stay uninstalled for this pilot — one context engine, and it is
   the one that is already there.
5. ~~**Graphify's cache-directory flag.**~~ Moot for this pilot; revisit only if a future
   repo uses Graphify instead of Atomic.
6. **`.codex/config.toml` env allowlist.** Verify; until then §10.3's shell discipline is
   the control. Not inspectable from the Cowork session — `.codex` is not shared with it.
7. ~~**Repository artifact locations.**~~ Resolved: `docs/plans`, `docs/reviews` and
   `docs/decisions` under the existing `docs/` tree, which already holds specs, release
   notes and `docs/wiki/`. `AGENTS.md`, `CLAUDE.md`, `CLAUDE.local.md` and `.github/` are
   left untouched.
8. **The hook policy question — this one gates `init`.** `CLAUDE.local.md` says "do not
   install or register hooks, MCP servers, PostgreSQL connections, AI Memory integrations,
   or governed-memory writes." `agentrun.sh init` generates a `pre-push` and arms it via
   per-worktree `core.hooksPath`.

   The case that it is out of scope of that rule: it is git-native rather than an
   Atomic/Claude hook; it is scoped to pilot worktrees only; the main worktree's
   `core.hooksPath` is untouched; it chains to the existing `.githooks/pre-push` rather
   than replacing it; and its whole function is *refusing* an action, not enabling one.

   The case against: the rule as written says hooks, and a policy you wrote to keep an
   agent framework contained should not be reinterpreted by an agent for its own
   convenience. That is why the harness is installed but `init` has **not** been run.

   Three ways forward: (a) amend `CLAUDE.local.md` to permit a git-native, refusal-only,
   pilot-scoped hook and run `init`; (b) run the pilot with `AGENTRUN_ALLOW_UNGUARDED=1`
   and accept that push protection is your discipline rather than a mechanism — which
   loses criterion 2 as a mechanical check; (c) install the guard yourself, by hand, so
   the hook is a human action. **(a) is the recommendation, (c) is the conservative
   equivalent.**
9. ~~**Codex availability.**~~ Moved to §18 step 3 as an install task with exact commands.
   Not verifiable or installable from a Cowork session.

---

## 16. Acceptance criteria

Falsifiable, pre-registered. v2's "Claude asks useful questions" had no judge.

| # | Criterion | Check |
|---|---|---|
| 1 | No file changed outside `scope_files` | `agentrun.sh scope` exits 0 |
| 2 | No push of any pilot branch | remote has no `pilot/*` ref |
| 3 | Correction passes ≤ 1 | ledger `correction_passes` |
| 4 | Codex surfaced ≥1 plan defect Claude accepted | review file records it |
| 5 | Every defect-classed finding has a failure scenario | review file |
| 6 | Required commands evidenced | `agentrun.sh evidence` exits 0 |
| 7 | Run cost under the §9.3 lane ceiling | `/cost` delta |
| 8a | Wall clock — **informational for runs 1–3**, then ≤ 2.5× the run-zero control | ledger timestamps |
| 8b | **Attended minutes ≤ 15 (full lane) / ≤ 5 (delegated-light)** | stopwatch, logged in the ledger notes |
| 9 | Zero external Fabric/Dataverse mutations | Fabric activity log |
| 10 | Index **present** (symlink resolved), consulted at least once, and never trusted for a file in `scope_files` | review file + `ls -l .claude/.atomic-index` |
| 11 | No compaction event mid-run | §9.3 trip-wire |

### 16.1 Run zero — the control

Before run one, do the chosen task **yourself in Claude Code, directly** — no Codex, no
plan file, no ceremony — and record wall clock, attended minutes and `/cost`. Log it in the
ledger as `run_id 00000000-control`.

That single extra run is what makes criterion 8a and the third kill criterion decidable.
Without it, "wall clock under 75 minutes" is a number someone made up, and "costs more than
doing it directly" cannot be evaluated at all. My own estimate for a full-lane run in this
repo is 30–45 minutes clean and 45–75 with a correction pass — use that only as a sanity
check on the control, never as the threshold.

### 16.2 Why 8b is the criterion that matters

8a measures the workflow; 8b measures **your** time — answering escalated questions,
reviewing findings, approving the plan. Codex working for twenty minutes costs nothing if
you are doing something else, so wall clock alone cannot distinguish a run that finishes in
an hour untouched from one that finishes in twenty minutes after interrupting you six
times. The first is a win and the second is why processes get abandoned.

Process abandonment is the likelier failure here than token exhaustion, and 8b is the only
criterion that measures it. If 8b fails repeatedly, the fix is lane demotion or fewer
escalations — not a faster model.

**Kill criteria, decided now:** if after ten runs criterion 4 has never been met, the
cross-model check is not earning its cost. If criteria 1, 2 or 6 fail more than once, the
enforcement layer is inadequate and the pilot stops rather than expands. If 8b exceeds the
run-zero control's attended minutes, the workflow is costing you more attention than doing
the work yourself and gets abandoned — say so out loud rather than tuning it.

---

## 17. Risks

| Risk | Mitigation | Mechanical? |
|---|---|---|
| Premature implementation | `/atomic-plan` preflight + approval gate | no |
| Bad plan implemented faithfully | Adversarial plan review + plan-defect section | no |
| Reviewer inflates findings | Severity gate + failure-scenario requirement | no |
| Token runaway | Fixed budget, ceiling, no review gate, no learning calls | partly |
| Scope drift | `agentrun.sh scope` | **yes** |
| Completion claimed without evidence | `agentrun.sh evidence` | **yes** |
| Pilot work reaching a shared remote | `pre-push` guard (name-based; see §10.1) | **yes** |
| Lost work on a failed run | WIP commits in the worktree + branch kept | **yes** |
| Index silently absent in a worktree | `start` symlinks it; criterion 10 checks it resolved | **yes** |
| Stale index asserted as fact | §4.2 manual freshness check + §4.3 trust rule | no |
| Shared index poisoned through the symlink | never `/refresh-wiki` from a pilot worktree (§4.1) | no |
| Guard installed but inert | Hand-verified push attempt, §18 step 6 | no |
| Duplicate planning/review layers | Reuse Atomic, install nothing parallel (§2.1) | no |
| Repo identity drift orphaning graph/ledger | Slug pinned in `--local agentrun.slug` | **yes** |
| External Fabric mutation | Credentials absent from the launching shell | partly |
| Concurrent edits to one checkout | One worktree per run | **yes** |
| Measurements invalidated mid-pilot | Workflow freeze (§9.2) | no |
| Process abandoned as too heavy | Intake lanes (§11) + criterion 8b | no |
| Plan silently paraphrased by compaction | §9.3 trip-wire: abort, restart from the plan file | no |
| "Too slow" judged against no baseline | Run-zero control (§16.1) | no |

---

## 18. Implementation order after approval

**Done and verified (2026-09-09):**

- `~/.agents/bin/agentrun.sh` and `~/.agents/handoff.tmpl` installed, checksum-verified.
- Harness state at `~/.agents/state/agentrun/pbir-design-analyzer`.
- `agentrun.sh init` run: identity pinned, guard generated with `.githooks` as chain target,
  ledger + registry created, `docs/{plans,reviews,decisions}` present.
- **Push guard verified against a real push** — `git push origin HEAD` from a `pilot/*`
  worktree was refused. Criterion 2 is mechanical.
- Codex plugin + CLI installed, authenticated, review gate off.
- Baseline sequence proven green in a fresh worktree, which is how the `build:webview`
  ordering trap was found (§12.0).
- Tasks 1–4 complete on `main` @ `7e8b66f6`: baseline script + generated report + script
  test, scoring-stage seam, read-only analysis context, Task 4 evidence, all 17 plan
  checkboxes ticked, and R7's `compile:webview` correction applied.
- Task 5 preconditions intact on `main`: no `presentation/` directory, no `scoreLabels.ts`,
  target helpers still present in `App.tsx`.
- `Claude outputs/` removed from the repo; hosted-readiness branch retired from origin.

**Remaining, in order. Steps 1–3 are prerequisites; nothing after them is safe until done.**

1. **Land `main` on `origin/main`.** This is the blocker. `agentrun.sh start` resolves its
   base as `agentrun.base` → `origin/main` → `main` → HEAD, so while `origin/main` sits at
   `4c56eaf3` a pilot run bases on a tree with none of Tasks 1–4. `main` is 8 ahead and 0
   behind, so either works:

   ```bash
   git push origin main                     # fast-forward; the repo documents a
                                            # solo-maintainer exception (1d8889c2)
   ```

   or open a PR from `codex/tasks1-4-main-merge-20260909` and merge, matching how PR #6 was
   handled. Either way, afterwards `git rev-parse origin/main` must equal `7e8b66f6`. If you
   deliberately keep it local, set `git config --local agentrun.base main` instead — but then
   remember pilot work merges into an unpushed line.

2. **Re-apply the `CLAUDE.local.md` hook amendment — it was lost.** On `main`, line 9 still
   reads the original blanket "Do not install or register hooks, MCP servers, …". The
   amendment lived on the retired hosted branch and did not survive consolidation. As it
   stands the repo's own policy prohibits the `pre-push` guard that `init` has installed —
   a contradiction in writing. Replace that line with:

   > - Do not install or register Claude Code lifecycle hooks (`PreToolUse`, `PostToolUse`,
   >   `SessionStart`, `Stop` and similar), MCP servers, PostgreSQL connections, AI Memory
   >   integrations, or governed-memory writes. Git hooks are project tooling and are
   >   permitted — this repository already ships `.githooks/` via `core.hooksPath`.

3. **Clear the leftover baseline worktree.** `pilot/20260909-baseline` still has a worktree
   at `.worktrees/run-20260909-baseline` (prunable, at the old base):

   ```bash
   cd ~/Documents/GitHub/pbir-design-analyzer
   agentrun.sh clean 20260909-baseline --delete-branch
   git worktree prune
   ```

4. **Re-measure the baseline at the new base.** The table in §12.0 was measured on
   `4c56eaf3`; Tasks 1–4 added a seam, a context and new tests, so every count has moved.

   ```bash
   agentrun.sh start baseline
   cd .worktrees/run-<id>/vscode-extension
   npm ci
   npm run build:webview
   npm test
   npm run lint
   cd ..
   node scripts/report-decomposition-baseline.mjs
   dotnet test service-dotnet/tests/Tests.csproj -c Release
   ```

   Then `cd` out and `agentrun.sh clean <id> --delete-branch`.

5. **Copy `pilot-verify-and-review-contract.md` into `docs/decisions/`**, with step 4's
   numbers substituted into its baseline table. Commit it. This is what the reviewer and
   Codex get pointed at.

6. **Run zero — the control.** Task 5 done directly in Claude Code: no Codex, no plan file,
   no ceremony. Record wall clock, attended minutes and `/cost`; log as `00000000-control`.
   Discard the work afterwards; the row is the deliverable.

7. **Run one — Task 5 through the loop.** `agentrun.sh start scorelabels`, fill `scope_files`
   from the plan's own file list, `/atomic-plan`, `/codex:adversarial-review` on the plan,
   `agentrun.sh handoff` into `/codex:rescue --wait`, `agentrun.sh gate`, `atomic-reviewer`,
   `agentrun.sh finish <id> <verdict> <attended-min>`.

   `required_commands` (R7-corrected, build first):
   `npm ci`, `npm run build:webview`, `npm run compile`, `npm test`, `npm run lint`.

8. Compare the ledger row against run zero. Ratchet the §9.3 ceilings, set criterion 8a at
   2.5× the control — **then freeze** (§9.2).
9. Ten runs, unchanged workflow.
10. Evaluate against §16. Expand, revise, or abandon.
11. Only then design the learning layer, in its own document.
