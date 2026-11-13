using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Qualtrics;
using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>Synthetic, offline coverage for the Qualtrics DI extension, structured errors/diagnostics, reusable
/// paging, bounded concurrency, directory/contact/mailing-list surface and data mapping added alongside the
/// pre-existing survey/export client. No test in this file performs network access.</summary>
public sealed class QualtricsExpansionTests
{
    private sealed class Token : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic-token"); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri QualtricsRoot = new("https://qualtrics.test/API/v3/");

    // ---------- QLT-001 / QLT-016: DI extension ----------

    [Fact]
    public void AddKorasQualtricsRejectsDuplicateRegistration()
    {
        var services = new ServiceCollection();
        services.AddKorasQualtrics(o => { o.ApiRoot = QualtricsRoot; }, _ => new Token());
        Assert.Throws<InvalidOperationException>(() => services.AddKorasQualtrics(o => { o.ApiRoot = QualtricsRoot; }, _ => new Token()));
    }

    [Theory]
    [InlineData("http://qualtrics.test/API/v3/")]
    [InlineData("https://user:secret@qualtrics.test/API/v3/")]
    [InlineData("https://qualtrics.test/API/v2/")]
    public void AddKorasQualtricsRejectsUnsafeOrWrongRootBeforeAnyHttpAttempt(string root)
    {
        var services = new ServiceCollection();
        Assert.Throws<OptionsValidationException>(() => services.AddKorasQualtrics(o => { o.ApiRoot = new Uri(root); }, _ => new Token()));
        // No HttpClient registration should have been added for a rejected configuration.
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(QualtricsClient));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100001)]
    [InlineData(-5)]
    public void AddKorasQualtricsRejectsBadMaxPages(int maxPages)
    {
        var services = new ServiceCollection();
        Assert.Throws<OptionsValidationException>(() => services.AddKorasQualtrics(o => { o.ApiRoot = QualtricsRoot; o.MaxPages = maxPages; }, _ => new Token()));
    }

    [Fact]
    public async Task AddKorasQualtricsRegistersAResolvableClientWithoutContactingNetworkDuringRegistration()
    {
        var services = new ServiceCollection();
        var httpCalls = 0;
        services.AddKorasQualtrics(o => { o.ApiRoot = QualtricsRoot; }, _ => new Token());
        services.AddHttpClient("Koras.Qualtrics.Api").ConfigurePrimaryHttpMessageHandler(() => new Handler((r, ct) => { httpCalls++; return Task.FromResult(Json("""{"result":{"elements":[],"nextPage":null}}""")); }));
        using var provider = services.BuildServiceProvider();
        Assert.Equal(0, httpCalls);
        var client = provider.GetRequiredService<QualtricsClient>();
        var surveys = new List<SurveySummary>();
        await foreach (var s in client.EnumerateSurveysAsync()) surveys.Add(s);
        Assert.Equal(1, httpCalls);
    }

    // ---------- QLT-005: structured errors and diagnostics ----------

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(AuthenticationException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(AuthorizationException))]
    [InlineData(HttpStatusCode.NotFound, typeof(ContractException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(ThrottleException))]
    [InlineData(HttpStatusCode.ServiceUnavailable, typeof(TransportException))]
    public void QualtricsApiExceptionClassifiesEachStatusCodeIntoTheSharedHierarchy(HttpStatusCode status, Type expectedInnerType)
    {
        var retryAfter = status == HttpStatusCode.TooManyRequests ? TimeSpan.FromSeconds(30) : (TimeSpan?)null;
        var ex = QualtricsApiException.Classify(status, attempts: 1, sanitizedRequestId: "req-1", retryAfter: retryAfter);
        Assert.Equal(status, ex.StatusCode);
        Assert.IsType(expectedInnerType, ex.InnerException);
        Assert.Equal("req-1", ex.Details.CorrelationId);
        if (status == HttpStatusCode.TooManyRequests)
        {
            Assert.Equal(retryAfter, ex.RetryAfter);
            Assert.Equal(retryAfter, ((ThrottleException)ex.InnerException!).RetryAfter);
        }
    }

    [Fact]
    public void TryExtractRequestIdNeverCrashesOnMalformedOrHostileMeta()
    {
        using var missing = JsonDocument.Parse("{}");
        using var wrongShape = JsonDocument.Parse("""{"meta":"not-an-object"}""");
        using var noRequestId = JsonDocument.Parse("""{"meta":{"httpStatus":"400 - Bad Request"}}""");
        using var hostile = JsonDocument.Parse(JsonSerializer.Serialize(new { meta = new { requestId = "abc\r\ninjected" } }));
        using var oversized = JsonDocument.Parse(JsonSerializer.Serialize(new { meta = new { requestId = new string('a', 500) } }));
        using var good = JsonDocument.Parse("""{"meta":{"requestId":"req-ok"}}""");
        Assert.Null(QualtricsApiException.TryExtractRequestId(missing.RootElement));
        Assert.Null(QualtricsApiException.TryExtractRequestId(wrongShape.RootElement));
        Assert.Null(QualtricsApiException.TryExtractRequestId(noRequestId.RootElement));
        Assert.Null(QualtricsApiException.TryExtractRequestId(hostile.RootElement));
        Assert.Null(QualtricsApiException.TryExtractRequestId(oversized.RootElement));
        Assert.Equal("req-ok", QualtricsApiException.TryExtractRequestId(good.RootElement));
    }

    [Fact]
    public async Task DiagnoseAsyncNeverIssuesNonGetAndReportsPassedStagesOnSuccess()
    {
        using var h = new HttpClient(new Handler((r, ct) =>
        {
            Assert.Equal(HttpMethod.Get, r.Method);
            return Task.FromResult(Json("""{"result":{"elements":[],"nextPage":null}}"""));
        }));
        var options = new QualtricsOptions(QualtricsRoot);
        var client = QualtricsClient.Create(h, new Token(), options);
        var connector = new QualtricsConnector(client, options);
        var results = await connector.DiagnoseAsync();
        Assert.Collection(results,
            r => { Assert.Equal(DiagnosticStage.Configuration, r.Stage); Assert.Equal(DiagnosticOutcome.Passed, r.Outcome); },
            r => { Assert.Equal(DiagnosticStage.Authentication, r.Stage); Assert.Equal(DiagnosticOutcome.Passed, r.Outcome); });
    }

    [Fact]
    public async Task DiagnoseAsyncReportsFailedAuthenticationOn401WithoutThrowing()
    {
        using var h = new HttpClient(new Handler((r, ct) => Task.FromResult(Json("{}", HttpStatusCode.Unauthorized))));
        var options = new QualtricsOptions(QualtricsRoot);
        var client = QualtricsClient.Create(h, new Token(), options);
        var connector = new QualtricsConnector(client, options);
        var results = await connector.DiagnoseAsync();
        var auth = results[1];
        Assert.Equal(DiagnosticOutcome.Failed, auth.Outcome);
        Assert.Equal("http_401", auth.SanitizedCode);
    }

    [Fact]
    public async Task DiagnoseAsyncSkipsAuthenticationStageWhenConfigurationIsInvalid()
    {
        var httpCalls = 0;
        using var h = new HttpClient(new Handler((r, ct) => { httpCalls++; return Task.FromResult(Json("""{"result":{"elements":[],"nextPage":null}}""")); }));
        var validClient = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var invalidOptionsForReporting = new QualtricsOptions(QualtricsRoot, MaxPages: 0);
        var connector = new QualtricsConnector(validClient, invalidOptionsForReporting);
        var results = await connector.DiagnoseAsync();
        Assert.Equal(DiagnosticOutcome.Failed, results[0].Outcome);
        Assert.Equal(DiagnosticOutcome.Skipped, results[1].Outcome);
        Assert.Equal(0, httpCalls);
    }

    // ---------- QLT-004: reusable paging ----------

    [Fact]
    public async Task CancellingMidEnumerationStopsPromptlyAfterTheAlreadyYieldedItemsWithoutFetchingTheNextPage()
    {
        // SDK-007 "cancellation after partial traversal": the first page's item must already have reached the
        // caller before cancellation takes effect, and no HTTP call for a subsequent page may follow.
        var calls = 0;
        using var cts = new CancellationTokenSource();
        using var h = new HttpClient(new Handler((r, ct) =>
        {
            calls++;
            return Task.FromResult(Json("""{"result":{"elements":[{"id":"SV_one"}],"nextPage":"https://qualtrics.test/API/v3/surveys?offset=1"}}"""));
        }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var surveys = new List<SurveySummary>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var s in c.EnumerateSurveysAsync(cts.Token))
            {
                surveys.Add(s);
                cts.Cancel(); // cancel only after the first item is already in hand
            }
        });

        Assert.Equal("SV_one", Assert.Single(surveys).Id);
        Assert.Equal(1, calls); // the second page's HTTP call never happened
    }

    [Fact]
    public async Task EmptyPageWithValidNextLinkContinuesToTheNextPage()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) =>
        {
            calls++;
            return Task.FromResult(calls == 1
                ? Json("""{"result":{"elements":[],"nextPage":"https://qualtrics.test/API/v3/surveys?offset=1"}}""")
                : Json("""{"result":{"elements":[{"id":"SV_two","name":"Second"}],"nextPage":null}}"""));
        }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var surveys = new List<SurveySummary>();
        await foreach (var s in c.EnumerateSurveysAsync()) surveys.Add(s);
        Assert.Equal(2, calls);
        Assert.Equal("SV_two", Assert.Single(surveys).Id);
    }

    [Fact]
    public async Task EnumerateSurveysCycleAndBoundsStillEnforcedAfterPagingExtraction()
    {
        using var h = new HttpClient(new Handler((r, ct) => Task.FromResult(Json("""{"result":{"elements":[],"nextPage":"https://qualtrics.test/API/v3/surveys?offset=same"}}"""))));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        await Assert.ThrowsAsync<InvalidDataException>(async () => { await foreach (var s in c.EnumerateSurveysAsync()) { } });
    }

    [Fact]
    public async Task SharedPagingHelperRejectsOffOriginLinkForContactsFamilyToo()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) =>
        { calls++; return Task.FromResult(Json("""{"result":{"elements":[],"nextPage":"https://evil.test/API/v3/directories/DIR_1/contacts"}}""")); }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        await Assert.ThrowsAsync<InvalidDataException>(async () => { await foreach (var contact in c.ListContactsAsync("DIR_1")) { } });
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ListSurveysPageAsyncSupportsPauseAndResumeWithoutRefetchingTheFirstPage()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) =>
        {
            calls++;
            if (calls == 1)
            {
                Assert.Equal("/API/v3/surveys", r.RequestUri!.AbsolutePath);
                return Task.FromResult(Json("""{"result":{"elements":[{"id":"SV_one"}],"nextPage":"https://qualtrics.test/API/v3/surveys?offset=1"}}"""));
            }
            Assert.Equal("?offset=1", r.RequestUri!.Query);
            return Task.FromResult(Json("""{"result":{"elements":[{"id":"SV_two"}],"nextPage":null}}"""));
        }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var page1 = await c.ListSurveysPageAsync();
        Assert.Equal("SV_one", Assert.Single(page1.Items).Id);
        Assert.NotNull(page1.Next);
        var page2 = await c.ListSurveysPageAsync(page1.Next);
        Assert.Equal("SV_two", Assert.Single(page2.Items).Id);
        Assert.Null(page2.Next);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ContactsContinuationCannotBeReplayedAgainstTheSurveysEndpoint()
    {
        using var h = new HttpClient(new Handler((r, ct) =>
            Task.FromResult(Json("""{"result":{"elements":[{"id":"SV_one"}],"nextPage":"https://qualtrics.test/API/v3/surveys?offset=1"}}"""))));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var page = await c.ListSurveysPageAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => c.ListContactsPageAsync("DIR_1", page.Next));
    }

    [Fact]
    public async Task ResumingAnExpiredCursorSurfacesTheRawProviderStatusWithoutRetryingOrHanging()
    {
        // QLT-004 "expired cursor": a well-formed, same-query continuation whose underlying cursor the provider has
        // since expired is a pagination failure, not a single-resource read - it must stay pinned to the raw
        // IntegrationRequestException contract (matching the read-vs-write/pagination split already established for
        // QLT-005/ADP-010), never silently retried and never miscategorized as a QualtricsApiException.
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) =>
        {
            calls++;
            return Task.FromResult(calls == 1
                ? Json("""{"result":{"elements":[{"id":"SV_one"}],"nextPage":"https://qualtrics.test/API/v3/surveys?offset=expired-cursor"}}""")
                : Json("expired-cursor-detail", HttpStatusCode.BadRequest));
        }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var page = await c.ListSurveysPageAsync();
        var error = await Assert.ThrowsAsync<IntegrationRequestException>(() => c.ListSurveysPageAsync(page.Next));
        Assert.Equal(HttpStatusCode.BadRequest, error.ResponseStatus);
        Assert.Equal(2, calls);
    }

    // ---------- QLT-013: bounded concurrency / throttle ----------

    [Fact]
    public async Task ConcurrencyLimiterBoundsSimultaneousOperations()
    {
        var limiter = new QualtricsConcurrencyLimiter(2);
        var active = 0; var maxActive = 0; var gate = new object();
        var tasks = Enumerable.Range(0, 6).Select(_ => limiter.RunAsync(async ct =>
        {
            lock (gate) { active++; maxActive = Math.Max(maxActive, active); }
            await Task.Delay(30, ct);
            lock (gate) { active--; }
            return true;
        }));
        await Task.WhenAll(tasks);
        Assert.True(maxActive <= 2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65)]
    public void ConcurrencyLimiterRejectsOutOfRangeConcurrency(int maxConcurrency)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new QualtricsConcurrencyLimiter(maxConcurrency));

    [Fact]
    public async Task CancellingWhileQueuedOnTheGateNeverStartsTheOperation()
    {
        var limiter = new QualtricsConcurrencyLimiter(1);
        var holderStarted = new TaskCompletionSource();
        var releaseHolder = new TaskCompletionSource();
        var holder = limiter.RunAsync(async ct =>
        {
            holderStarted.SetResult();
            await releaseHolder.Task;
            return true;
        });
        await holderStarted.Task;

        var queuedStarted = false;
        using var cts = new CancellationTokenSource();
        var queued = limiter.RunAsync(ct => { queuedStarted = true; return Task.FromResult(true); }, cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
        Assert.False(queuedStarted);

        releaseHolder.SetResult();
        Assert.True(await holder);
    }

    [Fact]
    public async Task LongRetryAfterOn429ThrowsCleanlyRatherThanWaiting()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) =>
        {
            calls++;
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromHours(1));
            return Task.FromResult(response);
        }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        await Assert.ThrowsAsync<IntegrationRequestException>(async () => { await foreach (var s in c.EnumerateSurveysAsync()) { } });
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task MalformedRetryAfterOn429FallsBackToBoundedBackoffRatherThanHanging()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) =>
        {
            calls++;
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.TryAddWithoutValidation("Retry-After", "not-a-valid-retry-after-value");
            return Task.FromResult(response);
        }));
        var transportOptions = new TransportOptions { MaxRetries = 1, BaseDelay = TimeSpan.FromMilliseconds(1), MaxDelay = TimeSpan.FromMilliseconds(50) };
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot), transportOptions);
        await Assert.ThrowsAsync<IntegrationRequestException>(async () => { await foreach (var s in c.EnumerateSurveysAsync()) { } });
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task StartResponseExportIsNeverAutomaticallyRetriedOnThrottle()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) => { calls++; return Task.FromResult(Json("{}", HttpStatusCode.TooManyRequests)); }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        await Assert.ThrowsAsync<IntegrationRequestException>(() => c.StartResponseExportAsync("SV_1"));
        Assert.Equal(1, calls);
    }

    // ---------- QLT-006 / QLT-009 / QLT-015: directory, contacts, mailing lists ----------

    [Fact]
    public async Task ListContactsPagesAndPreservesUnknownFieldsAndNullVersusOmission()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) =>
        {
            calls++;
            Assert.Equal(HttpMethod.Get, r.Method);
            return Task.FromResult(calls == 1
                ? Json("""{"result":{"elements":[{"id":"CID_1","email":"a@example.com","unsubscribed":null,"marketingOptOutDate":"2026-01-01"},{"id":"CID_2","email":"b@example.com"}],"nextPage":"https://qualtrics.test/API/v3/directories/DIR_1/contacts?offset=1"}}""")
                : Json("""{"result":{"elements":[],"nextPage":null}}"""));
        }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var contacts = new List<QualtricsContact>();
        await foreach (var contact in c.ListContactsAsync("DIR_1")) contacts.Add(contact);
        Assert.Equal(2, contacts.Count);
        Assert.Equal(JsonValueKind.Null, contacts[0].Unsubscribed.ValueKind); // present, explicit null
        Assert.Equal(JsonValueKind.Undefined, contacts[1].Unsubscribed.ValueKind); // entirely absent
        Assert.Equal("2026-01-01", contacts[0].AdditionalData!["marketingOptOutDate"].GetString());
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task CreateContactRequiresExplicitOptIn()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) => { calls++; return Task.FromResult(Json("""{"result":{"id":"CID_new"}}""")); }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var contact = new QualtricsContact { Email = "new@example.com" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.CreateContactAsync("DIR_1", contact));
        Assert.Equal(0, calls);
        var id = await c.CreateContactAsync("DIR_1", contact, writesEnabled: true);
        Assert.Equal("CID_new", id);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CreateContactRejectsMissingRequiredFieldLocallyBeforeAnyHttpCall()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) => { calls++; return Task.FromResult(Json("{}")); }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var contact = new QualtricsContact { FirstName = "NoContactInfo" };
        await Assert.ThrowsAsync<ArgumentException>(() => c.CreateContactAsync("DIR_1", contact, writesEnabled: true));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateContactRejectsInvalidDirectoryIdLocally()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) => { calls++; return Task.FromResult(Json("{}")); }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var contact = new QualtricsContact { Email = "x@example.com" };
        await Assert.ThrowsAsync<ArgumentException>(() => c.CreateContactAsync("../escape", contact, writesEnabled: true));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetContactClassifiesForbiddenAndNotFoundWithoutLeakingBody(HttpStatusCode status)
    {
        using var h = new HttpClient(new Handler((r, ct) => Task.FromResult(Json("private-detail", status))));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var ex = await Assert.ThrowsAsync<QualtricsApiException>(() => c.GetContactAsync("DIR_1", "CID_1"));
        Assert.Equal(status, ex.StatusCode);
        Assert.DoesNotContain("private-detail", ex.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetSurveyClassifiesForbiddenAndNotFoundWithoutLeakingBody(HttpStatusCode status)
    {
        // QLT-005/QLT-002: closes the "survey/export methods still throw the original IntegrationRequestException"
        // gap recorded when QualtricsApiException was first introduced for contacts/mailing lists only.
        using var h = new HttpClient(new Handler((r, ct) => Task.FromResult(Json("private-survey-detail", status))));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var ex = await Assert.ThrowsAsync<QualtricsApiException>(() => c.GetSurveyAsync("SV_private"));
        Assert.Equal(status, ex.StatusCode);
        Assert.DoesNotContain("private-survey-detail", ex.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetExportProgressClassifiesForbiddenAndNotFoundWithoutLeakingBody(HttpStatusCode status)
    {
        using var h = new HttpClient(new Handler((r, ct) => Task.FromResult(Json("private-progress-detail", status))));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var ex = await Assert.ThrowsAsync<QualtricsApiException>(() => c.GetExportProgressAsync("SV_1", "progress-1"));
        Assert.Equal(status, ex.StatusCode);
        Assert.DoesNotContain("private-progress-detail", ex.ToString());
    }

    [Fact]
    public async Task UpdateContactRequiresExplicitOptInAndSendsPut()
    {
        var calls = 0; HttpMethod? seenMethod = null;
        using var h = new HttpClient(new Handler((r, ct) => { calls++; seenMethod = r.Method; return Task.FromResult(Json("null")); }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var contact = new QualtricsContact { Email = "x@example.com" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.UpdateContactAsync("DIR_1", "CID_1", contact));
        Assert.Equal(0, calls);
        await c.UpdateContactAsync("DIR_1", "CID_1", contact, writesEnabled: true);
        Assert.Equal(HttpMethod.Put, seenMethod);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task CreateContactClassifiesForbiddenAndDuplicateExternalIdWithoutLeakingBody(HttpStatusCode status)
    {
        // QLT-006 "duplicate external IDs": a 409 Conflict is how Qualtrics would reject a contact create whose
        // extRef already exists in the directory; it must classify the same as any other write failure, not leak
        // the provider's response body, and never be silently retried (CreateContactAsync is a POST).
        using var h = new HttpClient(new Handler((r, ct) => Task.FromResult(Json("private-create-detail", status))));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var contact = new QualtricsContact { Email = "dup@example.com", ExternalReference = "ext-123" };
        var ex = await Assert.ThrowsAsync<QualtricsApiException>(() => c.CreateContactAsync("DIR_1", contact, writesEnabled: true));
        Assert.Equal(status, ex.StatusCode);
        Assert.DoesNotContain("private-create-detail", ex.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task UpdateContactClassifiesForbiddenAndNotFoundWithoutLeakingBody(HttpStatusCode status)
    {
        using var h = new HttpClient(new Handler((r, ct) => Task.FromResult(Json("private-update-detail", status))));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var contact = new QualtricsContact { Email = "x@example.com" };
        var ex = await Assert.ThrowsAsync<QualtricsApiException>(() => c.UpdateContactAsync("DIR_1", "CID_1", contact, writesEnabled: true));
        Assert.Equal(status, ex.StatusCode);
        Assert.DoesNotContain("private-update-detail", ex.ToString());
    }

    [Fact]
    public void ContactChangeOnlySerializesFieldsThatWereExplicitlySet()
    {
        // QLT-015: a ContactChange with only FirstName touched must serialize to a body containing exactly that
        // field - no lastName/email/etc. defaults leaking in as JSON nulls that would wipe the contact's existing
        // values on a PUT.
        var change = new ContactChange().SetFirstName("Updated");
        var json = JsonSerializer.Serialize(change);
        using var parsed = JsonDocument.Parse(json);
        Assert.Equal("Updated", parsed.RootElement.GetProperty("firstName").GetString());
        Assert.False(parsed.RootElement.TryGetProperty("lastName", out _));
        Assert.False(parsed.RootElement.TryGetProperty("email", out _));
        Assert.False(parsed.RootElement.TryGetProperty("unsubscribed", out _));
    }

    [Fact]
    public void ContactChangeCanExplicitlyClearAFieldToNullDistinctFromLeavingItUntouched()
    {
        var untouched = new ContactChange();
        var clearedToNull = new ContactChange().SetLanguage(null);
        Assert.False(untouched.HasAnyChange);
        Assert.True(clearedToNull.HasAnyChange);
        using var parsed = JsonDocument.Parse(JsonSerializer.Serialize(clearedToNull));
        Assert.Equal(JsonValueKind.Null, parsed.RootElement.GetProperty("language").ValueKind);
    }

    [Fact]
    public async Task UpdateContactAsyncWithContactChangeRequiresAtLeastOneFieldAndExplicitOptIn()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) => { calls++; return Task.FromResult(Json("null")); }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        await Assert.ThrowsAsync<ArgumentException>(() => c.UpdateContactAsync("DIR_1", "CID_1", new ContactChange(), writesEnabled: true));
        Assert.Equal(0, calls);
        var change = new ContactChange().SetEmail("new@example.com");
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.UpdateContactAsync("DIR_1", "CID_1", change));
        Assert.Equal(0, calls);
        await c.UpdateContactAsync("DIR_1", "CID_1", change, writesEnabled: true);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task UpdateContactAsyncWithContactChangeClassifiesForbiddenAndNotFoundWithoutLeakingBody(HttpStatusCode status)
    {
        using var h = new HttpClient(new Handler((r, ct) => Task.FromResult(Json("private-change-detail", status))));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var change = new ContactChange().SetFirstName("X");
        var ex = await Assert.ThrowsAsync<QualtricsApiException>(() => c.UpdateContactAsync("DIR_1", "CID_1", change, writesEnabled: true));
        Assert.Equal(status, ex.StatusCode);
        Assert.DoesNotContain("private-change-detail", ex.ToString());
    }

    [Fact]
    public async Task ListMailingListsPagesUsingTheSharedPagingHelper()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) => { calls++; return Task.FromResult(Json("""{"result":{"elements":[{"id":"ML_1","name":"VIP"}],"nextPage":null}}""")); }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var lists = new List<QualtricsMailingList>();
        await foreach (var l in c.ListMailingListsAsync("DIR_1")) lists.Add(l);
        Assert.Equal("ML_1", Assert.Single(lists).Id);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetMailingListClassifiesForbiddenAndNotFoundWithoutLeakingBody(HttpStatusCode status)
    {
        // QLT-009 "missing list" / "denied permission": GetMailingListAsync must classify the same way as
        // GetContactAsync/GetSurveyAsync, not leak the provider's response body.
        using var h = new HttpClient(new Handler((r, ct) => Task.FromResult(Json("private-list-detail", status))));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var ex = await Assert.ThrowsAsync<QualtricsApiException>(() => c.GetMailingListAsync("DIR_1", "ML_1"));
        Assert.Equal(status, ex.StatusCode);
        Assert.DoesNotContain("private-list-detail", ex.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task CreateMailingListClassifiesForbiddenAndConflictWithoutLeakingBody(HttpStatusCode status)
    {
        using var h = new HttpClient(new Handler((r, ct) => Task.FromResult(Json("private-list-create-detail", status))));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        var ex = await Assert.ThrowsAsync<QualtricsApiException>(() => c.CreateMailingListAsync("DIR_1", new QualtricsMailingList { Name = "VIP" }, writesEnabled: true));
        Assert.Equal(status, ex.StatusCode);
        Assert.DoesNotContain("private-list-create-detail", ex.ToString());
    }

    [Fact]
    public async Task CreateMailingListRequiresNameLocallyAndExplicitOptIn()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler((r, ct) => { calls++; return Task.FromResult(Json("""{"result":{"id":"ML_new"}}""")); }));
        var c = QualtricsClient.Create(h, new Token(), new(QualtricsRoot));
        await Assert.ThrowsAsync<ArgumentException>(() => c.CreateMailingListAsync("DIR_1", new QualtricsMailingList(), writesEnabled: true));
        Assert.Equal(0, calls);
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.CreateMailingListAsync("DIR_1", new QualtricsMailingList { Name = "VIP" }));
        Assert.Equal(0, calls);
        var id = await c.CreateMailingListAsync("DIR_1", new QualtricsMailingList { Name = "VIP" }, writesEnabled: true);
        Assert.Equal("ML_new", id);
        Assert.Equal(1, calls);
    }

    // ---------- QLT-012: data mapping ----------

    [Fact]
    public void SurveySummaryMapperPreservesUnknownFieldsAndOmitsRecordsWithoutAnIdentifier()
    {
        var mapper = new SurveySummaryMapper();
        var withId = JsonSerializer.Deserialize<SurveySummary>("""{"id":"SV_1","name":"X","future":true}""")!;
        var result = mapper.Map(withId);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Extensions["future"].GetBoolean());
        var withoutId = JsonSerializer.Deserialize<SurveySummary>("""{"name":"NoId"}""")!;
        Assert.True(mapper.Map(withoutId).IsOmitted);
    }

    [Fact]
    public void QualtricsContactMapperGoldenFixtureDistinguishesNullFromOmissionAtTheSourceDtoLevel()
    {
        var explicitNull = JsonSerializer.Deserialize<QualtricsContact>("""{"id":"CID_1","unsubscribed":null,"vipTier":"gold"}""")!;
        var absent = JsonSerializer.Deserialize<QualtricsContact>("""{"id":"CID_2"}""")!;
        Assert.Equal(JsonValueKind.Null, explicitNull.Unsubscribed.ValueKind); // present, explicit null
        Assert.Equal(JsonValueKind.Undefined, absent.Unsubscribed.ValueKind); // entirely absent

        var mapper = new QualtricsContactMapper();
        var mapped = mapper.Map(explicitNull);
        Assert.True(mapped.IsSuccess);
        Assert.Null(mapped.Value!.OptedOut);
        Assert.Equal("gold", mapped.Value.Extensions["vipTier"].GetString());
    }

    [Fact]
    public void QualtricsContactMapperFailsWithAFieldErrorWhenTheIdentifierIsMissing()
    {
        var noId = JsonSerializer.Deserialize<QualtricsContact>("""{"email":"a@example.com"}""")!;
        var result = new QualtricsContactMapper().Map(noId);
        Assert.False(result.IsSuccess);
        Assert.Equal("id", Assert.Single(result.Errors).Field);
    }

    [Fact]
    public void QualtricsContactMapperPreservesUnicodeNamesAndNestedEmbeddedDataKeys()
    {
        // QLT-012 "Unicode, embedded keys": a contact's typed name fields and its unknown "embeddedData" object
        // (itself holding arbitrary, possibly Unicode-keyed/valued fields Qualtrics customers configure) must survive
        // mapping byte-for-byte - neither transliterated, mangled, nor dropped as "just another unknown field".
        var source = JsonSerializer.Deserialize<QualtricsContact>("""
            {"id":"CID_unicode","firstName":"Ümit","lastName":"张","email":"a@example.com",
             "embeddedData":{"部門":"营销","note":"café 😀"}}
            """)!;
        var mapped = new QualtricsContactMapper().Map(source);
        Assert.True(mapped.IsSuccess);
        Assert.Equal("Ümit", mapped.Value!.FirstName);
        Assert.Equal("张", mapped.Value.LastName);
        var embeddedData = mapped.Value.Extensions["embeddedData"];
        Assert.Equal("营销", embeddedData.GetProperty("部門").GetString());
        Assert.Equal("café 😀", embeddedData.GetProperty("note").GetString());
    }
}
