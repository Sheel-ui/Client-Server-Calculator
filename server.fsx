open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading

// Define a record type to store information about connected clients
type ClientInfo =
    {
        ClientId: string
        Socket: Socket
    }

let mutable port = 8888
let handleClient (clientInfo: ClientInfo) =
    try
        while true do
            // Initialize a buffer to receive data from the client 
            let buffer = Array.zeroCreate<byte> 1024

            // Get the client's unique identifier and socket
            let id = clientInfo.ClientId
            let clientSocket = clientInfo.Socket

            // Parsing the input buffer
            let bytesRead = clientSocket.Receive(buffer)
            let input = Encoding.ASCII.GetString(buffer, 0, bytesRead).TrimEnd('\n')
            let parts = input.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            let command, numbers = parts.[0], parts.[1..]
            
            // if client pings the server
            if input="#Ping" then
                clientSocket.Send(Encoding.ASCII.GetBytes("ACK")) |> ignore
            else
                printfn "Received: %s" input

                // check for commands
                if command="add" || command="subtract" || command="multiply" then
                    if Array.length numbers < 2 then
                        // number of inputs less than 2
                        printfn "Responding to client %s with result: -2" id
                        clientSocket.Send(Encoding.ASCII.GetBytes("-2")) |> ignore
                    else if Array.length numbers > 4 then
                        // number of inputs more than 4
                        printfn "Responding to client %s with result: -3" id
                        clientSocket.Send(Encoding.ASCII.GetBytes("-3")) |> ignore
                    else
                        try
                            let result =
                                match command with
                                | "add" -> numbers |> Array.map Int32.Parse |> Array.sum
                                | "subtract" -> numbers |> Array.map Int32.Parse |> Array.reduce (-)
                                | "multiply" -> numbers |> Array.map Int32.Parse |> Array.reduce (*)
                                | _ -> 0 // Handle invalid command appropriately
                            let resultStr: string = result.ToString()
                            printfn "Responding to client %s with result: %s" id resultStr
                            clientSocket.Send(Encoding.ASCII.GetBytes(resultStr)) |> ignore
                        with
                            // error in paring integer
                            | :? FormatException ->
                                printfn "Responding to client %s with result: -4" id
                                clientSocket.Send(Encoding.ASCII.GetBytes("-4")) |> ignore
                else if command.Trim()="bye" then
                    printfn "Responding to client %s with result: -5" id
                    // Send the response back to the client
                    clientSocket.Send(Encoding.ASCII.GetBytes("-5")) |> ignore
                    clientSocket.Close()
                    raise (System.Exception("Client "+id+" terminated"))
                else if command.Trim()="terminate" then
                    printfn "Responding to client %s with result: -5" id
                    clientSocket.Send(Encoding.ASCII.GetBytes("-5")) |> ignore
                    clientSocket.Close()
                    printf "Server Shutdown"
                    Environment.Exit(1)
                else
                    // command not recognized
                    printfn "Responding to client %s with result: -1" id
                    clientSocket.Send(Encoding.ASCII.GetBytes("-1")) |> ignore
    // handling exceptions
    with
    | :? System.Net.Sockets.SocketException as ex ->
        printfn "SocketException: %s" ex.Message
    | :? System.IndexOutOfRangeException as ex ->
        printfn "Client %s unexpectedly terminated the connection." clientInfo.ClientId
    | ex ->
        printfn "%s" ex.Message


let main argv =
    
    let a = Environment.GetCommandLineArgs()
    if Array.length a > 2 then
        port <- int a.[2]

    // Create a TCP listener that listens on any available IP address and port 8888    
    let listener = new TcpListener(IPAddress.Any, port)

    
    // Start listening for incoming client connections
    listener.Start()
    let mutable id: int = 0
    printfn "Server is running and listening on port %s." (string port)
    while true do
        // Accept an incoming client connection and get the client socket and responding hello
        let clientSocket = listener.AcceptSocket()
        clientSocket.Send(Encoding.ASCII.GetBytes("Hello!")) |> ignore
        
        // Generate a unique identifier for the client.
        id <- id+1
        let clientId: string = string id

        printfn "Client %s connected" clientId
        // Create a ClientInfo record to store client information
        let clientInfo = { ClientId = clientId; Socket = clientSocket }

        // Create a callback to handle the client on a separate thread
        let callback = new WaitCallback(fun state -> handleClient (state :?> ClientInfo))

        // Queue the client handling function for execution on a thread pool thread
        ThreadPool.QueueUserWorkItem(callback, clientInfo) |> ignore

    0 // return an integer exit code
main [||]