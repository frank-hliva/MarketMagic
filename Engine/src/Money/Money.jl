module Money
    module File

        using CSV, DataFrames, Main.Cells
        using Main.Model: DataTable


        function load(inputStream::IO)::DataTable
            local dataFrame = CSV.read(
                inputStream,
                DataFrame;
                delim = ";",
                header = true,
                comment = "#",
                quotechar = '"',
                escapechar = '"',
                ignoreemptyrows = true,
                silencewarnings = true
            )
            
            DataTable(
                columns = string.(names(dataFrame)),
                cells = Cells.removeNothing(Matrix(dataFrame))
            )
        end

        function new()::DataTable
            dataFrame = DataFrame(
                Subject = String[],
                Note = String[],
                Price = Float64[]
            )
            local inputStream = IOBuffer()
            CSV.write(
                inputStream,
                dataFrame;
                delim = ";",
                writeheader = true,
                comment = "#",
                quotechar = '"',
                escapechar = '"'
            )
            seekstart(inputStream)
            return load(inputStream)
        end

        function save(outputStream::IO, dataTable::DataTable)
            local dataFrame = DataFrame(
                dataTable.cells, 
                dataTable.columns
            )
            
            CSV.write(
                outputStream,
                dataFrame;
                delim = ";",
                writeheader = true,
                comment = "#",
                quotechar = '"',
                escapechar = '"'
            )
        end

        function sum(dataTable::DataTable)
            local priceColumnNames = ["price", "prices", "value", "amount"]
            local priceColumnIndex = findfirst(name -> lowercase(name) in priceColumnNames, dataTable.columns)
            if priceColumnIndex === nothing
                nothing
            else
                Base.sum(
                    !isempty(price) ? parse(Float64, price) : 0.0
                    for price in dataTable.cells[:, priceColumnIndex]
                )
            end
        end

        export load, new, save, sum

    end
end