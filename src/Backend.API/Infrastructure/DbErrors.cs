using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Backend.API.Infrastructure;

public static class DbErrors
{
    // Violación de índice único (por ejemplo, dos peticiones simultáneas que crean lo mismo).
    public static bool IsUniqueViolation(DbUpdateException exception, string? constraintName = null)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
            && (constraintName is null || string.Equals(pg.ConstraintName, constraintName, StringComparison.Ordinal));
    }

    // El registro tiene información relacionada (llave foránea).
    public static bool IsForeignKeyViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
    }
}
