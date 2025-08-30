module Ebay

@kwdef struct UploadDataTable
    id::Int64
    columns::Vector{String}
    enums::Dict{String, Vector{String}}
    cells::Matrix{String}
end

module UploadTemplate
    module File

        using CSV

        const HEADER_ID_META = "Info;Version=1.0.0;Template=fx_category_template_EBAY_DE";

        function readColumns(stream::IO)
            CSV.File(
                stream;
                header = 2,
                skipto = 2,
                limit = 0,
                delim = ";",
                comment = "#",
                silencewarnings = true
            ) |> 
            propertynames |>
            columns -> string.(columns)
        end
        
        function readEnumRows(stream::IO)
            CSV.File(
                stream;
                skipto = 3,
                header=["key", "value"],
                delim = ";",
                comment = "#",
                ignoreemptyrows = true,
                silencewarnings = true
            )
        end

        const categoryIdToken = ">>> For categoryId:"

        function parseCategoryId(enumRows)::Int64
            local foundIndex = findfirst(row -> startswith(row.value, categoryIdToken), enumRows)
            if isnothing(foundIndex)
                nothing
            else
                enumRows[foundIndex].value[length(categoryIdToken) + 1 : end] |>
                strip |>
                string |>
                numericString -> parse(Int64, numericString)
            end
        end

        const enumValueToken = ">>> The recommended value(s) for aspect "
        
        function parseEnums(text::AbstractString)
            CSV.File(
                IOBuffer(text);
                delim = ";",
                comment = "#",
                silencewarnings = true
            ) |>
            propertynames |>
            columns -> strip.(string.(columns))
        end

        function parseKeyEnumPair(text::AbstractString)
            local colonIndex = findfirst(':', text)
            if isnothing(colonIndex)
                return nothing
            end
            "C:$(strip(text[1 : colonIndex - 1]))",
            strip(text[colonIndex + 2 : end]) |> parseEnums
        end

        function parseAllEnums(enumRows)
            enumRows |> 
            data -> filter(row -> row.key == "Info" && startswith(row.value, enumValueToken), data) |>
            data -> map(((_, value),) -> value[length(enumValueToken) + 1 : end] |> string |> strip |> parseKeyEnumPair, data)
        end
    end

    using CSV, DataFrames, .File, MLStyle

    function tryGetHeader(input::String)::Union{Nothing, Tuple{String, String}}
        @match match(r"Info;Version=([^;]+);Template=([^;]+)", input) begin
            nothing => nothing
            m => (m.captures[1], m.captures[2])
        end
    end

    function tryGetHeader(stream::IO)::Union{Nothing, Tuple{String, String}}
        local currentPosition = position(stream)
        local result = stream |> readline |> tryGetHeader
        seek(stream, currentPosition)
        return result
    end

    function load(templateStream::IO)::Main.Ebay.UploadDataTable
        seekstart(templateStream)
        local columns = templateStream |> File.readColumns
        seekstart(templateStream)
        local enumRows = templateStream |> File.readEnumRows
        local enumMap = Dict(enumRows |> File.parseAllEnums)

        Main.Ebay.UploadDataTable(
            id = File.parseCategoryId(enumRows),
            columns = columns,
            enums = map(column -> begin
                column,
                haskey(enumMap, column) ? enumMap[column] : Vector{String}()
            end, columns) |> Dict,
            cells = fill("", 0, length(columns))
        )
    end

    function withCells(dataTable::Main.Model.DataTable, uploadDataTable::Main.Ebay.UploadDataTable)::Main.Ebay.UploadDataTable
        local lastRowIndex = size(uploadDataTable.cells, 1)
        local numberOfNewRows = size(dataTable.cells, 1)
        local columnCount = length(uploadDataTable.columns)

        local newUploadDataTable = Main.Ebay.UploadDataTable(
            id = uploadDataTable.id,
            columns = deepcopy(uploadDataTable.columns),
            enums = deepcopy(uploadDataTable.enums),
            cells = vcat(deepcopy(uploadDataTable.cells), fill("", numberOfNewRows, columnCount))
        )
        
        for (sourceColumnIndex, sourceColumn) in enumerate(dataTable.columns)   
            local uploadColumnIndex = findfirst(==(sourceColumn), uploadDataTable.columns)
            if uploadColumnIndex !== nothing
                newUploadDataTable.cells[(lastRowIndex + 1) : end, uploadColumnIndex] = dataTable.cells[:, sourceColumnIndex]
            end
        end
        newUploadDataTable
    end

    function save(outputStream::IO, uploadDataTable::Main.Ebay.UploadDataTable)
        println(outputStream, HEADER_ID_META)
        local buffer = IOBuffer()
        local dataFrame = DataFrame(
            uploadDataTable.cells, 
            uploadDataTable.columns
        )
        CSV.write(
            buffer,
            dataFrame;
            delim = ";",
            writeheader = true,
            quotechar = '"',
            escapechar = '"'
        )
        seekstart(buffer)
        write(outputStream, read(buffer))
    end

    export load, withCells, save
end


module Document
    using CSV, DataFrames, Main.Cells

    module Columns
        using DataFrames

        const ColumnMapping = Dict{String, String}

        const commonMapping = ColumnMapping(
            # Basic information
            "Title" => "*Title",
            "Start price" => "*StartPrice", 
            "Auction Buy It Now price" => "BuyItNowPrice",
            "Available quantity" => "*Quantity",
            "Format" => "*Format",
            "Condition" => "*ConditionID",
            "Ebay category 1 number" => "*Category",

            # Id numbers
            "Custom label (SKU)" => "CustomLabel",  # SKU id
            "P:UPC" => "C:Herstellernummer",        # UPC id
            "P:EAN" => "C:Herstellernummer",        # EAN id
        )

        function rename(columnNameMapping::ColumnMapping, dataFrame::DataFrame)
            for (oldName, newName) in columnNameMapping
                local columns = names(dataFrame)
                if oldName in columns && !(newName in columns)
                    rename!(dataFrame, oldName => newName)
                end
            end
        end
    end

    function load(documentStream::IO, columnNameMapping::Columns.ColumnMapping)::Main.Model.DataTable
        local dataFrame = CSV.read(
            documentStream,
            DataFrame;
            delim = ";",
            header = true,
            comment = "#",
            ignoreemptyrows = true,
            silencewarnings = true
        )
        
        if columnNameMapping !== nothing
            Columns.rename(columnNameMapping, dataFrame)
        end
        
        Main.Model.DataTable(
            columns = string.(names(dataFrame)),
            cells = Cells.removeNothing(Matrix(dataFrame))
        )
    end

    function load(documentStream::IO)::Main.Model.DataTable
        load(documentStream, Columns.commonMapping)
    end

    export load
end

export UploadTemplate, Document, DataTable

end #module Ebay