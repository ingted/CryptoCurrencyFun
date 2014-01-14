module BitcoinFs.BlockParserTests

open System
open System.Diagnostics
open System.IO
open System.Text
open NUnit.Framework
open FsUnit

// TODO find better place for this helper
let hexdump (bytes: byte[]) =
    let memStream = new MemoryStream(bytes)
    let buffer = Array.zeroCreate 16
    let read = ref 1
    let totalRead = ref 0
    while !read > 0 do
        read := memStream.Read(buffer, 0, buffer.Length)
        printf "%08x  " !totalRead
        let printBlock i =
            for x in i .. i + 3 do 
                if i <= !read then printf "%02x " buffer.[x]
                else printf "   "
            printf " "
        for i in 0 .. 4 .. 15 do 
            printBlock i
        let chars = Encoding.ASCII.GetChars(buffer)
        printf "  |"
        for x in chars do
            let printChar =
                if Char.IsControl(x) then
                    '.'
                else
                    x
            printf "%c" printChar
        printfn "|"
        totalRead := !totalRead + !read

[<Test>]
let shouldReadFirstThreeMessages() =
    let target = "/home/robert/.bitcoin/blocks/blk00000.dat"

    let stream = File.getByteStream target 
    let blocks = BlockParser.readMessages 0 3 (fun ex _ -> printfn "%O" ex) stream
    printfn "%A" blocks

[<Test>]
let shouldReadMessagesFourToFive() =
    let target = "/home/robert/.bitcoin/blocks/blk00000.dat"

    let stream = File.getByteStream target 
    let blocks = BlockParser.readMessages 3 4 (fun _ _ -> ()) stream
    printfn "%A" blocks

let errorsDir = "/home/robert/code/BitcoinFs/errors"
let writeErrorFile e message =
    if not (Directory.Exists errorsDir) then Directory.CreateDirectory errorsDir |> ignore
    let fileName = String.Format("{0:yyyy-MM-dd_HH-mm-ss-fffff}.txt", DateTime.Now)
    use file = new StreamWriter(File.OpenWrite(Path.Combine(errorsDir, fileName)))
    file.WriteLine(e.ToString())
    file.WriteLine()
    file.Write("let message = [|")
    message |> Seq.iteri(fun i x ->
                            file.Write(sprintf "0x%xuy; " x)
                            if i % 20 = 0 then
                                file.WriteLine()
                                file.Write("                "))
    file.WriteLine(" |]")

[<Test>]
let readAllMessagesSummarizeNonCanonical() =
    //let target, spec = "/home/robert/.bitcoin/blocks", "*.dat"
    let timer = Stopwatch.StartNew()
    //let stream = Directory.getByteStreamOfFiles target spec 
    let stream = File.getByteStream "/home/robert/.bitcoin/blocks/blk00000.dat" 
    let blocks = BlockParser.readAllMessages writeErrorFile stream
    let blockCounter = ref 0
    let payToAddress = ref 0
    let payToPK = ref 0
    for block in blocks do
        let outputCounter = ref 0
        for trans in block.Transactions do
            for output in trans.Outputs do
                match output.CanonicalOutputScript with
                | Some payment -> 
                    match payment with
                    | PayToPublicKey _ -> incr payToPK
                    | PayToAddress _ -> incr payToAddress
                | None -> printfn "None canonical output block: %i output: %i" !blockCounter !outputCounter
                incr outputCounter
        incr blockCounter
        if !blockCounter % 100 = 0 then printfn "done: %i" !blockCounter
    printfn "Pay To Address: %i" !payToAddress
    printfn "Pay To Public Key: %i" !payToPK
    printfn "Processed blocks: %i in %O" !blockCounter timer.Elapsed


let message = [|0x1uy; 
                0x0uy; 0x0uy; 0x0uy; 0x8buy; 0x16uy; 0x1buy; 0xc5uy; 0xc0uy; 0x84uy; 0xdcuy; 0x6auy; 0x90uy; 0x41uy; 0x25uy; 0xb5uy; 0x4cuy; 0xf4uy; 0x59uy; 0x83uy; 0x13uy; 
                0x4cuy; 0xc0uy; 0x6uy; 0x47uy; 0xbauy; 0xa3uy; 0x7uy; 0x52uy; 0x2uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x64uy; 0x62uy; 0xdduy; 0xa5uy; 0x9fuy; 
                0xe7uy; 0x8euy; 0xaeuy; 0xfauy; 0xc5uy; 0xcbuy; 0xefuy; 0x76uy; 0xd9uy; 0x2auy; 0xdeuy; 0xe2uy; 0xcduy; 0x59uy; 0xfauy; 0x76uy; 0x11uy; 0x76uy; 0xf3uy; 0x29uy; 
                0x5fuy; 0x5cuy; 0x11uy; 0xc5uy; 0x43uy; 0x8fuy; 0x4cuy; 0xcduy; 0x55uy; 0x5buy; 0x4euy; 0x86uy; 0x4auy; 0x9uy; 0x1auy; 0x96uy; 0xdeuy; 0x8uy; 0xf8uy; 0x1uy; 
                0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 
                0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0xffuy; 0xffuy; 0xffuy; 
                0xffuy; 0x41uy; 0xfcuy; 0x70uy; 0x3uy; 0x5cuy; 0x7auy; 0x81uy; 0xbcuy; 0x6fuy; 0x58uy; 0x50uy; 0x53uy; 0x68uy; 0x44uy; 0xdeuy; 0xf8uy; 0x5auy; 0xacuy; 0x9duy; 
                0x4fuy; 0xc8uy; 0x9buy; 0xb2uy; 0xcbuy; 0xeeuy; 0xb6uy; 0xe3uy; 0x63uy; 0x14uy; 0x64uy; 0x17uy; 0xb7uy; 0x4cuy; 0x5duy; 0x3uy; 0xf3uy; 0x55uy; 0xfcuy; 0x83uy; 
                0x20uy; 0x3euy; 0x63uy; 0xf0uy; 0xbuy; 0x1cuy; 0x14uy; 0xa5uy; 0xa8uy; 0x6buy; 0x4buy; 0x68uy; 0x32uy; 0x40uy; 0x44uy; 0x5uy; 0xfuy; 0xdfuy; 0x76uy; 0x39uy; 
                0xfcuy; 0x36uy; 0xceuy; 0x5duy; 0x24uy; 0x5duy; 0x24uy; 0xffuy; 0xffuy; 0xffuy; 0xffuy; 0x13uy; 0x16uy; 0x2duy; 0xb9uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 
                0x43uy; 0x41uy; 0x4uy; 0x46uy; 0x84uy; 0x3buy; 0xe4uy; 0x2uy; 0x3auy; 0x6cuy; 0xduy; 0x8duy; 0x8duy; 0x7auy; 0x5duy; 0x6auy; 0x24uy; 0xa8uy; 0xa0uy; 0xeeuy; 
                0x85uy; 0x24uy; 0x69uy; 0x0uy; 0xd2uy; 0xf9uy; 0x9duy; 0xffuy; 0xf6uy; 0x7fuy; 0xe0uy; 0x26uy; 0x8fuy; 0x35uy; 0x7buy; 0xe3uy; 0x96uy; 0x46uy; 0x50uy; 0x5euy; 
                0x4buy; 0x7duy; 0xf0uy; 0x49uy; 0xd6uy; 0x6buy; 0x6uy; 0x9auy; 0x42uy; 0x79uy; 0xb7uy; 0x73uy; 0x30uy; 0xefuy; 0xbduy; 0xf5uy; 0xfcuy; 0x76uy; 0x94uy; 0x9fuy; 
                0xfauy; 0xe8uy; 0xe5uy; 0x6duy; 0x24uy; 0x7auy; 0x12uy; 0xacuy; 0xf6uy; 0x99uy; 0x7fuy; 0x4uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0x60uy; 
                0xbeuy; 0xcauy; 0x1auy; 0x96uy; 0xauy; 0x2auy; 0x32uy; 0x30uy; 0x81uy; 0x8euy; 0xf4uy; 0x6duy; 0xdfuy; 0x7euy; 0x5euy; 0x75uy; 0x7euy; 0xe4uy; 0xafuy; 0xcduy; 
                0x69uy; 0x45uy; 0xeauy; 0xfauy; 0xcuy; 0x43uy; 0x32uy; 0x0uy; 0x6auy; 0x3uy; 0xd1uy; 0x76uy; 0x8uy; 0x2uy; 0xb5uy; 0x8cuy; 0x60uy; 0xf2uy; 0xa1uy; 0xacuy; 
                0xd7uy; 0xd7uy; 0x9cuy; 0xc0uy; 0xebuy; 0x98uy; 0x1buy; 0x5euy; 0xa4uy; 0x9uy; 0x2euy; 0xd4uy; 0x7auy; 0x73uy; 0x16uy; 0x3buy; 0xeauy; 0xbuy; 0x2buy; 0xebuy; 
                0xe3uy; 0x3fuy; 0xb2uy; 0xacuy; 0x5auy; 0x67uy; 0xb8uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0x8buy; 0x60uy; 0x23uy; 0x34uy; 0x8auy; 
                0x5cuy; 0x2cuy; 0x5cuy; 0x3euy; 0x6euy; 0x3uy; 0x1euy; 0x4buy; 0x88uy; 0x56uy; 0x2fuy; 0x22uy; 0x8buy; 0x32uy; 0xaduy; 0x42uy; 0x84uy; 0xd3uy; 0x54uy; 0x9duy; 
                0x4fuy; 0x26uy; 0x9duy; 0x61uy; 0xa7uy; 0x40uy; 0xdauy; 0xc3uy; 0xb3uy; 0x39uy; 0x48uy; 0xf2uy; 0x75uy; 0x2euy; 0x2duy; 0xb4uy; 0x1duy; 0x2fuy; 0x30uy; 0x40uy; 
                0xe0uy; 0x7duy; 0xc3uy; 0x3euy; 0xccuy; 0x76uy; 0xc4uy; 0x48uy; 0x49uy; 0xf9uy; 0x1euy; 0xc7uy; 0x4fuy; 0x9cuy; 0xaduy; 0x68uy; 0x45uy; 0x3cuy; 0xc4uy; 0xacuy; 
                0x4auy; 0x78uy; 0x7duy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0xffuy; 0xd0uy; 0x3duy; 0xe4uy; 0x4auy; 0x6euy; 0x11uy; 0xb9uy; 0x91uy; 
                0x7fuy; 0x3auy; 0x29uy; 0xf9uy; 0x44uy; 0x32uy; 0x83uy; 0xd9uy; 0x87uy; 0x1cuy; 0x9duy; 0x74uy; 0x3euy; 0xf3uy; 0xduy; 0x5euy; 0xdduy; 0xcduy; 0x37uy; 0x9uy; 
                0x4buy; 0x64uy; 0xd1uy; 0xb3uy; 0xd8uy; 0x9uy; 0x4uy; 0x96uy; 0xb5uy; 0x32uy; 0x56uy; 0x78uy; 0x6buy; 0xf5uy; 0xc8uy; 0x29uy; 0x32uy; 0xecuy; 0x23uy; 0xc3uy; 
                0xb7uy; 0x4duy; 0x9fuy; 0x5uy; 0xa6uy; 0xf9uy; 0x5auy; 0x8buy; 0x55uy; 0x29uy; 0x35uy; 0x26uy; 0x56uy; 0x66uy; 0x4buy; 0xacuy; 0xcuy; 0x25uy; 0x8buy; 0x14uy; 
                0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0xbuy; 0xe3uy; 0x5buy; 0xc9uy; 0xfbuy; 0x71uy; 0x1buy; 0xbcuy; 0x3duy; 0x28uy; 0xcauy; 0x37uy; 
                0x9buy; 0xd0uy; 0x95uy; 0x49uy; 0xc1uy; 0xefuy; 0x53uy; 0xf2uy; 0x88uy; 0xacuy; 0xefuy; 0x1duy; 0x35uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 
                0xa9uy; 0x14uy; 0x13uy; 0xc3uy; 0x18uy; 0x6duy; 0x52uy; 0x84uy; 0xa9uy; 0xf6uy; 0x4auy; 0xd6uy; 0x23uy; 0xd5uy; 0x44uy; 0x53uy; 0x9auy; 0x84uy; 0x47uy; 0xduy; 
                0x4fuy; 0x72uy; 0x88uy; 0xacuy; 0x45uy; 0x76uy; 0x80uy; 0x28uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0x54uy; 0xeauy; 0x31uy; 0x90uy; 
                0xe8uy; 0xa4uy; 0xfeuy; 0x3buy; 0x60uy; 0x6auy; 0x85uy; 0x7euy; 0xc2uy; 0x6fuy; 0x18uy; 0x87uy; 0x44uy; 0x7uy; 0x96uy; 0x92uy; 0x88uy; 0xacuy; 0xdauy; 0x72uy; 
                0xb5uy; 0x5uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0x69uy; 0x73uy; 0x6duy; 0x1fuy; 0x83uy; 0xaduy; 0xf3uy; 0xdbuy; 0xa9uy; 0x40uy; 
                0x1euy; 0xc9uy; 0xf3uy; 0x83uy; 0x70uy; 0xeeuy; 0xa7uy; 0xfeuy; 0x26uy; 0x41uy; 0x88uy; 0xacuy; 0xebuy; 0x17uy; 0x4euy; 0x4uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 
                0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0x83uy; 0x57uy; 0x13uy; 0xc9uy; 0xe0uy; 0x8fuy; 0x65uy; 0x1euy; 0x90uy; 0x71uy; 0x1fuy; 0x4cuy; 0x7cuy; 0x89uy; 0x85uy; 0xd9uy; 
                0xf6uy; 0x35uy; 0x33uy; 0xdauy; 0x88uy; 0xacuy; 0xb2uy; 0x59uy; 0xb2uy; 0x16uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0x84uy; 0xafuy; 
                0x82uy; 0xf6uy; 0x35uy; 0xbfuy; 0x5cuy; 0x5euy; 0x70uy; 0x92uy; 0x97uy; 0x63uy; 0xb1uy; 0x55uy; 0x19uy; 0xa6uy; 0x9euy; 0x3uy; 0xfcuy; 0xecuy; 0x88uy; 0xacuy; 
                0x48uy; 0x54uy; 0x26uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0x9euy; 0xb1uy; 0x93uy; 0xa2uy; 0x90uy; 0xb2uy; 0x2euy; 0xeauy; 
                0xf5uy; 0xccuy; 0x8cuy; 0x1duy; 0x9fuy; 0xf2uy; 0xc4uy; 0x56uy; 0x9uy; 0x64uy; 0x5euy; 0x3auy; 0x88uy; 0xacuy; 0xe2uy; 0x41uy; 0x27uy; 0x6uy; 0x0uy; 0x0uy; 
                0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0xbcuy; 0xf8uy; 0x2auy; 0x71uy; 0x3auy; 0x32uy; 0x9uy; 0x42uy; 0xceuy; 0x47uy; 0xe0uy; 0x73uy; 0x78uy; 0x7buy; 
                0x48uy; 0xe7uy; 0x3euy; 0xd2uy; 0x1buy; 0xc3uy; 0x88uy; 0xacuy; 0xa4uy; 0x16uy; 0x16uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 
                0xcduy; 0x81uy; 0xc2uy; 0xe5uy; 0x97uy; 0x47uy; 0x7duy; 0xe0uy; 0xf5uy; 0x7uy; 0x4auy; 0x5duy; 0xaeuy; 0xf8uy; 0x7euy; 0x8buy; 0xfuy; 0x16uy; 0x3fuy; 0x52uy; 
                0x88uy; 0xacuy; 0x2duy; 0xcduy; 0xb4uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0xd8uy; 0x45uy; 0x72uy; 0xf9uy; 0xfuy; 0x12uy; 
                0x81uy; 0x15uy; 0xecuy; 0xaauy; 0xa5uy; 0x74uy; 0x2duy; 0x66uy; 0x19uy; 0x35uy; 0x56uy; 0x56uy; 0x45uy; 0x64uy; 0x88uy; 0xacuy; 0x9auy; 0x35uy; 0xdbuy; 0x39uy; 
                0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0xdcuy; 0x8buy; 0x7buy; 0xfauy; 0x66uy; 0x4auy; 0x3fuy; 0x1euy; 0x6fuy; 0xbfuy; 0x19uy; 0x12uy; 
                0x38uy; 0x70uy; 0x21uy; 0x60uy; 0x75uy; 0x0uy; 0x22uy; 0x46uy; 0x88uy; 0xacuy; 0xcfuy; 0x4fuy; 0xcuy; 0x1auy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 
                0xa9uy; 0x14uy; 0xdcuy; 0x94uy; 0xbcuy; 0x48uy; 0xa2uy; 0xfbuy; 0xa6uy; 0x90uy; 0x94uy; 0x87uy; 0x13uy; 0xaduy; 0xd3uy; 0x13uy; 0x2duy; 0xafuy; 0xaduy; 0x4duy; 
                0x9uy; 0x7buy; 0x88uy; 0xacuy; 0xfcuy; 0xe9uy; 0x7uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0xe7uy; 0x84uy; 0xb3uy; 0xfbuy; 
                0x3euy; 0xf9uy; 0x16uy; 0x13uy; 0xabuy; 0x20uy; 0x5cuy; 0x6cuy; 0x46uy; 0xc9uy; 0x77uy; 0x85uy; 0x39uy; 0xa6uy; 0x59uy; 0xbcuy; 0x88uy; 0xacuy; 0xa2uy; 0xe4uy; 
                0x1buy; 0x8uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0xfduy; 0x35uy; 0x7cuy; 0xaeuy; 0x1uy; 0xa0uy; 0xbduy; 0x79uy; 0xb5uy; 0xb3uy; 
                0x3buy; 0xb9uy; 0x94uy; 0xbfuy; 0x2cuy; 0xe3uy; 0x93uy; 0xd8uy; 0xc1uy; 0x57uy; 0x88uy; 0xacuy; 0x97uy; 0xdfuy; 0x7cuy; 0x5cuy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 
                0x19uy; 0x76uy; 0xa9uy; 0x14uy; 0x92uy; 0x28uy; 0x97uy; 0xdcuy; 0x95uy; 0x6duy; 0x64uy; 0x4duy; 0x40uy; 0x10uy; 0xe8uy; 0xecuy; 0x8fuy; 0x39uy; 0x1fuy; 0xc5uy; 
                0x31uy; 0x1cuy; 0xc1uy; 0x22uy; 0x88uy; 0xacuy; 0x0uy; 0x0uy; 0x0uy; 0x0uy;  |]


let message2 = [|0x1uy; 
                0x0uy; 0x0uy; 0x0uy; 0xbeuy; 0xd4uy; 0x82uy; 0xccuy; 0xb4uy; 0x2buy; 0xf5uy; 0xc2uy; 0xduy; 0x0uy; 0xa5uy; 0xbbuy; 0x9fuy; 0x7duy; 0x68uy; 0x8euy; 0x97uy; 
                0xb9uy; 0x4cuy; 0x62uy; 0x2auy; 0x7fuy; 0x42uy; 0xf3uy; 0xaauy; 0xf2uy; 0x3fuy; 0x8buy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x1cuy; 0xafuy; 0xcbuy; 0x3euy; 0x4cuy; 
                0xaduy; 0x2buy; 0x4euy; 0xeduy; 0x7fuy; 0xb7uy; 0xfcuy; 0xb7uy; 0xe4uy; 0x98uy; 0x87uy; 0xd7uy; 0x40uy; 0xd6uy; 0x60uy; 0x82uy; 0xebuy; 0x45uy; 0x98uy; 0x11uy; 
                0x94uy; 0xc5uy; 0x32uy; 0xb5uy; 0x8duy; 0x47uy; 0x52uy; 0x58uy; 0xeeuy; 0x6auy; 0x49uy; 0xffuy; 0xffuy; 0x0uy; 0x1duy; 0x1buy; 0xc0uy; 0xe2uy; 0x32uy; 0x2uy; 
                0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 
                0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0xffuy; 0xffuy; 0xffuy; 
                0xffuy; 0x7uy; 0x4uy; 0xffuy; 0xffuy; 0x0uy; 0x1duy; 0x1uy; 0x1auy; 0xffuy; 0xffuy; 0xffuy; 0xffuy; 0x1uy; 0x0uy; 0xf2uy; 0x5uy; 0x2auy; 0x1uy; 0x0uy; 
                0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0x35uy; 0xd6uy; 0x6duy; 0x6cuy; 0xefuy; 0x63uy; 0xa3uy; 0x46uy; 0x11uy; 0x10uy; 0xc8uy; 0x10uy; 0x97uy; 0x5buy; 0x88uy; 
                0x16uy; 0x30uy; 0x83uy; 0x72uy; 0xb5uy; 0x82uy; 0x74uy; 0xd8uy; 0x84uy; 0x36uy; 0xa9uy; 0x74uy; 0xb4uy; 0x78uy; 0xd9uy; 0x8duy; 0x8duy; 0x97uy; 0x2fuy; 0x72uy; 
                0x33uy; 0xeauy; 0x8auy; 0x52uy; 0x42uy; 0xd1uy; 0x51uy; 0xdeuy; 0x9duy; 0x4buy; 0x1auy; 0xc1uy; 0x1auy; 0x6fuy; 0x7fuy; 0x84uy; 0x60uy; 0xe8uy; 0xf9uy; 0xb1uy; 
                0x46uy; 0xd9uy; 0x7cuy; 0x7buy; 0xaduy; 0x98uy; 0xcuy; 0xc5uy; 0xceuy; 0xacuy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x1uy; 0xbauy; 
                0x91uy; 0xc1uy; 0xd5uy; 0xe5uy; 0x5auy; 0x9euy; 0x2fuy; 0xabuy; 0x4euy; 0x41uy; 0xf5uy; 0x5buy; 0x86uy; 0x2auy; 0x73uy; 0xb2uy; 0x47uy; 0x19uy; 0xaauy; 0xd1uy; 
                0x3auy; 0x52uy; 0x7duy; 0x16uy; 0x9cuy; 0x1fuy; 0xaduy; 0x3buy; 0x63uy; 0xb5uy; 0x12uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x48uy; 0x47uy; 0x30uy; 0x44uy; 0x2uy; 
                0x20uy; 0x41uy; 0xd5uy; 0x6duy; 0x64uy; 0x9euy; 0x3cuy; 0xa8uy; 0xa0uy; 0x6fuy; 0xfcuy; 0x10uy; 0xdbuy; 0xc6uy; 0xbauy; 0x37uy; 0xcbuy; 0x95uy; 0x8duy; 0x11uy; 
                0x77uy; 0xccuy; 0x8auy; 0x15uy; 0x5euy; 0x83uy; 0xd0uy; 0x64uy; 0x6cuy; 0xd5uy; 0x85uy; 0x26uy; 0x34uy; 0x2uy; 0x20uy; 0x47uy; 0xfduy; 0x6auy; 0x2uy; 0xe2uy; 
                0x6buy; 0x0uy; 0xdeuy; 0x9fuy; 0x60uy; 0xfbuy; 0x61uy; 0x32uy; 0x68uy; 0x56uy; 0xe6uy; 0x6duy; 0x7auy; 0xduy; 0x5euy; 0x2buy; 0xc9uy; 0xd0uy; 0x1fuy; 0xb9uy; 
                0x5fuy; 0x68uy; 0x9fuy; 0xc7uy; 0x5uy; 0xc0uy; 0x4buy; 0x1uy; 0xffuy; 0xffuy; 0xffuy; 0xffuy; 0x1uy; 0x0uy; 0xe1uy; 0xf5uy; 0x5uy; 0x0uy; 0x0uy; 0x0uy; 
                0x0uy; 0x43uy; 0x41uy; 0x4uy; 0xfeuy; 0x1buy; 0x9cuy; 0xcfuy; 0x73uy; 0x2euy; 0x1fuy; 0x6buy; 0x76uy; 0xcuy; 0x5euy; 0xd3uy; 0x15uy; 0x23uy; 0x88uy; 0xeeuy; 
                0xeauy; 0xdduy; 0x4auy; 0x7uy; 0x3euy; 0x62uy; 0x1fuy; 0x74uy; 0x1euy; 0xb1uy; 0x57uy; 0xe6uy; 0xa6uy; 0x2euy; 0x35uy; 0x47uy; 0xc8uy; 0xe9uy; 0x39uy; 0xabuy; 
                0xbduy; 0x6auy; 0x51uy; 0x3buy; 0xf3uy; 0xa1uy; 0xfbuy; 0xe2uy; 0x8fuy; 0x9euy; 0xa8uy; 0x5auy; 0x4euy; 0x64uy; 0xc5uy; 0x26uy; 0x70uy; 0x24uy; 0x35uy; 0xd7uy; 
                0x26uy; 0xf7uy; 0xffuy; 0x14uy; 0xdauy; 0x40uy; 0xbauy; 0xe4uy; 0xacuy; 0x0uy; 0x0uy; 0x0uy; 0x0uy;  |]


let message3 = [|0x1uy; 
                0x0uy; 0x0uy; 0x0uy; 0x75uy; 0x61uy; 0x62uy; 0x36uy; 0xccuy; 0x21uy; 0x26uy; 0x3uy; 0x5fuy; 0xaduy; 0xb3uy; 0x8duy; 0xebuy; 0x65uy; 0xb9uy; 0x10uy; 0x2cuy; 
                0xc2uy; 0xc4uy; 0x1cuy; 0x9uy; 0xcduy; 0xf2uy; 0x9fuy; 0xc0uy; 0x51uy; 0x90uy; 0x68uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0xfeuy; 0x7duy; 0x5euy; 0x12uy; 0xefuy; 
                0xfuy; 0xf9uy; 0x1uy; 0xf6uy; 0x5uy; 0x2uy; 0x11uy; 0x24uy; 0x99uy; 0x19uy; 0xb1uy; 0xc0uy; 0x65uy; 0x37uy; 0x71uy; 0x83uy; 0x2buy; 0x3auy; 0x80uy; 0xc6uy; 
                0x6cuy; 0xeauy; 0x42uy; 0x84uy; 0x7fuy; 0xauy; 0xe1uy; 0xd4uy; 0xd2uy; 0x6euy; 0x49uy; 0xffuy; 0xffuy; 0x0uy; 0x1duy; 0x0uy; 0xf0uy; 0xa4uy; 0x41uy; 0x4uy; 
                0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 
                0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0xffuy; 0xffuy; 0xffuy; 
                0xffuy; 0x8uy; 0x4uy; 0xffuy; 0xffuy; 0x0uy; 0x1duy; 0x2uy; 0x91uy; 0x5uy; 0xffuy; 0xffuy; 0xffuy; 0xffuy; 0x1uy; 0x0uy; 0xf2uy; 0x5uy; 0x2auy; 0x1uy; 
                0x0uy; 0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0x6duy; 0x87uy; 0x9uy; 0xa0uy; 0x41uy; 0xd3uy; 0x43uy; 0x57uy; 0x69uy; 0x7duy; 0xfcuy; 0xb3uy; 0xauy; 0x9duy; 
                0x5uy; 0x90uy; 0xauy; 0x62uy; 0x94uy; 0x7uy; 0x80uy; 0x12uy; 0xbfuy; 0x3buy; 0xb0uy; 0x9cuy; 0x6fuy; 0x9buy; 0x52uy; 0x5fuy; 0x1duy; 0x16uy; 0xd5uy; 0x50uy; 
                0x3duy; 0x79uy; 0x5uy; 0xdbuy; 0x1auy; 0xdauy; 0x95uy; 0x1uy; 0x44uy; 0x6euy; 0xa0uy; 0x7uy; 0x28uy; 0x66uy; 0x8fuy; 0xc5uy; 0x71uy; 0x9auy; 0xa8uy; 0xbuy; 
                0xe2uy; 0xfduy; 0xfcuy; 0x8auy; 0x85uy; 0x8auy; 0x4duy; 0xbduy; 0xd4uy; 0xfbuy; 0xacuy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x2uy; 
                0x55uy; 0x60uy; 0x5duy; 0xc6uy; 0xf5uy; 0xc3uy; 0xdcuy; 0x14uy; 0x8buy; 0x6duy; 0xa5uy; 0x84uy; 0x42uy; 0xb0uy; 0xb2uy; 0xcduy; 0x42uy; 0x2buy; 0xe3uy; 0x85uy; 
                0xeauy; 0xb2uy; 0xebuy; 0xeauy; 0x41uy; 0x19uy; 0xeeuy; 0x9cuy; 0x26uy; 0x8duy; 0x28uy; 0x35uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x49uy; 0x48uy; 0x30uy; 0x45uy; 
                0x2uy; 0x21uy; 0x0uy; 0xaauy; 0x46uy; 0x50uy; 0x4buy; 0xaauy; 0x86uy; 0xdfuy; 0x8auy; 0x33uy; 0xb1uy; 0x19uy; 0x2buy; 0x1buy; 0x93uy; 0x67uy; 0xb4uy; 0xd7uy; 
                0x29uy; 0xdcuy; 0x41uy; 0xe3uy; 0x89uy; 0xf2uy; 0xc0uy; 0x4fuy; 0x3euy; 0x5cuy; 0x7fuy; 0x5uy; 0x59uy; 0xaauy; 0xe7uy; 0x2uy; 0x20uy; 0x5euy; 0x82uy; 0x25uy; 
                0x3auy; 0x54uy; 0xbfuy; 0x5cuy; 0x4fuy; 0x65uy; 0xb7uy; 0x42uy; 0x85uy; 0x51uy; 0x55uy; 0x4buy; 0x20uy; 0x45uy; 0x16uy; 0x7duy; 0x6duy; 0x20uy; 0x6duy; 0xfeuy; 
                0x6auy; 0x2euy; 0x19uy; 0x81uy; 0x27uy; 0xd3uy; 0xf7uy; 0xdfuy; 0x15uy; 0x1uy; 0xffuy; 0xffuy; 0xffuy; 0xffuy; 0x55uy; 0x60uy; 0x5duy; 0xc6uy; 0xf5uy; 0xc3uy; 
                0xdcuy; 0x14uy; 0x8buy; 0x6duy; 0xa5uy; 0x84uy; 0x42uy; 0xb0uy; 0xb2uy; 0xcduy; 0x42uy; 0x2buy; 0xe3uy; 0x85uy; 0xeauy; 0xb2uy; 0xebuy; 0xeauy; 0x41uy; 0x19uy; 
                0xeeuy; 0x9cuy; 0x26uy; 0x8duy; 0x28uy; 0x35uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x48uy; 0x47uy; 0x30uy; 0x44uy; 0x2uy; 0x20uy; 0x23uy; 0x29uy; 0x48uy; 0x4cuy; 
                0x35uy; 0xfauy; 0x9duy; 0x6buy; 0xb3uy; 0x2auy; 0x55uy; 0xa7uy; 0xcuy; 0x9uy; 0x82uy; 0xf6uy; 0x6uy; 0xceuy; 0xeuy; 0x36uy; 0x34uy; 0xb6uy; 0x90uy; 0x6uy; 
                0x13uy; 0x86uy; 0x83uy; 0xbcuy; 0xd1uy; 0x2cuy; 0xbbuy; 0x66uy; 0x2uy; 0x20uy; 0xcuy; 0x28uy; 0xfeuy; 0xb1uy; 0xe2uy; 0x55uy; 0x5cuy; 0x32uy; 0x10uy; 0xf1uy; 
                0xdduy; 0xdbuy; 0x29uy; 0x97uy; 0x38uy; 0xb4uy; 0xffuy; 0x8buy; 0xbeuy; 0x96uy; 0x67uy; 0xb6uy; 0x8cuy; 0xb8uy; 0x76uy; 0x4buy; 0x5auy; 0xc1uy; 0x7buy; 0x7auy; 
                0xdfuy; 0x0uy; 0x1uy; 0xffuy; 0xffuy; 0xffuy; 0xffuy; 0x2uy; 0x0uy; 0xe1uy; 0xf5uy; 0x5uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0x6auy; 
                0x7uy; 0x65uy; 0xb5uy; 0x86uy; 0x56uy; 0x41uy; 0xceuy; 0x8uy; 0xdduy; 0x39uy; 0x69uy; 0xauy; 0xaduy; 0xe2uy; 0x6duy; 0xfbuy; 0xf5uy; 0x51uy; 0x14uy; 0x30uy; 
                0xcauy; 0x42uy; 0x8auy; 0x30uy; 0x89uy; 0x26uy; 0x13uy; 0x61uy; 0xceuy; 0xf1uy; 0x70uy; 0xe3uy; 0x92uy; 0x9auy; 0x68uy; 0xaeuy; 0xe3uy; 0xd8uy; 0xd4uy; 0x84uy; 
                0x8buy; 0xcuy; 0x51uy; 0x11uy; 0xb0uy; 0xa3uy; 0x7buy; 0x82uy; 0xb8uy; 0x6auy; 0xd5uy; 0x59uy; 0xfduy; 0x2auy; 0x74uy; 0x5buy; 0x44uy; 0xd8uy; 0xe8uy; 0xd9uy; 
                0xdfuy; 0xdcuy; 0xcuy; 0xacuy; 0x0uy; 0x18uy; 0xduy; 0x8fuy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0x4auy; 0x65uy; 0x6fuy; 0x6uy; 0x58uy; 
                0x71uy; 0xa3uy; 0x53uy; 0xf2uy; 0x16uy; 0xcauy; 0x26uy; 0xceuy; 0xf8uy; 0xdduy; 0xe2uy; 0xf0uy; 0x3euy; 0x8cuy; 0x16uy; 0x20uy; 0x2duy; 0x2euy; 0x8auy; 0xd7uy; 
                0x69uy; 0xf0uy; 0x20uy; 0x32uy; 0xcbuy; 0x86uy; 0xa5uy; 0xebuy; 0x5euy; 0x56uy; 0x84uy; 0x2euy; 0x92uy; 0xe1uy; 0x91uy; 0x41uy; 0xd6uy; 0xauy; 0x1uy; 0x92uy; 
                0x8fuy; 0x8duy; 0xd2uy; 0xc8uy; 0x75uy; 0xa3uy; 0x90uy; 0xf6uy; 0x7cuy; 0x1fuy; 0x6cuy; 0x94uy; 0xcfuy; 0xc6uy; 0x17uy; 0xc0uy; 0xeauy; 0x45uy; 0xafuy; 0xacuy; 
                0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x2uy; 0x5fuy; 0x9auy; 0x6uy; 0xd3uy; 0xacuy; 0xdcuy; 0xebuy; 0x56uy; 0xbeuy; 0x1buy; 0xfeuy; 
                0xaauy; 0x3euy; 0x8auy; 0x25uy; 0xe6uy; 0x2duy; 0x18uy; 0x2fuy; 0xa2uy; 0x4fuy; 0xefuy; 0xe8uy; 0x99uy; 0xd1uy; 0xc1uy; 0x7fuy; 0x1duy; 0xaduy; 0x4cuy; 0x20uy; 
                0x28uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x48uy; 0x47uy; 0x30uy; 0x44uy; 0x2uy; 0x20uy; 0x5duy; 0x60uy; 0x58uy; 0x48uy; 0x41uy; 0x57uy; 0x23uy; 0x5buy; 0x6uy; 
                0x2uy; 0x8cuy; 0x30uy; 0x73uy; 0x6cuy; 0x15uy; 0x61uy; 0x3auy; 0x28uy; 0xbduy; 0xb7uy; 0x68uy; 0xeeuy; 0x62uy; 0x80uy; 0x94uy; 0xcauy; 0x8buy; 0x0uy; 0x30uy; 
                0xd4uy; 0xd6uy; 0xebuy; 0x2uy; 0x20uy; 0x32uy; 0x87uy; 0x89uy; 0xc9uy; 0xa2uy; 0xecuy; 0x27uy; 0xdduy; 0xaeuy; 0xc0uy; 0xaduy; 0x5euy; 0xf5uy; 0x8euy; 0xfduy; 
                0xeduy; 0x42uy; 0xe6uy; 0xeauy; 0x17uy; 0xc2uy; 0xe1uy; 0xceuy; 0x83uy; 0x8fuy; 0x3duy; 0x69uy; 0x13uy; 0xf5uy; 0xe9uy; 0x5duy; 0xb6uy; 0x1uy; 0xffuy; 0xffuy; 
                0xffuy; 0xffuy; 0x5fuy; 0x9auy; 0x6uy; 0xd3uy; 0xacuy; 0xdcuy; 0xebuy; 0x56uy; 0xbeuy; 0x1buy; 0xfeuy; 0xaauy; 0x3euy; 0x8auy; 0x25uy; 0xe6uy; 0x2duy; 0x18uy; 
                0x2fuy; 0xa2uy; 0x4fuy; 0xefuy; 0xe8uy; 0x99uy; 0xd1uy; 0xc1uy; 0x7fuy; 0x1duy; 0xaduy; 0x4cuy; 0x20uy; 0x28uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x4auy; 0x49uy; 
                0x30uy; 0x46uy; 0x2uy; 0x21uy; 0x0uy; 0xc4uy; 0x5auy; 0xf0uy; 0x50uy; 0xd3uy; 0xceuy; 0xa8uy; 0x6uy; 0xceuy; 0xdduy; 0xauy; 0xb2uy; 0x25uy; 0x20uy; 0xc5uy; 
                0x3euy; 0xbeuy; 0x63uy; 0xb9uy; 0x87uy; 0xb8uy; 0x95uy; 0x41uy; 0x46uy; 0xcduy; 0xcauy; 0x42uy; 0x48uy; 0x7buy; 0x84uy; 0xbduy; 0xd6uy; 0x2uy; 0x21uy; 0x0uy; 
                0xb9uy; 0xb0uy; 0x27uy; 0x71uy; 0x6auy; 0x6buy; 0x59uy; 0xe6uy; 0x40uy; 0xdauy; 0x50uy; 0xa8uy; 0x64uy; 0xd6uy; 0xdduy; 0x8auy; 0xeuy; 0xf2uy; 0x4cuy; 0x76uy; 
                0xceuy; 0x62uy; 0x39uy; 0x1fuy; 0xa3uy; 0xeauy; 0xbauy; 0xf4uy; 0xd2uy; 0x88uy; 0x6duy; 0x2duy; 0x1uy; 0xffuy; 0xffuy; 0xffuy; 0xffuy; 0x2uy; 0x0uy; 0xe1uy; 
                0xf5uy; 0x5uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0x6auy; 0x7uy; 0x65uy; 0xb5uy; 0x86uy; 0x56uy; 0x41uy; 0xceuy; 0x8uy; 0xdduy; 0x39uy; 
                0x69uy; 0xauy; 0xaduy; 0xe2uy; 0x6duy; 0xfbuy; 0xf5uy; 0x51uy; 0x14uy; 0x30uy; 0xcauy; 0x42uy; 0x8auy; 0x30uy; 0x89uy; 0x26uy; 0x13uy; 0x61uy; 0xceuy; 0xf1uy; 
                0x70uy; 0xe3uy; 0x92uy; 0x9auy; 0x68uy; 0xaeuy; 0xe3uy; 0xd8uy; 0xd4uy; 0x84uy; 0x8buy; 0xcuy; 0x51uy; 0x11uy; 0xb0uy; 0xa3uy; 0x7buy; 0x82uy; 0xb8uy; 0x6auy; 
                0xd5uy; 0x59uy; 0xfduy; 0x2auy; 0x74uy; 0x5buy; 0x44uy; 0xd8uy; 0xe8uy; 0xd9uy; 0xdfuy; 0xdcuy; 0xcuy; 0xacuy; 0x0uy; 0x18uy; 0xduy; 0x8fuy; 0x0uy; 0x0uy; 
                0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0x6auy; 0x7uy; 0x65uy; 0xb5uy; 0x86uy; 0x56uy; 0x41uy; 0xceuy; 0x8uy; 0xdduy; 0x39uy; 0x69uy; 0xauy; 0xaduy; 0xe2uy; 
                0x6duy; 0xfbuy; 0xf5uy; 0x51uy; 0x14uy; 0x30uy; 0xcauy; 0x42uy; 0x8auy; 0x30uy; 0x89uy; 0x26uy; 0x13uy; 0x61uy; 0xceuy; 0xf1uy; 0x70uy; 0xe3uy; 0x92uy; 0x9auy; 
                0x68uy; 0xaeuy; 0xe3uy; 0xd8uy; 0xd4uy; 0x84uy; 0x8buy; 0xcuy; 0x51uy; 0x11uy; 0xb0uy; 0xa3uy; 0x7buy; 0x82uy; 0xb8uy; 0x6auy; 0xd5uy; 0x59uy; 0xfduy; 0x2auy; 
                0x74uy; 0x5buy; 0x44uy; 0xd8uy; 0xe8uy; 0xd9uy; 0xdfuy; 0xdcuy; 0xcuy; 0xacuy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x2uy; 0xe2uy; 
                0x27uy; 0x4euy; 0x5fuy; 0xeauy; 0x1buy; 0xf2uy; 0x9duy; 0x96uy; 0x39uy; 0x14uy; 0xbduy; 0x30uy; 0x1auy; 0xa6uy; 0x3buy; 0x64uy; 0xdauy; 0xafuy; 0x8auy; 0x3euy; 
                0x88uy; 0xf1uy; 0x19uy; 0xb5uy; 0x4uy; 0x6cuy; 0xa5uy; 0x73uy; 0x8auy; 0xfuy; 0x6buy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x48uy; 0x47uy; 0x30uy; 0x44uy; 0x2uy; 
                0x20uy; 0x16uy; 0xe7uy; 0xa7uy; 0x27uy; 0xa0uy; 0x61uy; 0xeauy; 0x22uy; 0x54uy; 0xa6uy; 0xc3uy; 0x58uy; 0x37uy; 0x6auy; 0xaauy; 0x61uy; 0x7auy; 0xc5uy; 0x37uy; 
                0xebuy; 0x83uy; 0x6cuy; 0x77uy; 0xd6uy; 0x46uy; 0xebuy; 0xdauy; 0x4cuy; 0x74uy; 0x8auy; 0xacuy; 0x8buy; 0x2uy; 0x20uy; 0x19uy; 0x2cuy; 0xe2uy; 0x8buy; 0xf9uy; 
                0xf2uy; 0xc0uy; 0x6auy; 0x64uy; 0x67uy; 0xe6uy; 0x53uy; 0x1euy; 0x27uy; 0x64uy; 0x8duy; 0x2buy; 0x3euy; 0x2euy; 0x2buy; 0xaeuy; 0x85uy; 0x15uy; 0x9cuy; 0x92uy; 
                0x42uy; 0x93uy; 0x98uy; 0x40uy; 0x29uy; 0x5buy; 0xa5uy; 0x1uy; 0xffuy; 0xffuy; 0xffuy; 0xffuy; 0xe2uy; 0x27uy; 0x4euy; 0x5fuy; 0xeauy; 0x1buy; 0xf2uy; 0x9duy; 
                0x96uy; 0x39uy; 0x14uy; 0xbduy; 0x30uy; 0x1auy; 0xa6uy; 0x3buy; 0x64uy; 0xdauy; 0xafuy; 0x8auy; 0x3euy; 0x88uy; 0xf1uy; 0x19uy; 0xb5uy; 0x4uy; 0x6cuy; 0xa5uy; 
                0x73uy; 0x8auy; 0xfuy; 0x6buy; 0x1uy; 0x0uy; 0x0uy; 0x0uy; 0x4auy; 0x49uy; 0x30uy; 0x46uy; 0x2uy; 0x21uy; 0x0uy; 0xb7uy; 0xa1uy; 0xa7uy; 0x55uy; 0x58uy; 
                0x8duy; 0x41uy; 0x90uy; 0x11uy; 0x89uy; 0x36uy; 0xe1uy; 0x5cuy; 0xd2uy; 0x17uy; 0xd1uy; 0x33uy; 0xb0uy; 0xe4uy; 0xa5uy; 0x3cuy; 0x3cuy; 0x15uy; 0x92uy; 0x40uy; 
                0x10uy; 0xd5uy; 0x64uy; 0x8duy; 0x89uy; 0x25uy; 0xc9uy; 0x2uy; 0x21uy; 0x0uy; 0xaauy; 0xefuy; 0x3uy; 0x18uy; 0x74uy; 0xdbuy; 0x21uy; 0x14uy; 0xf2uy; 0xd8uy; 
                0x69uy; 0xacuy; 0x2duy; 0xe4uy; 0xaeuy; 0x53uy; 0x90uy; 0x8fuy; 0xbfuy; 0xeauy; 0x5buy; 0x2buy; 0x18uy; 0x62uy; 0xe1uy; 0x81uy; 0x62uy; 0x6buy; 0xb9uy; 0x0uy; 
                0x5cuy; 0x9fuy; 0x1uy; 0xffuy; 0xffuy; 0xffuy; 0xffuy; 0x2uy; 0x0uy; 0xe1uy; 0xf5uy; 0x5uy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0x4auy; 
                0x65uy; 0x6fuy; 0x6uy; 0x58uy; 0x71uy; 0xa3uy; 0x53uy; 0xf2uy; 0x16uy; 0xcauy; 0x26uy; 0xceuy; 0xf8uy; 0xdduy; 0xe2uy; 0xf0uy; 0x3euy; 0x8cuy; 0x16uy; 0x20uy; 
                0x2duy; 0x2euy; 0x8auy; 0xd7uy; 0x69uy; 0xf0uy; 0x20uy; 0x32uy; 0xcbuy; 0x86uy; 0xa5uy; 0xebuy; 0x5euy; 0x56uy; 0x84uy; 0x2euy; 0x92uy; 0xe1uy; 0x91uy; 0x41uy; 
                0xd6uy; 0xauy; 0x1uy; 0x92uy; 0x8fuy; 0x8duy; 0xd2uy; 0xc8uy; 0x75uy; 0xa3uy; 0x90uy; 0xf6uy; 0x7cuy; 0x1fuy; 0x6cuy; 0x94uy; 0xcfuy; 0xc6uy; 0x17uy; 0xc0uy; 
                0xeauy; 0x45uy; 0xafuy; 0xacuy; 0x0uy; 0x18uy; 0xduy; 0x8fuy; 0x0uy; 0x0uy; 0x0uy; 0x0uy; 0x43uy; 0x41uy; 0x4uy; 0x6auy; 0x7uy; 0x65uy; 0xb5uy; 0x86uy; 
                0x56uy; 0x41uy; 0xceuy; 0x8uy; 0xdduy; 0x39uy; 0x69uy; 0xauy; 0xaduy; 0xe2uy; 0x6duy; 0xfbuy; 0xf5uy; 0x51uy; 0x14uy; 0x30uy; 0xcauy; 0x42uy; 0x8auy; 0x30uy; 
                0x89uy; 0x26uy; 0x13uy; 0x61uy; 0xceuy; 0xf1uy; 0x70uy; 0xe3uy; 0x92uy; 0x9auy; 0x68uy; 0xaeuy; 0xe3uy; 0xd8uy; 0xd4uy; 0x84uy; 0x8buy; 0xcuy; 0x51uy; 0x11uy; 
                0xb0uy; 0xa3uy; 0x7buy; 0x82uy; 0xb8uy; 0x6auy; 0xd5uy; 0x59uy; 0xfduy; 0x2auy; 0x74uy; 0x5buy; 0x44uy; 0xd8uy; 0xe8uy; 0xd9uy; 0xdfuy; 0xdcuy; 0xcuy; 0xacuy; 
                0x0uy; 0x0uy; 0x0uy; 0x0uy;  |]


[<Test>]
let readFailedMessage() =
    printfn "message.Length %i" message.Length
    let block = BlockParser.readMessage message
    printfn "%A" block

[<Test>]
let dumpFailedMessage() =
    printfn "message.Length %i" message.Length
    hexdump message
