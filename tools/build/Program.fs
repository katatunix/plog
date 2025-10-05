open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.Core.TargetOperators

let net = "net9.0"

let createTargets () =
    Target.create "clean" <| fun _ ->
        Shell.deleteDirs [
            "build"
            for project in [ "PLog.Mac"; "PLog" ] do
                $"{project}/obj"
                for config in [ "Release"; "Debug" ] do
                    $"{project}/bin/{config}"
        ]

    Target.create "build" <| fun _ ->
        "PLog.Mac" |> DotNet.build (fun o -> { o with Configuration = DotNet.Release })

    Target.create "publish" <| fun _ ->
        let exec runtime =
            "PLog.Mac"
            |> DotNet.publish
                (fun o ->
                    { o with
                        Configuration = DotNet.Release
                        SelfContained = Some true
                        Runtime = Some $"osx-{runtime}"
                    })
        exec "x64"
        exec "arm64"

    Target.create "pack" <| fun p ->
        let version =
            match p.Context.Arguments with
            | [ version ] -> version
            | _ -> failwith "Version is required"
        Shell.mkdir "build"
        let exec runtime =
            CreateProcess.fromRawCommandLine
                "tools/create-dmg/create-dmg"
                $"--no-internet-enable \
                  --window-pos 200 120 \
                  --window-size 800 480 \
                  --app-drop-link 400 200 \
                  --add-file readme.txt build.sample/readme.txt 550 32 \
                  build/PLog.Mac.{runtime}.v{version}.dmg PLog.Mac/bin/Release/{net}/osx-{runtime}/PLog.Mac.app"
            |> Proc.run
            |> ignore
        exec "x64"
        exec "arm64"

let setupDependencies () =
    "clean" ==> "publish" ==> "pack" |> ignore

[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false ""
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    createTargets ()
    setupDependencies ()

    Target.runOrDefaultWithArguments "pack"

    0
