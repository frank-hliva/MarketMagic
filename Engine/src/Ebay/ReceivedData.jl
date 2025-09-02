module ReceivedData

using ZMQ, JSON3, MLStyle, Main.Ebay

function hasUploadDataTableEnums(input::Dict{String, Any})::Bool
    haskey(input, "uploadDataTable") && 
    isa(input["uploadDataTable"], Dict) && 
    haskey(input["uploadDataTable"], "enums")
end

function unboxEnums(input::Dict{String, Any})
    if hasUploadDataTableEnums(input)
        input["uploadDataTable"]["enums"] = Dict{String, Main.Ebay.Enumeration}(
            key => Main.Ebay.Enumeration(
                values = value["values"],
                isFixed = value["isFixed"]
            )
            for (key, value) in input["uploadDataTable"]["enums"]
        )
    end
    input
end

function parse(requestString)
    JSON3.read(requestString, Dict{String, Any}) |> unboxEnums
end

export parse

end