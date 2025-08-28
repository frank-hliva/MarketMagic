module Money
    module File

    function new(inputStream::IOStream)::Main.Model.DataTable
        using CSV, DataFrames
        dataFrame = DataFrame(
            Subject = String[],
            Note = String[],
            Price = Float64[]
        )
        CSV.write(
            inputStream,
            dataFrame;
            delim = ";",
            writeheader = true,
            comment = "#",
            quotechar = '"',
            escapechar = '"'
        )
        seek(inputStream, 0)
        return load(inputStream)
    end

    function load(inputStream::IOStream)::Main.Model.DataTable
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
        
        Main.Model.DataTable(
            columns = string.(names(dataFrame)),
            cells = Cells.removeNothing(Matrix(dataFrame))
        )
    end

    function save(outputStream::IOStream, dataTable::Main.Model.DataTable)
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