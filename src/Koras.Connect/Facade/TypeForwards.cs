using System.Runtime.CompilerServices;
using Koras.Connect.Abstractions;
using Koras.Connect.Authentication;
using Koras.Connect.Exceptions;
using Koras.Connect.Export;
using Koras.Connect.Synchronization;
using Koras.Connect.Transport;

// Koras.Connect is a transitional compatibility facade (master spec section 6): every public type that
// shipped in preview.2 under this package now lives in Koras.Connect.Abstractions/.Core/.Authentication/.Export
// and is forwarded here under its unchanged original namespace so existing preview.2 consumers keep compiling
// and resolving without a code change. This facade carries no source of its own and is not a ninth permanent
// target package (ADR-M06).

// Moved to Koras.Connect.Abstractions
[assembly: TypeForwardedTo(typeof(EntitlementState))]
[assembly: TypeForwardedTo(typeof(EntitlementEvidence))]
[assembly: TypeForwardedTo(typeof(ConnectorDescriptor))]
[assembly: TypeForwardedTo(typeof(DiagnosticStage))]
[assembly: TypeForwardedTo(typeof(DiagnosticOutcome))]
[assembly: TypeForwardedTo(typeof(ConnectionDiagnostic))]
[assembly: TypeForwardedTo(typeof(IConnector))]
[assembly: TypeForwardedTo(typeof(ICredentialProvider<>))]
[assembly: TypeForwardedTo(typeof(StaticCredentialProvider<>))]
[assembly: TypeForwardedTo(typeof(DelegatingCredentialProvider<>))]
[assembly: TypeForwardedTo(typeof(FieldError))]
[assembly: TypeForwardedTo(typeof(MappingResult<>))]
[assembly: TypeForwardedTo(typeof(IRecordMapper<,>))]
[assembly: TypeForwardedTo(typeof(ContinuationToken))]
[assembly: TypeForwardedTo(typeof(Page<>))]
[assembly: TypeForwardedTo(typeof(IValidatableOptions))]
[assembly: TypeForwardedTo(typeof(OptionsValidationException))]
[assembly: TypeForwardedTo(typeof(IAccessTokenSource))]
[assembly: TypeForwardedTo(typeof(IRequestAuthenticator))]
[assembly: TypeForwardedTo(typeof(IExportProvider))]
[assembly: TypeForwardedTo(typeof(ExportSchema<>))]
[assembly: TypeForwardedTo(typeof(ExportColumn<>))]

// Moved to Koras.Connect.Core
[assembly: TypeForwardedTo(typeof(ErrorDetails))]
[assembly: TypeForwardedTo(typeof(IntegrationException))]
[assembly: TypeForwardedTo(typeof(ConfigurationException))]
[assembly: TypeForwardedTo(typeof(AuthenticationException))]
[assembly: TypeForwardedTo(typeof(AuthorizationException))]
[assembly: TypeForwardedTo(typeof(ThrottleException))]
[assembly: TypeForwardedTo(typeof(TransportException))]
[assembly: TypeForwardedTo(typeof(ContractException))]
[assembly: TypeForwardedTo(typeof(DeadlineExceededException))]
[assembly: TypeForwardedTo(typeof(IntegrationExceptionClassifier))]
[assembly: TypeForwardedTo(typeof(EndpointSafety))]
[assembly: TypeForwardedTo(typeof(DiagnosticEvents))]
[assembly: TypeForwardedTo(typeof(IntegrationRequestException))]
[assembly: TypeForwardedTo(typeof(IntegrationJsonResponse))]
[assembly: TypeForwardedTo(typeof(TransportOptions))]
[assembly: TypeForwardedTo(typeof(IntegrationTransport))]
[assembly: TypeForwardedTo(typeof(BearerTokenAuthenticator))]
[assembly: TypeForwardedTo(typeof(HeaderTokenAuthenticator))]
[assembly: TypeForwardedTo(typeof(RecordChange<>))]
[assembly: TypeForwardedTo(typeof(Reconciliation))]

// Moved to Koras.Connect.Authentication
[assembly: TypeForwardedTo(typeof(RedactedCredential<>))]
[assembly: TypeForwardedTo(typeof(CertificateCredentialProvider))]
[assembly: TypeForwardedTo(typeof(ClientCertificateTransport))]
[assembly: TypeForwardedTo(typeof(ClientCredentialsOptions))]
[assembly: TypeForwardedTo(typeof(ClientCredentialsTokenSource))]

// Moved to Koras.Connect.Export
[assembly: TypeForwardedTo(typeof(CsvExportProvider))]
[assembly: TypeForwardedTo(typeof(XmlExportProvider))]
[assembly: TypeForwardedTo(typeof(XlsxExportProvider))]
[assembly: TypeForwardedTo(typeof(JsonExportProvider))]
[assembly: TypeForwardedTo(typeof(TabularExport))]
