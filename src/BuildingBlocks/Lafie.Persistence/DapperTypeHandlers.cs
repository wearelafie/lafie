using System.Data;
using Dapper;

namespace Lafie.Persistence;

/// <summary>
/// Handlers Dapper pour <see cref="DateOnly"/>/<see cref="TimeOnly"/> : Dapper ne les accepte pas
/// nativement en paramètre. Npgsql, lui, mappe `date`/`time`. Enregistrés une seule fois.
/// </summary>
public static class DapperTypeHandlers
{
    private static bool _registered;
    private static readonly object Gate = new();

    public static void Register()
    {
        if (_registered) return;
        lock (Gate)
        {
            if (_registered) return;
            SqlMapper.AddTypeHandler(new DateOnlyHandler());
            SqlMapper.AddTypeHandler(new TimeOnlyHandler());
            _registered = true;
        }
    }

    private sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value = value; // Npgsql gère DateOnly -> date
        }

        public override DateOnly Parse(object value) => value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => DateOnly.Parse((string)value),
        };
    }

    private sealed class TimeOnlyHandler : SqlMapper.TypeHandler<TimeOnly>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly value)
        {
            parameter.DbType = DbType.Time;
            parameter.Value = value;
        }

        public override TimeOnly Parse(object value) => value switch
        {
            TimeOnly t => t,
            DateTime dt => TimeOnly.FromDateTime(dt),
            TimeSpan ts => TimeOnly.FromTimeSpan(ts),
            _ => TimeOnly.Parse((string)value),
        };
    }
}
