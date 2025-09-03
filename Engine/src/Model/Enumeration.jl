@kwdef struct Enumeration
    values::Vector{String}
    isFixed::Bool
    defaultValue::Union{Nothing, String} = nothing
end