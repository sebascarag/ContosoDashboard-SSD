# ContosoDashboard Constitution

## Core Principles

### I. Training Purpose and Offline Operation
ContosoDashboard MUST remain an educational training application, not a production-ready
product. Its core workflows MUST run locally without requiring cloud services or other
external service dependencies. Documentation MUST distinguish mock training behavior and
known limitations from production security guarantees.

### II. Layered Design and Infrastructure Abstraction
Changes MUST preserve clear separation between the Blazor presentation layer, business
services, data access, and models. Business logic MUST depend on abstractions rather than
provider-specific infrastructure when a replaceable service boundary is appropriate.
Training implementations MUST remain local and self-contained; cloud implementations are
future migration options, not runtime prerequisites. This keeps the application teachable
and usable offline.

### III. Authorization and Data Isolation
Every protected operation MUST enforce authorization on the server, including service-level
checks for resource ownership, project membership, and role permissions where applicable.
UI visibility and route attributes alone MUST NOT be treated as sufficient authorization.
Queries and mutations MUST return or change only data the current user is permitted to
access. This is necessary to preserve the project's user-isolation and IDOR-prevention goals.

### IV. Secure Input and Data Handling
User-controlled values MUST be validated at application boundaries and safely handled by
data and storage layers. File features MUST follow their approved requirements for type and
size validation, malware scanning, authorization, and storage outside web-accessible paths;
user-supplied filenames MUST NOT be used as storage paths. Sensitive operations MUST fail
closed when authorization or validation cannot be established. Training limitations MUST
not be described as production-grade protections.

### V. Verifiable, Focused Changes
Changes MUST be limited to the approved feature or maintenance scope and MUST avoid
unnecessary architectural complexity. Behavior changes MUST have automated tests where a
test harness is available, with focused coverage for authorization and data isolation when
those boundaries change. If automated coverage is not available, the change MUST document
the relevant manual verification. Build and applicable checks MUST pass before a change is
considered complete.

## Project Constraints and Security Boundaries

- The application uses ASP.NET Core with Blazor Server and Entity Framework Core; changes
  MUST remain compatible with the project's configured target framework and existing
  architecture.
- Core training operation MUST remain offline-capable and use local infrastructure. New
  infrastructure dependencies MUST be behind suitable interfaces when replaceability is
  required, and MUST NOT make cloud access mandatory.
- Mock authentication and authorization are training mechanisms only. Production use MUST
  not be represented as safe without replacing them with an appropriate identity solution
  and addressing the limitations documented in the README.
- Resource access MUST follow least privilege and enforce user and project boundaries in
  server-side services, including for future document storage and sharing features.

## Development Workflow

- New features and material behavior changes MUST follow the repository's Spec-Driven
  Development workflow: requirements are specified, implementation is planned, and
  dependency-ordered tasks are reviewed before implementation.
- Specifications, plans, implementation reviews, and applicable tests MUST be checked
  against this constitution. A conflict MUST be resolved by updating the proposal or
  obtaining an approved constitution amendment; implementation MUST NOT silently override
  a principle.
- Reviews MUST consider authorization, data isolation, offline operation, and the tests or
  other verification evidence applicable to the change.

## Governance

This constitution governs project requirements and takes precedence over conflicting
implementation guidance. Amendments MUST be proposed as version-controlled changes with
their rationale and impact documented, reviewed and approved by repository maintainers,
and reflected in the version and amendment date before merge. Changes that require migration
or alter existing behavior MUST document the necessary transition.

The constitution uses semantic versioning. A MAJOR increment marks a backward-incompatible
principle removal or redefinition; a MINOR increment marks a new principle or section, or
materially expanded guidance; a PATCH increment marks clarifications and non-semantic
refinements. The ratification date records initial adoption; the last-amended date changes
whenever an approved amendment is made.

Every feature proposal and change review MUST assess compliance with the principles above.
Any exception MUST identify the affected rule, scope, rationale, risks, and approval in the
change record; it MUST NOT be treated as a permanent amendment unless the constitution is
formally updated.

**Version**: 1.0.0 | **Ratified**: 2026-11-20 | **Last Amended**: 2026-11-20
