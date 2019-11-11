using AElf.Contracts.TokenConverter;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class ResourceTradeCommand : BaseCommand
    {
        public ResourceTradeCommand(INodeManager nodeManager, ContractServices contractServices)
            : base(nodeManager, contractServices)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;

            var beforeNativeToken = Services.Token.GetUserBalance(parameters[0]);
            var beforeResourceToken = Services.Token.GetUserBalance(parameters[0], parameters[2]);
            $"Account: {parameters[0]}, {NodeOption.NativeTokenSymbol}={beforeNativeToken}, {parameters[2]}={beforeResourceToken}"
                .WriteSuccessLine();

            var tokenConverter = Services.Genesis.GetTokenConverterStub(parameters[0]);
            if (parameters[1].Equals("buy"))
                AsyncHelper.RunSync(() => tokenConverter.Buy.SendAsync(new BuyInput
                {
                    Symbol = parameters[2],
                    Amount = long.Parse(parameters[3]),
                    PayLimit = 0
                }));

            if (parameters[1].Equals("sell"))
                AsyncHelper.RunSync(() => tokenConverter.Sell.SendAsync(new SellInput
                {
                    Symbol = parameters[2],
                    Amount = long.Parse(parameters[3]),
                    ReceiveLimit = 0
                }));

            var afterNativeToken = Services.Token.GetUserBalance(parameters[0]);
            var afterResourceToken = Services.Token.GetUserBalance(parameters[0], parameters[2]);

            $"Account: {parameters[0]}, {NodeOption.NativeTokenSymbol}={afterNativeToken}, {parameters[2]}={afterResourceToken}"
                .WriteSuccessLine();
            $"Price({NodeOption.NativeTokenSymbol}/{parameters[2]}): {(float) (beforeNativeToken - afterNativeToken) / (float) (afterResourceToken - beforeResourceToken)}"
                .WriteSuccessLine();
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "resource",
                Description = "Resource buy and sell"
            };
        }

        public override string[] InputParameters()
        {
            var from = "ZCP9k7YPHgeMM1XF94BjayULQ6hm3E5QFrsXxuPfUtJFz6sGP";
            var operation1 = "buy";
            var operation2 = "sell";
            var symbol = "STA";
            var amount = 1000;

            "Parameter: [From] [Operation] [Symbol] [Amount]".WriteSuccessLine();
            $"eg1: {from} {operation1} {symbol} {amount}".WriteSuccessLine();
            $"eg2: {from} {operation2} {symbol} {amount}".WriteSuccessLine();

            return CommandOption.InputParameters(4);
        }
    }
}