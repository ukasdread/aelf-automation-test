using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Contracts.Association;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.E2ETest
{
    public class ContractTestBase
    {
        public static string MainConfig = "nodes-env1-main";
        public static string SideConfig = "nodes-env1-side1";
        public static Address AssociationOrganization;
        public static Address ReferendumOrganization;

        public ContractTestBase()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig(MainConfig);
            ConfigNodes = NodeInfoHelper.Config.Nodes;
            var firstBp = ConfigNodes.First();

            NodeManager = new NodeManager(firstBp.Endpoint);
            AuthorityManager = new AuthorityManager(NodeManager);
            ContractManager = new ContractManager(NodeManager, firstBp.Account);
            EnvCheck = EnvCheck.GetDefaultEnvCheck();
            TransferToNodes();
            AssociationOrganization = AuthorityManager.CreateAssociationOrganization();
            ReferendumOrganization = AuthorityManager.CreateReferendumOrganization();
        }

        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }
        public AuthorityManager AuthorityManager { get; set; }

        public EnvCheck EnvCheck { get; set; }
        public ILog Logger { get; set; }

        public List<Node> ConfigNodes { get; set; }

        public void TransferToNodes()
        {
            foreach (var node in ConfigNodes)
            {
                var symbol = ContractManager.Token.GetPrimaryTokenSymbol();
                var balance = ContractManager.Token.GetUserBalance(node.Account, symbol);
                if (node.Account.Equals(ContractManager.CallAddress) || balance > 10000000000) continue;
                ContractManager.Token.TransferBalance(ContractManager.CallAddress, node.Account, 100000000000,
                    symbol);
            }
        }
    }
}