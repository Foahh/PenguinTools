﻿using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Media;
using PenguinTools.Core.Resources;
using PenguinTools.Models;
using System.IO;

namespace PenguinTools.ViewModels;

public class MusicViewModel : WatchViewModel<MusicModel>
{
    protected override Task<MusicModel> ReadModel(string path, IDiagnostic diag, IProgress<string>? prog = null, CancellationToken ct = default)
    {
        var model = new MusicModel();
        model.Meta.BgmFilePath = ModelPath;
        return Task.FromResult(model);
    }

    protected override bool CanRun()
    {
        return !string.IsNullOrWhiteSpace(ModelPath);
    }

    protected async override Task Action(IDiagnostic diag, IProgress<string>? prog = null, CancellationToken ct = default)
    {
        if (Model?.Id is null) throw new DiagnosticException(Strings.Error_Song_id_is_not_set);

        var dlg = new OpenFolderDialog
        {
            InitialDirectory = Path.GetDirectoryName((string?)ModelPath),
            Title = Strings.Title_Select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (dlg.ShowDialog() != true) return;
        var path = dlg.FolderName;

        var converter = new MusicConverter(diag, prog)
        {
            Meta = Model.Meta,
            OutFolder = path
        };
        await converter.ConvertAsync(ct);
    }
}