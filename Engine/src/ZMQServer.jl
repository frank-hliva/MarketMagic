module ZMQServer

using ZMQ
using JSON3
using MLStyle

# include("Ebay/UploadTemplate.jl")
using Main.Ebay

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

#= function tryGetUploadTemplateHeader(input::String)::Union{Nothing, Tuple{String, String}}
    @match match(r"^Info;Version=([^;]+);Template=([^;]+)$", input) begin
        nothing => nothing
        m => (m.captures[1], m.captures[2])
    end
end

function tryGetUploadTemplateHeader(stream::IOStream)::Union{Nothing, Tuple{String, String}}
    local currentPosition = position(stream)
    local result = tryGetUploadTemplateHeader(stream)
    seek(stream, currentPosition)
    return result
end

function handleLoadUploadTemplate(path::String)
    try
        state.uploadDataTable = open(path) do templateStream
            @match tryGetUploadTemplateHeader(templateStream) begin
                nothing => nothing
                _ => Ebay.UploadTemplate.load(templateStream)
            end
        end
        if state.uploadDataTable === nothing
            return Dict(
                "success" => false,
                "error" => "Incorrect CSV file type, please load an upload template."
            )
        else
            return Dict(
                "success" => true,
                "message" => "Upload template loaded successfully from: $path"
            )
        end
    catch e
        return Dict(
            "success" => false,
            "error" => "Failed to load upload template: $(string(e))"
        )
    end
end =#

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

        local exportedData = open(path) do exportedDataStream
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

function handleSaveUploadTemplate(path::String, uploadDataTable::Main.Ebay.UploadDataTable)
    try
        state.uploadDataTable = uploadDataTable

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
            uploadDataTableDict = get(commandData, "uploadDataTable", "")
            if isempty(path)
                return Dict("success" => false, "error" => "Path parameter required")
            end

            local uploadDataTable = Main.Ebay.UploadDataTable(
                id = uploadDataTableDict["id"],
                columns = uploadDataTableDict["columns"],
                enums = uploadDataTableDict["enums"],
                cells = nestedArrayToMatrix(Vector{Vector{String}}(uploadDataTableDict["cells"]))
            )
            handleSaveUploadTemplate(path, uploadDataTable)
        end
        
        "fetchUploadTemplate" => handleFetchUploadTemplate()
        
        _ => Dict(
            "success" => false,
            "error" => "Unknown command: $command"
        )
    end
end

function startServer(port::Int = 7333)
    context = Context()
    socket = Socket(context, REP)
    
    try
        bind(socket, "tcp://*:$port")
        println("\n\e[38;5;183mMarketMagic\e[38;5;75m ZMQ Server\e[0m started on port \e[93m$port\e[0m ✅\n")
        println("Available commands:")
        println("  • \e[38;5;43mloadUploadTemplate\e[0m")
        println("  • \e[38;5;43maddExportedData\e[0m")
        println("  • \e[38;5;43msaveUploadTemplate\e[0m")
        println("  • \e[38;5;43mfetchUploadTemplate\e[0m")
        println("\nWaiting for requests...\n")
        
        while true
            requestBytes = recv(socket)
            requestString = String(requestBytes)
            
            println("\e[38;5;75mReceived request\e[0m: $requestString")
            
            try
                commandData = JSON3.read(requestString, Dict{String, Any})
                
                response = handleCommand(commandData)
                
                responseString = JSON3.write(response)
                send(socket, responseString)
                
                println("\e[38;5;43mSent response\e[0m: \e[93m$responseString\e[0m")
                
            catch e
                errorResponse = Dict(
                    "success" => false,
                    "error" => "Invalid request format: $(string(e))"
                )
                responseString = JSON3.write(errorResponse)
                send(socket, responseString)
                
                println("\e[91mSent error response\e[0m: $responseString")
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
