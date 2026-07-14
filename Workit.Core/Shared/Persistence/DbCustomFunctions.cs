using Microsoft.EntityFrameworkCore;

namespace Workit.Core.Shared.Persistence;

public static class DbCustomFunctions
{
    public static DateTime DateTrunc(string type, DateTime date)
        => throw new NotSupportedException();

    public static DateTime DateTrunc(string type, DateTimeOffset date)
        => throw new NotSupportedException();

    public static DateTime TimeZone(string timezone, DateTimeOffset date)
        => throw new NotSupportedException();

    public static DateTime Greatest(DateTimeOffset date1, DateTimeOffset date2)
        => throw new NotSupportedException();

    public static DateTime Least(DateTimeOffset date1, DateTimeOffset date2)
        => throw new NotSupportedException();

    public static int Mod(int value, int divisor)
        => throw new NotSupportedException();

    public static string[] SplitStringToArray(string value, string delimiter)
        => throw new NotSupportedException();
}

public static class DateTruncTypes
{
    public const string Day = "day";
    public const string Hour = "hour";
    public const string Month = "month";
    public const string Year = "year";
}

public static class ModelBuilderExtensions
{
    public static void MapDbFunctions(this ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(
                typeof(DbCustomFunctions).GetMethod(
                    nameof(DbCustomFunctions.DateTrunc),
                    [typeof(string), typeof(DateTime)])!)
            .HasName("date_trunc");

        modelBuilder.HasDbFunction(
                typeof(DbCustomFunctions).GetMethod(
                    nameof(DbCustomFunctions.DateTrunc),
                    [typeof(string), typeof(DateTimeOffset)])!)
            .HasName("date_trunc");

        modelBuilder.HasDbFunction(
                typeof(DbCustomFunctions).GetMethod(
                    nameof(DbCustomFunctions.Greatest),
                    [typeof(DateTimeOffset), typeof(DateTimeOffset)])!)
            .HasName("greatest");

        modelBuilder.HasDbFunction(
                typeof(DbCustomFunctions).GetMethod(
                    nameof(DbCustomFunctions.Least),
                    [typeof(DateTimeOffset), typeof(DateTimeOffset)])!)
            .HasName("least");

        modelBuilder.HasDbFunction(
                typeof(DbCustomFunctions).GetMethod(
                    nameof(DbCustomFunctions.TimeZone),
                    [typeof(string), typeof(DateTimeOffset)])!)
            .HasName("timezone");

        modelBuilder.HasDbFunction(
                typeof(DbCustomFunctions).GetMethod(
                    nameof(DbCustomFunctions.Mod),
                    [typeof(int), typeof(int)])!)
            .HasName("mod");

        modelBuilder.HasDbFunction(
                typeof(DbCustomFunctions).GetMethod(
                    nameof(DbCustomFunctions.SplitStringToArray),
                    [typeof(string), typeof(string)])!)
            .HasName("string_to_array");
    }
}
