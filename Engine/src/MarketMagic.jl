include("Ebay/UploadTemplate.jl")
using Ebay

# include("ZMQServer.jl")
# using .ZMQServer

# function main()
#     port = 5555
    
#     if length(ARGS) >= 1
#         try
#             port = parse(Int, ARGS[1])
#         catch
#             println("Warning: Invalid port argument '$(ARGS[1])', using default port $port")
#         end
#     end
    
#     println("Starting MarketMagic ZMQ Server...")
#     ZMQServer.startServer(port)
# end

# if abspath(PROGRAM_FILE) == @__FILE__
#     main()
# end
