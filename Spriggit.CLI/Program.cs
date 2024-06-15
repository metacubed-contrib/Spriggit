﻿using System.Globalization;
using CommandLine;
using Spriggit.CLI;
using Spriggit.CLI.Commands;
using Spriggit.Engine.Services.Singletons;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var vers = new CurrentVersionsProvider();
Console.WriteLine("Spriggit version {Version}", vers.SpriggitVersion);

return await Parser.Default.ParseArguments(args, typeof(DeserializeCommand), typeof(SerializeCommand), typeof(FormIDCollisionCommand))
    .MapResult(
        async (DeserializeCommand deserialize) => await EngineRunner.Run(deserialize, null),
        async (SerializeCommand serialize) => await EngineRunner.Run(serialize, null),
        async (FormIDCollisionCommand formIdCollision) => await FormIDCollisionRunner.Run(formIdCollision),
        async (MergeVersionSyncerCommand versionSyncer) => await MergeVersionSyncerRunner.Run(versionSyncer),
        async _ => -1);