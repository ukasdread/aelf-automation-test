using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum ParliamentMethod
    {
        //View,
        GetGenesisOwnerAddress,
        //Action
        Approve,
        CreateProposal,
        GetProposal,
        Release,
        CreateOrganization,
        
        //View
        GetGenesisOwnerAddress

    }

    public class ParliamentAuthContract : BaseContract<ParliamentMethod>
    {
        public ParliamentAuthContract(IApiHelper ch, string account) : base(ch, "AElf.Contracts.Parliament", account)
        {
        }

        public ParliamentAuthContract(IApiHelper ch, string callAddress, string contractAddress) : base(ch,
            contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}