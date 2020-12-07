namespace Kmaooad.Ghx

open System
open System.Text.RegularExpressions
open FSharp.Data
open FSharp.Data.JsonExtensions
open FsHttp.DslCE
open FsHttp

module Codecov =

    let getUploadToken repoName =
        async {
            let authToken =
                Environment.GetEnvironmentVariable("KMAOOAD_CODECOV_TOKEN")

            let! response =
                httpAsync {
                    GET $"https://codecov.io/api/gh/kmaooad/{repoName}"
                    Authorization $"token {authToken}"
                }

            let token =
                response
                |> Response.toJson
                |> fun json -> json?repo?upload_token.AsString()

            return token
        }

  
    let getCommitCoverage repoName maybesha =
        async {
            printfn "Getting coverage for %s %A" repoName maybesha
            let authToken =
                Environment.GetEnvironmentVariable("KMAOOAD_CODECOV_TOKEN")

            let url =
              match maybesha with
              | Some sha -> $"https://codecov.io/api/gh/kmaooad/{repoName}/commit/{sha}"
              | None -> $"https://codecov.io/api/gh/kmaooad/{repoName}/tree/master"
   
            let! response =
                httpAsync {
                    GET url
                    Authorization $"token {authToken}"
                }

            let coverage =
                try
                    response
                    |> Response.toJson
                    |> fun json -> json?commit?totals?c.AsString() |> float
                with _ -> 0.0

            return coverage
        }

    let getGraphToken repoName =
        async {
            let authToken =
                Environment.GetEnvironmentVariable("KMAOOAD_CODECOV_TOKEN")

            let! response =
                httpAsync {
                    GET $"https://codecov.io/api/gh/kmaooad/{repoName}"
                    Authorization $"token {authToken}"
                }

            let token =
                response
                |> Response.toJson
                |> fun json -> json?repo?image_token.AsString()

            return token
        }
