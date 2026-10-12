# Authentication Architecture

> **Status: proposed — not implemented.**
> This document records the authentication and authorization decisions for BOX *before*
> any implementation. It is the deliverable of issue
> [#3](https://github.com/Raf-Hs/box-backend/issues/3) and the reference for the
> implementation issues [#4](https://github.com/Raf-Hs/box-backend/issues/4)–[#9](https://github.com/Raf-Hs/box-backend/issues/9)
> and the coordinated frontend work (epic
> [#27](https://github.com/Raf-Hs/box-frontend/issues/27)).

## 1. Context

- **Frontend:** Angular 19 on Vercel — `https://box-frontend-1l88.vercel.app`
- **Backend:** ASP.NET Core (.NET 10) on Render, Docker, SQLite at `/data/dailytracker.db`
  — `https://box-backend-um5d.onrender.com`
- The two are on **different registrable domains**, so any browser request from the frontend
  to the backend today is **cross-site**.
- Current state: no user model, no authentication, no authorization. `Habit` is a global
  table seeded with six defaults; `DailyHabitEntry` belongs to a `Habit`. Every endpoint is
  anonymous, and CORS allows `localhost:4200` and the Vercel origin with any header/method
  and **no** credentials.
- Existing data: one anonymous dataset (six habits and their entries). It has no owner.

## 2. Decision summary

| # | Decision | Choice |
| --- | --- | --- |
| D1 | Identity foundation | **ASP.NET Core Identity** with EF Core + SQLite |
| D2 | Authentication transport | **HttpOnly cookie session**, delivered same-origin through a Vercel `/api` rewrite |
| D3 | Session lifetime & revocation | Short-lived sliding cookie + **security-stamp** revocation; persisted Data Protection keys |
| D4 | CSRF | `SameSite=Lax` **plus** an antiforgery token (double-submit cookie) |
| D5 | CORS | Exact origins only; credentials **only** for local development |
| D6 | User ownership | Habits are **user-owned**; the six defaults become a per-user seed template |
| D7 | Existing anonymous data | Preserved with a `NULL` owner, excluded from all user views, claimed only by an explicit documented step |

## 3. D1 — Identity foundation: ASP.NET Core Identity

**Decision:** use **ASP.NET Core Identity** (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`)
on top of the existing `AppDbContext` (converted to `IdentityDbContext<AppUser>`).

**Why:**
- Provides a maintained, audited password hasher (PBKDF2) — we must never implement custom
  cryptography.
- Provides the user store, unique-email handling, password policy, and **lockout** out of the box.
- Supports future additions (roles, external logins, MFA) without redesign.
- Integrates directly with EF Core migrations we already use.

**Rejected:** a hand-rolled user table with a custom hasher — more code, more risk, no benefit.

**Concretely:**
- `AppUser : IdentityUser` (email as the primary identifier, normalized and unique).
- `AppDbContext : IdentityDbContext<AppUser>`; existing `DbSet`s are kept.
- Password policy and lockout are set explicitly (for example, minimum length 8,
  `MaxFailedAccessAttempts = 5`, lockout window of 5 minutes).
- Role support is **not** required yet; the model remains extensible.

## 4. D2 — Authentication transport

The frontend and backend are on different registrable domains, so the transport decision is
driven by the deployment topology, not by preference.

### Options considered

**A. HttpOnly cookie session, cross-site (`SameSite=None; Secure`).**
Cookies avoid `localStorage` entirely and cannot be read by JavaScript. But a cookie set by
`box-backend-um5d.onrender.com` and sent from `box-frontend-1l88.vercel.app` is a
**third-party cookie**: Safari blocks it by default, and Chrome is phasing third-party cookies
out. It also requires credentialed CORS. **Rejected as the primary approach** — unreliable.

**B. Bearer access token in memory + rotating refresh token in an HttpOnly cookie.**
Better than A for the access token (never persisted, immune to XSS exfiltration), but the
refresh token is still a third-party cookie, so *persistence across reloads* inherits the
same browser problem. Keeping the refresh token in memory instead would force a re-login on
every reload, violating the "persistent identity across page reloads" requirement.

**C. Cookie session exposed **same-origin** through a Vercel rewrite (chosen).**
Vercel rewrites `/api/*` to the Render backend, so the browser only ever talks to the
frontend's own origin. The backend's `Set-Cookie` is proxied through and stored as a
**first-party** cookie. `SameSite=Lax` then works normally, third-party-cookie blocking is
irrelevant, and no token is ever exposed to JavaScript.

**Decision: option C** — a cookie session served same-origin via a Vercel rewrite, with
option B kept as the documented fallback if the proxy proves impractical.

**Concretely:**
- Backend uses the Identity application cookie. For browsers reachable only through the proxy
  this is first-party.
- The frontend's HTTP client sends credentials (`withCredentials: true` in Angular).
- Local development works without the proxy: `localhost:4200` → `localhost:5000` are
  same-site (cookies ignore ports), so `SameSite=Lax` still applies; **credentialed CORS** covers
  the cross-origin dev case (D5).

**Trade-offs accepted:** an extra proxy hop (latency, and a Vercel rewrite that must forward
`Set-Cookie`); direct cross-origin calls from an arbitrary client are not a supported use case.

## 5. D3 — Session lifetime and revocation

- **Expiry:** short absolute lifetime with **sliding expiration** (for example, 60-minute
  sliding window, 8-hour absolute cap) so an active user is not interrupted while an idle
  session eventually dies.
- **Cookie flags:** `HttpOnly = true`, `Secure = Always` in production, `SameSite = Lax`,
  host-only (no `Domain` attribute so it applies to the proxy host).
- **Revocation:** enable **security-stamp validation** so a logout, password change, or an
  administrative action invalidates existing cookies server-side within a short interval.
- **Logout:** `SignOutAsync` clears the cookie **and** rotates the security stamp so a copied
  cookie cannot be replayed.

## 6. D4 — CSRF protection

Because authentication is cookie-based, CSRF must be handled explicitly.

- `SameSite=Lax` already blocks the cookie on cross-site `POST`/`PUT`/`PATCH` requests, which
  removes the most common CSRF vector.
- **Defence in depth:** require an **antiforgery token** on every state-changing request using
  ASP.NET Core antiforgery middleware + a double-submit cookie:
  - The backend writes a readable `XSRF-TOKEN` cookie.
  - The frontend (Angular `withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' })`)
    echoes it in the `X-XSRF-TOKEN` header.
  - The backend validates the header token on unsafe methods.
- `GET` requests are safe/idempotent and are not required to carry the token.

## 7. D5 — CORS

- Production browsers use the same-origin proxy, so CORS is **not** needed there.
- Local development is cross-origin (`localhost:4200` → `localhost:5000`) and needs
  **credentialed CORS**: exact origins (`http://localhost:4200`), `AllowCredentials()`, and an
  explicit header/method list. A wildcard origin can never be combined with credentials.
- The currently-allowed production Vercel origin is retained only for transitional direct
  access; it does not need credentials once the proxy is in place.
- CORS is **not** an access-control mechanism — authorization is enforced by the API itself.

## 8. D6 — User ownership model

**Decision: habits are user-owned; the six defaults are a template copied to each user at
registration.**

- `Habit` gains an owner: `UserId` (FK to `AppUser`).
- `DailyHabitEntry` keeps `HabitId`; ownership is derived through the owned `Habit`. Entries
  are **not** given a redundant `UserId`.
- On registration, the six defaults (`Sleep`, `Food`, `Water`, `Walk`, `Sun`, `Stand up`) are
  copied as that user's habits, so each user can adjust their own targets.
- The current "seed if the table is empty" behaviour is replaced by per-user seeding; a fresh
  database no longer contains global habits.
- Every read and write is scoped to the authenticated user. Ownership is enforced in the
  service layer and, as defence in depth, with an EF Core **global query filter** on `Habit`
  (`h.UserId == currentUserId`) backed by an `ICurrentUser` abstraction. Entry queries travel
  through the owned habit.
- The unique index on `(HabitId, Date)` is unchanged.

**Rejected alternatives:** globally shared habits with per-user target overrides (more tables
and more branching for no current benefit); leaving habits global (impossible to isolate users).

## 9. D7 — Migration of existing anonymous data

The existing habits and entries have **no owner**. They must not be silently assigned to an
arbitrary account and must not be deleted.

**Plan:**
1. **Migration 1:** add Identity tables and a **nullable** `Habit.UserId`.
2. Existing rows keep `UserId = NULL`. Because every user query filters on the authenticated
   user, `NULL`-owned rows are automatically invisible to all users — preserved, but
   quarantined.
3. **Claim procedure (explicit, documented, never automatic):** after the intended account is
   registered, an operator runs a documented one-off command/script to set `UserId` on the
   legacy habits (and thus their entries) to that account. This is a deliberate action with a
   named owner.
4. **Optional migration 2:** once the legacy data is claimed or archived, make `UserId`
   non-nullable.

## 10. D8 — Deployment, configuration, and secrets

- **Data Protection key ring must persist.** Cookies are encrypted with Data Protection keys;
  if the key ring is lost on restart, every session is invalidated. On Render, persist the key
  ring to the mounted disk:
  `PersistKeysToFileSystem("/data/keys")` and a fixed `SetApplicationName("box-backend")`.
- **HTTPS and proxies.** Render terminates TLS; enable `UseForwardedHeaders` (and trust the
  proxy) so `Secure` cookies and generated URLs use `https`, and enable HTTPS redirection in
  production.
- **Secrets.** No JWT signing key is needed for cookie auth, but any future signing key or
  connection string stays in environment variables (`ConnectionStrings__DefaultConnection`,
  etc.) and is never committed. `.gitignore` already excludes `*.db`.
- **Health.** `GET /health` remains anonymous.
- **CORS.** Configured per D5.

> **Critical open question — SQLite persistence on Render.** If `/data` is **not** a persistent
> Render disk (persistent disks are a paid feature), the SQLite database and the Data
> Protection key ring are lost on every deploy, which would silently destroy all accounts and
> sessions. This **must be verified before implementing** #4. If the disk is not persistent,
> the storage strategy must change (managed Postgres, or a confirmed persistent disk).

## 11. D9 — Error handling and status codes

- `401 Unauthorized` — no valid session.
- `403 Forbidden` — authenticated but not permitted (used sparingly; for
  other users' record ids, prefer `404` so existence is not leaked).
- `400 Bad Request` — validation failure.
- `409 Conflict` — duplicate registration.
- `429 Too Many Requests` — lockout / rate limiting.
- Messages are generic and **never** reveal whether an email exists, which field failed, or
  any internal detail.

## 12. API contract (proposed)

| Method | Endpoint | Purpose | Success | Notes |
| --- | --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Create an account | `201` | Validates email/password server-side; seeds the six habits |
| `POST` | `/api/auth/login` | Start a session | `200` + `Set-Cookie` | Lockout on repeated failures |
| `POST` | `/api/auth/logout` | End the session | `204` | Clears cookie, rotates security stamp |
| `GET` | `/api/auth/me` | Current user | `200` / `401` | Used to restore state on reload |
| `GET` | `/api/auth/csrf` | Issue the antiforgery cookie | `204` | Sets `XSRF-TOKEN` |
| `GET`/`PATCH` | `/api/habits*` | Habits | as today | Now authenticated and user-scoped |
| `GET`/`PUT` | `/api/daily-entries*` | Entries | as today | Now authenticated and user-scoped |
| `GET` | `/health` | Health | `200` | Stays anonymous |

## 13. Frontend implications

- Add an `AuthService` with signals for the current user; call `register`, `login`, `logout`,
  and `me`.
- Configure `withCredentials` and the CSRF header/cookie names in `app.config.ts`.
- Add route guards and redirect-back after login.
- Add the **Vercel `/api` rewrite** (a `vercel.json` change) so the cookie is first-party.
- Never store session material in `localStorage`.
- Handle `401` by clearing state and routing to login.

These belong to frontend issues [#29](https://github.com/Raf-Hs/box-frontend/issues/29)–[#33](https://github.com/Raf-Hs/box-frontend/issues/33)
and the documentation issue [#34](https://github.com/Raf-Hs/box-frontend/issues/34).

## 14. Risks and open questions

1. **Render disk persistence** (see D8) — blocking; verify before #4.
2. **Vercel rewrite behaviour** — confirm `Set-Cookie` is forwarded and the hop has an
   acceptable timeout.
3. **Render cold starts** — the first request after idle may be slow; the login screen must
   show a loading state.
4. **Email confirmation and password reset are out of scope** — accounts are created without
   verification and cannot be recovered. Documented as a known limitation with follow-up
   issues.
5. **Account enumeration** — registration and login messages must stay uniform.
6. **Tracked vs. untracked days** — unrelated to ownership, but the ownership migration is a
   natural point to revisit the existing known limitation (zero-filled history). It is
   intentionally **not** bundled into the authentication work.

## 15. Implementation order

1. **#4** Identity + `AppUser` + migration + per-user seeding foundation.
2. **#5** Registration endpoint.
3. **#6** Login / logout / session + security stamp.
4. **#7** Habit ownership + entry scoping + legacy-data handling.
5. **#8** Authorization on all personal endpoints + cross-user isolation tests.
6. **#9** Deployment configuration (Data Protection keys, HTTPS, CORS, environment variables).
7. Frontend #29–#34 coordinated against this contract.

## 16. References

- Workflow: [`docs/DEVELOPMENT_WORKFLOW.md`](DEVELOPMENT_WORKFLOW.md)
- Backend epic: [#1](https://github.com/Raf-Hs/box-backend/issues/1)
- Frontend epic: [#27](https://github.com/Raf-Hs/box-frontend/issues/27)
