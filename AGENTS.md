## Prime Directives (SOLID • DRY • KISS • Evolutionary Change)

You are contributing to a C#/.NET codebase. Follow these rules in this order of priority:

1) **Safety & Tests First**
   - Never change behavior without tests that prove the behavior.
   - If code is legacy/untested, write **characterization tests** before refactoring.
   - Add/expand unit tests for new code paths; prefer pure functions for core logic.

2) **DRY Above All**
   - Before writing anything, **search the repo** for similar code (name/sig/behavior). Reuse or extend before you rewrite.
   - If duplication is discovered, refactor to a single abstraction. Prefer small internal helpers over copy/paste.
   - If reuse would force leaky coupling, introduce an **interface** in the consuming layer and adapt.

3) **SOLID Design Rules**
   - **S**ingle Responsibility: Each class has one reason to change. If a class does logging + parsing, split them.
   - **O**pen/Closed: New behavior should come from extension (new types/strategies), not editing core switch/if ladders.
   - **L**iskov: No surprising pre/postcondition changes. Subtypes must be substitutable.
   - **I**nterface Segregation: Prefer small, focused interfaces. Avoid “fat” god interfaces.
   - **D**ependency Inversion: Depend on interfaces/abstractions. Inject via constructors; avoid hardwired statics/singletons.

4) **KISS: Keep It Simple, Stupid**
   - Simplicity beats cleverness. **Prefer the smallest, clearest solution that solves the problem well.**
   - If a solution needs a diagram to explain, it’s probably too complex.
   - Avoid premature abstraction — don’t introduce a pattern or interface unless it serves an immediate purpose.
   - Eliminate unnecessary indirection, inheritance, and “magic.” Code should be self-evident to future maintainers.
   - Favor straightforward data flow and control structures over deeply nested logic or over-engineered solutions.
   - Simplicity ≠ naïveté: still enforce SRP, OCP, and DIP — but do it in the **least complex way possible**.

5) **Evolve, Don’t Mutate**
   - Prefer **additive paths** (new types/adapters) over editing existing core code. Use the **Strangler Fig** approach:
     - Create a new implementation alongside old.
     - Write adapters/facades to route traffic.
     - Migrate callers gradually with feature flags.
     - Deprecate old paths with clear timelines.
   - Mark superseded APIs with `[Obsolete("Use XyzService2", error: false)]` and link to the replacement.

---

## Change Protocol (You must follow this order)

1) **Understand**
   - Restate the user story/bug and acceptance criteria.
   - List inputs/outputs, side effects, and cross-cutting concerns (perf, security, PII).

2) **Inventory & Reuse Check**
   - Search for existing: services, strategies, validators, mappers, options, policies, extension methods.
   - List candidates with file paths and explain reuse plan or why not.

3) **Design Sketch**
   - Propose a **minimal, SOLID, DRY, and KISS-compliant design**:
     - New interfaces (names, members).
     - New classes (responsibility in one sentence).
     - Where dependencies are injected (constructor signatures).
     - How old code is adapted or routed.
   - Provide a dependency diagram (mermaid) showing direction **toward abstractions**.

4) **Risk & Migration Plan**
   - Identify blast radius and rollback plan.
   - Flag and propose **feature toggles** or config gates for new behavior.
   - Note deprecations (what/when/how measured).

5) **Test Plan (must be concrete)**
   - List unit tests to add (method names, cases, edge cases).
   - List integration tests to add or update.
   - Define invariants and expected exceptions.
   - If refactor-only: create characterization tests first.

6) **Implementation**
   - Implement the **simplest, smallest valuable slice first**.
   - Keep methods small (<25 lines when practical). No magic numbers. Guard clauses allowed.
   - Prefer composition over inheritance; avoid static state.

7) **Validation**
   - Run/describe how tests pass.
   - Show duplication eliminated (before/after paths).
   - Provide perf/security notes if relevant.

---

## Code Rules (quick checks you must self-enforce)

- **Constructor injection only**; no service locator anti-pattern.
- **No static mutable state**; prefer options/config & DI lifetimes.
- **No god classes** (> ~300 LoC or > ~7 public members without strong justification).
- **No long switch/if ladders** for behavior: use Strategy/State/Specification/Visitor if needed — but **only if needed** (KISS).
- **Pure core, impure edges**: core logic is side-effect free; I/O and frameworks live at boundaries.
- **Immutability by default**: make models/records immutable unless mutability is required.
- **CQS**: query methods don’t mutate; command methods don’t return domain data.
- **Mapping & validation**
  - Centralize mappers/validators; don’t duplicate ad-hoc per feature.
  - Prefer FluentValidation (or existing project standard) over manual checks sprinkled across code.

---

## Duplication Prevention Routine (You must run this before coding)

1) Search for similar method names/semantics: `<Verb><Noun>`, `<Try|Ensure|Validate|Parse|Map>`
2) Search for near-dup logic by shape (conditions/loops) and domain keywords.
3) If ≥2 similar spots exist, propose a shared abstraction (helper, policy, strategy).
4) If domains differ, keep call-site adapters thin; don’t force leaky base classes.

---

## Refactor-First Patterns to Prefer

- **Specification** for query/filters (composable predicates).
- **Strategy** for variant business rules.
- **Decorator** for cross-cutting concerns (caching, logging, retry).
- **Adapter/Facade** to wrap legacy APIs.
- **Factory** to select strategies by config/feature flag.
- **Outbox / Domain Events** for side-effects decoupling (if applicable).

⚠️ **KISS Clause:** Patterns are not mandatory. Use them only if they significantly improve readability, reusability, or testability. If a plain method or function is clearer, use that.

---

## Required Artifacts per Significant Change

- **Tests** (unit + integration for new edges).
- **ADR** (Architecture Decision Record) if adding an abstraction or changing direction.
- **Deprecation note** in the replacing type’s XML doc with link to ADR.
- **Mermaid diagram** of the affected slice.

---

## Conventional Commits (enforced)

Use: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `perf:`, `chore:`, `build:`.
- Refactors that keep behavior: `refactor:`
- New abstractions or removal of duplication: `refactor:` or `feat:` (if public API)
- Deprecations: mention in body with `DEPRECATES: <Type.Member>`.

---

## Your Final Self-Review (must pass before completion)

- ✅ Did I **reuse** something instead of writing new?
- ✅ Did I **add tests first** (or characterization) before refactor?
- ✅ Are classes/methods **single-purpose** and small?
- ✅ Did I **extend** instead of editing a stable core?
- ✅ Are dependencies **inverted and injected**?
- ✅ Did I **avoid duplication** and centralize cross-cutting concerns?
- ✅ Did I **keep it as simple as possible** while still SOLID and DRY?
- ✅ Is there a **migration + rollback plan** with toggles and `[Obsolete]`?
- ✅ Did I update **docs/ADR/diagrams**?
