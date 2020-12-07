module Kmaooad.Ghx.Tests

open System
open Xunit
open Kmaooad.Ghx
open FSharp.Control.Tasks.V2.ContextInsensitive

[<Fact>]
let ``Can get Codecov token`` () =
    async {
        let! token = Codecov.getUploadToken "201-trojan"
        Assert.NotEmpty(token)
    }
    |> Async.RunSynchronously

[<Fact>]
let ``Can set GitHub secret`` () =
    async { do! GitHubRepos.setSecret "201-trojan" "test" "test" }
    |> Async.RunSynchronously

[<Fact>]
let ``Can upload file to repo`` () =
    task { do! GitHubRepos.uploadFile "coding201" ".github/workflows/test.md" "test" "master" false }
    |> Async.AwaitTask
    |> Async.RunSynchronously

[<Fact>]
let ``Can get latest commit`` () =
    task {
        let! repoId = GitHubRepos.getRepoId "201-trojan"
        let! sha = GitHubRepos.getLatestCommit repoId (DateTimeOffset(DateTime(2020, 11, 11)))
        printfn "%O" sha
    }
    |> Async.AwaitTask
    |> Async.RunSynchronously

[<Fact>]
let ``Can create branch`` () =
    task { do! GitHubRepos.branch "202-avm" "d509e275c9812d85304b482c4789db0b6daba8e9" }
    |> Async.AwaitTask
    |> Async.RunSynchronously

[<Fact>]
let ``Can get commit coverage`` () =
    async {
        let! coverage = Codecov.getCommitCoverage "201-trojan" "da311a87dbe47ffb1c03aa2922d9c1dfcc1f6192"
        printfn "%O" coverage
    }
    |> Async.RunSynchronously
