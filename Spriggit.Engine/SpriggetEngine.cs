using System.IO.Abstractions;
using Noggog;
using Noggog.IO;
using Noggog.WorkEngine;
using Serilog;
using Spriggit.Core;

namespace Spriggit.Engine;

public class SpriggitEngine(
    IFileSystem fileSystem,
    IWorkDropoff? workDropoff,
    ICreateStream? createStream,
    EntryPointCache entryPointCache,
    SpriggitMetaLocator spriggitMetaLocator,
    ILogger logger,
    GetMetaToUse getMetaToUse,
    SerializeBlocker serializeBlocker,
    CurrentVersionsProvider currentVersionsProvider)
{
    public async Task Serialize(
        FilePath bethesdaPluginPath, 
        DirectoryPath outputFolder, 
        SpriggitMeta? meta,
        CancellationToken cancel)
    {
        logger.Information("Spriggit version {Version}", currentVersionsProvider.SpriggitVersion);
        
        serializeBlocker.CheckAndBlock(outputFolder);
        
        if (meta == null)
        {
            meta = spriggitMetaLocator.LocateAndParse(outputFolder);
        }

        if (meta == null)
        {
            throw new NotSupportedException($"Could not locate meta to run with.  Either run serialize in with a .spriggit file present, or specify at least GameRelease and PackageName");
        }
        
        logger.Information("Getting entry point for {Meta}", meta);
        var entryPt = await entryPointCache.GetFor(meta, cancel);
        if (entryPt == null) throw new NotSupportedException($"Could not locate entry point for: {meta}");

        logger.Information("Starting to serialize from {BethesdaPluginPath} to {Output} with {Meta}", bethesdaPluginPath, outputFolder, meta);
        await entryPt.Serialize(
            bethesdaPluginPath,
            outputFolder,
            meta.Release,
            fileSystem: fileSystem,
            workDropoff: workDropoff,
            streamCreator: createStream,
            meta: new SpriggitSource()
            {
                PackageName = entryPt.Package.Id,
                Version = entryPt.Package.Version.ToString()
            },
            cancel: cancel);
        logger.Information("Finished serializing from {BethesdaPluginPath} to {Output} with {Meta}", bethesdaPluginPath, outputFolder, meta);
    }

    public async Task Deserialize(
        string spriggitPluginPath, 
        FilePath outputFile,
        SpriggitSource? source,
        CancellationToken cancel)
    {
        logger.Information("Spriggit version {Version}", currentVersionsProvider.SpriggitVersion);
        
        logger.Information("Getting meta to use for {Source} at path {PluginPath}", source, spriggitPluginPath);
        var meta = await getMetaToUse.Get(source, spriggitPluginPath, cancel);
        
        logger.Information("Getting entry point for {Meta}", meta);
        var entryPt = await entryPointCache.GetFor(meta, cancel);
        if (entryPt == null) throw new NotSupportedException($"Could not locate entry point for: {meta}");

        cancel.ThrowIfCancellationRequested();

        var dir = outputFile.Directory;
        if (dir != null)
        {
            logger.Information("Creating directory {Dir}", dir);
            fileSystem.Directory.CreateDirectory(dir);
        }
        
        logger.Information("Starting to deserialize from {BethesdaPluginPath} to {Output} with {Meta}", spriggitPluginPath, outputFile, meta);
        await entryPt.Deserialize(
            spriggitPluginPath,
            outputFile,
            workDropoff: workDropoff,
            fileSystem: fileSystem,
            streamCreator: createStream,
            cancel: cancel);
        logger.Information("Finished deserializing with {BethesdaPluginPath} to {Output} with {Meta}", spriggitPluginPath, outputFile, meta);
    }
}