namespace ServerSpa

open System
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks

open FSharp.Control.Tasks

open Microsoft.AspNetCore.Http

open Giraffe
open Giraffe.Serialization

open Saturn.Application
open Saturn.Pipeline
open Saturn.PipelineHelpers
open Saturn.CSRF
open Saturn.Endpoint

open ServerSpa.Pages

module Program =
    let setTurbolinksLocationHeader: HttpHandler =
        let isTurbolink (ctx: HttpContext) =
            ctx.Request.Headers.ContainsKey "Turbolinks-Referrer"

        fun next ctx ->
            task {
                if isTurbolink ctx
                then ctx.SetHttpHeader "Turbolinks-Location" (ctx.Request.Path + ctx.Request.QueryString)

                return! next ctx
            }

    let browser =
        pipeline {
            plug putSecureBrowserHeaders
            set_header "x-pipeline-type" "Browser"
            plug setTurbolinksLocationHeader
        }

    let defaultView =
        router {
            get "/" Auth.Login
            get "/index.html" (redirectTo false "/")
            get "/default.html" (redirectTo false "/")

        }

    let authRouter =
        router {
            get "/signup" Auth.SignUp
            post "/signup" (csrf >=> Auth.ProcessSignup)
            post "/login" (csrf >=> Auth.ProcessLogin)
        }

    let profileRouter =
        router {
            get
                "/"
                (requiresAuthentication Layouts.Forbidden
                 >=> Profile.Index)

            get
                "/edit"
                (requiresAuthentication Layouts.Forbidden
                 >=> Profile.EditUserInfoPartial)

            post
                "/save"
                (requiresAuthentication Layouts.Forbidden
                 >=> csrf
                 >=> Profile.UserInfoPartial)
        }

    let browserRouter =
        router {
            pipe_through browser

            forward "" defaultView
            forward "/auth" authRouter
            forward "/profile" profileRouter
        }

    let private JsonSerializer =
        let opts = JsonSerializerOptions()
        opts.AllowTrailingCommas <- true
        opts.ReadCommentHandling <- JsonCommentHandling.Skip
        opts.IgnoreNullValues <- true
        opts.Converters.Add(JsonFSharpConverter())

        { new IJsonSerializer with
            member this.Deserialize<'T>(arg1: byte []): 'T =
                let spn = ReadOnlySpan(arg1)
                JsonSerializer.Deserialize<'T>(spn, opts)

            member this.Deserialize<'T>(arg1: string): 'T =
                JsonSerializer.Deserialize<'T>(arg1, opts)

            member this.DeserializeAsync(arg1: System.IO.Stream): Task<'T> =
                JsonSerializer
                    .DeserializeAsync<'T>(arg1, opts)
                    .AsTask()

            member this.SerializeToBytes<'T>(arg1: 'T): byte array =
                JsonSerializer.SerializeToUtf8Bytes(arg1, opts)

            member this.SerializeToStreamAsync<'T> (arg1: 'T) (arg2: System.IO.Stream): Task =
                JsonSerializer.SerializeAsync(arg2, arg1, opts)

            member this.SerializeToString<'T>(arg1: 'T): string =
                JsonSerializer.Serialize(arg1, typeof<'T>, opts) }

    let app =
        application {
            use_endpoint_router browserRouter

            use_antiforgery_with_config
                (fun cfg ->
                    cfg.HeaderName <- "XSRF-TOKEN"
                    cfg.Cookie.Name <- "XSRF-TOKEN")

            use_cookies_authentication "http://localhost:5001"
            use_json_serializer JsonSerializer
            use_static "wwwroot"
            use_developer_exceptions
            use_gzip
        }

    [<EntryPoint>]
    let main _ =
        run app
        0 // return an integer exit code
