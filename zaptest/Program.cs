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

    [Verb("list", HelpText = "list zap transactions")]
    class ListOptions
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

        [Option('A', "attachment", Required = false, HelpText = "Attachment (user defined data) associated with this transaction")]
        public string Attachment { get; set; }
    }

    class Program
    {
        static readonly string ASSET_ID = "35twb3NRL7cZwD1JjPHGzFLQ1P4gtUutTuFEXAg1f1hG";

        static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<ShowOptions, ListOptions, SpendOptions>(args)
                .MapResult(
                (ShowOptions opts) => RunShowAndReturnExitCode(opts),
                (ListOptions opts) => RunListAndReturnExitCode(opts),
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
            var asset = node.GetAsset(ASSET_ID);
            var balance = node.GetBalance(account.Address, asset);
            Console.WriteLine($"Balance: {balance}");

            return 0;
        }

        static int RunListAndReturnExitCode(ListOptions opts)
        {
            // get account
            var account = GetAccount(opts.Seed, opts.Base58);
            Console.WriteLine($"Address: {account.Address}");

            // get transaction list
            var node = new Node();
            var txs = node.ListTransactions(account.Address);
            Console.WriteLine("Txs:");            
            foreach (var tx in txs)
                Console.WriteLine(JsonConvert.SerializeObject(tx.GetJson(), Formatting.Indented));

            return 0;
        }

        static int RunSpendAndReturnExitCode(SpendOptions opts)
        {
            decimal fee = 0.1M;

            // get asset details
            var node = new Node();
            var asset = new Node().GetAsset(ASSET_ID);

            // get account
            var account = GetAccount(opts.Seed, opts.Base58);
            Console.WriteLine($"Source Address: {account.Address}");

            // encode attachment
            byte[] attachment = null;
            if (!string.IsNullOrEmpty(opts.Attachment))
            {
                attachment = Encoding.UTF8.GetBytes(opts.Attachment);
                Console.WriteLine($"Base58 Attachment: {opts.Attachment} ({attachment.Length})");
            }

            // create transaction to transfer our asset (using fee sponsorship)
            var tx = new TransferTransaction(account.PublicKey, DateTime.UtcNow, opts.Recipient, asset, opts.Amount, fee, asset, attachment);
            tx.Sign(account);
            Console.WriteLine("Transaction:");
            Console.WriteLine(JsonConvert.SerializeObject(tx, Formatting.Indented));

            // broadcast the transaction
            Console.WriteLine("Broadcasting transaction...");
            Console.WriteLine(node.Broadcast(tx));

            return 0;
        }
    }
}
