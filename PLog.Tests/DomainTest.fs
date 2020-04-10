module PLog.Tests.DomainTest

open NUnit.Framework
open PLog.Domain

let ADB = "/Users/nghia/DevTools/android-sdk/platform-tools/adb"

[<Test>]
let ``test fetchDevices`` () =
    fetchDevices ADB
    |> printfn "%A"

[<Test>]
let ``test parseLogItem`` () =
    let logItem = @"03-24 11:06:58.173  2107  2107 I MicroDetectionWorker: #startMicroDetector [speakerMode: 0]"
    let expected = {
        Content = logItem
        Tag = Some "MicroDetectionWorker"
        Pid = Some 2107
        Severity = Info
    }
    Assert.AreEqual (expected, parseLogItem logItem)

    let logItem = @"03-24 11:06:55.339  1593  1606 E memtrack: Couldn't load memtrack module"
    let expected = {
        Content = logItem
        Tag = Some "memtrack"
        Pid = Some 1593
        Severity = Err
    }
    Assert.AreEqual (expected, parseLogItem logItem)

    let logItem = @"dfiudyfiudhfiudgigdigfd diufiudigugdig diufiudgiudg iud iud idig"
    let expected = {
        Content = logItem
        Tag = None
        Pid = None
        Severity = Info
    }
    Assert.AreEqual (expected, parseLogItem logItem)
