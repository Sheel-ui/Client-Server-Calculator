open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading

let pingIntervalMs = 1000 
let mutable port = 8888
let main argv =

    let a = Environment.GetCommandLineArgs()
    if Array.length a > 2 then
        port <- int a.[2]
    // Create a new TCP client instance and to the localhost on port 8888
    let client = new TcpClient()
    client.Connect(IPAddress.Loopback, port)
    printfn "Connected to port %s" (string port)


    let stream = client.GetStream()
    // Create a buffer to receive the greeting message from the server
    let greetingBuffer = Array.zeroCreate<byte> 1024
    let greetingBytesReceived = stream.Read(greetingBuffer, 0, greetingBuffer.Length)
    let greetingMessage = Encoding.ASCII.GetString(greetingBuffer, 0, greetingBytesReceived)
    printfn "Server response: %s" greetingMessage


    // Thread to check if server is Running
    let pingThread = new Thread(
        (fun () ->
            try
                while true do
                    // Send a ping message to the server
                    let pingMessage = "#Ping"
                    let pingBytes = Encoding.ASCII.GetBytes(pingMessage)
                    stream.Write(pingBytes, 0, pingBytes.Length)
                    stream.Flush()
                    
                    // Wait for a response from the server
                    let buffer = Array.zeroCreate<byte> 1024
                    let bytesRead = stream.Read(buffer, 0, buffer.Length)
                    let message = Encoding.ASCII.GetString(buffer, 0, bytesRead)
                    
                    Thread.Sleep(pingIntervalMs)
            with
                | _ ->
                    // Didn't receive any response from server
                    printfn "\nServer is terminated. Initiating Client shutdown."
                    client.Close()    
                    Environment.Exit(1)
                    
        )
    )

    // Start the ping thread
    pingThread.Start()

    while true do

        // Setup the network stream for sending and receiving data
        let stream = client.GetStream()

        // Display available options to the user and take the input
        //printfn "Options:"
        //printfn "1.add 2.subtract 3.multiply"
        printf "Sending command: "
        let input = Console.ReadLine()
        //printf "Sending command: %s \n" input

        // Create a StreamWriter to write data to the network stream
        let sw = new System.IO.StreamWriter(stream)
        sw.WriteLine(input)
        sw.Flush()

        // Create a buffer to read the data from the server
        let buffer = Array.zeroCreate 1024
        let bytesRead = stream.Read(buffer, 0, buffer.Length)

        // Convert the received data to a string
        let message = Encoding.ASCII.GetString(buffer, 0, bytesRead)

        // Check the server's response and provide appropriate feedback
        if message="-1" then
            printfn "Server response: incorrect operation command"
        else if message="-2" then
            printfn "Server response: number of inputs is less than two"
        else if message="-3" then
            printfn "Server response: number of inputs is more than four"
        else if message="-4" then
            printfn "Server response: one or more of the inputs contain(s) non-number(s)"
        else if message="-5" then
            printfn "exit"
            client.Close()
            Environment.Exit(1)
        else
            printfn "Server response: %s" message
    0 // return an integer exit code
main [||]