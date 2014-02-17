namespace BitcoinFs.Neo4jClient
open System
open System.Linq
open System.Diagnostics
open System.Collections.Generic
open BitcoinFs
open Neo4jClient
open Neo4jClient.Cypher

[<CLIMutable>]
type NeoInput = 
    { Hash: string
      Index: int
      Value: int64
      Address: string }

[<CLIMutable;>]
type NeoOutput = 
    { Value: int64
      Address: string
      Index: int }

[<CLIMutable>]
type NeoTransaction = 
    { TransactionHash: string
      TotalInputs: int64
      TotalOutputs: int64 }

[<CLIMutable>]
type NeoBlock = 
    { Hash: string
      Timestamp: DateTimeOffset
      Height: int64 } 

module LoadBlockChainModel =
    // TODO make db location configurable
    // TODO tidy up db access
    // TODO make restartable (need to store byte index)
    let client = new GraphClient(new Uri("http://localhost:7474/db/data"))
    client.Connect()

    // TODO feels like we could have a generic save method
    let saveNeoOutput transactionHash neoOutput =
        client.Cypher
            .Create("(o:Output {param})")
            .WithParam("param", neoOutput)
            .With("o")
            .Match("(t:Transaction)")
            .Where(fun t -> t.TransactionHash = transactionHash)
            .CreateUnique("t-[:output]->o")
            .ExecuteWithoutResults()

    type PayDirection = To | From

    let payAddress direction transactionHash address value transTime =
        let sign, source, dest = 
            match direction with 
            | To -> "+", "t", "a"
            | From -> "-", "a", "t"
        client.Cypher
            .Merge("(a:Address { Address: {param}})")
            .WithParam("param", address)
            .OnCreate()
            .Set("a.Balance = {balance}")
            .OnMatch()
            .Set(sprintf "a.Balance = a.Balance %s {balance}" sign)
            .With("a")
            .Match("(t:Transaction)")
            .Where(fun t -> t.TransactionHash = transactionHash)
            .Create(sprintf "%s-[:pays {Amount: {balance}, Time: {time}}]->%s" source dest)
            .WithParam("balance", value)
            .WithParam("time", transTime)
            .ExecuteWithoutResults()

    let neoOutputOfOutput transactionHash transTime i (output: Output): NeoOutput =
        let extractAddressFromScript canonicalScript =
            match canonicalScript with
            | PayToPublicKey address -> address.AsString
            | PayToAddress address -> address.AsString
        let address = output.CanonicalOutputScript |> Option.map extractAddressFromScript
        let address =
            match address with 
            | Some address -> 
                payAddress To transactionHash address output.Value transTime
                address 
            | _ -> null
        let output =
            { Value = output.Value
              Address = address
              Index = i }
        saveNeoOutput transactionHash output
        output

    let lookupTransaction transHash i =
        client.Cypher
            .Match("(t:Transaction)-[:output]->(o:Output)")
            .Where(fun t -> t.TransactionHash = transHash)
            .AndWhere(fun o -> o.Index = i)
            .Return<NeoOutput>("o")
            .Results
            .SingleOrDefault()

    let saveNeoInput transactionHash neoInput =
        client.Cypher
            .Create("(i:Input {param})")
            .WithParam("param", neoInput)
            .With("i")
            .Match("(t:Transaction)")
            .Where(fun t -> t.TransactionHash = transactionHash)
            .CreateUnique("t-[:input]->i")
            .ExecuteWithoutResults()

    let neoInputOfInput transactionHash transTime (input: Input) =
        let inputHash = Conversion.littleEndianBytesToHexString input.InputHash
        let outputAddress, outputValue =
            if input.InputTransactionIndex = -1 then
                null, 0L
            else
                let output = lookupTransaction inputHash input.InputTransactionIndex
                if output :> obj <> null then
                    output.Address, output.Value
                else
                    printfn "can't find input %s %i" inputHash input.InputTransactionIndex
                    null, 0L
        if outputAddress <> null then
            payAddress From transactionHash outputAddress outputValue transTime
        let input = 
            { Hash =  inputHash
              Index = input.InputTransactionIndex
              Address = outputAddress
              Value = outputValue }
        saveNeoInput transactionHash input
        input

    let saveNeoTrans neoTrans =
        client.Cypher
            .Create("(t:Transaction {param})")
            .WithParam("param", neoTrans)
            .ExecuteWithoutResults()

    let tansactionRelations trans blockHash =
        client.Cypher
            .Match("(t:Transaction)", "(b:Block)")
            .Where(fun t -> t.TransactionHash = trans.TransactionHash)
            .AndWhere(fun b -> b.Hash = blockHash)
            .CreateUnique("t-[:belongsTo]->b")
            .CreateUnique("t<-[:owns]-b")
            .ExecuteWithoutResults()

    let updateTransaction (trans: NeoTransaction) =
        client.Cypher
            .Match("(t:Transaction)")
            .Where(fun t -> t.TransactionHash = trans.TransactionHash)
            .Set("t.TotalInputs = {inputparam}")
            .WithParam("inputparam", trans.TotalInputs)
            .Set("t.TotalOutputs = {outputparam}")
            .WithParam("outputparam", trans.TotalOutputs)
            .ExecuteWithoutResults()

    let neoTransOfTrans (trans: Transaction) (hash: string) timestamp =
        let transactionHash = Conversion.littleEndianBytesToHexString trans.TransactionHash
        let emptyTransaction =
            { TransactionHash = transactionHash
              TotalInputs = 0L
              TotalOutputs = 0L }
        saveNeoTrans emptyTransaction
        let inputs = Array.map (neoInputOfInput transactionHash timestamp)  trans.Inputs
        let outputs = Array.mapi (neoOutputOfOutput transactionHash timestamp) trans.Outputs
        let totalInputs = inputs |> Seq.sumBy (fun x -> x.Value)
        // TODO this assumes when addresss is none, the payment goes to the minor, this is usually but not always true
        let totalOutputs = outputs |> Seq.filter (fun x -> x.Address <> null) |> Seq.sumBy (fun x -> x.Value)
        let transaction = {emptyTransaction with TotalInputs = totalInputs; TotalOutputs = totalOutputs}
        updateTransaction transaction
        tansactionRelations transaction hash

    let blockRelations prev curr =
        client.Cypher
            .Match("(p:Block)", "(c:Block)")
            .Where(fun p -> p.Hash = prev.Hash)
            .AndWhere(fun c -> c.Hash = curr.Hash)
            .CreateUnique("p-[:next]->c")
            .CreateUnique("p<-[:prev]-c")
            .ExecuteWithoutResults()

    let saveNeoBlock neoBlock =
        client.Cypher
            .Create("(b:Block {param})")
            .WithParam("param", neoBlock)
            .ExecuteWithoutResults()

    let neoBlockOfBlock (block: Block) hash height =
        let timestamp = new DateTimeOffset(block.Timestamp, new TimeSpan(0L))
        let neoBlock = 
            { Hash = hash
              Timestamp = timestamp
              Height = height }
        saveNeoBlock neoBlock
        for trans in block.Transactions do
            neoTransOfTrans trans hash timestamp
        neoBlock
 
    let scanBlocks (prevBlock, currBlock, i) (nextBlock: Block) =
        match prevBlock, currBlock with
        | Some prevNeoBlock, Some currBlock ->
            let currNeoBlock = neoBlockOfBlock currBlock (Conversion.littleEndianBytesToHexString nextBlock.Hash) i
            blockRelations prevNeoBlock currNeoBlock
            Some currNeoBlock, Some nextBlock, (i + 1L)
        | None, Some currBlock ->
            let currNeoBlock = neoBlockOfBlock currBlock (Conversion.littleEndianBytesToHexString nextBlock.Hash) i
            Some currNeoBlock, Some nextBlock, (i + 1L)
        | None, None -> 
            None, Some nextBlock, i
        | _ -> failwith "assert false"

    let addIndexes() =
        let rawClient = client :> IRawGraphClient
        let query text = new CypherQuery(text, new Dictionary<string, obj>(), CypherResultMode.Set)
        rawClient.ExecuteCypher(query "CREATE INDEX ON :Block(Hash)")
        rawClient.ExecuteCypher(query "CREATE INDEX ON :Block(Height)")
        rawClient.ExecuteCypher(query "CREATE INDEX ON :Transaction(TransactionHash)")

    let load() =
        addIndexes()
        let target = "/home/robert/.bitcoin/blocks/blk00000.dat"
        let parser = BlockParserStream.FromFile(target)
        let sw = Stopwatch.StartNew() 
        parser.StreamEnded
        |> Event.add (fun _ -> 
            printfn "Done in %O" sw.Elapsed
            Process.GetCurrentProcess().Kill())
        parser.NewBlock 
        |> Event.scan scanBlocks (None, None, 0L) 
        |> ignore
        parser.StartPushBetween 0 50

    load()

