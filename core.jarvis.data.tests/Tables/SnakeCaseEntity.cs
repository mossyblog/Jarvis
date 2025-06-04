namespace core.jarvis.data.tests.Tables
{
    /// <summary>
    /// INTENT: Represents a snake_case mapped entity for integration tests.
    /// PURPOSE: Used to validate convention-based mapping in PgClient.
    /// BUSINESS CONTEXT: Ensures entities can be mapped without attributes.
    /// WHY IMPORTANT: Reduces boilerplate and enforces naming consistency.
    /// ARCHITECTURAL SIGNIFICANCE: Used by all snake_case mapping tests.
    /// FUTURE RESILIENCE: Protects against regressions in mapping logic.
    /// </summary>
    public class SnakeCaseEntity
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}