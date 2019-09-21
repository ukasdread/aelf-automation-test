using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElfChain.Console.Commands;
using log4net;

namespace AElfChain.Console
{
    public class TransactionScripts
    {
        private INodeManager NodeManager { get; set; }

        private ILog Logger = Log4NetHelper.GetLogger();

        public List<BaseCommand> Commands;

        public TransactionScripts(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
            Commands = new List<BaseCommand>();
            InitializeCommands();
        }

        public async Task ExecuteTransactionCommand()
        {
            while (true)
            {
                var input = GetUsageInfo();
                var result = int.TryParse(input, out var select);
                if (!result || select > Commands.Count)
                {
                    Logger.Error("Wrong input selection.");
                    continue;
                }

                var command = Commands[select - 1];
                $"Name: {command.GetCommandInfo()}".WriteSuccessLine();
                command.RunCommand();

                "Quit transaction execution(yes/no)? ".WriteWarningLine(changeLine: false);
                input = System.Console.ReadLine();
                if(input.ToLower().Trim().Equals("yes"))
                    break;
            }
        }

        public void InitializeCommands()
        {
            Commands.Add(new TransferCommand(NodeManager));
            Commands.Add(new QueryTokenCommand(NodeManager));
            Commands.Add(new DeployCommand(NodeManager));
            Commands.Add(new ResourceTradeCommand(NodeManager));
            Commands.Add(new SetConnectorCommand(NodeManager));
            Commands.Add(new SetTransactionFeeCommand(NodeManager));
            Commands.Add(new SwitchOtherChainCommand(NodeManager));
        }

        public string GetUsageInfo()
        {
            var count = 1;
            $"=====================Command=====================".WriteSuccessLine();
            foreach (var command in Commands)
            {
                $"{count:0}. {command.GetCommandInfo()}".WriteSuccessLine();
                count++;
            }
            $"=================================================".WriteSuccessLine();
            $"Please input which command you want to execution: ".WriteSuccessLine(changeLine:false);
            return System.Console.ReadLine();
        }
    }
}