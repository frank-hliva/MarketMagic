@kwdef struct UploadDataTable
    id::Int64
    columns::Vector{String}
    enums::Dict{String, Enumeration}
    cells::Matrix{String}
end