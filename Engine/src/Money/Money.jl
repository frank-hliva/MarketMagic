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

    end
end