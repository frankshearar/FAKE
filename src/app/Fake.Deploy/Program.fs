﻿
open Fake
open DeploymentHelper

type DeployCommand = {
    Name :  string;
    Parameters : string list
    Description : string
    Function: string array -> unit
}  

module Main = 
    let registeredCommands = System.Collections.Generic.Dictionary<_,_>()
    let register command = registeredCommands.Add("/" + command.Name.ToLower(), command)

    let printUsage() =
        printfn "---- Usage -----"

        registeredCommands        
        |> Seq.map (fun p -> sprintf "/%s %s ->\r\n\t%s" p.Value.Name (separated " " p.Value.Parameters) p.Value.Description)
        |> Seq.iter (printfn "%s\r\n")
            
        printfn "Otherwise the service is just started as a command line process"

    let listen args =
        use srv = new Services.FakeDeployService()
        srv.Start(args)
        System.Console.ReadLine() |> ignore

    { Name = "install"
      Parameters = []
      Description = "installs the deployment agent as a service"
      Function = fun _ -> Installers.installServices() }
        |> register

    { Name = "uninstall"
      Parameters = []
      Description = "uninstalls the deployment agent"
      Function = fun _ -> Installers.uninstallServices() }
        |> register

    { Name = "start"
      Parameters = []
      Description = "starts the deployment agent"
      Function = fun _ -> Services.getFakeAgentService().Start() }
        |> register

    { Name = "stop"
      Parameters = []
      Description = "stops the deployment agent"
      Function = fun _ -> Services.getFakeAgentService().Stop() }
        |> register

    { Name = "listen"
      Parameters = ["serverName"; "port"]
      Description = "starts the deployment agent"
      Function = listen }
        |> register
        
    let traceDeploymentResult server fileName = function        
        | Success -> tracefn "Deployment of %s to %s successful" fileName server
        | Failure exn -> traceError <| sprintf "Deployment of %s to %s failed\r\n%A" fileName server exn 
        | Cancelled -> tracefn "Deployment of %s to %s cancelled" fileName server
        | RolledBack -> tracefn "Deployment of %s to %s rolled back" fileName server
        | Unknown -> traceError <| sprintf  "Deployment of %s to %s failed\r\nCould not derive reason sorry!!!" fileName server

    { Name = "deployRemote"
      Parameters = ["url"; "package"]
      Description = "pushes the deployment package to the deployment agent\r\n\tlistening on the url"
      Function = 
        fun args ->
            postDeploymentPackage args.[1] args.[2]
            |> traceDeploymentResult args.[1] args.[2] }
        |> register

    { Name = "deploy"
      Parameters = ["package"]
      Description = "runs the deployment on the local machine (for testing purposes)"
      Function =
        fun args -> 
            runDeploymentFromPackageFile args.[1]
            |> traceDeploymentResult "local" args.[1] }
        |> register

    { Name = "help"
      Parameters = []
      Description = "prints this message"
      Function = fun _ -> printUsage() }
        |> register   
     

    [<EntryPoint>]
    let main(args) =
        if args <> null && args.Length > 0 then
            match args.[0].ToLower() |> registeredCommands.TryGetValue with
            | true,cmd -> cmd.Function args
            | false,_ -> printUsage()
        else 
            listen args

        0
