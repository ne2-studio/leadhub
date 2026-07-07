# LeadHub — Frontend

Admin panel for LeadHub: manage forms, browse submissions, and keep an eye on activity across all
of them. React 19 + TypeScript + Vite + Tailwind CSS v4 + Zustand, following the conventions in
[`docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md). Talks to the backend documented in
[`docs/API.md`](../docs/API.md). Visual styling follows [`docs/DESIGN.md`](../docs/DESIGN.md) (the
Exeal design system), adapted for a data-dense admin UI.

## Layers

```
components/  →  store/use{Form,Submission}Store (Zustand)  →  api.ts  →  types.ts
```

- `types.ts` — `Form` and `Submission` entity classes.
- `api.ts` — `api.forms.*` / `api.submissions.*`, namespaced fetch client over the admin API.
- `store/useFormStore.ts` — forms list + CRUD actions.
- `store/useSubmissionStore.ts` — paginated submissions for a form, submission detail, and the
  dashboard's cross-form "recent submissions" feed.
- `components/` — `Dashboard`, `Forms`, `FormEditor` (create/edit modal), `Submissions`,
  `SubmissionDetail` (modal), `Layout`.

## Run locally

**Prerequisites:** Node.js 22+

1. Copy `.env.example` to `.env` and set `VITE_API_URL` to the backend's URL.
2. Set the real OIDC `authority`/`client_id` in `src/main.tsx` (or via `VITE_OIDC_*` env vars).
3. Install dependencies: `npm install`
4. Run the app: `npm run dev`

## Scripts

- `npm run dev` — start the Vite dev server on port 3000.
- `npm run build` — production build to `dist/`.
- `npm run lint` — type-check (`tsc --noEmit`).
