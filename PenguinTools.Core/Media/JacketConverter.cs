﻿using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Media;

public class JacketConverter(IDiagnostic diag, IProgress<string>? prog = null) : ConverterBase(diag, prog)
{
    public required string InPath { get; init; }
    public required string OutPath { get; init; }

    protected async override Task ActionAsync(CancellationToken ct = default)
    {
        Progress?.Report(Strings.Status_Converting_jacket);
        ct.ThrowIfCancellationRequested();
        await Manipulate.ConvertJacketAsync(InPath, OutPath, ct);
        ct.ThrowIfCancellationRequested();
    }

    protected override Task ValidateAsync(CancellationToken ct = default)
    {
        if (!File.Exists(InPath)) Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, InPath);
        return Task.CompletedTask;
    }
}