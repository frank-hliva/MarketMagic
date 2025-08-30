module ZMQServer

using ZMQ, JSON3, MLStyle, Main.Ebay, Main.Money

mutable struct ServerState
    uploadDataTable::Union{Nothing, Ebay.UploadDataTable}
    moneyDataTable::Union{Nothing, Main.Model.DataTable}
end

const state = ServerState(nothing, nothing)

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
        open(path) do templateStream
            @match Ebay.UploadTemplate.tryGetHeader(templateStream) begin
                nothing => begin
                    Dict(
                        "success" => false,
                        "error" => "Invalid format: csv file \"$path\" is not an upload template.\nPlease open the correct upload template"
                    )
                end
                _ => begin
                    state.uploadDataTable = Ebay.UploadTemplate.load(templateStream)
                    Dict(
                        "success" => true,
                        "message" => "Upload template loaded successfully from: $path"
                    )
                end 
            end
        end
    catch e
        Dict(
            "success" => false,
            "error" => "Failed to load upload template",
            "internalError" => string(e)
        )
    end
end

function handleLoadDocument(path::String)
    try
        if state.uploadDataTable === nothing
            Dict(
                "success" => false,
                "error" => "No upload template loaded. Load upload template first."
            )
        else
            open(path) do documentStream
                @match Ebay.UploadTemplate.tryGetHeader(documentStream) begin
                    nothing => begin
                        local document = Ebay.Document.load(documentStream)
                        state.uploadDataTable = Ebay.UploadTemplate.withCells(document, state.uploadDataTable)
                        Dict(
                            "success" => true,
                            "message" => "Document added successfully from: $path"
                        )
                    end
                    _ => begin
                        Dict(
                            "success" => false,
                            "error" => "Invalid format: csv file \"$path\" is an upload template.\nPlease open the correct csv data file"
                        )
                    end
                end
            end
        end
    catch e
        Dict(
            "success" => false,
            "error" => "Failed to add exported data",
            "internalError" => string(e)
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
            "error" => "Failed to save upload template",
            "internalError" => string(e)
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
            "error" => "Failed to fetch upload template",
            "internalError" => string(e)
        )
    end
end

function handleMoneyDocumentFetch()
    try
        if state.moneyDataTable === nothing
            return Dict(
                "success" => false,
                "error" => "No money document loaded."
            )
        end

        return Dict(
            "success" => true,
            "data" => Dict(
                "id" => -1,
                "columns" => state.moneyDataTable.columns,
                "enums" => Dict([]),
                "cells" => matrixToNestedArray(state.moneyDataTable.cells)
            )
        )

    catch e
        return Dict(
            "success" => false,
            "error" => "Failed to fetch money document",
            "internalError" => string(e)
        )
    end
end

function handleMoneyDocumentNew()
    try
        state.moneyDataTable = Money.File.new()
        return Dict(
            "success" => true,
            "message" => "Money document created"
        )
    catch e
        return Dict(
            "success" => false,
            "error" => "Failed to create money document",
            "internalError" => string(e)
        )
    end
end

function handleMoneyDocumentLoad(path::String)
    try
        open(path) do stream
            state.moneyDataTable = Money.File.load(stream)
        end
        return Dict(
            "success" => true,
            "message" => "Money document loaded at: $path"
        )
    catch e
        return Dict(
            "success" => false,
            "error" => "Failed to load money document",
            "internalError" => string(e)
        )
    end
end

function handleMoneyDocumentSave(path::String, dataTableDict)
    try
        local dataTable = Main.Model.DataTable(
            columns = dataTableDict["columns"],
            cells = dataTableDict["cells"]
        )
        open(path, "w") do stream
            Money.File.save(stream, dataTable)
        end
        state.moneyDataTable = dataTable
        return Dict(
            "success" => true,
            "message" => "Money document saved to: $path"
        )
    catch e
        return Dict(
            "success" => false,
            "error" => "Failed to save money document",
            "internalError" => string(e)
        )
    end
end

function handleCommand(commandData::Dict)
    command = get(commandData, "command", "")
    
    @match command begin
        # UPLOAD TEMPLATE

        "eBay.UploadTemplate.fetch" => handleFetchUploadTemplate()

        "eBay.UploadTemplate.load" => begin
            path = get(commandData, "path", "")
            if isempty(path)
                return Dict("success" => false, "error" => "Path parameter required")
            end
            handleLoadUploadTemplate(path)
        end

        "eBay.UploadTemplate.save" => begin
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

        "eBay.Document.load" => begin
            path = get(commandData, "path", "")
            if isempty(path)
                return Dict("success" => false, "error" => "Path parameter required")
            end
            handleLoadDocument(path)
        end

        # MONEY DOCUMENT

        "Money.Document.fetch" => handleMoneyDocumentFetch()

        "Money.Document.new" => begin
            handleMoneyDocumentNew()
        end

        "Money.Document.load" => begin
            path = get(commandData, "path", "")
            if isempty(path)
                return Dict("success" => false, "error" => "Path parameter required")
            end
            handleMoneyDocumentLoad(path)
        end
        
        "Money.Document.save" => begin
            path = get(commandData, "path", "")
            dataTableDict = get(commandData, "dataTable", nothing)
            if isempty(path) || dataTableDict === nothing
                return Dict("success" => false, "error" => "Path and dataTable required")
            end
            handleMoneyDocumentSave(path, dataTableDict)
        end
        
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
        println("  • \e[38;5;43mloadDocument\e[0m")
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
                    "error" => "Invalid request format",
                    "internalError" => string(e)
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
