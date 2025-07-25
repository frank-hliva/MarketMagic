module Ebay

@kwdef struct DataTable
    columns::Vector{String}
    cells::Matrix{String}
end

@kwdef struct UploadDataTable
    id::Int64
    columns::Vector{String}
    enums::Dict{String, Vector{String}}
    cells::Matrix{String}
end

module UploadTemplate
    module File

        using CSV

        function readColumns(stream::IOStream)
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
        
        function readEnumRows(stream::IOStream)
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

    using CSV
    using DataFrames
    using .File

    function load(templateStream::IOStream)::Main.Ebay.UploadDataTable
        seek(templateStream, 0)
        local columns = templateStream |> File.readColumns
        seek(templateStream, 0)
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

    function withCells(dataTable::Main.Ebay.DataTable, uploadDataTable::Main.Ebay.UploadDataTable)::Main.Ebay.UploadDataTable
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

    function save(outputStream::IOStream, uploadDataTable::Main.Ebay.UploadDataTable)
        local dataFrame = DataFrame(
            uploadDataTable.cells, 
            uploadDataTable.columns
        )
        
        CSV.write(
            outputStream,
            dataFrame;
            delim = ";",
            writeheader = true,
            quotechar = '"',
            escapechar = '"'
        )
    end

    export load, withCells, save
end


module ExportedData
    using CSV
    using DataFrames

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

    module Cells
        function removeNothing(cells)::Matrix{String}
            string.(replace(cells, missing => ""))
        end
    end

    function load(exportedDataStream::IOStream, columnNameMapping::Columns.ColumnMapping)::Main.Ebay.DataTable
        local dataFrame = CSV.read(
            exportedDataStream,
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
        
        Main.Ebay.DataTable(
            columns = string.(names(dataFrame)),
            cells = Cells.removeNothing(Matrix(dataFrame))
        )
    end

    function load(exportedDataStream::IOStream)::Main.Ebay.DataTable
        load(exportedDataStream, Columns.commonMapping)
    end

    export load
end

export UploadTemplate, ExportedData, DataTable

end #module Ebay