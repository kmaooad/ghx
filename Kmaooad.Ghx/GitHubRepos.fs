namespace Kmaooad.Ghx

open System
open Octokit
open FSharp.Control.Tasks.V2.ContextSensitive
open System.Text.RegularExpressions
open FsHttp.DslCE
open FsHttp
open FSharp.Data.HttpRequestHeaders
open FSharp.Data.JsonExtensions
open FSharp.Data
open FSharp.Json
open System.Threading.Tasks
open System.Collections.Generic

module GitHubRepos =

    let private token =
        Environment.GetEnvironmentVariable("KMAOOAD_GHX_TOKEN")

    let client =

        let client =
            GitHubClient(ProductHeaderValue("kmaooad-ghx"))

        client.Credentials <- Credentials(token)
        client

    let getStats repoId =
        task {
            let! p = client.Repository.Statistics.GetParticipation(repoId)
            let! f = client.Repository.Statistics.GetCodeFrequency(repoId)

            return
                {| TotalCommits = (Math.Max((p.TotalCommits() - 3), 0))
                   Additions =
                       (Math.Max
                           (((f.AdditionsAndDeletionsByWeek
                              |> List.ofSeq
                              |> List.sumBy (fun f1 -> f1.Additions))
                             - 609),
                            0))
                   Deletions =
                       (f.AdditionsAndDeletionsByWeek
                        |> List.ofSeq
                        |> List.sumBy (fun f1 -> f1.Deletions)) |}
        }

    let getAllForOrg orgName namePattern =
        task {

            let! allRepos = client.Repository.GetAllForOrg(orgName)

            return
                allRepos
                |> List.ofSeq
                |> List.filter (fun r -> (Regex.IsMatch(r.Name, namePattern)))
        }

    let getRepoId name =
        task {

            let! repo = client.Repository.Get("kmaooad", name)

            return repo.Id
        }

    let branch repo sha =
        task {
            let newref = NewReference("refs/heads/ddln", sha)
            let! r = client.Git.Reference.Create("kmaooad", repo, newref)
            r |> ignore
        }

    let private getFile repoName filePath branch =
        task {
            try
                let! files = client.Repository.Content.GetAllContentsByRef("kmaooad", repoName, filePath, branch)

                return
                    match List.ofSeq files with
                    | f :: _ -> Some f
                    | _ -> None

            with _ -> return None
        }

    let uploadFile repoName destFile content (branch: string) overwrite =
        task {

            let! file = getFile repoName destFile branch

            match file with
            | Some f when overwrite ->
                let request =
                    UpdateFileRequest("Update file via GHX", content, f.Sha, branch)

                let! r = client.Repository.Content.UpdateFile("kmaooad", repoName, destFile, request)

                printfn "UPDATED"

                r |> ignore
            | None ->
                let request =
                    CreateFileRequest("Create file via GHX", content, branch)

                let! r = client.Repository.Content.CreateFile("kmaooad", repoName, destFile, request)

                printfn "CREATED"

                r |> ignore
            | _ -> printfn "SKIPPED"

        }

    let getPublicKey repoName =
        async {

            let url =
                $"https://api.github.com/repos/kmaooad/{repoName}/actions/secrets/public-key"

            let (_, basicAuth) = BasicAuth "ironpercival" token

            let! response =
                httpAsync {
                    GET url
                    UserAgent "ironpercival"
                    Authorization basicAuth
                }

            return
                response
                |> Response.toJson
                |> fun json -> (json?key_id.AsString(), json?key.AsString())
        }

    let getLatestCommit repoId date =
        task {
            try
                let! commits = client.Repository.Commit.GetAll(repoId)

                return
                    match (List.ofSeq commits)
                          |> List.filter (fun c -> c.Commit.Committer.Date < date)
                          |> List.sortByDescending (fun c -> c.Commit.Committer.Date) with
                    | c :: _ when not (String.IsNullOrEmpty(c.Sha)) -> Some c.Sha
                    | _ :: _ -> Some "null"
                    | _ -> None

            with _ -> return None
        }

    let getRepoContributors repoId = 
        task {
                let! teams = client.Repository.GetAllTeams(repoId)
                let getTeam tid = 
                    task { 
                        let! tm = client.Organization.Team.GetAllMembers(tid)
                        return tm |> Seq.map(fun m -> m.Login) } |> Async.AwaitTask |> Async.RunSynchronously
                let contribs = teams |> Seq.collect (fun t -> (getTeam t.Id))
                return contribs
        }

    let setSecret repoName secretName (secretValue: string) =

        async {

            let url =
                $"https://api.github.com/repos/kmaooad/{repoName}/actions/secrets/{secretName}"

            let (_, basicAuth) = BasicAuth "ironpercival" token

            let! (keyId, publicKey) = getPublicKey repoName

            let secretValueBytes =
                System.Text.Encoding.UTF8.GetBytes(secretValue)

            let publicKeyBytes = Convert.FromBase64String(publicKey)

            let sealedPublicKeyBytes =
                Sodium.SealedPublicKeyBox.Create(secretValueBytes, publicKeyBytes)

            let sealedPublicKey =
                Convert.ToBase64String(sealedPublicKeyBytes)

            let! response =
                httpAsync {
                    PUT url
                    UserAgent "ironpercival"
                    Authorization basicAuth
                    body

                    json
                        (Json.serialize
                            {| encrypted_value = sealedPublicKey
                               key_id = keyId |})
                }

            response |> ignore
        }
