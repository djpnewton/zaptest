using System;
using System.Text;
using WavesCS;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;

namespace test
{
    [Verb("show", HelpText = "Show zap - show funds in an account")]
    class ShowOptions
    { 
        [Option('s', "seed", Required = true, HelpText = "Seed (used to generate account)")]
        public string Seed { get; set; }

        [Option('b', "base58", Required = false, Default = false, HelpText = "Seed is base58 encoded")]
        public bool Base58 { get; set; }
    }

    [Verb("spend", HelpText = "Spend zap - send funds from an account")]
    class SpendOptions
    { 
        [Option('s', "seed", Required = true, HelpText = "Seed (used to generate account)")]
        public string Seed { get; set; }

        [Option('b', "base58", Required = false, Default = false, HelpText = "Seed is base58 encoded")]
        public bool Base58 { get; set; }

        [Option('r', "recipient", Required = true, HelpText = "The recipient account for the zap")]
        public string Recipient { get; set; }

        [Option('a', "amount", Required = true, HelpText = "Amount in 1/100s of a zap")]
        public long Amount { get; set; }
    }

    class Program
    {
        static readonly string ASSET_ID = "35twb3NRL7cZwD1JjPHGzFLQ1P4gtUutTuFEXAg1f1hG";

        static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<ShowOptions, SpendOptions>(args)
                .MapResult(
                (ShowOptions opts) => RunShowAndReturnExitCode(opts),
                (SpendOptions opts) => RunSpendAndReturnExitCode(opts),
                errs => 1);
        }

        static PrivateKeyAccount GetAccount(string seed, bool base58)
        {
            //var seed = Base58.Decode("2PLzSeA7TSYAehA5y1Qj1U5GZw9nQaHVLy2MbBwM6cCtqSS26hL6PrNNDg6k25kaWygaeEcX6KfzwPkNc9G1zLbF");          
            //var recipient = "3Mr2S13RUQzn2peQMZjf7bTrjkkUqtJaYEa";

            // create account
            var seedBytes = Encoding.UTF8.GetBytes(seed);
            if (base58)
                seedBytes = Base58.Decode(seed);
            return PrivateKeyAccount.CreateFromSeed(seedBytes, AddressEncoding.TestNet);
        }

        static int RunShowAndReturnExitCode(ShowOptions opts)
        {
            // get account
            var account = GetAccount(opts.Seed, opts.Base58);
            Console.WriteLine($"Address: {account.Address}");

            // get balance
            var node = new Node();
            var balance = node.GetBalance(account.Address, ASSET_ID);
            Console.WriteLine($"Balance: {balance}");

            return 0;
        }

        static int RunSpendAndReturnExitCode(SpendOptions opts)
        {
            long fee = 10;

            // get account
            var account = GetAccount(opts.Seed, opts.Base58);
            Console.WriteLine($"Source Address: {account.Address}");

            // create transaction to transfer our asset (using fee sponsorship)
            var tx = Transactions.MakeTransferTransaction(account, opts.Recipient, opts.Amount, ASSET_ID, fee, ASSET_ID, "");
            Console.WriteLine("Transaction:");
            Console.WriteLine(JsonConvert.SerializeObject(tx, Formatting.Indented));

            // connect to a public node and broadcast the transaction
            Console.WriteLine("Broadcasting transaction...");
            var node = new Node();
            Console.WriteLine(node.Broadcast(tx));

            return 0;
        }
    }
}
