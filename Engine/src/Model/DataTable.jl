module Model

@kwdef struct DataTable
    columns::Vector{String}
    cells::Matrix{String}
end

end