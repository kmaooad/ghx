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
                let authToken = Environment.GetEnvironmentVariable("KMAOOAD_CODECOV_TOKEN") 
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

    let getGraphToken repoName = 
            async {
                let authToken = Environment.GetEnvironmentVariable("KMAOOAD_CODECOV_TOKEN") 
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
