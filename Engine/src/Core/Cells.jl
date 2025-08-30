module Cells
    function removeNothing(cells)::Matrix{String}
        string.(replace(cells, missing => ""))
    end
end