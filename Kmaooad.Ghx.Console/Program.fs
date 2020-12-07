// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
open Kmaooad.Ghx
open System
open FSharp.Control.Tasks.V2.ContextInsensitive


let setCodecov namePattern =
    async {
        let! repos = GitHubRepos.getAllForOrg "kmaooad" $"^{namePattern}" |> Async.AwaitTask

        return! repos 
        |> List.map(fun  r  -> 
                        async {
                            let! token = Codecov.getUploadToken r.Name
                            do! GitHubRepos.setSecret r.Name "CODECOV_TOKEN" token
                            printfn "Set CodeCov for %s" r.Name
                        } 
                    )
        |> Async.Parallel
    } |> Async.RunSynchronously |> ignore

let uploadFile namePattern source dest branch overwrite =
    task {
        let! repos = GitHubRepos.getAllForOrg "kmaooad" $"^{namePattern}"
        let fileContent = System.IO.File.ReadAllText(source)
        repos 
        |> List.iteri(fun i r  -> 
                        printf "Uploading file to %s ... " r.Name
                        task {
                            do! GitHubRepos.uploadFile r.Name dest fileContent branch overwrite
                        } |> Async.AwaitTask |> Async.RunSynchronously
                    )
    } |> Async.AwaitTask |> Async.RunSynchronously
let coverageBadge r t = $"[![codecov](https://codecov.io/gh/kmaooad/{r}/branch/master/graph/badge.svg?token={t})](https://codecov.io/gh/kmaooad/{r})"

type UserRow = {
          User:string
          Repo:string
          Coverage1:float
          Coverage2:float
          Assignment:string
      }

let commitCoverage () =
      
      let input = [("201",DateTimeOffset(DateTime(2020,11,11)));("202",DateTimeOffset(DateTime(2020,12,8)));("203",DateTimeOffset(DateTime(2020,12,8)))]

      let data = 
          input 
            |> List.map(
                fun (namePattern,deadline) ->
                  task {
                    let! repos = GitHubRepos.getAllForOrg "kmaooad" $"^{namePattern}"
                    let covers = 
                        repos 
                            |> List.map(fun r  -> 
                                    task {
                                        let! sha1 = GitHubRepos.getLatestCommit r.Id deadline
                                        let! coverage1 =
                                            match sha1 with 
                                            |Some s1 when s1 <> "null" ->  (Codecov.getCommitCoverage r.Name (Some s1)) 
                                            | _ -> async { return 0.0 }
                                        let! coverage2 = Codecov.getCommitCoverage r.Name None
                                        return (r,coverage1,coverage2)

                                    } |> Async.AwaitTask |> Async.RunSynchronously
                                )
                    return 
                        covers 
                        |> List.map(fun (r,c1,c2)-> 
                                  task {
                                    let! contribs = GitHubRepos.getRepoContributors r.Id
                                    return contribs |> Seq.filter (fun s -> s<>"ironpercival") |> Seq.map(fun s -> { User = s; Repo = r.Name; Assignment = namePattern; Coverage1 = c1; Coverage2 = c2 })
                                  } |> Async.AwaitTask |> Async.RunSynchronously)
                        |> Seq.concat
               
                    } |> Async.AwaitTask |> Async.RunSynchronously
                    )
                    |> Seq.concat |> List.ofSeq |> List.sortBy(fun d -> d.User)
      let lines = data |> List.map (fun { User = u;Repo = r;Assignment = a; Coverage1 = c1; Coverage2 = c2} -> $"{u},{r},{a},{c1},{c2}")
      System.IO.File.AppendAllLines("grdg.csv",lines)
        
    
let branch namePattern deadline =
    task {
        let! repos = GitHubRepos.getAllForOrg "kmaooad" $"^{namePattern}"
        repos 
        |> List.iteri(fun i r  -> 
                        printf "Creating deadline branch for %s ... " r.Name
                        task {
                            let! sha1 = GitHubRepos.getLatestCommit r.Id deadline
                            let! sha2 = GitHubRepos.getLatestCommit r.Id (DateTimeOffset(DateTime(2021,1,1)))
                            let! r = 
                              match (sha1,sha2) with 
                              | (Some s1,Some s2) -> task { 
                                  let! r = GitHubRepos.branch r.Name s1
                                  printfn "CREATED" 
                                }
                              | _ -> task { printfn "SKIPPED" }
                            r |> ignore
                        } |> Async.AwaitTask |> Async.RunSynchronously
                    )
    } |> Async.AwaitTask |> Async.RunSynchronously



let makeReport assignment =
    task {
        let! repos = GitHubRepos.getAllForOrg "kmaooad" $"^{assignment}"
        repos 
        |> List.iteri(fun i r  -> 
                        task {
                            let! stats = GitHubRepos.getStats r.Id
                            let! covImageToken = Codecov.getGraphToken r.Name
                            let line = sprintf "| %d | %s | %s | %s | %d | %d/%d/%d | %s" (i+1) 
                                        r.Name 
                                        (r.CreatedAt.ToString("MMM dd")) 
                                        (r.UpdatedAt.ToString("MMM dd"))
                                        stats.TotalCommits
                                        stats.Additions
                                        stats.Deletions
                                        (stats.Additions + stats.Deletions)
                                        (coverageBadge r.Name covImageToken)

                            System.IO.File.AppendAllLines($"{assignment}.md",[line])
                        } |> Async.AwaitTask |> Async.RunSynchronously
                    )
    } |> Async.AwaitTask |> Async.RunSynchronously
    

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | "report"::a::[] -> makeReport a
    | "branch"::a::d::[] -> branch a (DateTimeOffset(DateTime.Parse(d)))
    | "commitCoverage"::_-> commitCoverage ()
    | "setCodecov"::p::[] -> setCodecov p
    | "uploadFile"::p::s::d::b::o::[] -> uploadFile p s d b (bool.Parse(o))
    | "uploadFile"::p::s::d::b::[] -> uploadFile p s d b false
    | _ -> ()
    0