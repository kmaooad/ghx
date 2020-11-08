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
    task { do! GitHubRepos.uploadFile "coding201" ".github/workflows/test.md" "test" }
    |> Async.AwaitTask
    |> Async.RunSynchronously
