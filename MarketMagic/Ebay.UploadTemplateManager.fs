module MarketMagic.Ebay

type UploadTemplateManager(zmqServerConfig : ZMQServerConfig) =
    inherit ZMQCommandManager(zmqServerConfig)

    member self.Fetch () =
        {| command = "eBay.UploadTemplate.fetch" |}
        |> self.SendCommand<CommandDataResponse<DataTable>>

    member self.Load (path : string) =
        {| command = "eBay.UploadTemplate.load"; path = path |}
        |> self.SendCommand<CommandMessageResponse>

    member self.Save (path : string, uploadDataTable : DataTable) =
        {| command = "eBay.UploadTemplate.save"; path = path; uploadDataTable = uploadDataTable |}
        |> self.SendCommand<CommandSaveResponse>

    member self.LoadDocument (path : string) =
        {| command = "eBay.Document.load"; path = path |}
        |> self.SendCommand<CommandMessageResponse>