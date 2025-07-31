namespace Lime

module Task =

    open System.Threading.Tasks

    let toUnitTask (task : Task<'a>) : Task<unit> =
        task.ContinueWith(System.Func<_,_>(fun (_: Task<'a>) -> ()))