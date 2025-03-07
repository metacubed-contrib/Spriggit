﻿using System.IO.Abstractions;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Starfield;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog;
using Noggog.Testing.FileSystem;
using Spriggit.Core;
using Spriggit.Yaml.Starfield;
using Xunit;

namespace Spriggit.Tests;

[Collection("Sequential")]
public class EndpointTests
{
    [Theory]
    [MutagenModAutoData(GameRelease.Starfield)]
    public async Task Deserialize(
        IFileSystem fileSystem,
        StarfieldMod mod,
        DirectoryPath dataFolder,
        DirectoryPath spriggitFolder,
        EntryPoint entryPoint)
    {
        var modPath = new ModPath(Path.Combine(dataFolder, mod.ModKey.ToString()));
        fileSystem.Directory.CreateDirectory(dataFolder);
        mod.WriteToBinary(modPath, new BinaryWriteParameters()
        {
            FileSystem = fileSystem
        });
        await entryPoint.Serialize(modPath: modPath,
            outputDir: spriggitFolder,
            dataPath: dataFolder,
            release: GameRelease.Starfield,
            workDropoff: null,
            knownMasters: Array.Empty<KnownMaster>(),
            fileSystem: fileSystem,
            streamCreator: null, meta: new SpriggitSource()
            {
                PackageName = "Spriggit.Yaml.Starfield",
                Version = "Test"
            }, cancel: CancellationToken.None);
        await entryPoint.Deserialize(inputPath: spriggitFolder,
            outputPath: modPath,
            dataPath: dataFolder,
            workDropoff: null,
            knownMasters: Array.Empty<KnownMaster>(),
            fileSystem: fileSystem,
            streamCreator: null, cancel: CancellationToken.None);
        fileSystem.File.Exists(modPath)
            .Should().BeTrue();
        var stringsFolder = Path.Combine(dataFolder, "Strings");
        fileSystem.Directory.Exists(stringsFolder)
            .Should().BeFalse();
    }
    
    [Theory, MutagenModAutoData(GameRelease.Starfield)]
    public async Task DeserializeLocalized(
        IFileSystem fileSystem,
        StarfieldMod mod,
        Armor armor,
        string name,
        string frenchName,
        DirectoryPath dataFolder,
        DirectoryPath dataFolder2,
        DirectoryPath spriggitFolder,
        EntryPoint entryPoint)
    {
        try
        {
            mod.Armors.Count.Should().Be(1);
            mod.UsingLocalization = true;
            armor.Name = name;
            armor.Name.Set(Language.French, frenchName);
            fileSystem.Directory.CreateDirectory(dataFolder);
            fileSystem.Directory.CreateDirectory(dataFolder2);
            var modPath = new ModPath(Path.Combine(dataFolder, mod.ModKey.ToString()));
            mod.WriteToBinary(modPath, new BinaryWriteParameters()
            {
                FileSystem = fileSystem
            });
            await entryPoint.Serialize(modPath: modPath,
                outputDir: spriggitFolder,
                dataPath: dataFolder,
                release: GameRelease.Starfield,
                workDropoff: null,
                knownMasters: Array.Empty<KnownMaster>(),
                fileSystem: fileSystem,
                streamCreator: null, meta: new SpriggitSource()
                {
                    PackageName = "Spriggit.Yaml.Starfield",
                    Version = "Test"
                }, cancel: CancellationToken.None);
            var modPath2 = new ModPath(Path.Combine(dataFolder2, mod.ModKey.ToString()));
            await entryPoint.Deserialize(inputPath: spriggitFolder,
                outputPath: modPath2,
                dataPath: dataFolder2,
                workDropoff: null,
                knownMasters: Array.Empty<KnownMaster>(),
                fileSystem: fileSystem,
                streamCreator: null, cancel: CancellationToken.None);
            fileSystem.File.Exists(modPath2)
                .Should().BeTrue();
            var stringsFolder = Path.Combine(dataFolder2, "Strings");
            fileSystem.Directory.Exists(stringsFolder)
                .Should().BeTrue();
            fileSystem.File.Exists(Path.Combine(stringsFolder, $"{mod.ModKey.Name}_en.STRINGS"))
                .Should().BeTrue();
            fileSystem.File.Exists(Path.Combine(stringsFolder, $"{mod.ModKey.Name}_fr.STRINGS"))
                .Should().BeTrue();
            fileSystem.File.Exists(Path.Combine(stringsFolder, $"{mod.ModKey.Name}_en.ILSTRINGS"))
                .Should().BeTrue();
            fileSystem.File.Exists(Path.Combine(stringsFolder, $"{mod.ModKey.Name}_fr.ILSTRINGS"))
                .Should().BeTrue();
            fileSystem.File.Exists(Path.Combine(stringsFolder, $"{mod.ModKey.Name}_en.DLSTRINGS"))
                .Should().BeTrue();
            fileSystem.File.Exists(Path.Combine(stringsFolder, $"{mod.ModKey.Name}_fr.DLSTRINGS"))
                .Should().BeTrue();
        }
        catch (Exception e)
        {
            throw;
        }
    }
    
    [Theory, MutagenModAutoData(GameRelease.Starfield)]
    public async Task DeserializeDifferentModKeyStrings(
        IFileSystem fileSystem,
        StarfieldMod mod,
        Armor armor,
        string name,
        string frenchName,
        DirectoryPath dataFolder,
        DirectoryPath spriggitFolder,
        DirectoryPath dataFolder2,
        ModKey otherModKey,
        EntryPoint entryPoint)
    {
        mod.UsingLocalization = true;
        armor.Name = name;
        armor.Name.Set(Language.French, frenchName);
        var modPath = new ModPath(Path.Combine(dataFolder, mod.ModKey.ToString()));
        fileSystem.Directory.CreateDirectory(dataFolder);
        fileSystem.Directory.CreateDirectory(dataFolder2);
        mod.WriteToBinary(modPath, new BinaryWriteParameters()
        {
            FileSystem = fileSystem
        });
        await entryPoint.Serialize(modPath: modPath,
            outputDir: spriggitFolder,
            dataPath: dataFolder,
            release: GameRelease.Starfield,
            workDropoff: null,
            knownMasters: Array.Empty<KnownMaster>(),
            fileSystem: fileSystem,
            streamCreator: null, meta: new SpriggitSource()
            {
                PackageName = "Spriggit.Yaml.Starfield",
                Version = "Test"
            }, cancel: CancellationToken.None);
        var modPath2 = Path.Combine(dataFolder2, otherModKey.ToString());
        await entryPoint.Deserialize(inputPath: spriggitFolder,
            outputPath: modPath2,
            dataPath: dataFolder2,
            workDropoff: null,
            knownMasters: Array.Empty<KnownMaster>(),
            fileSystem: fileSystem,
            streamCreator: null, cancel: CancellationToken.None);
        var otherStringsFolder = Path.Combine(dataFolder2, "Strings");
        var path = Path.Combine(otherStringsFolder, $"{otherModKey.Name}_en.STRINGS");
        fileSystem.File.Exists(path)
            .Should().BeTrue();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{otherModKey.Name}_fr.STRINGS"))
            .Should().BeTrue();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{otherModKey.Name}_en.ILSTRINGS"))
            .Should().BeTrue();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{otherModKey.Name}_fr.ILSTRINGS"))
            .Should().BeTrue();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{otherModKey.Name}_en.DLSTRINGS"))
            .Should().BeTrue();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{otherModKey.Name}_fr.DLSTRINGS"))
            .Should().BeTrue();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{mod.ModKey.Name}_en.STRINGS"))
            .Should().BeFalse();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{mod.ModKey.Name}_fr.STRINGS"))
            .Should().BeFalse();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{mod.ModKey.Name}_en.ILSTRINGS"))
            .Should().BeFalse();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{mod.ModKey.Name}_fr.ILSTRINGS"))
            .Should().BeFalse();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{mod.ModKey.Name}_en.DLSTRINGS"))
            .Should().BeFalse();
        fileSystem.File.Exists(Path.Combine(otherStringsFolder, $"{mod.ModKey.Name}_fr.DLSTRINGS"))
            .Should().BeFalse();
    }
}