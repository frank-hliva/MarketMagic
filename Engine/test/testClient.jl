# Test client for ZMQ Server
using ZMQ
using JSON3

function testClient(port::Int = 7333)
    context = Context()
    socket = Socket(context, REQ)
    
    try
        connect(socket, "tcp://localhost:$port")
        println("Connected to server on port $port")
        
        # Test commands
        testCommands = [
            Dict("command" => "loadUploadTemplate", "path" => "./data/template.csv"),
            Dict("command" => "fetchUploadTemplate"),
            Dict("command" => "addExportedData", "path" => "./data/active.csv"),
            Dict("command" => "fetchUploadTemplate"),
            Dict("command" => "saveUploadTemplate", "path" => "./data/output_test.csv")
        ]
        
        for (i, command) in enumerate(testCommands)
            println("\n--- Test $i: $(command["command"]) ---")
            
            # Send request
            request = JSON3.write(command)
            println("Sending: $request")
            send(socket, request)
            
            # Receive response
            responseBytes = recv(socket)
            responseString = String(responseBytes)
            println("Received: $responseString")
            
            # Parse response
            response = JSON3.read(responseString, Dict{String, Any})
            
            if haskey(response, "success") && response["success"]
                println("✅ Success: $(get(response, "message", "Command executed"))")
                if haskey(response, "data")
                    data = response["data"]
                    println("Data keys: $(keys(data))")
                    if haskey(data, "cells")
                        cells = data["cells"]
                        println("Cells size: $(length(cells)) rows")
                        if !isempty(cells)
                            println("First row: $(cells[1])")
                        end
                    end
                end
            else
                println("❌ Error: $(get(response, "error", "Unknown error"))")
            end
        end
        
    catch e
        println("Client error: $(string(e))")
    finally
        close(socket)
        close(context)
    end
end

if abspath(PROGRAM_FILE) == @__FILE__
    testClient()
end
