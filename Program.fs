open System
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Twilio.AspNet.Core
open Twilio.TwiML

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services.AddTwilioRequestValidation() |> ignore
    builder.Services.Configure<ForwardedHeadersOptions>(
      fun (options: ForwardedHeadersOptions) -> options.ForwardedHeaders <- ForwardedHeaders.All
    ) |> ignore
    
    let app = builder.Build()
    
    app.UseForwardedHeaders() |> ignore

    let twilioEndpoints = app.MapGroup("").ValidateTwilioRequest()

    twilioEndpoints.MapPost("/message", Func<HttpRequest, CancellationToken, Task<TwiMLResult>>(fun request ctx -> 
        task {
            let! form = request.ReadFormAsync(ctx).ConfigureAwait(false)
            let body = form["Body"]
            return MessagingResponse().Message($"You said: {body}!").ToTwiMLResult()
        })
    ) |> ignore

    twilioEndpoints.MapPost("/voice", Func<TwiMLResult>(fun () -> 
      VoiceResponse()
       .Say("Which is better? Press 1 for cake, 2 for pie.")
       .Gather(
           action = Uri("/voice/response", UriKind.Relative),
           numDigits = 1
        )
       // redirect back to current endpoint if no response
       .Redirect(Uri("/voice", UriKind.Relative)) 
       .ToTwiMLResult()
    )) |> ignore
    
    twilioEndpoints.MapPost("/voice/response", Func<HttpRequest, CancellationToken, Task<TwiMLResult>>(fun request ctx -> 
        task {
            let! form = request.ReadFormAsync(ctx).ConfigureAwait(false)
            let result = form["Digits"].ToString()
            return VoiceResponse()
               .Say(if result = "1" then "The cake is a lie."
                    else if result = "2" then "Yum, pie."
                    else "You do you.")
               .ToTwiMLResult()
        })
    ) |> ignore

    app.Run()

    0 // Exit code