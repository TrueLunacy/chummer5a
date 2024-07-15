using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace RecordSourceGenerator
{
    public sealed record Model(string? Namespace,
        string ClassIdentifier,
        string XmlElementName,
        bool IsStruct,
        ImmutableArray<Parameter> Params)
    {
        public bool Equals(Model? other)
        {
            return other is not null
                && Namespace == other.Namespace
                && ClassIdentifier == other.ClassIdentifier
                && IsStruct == other.IsStruct
                && Params.SequenceEqual(other.Params);
        }

        public override int GetHashCode()
        {
            return (Namespace?.GetHashCode() ?? 0)
                ^ ClassIdentifier.GetHashCode()
                ^ IsStruct.GetHashCode()
                ^ Params.Select(p => p.GetHashCode()).Aggregate((l, r) => l ^ r);
        }
    }
}
