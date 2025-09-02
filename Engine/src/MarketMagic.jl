include("Core/Cells.jl")
include("Model/DataTable.jl")
include("Ebay/UploadTemplate.jl")
include("Money/Money.jl")
include("ZMQServer.jl")

using .ZMQServer, .Ebay, .Money

function main()
    port = 7333
    
    if length(ARGS) >= 1
        try
            port = parse(Int, ARGS[1])
        catch
            println("Warning: Invalid port argument '$(ARGS[1])', using default port $port")
        end
    end
    
    println("Starting MarketMagic ZMQ Server...")
    ZMQServer.startServer(port)
end

if abspath(PROGRAM_FILE) == @__FILE__
    main()
end

# ZMQServer.handleLoadUploadTemplate("b:/VikingTekDIY/2025-08-30/eBay-category-listing-template-Aug-31-2025-1-17-35.csv")
# ZMQServer.handleLoadDocument("b:/VikingTekDIY/2025-08-30/eBay-all-active-listings-report-2025-08-30-13247128645.csv")
# ZMQServer.handleSaveUploadTemplate("b:/VikingTekDIY/2025-08-30/save.csv", ZMQServer.state.uploadDataTable)

# open("b:/VikingTekDIY/2025-08-30/eBay-category-listing-template-Aug-31-2025-1-17-35.csv") do stream
#     template = Main.Ebay.UploadTemplate.load(stream)
#     output = template.enums
#     output
# end
