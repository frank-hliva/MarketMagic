module MarketMagic.Money

type MoneyDocumentManager(zmqServerConfig : ZMQServerConfig) =
    inherit ZMQCommandManager(zmqServerConfig)

    member self.Fetch () =
        {| command = "Money.Document.fetch" |}
        |> self.SendCommand<CommandDataResponse<DataTable>>

    member self.New () =
        {| command = "Money.Document.new" |}
        |> self.SendCommand<CommandMessageResponse>

    member self.Load (path : string) =
        {| command = "Money.Document.load"; path = path |}
        |> self.SendCommand<CommandMessageResponse>

    member self.Save (path : string, uploadDataTable : DataTable) =
        {| command = "Money.Document.save"; path = path; uploadDataTable = uploadDataTable |}
        |> self.SendCommand<CommandSaveResponse>