// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
open Kmaooad.Ghx
open System
open FSharp.Control.Tasks.V2.ContextInsensitive


let setCodecov namePattern =
    task {
        let! repos = GitHubRepos.getAllForOrg "kmaooad" $"^{namePattern}"

        repos 
        |> List.iteri(fun i r  -> 
                        task {
                            let! token = Codecov.getUploadToken r.Name
                            do! GitHubRepos.setSecret r.Name "CODECOV_TOKEN" token
                        } |> Async.AwaitTask |> Async.RunSynchronously
                    )
    } |> Async.AwaitTask |> Async.RunSynchronously

let uploadFile namePattern source dest overwrite =
    task {
        let! repos = GitHubRepos.getAllForOrg "kmaooad" $"^{namePattern}"
        let fileContent = System.IO.File.ReadAllText(source)
        repos 
        |> List.iteri(fun i r  -> 
                        printf "Uploading file to %s ... " r.Name
                        task {
                            do! GitHubRepos.uploadFile r.Name dest fileContent overwrite
                        } |> Async.AwaitTask |> Async.RunSynchronously
                    )
    } |> Async.AwaitTask |> Async.RunSynchronously

let coverageBadge r t = $"[![codecov](https://codecov.io/gh/kmaooad/{r}/branch/master/graph/badge.svg?token={t})](https://codecov.io/gh/kmaooad/{r})"

let makeReport assignment =
    task {
        let! repos = GitHubRepos.getAllForOrg "kmaooad" $"^{assignment}"
        repos 
        |> List.iteri(fun i r  -> 
                        task {
                            let! stats = GitHubRepos.getStats r.Id
                            let! covImageToken = Codecov.getGraphToken r.Name
                            let line = sprintf "| %d | %s | %s | %s | %d | %d/%d | %s" (i+1) 
                                        r.Name 
                                        (r.CreatedAt.ToString("MMM dd")) 
                                        (r.UpdatedAt.ToString("MMM dd"))
                                        stats.TotalCommits
                                        stats.Additions
                                        stats.Deletions
                                        (coverageBadge r.Name covImageToken)

                            System.IO.File.AppendAllLines($"{assignment}.md",[line])
                        } |> Async.AwaitTask |> Async.RunSynchronously
                    )
    } |> Async.AwaitTask |> Async.RunSynchronously
    

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | "report"::a::[] -> makeReport a
    | "setCodecov"::p::[] -> setCodecov p
    | "uploadFile"::p::s::d::o::[] -> uploadFile p s d (bool.Parse(o))
    | "uploadFile"::p::s::d::[] -> uploadFile p s d false
    | _ -> ()
    0