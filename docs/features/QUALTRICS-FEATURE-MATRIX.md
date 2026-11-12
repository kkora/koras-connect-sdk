# Qualtrics capability matrix

Package: `Koras.Connect.Qualtrics`, .NET 10. All implemented contracts use synthetic tests; no live account/token/entitlement validation.

| ID | Operation | Status / limits |
|---|---|---|
| QLT-001 | X-API-TOKEN from rotating host token source, explicit data-center `/API/v3/` root | Implemented. Redirect-disabled HTTP clients required; no bearer credential added by Create. |
| QLT-002 | GET `/surveys`; GET `/surveys/{surveyId}` | Implemented. Typed summaries, preserved unknown fields, raw survey design document; retrieval rights may differ from listing rights. |
| QLT-003 | POST `/surveys/{surveyId}/export-responses` | Implemented. Typed bounded options for formats/date/filter/continuation; no automatic POST retry. |
| QLT-003b | GET `/surveys/{surveyId}/export-responses/{progressId}` | Implemented. complete/inProgress/failed states, bounded cancellation-aware polling. |
| QLT-003c | GET `/surveys/{surveyId}/export-responses/{fileId}/file` | Implemented. Uses returned fileId, streams raw bytes to caller-owned stream, leaves archive extraction to the host. |
| QLT-004 | nextPage pagination, throttling, timeout and cancellation | Implemented. Same-origin/API-path checks, cycle/page bounds, core GET retry and Retry-After. |
| QLT-005 | Typed survey/progress contracts and safe diagnostics/errors | Implemented. No provider error payload copied to output files or automatic logs. |
| QLT-006 | Distribution and directory writes | Not implemented (P1). Separate entitled contracts required; no guessed endpoints. |
| QLT-007 | Events/webhook receiver | Not implemented (P1). Host infrastructure/authentication and approved provider contract required. |
| QLT-008 | Synthetic contract fixtures and package consumer | Implemented; live tenant evidence unavailable. |

Official sources reviewed November 12, 2026:

- [API authentication](https://www.qualtrics.com/support/integrations/api-integration/overview/).
- [List surveys](https://developer-docs.qualtrics.com/docs/public-api/surveys/list-surveys/) and [response schema](https://developer-docs.qualtrics.com/docs/public-api/surveys/schemas/list-surveys-response/).
- [Get survey](https://developer-docs.qualtrics.com/docs/public-api/surveys/get-survey/).
- [Start response export](https://developer-docs.qualtrics.com/docs/public-api/responseImportsExports/create-export/).
- [Progress](https://developer-docs.qualtrics.com/docs/public-api/responseImportsExports/get-export-progress/) and [file download](https://developer-docs.qualtrics.com/docs/public-api/responseImportsExports/get-export-file/).

Cross-provider orchestration, durable export jobs, scheduling, tenant/account administration and ZIP extraction are caller responsibilities.
