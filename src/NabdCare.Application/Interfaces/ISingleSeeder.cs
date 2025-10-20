namespace NabdCare.Application.Interfaces;

/// <summary>
/// Interface for database seeding operations
/// </summary>
public interface ISingleSeeder
{
    /// <summary>
    /// Order in which this seeder should run (lower numbers run first)
    /// Default: 999 (runs last)
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Execute the seeding logic
    /// </summary>
    Task SeedAsync();
}