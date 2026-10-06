using Annotate.Markdown;

namespace Annotate.Plans.Application;

public sealed record RevisionBlock(
    string Key,
    BlockKind Kind,
    string SectionPath,
    int Start,
    int End,
    BlockChange Change,
    string ContentHash);