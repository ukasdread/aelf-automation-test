using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElfChain.Console.InputOption;
using AElfChain.SDK;
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class BlockChainCommand : BaseCommand
    {
        private ConsoleReader Reader { get; set; }
        private ApiCompletionEngine AutoEngine { get; set; }
        public IApiService ApiService => NodeManager.ApiService;
        
        public BlockChainCommand(INodeManager nodeManager, ContractServices contractServices) : base(nodeManager, contractServices)
        {
            Logger = Log4NetHelper.GetLogger();
            AutoEngine = new ApiCompletionEngine();
        }

        public override void RunCommand()
        {
            Reader = new ConsoleReader(AutoEngine);
            var input = CommandOption.InputParameters(1, Reader);
            var command = input[0].Trim();
            switch (command)
            {
                 case "BlockHeight":
                     GetBlockHeight();
                     break;
                 case "BlockByHash":
                     GetBlockByHash();
                     break;
                 case "BlockByHeight":
                     GetBlockByHeight();
                     break;
                 case "TransactionPoolStatus":
                     GetTransactionPoolStatus();
                     break;
                 case "BlockState":
                     GetBlockState();
                     break;
                 case "CurrentRoundInformation":
                     break;
                 case "ChainStatus":
                     GetChainStatus();
                     break;
                 case "TaskQueueStatus":
                     GetTaskQueueStatus();
                     break;
                 case "TransactionResult":
                     GetTransactionResult();
                     break;
                 case "TransactionResults":
                     GetTransactionResults();
                     break;
                 default:
                     Logger.Error("Not supported api method.");
                     break;
            }
        }

        private void GetBlockHeight()
        {
            var height = AsyncHelper.RunSync(ApiService.GetBlockHeightAsync);
            Logger.Info($"Current chain height: {height}");
        }

        private void GetBlockByHash()
        {
            "Parameter: [BlockHash] [IncludeTransaction]=false".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var hash = input[0];
            var includeTransaction = input.Length != 1 && bool.Parse(input[1]);
            var block = AsyncHelper.RunSync(() => ApiService.GetBlockAsync(hash, includeTransaction));
            Logger.Info(JsonConvert.SerializeObject(block, Formatting.Indented));
        }

        private void GetBlockByHeight()
        {
            "Parameter: [Height] [IncludeTransaction]=false".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var height = long.Parse(input[0]);
            var includeTransaction = input.Length != 1 && bool.Parse(input[1]);
            var block = AsyncHelper.RunSync(() => ApiService.GetBlockByHeightAsync(height, includeTransaction));
            Logger.Info(JsonConvert.SerializeObject(block, Formatting.Indented));
        }

        private void GetTransactionPoolStatus()
        {
            var transactionPoolStatusInfo = AsyncHelper.RunSync(ApiService.GetTransactionPoolStatusAsync);
            Logger.Info(JsonConvert.SerializeObject(transactionPoolStatusInfo, Formatting.Indented));
        }

        private void GetBlockState()
        {
            "Parameter: [BlockHash]".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var hash = input[0];
            var blockState = AsyncHelper.RunSync(() => ApiService.GetBlockStateAsync(hash));
            Logger.Info(JsonConvert.SerializeObject(blockState, Formatting.Indented));
        }
        
        private void GetChainStatus()
        {
            var chainInfo = AsyncHelper.RunSync(ApiService.GetChainStatusAsync);
            Logger.Info(JsonConvert.SerializeObject(chainInfo, Formatting.Indented));
        }

        private void GetTaskQueueStatus()
        {
            var taskQueueInfo = AsyncHelper.RunSync(ApiService.GetTaskQueueStatusAsync);
            Logger.Info(JsonConvert.SerializeObject(taskQueueInfo, Formatting.Indented));
        }

        private void GetTransactionResult()
        {
            "Parameter: [TransactionId]".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var transactionId = input[0];
            var resultDto = AsyncHelper.RunSync(() => ApiService.GetTransactionResultAsync(transactionId));
            Logger.Info(JsonConvert.SerializeObject(resultDto, Formatting.Indented));
        }

        private void GetTransactionResults()
        {
            "Parameter: [BlockHash] [Offset]=0 [Limit]=10".WriteSuccessLine();
            var input = CommandOption.InputParameters(1);
            var blockHash = input[0];
            var offset = input.Length>=2 ? int.Parse(input[1]) : 0;
            var limit = input.Length == 3 ? int.Parse(input[2]) : 10;
            var resultDto = AsyncHelper.RunSync(() => ApiService.GetTransactionResultsAsync(blockHash, offset, limit));
            Logger.Info(JsonConvert.SerializeObject(resultDto, Formatting.Indented));
        }

        public override string GetCommandInfo()
        {
            return "Query block chain api";
        }

        public override string[] InputParameters()
        {
            throw new System.NotImplementedException();
        }
    }
}