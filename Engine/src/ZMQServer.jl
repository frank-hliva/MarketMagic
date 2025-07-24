module ZMQServer

using ZMQ
using JSON3
using MLStyle

include("Ebay/UploadTemplate.jl")
using .Ebay

mutable struct ServerState
    uploadDataTable::Union{Nothing, Ebay.UploadDataTable}
end

const state = ServerState(nothing)

function matrixToNestedArray(matrix::Matrix{String})
    return [collect(row) for row in eachrow(matrix)]
end

function nestedArrayToMatrix(nestedArray::Vector{Vector{String}})
    if isempty(nestedArray)
        return Matrix{String}(undef, 0, 0)
    end
    rows = length(nestedArray)
    cols = length(nestedArray[1])
    matrix = Matrix{String}(undef, rows, cols)
    for (i, row) in enumerate(nestedArray)
        matrix[i, :] = row
    end
    return matrix
end

function handleLoadUploadTemplate(path::String)
    try
        state.uploadDataTable = open(path) do templateStream
            Ebay.UploadTemplate.load(templateStream)
        end
        return Dict(
            "success" => true,
            "message" => "Upload template loaded successfully from: $path"
        )
    catch e
        return Dict(
            "success" => false,
            "error" => "Failed to load upload template: $(string(e))"
        )
    end
end

function handleAddExportedData(path::String)
    try
        if state.uploadDataTable === nothing
            return Dict(
                "success" => false,
                "error" => "No upload template loaded. Load upload template first."
            )
        end

        exportedData = open(path) do exportedDataStream
            Ebay.ExportedData.load(exportedDataStream)
        end

        state.uploadDataTable = Ebay.UploadTemplate.withCells(exportedData, state.uploadDataTable)
        
        return Dict(
            "success" => true,
            "message" => "Exported data added successfully from: $path"
        )
    catch e
        return Dict(
            "success" => false,
            "error" => "Failed to add exported data: $(string(e))"
        )
    end
end

function handleSaveUploadTemplate(path::String)
    try
        if state.uploadDataTable === nothing
            return Dict(
                "success" => false,
                "error" => "No upload template to save. Load upload template first."
            )
        end

        open(path, "w") do outputStream
            Ebay.UploadTemplate.save(outputStream, state.uploadDataTable)
        end
        
        return Dict(
            "success" => true,
            "message" => "Upload template saved successfully to: $path"
        )
    catch e
        return Dict(
            "success" => false,
            "error" => "Failed to save upload template: $(string(e))"
        )
    end
end

function handleFetchUploadTemplate()
    try
        if state.uploadDataTable === nothing
            return Dict(
                "success" => false,
                "error" => "No upload template loaded."
            )
        end

        return Dict(
            "success" => true,
            "data" => Dict(
                "id" => state.uploadDataTable.id,
                "columns" => state.uploadDataTable.columns,
                "enums" => state.uploadDataTable.enums,
                "cells" => matrixToNestedArray(state.uploadDataTable.cells)
            )
        )
    catch e
        return Dict(
            "success" => false,
            "error" => "Failed to fetch upload template: $(string(e))"
        )
    end
end

function handleCommand(commandData::Dict)
    command = get(commandData, "command", "")
    
    @match command begin
        "loadUploadTemplate" => begin
            path = get(commandData, "path", "")
            if isempty(path)
                return Dict("success" => false, "error" => "Path parameter required")
            end
            handleLoadUploadTemplate(path)
        end
        
        "addExportedData" => begin
            path = get(commandData, "path", "")
            if isempty(path)
                return Dict("success" => false, "error" => "Path parameter required")
            end
            handleAddExportedData(path)
        end
        
        "saveUploadTemplate" => begin
            path = get(commandData, "path", "")
            if isempty(path)
                return Dict("success" => false, "error" => "Path parameter required")
            end
            handleSaveUploadTemplate(path)
        end
        
        "fetchUploadTemplate" => handleFetchUploadTemplate()
        
        _ => Dict(
            "success" => false,
            "error" => "Unknown command: $command"
        )
    end
end

function startServer(port::Int = 5555)
    context = Context()
    socket = Socket(context, REP)
    
    try
        bind(socket, "tcp://*:$port")
        println("ZMQ Server started on port $port")
        println("Available commands:")
        println("  - loadUploadTemplate")
        println("  - addExportedData")
        println("  - saveUploadTemplate")
        println("  - fetchUploadTemplate")
        println("Waiting for requests...")
        
        while true
            requestBytes = recv(socket)
            requestString = String(requestBytes)
            
            println("Received request: $requestString")
            
            try
                commandData = JSON3.read(requestString, Dict{String, Any})
                
                response = handleCommand(commandData)
                
                responseString = JSON3.write(response)
                send(socket, responseString)
                
                println("Sent response: $responseString")
                
            catch e
                errorResponse = Dict(
                    "success" => false,
                    "error" => "Invalid request format: $(string(e))"
                )
                responseString = JSON3.write(errorResponse)
                send(socket, responseString)
                
                println("Sent error response: $responseString")
            end
        end
        
    catch e
        println("Server error: $(string(e))")
    finally
        close(socket)
        close(context)
    end
end

function stopServer()
    println("Server stopped")
end

export startServer, stopServer

end
