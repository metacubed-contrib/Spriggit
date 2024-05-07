using System.IO.Abstractions;
using LibGit2Sharp;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Noggog.IO;
using Spriggit.Core;

namespace Spriggit.Engine.Collision;

public class FormIDCollisionFixer
{
    private readonly IFileSystem _fileSystem;
    private readonly FormIDReassigner _reassigner;
    private readonly FormIDCollisionDetector _detector;

    public FormIDCollisionFixer(
        IFileSystem fileSystem,
        FormIDReassigner reassigner,
        FormIDCollisionDetector detector)
    {
        _fileSystem = fileSystem;
        _reassigner = reassigner;
        _detector = detector;
    }

    public async Task DetectAndFix<TMod, TModGetter>(
        IEntryPoint entryPoint,
        DirectoryPath gitRootPath,
        DirectoryPath spriggitModPath,
        Signature fixSignature)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>
    {
        using var tmp = TempFolder.Factory(fileSystem: _fileSystem);

        var meta = await entryPoint.TryGetMetaInfo(
            spriggitModPath,
            workDropoff: null,
            fileSystem: _fileSystem,
            streamCreator: null,
            cancel: CancellationToken.None);
        if (meta == null)
        {
            throw new Exception("Could not locate target meta");
        }

        FilePath origMergedModPath = Path.Combine(tmp.Dir, "MergedOrig", meta.ModKey.FileName);
        origMergedModPath.Directory?.Create(_fileSystem);

        await entryPoint.Deserialize(
            spriggitModPath,
            origMergedModPath,
            workDropoff: null,
            fileSystem: _fileSystem,
            streamCreator: null,
            cancel: CancellationToken.None);

        var origMergedMod = ModInstantiator<TMod>.Importer(origMergedModPath.Path, meta.Release, fileSystem: _fileSystem);
        
        var collisions = _detector.LocateCollisions(origMergedMod);
        if (collisions.Count == 0) return;

        foreach (var coll in collisions)
        {
            if (coll.Value.Count != 2)
            {
                throw new Exception($"Collision detected with not exactly two participants: {string.Join(", ", coll.Value.Take(50))}");
            }
        }

        var toReassign = collisions.SelectMany(x => x.Value)
            .Select(x => x.ToFormLinkInformation())
            .ToArray();

        var repo = new LibGit2Sharp.Repository(gitRootPath);
        if (repo == null)
        {
            throw new Exception("No git repository detected");
        }

        if (!repo.Head.Commits.Any())
        {
            throw new Exception("Git repository had no commits at HEAD");
        }
        
        var parents = repo.Head.Commits.First().Parents
            .ToArray();

        if (parents.Length != 2)
        {
            throw new Exception("Git did not have a merge commit at HEAD");
        }
        
        var origBranch = repo.Head;
        var branch = repo.CreateBranch("Spriggit-Merge-Fix", parents[0]);
        Commands.Checkout(repo, branch);

        await entryPoint.Deserialize(
            spriggitModPath,
            origMergedModPath,
            workDropoff: null,
            fileSystem: _fileSystem,
            streamCreator: null,
            cancel: CancellationToken.None);

        var branchMod = ModInstantiator<TMod>.Importer(origMergedModPath.Path, meta.Release, fileSystem: _fileSystem);

        _reassigner.Reassign<TMod, TModGetter>(
            branchMod, 
            () => origMergedMod.GetNextFormKey(),
            toReassign);
        
        branchMod.WriteToBinary(origMergedModPath.Path);
        
        await entryPoint.Serialize(
            origMergedModPath.Path,
            spriggitModPath,
            meta.Release,
            workDropoff: null,
            fileSystem: _fileSystem,
            streamCreator: null,
            meta: meta.Source,
            cancel: CancellationToken.None);

        Commands.Checkout(repo, branch);
        Commands.Stage(repo, Path.Combine(spriggitModPath, "*"));
        repo.Commit("FormID Collision Fix", fixSignature, fixSignature, new CommitOptions());

        Commands.Checkout(repo, origBranch);
        repo.Merge(branch, fixSignature);
        
        FilePath newMergedModPath = Path.Combine(tmp.Dir, "MergedNew", meta.ModKey.FileName);
        newMergedModPath.Directory?.Create(_fileSystem);

        await entryPoint.Deserialize(
            spriggitModPath,
            newMergedModPath,
            workDropoff: null,
            fileSystem: _fileSystem,
            streamCreator: null,
            cancel: CancellationToken.None);

        var newMergedMod = ModInstantiator<TMod>.Importer(newMergedModPath.Path, meta.Release, fileSystem: _fileSystem);

        var newCollisions = _detector.LocateCollisions(newMergedMod);
        if (newCollisions.Count != 0)
        {
            throw new Exception($"Fix still had collided FormIDs.  Leaving in a bad state");
        }
    }
}