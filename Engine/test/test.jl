include("Ebay/UploadTemplate.jl")

using .Ebay

const uploadDataTable = open("./data/template.csv") do templateStream
    Ebay.UploadTemplate.load(templateStream)
end

const modifiedUploadDataTable = open("./data/active.csv") do exportedDataStream
    local exportedData = Ebay.ExportedData.load(exportedDataStream)
    Ebay.UploadTemplate.withCells(exportedData, uploadDataTable)
end

for cell in modifiedUploadDataTable.cells[1, :]
    print('>', cell, "\n")
end

open("./data/output.csv", "w") do outputStream
    Ebay.UploadTemplate.save(outputStream, modifiedUploadDataTable)
end