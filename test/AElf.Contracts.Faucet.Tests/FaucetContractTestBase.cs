using AElf.Boilerplate.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;

namespace AElf.Contracts.Faucet
{
    public class FaucetContractTestBase : DAppContractTestBase<FaucetContractTestModule>
    {
        internal FaucetContractContainer.FaucetContractStub GetFaucetContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<FaucetContractContainer.FaucetContractStub>(DAppContractAddress, senderKeyPair);
        }
        
        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, senderKeyPair);
        }
    }
}