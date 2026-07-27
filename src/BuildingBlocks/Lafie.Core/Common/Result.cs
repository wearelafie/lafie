namespace Lafie.Core.Common;

/// <summary>Résultat d'opération (succès/échec) sans exceptions pour le flux métier.</summary>
public class Result
{
    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null) throw new InvalidOperationException("Un succes ne porte pas d'erreur.");
        if (!isSuccess && string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException("Un echec doit porter une erreur.");
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;
    internal Result(T? value, bool isSuccess, string? error) : base(isSuccess, error) => _value = value;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Pas de valeur sur un resultat en echec.");
}
