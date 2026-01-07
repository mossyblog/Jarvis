using core.jarvis.data.RLS;
using Shouldly;
using Xunit;

namespace core.jarvis.data.tests.RLS
{
    /// <summary>
    /// Security tests for RLS expression validation.
    /// Verifies that SQL injection attempts are properly rejected.
    /// </summary>
    public class RLSExpressionValidatorTests
    {
        #region Valid Expression Tests

        [Theory]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("false")]
        [InlineData("FALSE")]
        public void Validate_BooleanLiteral_ReturnsSuccess(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);
            result.IsValid.ShouldBeTrue($"Expression '{expression}' should be valid");
        }

        [Theory]
        [InlineData("tenant_id = current_setting('app.tenant_id', true)::uuid")]
        [InlineData("user_id = current_setting('app.user_id', true)::uuid")]
        [InlineData("owner_id = current_setting('app.owner_id', false)::uuid")]
        public void Validate_CurrentSettingComparison_ReturnsSuccess(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);
            result.IsValid.ShouldBeTrue($"Expression '{expression}' should be valid");
        }

        [Theory]
        [InlineData("is_public = TRUE")]
        [InlineData("visible = true")]
        [InlineData("status = 'active'")]
        [InlineData("classification = 'public'")]
        public void Validate_SimpleLiteralComparison_ReturnsSuccess(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);
            result.IsValid.ShouldBeTrue($"Expression '{expression}' should be valid");
        }

        [Theory]
        [InlineData("classification IN ('public', 'internal')")]
        [InlineData("role IN ('admin', 'manager', 'user')")]
        [InlineData("status IN ('active')")]
        public void Validate_InClause_ReturnsSuccess(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);
            result.IsValid.ShouldBeTrue($"Expression '{expression}' should be valid");
        }

        [Theory]
        [InlineData("current_setting('app.role', true) IN ('admin', 'manager')")]
        [InlineData("current_setting('app.role', true) IN ('Admin', 'Manager', 'admin', 'manager')")]
        public void Validate_CurrentSettingInClause_ReturnsSuccess(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);
            result.IsValid.ShouldBeTrue($"Expression '{expression}' should be valid");
        }

        [Theory]
        [InlineData("owner_id = current_user_id()")]
        [InlineData("tenant_id = current_tenant_id()")]
        public void Validate_FunctionCallComparison_ReturnsSuccess(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);
            result.IsValid.ShouldBeTrue($"Expression '{expression}' should be valid");
        }

        [Fact]
        public void Validate_ComplexExpressionWithAndOr_ReturnsSuccess()
        {
            var expression = "tenant_id = current_setting('app.tenant_id', true)::uuid AND (user_id = current_setting('app.user_id', true)::uuid OR is_public = TRUE)";

            var result = RLSExpressionValidator.Validate(expression);
            result.IsValid.ShouldBeTrue($"Expression should be valid");
        }

        [Fact]
        public void Validate_ComplexRoleBasedExpression_ReturnsSuccess()
        {
            var expression = @"tenant_id = current_setting('app.tenant_id', true)::uuid AND (
                current_setting('app.role', true) IN ('admin', 'Admin') OR
                (current_setting('app.role', true) IN ('manager', 'Manager') AND classification IN ('public', 'internal', 'confidential')) OR
                classification = 'public'
            )";

            var result = RLSExpressionValidator.Validate(expression);
            result.IsValid.ShouldBeTrue($"Complex role-based expression should be valid");
        }

        [Fact]
        public void Validate_NullOrEmptyExpression_ReturnsSuccess()
        {
            RLSExpressionValidator.Validate(null).IsValid.ShouldBeTrue();
            RLSExpressionValidator.Validate("").IsValid.ShouldBeTrue();
            RLSExpressionValidator.Validate("   ").IsValid.ShouldBeTrue();
        }

        #endregion

        #region SQL Injection Attack Tests

        [Theory]
        [InlineData("true; DROP TABLE users;")]
        [InlineData("tenant_id = '1'; DROP TABLE accounts;")]
        [InlineData("1=1; DELETE FROM users;")]
        public void Validate_SemicolonInjection_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("true -- malicious comment")]
        [InlineData("tenant_id = '1' -- DROP TABLE users")]
        [InlineData("1=1--")]
        public void Validate_CommentInjection_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("true /* comment */")]
        [InlineData("tenant_id = '1' /* DROP TABLE */")]
        [InlineData("/* malicious */ true")]
        public void Validate_BlockCommentInjection_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("DROP TABLE users")]
        [InlineData("true AND DROP POLICY test")]
        [InlineData("CREATE TABLE evil")]
        [InlineData("ALTER TABLE users")]
        [InlineData("TRUNCATE users")]
        public void Validate_DDLKeywords_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("GRANT ALL ON users TO attacker")]
        [InlineData("REVOKE SELECT ON users FROM public")]
        public void Validate_PrivilegeCommands_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("INSERT INTO users VALUES (1)")]
        [InlineData("UPDATE users SET role = 'admin'")]
        [InlineData("DELETE FROM audit_log")]
        public void Validate_DMLStatements_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("EXECUTE 'DROP TABLE users'")]
        [InlineData("EXEC sp_executesql")]
        public void Validate_ExecuteCommands_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("COPY users TO '/tmp/data'")]
        [InlineData("true AND COPY data FROM '/etc/passwd'")]
        public void Validate_CopyCommands_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("BEGIN; DROP TABLE users; COMMIT")]
        [InlineData("ROLLBACK")]
        [InlineData("SAVEPOINT evil")]
        public void Validate_TransactionCommands_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("pg_sleep(10)")]
        [InlineData("true AND pg_terminate_backend(123)")]
        [InlineData("pg_cancel_backend(123)")]
        public void Validate_SystemFunctions_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("$$DROP TABLE users$$")]
        [InlineData("$tag$malicious$tag$")]
        [InlineData("true AND $$evil$$")]
        public void Validate_DollarQuoting_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("tenant_id = '1' UNION SELECT * FROM users")]
        [InlineData("true UNION ALL SELECT password FROM credentials")]
        public void Validate_UnionInjection_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("\\x41\\x42\\x43")]
        [InlineData("\\u0041\\u0042")]
        public void Validate_EscapeSequences_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Fact]
        public void Validate_NewlineWithDDL_ReturnsDangerousPattern()
        {
            var expression = "true\nDROP TABLE users";

            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse("Expression with newline followed by DDL should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Theory]
        [InlineData("SET role = 'admin'")]
        [InlineData("true AND SET session = 'hack'")]
        public void Validate_SetCommands_ReturnsDangerousPattern(string expression)
        {
            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        #endregion

        #region Malformed Expression Tests

        [Fact]
        public void Validate_UnbalancedParentheses_ReturnsFailure()
        {
            var expression = "tenant_id = current_setting('app.tenant_id', true)::uuid AND (user_id = '1'";

            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse("Unbalanced parentheses should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.MalformedExpression);
        }

        [Fact]
        public void Validate_ExtraClosingParenthesis_ReturnsFailure()
        {
            var expression = "tenant_id = '1')";

            var result = RLSExpressionValidator.Validate(expression);

            result.IsValid.ShouldBeFalse("Extra closing parenthesis should be rejected");
            result.FailureReason.ShouldBe(RLSValidationFailureReason.MalformedExpression);
        }

        #endregion

        #region ValidateOrThrow Tests

        [Fact]
        public void ValidateOrThrow_WithValidExpression_DoesNotThrow()
        {
            var expression = "tenant_id = current_setting('app.tenant_id', true)::uuid";

            // Should not throw
            RLSExpressionValidator.ValidateOrThrow(expression);
        }

        [Fact]
        public void ValidateOrThrow_WithInjectionAttempt_ThrowsValidationException()
        {
            var expression = "true; DROP TABLE users;";

            var ex = Assert.Throws<RLSExpressionValidationException>(() =>
                RLSExpressionValidator.ValidateOrThrow(expression, "USING expression"));

            ex.Message.ShouldContain("USING expression");
            ex.Message.ShouldContain("failed validation");
            ex.Expression.ShouldBe(expression);
            ex.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
        }

        [Fact]
        public void ValidateOrThrow_WithCustomExpressionName_IncludesNameInException()
        {
            var expression = "DROP TABLE users";

            var ex = Assert.Throws<RLSExpressionValidationException>(() =>
                RLSExpressionValidator.ValidateOrThrow(expression, "WITH CHECK expression for policy 'test_policy'"));

            ex.Message.ShouldContain("WITH CHECK expression for policy 'test_policy'");
        }

        #endregion

        #region Integration with RLSMigrationHelper Tests

        [Fact]
        public void RLSMigrationHelper_WithMaliciousUsingExpression_ThrowsValidationException()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "users",
                Type = PolicyType.Select,
                PolicyName = "malicious_policy",
                UsingExpression = "true; DROP TABLE users;" // SQL injection attempt
            });

            var conn = new Npgsql.NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
            var pgClient = new PgClient(conn, registry);
            var helper = new RLSMigrationHelper(pgClient, registry);

            // Act & Assert
            var ex = Assert.Throws<RLSExpressionValidationException>(() =>
                helper.GenerateMigrationScript());

            ex.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
            ex.Message.ShouldContain("USING expression");
            ex.Message.ShouldContain("malicious_policy");
        }

        [Fact]
        public void RLSMigrationHelper_WithMaliciousWithCheckExpression_ThrowsValidationException()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "users",
                Type = PolicyType.Insert,
                PolicyName = "malicious_check_policy",
                UsingExpression = "true",
                WithCheckExpression = "true -- DROP TABLE users" // Comment injection attempt
            });

            var conn = new Npgsql.NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
            var pgClient = new PgClient(conn, registry);
            var helper = new RLSMigrationHelper(pgClient, registry);

            // Act & Assert
            var ex = Assert.Throws<RLSExpressionValidationException>(() =>
                helper.GenerateMigrationScript());

            ex.FailureReason.ShouldBe(RLSValidationFailureReason.DangerousPattern);
            ex.Message.ShouldContain("WITH CHECK expression");
        }

        [Fact]
        public void RLSMigrationHelper_WithValidExpressions_GeneratesScript()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "secure_table",
                Type = PolicyType.All,
                PolicyName = "secure_policy",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid",
                WithCheckExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            });

            var conn = new Npgsql.NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
            var pgClient = new PgClient(conn, registry);
            var helper = new RLSMigrationHelper(pgClient, registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldContain("CREATE POLICY \"secure_policy\"");
            script.ShouldContain("USING (tenant_id = current_setting('app.tenant_id', true)::uuid)");
            script.ShouldContain("WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid)");
        }

        [Theory]
        [InlineData("DROP POLICY existing ON users")]
        [InlineData("true; CREATE USER attacker")]
        [InlineData("tenant_id = '1' /* UNION SELECT * FROM credentials */")]
        [InlineData("1=1; GRANT ALL PRIVILEGES TO attacker")]
        [InlineData("true$$ DROP TABLE users $$")]
        public void RLSMigrationHelper_WithVariousInjectionAttempts_RejectsAll(string maliciousExpression)
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "target_table",
                Type = PolicyType.Select,
                PolicyName = "attack_policy",
                UsingExpression = maliciousExpression
            });

            var conn = new Npgsql.NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
            var pgClient = new PgClient(conn, registry);
            var helper = new RLSMigrationHelper(pgClient, registry);

            // Act & Assert
            Assert.Throws<RLSExpressionValidationException>(() =>
                helper.GenerateMigrationScript());
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Validate_ExpressionWithAllowedSpecialCharacters_ReturnsSuccess()
        {
            // Test that legitimate expressions with various operators work
            var expression = "tenant_id = current_setting('app.tenant_id', true)::uuid AND status <> 'deleted'";

            var result = RLSExpressionValidator.Validate(expression);
            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Validate_CaseSensitivity_DetectsKeywordsInAnyCase()
        {
            var expressions = new[]
            {
                "drop TABLE users",
                "DROP table users",
                "dRoP TaBlE users",
                "true; delete from users",
                "UNION select * from users"
            };

            foreach (var expression in expressions)
            {
                var result = RLSExpressionValidator.Validate(expression);
                result.IsValid.ShouldBeFalse($"Expression '{expression}' should be rejected regardless of case");
            }
        }

        [Fact]
        public void Validate_LegitimateCurrentSettingWithSet_DoesNotFalsePositive()
        {
            // Ensure current_setting doesn't trigger false positive for SET detection
            var expression = "tenant_id = current_setting('app.tenant_id', true)::uuid";

            var result = RLSExpressionValidator.Validate(expression);
            result.IsValid.ShouldBeTrue("Legitimate current_setting should not be rejected");
        }

        #endregion
    }
}
