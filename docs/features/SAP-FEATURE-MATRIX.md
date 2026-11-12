# SAP SuccessFactors capability matrix

Package: `Koras.Connect.Sap.SuccessFactors`, .NET 10. All implemented operations are mock/contract-tested; live tenant validation is unavailable.

| ID | Contract | Status / limits |
|---|---|---|
| SAP-001 | Authorized bearer OAuth/OIDC; SAML bearer token exchange POST `/oauth/token` | Implemented. company_id/client_id/grant_type/assertion; host/IdP generates signed Base64 assertion. No assumption that an unrelated IAS token works. |
| SAP-002 | GET `/odata/v2/$metadata`; GET `/odata/v2/{EntitySet}` | Implemented. Safe XML parsing, typed PerPerson/EmpEmployment/EmpJob plus generic typed records and preserved unknown fields. |
| SAP-003 | POST `/odata/v2/upsert?$format=json` | Implemented. Explicit write flag, 1–1000 row SDK bound, per-row `status`/`httpCode`/index/results; HTTP200 can contain errors. Caller verifies entity/fields/RBP/target population. |
| SAP-004 | `$filter`, `$select`, `$expand`, effective dates, cursor/snapshot, `d.results`/`d.__next` | Implemented. URL escaping, origin/path boundaries, repeated-link and page limits. Cursor/entity availability is tenant-specific; no universal delta query. |
| SAP-005 | Cancellation, timeouts, transient read retry, HTTP errors, safe core diagnostics | Implemented. No automatic upsert retry; failed row messages are returned for controlled handling, not logged by the SDK. |
| SAP-006 | Synthetic unit/contract fixtures and independent package consumer | Implemented; no live connectivity/auth/RBP acceptance claim. |
| SAP-007 | Additional SAP APIs / complete business schema validation | Not implemented. Require separate official entity contracts and entitlement validation. |

Official sources reviewed November 12, 2026:

- [SAP API authentication](https://help.sap.com/docs/successfactors-platform/sap-successfactors-api-reference-guide-odata-v2/authentication).
- [SAML bearer token request](https://help.sap.com/docs/successfactors-platform/sap-successfactors-api-reference-guide-odata-v2/requesting-access-token?locale=en-US).
- [Upsert operation](https://help.sap.com/docs/successfactors-platform/sap-successfactors-api-reference-guide-odata-v2/upsert?locale=en-US&state=PRODUCTION&version=LATEST).
- [Entity paging/effective Employee Central data](https://help.sap.com/docs/successfactors-platform/sap-successfactors-api-reference-guide-odata-v2/pagination-12fe2115d865471f9e4b91d55ccfe51a).
- [Official OData reference PDF](https://help.sap.com/doc/a7c08a422cc14e1eaaffee83610a981d/2411/en-US/SF_HCM_OData_API_DEV.pdf).

API host, company, registered OAuth client, technical-user permissions and effective-date semantics must be validated by the consuming organization. ADP-to-SAP orchestration belongs to the CLI/Cloud, not this SDK.
