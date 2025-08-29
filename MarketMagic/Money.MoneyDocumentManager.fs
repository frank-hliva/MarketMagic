module MarketMagic.Money

type MoneyDocumentManager(zmqServerConfig : ZMQServerConfig) =
    inherit ZMQCommandManager(zmqServerConfig)

    member self.Fetch () =
        {| command = "Money.Document.fetch" |}
        |> self.SendCommand<CommandDataResponse<DataTable>>

    member self.New () =
        {| command = "Money.Document.new" |}
        |> self.SendCommand<CommandDataResponse<DataTable>>

    member self.Load (path : string, dataTable : DataTable) =
        {| command = "Money.Document.load"; path = path; dataTable = dataTable |}
        |> self.SendCommand<CommandSaveResponse>

    member self.Save (path : string) =
        {| command = "Money.Document.save"; path = path |}
        |> self.SendCommand<CommandMessageResponse>