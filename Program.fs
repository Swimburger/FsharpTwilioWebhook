open System
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Twilio.TwiML
open Twilio.AspNet.Core

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services.AddTwilioRequestValidation() |> ignore

    builder.Services.Configure<ForwardedHeadersOptions>(
        fun (options: ForwardedHeadersOptions) -> options.ForwardedHeaders <- ForwardedHeaders.All
    ) |> ignore

    let app = builder.Build()

    app.UseForwardedHeaders() |> ignore

    app.MapPost("/message",Func<HttpRequest, CancellationToken, Task<TwiMLResult>>(fun request ctx ->
            task {
                let! (form: IFormCollection) = request.ReadFormAsync(ctx).ConfigureAwait(false)
                let from = form["From"]
                return MessagingResponse().Message($"Ahoy {from}!").ToTwiMLResult()
            })
        )
        .ValidateTwilioRequest() |> ignore
    
    app.MapPost("/voice", Func<TwiMLResult>(fun () -> VoiceResponse().Say("Ahoy!").ToTwiMLResult()))
        .ValidateTwilioRequest() |> ignore

    app.Run()

    0 // Exit code
