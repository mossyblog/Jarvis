using Npgsql;
using Shouldly;

namespace core.jarvis.data.tests.Tables
{
    /// <summary>
    /// INTENT: Validate that C# PascalCase properties automatically map to PostgreSQL snake_case columns without attributes.
    /// PURPOSE: Ensure clean C# record types work seamlessly with PostgreSQL naming conventions.
    /// BUSINESS CONTEXT: Allows developers to write idiomatic C# code without database concerns.
    /// WHY IMPORTANT: Prevents the need for Column attributes or other decorations on IComponent types.
    /// ARCHITECTURAL SIGNIFICANCE: Maintains separation between domain models and database schema.
    /// FUTURE RESILIENCE: Protects against regression when adding new component types with complex property names.
    /// </summary>
    public class PascalToSnakeCaseMappingTests : IAsyncLifetime
    {
        private NpgsqlConnection _conn = null!;
        private PgClient _client = null!;

        public async Task InitializeAsync()
        {
            var connString = TestHelpers.GetConnectionStringFromEnv();
            _conn = new NpgsqlConnection(connString);
            _client = await PgClientFactory.Create(_conn);

            // Create test table with various snake_case column patterns
            var createTableSql = @"
                DROP TABLE IF EXISTS complex_mapping_entity;
                CREATE TABLE complex_mapping_entity (
                    id SERIAL PRIMARY KEY,
                    is_active BOOLEAN NOT NULL,
                    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                    has_children BOOLEAN NOT NULL DEFAULT FALSE,
                    can_edit BOOLEAN NOT NULL DEFAULT TRUE,
                    should_notify BOOLEAN NOT NULL DEFAULT TRUE,
                    xml_content TEXT,
                    json_data TEXT,
                    html_template TEXT,
                    api_key TEXT,
                    url_path TEXT,
                    io_operation TEXT,
                    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP,
                    deleted_at TIMESTAMP,
                    first_name TEXT NOT NULL,
                    last_name TEXT NOT NULL,
                    middle_name TEXT,
                    phone_number TEXT,
                    email_address TEXT NOT NULL,
                    street_address TEXT,
                    postal_code TEXT,
                    country_code TEXT,
                    tax_id_number TEXT,
                    social_security_number TEXT,
                    drivers_license_number TEXT,
                    passport_number TEXT,
                    credit_card_number TEXT,
                    bank_account_number TEXT,
                    routing_number TEXT,
                    swift_code TEXT,
                    iban_number TEXT,
                    company_registration_number TEXT,
                    vat_number TEXT,
                    employee_id TEXT,
                    department_id TEXT,
                    manager_id TEXT,
                    project_id TEXT,
                    customer_id TEXT,
                    order_id TEXT,
                    invoice_id TEXT,
                    transaction_id TEXT,
                    reference_id TEXT,
                    tracking_id TEXT,
                    session_id TEXT,
                    request_id TEXT,
                    correlation_id TEXT,
                    max_retry_count INTEGER DEFAULT 3,
                    current_retry_count INTEGER DEFAULT 0,
                    timeout_seconds INTEGER DEFAULT 30,
                    batch_size INTEGER DEFAULT 100,
                    page_size INTEGER DEFAULT 20,
                    total_count BIGINT DEFAULT 0,
                    success_count BIGINT DEFAULT 0,
                    error_count BIGINT DEFAULT 0,
                    warning_count BIGINT DEFAULT 0,
                    info_count BIGINT DEFAULT 0,
                    debug_count BIGINT DEFAULT 0,
                    trace_count BIGINT DEFAULT 0
                );";
            using (var cmd = new NpgsqlCommand(createTableSql, _conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Reset sequence
            var resetSeqSql = "ALTER SEQUENCE IF EXISTS complex_mapping_entity_id_seq RESTART WITH 1;";
            using (var cmd = new NpgsqlCommand(resetSeqSql, _conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task DisposeAsync()
        {
            var dropSql = "DROP TABLE IF EXISTS complex_mapping_entity;";
            using var cmd = new NpgsqlCommand(dropSql, _conn);
            await cmd.ExecuteNonQueryAsync();
            await _conn.CloseAsync();
        }

        /// <summary>
        /// INTENT: Test that boolean properties starting with Is/Has/Can/Should map correctly.
        /// PURPOSE: Ensure common boolean naming patterns work without attributes.
        /// </summary>
        [Fact]
        public async Task BooleanProperties_WithCommonPrefixes_MapCorrectly()
        {
            // Arrange
            var entity = new ComplexMappingEntity
            {
                IsActive = true,
                IsDeleted = false,
                HasChildren = true,
                CanEdit = false,
                ShouldNotify = true,
                FirstName = "Test",
                LastName = "User",
                EmailAddress = "test@example.com"
            };

            // Act
            await _client.From<ComplexMappingEntity>().Insert(entity);
            var results = await _client.From<ComplexMappingEntity>()
                .Filter("email_address", "eq", entity.EmailAddress)
                .Get();

            // Assert
            results.ShouldNotBeEmpty();
            var retrieved = results[0];
            retrieved.IsActive.ShouldBe(true);
            retrieved.IsDeleted.ShouldBe(false);
            retrieved.HasChildren.ShouldBe(true);
            retrieved.CanEdit.ShouldBe(false);
            retrieved.ShouldNotify.ShouldBe(true);
        }

        /// <summary>
        /// INTENT: Test that acronyms in property names map correctly (XMLContent -> xml_content).
        /// PURPOSE: Ensure uppercase acronyms are properly converted to lowercase snake_case.
        /// </summary>
        [Fact]
        public async Task AcronymProperties_MapToLowercaseSnakeCase()
        {
            // Arrange
            var entity = new ComplexMappingEntity
            {
                XMLContent = "<test>data</test>",
                JSONData = """{"key": "value"}""",
                HTMLTemplate = "<div>Hello</div>",
                APIKey = "secret-key-123",
                URLPath = "/api/v1/users",
                IOOperation = "READ",
                FirstName = "Acronym",
                LastName = "Test",
                EmailAddress = "acronym@test.com"
            };

            // Act
            await _client.From<ComplexMappingEntity>().Insert(entity);
            var results = await _client.From<ComplexMappingEntity>()
                .Filter("email_address", "eq", entity.EmailAddress)
                .Get();

            // Assert
            results.ShouldNotBeEmpty();
            var retrieved = results[0];
            retrieved.XMLContent.ShouldBe("<test>data</test>");
            retrieved.JSONData.ShouldBe("""{"key": "value"}""");
            retrieved.HTMLTemplate.ShouldBe("<div>Hello</div>");
            retrieved.APIKey.ShouldBe("secret-key-123");
            retrieved.URLPath.ShouldBe("/api/v1/users");
            retrieved.IOOperation.ShouldBe("READ");
        }

        /// <summary>
        /// INTENT: Test that compound words with ID suffix map correctly (EmployeeID -> employee_id).
        /// PURPOSE: Ensure ID fields follow snake_case conventions.
        /// </summary>
        [Fact]
        public async Task CompoundIDProperties_MapCorrectly()
        {
            // Arrange
            var entity = new ComplexMappingEntity
            {
                EmployeeID = "EMP-001",
                DepartmentID = "DEPT-IT",
                ManagerID = "MGR-100",
                ProjectID = "PROJ-2024",
                CustomerID = "CUST-5000",
                OrderID = "ORD-12345",
                InvoiceID = "INV-67890",
                TransactionID = "TXN-ABCDEF",
                ReferenceID = "REF-XYZ",
                TrackingID = "TRK-111",
                SessionID = "SESS-222",
                RequestID = "REQ-333",
                CorrelationID = "CORR-444",
                FirstName = "ID",
                LastName = "Test",
                EmailAddress = "id@test.com"
            };

            // Act
            await _client.From<ComplexMappingEntity>().Insert(entity);
            var results = await _client.From<ComplexMappingEntity>()
                .Filter("email_address", "eq", entity.EmailAddress)
                .Get();

            // Assert
            results.ShouldNotBeEmpty();
            var retrieved = results[0];
            retrieved.EmployeeID.ShouldBe("EMP-001");
            retrieved.DepartmentID.ShouldBe("DEPT-IT");
            retrieved.ManagerID.ShouldBe("MGR-100");
            retrieved.ProjectID.ShouldBe("PROJ-2024");
        }

        /// <summary>
        /// INTENT: Test that multi-word properties map correctly (FirstName -> first_name).
        /// PURPOSE: Ensure standard camelCase properties work with snake_case columns.
        /// </summary>
        [Fact]
        public async Task MultiWordProperties_MapToSnakeCase()
        {
            // Arrange
            var entity = new ComplexMappingEntity
            {
                FirstName = "John",
                LastName = "Doe",
                MiddleName = "William",
                PhoneNumber = "+1-555-0123",
                EmailAddress = "john.doe@example.com",
                StreetAddress = "123 Main St",
                PostalCode = "12345",
                CountryCode = "US",
                TaxIDNumber = "123-45-6789",
                SocialSecurityNumber = "987-65-4321",
                DriversLicenseNumber = "DL-123456",
                PassportNumber = "P-7890123",
                CreditCardNumber = "4111-1111-1111-1111",
                BankAccountNumber = "1234567890",
                RoutingNumber = "021000021",
                SwiftCode = "CHASUS33",
                IBANNumber = "GB82WEST12345698765432",
                CompanyRegistrationNumber = "CRN-001",
                VATNumber = "VAT-123"
            };

            // Act
            await _client.From<ComplexMappingEntity>().Insert(entity);
            var results = await _client.From<ComplexMappingEntity>()
                .Filter("email_address", "eq", entity.EmailAddress)
                .Get();

            // Assert
            results.ShouldNotBeEmpty();
            var retrieved = results[0];
            retrieved.FirstName.ShouldBe("John");
            retrieved.LastName.ShouldBe("Doe");
            retrieved.MiddleName.ShouldBe("William");
            retrieved.PhoneNumber.ShouldBe("+1-555-0123");
            retrieved.TaxIDNumber.ShouldBe("123-45-6789");
            retrieved.VATNumber.ShouldBe("VAT-123");
        }

        /// <summary>
        /// INTENT: Test that numeric properties with Count suffix map correctly.
        /// PURPOSE: Ensure counter fields work with snake_case naming.
        /// </summary>
        [Fact]
        public async Task NumericCountProperties_MapCorrectly()
        {
            // Arrange
            var entity = new ComplexMappingEntity
            {
                MaxRetryCount = 5,
                CurrentRetryCount = 2,
                TimeoutSeconds = 60,
                BatchSize = 50,
                PageSize = 25,
                TotalCount = 1000,
                SuccessCount = 950,
                ErrorCount = 30,
                WarningCount = 15,
                InfoCount = 5,
                DebugCount = 0,
                TraceCount = 0,
                FirstName = "Counter",
                LastName = "Test",
                EmailAddress = "counter@test.com"
            };

            // Act
            await _client.From<ComplexMappingEntity>().Insert(entity);
            var results = await _client.From<ComplexMappingEntity>()
                .Filter("email_address", "eq", entity.EmailAddress)
                .Get();

            // Assert
            results.ShouldNotBeEmpty();
            var retrieved = results[0];
            retrieved.MaxRetryCount.ShouldBe(5);
            retrieved.CurrentRetryCount.ShouldBe(2);
            retrieved.TimeoutSeconds.ShouldBe(60);
            retrieved.BatchSize.ShouldBe(50);
            retrieved.PageSize.ShouldBe(25);
            retrieved.TotalCount.ShouldBe(1000);
            retrieved.SuccessCount.ShouldBe(950);
            retrieved.ErrorCount.ShouldBe(30);
        }

        /// <summary>
        /// INTENT: Test DateTime properties with At suffix (CreatedAt -> created_at).
        /// PURPOSE: Ensure timestamp fields map correctly.
        /// </summary>
        [Fact]
        public async Task DateTimeProperties_WithAtSuffix_MapCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var entity = new ComplexMappingEntity
            {
                FirstName = "DateTime",
                LastName = "Test",
                EmailAddress = "datetime@test.com",
                UpdatedAt = now,
                DeletedAt = now.AddDays(1)
            };

            // Act
            await _client.From<ComplexMappingEntity>().Insert(entity);
            var results = await _client.From<ComplexMappingEntity>()
                .Filter("email_address", "eq", entity.EmailAddress)
                .Get();

            // Assert
            results.ShouldNotBeEmpty();
            var retrieved = results[0];
            retrieved.UpdatedAt.HasValue.ShouldBeTrue();
            retrieved.UpdatedAt.Value.ShouldBeInRange(now.AddSeconds(-1), now.AddSeconds(1));
            retrieved.DeletedAt.HasValue.ShouldBeTrue();
            retrieved.DeletedAt.Value.ShouldBeInRange(now.AddDays(1).AddSeconds(-1), now.AddDays(1).AddSeconds(1));
            // CreatedAt should be set by database default, but since we're not returning it after insert,
            // we can't verify it here. In a real scenario, you'd need to fetch the record again or use RETURNING clause.
        }

        /// <summary>
        /// INTENT: Verify the StringExtensions.ToSnakeCase method handles all edge cases.
        /// PURPOSE: Ensure the core mapping function works correctly.
        /// </summary>
        [Theory]
        [InlineData("FirstName", "first_name")]
        [InlineData("IsActive", "is_active")]
        [InlineData("XMLContent", "xml_content")]
        [InlineData("HTMLParser", "html_parser")]
        [InlineData("IOOperation", "io_operation")]
        [InlineData("CustomerID", "customer_id")]
        [InlineData("URLPath", "url_path")]
        [InlineData("APIKey", "api_key")]
        [InlineData("JSONData", "json_data")]
        [InlineData("VATNumber", "vat_number")]
        [InlineData("IBANNumber", "iban_number")]
        [InlineData("A", "a")]
        [InlineData("AB", "ab")]
        [InlineData("ABC", "abc")]
        [InlineData("ABCDef", "abc_def")]
        [InlineData("IOError", "io_error")]
        [InlineData("ReadXMLFromHTTPSURL", "read_xml_from_httpsurl")]
        public void ToSnakeCase_ConvertsCorrectly(string input, string expected)
        {
            // Act
            var result = input.ToSnakeCase();

            // Assert
            result.ShouldBe(expected);
        }
    }

    // Test entity with complex property names
    public record ComplexMappingEntity
    {
        public int Id { get; set; }
        
        // Boolean properties with common prefixes
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool HasChildren { get; set; }
        public bool CanEdit { get; set; }
        public bool ShouldNotify { get; set; }
        
        // Properties with acronyms
        public string? XMLContent { get; set; }
        public string? JSONData { get; set; }
        public string? HTMLTemplate { get; set; }
        public string? APIKey { get; set; }
        public string? URLPath { get; set; }
        public string? IOOperation { get; set; }
        
        // DateTime properties
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        
        // Multi-word properties
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? MiddleName { get; set; }
        public string? PhoneNumber { get; set; }
        public string EmailAddress { get; set; } = "";
        public string? StreetAddress { get; set; }
        public string? PostalCode { get; set; }
        public string? CountryCode { get; set; }
        
        // Properties with ID suffix
        public string? TaxIDNumber { get; set; }
        public string? SocialSecurityNumber { get; set; }
        public string? DriversLicenseNumber { get; set; }
        public string? PassportNumber { get; set; }
        public string? CreditCardNumber { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? RoutingNumber { get; set; }
        public string? SwiftCode { get; set; }
        public string? IBANNumber { get; set; }
        public string? CompanyRegistrationNumber { get; set; }
        public string? VATNumber { get; set; }
        public string? EmployeeID { get; set; }
        public string? DepartmentID { get; set; }
        public string? ManagerID { get; set; }
        public string? ProjectID { get; set; }
        public string? CustomerID { get; set; }
        public string? OrderID { get; set; }
        public string? InvoiceID { get; set; }
        public string? TransactionID { get; set; }
        public string? ReferenceID { get; set; }
        public string? TrackingID { get; set; }
        public string? SessionID { get; set; }
        public string? RequestID { get; set; }
        public string? CorrelationID { get; set; }
        
        // Numeric properties with Count suffix
        public int MaxRetryCount { get; set; }
        public int CurrentRetryCount { get; set; }
        public int TimeoutSeconds { get; set; }
        public int BatchSize { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public long SuccessCount { get; set; }
        public long ErrorCount { get; set; }
        public long WarningCount { get; set; }
        public long InfoCount { get; set; }
        public long DebugCount { get; set; }
        public long TraceCount { get; set; }
    }
}