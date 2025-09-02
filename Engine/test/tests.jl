using ZMQ, JSON3, MLStyle, Main.Ebay, Main.Money

requestString = read("C:/Workspace/MarketMagic/Engine/test/tests.json", String)
#commandData = JSON3.read(requestString, Dict{String, Any})
commandData = unboxCommandData(requestString)
println("::: commandData", unboxCommandData(requestString))

ZMQServer.handleCommand(commandData)

ZMQServer.handleLoadUploadTemplate("b:/VikingTekDIY/2025-08-30/eBay-category-listing-template-Aug-31-2025-1-17-35.csv")
ZMQServer.handleLoadDocument("b:/VikingTekDIY/2025-08-30/eBay-all-active-listings-report-2025-08-30-13247128645.csv")
ZMQServer.handleSaveUploadTemplate("b:/VikingTekDIY/2025-08-30/save.csv", ZMQServer.state.uploadDataTable)

open("b:/VikingTekDIY/2025-08-30/eBay-category-listing-template-Aug-31-2025-1-17-35.csv") do stream
    template = Main.Ebay.UploadTemplate.load(stream)
    output = template.enums
    output
end
